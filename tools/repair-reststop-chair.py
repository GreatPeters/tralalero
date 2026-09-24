"""Review a shading-only correction of the bounded F08 reduction result."""
import json
from pathlib import Path
import runpy
import sys
import bpy

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'outputs/reststop-production-2026-09-24/assets/F08_020af4bd20/stages/m1_t1/low2/model.glb'
OUT = ROOT / 'outputs/reststop-production-2026-09-24/manual/F08-r3'
assert not (OUT / 'model.blend').exists(), 'Preserve the accepted repair; choose a new revision path'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
before = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
for obj in meshes:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    low = min(v.co.z for v in obj.data.vertices)
    high = max(v.co.z for v in obj.data.vertices)
    group = obj.vertex_groups.new(name='FrameOnly')
    indices = [v.index for v in obj.data.vertices if (v.co.z-low)/(high-low) < .49]
    group.add(indices, 1, 'REPLACE')
    modifier = obj.modifiers.new('Frame_surface_normals', 'WEIGHTED_NORMAL')
    modifier.keep_sharp = True
    modifier.weight = 60
    modifier.vertex_group = group.name
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    replacements = {}
    for polygon in obj.data.polygons:
        if not all((obj.data.vertices[i].co.z-low)/(high-low) < .49 for i in polygon.vertices):
            continue
        old_index = polygon.material_index
        if old_index not in replacements:
            material = obj.data.materials[old_index].copy()
            material.name = 'Frame_PBR_repaired'
            shader = material.node_tree.nodes.get('Principled BSDF')
            for link in list(material.node_tree.links):
                if link.to_node == shader and link.to_socket.name == 'Normal':
                    material.node_tree.links.remove(link)
            obj.data.materials.append(material)
            replacements[old_index] = len(obj.data.materials)-1
        polygon.material_index = replacements[old_index]
    obj.select_set(False)
after = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
assert before == after and after <= 15000
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'model.blend'))
bpy.ops.export_scene.gltf(filepath=str(OUT / 'model.glb'), export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(OUT / 'model.fbx'), use_selection=False, object_types={'MESH'}, add_leaf_bones=False, path_mode='COPY', embed_textures=True)
(OUT / 'repair.json').write_text(json.dumps({'source': str(SOURCE), 'method': 'weighted frame normals plus removal of the damaged baked normal channel on lower-frame faces only; geometry, UVs and other PBR channels retained', 'triangles_before': before, 'triangles_after': after}, indent=2), encoding='utf8')
sys.argv = [str(ROOT / 'tools/reststop-production-review-render.py'), '--', str(OUT)]
runpy.run_path(str(ROOT / 'tools/reststop-production-review-render.py'), run_name='__main__')
