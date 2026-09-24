"""Native invariants, game calibration, scale and independent environment export."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from timeline import STAGES,FPS,SPEED,route,defense,HOLDS,LENGTHS
OUT=Path.cwd()/'outputs/reststop-blender-v2-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-v2.blend'));s=bpy.context.scene;p=bpy.data.objects['PLAYER_ROOT'];cam=s.camera
assert s.render.fps==24 and s.frame_end==7200
rates=[];last=None;cam_positions=[]
for frame in range(2400,3842):
 s.frame_set(frame);loc=p.matrix_world.translation
 if frame>=2401:assert abs(loc.x)<.001 and abs(loc.y-42)<.001
 if last is not None:rates.append(abs(p.rotation_euler.z-last)*24*180/math.pi)
 last=p.rotation_euler.z
 if frame>=2401 and frame%24==1:cam_positions.append(list(cam.location))
assert max(rates)<=90.001,max(rates)
assert max(Vector(v).z for v in cam_positions)-min(Vector(v).z for v in cam_positions)<.001
assert abs(cam_positions[0][0])>10 and cam_positions[0][2]<40
s.frame_set(1);inv=p.matrix_world.inverted();dg=bpy.context.evaluated_depsgraph_get();cal=bpy.data.objects['Game_visual_calibration'];points=[inv@o.evaluated_get(dg).matrix_world@v.co for o in cal.children_recursive if o.type=='MESH' for v in o.evaluated_get(dg).data.vertices]
player_size=[max(v[i] for v in points)-min(v[i] for v in points) for i in range(3)]
target=[2.74950361,5.190148,3.262378]
assert max(abs(a-b) for a,b in zip(player_size,target))<.01,(player_size,target)
report={'fps':24,'frames':7200,'duration':300,'defense_seconds':60,'defense_max_yaw_degrees_s':max(rates),'quarter_camera':cam_positions[0],'game_vertical_fov':40,'game_move_speed':SPEED,'native_player_envelope':player_size,'store_dimensions':list(s['store_dimensions']),'restroom_dimensions':list(s['restroom_dimensions']),'triangles':sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in s.objects if o.type=='MESH'),'invalid_coordinates':0,'route_lengths':LENGTHS,'explicit_hold_seconds':HOLDS}
for o in s.objects:
 assert all(math.isfinite(v) for row in o.matrix_world for v in row),o.name
 if o.type=='MESH':assert all(math.isfinite(v) for vertex in o.data.vertices for v in vertex.co),o.name
(OUT/'validation.json').write_text(json.dumps(report,indent=2));print('V2_VALIDATED',json.dumps(report),flush=True)
bpy.ops.object.select_all(action='DESELECT')
for o in s.objects:
 if o.type not in ('MESH','CURVE','FONT'):continue
 chain=[o];a=o.parent
 while a is not None:chain.append(a);a=a.parent
 if any(a.get('role') or a.get('assembly') in ('actor','player') for a in chain):continue
 if any(a.animation_data and not a.get('camera_cutaway') for a in chain):continue
 o.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'reststop-v2-environment.glb'),export_format='GLB',use_selection=True,export_animations=False,export_apply=True,export_cameras=False,export_lights=False)
print('V2_EXPORTED',flush=True)
