"""Diagnose contact and dominant weights without changing the candidate."""
import json
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
folder=ROOT/'outputs/reststop-production-2026-09-24/rigged/H01-r6'
bpy.ops.wm.open_mainfile(filepath=str(folder/'ParkingMarshal.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
body=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.find_armature()==rig)
report={'body_matrix':[list(r) for r in body.matrix_world], 'rig_matrix':[list(r) for r in rig.matrix_world]}
for action_name,frame in [('walk',8),('attack_once',10),('idle',1)]:
    action=bpy.data.actions[action_name];rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
    evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
    ids=sorted(range(len(mesh.vertices)),key=lambda i:(evaluated.matrix_world@mesh.vertices[i].co).z)[:12]
    report[action_name]=[{'id':i,'rest':list(body.data.vertices[i].co),'pose':list(evaluated.matrix_world@mesh.vertices[i].co),'weights':{body.vertex_groups[g.group].name:g.weight for g in body.data.vertices[i].groups}} for i in ids]
    evaluated.to_mesh_clear()
    report[action_name+'_root']=list(rig.pose.bones['Root'].location)
(folder/'contact-diagnostic.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report),flush=True)
rig.data.pose_position='REST';bpy.context.view_layer.update()
report['vertices']=[list(v.co) for v in body.data.vertices]
report['bones']={b.name:[list(b.head_local),list(b.tail_local)] for b in rig.data.bones}
(folder/'rest-diagnostic.json').write_text(json.dumps(report))
