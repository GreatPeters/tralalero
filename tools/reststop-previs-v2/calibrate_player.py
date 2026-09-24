"""Calibrate the evaluated first-frame body, excluding the aim indicator."""
import bpy,json
from pathlib import Path
out=Path.cwd()/'outputs/reststop-blender-v2-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(out/'reststop-v2.blend'));s=bpy.context.scene;s.frame_set(1)
p=bpy.data.objects['PLAYER_ROOT'];cal=bpy.data.objects['Game_visual_calibration'];inv=p.matrix_world.inverted();dg=bpy.context.evaluated_depsgraph_get()
pts=[inv@o.evaluated_get(dg).matrix_world@v.co for o in cal.children_recursive if o.type=='MESH' for v in o.evaluated_get(dg).data.vertices]
measured=[max(v[i] for v in pts)-min(v[i] for v in pts) for i in range(3)];target=[2.74950361,5.190148,3.262378]
cal.scale=tuple(cal.scale[i]*target[i]/measured[i] for i in range(3));bpy.context.view_layer.update()
s['body_size_basis']='Evaluated body at frame 1, excluding aim indicator; gait changes posed extents.'
s['final_calibrated']=True
bpy.ops.wm.save_as_mainfile(filepath=str(out/'reststop-v2.blend'),compress=True)
m=json.loads((out/'build-metrics.json').read_text());m['visual_calibration']=list(cal.scale);m['shark_calibrated_envelope']=target;m['body_size_basis']=s['body_size_basis'];(out/'build-metrics.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf8')
print('BODY_CALIBRATED',measured,target,flush=True)
