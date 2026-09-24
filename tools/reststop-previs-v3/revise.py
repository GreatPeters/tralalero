"""Reproducible v3: remove carts, diversify real cars, enforce rail + lateral input."""
import bpy,math,json,sys
from pathlib import Path
from collections import Counter
from mathutils import Vector,Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
import geometry as g
import vehicles
from timeline import FPS,STAGES,CURVES,CUM,LENGTHS,HOLDS,SPEED,center_route,prior_route,route,rail_frame,lane_offset,angle_delta,stage_index,sample_distance,smooth
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v3-2026-09-23';bpy.ops.wm.open_mainfile(filepath=str(ROOT/'outputs/reststop-blender-v2-2026-09-23/reststop-v2.blend'));s=bpy.context.scene;s.frame_set(1)
g.M.update({m.name:m for m in bpy.data.materials});g.ZONE='V3_REFINEMENT'
def remove_tree(root):
 for o in reversed([root]+list(root.children_recursive)):
  if o.name in bpy.data.objects:bpy.data.objects.remove(o,do_unlink=True)
removed=[]
for name in [o.name for o in s.objects if o.name.startswith('Cart_train_') or o.name.startswith('Cart_crossing_')]:
 o=bpy.data.objects.get(name)
 if o is not None:removed.append(name);remove_tree(o)
assert not any('cart' in o.name.lower() for o in s.objects)
print('V3_CARTS_REMOVED',len(removed),flush=True)

templates={n:vehicles.load(n) for n in vehicles.SPECS};s.render.fps=24;s.render.fps_base=1
def nearest(x,y):
 best=1e9
 for points in CURVES:
  for p,q in zip(points,points[1:]):
   dx=q[0]-p[0];dy=q[1]-p[1];u=max(0,min(1,((x-p[0])*dx+(y-p[1])*dy)/max(.001,dx*dx+dy*dy)));best=min(best,math.hypot(x-p[0]-u*dx,y-p[1]-u*dy))
 return best
roots=sorted([o for o in s.objects if o.get('assembly')=='vehicle'],key=lambda o:o.name);counts=Counter();schedule=['081','054','067','083','082','080'];parked=0
for root in roots:
 if root.name.startswith('Parked_game_car_'):
  n=schedule[parked%len(schedule)];parked+=1
  if n in ('054','080','083') and nearest(root.location.x,root.location.y)<12:n=['081','067','082'][parked%3]
 elif root.name.startswith('Charging'):n=['081','082','067'][int(root.name[-2:])%3]
 elif root.name.startswith('Reversing'):n=['081','067','082','081'][int(root.name[-1])%4]
 else:n='067' if root.name.endswith('0') else '081'
 for child in list(root.children):
  if child.type=='MESH':remove_tree(child)
 vehicles.attach(root,n,templates[n]);counts[n]+=1
bus=bpy.data.objects['Crossing_shuttle_bus']
for child in list(bus.children):remove_tree(child)
vehicles.attach(bus,'080',templates['080']);counts['080']+=1
for o in list(s.objects):
 if o.name.startswith('02_PARKING__white__'):bpy.data.objects.remove(o,do_unlink=True)
g.ZONE='V3_PARKING_BAYS'
for root in roots:
 if not root.name.startswith('Parked_game_car_'):continue
 width,length,height=vehicles.SPECS[root['source_model']][1]
 for side in (-1,1):
  local=Vector((side*(width/2+.45),0,0));p=root.location+root.rotation_euler.to_matrix()@local;o=g.cube('Parking_bay_'+root.name,(p.x,p.y,.274),(.1,length+1.1,.016),'white',0);o.rotation_euler.z=root.rotation_euler.z
print('V3_VEHICLES',dict(counts),flush=True)

def fcurves(obj):
 result={}
 for layer in obj.animation_data.action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for fc in bag.fcurves:result[(fc.data_path,fc.array_index)]=fc
 return result
player=bpy.data.objects['PLAYER_ROOT'];cam=s.camera;pf=fcurves(player);cf=fcurves(cam)
world_yaws=[pf[('rotation_euler',2)].evaluate(f) for f in range(1,7202)]
camera_keys=[]
for f in range(1,7202,6):
 t=(f-1)/24;old=prior_route(t);center=center_route(t);delta=Vector((old[0]-center[0],old[1]-center[1],0));pos=Vector(tuple(cf[('location',i)].evaluate(f) for i in range(3)))-delta;pos.x+=3.2*min(smooth(t-74),1-smooth(t-78));rot=tuple(cf[('rotation_euler',i)].evaluate(f) for i in range(3));camera_keys.append((f,pos,rot))
player.animation_data_clear();player.parent=None;player.location=(0,0,0)
rail=g.empty('AUTO_RAIL_ANCHOR');rail['control']='automatic fixed path; no user forward or yaw input';rail['speed_reference']=7.8
player.parent=rail;player.matrix_parent_inverse=Matrix.Identity(4);player['lane_offset']=0.;player['input_axis']='left_right_only';player.id_properties_ui('lane_offset').update(min=-2.2,max=2.2,description='Only player position input; perpendicular to the fixed route.')
player.lock_location=(False,True,True);player.lock_rotation=(True,True,True)
c=player.constraints.new('LIMIT_LOCATION');c.name='Lateral_only_corridor';c.owner_space='LOCAL'
for axis,lo,hi in [('x',-2.2,2.2),('y',0,0),('z',0,0)]:setattr(c,'use_min_'+axis,True);setattr(c,'use_max_'+axis,True);setattr(c,'min_'+axis,lo);setattr(c,'max_'+axis,hi)
driver=player.driver_add('location',0).driver;v=driver.variables.new();v.name='lane';v.type='SINGLE_PROP';v.targets[0].id=player;v.targets[0].data_path='["lane_offset"]';driver.expression='max(-2.2,min(2.2,lane))'
def ground(x,y):
 if -140<x<122 and -139<y<-25:return .26
 if y>-32 or 220<x<316 and -92<y<38:return .18
 return .08
