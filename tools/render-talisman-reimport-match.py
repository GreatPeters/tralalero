import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'outputs/talisman-polish-2026-09-20'
bpy.ops.wm.open_mainfile(filepath=str(root/'Talisman.blend'))
for o in list(bpy.data.objects):
    if o.type=='MESH' and o.name!='StudioGround':bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.import_scene.gltf(filepath=str(root/'Talisman.glb'))
bpy.context.scene.render.filepath=str(root/'reimport-matched.png')
bpy.ops.render.render(write_still=True)
