"""Preserve every baked pose key in the Unity-bound FBX, retaining prior exports."""
import json
import sys
from pathlib import Path
import bpy

root = Path(sys.argv[sys.argv.index('--')+1])
for folder in sorted((root/'outputs/chapters-polish-2026-09-12/rigged/v2').iterdir()):
    if not folder.is_dir():
        continue
    name = folder.name
    destination = folder/(name+'-full-keys.fbx')
    if destination.exists():
        continue
    bpy.ops.wm.open_mainfile(filepath=str(folder/(name+'.blend')))
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    rig.animation_data.action = bpy.data.actions['idle']
    bpy.context.scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj == rig or (obj.type == 'MESH' and obj.parent == rig):
            obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(filepath=str(destination), use_selection=True, add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_step=1, bake_anim_simplify_factor=0, path_mode='COPY', embed_textures=True)
    print(json.dumps({'name': name, 'file': str(destination), 'keySimplification': 0}), flush=True)
