import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from timeline import route,rail_frame,lane_offset,angle_delta
OUT=Path.cwd()/'outputs/reststop-blender-v4-2026-09-23';bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-v4.blend'));s=bpy.context.scene;p=bpy.data.objects['PLAYER_ROOT'];rail=bpy.data.objects['AUTO_RAIL_ANCHOR'];cam=s.camera
assert s.render.fps==24 and s.frame_end==7200 and p.parent==rail
assert not any('cart' in o.name.lower() for o in s.objects)
errors=[];max_lane=0.;samples=[]
for frame in sorted(set(list(range(1,7201,24))+[round(t*24)+1 for t in [9,31.5,35.5,43.5,47,57,87,179,207,235,263,288]])):
 s.frame_set(frame);t=(frame-1)/24;target=route(t);world=p.matrix_world.translation;err=math.hypot(world.x-target[0],world.y-target[1]);errors.append(err);max_lane=max(max_lane,abs(p.location.x));assert err<.002,(frame,list(world),target,err);assert abs(p.location.y)<1e-6
 samples.append({'frame':frame,'world':list(world),'lane_offset':p['lane_offset']})
rates=[];last=None
for frame in range(2400,3842):
 s.frame_set(frame);world=p.matrix_world.translation;angle=p.matrix_world.to_euler().z
 if frame>=2401:assert abs(world.x)<.001 and abs(world.y-42)<.001 and abs(p['lane_offset'])<1e-6
 if last is not None:rates.append(abs(angle_delta(last,angle))*24*180/math.pi)
 last=angle
assert max(rates)<90.01,max(rates)
# At one fixed progress value, change only lateral input. The camera and rail
# do not move, and the location constraint rejects local forward displacement.
s.frame_set(25*24+1);anchor=rail.matrix_world.translation.copy();camera=cam.matrix_world.copy();deltas=[]
for value in (-999,999):
 p['lane_offset']=value;p.location.y=50;bpy.context.view_layer.update();w=p.matrix_world.translation;local=rail.matrix_world.inverted()@w;assert abs(local.y)<.0001 and abs(abs(local.x)-2.2)<.0001;assert (cam.matrix_world.translation-camera.translation).length<1e-8;deltas.append(list(local))
p.location.y=0;s.frame_set(1)
counts=json.loads(s['vehicle_types']);assert len(counts)==6 and sum(counts.values())==91 and all(v>0 for v in counts.values())
for o in s.objects:assert all(math.isfinite(v) for row in o.matrix_world for v in row),o.name
report={'native_reopened':True,'fps':24,'frames':7200,'carts_remaining':0,'vehicle_counts':counts,'vehicle_total':91,'max_lane_position_error':max(errors),'max_lateral_offset':max_lane,'rail_samples':len(samples),'forward_input_rejected':True,'camera_independent_of_lateral_input':True,'extreme_input_clamped_positions':deltas,'defense_max_degrees_s':max(rates),'defense_stationary_seconds':60}
(OUT/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8');print('V4_VALIDATED',json.dumps(report),flush=True)
bpy.ops.object.select_all(action='DESELECT')
for o in s.objects:
 if o.type not in ('MESH','CURVE','FONT'):continue
 chain=[o];a=o.parent
 while a is not None:chain.append(a);a=a.parent
 if any(a.get('role') or a.get('assembly') in ('actor','player') for a in chain):continue
 if any(a.animation_data and not a.get('camera_cutaway') for a in chain):continue
 o.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'reststop-v4-environment.glb'),export_format='GLB',use_selection=True,export_animations=False,export_apply=True,export_cameras=False,export_lights=False);print('V4_EXPORTED',flush=True)
