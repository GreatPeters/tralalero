"""Own the FlatKit passes locally, preserving their shared HLSL and buffer layout.

Unity 6000.2.6f1 asserts when UsePass shares these passes across the 71/75-keyword
spaces. Run --check after a FlatKit update; run without it to resync pass bodies.
The custom OutlineLegacy pass and material properties are never regenerated.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SHADERS = ROOT / 'Assets/FlatKit/Shaders/StylizedSurface'
source = (SHADERS / 'StylizedSurface.shader').read_text(encoding='utf-8')
path = SHADERS / 'StylizedSurfaceOutline.shader'
target = path.read_text(encoding='utf-8')
names = ('ForwardLit', 'ShadowCaster', 'GBuffer', 'DepthOnly', 'DepthNormals', 'Meta')

def passes(text):
    result = {}
    for match in re.finditer(r'(?m)^[ \t]*Pass\s*\n\s*\{', text):
        start = match.start()
        pos = text.index('{', start)
        depth = 1
        end = pos + 1
        while depth:
            if text[end] == '{': depth += 1
            elif text[end] == '}': depth -= 1
            end += 1
        block = text[start:end]
        name = re.search(r'\bName\s+"([^"]+)"', block).group(1)
        result[name] = (start, end, block)
    return result

canonical = passes(source)
own = passes(target)
if '--check' in sys.argv:
    assert not re.search(r'^\s*UsePass\s', target, re.MULTILINE)
    for name in names:
        assert own[name][2].strip() == canonical[name][2].strip(), name
    print('Six local pass bodies match the current FlatKit source.')
else:
    for name in names:
        directive = f'        UsePass "FlatKit/Stylized Surface/{name}"'
        if directive in target:
            target = target.replace(directive, canonical[name][2], 1)
        else:
            start, end, _ = passes(target)[name]
            target = target[:start] + canonical[name][2] + target[end:]
    if 'HLSLINCLUDE' not in target:
        begin = source.index('HLSLINCLUDE')
        end = source.index('ENDHLSL', begin) + len('ENDHLSL')
        header = '\n'.join('        ' + line.strip() for line in source[begin:end].splitlines())
        target = target.replace('        LOD 300', '        LOD 300\n\n' + header, 1)
    target = target.replace(
        "        // Every pass shares FlatKit's UnityPerMaterial layout (LibraryUrp/StylizedInput.hlsl),\n"
        "        // which keeps this shader SRP Batcher compatible. The previous CG outline pass and the\n"
        "        // SimpleLit-based passes used other layouts, so each object cost its own SetPass.",
        "        // Declare these passes in this shader's keyword space. Cross-shader UsePass\n"
        "        // raises native keyword-state assertions on Unity 6000.2.6f1 (71 versus 75).\n"
        "        // Pass bodies stay synced with StylizedSurface.shader by\n"
        "        // tools/sync-legacy-outline-passes.py; shared StylizedInput keeps SRP batching.")
    path.write_text(target, encoding='utf-8')
    print('Updated six owned passes; preserved OutlineLegacy and all properties.')
