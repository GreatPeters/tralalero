import bpy
import json
from pathlib import Path

root=Path.cwd()
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(root/'Assets/ShooterSurvival/Models/Chapters/Mascots/CoffeeVendor/CoffeeVendor.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
action=next(a for a in bpy.data.actions if a.name.endswith('attack_once'))
rig.animation_data.action=action
if action.slots:rig.animation_data.action_slot=action.slots[0]
frames=[]
for frame in range(1,32):
    bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
    p=rig.matrix_world@rig.pose.bones['Hand.R'].head
    frames.append({'frame':frame,'seconds':(frame-1)/30,'position':list(p)})
out=root/'tmp/campaign-balance-2026-09-23/reststop-model-inspection/coffee-throw-trajectory.json'
out.write_text(json.dumps(frames,indent=2),encoding='utf-8')
print('Forward-most hand:',min(frames,key=lambda row:row['position'][1]))
