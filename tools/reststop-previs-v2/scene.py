"""Rest-stop v2: calibrated game scale, large traversable facilities and new beats."""
import bpy,bmesh,math,json,random,sys
from pathlib import Path
from mathutils import Vector,Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
import architecture as a
import geometry as g
import people
from timeline import FPS,SPEED,STAGES,PATHS,CURVES,LENGTHS,HOLDS,CENTER,route,center_route,smooth,angle_delta,defense,vent_active
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23';s=bpy.context.scene
random.seed(2309232)
GAME=json.loads((ROOT/'map-concepts/reststop-blender-v2-2026-09-23/game-scale.json').read_text())['result']['restStop']
EVENTS=[];OCCLUDERS=[];VEHICLES=[];PEOPLE=[]
ROOMS={'hall':[-54,42,0,84],'dining':[42,118,0,96],'store':[118,206,0,92],'restroom':[206,278,36,120]}
def ground_height(x,y):
 if -140<x<122 and -139<y<-25:return .26
 if y>-32 or 220<x<316 and -92<y<38:return .18
 return .08

def nearest_path(x,y,index=None):
 best=1e9
 for points in (CURVES if index is None else [CURVES[index]]):
  for p,q in zip(points,points[1:]):
   vx=q[0]-p[0];vy=q[1]-p[1];u=max(0,min(1,((x-p[0])*vx+(y-p[1])*vy)/max(.001,vx*vx+vy*vy)));best=min(best,math.hypot(x-p[0]-u*vx,y-p[1]-u*vy))
 return best
def wall(name,loc,size,mat='cream'):
 o=g.cube(name,loc,size,mat,.06);o['camera_cutaway']=True;OCCLUDERS.append(o);return o
def room(name,bounds,height=8):
 x0,x1,y0,y1=bounds;a.floor(name+'_floor',(x0+x1)/2,(y0+y1)/2,x1-x0,y1-y0,4)
 wall(name+'_back_wall',((x0+x1)/2,y1,height/2),(x1-x0,.4,height))
 for x in (x0,x1):
  for yy in (y0+7,y1-7):wall(name+'_corner_pier',(x,yy,height/2),(.6,10,height))
 a.roof(name+'_roof',x0-.6,x1+.6,y0-.6,y1+.6,height,2.5)
 for x in range(round(x0)+8,round(x1)-5,12):
  if name=='Main_hall' and -49<x<-25:continue
  wall(name+'_front_pier',(x,y0,height/2),(.5,.5,height))
  panel=g.cube(name+'_front_glazing',(x+5.5,y0,height*.48),(10.3,.045,height*.90),'glass',.015);panel['camera_cutaway']=True;OCCLUDERS.append(panel)

TEMPLATES={}
def load_car(number,target):
 path=ROOT/('Assets/ShooterSurvival/Models/Chapters/Props/067/Model.fbx' if number=='067' else 'Assets/ShooterSurvival/Models/Highway/Props/082/Model.fbx')
 old=set(s.objects);bpy.ops.import_scene.fbx(filepath=str(path));new=[o for o in s.objects if o not in old];new_names=[o.name for o in new]
 meshes=[o for o in new if o.type=='MESH']
 for o in new:o.animation_data_clear()
 for o in meshes:
  matrix=o.matrix_world.copy();o.parent=None;o.matrix_world=matrix
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();o=bpy.context.object;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 points=[v.co for v in o.data.vertices];dims=[max(v[i] for v in points)-min(v[i] for v in points) for i in range(3)]
 longest=max(range(3),key=lambda i:dims[i])
 rotation=Matrix.Rotation(math.pi/2,4,'Z' if longest==0 else 'X') if longest!=1 else Matrix.Identity(4)
 for v in o.data.vertices:v.co=rotation@v.co
 dims=[max(v.co[i] for v in o.data.vertices)-min(v.co[i] for v in o.data.vertices) for i in range(3)]
 if dims[2]>dims[0]:
  rotation=Matrix.Rotation(math.pi/2,4,'Y')
  for v in o.data.vertices:v.co=rotation@v.co
 lo=Vector(tuple(min(v.co[i] for v in o.data.vertices) for i in range(3)));hi=Vector(tuple(max(v.co[i] for v in o.data.vertices) for i in range(3)));center=Vector(((hi.x+lo.x)/2,(hi.y+lo.y)/2,lo.z))
 for v in o.data.vertices:
  p=v.co-center;v.co=Vector(tuple(p[i]*target[i]/(hi[i]-lo[i]) for i in range(3)))
 mat=g.material('Game_car_'+number,(.6,.65,.67),.64,.05);bs=mat.node_tree.nodes.get('Principled BSDF');image=bpy.data.images.load(str(ROOT/f'Assets/ShooterSurvival/Models/Highway/Props/{number}/BaseColor.png'));image.pack();tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);mat.node_tree.nodes.active=tex
 o.data.materials.clear();o.data.materials.append(mat)
 for p in o.data.polygons:p.material_index=0
 data=o.data;data.use_fake_user=True;TEMPLATES[number]=data
 for name in new_names:
  item=bpy.data.objects.get(name)
  if item is not None:bpy.data.objects.remove(item,do_unlink=True)
def car(name,x,y,yaw=0,variant='067'):
 r=g.empty(name,(x,y,ground_height(x,y)));r.rotation_euler.z=yaw;r['assembly']='vehicle';r['source_model']=variant
 ob=bpy.data.objects.new(name+'_game_mesh',TEMPLATES[variant]);s.collection.objects.link(ob);ob.parent=r;ob['zone']=g.ZONE
 VEHICLES.append(r);return r
