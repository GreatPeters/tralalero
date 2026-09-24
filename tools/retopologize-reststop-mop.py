"""Retopologize P04's accepted textured source after both automatic passes failed."""
import argparse
import importlib.util
import json
import math
from pathlib import Path
import runpy
import shutil
import sys

import bpy
from mathutils import Vector

root = Path(__file__).resolve().parents[1]
out = root / 'outputs/reststop-production-2026-09-24'
source = out / 'manual/P04-r1/texture1/model.glb'
parser = argparse.ArgumentParser()
parser.add_argument('--revision', type=int, required=True)
parser.add_argument('--voxel-fraction', type=float, default=.0015)
parser.add_argument('--geometry-source', type=Path)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
assert args.revision >= 3
assert .001 <= args.voxel_fraction <= .006
folder = out / ('manual/P04-r' + str(args.revision))
geometry_source = args.geometry_source.resolve() if args.geometry_source else None
assert not folder.exists()
for index in (1, 2):
    assert json.loads((source.parent / ('low' + str(index)) / 'visual-review.json').read_text())['verdict'] == 'mesh'
folder.mkdir(parents=True)
app = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
spec = importlib.util.spec_from_file_location('mop_refiner_helpers', app / 'blender_refine.py')
refiner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(refiner)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(meshes) == 1
high = meshes[0]
high.name = 'Detailed_Source'
refiner.active(high)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
lo, hi = refiner.bounds(high)
offset = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
for vertex in high.data.vertices:
    vertex.co -= offset
diagonal = (hi - lo).length
height = hi.z - lo.z
if geometry_source:
    existing = set(bpy.context.scene.objects)
    bpy.ops.import_scene.gltf(filepath=str(geometry_source))
    low = next(obj for obj in bpy.context.scene.objects if obj not in existing and obj.type == 'MESH')
    refiner.active(low)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    geometry_lo, geometry_hi = refiner.bounds(low)
    geometry_center = (geometry_lo + geometry_hi) * .5
    original_center = (lo + hi) * .5
    geometry_scale = height / (geometry_hi.z - geometry_lo.z)
    for vertex in low.data.vertices:
        vertex.co = (vertex.co - geometry_center) * geometry_scale + original_center - offset
else:
    low = high.copy()
    low.data = high.data.copy()
    bpy.context.collection.objects.link(low)
low.name = 'Asset_Optimized'
refiner.active(low)
refiner.clean(low, diagonal * 2e-6, merge=True)
low.data.normals_split_custom_set_from_vertices([(0., 0., 0.)] * len(low.data.vertices))
for polygon in low.data.polygons:
    polygon.use_smooth = True
prepared_surface = refiner.inspect(low)
print('Prepared surface: ' + json.dumps(prepared_surface), flush=True)
if not geometry_source:
    low.data.remesh_voxel_size = height * args.voxel_fraction
    low.data.remesh_voxel_adaptivity = 0
    low.data.use_remesh_preserve_volume = True
    bpy.ops.object.voxel_remesh()
voxel_triangles = refiner.tris(low)
print('Voxel surface triangles: ' + str(voxel_triangles), flush=True)
refiner.clean(low, diagonal * 1e-7, merge=False)
reduction_counts = [refiner.tris(low)]
for index in range(3):
    if 0 < refiner.tris(low) <= 15000:
        break
    modifier = low.modifiers.new('Controlled reduction of coherent voxel surface', 'DECIMATE')
    modifier.ratio = min(1., 14800 / refiner.tris(low))
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    refiner.clean(low, diagonal * 1e-7, merge=False)
    reduction_counts.append(refiner.tris(low))
    print('Reduction counts: ' + str(reduction_counts), flush=True)
if not 0 < refiner.tris(low) <= 15000:
    bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'failed-state.blend'))
    (folder / 'attempt-failure.json').write_text(json.dumps({'reason': 'Triangle target not met',
        'counts': reduction_counts, 'inspection': refiner.inspect(low)}, indent=2), encoding='utf8')
    raise RuntimeError('Controlled retopology did not meet the triangle target')
for polygon in low.data.polygons:
    polygon.use_smooth = True
low.data.set_sharp_from_angle(angle=math.radians(50))
for layer in list(low.data.uv_layers):
    low.data.uv_layers.remove(layer)
low.data.uv_layers.new(name='UVMap')
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.006)
bpy.ops.object.mode_set(mode='OBJECT')
refiner.bake(high, low, folder, 2048, diagonal)
refiner.active(low)
bpy.ops.export_scene.fbx(filepath=str(folder / 'model.fbx'), use_selection=True, object_types={'MESH'},
    axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True, add_leaf_bones=False)
bpy.ops.export_scene.gltf(filepath=str(folder / 'model.glb'), use_selection=True, export_format='GLB')
shutil.copy2(source, folder / 'trellis_source.glb')
shutil.copy2(out / 'reviews/P04-cached-stages/stage161/model.glb', folder / 'original-trellis-source.glb')
(folder / 'repair.json').write_text(json.dumps({'method': 'Voxel retopology of accepted textured source, controlled reduction, new UV and 2K PBR bake from unchanged source material',
    'source': str(source), 'geometry_source': str(geometry_source) if geometry_source else None,
    'prepared_surface': prepared_surface,
    'voxel_size': height * args.voxel_fraction, 'voxel_triangles': voxel_triangles,
    'triangles': refiner.tris(low), 'reduction_counts': reduction_counts,
    'original_automatic_reductions': 2, 'extra_ai_generations': 0}, indent=2), encoding='utf8')
refiner.studio(low, folder)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'model.blend'))
refiner.verify({'output': str(folder), 'target_triangles': 15000})
script = root / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(folder)]
runpy.run_path(str(script), run_name='__main__')
print(json.dumps({'folder': str(folder), 'visual_review_required': True}), flush=True)
