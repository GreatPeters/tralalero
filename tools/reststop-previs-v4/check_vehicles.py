import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent));from vehicles import SPECS
OUT=Path.cwd()/'outputs/reststop-blender-v4-2026-09-23';bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-v4.blend'));s=bpy.context.scene;s.frame_set(1);roots=[o for o in s.objects if o.get('vehicle_type')];sizes={};contact=[]
for root in roots:
 body=next(o for o in root.children if o.type=='MESH' and '_body_' in o.name);pts=[Vector((v.co.x*root.scale.x,v.co.y*root.scale.y,v.co.z*root.scale.z)) for v in body.data.vertices];dims=[max(p[i] for p in pts)-min(p[i] for p in pts) for i in range(3)];target=[v*1.8 for v in SPECS[root['source_model']][1]];assert max(abs(a-b) for a,b in zip(dims,target))<.001,(root.name,dims,target);assert abs(min(p.z for p in pts))<.001;sizes[root['source_model']]=dims
def overlaps(a,half_a,ya,b,half_b,yb):
 ax=Vector((math.cos(ya),math.sin(ya)));ay=Vector((-math.sin(ya),math.cos(ya)));bx=Vector((math.cos(yb),math.sin(yb)));by=Vector((-math.sin(yb),math.cos(yb)));delta=Vector((a.x-b.x,a.y-b.y))
 for v in (ax,ay,bx,by):
  ra=half_a[0]*abs(ax.dot(v))+half_a[1]*abs(ay.dot(v));rb=half_b[0]*abs(bx.dot(v))+half_b[1]*abs(by.dot(v))
  if abs(delta.dot(v))>ra+rb:return False
 return True
player=bpy.data.objects['PLAYER_ROOT'];tests=[('Reversing_game_car_'+str(i),t-3,t+3) for i,t in enumerate((31.5,35.5,43.5,47))]+[('Crossing_shuttle_bus',260,274),('Exit_convoy_0',275,300),('Exit_convoy_1',275,300)]
for name,a,b in tests:
 root=bpy.data.objects[name];dims=sizes[root['source_model']]
 for i in range(round((b-a)*5)+1):
  t=a+i/5;s.frame_set(round(t*24)+1)
  if overlaps(player.matrix_world.translation,(2.7495/2,5.1901/2),player.matrix_world.to_euler().z,root.matrix_world.translation,(dims[0]/2,dims[1]/2),root.matrix_world.to_euler().z):contact.append({'vehicle':name,'time':round(t,3)})
report={'count':len(roots),'scale_factor':1.8,'measured_dimensions':sizes,'all_body_bottoms_at_root_ground':True,'authored_motion_bounding_box_overlap_samples':contact,'test':'Conservative planar visual bounding boxes sampled every 0.2 seconds around moving hazards; not Unity physics.'};(OUT/'vehicle-validation.json').write_text(json.dumps(report,indent=2));print('VEHICLE_SCALE_CHECK',json.dumps(report),flush=True)