def actor(name,role,x,y,height=3.05):
 p=people.person(name,role,(x,y,ground_height(x,y)),1);p['root']['assembly']='actor';bpy.context.view_layer.update()
 points=[o.matrix_world@Vector(c) for o in p['root'].children_recursive if o.type=='MESH' for c in o.bound_box]
 z0=min(v.z for v in points);z1=max(v.z for v in points);scale=height/(z1-z0);p['root'].scale=(scale,)*3;p['root']['nominal_scale']=scale;PEOPLE.append(p);return p
def show(root,start,end):
 scale=root.get('nominal_scale',1.0);root.scale=(scale,)*3;g.visibility(root,start,end)
def event(name,start,end,kind):EVENTS.append(dict(name=name,start=start,end=end,kind=kind))
def mover(root,points):
 for t,x,y,z in points:g.key(root,t,'location',(x,y,z))
def signal(name,x,y,start,end,size=2):
 root=a.warning_ring(name,x,y,size,start,end)
 return root
def gate(name,x,y,length,start,end,angle=0):
 base=g.cube(name+'_base',(x,y,1.15),(1,1,2.3),'ochre',.12);root=g.empty(name+'_hinge',(x,y,2.1));root.rotation_euler.z=angle;root['assembly']='gate'
 g.cube(name+'_arm',(length/2,0,0),(length,.24,.25),'white',.04,root)
 for xx in range(1,int(length),2):g.cube(name+'_red_band',(xx,0,0),(.8,.25,.26),'red',.01,root)
 for t,tilt in [(0,0),(start,0),(start+1.1,-math.pi/2),(end,-math.pi/2),(end+1.1,0),(300,0)]:g.key(root,t,'rotation_euler',(0,tilt,angle))
 event(name,start,end,'hinged barrier');return root

print('V2 1 game vehicles',flush=True)
load_car('067',(2.7688,4.6671,1.737));load_car('082',(2.9549,4.6808,2.2115))
g.ZONE='00_SITE';g.cube('Whole_reststop_ground',(85,-68,-.6),(740,650,1),'grass',0)
g.ZONE='01_ENTRY';a.road('Highway',[(-266,-330),(-266,180)],24)
for x in (-273,-266,-259):
 for y in range(-320,180,12):g.cube('Highway_lane_paint',(x,y,.065),(.13,5,.02),'white',0)
a.road('Reststop_entry',CURVES[0],14);a.road('Entry_green_guide',CURVES[0],.30,'green',.075)
for i in range(0,len(CURVES[0])-1,4):
 x,y=CURVES[0][i];q=CURVES[0][i+1];v=Vector((q[0]-x,q[1]-y)).normalized();nx=-v.y;ny=v.x
 for side in (-1,1):g.cube('Guardrail_post',(x+side*8*nx,y+side*8*ny,.55),(.13,.13,1.1),'metal',.02)
a.sign('Entry_sign','휴게소  ↗',-224,-216,8.5,14,2.5,'green',1.4)
for x in (-230,-218):g.cube('Entry_sign_column',(x,-215.7,4.25),(.3,.3,8.5),'metal',.04)
gate('Entry_open_gate',-159,-139,10,18,25,math.pi/2)

g.ZONE='02_PARKING';a.floor('Parking_perimeter',-9,-82,268,120,12);g.cube('Parking_tarmac',(-9,-82,.18),(262,114,.10),'asphalt',0)
candidates=[]
for row,y in enumerate((-130,-110,-90,-48)):
 for col in range(33):
  x=-138+col*7.7
  if nearest_path(x,y)>7:candidates.append((x,y,row,col))
random.shuffle(candidates)
for i,(x,y,row,col) in enumerate(candidates[:76]):
 car('Parked_game_car_%02d'%i,x,y,math.pi if row%2 else 0,'082' if i%3==0 else '067')
 for dx in (-2.0,2.0):g.cube('Parking_bay_line',(x+dx,y,.245),(.10,6.5,.015),'white',0)
for x,y in [(-134,-61),(-20,-92),(48,-91),(100,-109),(100,-44)]:g.tree('Parking_island',x,y,1.0)
for x in (-124,-32,52,112):
 g.cylinder('Parking_lamp',(x,-116,5),.12,10,'metal');g.cube('Parking_light',(x,-115.6,10),(1.8,1.2,.15),'light',.06)

print('V2 2 large architecture',flush=True)
g.ZONE='03_FORECOURT';a.floor('Large_promenade',51,-16,318,32,4);a.canopy('Front_arcade',-108,204,-7)
for x in (-94,-76,-58):a.food_counter('Outdoor_snack',x,0,'호두과자' if x==-94 else '간식',12)
for x in (-98,-60,-12,30,60,96,140,184):
 if nearest_path(x,-22)>5:g.tree('Forecourt_tree',x,-22,.85)
 if nearest_path(x,-16)>5:g.bench('Forecourt_bench',x,-16)
for x in (-75,-45,18,72,146):a.vending('Vending',x,-1.3)

g.ZONE='04_05_HALL';room('Main_hall',ROOMS['hall'],9)
a.sign('Main_identity','휴게소',-36,-.4,7.4,16,1.6,'ochre',1.3);a.portal('Main_entrance',-36,0,11,6.8)
for x in (-44,-4,34):a.food_counter('Hall_counter',x,80,'휴게소 안내' if x==-4 else '간식',13)
for x,y in [(-43,36),(-43,70),(30,40),(30,74)]:
 if nearest_path(x,y)>6:g.tree('Hall_planter',x,y,.72)
