"""Inspect the saved high material's linked alpha without editing the model."""
import json
from pathlib import Path
import bpy

root = Path(__file__).resolve().parents[1]
folder = root / 'outputs/reststop-production-2026-09-24'
path = folder / 'reviews/S10-alpha-node-diagnostic-r1.json'
assert not path.exists()
bpy.ops.wm.open_mainfile(filepath=str(folder / 'manual/S10-r1/source/low1/model.blend'))
records = []
for mat in bpy.data.objects['Detailed_Source'].data.materials:
    shader = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    alpha = shader.inputs['Alpha']
    record = {'material': mat.name, 'alpha_default': alpha.default_value, 'linked': alpha.is_linked, 'nodes': []}
    for node in mat.node_tree.nodes:
        if node.type in ('MATH', 'TEX_IMAGE'):
            record['nodes'].append({'name': node.name, 'type': node.type,
                'operation': getattr(node, 'operation', None),
                'image': node.image.name if node.type == 'TEX_IMAGE' and node.image else None,
                'inputs': [{'name': s.name, 'linked': s.is_linked,
                            'value': float(s.default_value) if hasattr(s, 'default_value') and isinstance(s.default_value, (int, float)) else None,
                            'from': [(l.from_node.name, l.from_socket.name) for l in s.links]} for s in node.inputs]})
    record['alpha_from'] = [(link.from_node.name, link.from_socket.name) for link in alpha.links]
    records.append(record)
path.write_text(json.dumps(records, indent=2), encoding='utf8')
print(json.dumps(records), flush=True)