last=None
for f in range(1,7202):
 t=(f-1)/24;p,forward,normal=rail_frame(t);yaw=math.atan2(forward[0],-forward[1])
 if last is not None:yaw=last+angle_delta(last,yaw)
 last=yaw;rail.location=(p[0],p[1],ground(*p));rail.rotation_euler.z=yaw;rail.keyframe_insert(data_path='location',frame=f);rail.keyframe_insert(data_path='rotation_euler',frame=f)
 player['lane_offset']=lane_offset(t);player.keyframe_insert(data_path='["lane_offset"]',frame=f);player.rotation_euler=(0,0,world_yaws[f-1]-yaw);player.keyframe_insert(data_path='rotation_euler',frame=f)
 idx=stage_index(t);elapsed=max(0,t-STAGES[idx][2]);travel=LENGTHS[idx]/SPEED;pause_at=travel*(.12 if idx==3 else .48);hold=max(0,HOLDS[idx]);moving=elapsed if elapsed<pause_at else pause_at if elapsed<pause_at+hold else elapsed-hold
 rail['auto_distance']=sum(LENGTHS[:idx])+(0 if idx==4 else max(0,min(LENGTHS[idx],moving*SPEED)));rail.keyframe_insert(data_path='["auto_distance"]',frame=f)
cam.animation_data_clear()
for f,pos,rot in camera_keys:
 cam.location=pos;cam.rotation_euler=rot;cam.keyframe_insert(data_path='location',frame=f);cam.keyframe_insert(data_path='rotation_euler',frame=f)
cam['control']='fixed rail camera; independent of lane_offset'
# Keep projectile emission aligned with the newly constrained player.
shot_times=(8,17,28,39,47,55,64,72,79,88,95,164,171,179,187,196,205,213,225,234,243,258,267,283,293)
for i,t in enumerate(shot_times):
 for side in (-1,1):
  o=bpy.data.objects.get('Route_shot_%02d_%s'%(i,side))
  if o is not None:o.location=(*route(t),1.9);o.keyframe_insert(data_path='location',frame=round(t*24)+1)
print('V3_RAIL_AUTHORED',flush=True)

g.material('Rail_gold',(.95,.52,.05),.8,0,1,.12);g.material('Rail_edge',(.015,.32,.42),.8,0,1,.04);g.ZONE='V3_ROUTE_GUIDES'
def point(i,d,offset=0):
 p=sample_distance(i,d);a=sample_distance(i,d-.2);b=sample_distance(i,d+.2);dx=b[0]-a[0];dy=b[1]-a[1];length=max(.0001,math.hypot(dx,dy));x=p[0]-dy/length*offset;y=p[1]+dx/length*offset;return (x,y,ground(*p)+.036)
def strip(i,distances,offset,width,mat,name):
 verts=[]
 for d in distances:verts.extend([point(i,d,offset-width/2),point(i,d,offset+width/2)])
 faces=[(j*2,j*2+1,j*2+3,j*2+2) for j in range(len(distances)-1)];return verts,faces
for i,(idx,label,a,b) in enumerate(STAGES):
 if i==4:continue
 end=LENGTHS[i];distances=[j*.7 for j in range(int(end/.7)+1)]+[end]
 for side in (-1,1):
  verts,faces=strip(i,distances,side*4.1,.095,'Rail_edge','');g.surface('Rail_boundary_'+idx+'_'+str(side),verts,faces,'Rail_edge')
 verts=[];faces=[]
 for j in range(int(end/7.5)):
  ds=[j*7.5+k*.6 for k in range(5)];vs,fs=strip(i,ds,0,.22,'Rail_gold','');base=len(verts);verts+=vs;faces.extend(tuple(base+n for n in face) for face in fs)
  if j%2==0:
   base=len(verts);d=j*7.5+4;verts.extend([point(i,d,-.7),point(i,d,.7),point(i,d+1.5)]);faces.append((base,base+1,base+2))
 g.surface('Automatic_route_'+idx,verts,faces,'Rail_gold')
g.linear_all();s.render.fps=24;s.render.fps_base=1;s.frame_start=1;s.frame_end=7200;s.frame_set(1)
s['version']='RestStop v3 fixed route and vehicle variety';s['movement_contract']='automatic forward on fixed route + local lateral input only';s['lane_limit']=2.2;s['moving_carts']=0;s['vehicle_count']=sum(counts.values());s['vehicle_types']=json.dumps(dict(counts));s['route_guides']='Review guide: gold automatic centerline; blue corridor boundaries';s['production']='Blender only; no TRELLIS, Seedance or Unity installation'
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reststop-v3.blend'),compress=True)
metrics={'base':'outputs/reststop-blender-v2-2026-09-23/reststop-v2.blend','removed_cart_roots':removed,'moving_cart_objects_remaining':0,'vehicle_count':sum(counts.values()),'vehicle_types':dict(counts),'lane_limit':2.2,'route_source':'same fixed centerline as v2, now represented by an automatic parent rail','controller':'PLAYER_ROOT lane_offset driver; local Y/Z constrained to zero','camera':'centerline only; original v2 lateral camera displacement removed','fps':24,'frames':7200,'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in s.objects if o.type=='MESH')}
(OUT/'build-metrics.json').write_text(json.dumps(metrics,ensure_ascii=False,indent=2),encoding='utf8');print('V3_COMPLETE',json.dumps(metrics,ensure_ascii=True),flush=True)