for x,y in [(-42,48),(-29,74),(7,75),(30,54)]:
 if nearest_path(x,y)>5:g.bench('Hall_seating',x,y)
for side in (-1,1):
 root=g.empty('Entry_sliding_door',(-36+side*2.55,-.12,0));root['assembly']='automatic door'
 panel=g.cube('Entry_door_glass',(0,0,3.2),(5,.07,6.3),'glass',.03,root)
 for dx in (-2.48,2.48):g.cube('Door_edge',(dx,0,3.2),(.12,.13,6.5),'metal',.02,root)
 for t,x in [(0,-36+side*2.55),(74,-36+side*2.55),(75.2,-36+side*7.8),(99,-36+side*7.8),(100,-36+side*2.55)]:g.key(root,t,'location',(x,-.12,0))
event('Automatic_main_doors',74,99,'sliding door')

g.ZONE='06_DINING';room('Dining',ROOMS['dining'],9)
for x in (50,65,80,94):
 for y in (10,28,48,61,88):
  if nearest_path(x,y)>6:
   tab=g.table('Dining_table',x,y);tab.scale=(1.3,)*3
for x,label in [(51,'한식'),(74,'우동'),(101,'돈가스')]:a.food_counter('Dining_food',x,92,label,18)
a.sign('Dining_sign','식당',76,1,7.2,22,1.4,'wood',1.5)

g.ZONE='07_STORE';room('Large_convenience_store',ROOMS['store'],9)
a.sign('Convenience_identity','편의점',160,-.4,7.3,36,1.6,'ochre',1.7)
SHELVES=[]
for y in (10,23,28,44,54,73):
 for x in (128,145,162,179,199):
  if nearest_path(x,y)<8:continue
  root=g.empty('Shelf_unit',(x,y,0));root['assembly']='shelf';SHELVES.append(root)
  g.cube('Shelf_back',(0,0,2),(10,.18,3.7),'wood',.04,root)
  for z in (.5,1.25,2.0,2.75,3.5):
   g.cube('Shelf_board',(0,0,z),(10.3,2.0,.10),'cream',.025,root)
   for j in range(12):
    for side in (-1,1):
     col='packet'+str((j+int(z*10))%4+1)
     if j%3==0:g.cylinder('Stock_bottle',(-4.5+j*.8,side*.62,z+.26),.13,.46,col,root,vertices=12)
     else:g.cube('Stock_packet',(-4.5+j*.8,side*.62,z+.28),(.55,.42,.50),col,.04,root)
for x in range(124,206,6):
 g.cube('Refrigerator',(x,90,2.2),(5,1.8,4.2),'charcoal',.08)
 for dx in (-1.22,1.22):
  g.cube('Fridge_door',(x+dx,89.05,2.3),(2.32,.06,3.5),'glass',.03)
  g.cube('Fridge_handle',(x+dx+.86,88.97,2),(.08,.10,.8),'metal',.03)
  for z in (.7,1.5,2.3,3.1):
   for j in range(5):g.cylinder('Fridge_drink',(x+dx-.8+j*.4,89.5,z),.11,.43,'packet'+str((j+int(z))%4+1),vertices=12)
for x in (130,155,180):
 g.cube('Checkout_counter',(x,3,1.45),(11,2.0,2.6),'cream',.08);g.cube('Checkout_trim',(x,1.95,1.35),(10.7,.1,2.1),'wood_light',.025)
 for dx in (-2,2):g.cube('Register_screen',(x+dx,2.7,3.05),(.9,.17,.7),'charcoal',.05)
a.sign('Restroom_direction','화장실 →',202,84,6.3,12,1.2,'green',.8)

g.ZONE='08_RESTROOM';a.floor('Restroom_72x84',242,78,72,84,4)
wall('Restroom_back',(242,120,4),(72,.4,8));wall('Restroom_right',(278,78,4),(.4,84,8))
for y0,y1 in [(36,72),(92,120)]:wall('Restroom_left',(206,(y0+y1)/2,4),(.4,y1-y0,8))
for x0,x1 in [(206,258),(274,278)]:wall('Restroom_front',((x0+x1)/2,36,4),(x1-x0,.4,8))
a.roof('Restroom',205.4,278.6,35.4,120.6,8,.8)
a.floor('Store_restroom_link',206,82,5,16,4)
a.portal('Restroom_exit',266,36,12,6.5);a.sign('Restroom_identity','화장실',263,35.6,6.4,26,1.4,'cream',1.3)
def toilet_cubicle(x,y):
 for dx in (-1.6,1.6):g.cube('Cubicle_side',(x+dx,y,1.9),(.12,4.8,3.65),'wood_light',.03)
 g.cube('Cubicle_door',(x,y-2.45,1.9),(3.1,.12,3.55),'wood_light',.04)
 g.uvball('Toilet_bowl',(x,y+.5,.88),(.62,.87,.53),'white');g.uvball('Toilet_seat',(x,y+.27,1.33),(.58,.72,.14),'white');g.cube('Toilet_tank',(x,y+1.24,1.35),(1.05,.46,1.1),'white',.13)
 g.uvball('Cubicle_handle',(x+1.12,y-2.53,1.75),(.07,.045,.08),'metal')
for y in (42,59,97,116):
 for x in range(224,278,5):
  if nearest_path(x,y)>6:toilet_cubicle(x,y)
