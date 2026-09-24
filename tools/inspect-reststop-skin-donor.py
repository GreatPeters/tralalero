"""Inspect the existing verified mascot rig and suspect generated-hand weights."""
import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
sources=[ROOT/'outputs/approved-road-concepts-2026-09-13/humans/ParkingMarshal/ParkingMarshal.blend',
         ROOT/'outputs/reststop-production-2026-09-24/rigged/H01-r3/ParkingMarshal.blend']
result=[]
for source in sources:
    if not source.exists():continue
    bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
    rig=next(o for o in scene.objects if o.type=='ARMATURE');rig.data.pose_position='REST'
    bpy.context.view_layer.update()
    meshes=[o for o in scene.objects if o.type=='MESH']
    rows=[]
    for obj in meshes:
        points=[obj.matrix_world@v.co for v in obj.data.vertices]
        rows.append({'name':obj.name,'vertices':len(points),'lo':[min(p[i] for p in points) for i in range(3)],'hi':[max(p[i] for p in points) for i in range(3)],'scale':list(obj.scale),'groups':[g.name for g in obj.vertex_groups]})
    result.append({'source':str(source),'rig':rig.name,'rig_scale':list(rig.scale),'rig_location':list(rig.location),'meshes':rows,'actions':[a.name for a in bpy.data.actions],
                   'bones':{b.name:{'head':list(b.head_local),'tail':list(b.tail_local)} for b in rig.data.bones}})
(ROOT/'outputs/reststop-production-2026-09-24/skin-donor-inspection.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print(json.dumps([{k:v for k,v in r.items() if k!='bones'} for r in result]),flush=True)
