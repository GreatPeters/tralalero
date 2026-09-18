"""Deduplicate OOXML style records without changing cells, formulas or layout.

Artifact Tool has no public style-table compaction API. This repair operates on
the package records and checks effective style identity for every original XF.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET
import zipfile

NS = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'
ET.register_namespace('', NS)
STYLE_TAG = re.compile(rb'<(?:[\w]+:)?(?:c|row|col)\b[^>]*>')
STYLE_ATTRIBUTE = re.compile(rb'\b(s|style)="(\d+)"')


def digest(element, attributes=None):
    node = copy.deepcopy(element)
    if attributes is not None:
        node.attrib.clear()
        node.attrib.update(attributes)
    return hashlib.sha256(ET.tostring(node)).hexdigest()


def effective_xfs(root):
    parts = {node.tag.split('}')[-1]: node for node in root}
    refs = {
        'fontId': [digest(node) for node in parts['fonts']],
        'fillId': [digest(node) for node in parts['fills']],
        'borderId': [digest(node) for node in parts['borders']],
    }
    formats = {int(node.attrib['numFmtId']):
               digest(node, {k: v for k, v in node.attrib.items() if k != 'numFmtId'})
               for node in parts.get('numFmts', [])}

    def signature(node, parents=None):
        attributes = dict(node.attrib)
        for field, entries in refs.items():
            if field in attributes:
                attributes[field] = entries[int(attributes[field])]
        if 'numFmtId' in attributes:
            value = int(attributes['numFmtId'])
            attributes['numFmtId'] = formats.get(value, 'builtin:' + str(value))
        if parents is not None and 'xfId' in attributes:
            attributes['xfId'] = parents[int(attributes['xfId'])]
        return digest(node, attributes)

    parents = [signature(node) for node in parts['cellStyleXfs']]
    cells = [signature(node, parents) for node in parts['cellXfs']]
    named = [signature(node, parents) for node in parts.get('cellStyles', [])]
    return cells, named


def compact_styles(root):
    parts = {node.tag.split('}')[-1]: node for node in root}
    counts = {name: len(node) for name, node in parts.items()}
    maps = {}

    def deduplicate(name):
        parent = parts[name]
        unique, lookup, mapping = [], {}, {}
        for index, node in enumerate(list(parent)):
            key = ET.tostring(node)
            if key not in lookup:
                lookup[key] = len(unique)
                unique.append(node)
            mapping[index] = lookup[key]
        parent[:] = unique
        parent.set('count', str(len(unique)))
        return mapping

    for name, field in [('fonts', 'fontId'), ('fills', 'fillId'), ('borders', 'borderId')]:
        maps[field] = deduplicate(name)
    if 'numFmts' in parts:
        parent = parts['numFmts']
        unique, lookup, mapping = [], {}, {}
        for node in list(parent):
            key = tuple(sorted((k, v) for k, v in node.attrib.items() if k != 'numFmtId'))
            old = int(node.attrib['numFmtId'])
            if key not in lookup:
                lookup[key] = old
                unique.append(node)
            mapping[old] = lookup[key]
        parent[:] = unique
        parent.set('count', str(len(unique)))
        maps['numFmtId'] = mapping

    def remap(node, fields):
        for field in fields:
            if field in node.attrib:
                old = int(node.attrib[field])
                node.set(field, str(maps[field].get(old, old)))

    for name in ('cellStyleXfs', 'cellXfs'):
        for node in parts[name]:
            remap(node, ('fontId', 'fillId', 'borderId', 'numFmtId'))
        if name == 'cellStyleXfs':
            maps['xfId'] = deduplicate(name)
        else:
            for node in parts[name]:
                remap(node, ('xfId',))
            cell_map = deduplicate(name)
    for node in parts.get('cellStyles', []):
        remap(node, ('xfId',))
    return cell_map, counts, {name: len(node) for name, node in parts.items()}


def rewrite_sheet(data, mapping):
    def tag(match):
        return STYLE_ATTRIBUTE.sub(lambda attr: attr[1] + b'="' +
                                   str(mapping[int(attr[2])]).encode() + b'"', match[0])
    return STYLE_TAG.sub(tag, data)


def without_style_ids(data):
    return STYLE_TAG.sub(lambda tag: STYLE_ATTRIBUTE.sub(b'', tag[0]), data)


def compact(source, destination):
    with zipfile.ZipFile(source) as archive:
        style_bytes = archive.read('xl/styles.xml')
        root = ET.fromstring(style_bytes)
        old_cells, old_named = effective_xfs(root)
        mapping, before, after = compact_styles(root)
        new_cells, new_named = effective_xfs(root)
        assert old_named == new_named, 'Named style appearance changed'
        assert all(old_cells[index] == new_cells[target] for index, target in mapping.items()), 'Cell style appearance changed'
        new_styles = ET.tostring(root, encoding='utf-8', xml_declaration=True)
        destination.parent.mkdir(parents=True, exist_ok=True)
        changed_parts = []
        with zipfile.ZipFile(destination, 'x', compression=zipfile.ZIP_DEFLATED) as output:
            for info in archive.infolist():
                original = archive.read(info.filename)
                data = original
                if info.filename == 'xl/styles.xml':
                    data = new_styles
                elif re.fullmatch(r'xl/worksheets/[^/]+\.xml', info.filename):
                    data = rewrite_sheet(original, mapping)
                    assert without_style_ids(original) == without_style_ids(data), info.filename
                if data != original:
                    changed_parts.append(info.filename)
                # writestr updates ZipInfo offsets; do not mutate the source index.
                output.writestr(copy.copy(info), data)
        with zipfile.ZipFile(destination) as output:
            assert archive.namelist() == output.namelist(), 'Package members changed'
            for name in archive.namelist():
                if name not in changed_parts:
                    assert archive.read(name) == output.read(name), name
    report = {'source': str(source), 'destination': str(destination),
              'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
              'outputSha256': hashlib.sha256(destination.read_bytes()).hexdigest(),
              'beforeCounts': before, 'afterCounts': after,
              'styleBytesBefore': len(style_bytes), 'styleBytesAfter': len(new_styles),
              'fileBytesBefore': source.stat().st_size, 'fileBytesAfter': destination.stat().st_size,
              'effectiveCellStylesVerified': len(old_cells), 'changedParts': changed_parts,
              'nonStyleWorksheetBytesUnchanged': True, 'otherPackagePartsUnchanged': True}
    destination.with_suffix('.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('destination', type=Path)
    args = parser.parse_args()
    print(json.dumps(compact(args.source, args.destination), indent=2))
