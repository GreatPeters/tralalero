import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'outputs/talisman-polish-2026-09-20'
bpy.ops.wm.open_mainfile(filepath=str(root/'Talisman.blend'))
m=bpy.data.materials.new('NeutralClay');m.use_nodes=True
p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.45,.45,.45,1);p.inputs['Roughness'].default_value=.65
bpy.context.view_layer.material_override=m
bpy.context.scene.render.filepath=str(root/'neutral.png');bpy.ops.render.render(write_still=True)
bpy.context.view_layer.material_override=None
for o in list(bpy.data.objects):
    if o.type!='MESH' or o.hide_render or o.name=='StudioGround':bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'TalismanAsset.blend'))