for x in range(229,277,6):
 for y in (73,88):
  if nearest_path(x,y)<5:continue
  g.cube('Wash_counter',(x,y,1.28),(5.6,1.7,2.1),'cream',.09)
  for dx in (-1.8,0,1.8):
   g.uvball('Sink_bowl',(x+dx,y-.2,2.41),(.66,.52,.17),'white');g.uvball('Sink_recess',(x+dx,y-.22,2.51),(.47,.33,.04),'tile_dark')
   g.curve('Sink_tap',[(x+dx,y+.4,2.35),(x+dx,y+.4,2.82),(x+dx,y,2.82)],'metal',.04)
   g.cube('Sink_mirror',(x+dx,y+.75,3.4),(1.55,.07,1.7),'metal',.025)
for x in range(228,278,5):
 if nearest_path(x,103)>5:
  g.uvball('Urinal',(x,103,1.68),(.54,.48,.90),'white');g.uvball('Urinal_recess',(x,102.6,1.7),(.35,.07,.56),'tile_dark');g.cube('Urinal_divider',(x+2,103,1.8),(.10,1.8,2.2),'cream',.04)
a.sign('Men_zone','남자',244,92,6.3,18,1.2,'navy',1.1);a.sign('Women_zone','여자',243,66,6.3,18,1.2,'wood',1.1)
for y in (57,100):
 wall('Privacy_screen',(228,y,2.0),(.12,7,4),'wood_light')

print('V2 3 fuel and exit',flush=True)
g.ZONE='09_FUEL';a.floor('Fuel_forecourt',268,-35,92,110,8);a.floor('Restroom_fuel_link',278,27,28,24,4)
for xx in (248,283):
 roof=g.cube('Fuel_canopy',(xx,-28,7.5),(28,66,.65),'red',.10);a.ROOFS.append(roof)
 top=g.cube('Fuel_canopy_top',(xx,-28,7.88),(28.3,66.3,.1),'cream',.03);a.ROOFS.append(top)
 for x in (xx-11,xx+11):
  for y in (-52,-4):wall('Fuel_column',(x,y,3.7),(.65,.65,7.4),'white')
for x in (254,278):
 for y in (-18,-48):
  g.cube('Pump_island',(x,y,.3),(3.1,5.6,.55),'cream',.12);g.cube('Pump',(x,y,2.0),(1.75,1.0,2.9),'white',.1);g.cube('Pump_red',(x,y,.9),(1.76,1.02,.5),'red',.05);g.cube('Pump_screen',(x,y-.54,2.45),(1.1,.025,.7),'charcoal',.03)
  for dx in (-1.0,1.0):g.curve('Pump_hose',[(x+dx,y,3),(x+dx*1.6,y,.75),(x+dx,y-.15,.8),(x+dx,y-.15,2.0)],'black',.06)
  for dx in (-1.6,1.6):g.cylinder('Pump_bollard',(x+dx,y-1.8,.95),.10,1.3,'ochre')
a.sign('Fuel_identity','주유소',268,-62,7.5,26,1.3,'red',1.4)
g.cube('Fuel_shop',(236,2,3),(14,20,6),'cream',.1);a.sign('Fuel_shop_name','주유 안내',236,-8.2,5.0,12,1,'red',.8)
for i in range(8):
 x=301;y=2-i*10;car('Charging_customer_%02d'%i,x,y,math.pi/2,'082' if i%2 else '067');g.cube('EV_charger',(307,y,1.4),(.8,.55,2.8),'green',.08)
g.ZONE='10_EXIT';a.road('Exit_road',CURVES[9],14);a.road('Exit_guidance',CURVES[9],.25,'white',.07)
a.sign('Exit_road_sign','고속도로  ↑',397,-214,8,17,2.3,'green',1.3)
for x in (390,404):g.cube('Exit_sign_post',(x,-213.7,4),(.28,.28,8),'metal',.04)
gate('Exit_gate_A',328,-132,11,283,290,0);gate('Exit_gate_B',375,-175,11,289,299,0)

g.ZONE='LANDSCAPE'
for x,y in [(-146,-144),(-154,-24),(-112,24),(-72,58),(-32,108),(25,118),(80,120),(145,122),(196,124),(300,125),(316,42),(325,-62),(115,-136),(30,-142),(-42,-147)]:g.tree('Site_tree',x,y,1.35)
for i,x in enumerate(range(-200,350,30)):g.uvball('Distant_hill',(x,165,-3),(27,29,12+(i%3)*3),'grass')

print('V2_ENVIRONMENT_READY',flush=True)

print('V2 4 choreographed mechanisms',flush=True)
def basis(t):
 p=Vector((*center_route(t),0));p0=Vector((*center_route(max(0,t-.15)),0));p1=Vector((*center_route(min(300,t+.15)),0));forward=(p1-p0).normalized()
 if forward.length<.5:forward=Vector((0,1,0))
 return p,forward,Vector((-forward.y,forward.x,0))
g.material('brake_light',(1,.035,.01),.35,0,1,2)
for n,t in enumerate((31.5,35.5,43.5,47.0)):
 p,f,right=basis(t);start=p+right*10;end=p+right*2.0
 r=car('Reversing_game_car_%d'%n,start.x,start.y,math.atan2(right.x,-right.y),'082' if n%2 else '067')
 # The car backs into one side while the shark retains a safe opposing gap.
 mover(r,[(0,start.x,start.y,.26),(t-2,start.x,start.y,.26),(t-.4,end.x,end.y,.26),(t+.3,end.x,end.y,.26),(t+2.8,start.x,start.y,.26)])
 lamps=g.empty('Reverse_brake_lights',(0,0,0),r)
 for x in (-1,1):g.cube('Brake_lamp',(x,2.35,1.0),(.3,.035,.18),'brake_light',.025,lamps)
 show(lamps,t-2.6,t+2.8);signal('Reverse_warning_%d'%n,p.x,p.y,t-3,t+.4,3.8);event('reverse_car_'+str(n),t-3,t+2.8,'brake lights then reversing car')

