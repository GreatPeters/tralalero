"""Enlarge all existing vehicles; repack parking around the unchanged route."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import geometry as g
from vehicles import SPECS
from timeline import CURVES,LENGTHS,sample_distance,rail_frame
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v4-2026-09-23';SCALE=1.8
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'outputs/reststop-blender-v3-2026-09-23/reststop-v3.blend'));s=bpy.context.scene;s.frame_set(1);g.M.update({m.name:m for m in bpy.data.materials});g.ZONE='V4_PARKING_BAYS'
vehicles=[o for o in s.objects if o.get('vehicle_type')];assert len(vehicles)==91
for root in vehicles:root.scale*=SCALE;root['v4_scale_factor']=SCALE
def curves(obj):
 return {(fc.data_path,fc.array_index):fc for layer in obj.animation_data.action.layers for strip in layer.strips for bag in strip.channelbags for fc in bag.fcurves}
traffic=[]
for i,t in enumerate((31.5,35.5,43.5,47.0)):
 root=bpy.data.objects['Reversing_game_car_%d'%i];p,f,n=rail_frame(t);start=Vector((p[0]+n[0]*10,p[1]+n[1]*10,.26));stop=Vector((p[0]+n[0]*5.2,p[1]+n[1]*5.2,.26))
 for tt,pos in [(0,start),(t-2,start),(t-.4,stop),(t+.3,stop),(t+2.8,start)]:root.location=pos;root.keyframe_insert(data_path='location',frame=round(tt*24)+1)
 width,length,height=[v*SCALE for v in SPECS[root['source_model']][1]];a=root.rotation_euler.z;hx=abs(math.cos(a))*width/2+abs(math.sin(a))*length/2;hy=abs(math.sin(a))*width/2+abs(math.cos(a))*length/2
 traffic.append((min(start.x,stop.x)-hx-.5,max(start.x,stop.x)+hx+.5,min(start.y,stop.y)-hy-.5,max(start.y,stop.y)+hy+.5))
 for child in root.children_recursive:
  if child.name.startswith('Brake_lamp'):
   w,l,h=SPECS[root['source_model']][1];child.location.y=l/2+.035;child.location.x=math.copysign(w/2-.3,child.location.x)
s.frame_set(1)
route_points=[]
for i in (0,1,2):
 for j in range(int(LENGTHS[i]/.5)+1):
  p=sample_distance(i,j*.5)
  if -155<p[0]<135 and -155<p[1]<0:route_points.append(p)
def distance_rect_point(cx,cy,hx,hy,x,y):return math.hypot(max(abs(cx-x)-hx,0),max(abs(cy-y)-hy,0))
def clear(cx,cy,hx,hy,placed):
 if cx-hx< -139.5 or cx+hx>121.5 or cy-hy< -138.5 or cy+hy> -26:return False
 if any(distance_rect_point(cx,cy,hx,hy,*p)<4.8 for p in route_points):return False
 circles=[(-134,-61,3.2),(-20,-92,3.2),(48,-91,3.2),(100,-109,3.2),(100,-44,3.2),(-106,-88,2.)]+[(x,-116,1.2) for x in (-124,-32,52,112)]
 if any(distance_rect_point(cx,cy,hx,hy,x,y)<r for x,y,r in circles):return False
 if any(cx+hx>x0 and cx-hx<x1 and cy+hy>y0 and cy-hy<y1 for x0,x1,y0,y1 in traffic):return False
 if any(abs(cx-x)<hx+hw+.7 and abs(cy-y)<hy+hl+.7 for x,y,hw,hl in placed):return False
 return True
parked=sorted([o for o in vehicles if o.name.startswith('Parked_game_car_')],key=lambda o:(-SPECS[o['source_model']][1][0]*SPECS[o['source_model']][1][1],o.name));assert len(parked)==76
candidates=[(-132.5+i*7.8,y) for y in (-129,-107,-85,-64,-43) for i in range(33)];candidates +=[(-132.5+i*3.9,-130+j*3.9) for i in range(65) for j in range(25)]
placed=[];positions=[]
for root in parked:
 width,length,height=[v*SCALE for v in SPECS[root['source_model']][1]];hx=width/2;hy=length/2;old=root.location.copy();choices=sorted(candidates,key=lambda p:(p[0]-old.x)**2+(p[1]-old.y)**2);chosen=next((p for p in choices if clear(*p,hx,hy,placed)),None)
 if chosen is None:raise RuntimeError('No clear parking position for '+root.name)
 x,y=chosen;root.location=(x,y,.26);placed.append((x,y,hx,hy));positions.append({'name':root.name,'model':root['source_model'],'old':list(old),'new':[x,y,.26],'dimensions':[width,length,height]})
for name in [o.name for o in s.objects if o.get('zone')=='V3_PARKING_BAYS']:
 bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
for root in parked:
 width,length,height=[v*SCALE for v in SPECS[root['source_model']][1]]
 for side in (-1,1):g.cube('V4_bay_'+root.name,(root.location.x+side*(width/2+.30),root.location.y,.276),(.10,length+.8,.014),'white',0)
g.linear_all();s.frame_set(1);s['version']='RestStop v4 larger vehicle scale';s['vehicle_scale_factor']=SCALE
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reststop-v4.blend'),compress=True)
metrics={'base':'outputs/reststop-blender-v3-2026-09-23/reststop-v3.blend','scale_factor_xyz':SCALE,'vehicles':91,'parked':76,'dimensions':{n:[v*SCALE for v in spec[1]] for n,spec in SPECS.items()},'minimum_route_edge_clearance':min(distance_rect_point(x,y,hx,hy,*p) for x,y,hx,hy in placed for p in route_points),'minimum_parking_gap':.7,'reverse_stopping_offset':5.2,'positions':positions}
(OUT/'scale-metrics.json').write_text(json.dumps(metrics,ensure_ascii=False,indent=2),encoding='utf8');print('V4_SCALED',json.dumps({k:v for k,v in metrics.items() if k!='positions'}),flush=True)
