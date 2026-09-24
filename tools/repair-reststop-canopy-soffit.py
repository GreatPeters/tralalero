"""Correct only the inspected canopy soffit after its three AI textures failed."""
import argparse
import hashlib
import json
import runpy
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(args.source).resolve()
output = Path(args.output).resolve()
assert not output.exists(), 'Preserve prior correction revisions'
image_hash = hashlib.sha256((OUT / 'inputs/G01.png').read_bytes()).hexdigest()
ledger_path = OUT / 'quality-ledgers' / (image_hash + '.json')
ledger = json.loads(ledger_path.read_text(encoding='utf8'))
assert ledger['terminal'] == 'review_needed'
assert sum(len(mesh['textures']) for mesh in ledger['meshes']) == 3
assert source.parent == Path(ledger['meshes'][0]['textures'][0]['folder'])
output.mkdir(parents=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(meshes) == 1
body = meshes[0]
bpy.context.view_layer.objects.active = body
body.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
vertices_before = [tuple(vertex.co) for vertex in body.data.vertices]
lo = Vector(tuple(min(v.co[i] for v in body.data.vertices) for i in range(3)))
hi = Vector(tuple(max(v.co[i] for v in body.data.vertices) for i in range(3)))
dimensions = hi - lo
long_axis = 0 if dimensions.x >= dimensions.y else 1
cross_axis = 1 - long_axis
length, width, height = dimensions[long_axis], dimensions[cross_axis], dimensions.z
down = sorted((polygon.center.z, polygon.area) for polygon in body.data.polygons if polygon.normal.z < -.85)
assert down
half_area = sum(area for z, area in down) / 2
accumulated = 0
for soffit_z, area in down:
    accumulated += area
    if accumulated >= half_area:
        break
def material(name, color, metallic, roughness):
    value = bpy.data.materials.new(name)
    value.use_nodes = True
    shader = value.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Metallic'].default_value = metallic
    shader.inputs['Roughness'].default_value = roughness
    return value
soffit = material('Corrected pale soffit', (.80, .79, .76), 0, .65)
seam = material('Subtle soffit panel joints', (.57, .57, .54), 0, .7)
frame = material('Flush light frame', (.24, .26, .27), .65, .3)
diffuser = material('Flush light diffuser', (.91, .89, .80), 0, .45)
body.data.materials.append(soffit)
soffit_index = len(body.data.materials) - 1
selected_area = 0
selected_faces = 0
margin = width * .025
for polygon in body.data.polygons:
    c = polygon.center
    if polygon.normal.z < -.6 and c.z <= soffit_z + height * .10 and lo.x + margin < c.x < hi.x - margin and lo.y + margin < c.y < hi.y - margin:
        polygon.material_index = soffit_index
        selected_area += polygon.area
        selected_faces += 1
assert selected_area > length * width * .60, selected_area
assert vertices_before == [tuple(vertex.co) for vertex in body.data.vertices]
def point(u, v, z):
    result = [0., 0., z]
    result[long_axis] = (lo[long_axis] + hi[long_axis]) / 2 + u
    result[cross_axis] = (lo[cross_axis] + hi[cross_axis]) / 2 + v
    return tuple(result)
centers = []
side = min(length * .05, width * .15)
for column, u in enumerate((-.31 * length, 0, .31 * length)):
    for row, v in enumerate((-.25 * width, .25 * width)):
        positions = []
        for scale, z in ((1, soffit_z - length * .00005), (1, soffit_z - length * .0008), (.76, soffit_z - length * .0008), (.76, soffit_z - length * .0006)):
            for x, y in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                positions.append(point(u + x * side * scale / 2, v + y * side * scale / 2, z))
        faces = []
        for i in range(4):
            j = (i + 1) % 4
            faces.extend(((4+i, 4+j, j, i), (4+i, 8+i, 8+j, 4+j), (8+i, 12+i, 12+j, 8+j)))
        faces.extend(((15, 14, 13, 12), (0, 1, 2, 3)))
        if long_axis == 1:
            faces = [tuple(reversed(face)) for face in faces]
        mesh = bpy.data.meshes.new('Flush light geometry')
        mesh.from_pydata(positions, [], faces)
        mesh.materials.append(frame)
        mesh.materials.append(diffuser)
        mesh.polygons[-2].material_index = 1
        light = bpy.data.objects.new(f'SoffitLight_{column+1}_{row+1}', mesh)
        bpy.context.collection.objects.link(light)
        centers.append(point(u, v, soffit_z))
seam_vertices = []
seam_faces = []
for index in range(1, 17):
    u = length * (-.475 + .95 * index / 17)
    start = len(seam_vertices)
    seam_vertices.extend(point(x, y, soffit_z - length * .0001) for x, y in ((u-length*.00025, -width*.46), (u+length*.00025, -width*.46), (u+length*.00025, width*.46), (u-length*.00025, width*.46)))
    face = tuple(start + i for i in (3, 2, 1, 0))
    seam_faces.append(tuple(reversed(face)) if long_axis == 1 else face)
mesh = bpy.data.meshes.new('Soffit panel joint lines')
mesh.from_pydata(seam_vertices, [], seam_faces)
mesh.materials.append(seam)
obj = bpy.data.objects.new('SoffitPanelJoints', mesh)
bpy.context.collection.objects.link(obj)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), export_format='GLB')
report = {'source': str(source), 'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
          'original_ledger': str(ledger_path), 'ai_texture_attempts_retained': 3, 'extra_ai_requests': 0,
          'method': 'Retain first TRELLIS roof/fascia/UV/PBR; replace only underside material, add six geometric framed flush lights in a 2x3 layout and subtle panel joints',
          'original_body_vertices_unchanged': True, 'soffit_faces_reassigned': selected_faces, 'soffit_area': selected_area,
          'soffit_z': soffit_z, 'long_axis': long_axis, 'fixture_centers': centers, 'fixture_side': side,
          'runtime_light_objects': 0}
(output / 'repair.json').write_text(json.dumps(report, indent=2), encoding='utf8')
script = ROOT / 'tools/reststop-production-review-render.py'
for flags in ([], ['--underside-only']):
    sys.argv = [str(script), '--', str(output), *flags]
    runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output / 'quality/bottom.png', output / 'preview.png')