for n,t in enumerate((60.5,66,171.5,181,203.5,211)):
 p,f,right=basis(t);start=p+right*9;end=p-right*9
 count=3 if t==203.5 else 1
 for j in range(count):
  cart=a.trolley('Cart_train_%d_%d'%(n,j),start.x+f.x*j*2.4,start.y+f.y*j*2.4);cart['assembly']='moving cart';cart.scale=(1.3,)*3
  mover(cart,[(0,start.x+f.x*j*2.4,start.y+f.y*j*2.4,.18),(t-2,start.x+f.x*j*2.4,start.y+f.y*j*2.4,.18),(t-1,end.x+f.x*j*2.4,end.y+f.y*j*2.4,.18),(t+3,end.x+f.x*j*2.4,end.y+f.y*j*2.4,.18)])
 signal('Cart_crossing_%d'%n,p.x,p.y,t-3,t-1,3);event('cart_train_'+str(n),t-3,t+3,'cart crossing with safe timed gap')

# Clearly hinged display tipping, followed by rolling cans on the side of the aisle.
p,f,right=basis(207);display=g.empty('Domino_display_hinge',p+right*4.8);display['assembly']='tipping display'
g.cube('Display_base',(0,0,.3),(3,2,.6),'wood',.07,display)
g.cube('Display_back',(0,.65,1.65),(3,.16,3),'ochre',.05,display)
for z in (.6,1.4,2.2):
 g.cube('Display_shelf',(0,0,z),(3.1,1.4,.10),'cream',.03,display)
 for x in (-1,0,1):g.cube('Display_box',(x,0,z+.33),(.7,.7,.56),'packet2',.05,display)
for t,v in [(0,0),(205.8,0),(206.5,1.25),(212,1.25)]:g.key(display,t,'rotation_euler',(v,0,0))
signal('Display_fall_warning',display.location.x,display.location.y,204.8,206.6,3.2);event('tipping_display',204.8,212,'hinged product display tips into side lane')
for j in range(4):
 root=g.empty('Rolling_can_%d'%j,p+right*5+f*j*.8);root['assembly']='rolling can';c=g.cylinder('Can_body',(0,0,.26),.26,.7,'packet'+str(j%4+1),root);c.rotation_euler.x=math.pi/2
 show(root,206.3,210)
 start=root.location.copy();end=start-right*3.5
 mover(root,[(206.3,*start),(209.3,*end)]);g.key(root,206.3,'rotation_euler',(0,0,0));g.key(root,209.3,'rotation_euler',(0,13.5,0))

for n,t in enumerate((228,240)):
 p,f,right=basis(t);q=p+right*5.5
 pipe=g.empty('Leaking_pipe_%d'%n,q);pipe['assembly']='pipe'
 g.curve('Fixed_water_pipe',[(0,0,.15),(0,0,2.8),(0,-.6,2.8)],'metal',.12,pipe,False)
 jet=g.empty('Water_jet',(0,0,0),pipe)
 g.curve('Jet_stream',[(0,-.6-3*u,2.8-2.6*u*u) for u in [j/18 for j in range(19)]],'water',.13,jet,False)
 g.uvball('Spreading_puddle',(0,-3.0,.12),(2.2,2.8,.025),'water',jet)
 show(jet,t-1,t+3);signal('Leak_warning_%d'%n,q.x,q.y,t-3,t,3);event('pipe_jet_'+str(n),t-3,t+3,'leak warning and water jet')
robot=g.empty('Cleaning_robot',(238,81,.25));robot['assembly']='cleaning robot'
g.cylinder('Robot_chassis',(0,0,.25),1.55,.50,'teal',robot);g.cylinder('Robot_bumper',(0,0,.1),1.65,.14,'charcoal',robot);g.uvball('Robot_sensor',(0,-.4,.7),(.6,.6,.4),'charcoal',robot)
for t in [220+i*.25 for i in range(121)]:
 phase=(t-220)*.75;g.key(robot,t,'location',(240+math.cos(phase)*5,79+math.sin(phase)*4,.25));g.key(robot,t,'rotation_euler',(0,0,phase))
event('cleaning_robot',220,250,'rotating scrubber route')

# A compact bus crosses before the player arrives; wheels and windows move together.
p,f,right=basis(269);bus=g.empty('Crossing_shuttle_bus',p+right*18);bus['assembly']='bus';bus.rotation_euler.z=math.atan2(right.x,-right.y)
g.cube('Bus_body',(0,0,1.9),(3.2,9.5,3.2),'cream',.35,bus);g.cube('Bus_windscreen',(0,-4.73,2.7),(2.85,.055,1.5),'navy',.06,bus)
for side in (-1,1):
 for y in (-3,-1,1,3):g.cube('Bus_side_window',(side*1.61,y,2.75),(.045,1.5,1.3),'navy',.04,bus)
 for y in (-3.1,3.1):
  w=g.cylinder('Bus_wheel',(side*1.62,y,.62),.6,.30,'charcoal',bus);w.rotation_euler.y=math.pi/2
start=p+right*18;end=p-right*18;mover(bus,[(0,*start),(265.5,*start),(267.8,*end),(274,*end)])
signal('Bus_crossing_warning',p.x,p.y,264,268,5);event('shuttle_bus_crossing',264,274,'shuttle crossing and refuge gap')
for n,t in enumerate((282,293)):
 p,f,right=basis(t);r=car('Exit_convoy_%d'%n,p.x+right.x*4.4,p.y+right.y*4.4,math.atan2(f.x,-f.y),'082')
 for tt in [275+i*.125 for i in range(201)]:
  ct=max(275,min(300,tt+(2.2 if n==0 else -2.2)));q,f,right=basis(ct)
  g.key(r,tt,'location',(q.x+right.x*4.4,q.y+right.y*4.4,.08));g.key(r,tt,'rotation_euler',(0,0,math.atan2(f.x,-f.y)))
 event('exit_convoy_'+str(n),275,300,'curved in-lane exit traffic')

