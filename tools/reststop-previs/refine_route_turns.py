"""Remove Euler wrap spins and bound route turns; report exactly affected frames."""
import bpy, math, sys, json, shutil
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from timeline import route, FPS
import geometry as g
OUT=Path.cwd()/'outputs/reststop-blender-300s-2026-09-23'
source=OUT/'reststop-master.blend';backup=OUT/'reststop-master-before-route-turn-fix.blend'
if not backup.exists():shutil.copy2(source,backup)
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.context.scene;p=bpy.data.objects['PLAYER_ROOT']
g.M={m.name:m for m in bpy.data.materials};g.ZONE='08_RESTROOM'
if 'Restroom_store_connector' not in bpy.data.objects:
    g.cube('Restroom_store_connector',(68.5,22,.026),(1.3,8,.24),'tile',.03)
    g.cube('Restroom_fuel_walkway',(88,4.5,.026),(7,27,.24),'tile',.04)
    for x in (84.4,91.6):g.cube('Restroom_walkway_curb',(x,4.5,.20),(.15,27,.30),'cream',.03)
before=[]
for f in range(1,2401):s.frame_set(f);before.append(p.rotation_euler.z)
yaw=math.pi
for i in range(600):
    t=i/6;a=route(max(0,t-.12));b=route(min(300,t+.12));dx=b[0]-a[0];dy=b[1]-a[1]
    target=math.atan2(dx,-dy) if abs(dx)+abs(dy)>.0001 else yaw
    delta=(target-yaw+math.pi)%(2*math.pi)-math.pi
    if i:yaw+=max(-math.pi/12,min(math.pi/12,delta))
    p.rotation_euler=(0,0,yaw);p.keyframe_insert(data_path='rotation_euler',frame=round(t*FPS)+1)
effect_changed=set();early_effects=[]
defense_data=json.loads((OUT/'defense-timeline.json').read_text())
for i,b in enumerate(defense_data['bullets']):
    obj=bpy.data.objects['Defense_water_%02d_impact'%i]
    first=max(1,round(b['end']*24)+1);last=max(first,round((b['end']+.12)*24)+1)
    s.frame_set(first-1)
    if max(obj.scale)>.01:
        early_effects.append(obj.name)
        # These effects are inside the opaque main roof before the cutaway at81s.
        effect_changed.update(range(81*24+2,first+1))
    action=obj.animation_data.action
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in list(bag.fcurves):
                    if fc.data_path=='scale':bag.fcurves.remove(fc)
    for frame,value in [(1,(0,0,0)),(first-1,(0,0,0)),(first,(1,1,1)),(last,(1,1,1)),(last+1,(0,0,0))]:
        obj.scale=value;obj.keyframe_insert(data_path='scale',frame=frame)
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for k in fc.keyframe_points:k.interpolation='LINEAR'
for layer in p.animation_data.action.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            for fc in bag.fcurves:
                for k in fc.keyframe_points:k.interpolation='LINEAR'
changed=[];after=[]
for f,old in enumerate(before,1):
    s.frame_set(f);new=p.rotation_euler.z;after.append(new)
    delta=abs((new-old+math.pi)%(2*math.pi)-math.pi)
    if delta>1e-4:changed.append(f)
maximum=max(abs((b-a+math.pi)%(2*math.pi)-math.pi)*24*180/math.pi for a,b in zip(after,after[1:]))
assert maximum<=90.01,maximum
changed=sorted(set(changed)|effect_changed|set(range(1,194)))
stages=[]
for f in changed:
    t=(f-1)/24;idx=min(3,int(t//25))+1 if t<100 else 5
    stages.append('%02d'%idx)
report={'frames':changed,'count':len(changed),'stages':sorted(set(stages)),'max_route_turn_deg_s':maximum,'early_effects_fixed':early_effects,'reason':'Bounded route yaw, integer-frame impact visibility, store/restroom bridge and paved restroom/fuel connection. Intro overview plus affected earlier frames rerendered; later stages render from this revision.'}
(OUT/'route-turn-patch.json').write_text(json.dumps(report,indent=2));s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True);print('ROUTE_TURN_PATCH',json.dumps({k:v for k,v in report.items() if k!='frames'}),flush=True)
