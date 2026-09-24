"""Use evaluated geometry for freshly imported skin bounds, preserving the standard evidence rig."""
import importlib.util
from pathlib import Path
import bpy
from mathutils import Vector

path=Path.home()/'.codex/skills/blender-asset-validation/scripts/render_evidence.py'
spec=importlib.util.spec_from_file_location('standard_evidence',path)
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
original_load=module.load_asset
def load(path):
    original_load(path)
    for obj in bpy.context.scene.objects:
        if any(c.name=='glTF_not_exported' for c in obj.users_collection):obj.hide_render=True
    if path.suffix.lower()=='.glb':bpy.context.scene.frame_set(15,subframe=.4)
    bpy.context.view_layer.update()
def bounds():
    points=[];dg=bpy.context.evaluated_depsgraph_get()
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH' or obj.hide_render:continue
        evaluated=obj.evaluated_get(dg);mesh=evaluated.to_mesh()
        points.extend(evaluated.matrix_world@v.co for v in mesh.vertices);evaluated.to_mesh_clear()
    if not points:raise RuntimeError('No visible evaluated mesh')
    return Vector(tuple(min(p[i] for p in points) for i in range(3))),Vector(tuple(max(p[i] for p in points) for i in range(3)))
module.load_asset=load;module.scene_bounds=bounds;module.main()