print('V2 5 actors and calibrated shark',flush=True)
for name,role,x,y in [('Parking_staff','marshal',-106,-88),('Snack_chef','chef',-77,2),('Barista','barista',-55,2),('Cashier','clerk',155,6),('Cleaner','cleaner',244,74),('Fuel_staff','attendant',250,-2)]:
 p=actor(name,role,x,y);people.animate_idle(p,0,300)
for i,(x,y) in enumerate([(-93,-8),(-78,-8),(-58,-23),(-30,-3),(7,-7),(40,-4),(63,-19),(92,-19)]):
 p=actor('Visitor_%02d'%i,'traveler',x,y,2.85);when=51+i*1.8;people.move(p,[(0,x,y),(when,x,y),(when+3,x-7,y+5),(when+5,x-12,y+8)]);people.animate_walk(p,when,when+5,1.8)

before=set(s.objects);source=next((ROOT/'Assets/JH/Model/Player/Sharks').rglob('Original.fbx'));bpy.ops.import_scene.fbx(filepath=str(source));imported=[o for o in s.objects if o not in before]
for o in imported:o.animation_data_clear()
rig=next(o for o in imported if o.type=='ARMATURE');rig.scale*=300;bpy.context.view_layer.update();meshes=[o for o in imported if o.type=='MESH']
points=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box];lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
rig.scale*=GAME['player']['visualBounds']['size'][1]/(hi.z-lo.z);bpy.context.view_layer.update();points=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box];lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
player=g.empty('PLAYER_ROOT');player['assembly']='player';player['nominal_game_height']=3.262378;offset=Vector((-(hi.x+lo.x)/2,-(hi.y+lo.y)/2,-lo.z))
raw_player_envelope=hi-lo
calibration=g.empty('Game_visual_calibration',(0,0,0),player)
game_size=GAME['player']['visualBounds']['size'];calibration.scale=(game_size[0]/raw_player_envelope.x,game_size[2]/raw_player_envelope.y,game_size[1]/raw_player_envelope.z)
for o in imported:
 if o.parent is None:o.location+=offset;o.parent=calibration
rig.name='Shark_game_source_rig'
DATA=defense();defense_yaws=dict((round(t*FPS)+1,math.pi-y) for t,y in DATA['yaw'])
player_yaws=[];yaw=math.pi;pre_aim_start=None
for frame in range(1,7202):
 t=(frame-1)/FPS;x,y=route(t);pos=(x,y,ground_height(x,y))
 if frame in defense_yaws:yaw=defense_yaws[frame]
 elif 98.5<=t<100:
  if pre_aim_start is None:pre_aim_start=yaw
  yaw=pre_aim_start+angle_delta(pre_aim_start,math.pi)*((t-98.5)/1.5)
 else:
  p0=center_route(max(0,t-.08));p1=center_route(min(300,t+.08));dx=p1[0]-p0[0];dy=p1[1]-p0[1]
  if abs(dx)+abs(dy)>.0001:
   target=math.atan2(dx,-dy);yaw+=max(-math.pi/2/FPS,min(math.pi/2/FPS,angle_delta(yaw,target)))
 player_yaws.append(yaw)
 if frame%2==1 or frame==7201 or 2401<=frame<=3841:g.key(player,t,'location',pos);g.key(player,t,'rotation_euler',(0,0,yaw))
 if frame%4==1:
  gait=math.sin(t*math.tau*2.2)*(.20 if not 100<=t<=160 else .012)
  for bone_name,sign in [('frontleg',1),('R_frontleg',-1),('backleg',-1),('R_backleg',1)]:
   if bone_name in rig.pose.bones:
    b=rig.pose.bones[bone_name];b.rotation_mode='XYZ';b.rotation_euler.x=gait*sign;b.keyframe_insert(data_path='rotation_euler',frame=frame)
aim=g.empty('Aim_marker',(0,0,.035),player);g.curve('Aim_ring',[(1.45*math.cos(i*math.tau/48),1.45*math.sin(i*math.tau/48),0) for i in range(49)],'reticle',.035,aim,False);g.surface('Aim_tip',[(-.3,-2.7,0),(.3,-2.7,0),(0,-3.6,0)],[(0,1,2)],'reticle',aim,False)

def projectile(name,t,end,start,target,mat='water'):
 root=g.empty(name);g.uvball(name+'_ball',(0,0,0),(.20,.20,.20),mat,root);g.key(root,t,'location',start);g.key(root,end,'location',target);show(root,t,end)
 hit=g.empty(name+'_hit',target);g.uvball('Hit_flash',(0,0,0),(.48,.48,.48),'reticle',hit);show(hit,end,end+.13)

