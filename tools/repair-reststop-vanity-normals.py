"""Repair T06's exterior planar shading while retaining basin/drain normals."""
import argparse
from array import array
import hashlib
import json
from pathlib import Path
import runpy
import shutil
import sys

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
SOURCE_SHA256 = '26bfb81cb3acac67ec351b080c280410e51024d453bc767b1551f9239056c0e2'
parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source, folder = Path(args.source).resolve(), Path(args.output).resolve()
assert source.is_relative_to(OUT / 'assets') and 'T06_' in str(source)
assert hashlib.sha256(source.read_bytes()).hexdigest() == SOURCE_SHA256
assert folder.is_relative_to(OUT / 'manual') and not folder.exists()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(objects) == 1
obj = objects[0]
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
mesh = obj.data
positions = [tuple(vertex.co) for vertex in mesh.vertices]
loop_vertices = [loop.vertex_index for loop in mesh.loops]
source_smooth = [polygon.use_smooth for polygon in mesh.polygons]
source_sharp = [edge.use_edge_sharp for edge in mesh.edges]
original_uvs = {}
for layer in mesh.uv_layers:
    values = array('f', [0.]) * (len(layer.data) * 2)
    layer.data.foreach_get('uv', values)
    original_uvs[layer.name] = values
source_normals = [normal.vector.copy() for normal in mesh.corner_normals]
assert min(normal.length for normal in source_normals) > .1
nonunit_source_normals = sum(abs(normal.length - 1.) > .001 for normal in source_normals)
normals = [normal.normalized() for normal in source_normals]
points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
span = hi - lo
assert min(span) > 0
normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
affected = set()
for polygon in mesh.polygons:
    center = obj.matrix_world @ polygon.center
    relative = [(center[i] - lo[i]) / span[i] for i in range(3)]
    normal = (normal_matrix @ polygon.normal).normalized()
    vertical = (abs(normal.x) > .96 and (relative[0] < .15 or relative[0] > .85)
                or abs(normal.y) > .96 and (relative[1] < .15 or relative[1] > .85))
    outer_boundary = (abs(normal.x) > .96 and (relative[0] < .025 or relative[0] > .975)
                      or abs(normal.y) > .96 and (relative[1] < .025 or relative[1] > .975))
    if vertical and (relative[2] < .82 or outer_boundary):
        affected.add(polygon.index)
assert 100 < len(affected) < len(mesh.polygons), 'Inspect an unexpected region selection'
modifier = obj.modifiers.new('Inspected exterior planar normals', 'WEIGHTED_NORMAL')
modifier.mode = 'FACE_AREA_WITH_ANGLE'
modifier.keep_sharp = True
modifier.weight = 60
bpy.ops.object.modifier_apply(modifier=modifier.name)
mesh = obj.data
weighted = [normal.vector.copy() for normal in mesh.corner_normals]
assert len(normals) == len(weighted)
assert positions == [tuple(vertex.co) for vertex in mesh.vertices]
assert loop_vertices == [loop.vertex_index for loop in mesh.loops]
merged = list(normals)
affected_loops = set()
materials = {}
for polygon in mesh.polygons:
    if polygon.index not in affected:
        continue
    for index in polygon.loop_indices:
        merged[index] = weighted[index]
        affected_loops.add(index)
    original = polygon.material_index
    if original not in materials:
        material = mesh.materials[original].copy()
        material.name = 'T06_Exterior_Planar_PBR'
        shader = next(node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
        for link in list(shader.inputs['Normal'].links):
            material.node_tree.links.remove(link)
        mesh.materials.append(material)
        materials[original] = len(mesh.materials) - 1
    polygon.material_index = materials[original]
for polygon, smooth in zip(mesh.polygons, source_smooth):
    polygon.use_smooth = smooth
for edge, sharp in zip(mesh.edges, source_sharp):
    edge.use_edge_sharp = sharp
edge_regions = [set() for _ in mesh.edges]
for polygon in mesh.polygons:
    for index in polygon.loop_indices:
        edge_regions[mesh.loops[index].edge_index].add(polygon.index in affected)
boundary_edges = 0
for edge, regions in zip(mesh.edges, edge_regions):
    if len(regions) > 1:
        edge.use_edge_sharp = True
        boundary_edges += 1
mesh.update()
mesh.normals_split_custom_set(normals)
mesh.update()
boundary_baseline = [normal.vector.normalized() for normal in mesh.corner_normals]
baseline_error = max((normal - source_normal).length
                     for normal, source_normal in zip(boundary_baseline, normals))
# Blender's encoded custom normals slightly change when a smooth fan splits.
# Compare the repair to the same split-boundary round trip, not raw lengths.
assert baseline_error < .006, baseline_error
mesh.normals_split_custom_set(merged)
mesh.update()
for layer in mesh.uv_layers:
    values = array('f', [0.]) * (len(layer.data) * 2)
    layer.data.foreach_get('uv', values)
    assert values == original_uvs[layer.name]
normal_error = max((normal.vector.normalized() - boundary_baseline[index]).length for index, normal in enumerate(mesh.corner_normals)
                   if index not in affected_loops)
if normal_error >= .001:
    loop_faces = {index: polygon for polygon in mesh.polygons for index in polygon.loop_indices}
    differences = sorted(((normal.vector.normalized() - normals[index]).length, index)
                         for index, normal in enumerate(mesh.corner_normals) if index not in affected_loops)
    print(json.dumps({'normal_diagnostics': [
        {'error': error, 'loop': index, 'smooth': loop_faces[index].use_smooth,
         'area': loop_faces[index].area, 'center': list(loop_faces[index].center),
         'before': list(normals[index]), 'after': list(mesh.corner_normals[index].vector)}
        for error, index in differences[-5:]]}), flush=True)
assert normal_error < .001, normal_error
mesh.calc_loop_triangles()
triangles = len(mesh.loop_triangles)
assert triangles == 14679
folder.mkdir(parents=True)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'model.blend'))
bpy.ops.export_scene.gltf(filepath=str(folder / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(folder / 'model.fbx'), use_selection=True, object_types={'MESH'},
                         axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True, add_leaf_bones=False)
shutil.copytree(source.parent / 'textures', folder / 'textures')
shutil.copy2(source.parent / 'trellis_source.glb', folder / 'trellis_source.glb')
(folder / 'repair.json').write_text(json.dumps({
    'source': str(source), 'source_sha256': SOURCE_SHA256, 'triangles': triangles,
    'method': 'Weighted corner normals plus removal of stale Normal input on inspected exterior vertical stone/wood planes; preserve original basin, drain and non-target normals/material links',
    'affected_faces': len(affected), 'affected_loops': len(affected_loops),
    'vertex_positions_loop_order_and_uvs_unchanged': True,
    'max_untargeted_normal_direction_error': normal_error,
    'boundary_only_roundtrip_max_direction_error': baseline_error,
    'nonunit_source_corner_normals_normalized': nonunit_source_normals,
    'normal_field_boundary_edges': boundary_edges,
    'extra_ai_requests': 0, 'extra_canonical_reduction_attempts': 0,
    'material_note': 'Keep both embedded materials. Normal.png remains valid on untreated surfaces and must not be reapplied to the corrected planar material.',
    'visual_review_required': True
}, indent=2), encoding='utf8')
script = ROOT / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(folder)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(folder / 'quality/hero.png', folder / 'preview.png')
print(json.dumps({'folder': str(folder), 'affected_faces': len(affected),
                  'visual_review_required': True}), flush=True)
