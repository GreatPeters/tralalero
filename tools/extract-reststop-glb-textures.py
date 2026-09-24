"""Extract original GLB PBR channels without selected-to-active rebaking."""
import argparse
import hashlib
import io
import json
import struct
from pathlib import Path

from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('--folder', required=True)
args = parser.parse_args()
folder = Path(args.folder).resolve()
raw = (folder / 'model.glb').read_bytes()
magic, version, length = struct.unpack_from('<III', raw)
assert magic == 0x46546C67 and version == 2 and length == len(raw)
chunks = {}
offset = 12
while offset < len(raw):
    size, kind = struct.unpack_from('<II', raw, offset)
    chunks[kind] = raw[offset + 8:offset + 8 + size]
    offset += 8 + size
data = json.loads(chunks[0x4E4F534A])
binary = chunks[0x004E4942]
assert len(data['materials']) == 1, 'Inspect multi-material assets separately'
material = data['materials'][0]
pbr = material['pbrMetallicRoughness']
assert pbr.get('baseColorFactor', [1, 1, 1, 1]) == [1, 1, 1, 1]
assert pbr.get('roughnessFactor', 1) == 1 and pbr.get('metallicFactor', 1) == 1
def image_bytes(texture_index):
    image = data['images'][data['textures'][texture_index]['source']]
    assert image['mimeType'] == 'image/png' and 'bufferView' in image
    view = data['bufferViews'][image['bufferView']]
    assert view['buffer'] == 0
    start = view.get('byteOffset', 0)
    return binary[start:start + view['byteLength']]
textures = folder / 'textures'
assert not textures.exists(), 'Preserve previously extracted texture sets'
base = image_bytes(pbr['baseColorTexture']['index'])
with Image.open(io.BytesIO(base)) as image:
    dimensions = image.size
assert dimensions == (2048, 2048)
textures.mkdir()
(textures / 'BaseColor.png').write_bytes(base)
with Image.open(io.BytesIO(image_bytes(pbr['metallicRoughnessTexture']['index']))) as image:
    assert image.size == dimensions
    channels = image.convert('RGB').split()
    channels[1].save(textures / 'Roughness.png')
    channels[2].save(textures / 'Metallic.png')
if 'normalTexture' in material:
    (textures / 'Normal.png').write_bytes(image_bytes(material['normalTexture']['index']))
    normal_note = 'Original normal texture extracted.'
else:
    Image.new('RGB', dimensions, (128, 128, 255)).save(textures / 'Normal.png')
    normal_note = 'Original material has no normal map. This flat placeholder is unconnected; preserve the embedded material.'
receipt = {'method': 'Original BaseColor PNG plus separate G roughness/B metallic from the embedded glTF packed texture',
           'dimensions': dimensions, 'double_sided': material.get('doubleSided', False), 'normal_note': normal_note,
           'files': {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(textures.glob('*.png'))}}
(folder / 'texture-extraction.json').write_text(json.dumps(receipt, indent=2), encoding='utf8')
print(json.dumps(receipt), flush=True)