# Reuse a bounded actor pool; a visible actor keeps all its own limb geometry.
pool=[];free=[]
for e in DATA['enemies']:
 start=e['spawn'];end=(e['dead']+.48) if e['dead'] is not None else 160
 slot=next((i for i,v in enumerate(free) if v<start-.1),None)
 if slot is None:slot=len(pool);pool.append(actor('Defense_pool_%02d'%slot,'police',0,0,3.0924));free.append(0)
 p=pool[slot];free[slot]=end;show(p['root'],start,end);death=e['dead'] or 160
 for t,x,y,stun in e['poses'][::3]:
  if t>death:break
  g.key(p['root'],t,'location',(x,y,.18));g.key(p['root'],t,'rotation_euler',(0,0,-e['angle']))
 people.animate_walk(p,start,death,1.8)
 g.key(p['root'],death,'rotation_euler',(0,0,-e['angle']));g.key(p['root'],min(160,death+.4),'rotation_euler',(math.pi/2,0,-e['angle']))
 for t,x,y,stun in e['poses'][::6]:
  if t>death:break
  if stun:g.key(p['torso'],t,'rotation_euler',(.25,0,.16))
  elif math.hypot(x,y-42)<=3.81:
   g.key(p['torso'],t,'rotation_euler',(.15,0,0))
   for kind,side,limb in p['limbs']:
    g.key(limb,t,'rotation_euler',((-1.1+.6*math.sin(t*9)) if kind=='arm' else 0,0,side*.07 if kind=='arm' else 0))
 x,y=e['poses'][0][1:3];signal('Door_approach_%02d'%e['id'],x,y,start-.75,start,1.8)
