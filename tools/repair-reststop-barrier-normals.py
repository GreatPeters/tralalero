"""Repair R07's broad flat panels while preserving rounded-part normal shading."""
import json
from pathlib import Path
import runpy
import shutil
import sys

import bpy

root = Path(__file__).resolve().parents[1]
out = root / 'outputs/reststop-production-2026-09-24'
source = out / 'assets/R07_9086977162/stages/m1_t1/low2'
folder = out / 'manual/R07-r2'
assert not folder.exists()
folder.mkdir()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source / 'model.glb'))
objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(objects) == 1
obj = objects[0]
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
mesh = obj.data
positions = [tuple(vertex.co) for vertex in mesh.vertices]
normals = [normal.vector.copy() for normal in mesh.corner_normals]
heights = [(obj.matrix_world @ vertex.co).z for vertex in mesh.vertices]
zmin, zmax = min(heights), max(heights)
height = zmax - zmin
normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
affected = set()
for polygon in mesh.polygons:
    z = sum(heights[index] for index in polygon.vertices) / len(polygon.vertices)
    fraction = (z - zmin) / height
    normal = (normal_matrix @ polygon.normal).normalized()
    flat_panel = .07 < fraction < .76 and max(abs(normal.x), abs(normal.y)) > .94
    base = fraction < .07 and max(abs(value) for value in normal) > .94
    if flat_panel or base:
        affected.add(polygon.index)
modifier = obj.modifiers.new('Weighted normals for inspected flat regions', 'WEIGHTED_NORMAL')
modifier.mode = 'FACE_AREA_WITH_ANGLE'
modifier.keep_sharp = True
modifier.weight = 60
bpy.ops.object.modifier_apply(modifier=modifier.name)
weighted = [normal.vector.copy() for normal in mesh.corner_normals]
assert len(normals) == len(weighted) and positions == [tuple(vertex.co) for vertex in mesh.vertices]
merged = list(normals)
materials = {}
for polygon in mesh.polygons:
    if polygon.index not in affected:
        continue
    for index in polygon.loop_indices:
        merged[index] = weighted[index]
    original = polygon.material_index
    if original not in materials:
        material = mesh.materials[original].copy()
        material.name = 'Flat cabinet and base'
        shader = next(node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
        for link in list(shader.inputs['Normal'].links):
            material.node_tree.links.remove(link)
        mesh.materials.append(material)
        materials[original] = len(mesh.materials) - 1
    polygon.material_index = materials[original]
mesh.normals_split_custom_set(merged)
triangles = sum(len(polygon.vertices) - 2 for polygon in mesh.polygons)
assert triangles == 13727 and affected
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'model.blend'))
bpy.ops.export_scene.gltf(filepath=str(folder / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(folder / 'model.fbx'), use_selection=True, object_types={'MESH'},
    axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True, add_leaf_bones=False)
shutil.copytree(source / 'textures', folder / 'textures')
shutil.copy2(source / 'trellis_source.glb', folder / 'trellis_source.glb')
(folder / 'repair.json').write_text(json.dumps({'source': str(source), 'triangles': triangles,
    'method': 'Weighted normals and disabled stale normal bake on inspected broad vertical cabinet faces and base; retain original corner normals and normal-map links on cap, barrel and rounded edges',
    'affected_faces': len(affected), 'vertex_positions_and_uvs_unchanged': True,
    'extra_ai_requests': 0, 'extra_reductions': 0}, indent=2), encoding='utf8')
script = root / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(folder)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(folder / 'quality/hero.png', folder / 'preview.png')
