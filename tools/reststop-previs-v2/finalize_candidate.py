"""Apply the final calibration/framing refinements without rebuilding static meshes."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from timeline import route,center_route,smooth,angle_delta
OUT=Path.cwd()/'outputs/reststop-blender-v2-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-v2.blend'));s=bpy.context.scene;s.frame_set(1);player=bpy.data.objects['PLAYER_ROOT'];cam=s.camera
if s.get('final_calibrated'):raise RuntimeError('Preserve the completed candidate')
game=json.loads((Path.cwd()/'map-concepts/reststop-blender-v2-2026-09-23/game-scale.json').read_text())['result']['restStop'];size=game['player']['visualBounds']['size']
metrics=json.loads((OUT/'build-metrics.json').read_text());raw=metrics.get('shark_raw_blender_envelope',metrics.get('shark_blender_envelope'))
cal=bpy.data.objects.new('Game_visual_calibration',None);s.collection.objects.link(cal);cal.parent=player;cal.scale=(size[0]/raw[0],size[2]/raw[1],size[1]/raw[2]);rig=bpy.data.objects['Shark_game_source_rig'];rig.parent=cal
for t,v in [(0,-.065),(97,-.065),(100,0),(160,0),(163,-.065),(300,-.065)]:cam.data.shift_y=v;cam.data.keyframe_insert(data_path='shift_y',frame=round(t*24)+1)
s.frame_set(round(98.5*24)+1);start=player.rotation_euler.z
for f in range(2365,2402):
 t=(f-1)/24;player.rotation_euler.z=start+angle_delta(start,math.pi)*((t-98.5)/1.5);player.keyframe_insert(data_path='rotation_euler',frame=f)
for i in range(1,17):
 t=160+i/4;x,y=route(t);p0=center_route(t-.05);p1=center_route(t+.05);heading=math.atan2(p1[0]-p0[0],-(p1[1]-p0[1]));f=Vector((math.sin(heading),-math.cos(heading),0));r=Vector((math.cos(heading),math.sin(heading),0));pos=Vector((x,y,.18))+r*.1170025-f*19.2690125+Vector((0,0,12.546));target=pos+f*math.cos(math.radians(17.4656067))*25+Vector((0,0,-math.sin(math.radians(17.4656067))*25))
 if t<163:
  u=smooth((t-160)/3);pos=Vector((20,16,27)).lerp(pos,u);target=Vector((0,42,1.7)).lerp(target,u)
 cam.location=pos;cam.rotation_euler=(target-pos).to_track_quat('-Z','Y').to_euler();cam.keyframe_insert(data_path='location',frame=round(t*24)+1);cam.keyframe_insert(data_path='rotation_euler',frame=round(t*24)+1)
for o in (player,cam):
 for layer in o.animation_data.action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for fc in bag.fcurves:
     for k in fc.keyframe_points:k.interpolation='LINEAR'
s['final_calibrated']=True;s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reststop-v2.blend'),compress=True)
metrics['shark_calibrated_envelope']=[size[0],size[2],size[1]];metrics['visual_calibration']=list(cal.scale);metrics['caption_safe_shift_y']=-.065;(OUT/'build-metrics.json').write_text(json.dumps(metrics,ensure_ascii=False,indent=2),encoding='utf8');print('FINAL_CALIBRATED',flush=True)