for i,b in enumerate(DATA['bullets']):projectile('Defense_shot_%03d'%i,b['t'],b['end'],(math.sin(b['yaw'])*1.8,42+math.cos(b['yaw'])*1.8,2.0),(b['x'],b['y'],1.8))
for door in range(4):
 angle=door*math.pi/2;x=math.sin(angle)*10;y=42+math.cos(angle)*10
 g.cube('Vent_grating',(x,y,.19),(3.5,3.5,.12),'metal',.04)
 for j in range(5):g.cube('Vent_slit',(x-1.4+j*.7,y,.26),(.11,3.15,.03),'charcoal',.01)
 steam=g.empty('Defense_steam_%d'%door,(x,y,.25))
 for j in range(4):g.uvball('Steam_plume',((j%2-.5)*1.1,(j//2-.5)*1.1,1.1),(.65,.65,1.8),'water',steam)
 for base in (107,127,147):show(steam,base+door*2,base+door*2+2);signal('Vent_warning',x,y,base+door*2-1,base+door*2,2.4)
 event('Defense_vent_'+str(door),107+door*2,149+door*2,'telegraphed vent slows approaching enemies')

route_pool={}
for n,t in enumerate((8,17,28,39,47,55,64,72,79,88,95,164,171,179,187,196,205,213,225,234,243,258,267,283,293)):
 role=['marshal','police','chef','barista','clerk','cleaner','attendant'][n%7]
 for side in (-1,1):
  key=(role,side)
  if key not in route_pool:route_pool[key]=actor('Route_%s_%s'%(role,side),role,0,0)
  p=route_pool[key];center,f,right=basis(t);start=center+f*17+right*side*3.4;finish=center+f*(17-3.8*1.72)+right*side*3.4
  show(p['root'],t-1.4,t+1.1);people.move(p,[(t-1.4,start.x,start.y),(t+.32,finish.x,finish.y)]);people.animate_walk(p,t-1.4,t+.32,1.65)
  heading=math.atan2(-f.x,f.y);g.key(p['root'],t+.32,'rotation_euler',(0,0,heading));g.key(p['root'],t+.9,'rotation_euler',(math.pi/2,0,heading))
  player_point=Vector((*route(t),1.9));projectile('Route_shot_%02d_%s'%(n,side),t,t+.32,tuple(player_point),(*finish.xy,1.7))

print('V2 6 game framing and quarter-view defense',flush=True)
data=bpy.data.cameras.new('Game_reference_camera');cam=bpy.data.objects.new('Game_reference_camera',data);s.collection.objects.link(cam);s.camera=cam;data.sensor_fit='VERTICAL';data.sensor_height=24;data.lens=24/(2*math.tan(math.radians(40)/2));data.clip_end=1400
for t,v in [(0,-.065),(97,-.065),(100,0),(160,0),(163,-.065),(300,-.065)]:
 data.shift_y=v;data.keyframe_insert(data_path='shift_y',frame=round(t*FPS)+1)
def camera_pose(t):
 x,y=route(t);idx=min(7200,round(t*FPS));heading=player_yaws[idx]
 if 160<t<164:
  p0=center_route(t-.05);p1=center_route(t+.05);heading=math.atan2(p1[0]-p0[0],-(p1[1]-p0[1]))
 # Unity offset/rotation translated from Y-up into Blender Z-up.
 forward=Vector((math.sin(heading),-math.cos(heading),0));right=Vector((math.cos(heading),math.sin(heading),0))
 pos=Vector((x,y,.18))+right*.1170025-forward*19.2690125+Vector((0,0,12.546))
 direction=forward*math.cos(math.radians(17.4656067))+Vector((0,0,-math.sin(math.radians(17.4656067))))
 target=pos+direction*25
 quarter_pos=Vector((20,42-26,27));quarter_target=Vector((0,42,1.7))
 if 97<t<100:
  u=smooth((t-97)/3);pos=pos.lerp(quarter_pos,u);target=target.lerp(quarter_target,u)
 elif 100<=t<=160:pos=quarter_pos;target=quarter_target
 elif 160<t<163:
  u=smooth((t-160)/3);pos=quarter_pos.lerp(pos,u);target=quarter_target.lerp(target,u)
 # The large indoor wings use a slightly raised game follow shot for aisle legibility.
 if 190<t<253:
  u=min(smooth((t-190)/2),1-smooth((t-250)/3));pos+=Vector((0,0,6))*u;target=target.lerp(Vector((x,y,1.7))+forward*6,u*.55)
 return pos,(target-pos).to_track_quat('-Z','Y').to_euler()
for i in range(1201):
 t=i/4;pos,rot=camera_pose(t);g.key(cam,t,'location',pos);g.key(cam,t,'rotation_euler',rot)

# Roofs are a documented cutaway. Other foreground walls/signs use line-of-sight keys.
for o in a.ROOFS:
 o['camera_cutaway']=True
 for t,v in [(0,False),(73,False),(73+1/FPS,True),(252,True),(252+1/FPS,False),(256,False),(256+1/FPS,True),(278,True),(278+1/FPS,False)]:g.key(o,t,'hide_render',v)
for o in list(s.objects):
 if o.type=='FONT' or 'sign' in o.name.lower() or 'identity' in o.name.lower():
  if o.parent is None and o not in OCCLUDERS:OCCLUDERS.append(o);o['camera_cutaway']=True
bpy.context.view_layer.update()
def intersects(lo,hi,origin,target):
 d=target-origin;near=0.;far=1.
 for axis in range(3):
  if abs(d[axis])<1e-8:
   if origin[axis]<lo[axis] or origin[axis]>hi[axis]:return False
  else:
   a0=(lo[axis]-origin[axis])/d[axis];a1=(hi[axis]-origin[axis])/d[axis]
   if a0>a1:a0,a1=a1,a0
   near=max(near,a0);far=min(far,a1)
   if near>far:return False
 return near<.98 and far>0
boxes=[]
for o in OCCLUDERS:
 if o.type not in ('MESH','FONT','CURVE'):continue
 pts=[o.matrix_world@Vector(v) for v in o.bound_box];lo=Vector(tuple(min(p[i] for p in pts)-.3 for i in range(3)));hi=Vector(tuple(max(p[i] for p in pts)+.3 for i in range(3)));boxes.append((o,lo,hi))
for o,lo,hi in boxes:
 last=None
 for j in range(601):
  t=j*.5;pos,_=camera_pose(t);x,y=route(t);hidden=intersects(lo,hi,pos,Vector((x,y,1.8)))
  if hidden!=last:
   if t>0:g.key(o,max(0,t-1/FPS),'hide_render',not hidden)
   g.key(o,t,'hide_render',hidden);last=hidden

print('V2 7 static mesh batching and save',flush=True)
g.linear_all();s.render.fps=24;s.render.fps_base=1;s.frame_start=1;s.frame_end=7200;s.frame_set(1)
groups={}
for o in list(s.objects):
 if o.type!='MESH' or o.animation_data or o.get('camera_cutaway'):continue
 ancestors=[];p=o.parent
 while p is not None:ancestors.append(p);p=p.parent
 if any(p.animation_data or p.get('role') or p.get('assembly') in ('actor','player','vehicle','gate','moving cart','rolling can','bus','cleaning robot','tipping display') for p in ancestors):continue
 owner=ancestors[-1] if ancestors else None;key=(o.get('zone','SITE'),tuple(m.name for m in o.data.materials),owner.name if owner else '')
 groups.setdefault(key,[]).append(o)
for (zone,mats,owner),objects in groups.items():
 if len(objects)<3:continue
 objects[0].data=objects[0].data.copy();bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name=zone+'__'+('_'.join(mats))+'__'+owner
bpy.ops.object.select_all(action='DESELECT')
for idx,label,a0,b0 in STAGES:s.timeline_markers.new(idx+' '+label,frame=round(a0*FPS)+1)
s['version']='RestStop v2 game-scale';s['store_dimensions']=[88,92];s['restroom_dimensions']=[72,84];s['game_forward_speed']=7.8;s['defense_seconds']=60;s['defense_camera']='fixed quarter view';s['production']='Local Blender only; no TRELLIS or Seedance calls'
for img in bpy.data.images:
 if img.filepath and img.size[0]>0:
  try:img.pack()
  except RuntimeError:pass
bpy.ops.file.pack_all()
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   sp=area.spaces.active;sp.overlay.show_overlays=False;sp.shading.color_type='TEXTURE'
   if sp.region_3d:sp.region_3d.view_perspective='CAMERA'
metrics={'objects':len(s.objects),'mesh_objects':sum(o.type=='MESH' for o in s.objects),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in s.objects if o.type=='MESH'),'vehicles':len(VEHICLES)+1,'parked_vehicles':76+8,'defense_pool':len(pool),'defense_enemies':len(DATA['enemies']),'route_enemy_encounters':50,'store_units':[88,92],'store_area_ratio_to_v1':88*92/(24*44),'restroom_units':[72,84],'restroom_area_ratio_to_v1':72*84/600,'shark_raw_blender_envelope':list(raw_player_envelope),'shark_calibrated_envelope':[game_size[0],game_size[2],game_size[1]],'visual_calibration':list(calibration.scale),'route_lengths':LENGTHS,'holds':HOLDS,'motion_speed':7.8,'fps':24,'frames':7200,'events':EVENTS}
(OUT/'build-metrics.json').write_text(json.dumps(metrics,ensure_ascii=False,indent=2),encoding='utf8');(OUT/'defense-timeline.json').write_text(json.dumps(DATA,separators=(',',':')),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reststop-v2.blend'),compress=True)
print('V2_COMPLETE',json.dumps({k:v for k,v in metrics.items() if k!='events'},ensure_ascii=True),flush=True)
# Normalize evaluated body after the complete pose/action setup, then preserve it.
import runpy
runpy.run_path(str(Path(__file__).with_name('calibrate_player.py')),run_name='__main__')
runpy.run_path(str(Path(__file__).with_name('repair_exit.py')),run_name='__main__')
