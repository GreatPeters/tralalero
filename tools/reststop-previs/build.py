"""Build a single coherent 300-second rest-stop previs scene in Blender."""
import bpy, math, json, sys, random, time
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import geometry as g
import people
from timeline import FPS, STAGES, route, smooth, defense

ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23';OUT.mkdir(parents=True,exist_ok=True)
random.seed(230923);bpy.ops.wm.read_factory_settings(use_empty=True);g.setup_materials()
scene=bpy.context.scene;scene.render.fps=FPS;scene.frame_start=1;scene.frame_end=300*FPS
scene.unit_settings.system='METRIC';scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=8
scene.eevee.use_raytracing=False;scene.eevee.use_fast_gi=False;scene.eevee.use_shadows=True
scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
scene.render.threads_mode='FIXED';scene.render.threads=4
scene.world=bpy.data.worlds.new('Daylight');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.68,.8,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=-.15
sun=bpy.data.lights.new('Afternoon sun','SUN');sun.energy=2.5;sun.angle=.15;so=bpy.data.objects.new('Afternoon sun',sun);scene.collection.objects.link(so);so.rotation_euler=(.44,-.56,-.40)
for x,y,power in [(0,10,7500),(60,22,6500),(84,28,3000)]:
    data=bpy.data.lights.new('Sky fill','AREA');data.energy=power;data.shape='DISK';data.size=45
    o=bpy.data.objects.new('Sky fill',data);scene.collection.objects.link(o);o.location=(x,y,32)

def road(name,points,width,mat='asphalt',z=.045):
    if len(points)>2:
        smoothed=[]
        for j in range(len(points)-1):
            p0=Vector(points[max(0,j-1)]);p1=Vector(points[j]);p2=Vector(points[j+1]);p3=Vector(points[min(len(points)-1,j+2)])
            for k in range(8):
                t=k/8;v=.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t);smoothed.append(tuple(v))
        points=smoothed+[points[-1]]
    verts=[]
    for i,(x,y) in enumerate(points):
        prev=Vector(points[max(0,i-1)]);nxt=Vector(points[min(len(points)-1,i+1)]);d=(nxt-prev).normalized();n=Vector((-d.y,d.x))
        verts.extend([(x+n.x*width/2,y+n.y*width/2,z),(x-n.x*width/2,y-n.y*width/2,z)])
    faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(points)-1)]
    return g.surface(name,verts,faces,mat)

def floor(name,x,y,w,d,grid=4):
    g.cube(name,(x,y,.02),(w,d,.24),'tile',.05)
    for xx in range(round(x-w/2)+grid,round(x+w/2),grid):g.cube(name+'_grout',(xx,y,.149),(.028,d,.012),'tile_dark',0)
    for yy in range(round(y-d/2)+grid,round(y+d/2),grid):g.cube(name+'_grout',(x,yy,.149),(w,.028,.012),'tile_dark',0)

def sign(name,label,x,y,z,width=8,height=1.4,mat='ochre',size=.9):
    g.cube(name+'_panel',(x,y,z),(width,.22,height),mat,.06)
    return g.text(name,label,(x,y-.13,z),size,'white')

def portal(name,x,y,w,h=5.8):
    for dx in (-w/2,w/2):g.cube(name+'_pier',(x+dx,y,h/2),(.45,.6,h),'cream',.05)
    g.cube(name+'_lintel',(x,y,h+.22),(w+.7,.65,.6),'cream',.06)

ROOFS=[]
def roof(name,x0,x1,y0,y1,eave=7.6,rise=3.0):
    ridge=(y0+y1)/2;v=[(x0,y0,eave),(x1,y0,eave),(x0,ridge,eave+rise),(x1,ridge,eave+rise),(x0,y1,eave),(x1,y1,eave)]
    r=g.surface(name,v,[(0,1,3,2),(2,3,5,4)],'charcoal');ROOFS.append(r)
    for y in (y0,y1):ROOFS.append(g.cube(name+'_fascia',((x0+x1)/2,y,eave-.1),(x1-x0,.25,.45),'ochre',.035))
    for x in range(round(x0)+1,round(x1),3):
        ROOFS.append(g.curve(name+'_seam',[(x,y0,eave+.04),(x,ridge,eave+rise+.04),(x,y1,eave+.04)],'metal',.024))
    return r

def canopy(name,x0,x1,y=-6):
    for x in range(round(x0),round(x1)+1,6):
        for yy in (y-3,y+3):g.cube(name+'_column',(x,yy,2.45),(.16,.16,4.9),'white',.025)
        pts=[(x,y-3+6*j/20,4.9+1.2*math.sin(math.pi*j/20)) for j in range(21)]
        g.curve(name+'_arch',pts,'white',.055)
    for yy in (y-3,y,y+3):g.beam(name+'_long_rail',(x0,yy,4.9+(1.2 if yy==y else 0)),(x1,yy,4.9+(1.2 if yy==y else 0)),.07,'white')
    verts=[(x,y-3+6*j/20,4.91+1.2*math.sin(math.pi*j/20)) for x in (x0,x1) for j in range(21)]
    ROOFS.append(g.surface(name+'_transparent_cover',verts,[(j,j+1,22+j,21+j) for j in range(20)],'glass'))

def food_counter(name,x,y,label,width=7):
    g.cube(name+'_front',(x,y,1.0),(width,1.1,1.7),'wood',.08)
    g.cube(name+'_worktop',(x,y,1.90),(width+.15,1.35,.15),'cream',.035)
    g.cube(name+'_backsplash',(x,y+1.15,2.0),(width,.18,3.6),'wood_light',.03)
    sign(name+'_menu',label,x,y+1.0,3.62,width,1,'wood',.65)
    for dx in (-2,0,2):
        g.cube(name+'_tray',(x+dx,y-.1,2.03),(1.4,.76,.08),'metal',.025)
        for a in range(4):g.uvball(name+'_food',(x+dx-.5+a*.3,y-.1,2.15),(.12,.16,.12),'ochre')
    for xx in (x-width/2+.2,x+width/2-.2):g.beam(name+'_screenpost',(xx,y-.56,2),(xx,y-.56,2.75),.035,'metal')
    g.cube(name+'_display_glass',(x,y-.56,2.40),(width-.3,.035,.72),'glass',.015)
    g.cube(name+'_display_top',(x,y,2.78),(width-.3,1.05,.035),'glass',.015)
    g.cube(name+'_till',(x+width*.38,y-.07,2.03),(.42,.4,.18),'charcoal',.04)
    screen=g.cube(name+'_register',(x+width*.38,y+.09,2.33),(.50,.07,.4),'charcoal',.03);screen.rotation_euler.x=-.25

def vending(name,x,y):
    for j in range(2):
        xx=x+j*1.35;g.cube(name+'_case',(xx,y,1.4),(1.2,.90,2.65),'red',.08)
        g.cube(name+'_window',(xx-.12,y-.461,1.66),(.84,.025,1.75),'navy',.025)
        for c in range(3):
            for r in range(4):
                g.cylinder(name+'_drink',(xx-.4+c*.28,y-.49,1.1+r*.38),.075,.24,'packet'+str((r+c)%4+1))
        g.cube(name+'_slot',(xx,y-.475,.44),(.75,.07,.20),'black',.025)
        g.cube(name+'_pay',(xx+.44,y-.49,1.3),(.14,.055,.33),'charcoal',.015)

def trolley(name,x,y,mat='metal'):
    r=g.empty(name,(x,y,.16));g.cube(name+'_basket',(0,0,.93),(1.4,.9,.55),mat,.05,r)
    g.cube(name+'_top',(0,0,1.3),(1.5,1.0,.08),'cream',.025,r)
    for dx in (-.54,.54):
        for dy in (-.33,.33):
            g.cube(name+'_leg',(dx,dy,.52),(.075,.075,.75),'metal',.02,r)
            w=g.cylinder(name+'_wheel',(dx,dy,.14),.14,.1,'black',r);w.rotation_euler.y=math.pi/2
    for j in range(3):g.cube(name+'_load',(-.4+j*.4,0,1.48),(.31,.47,.3),'packet'+str(j+1),.04,r)
    return r

def warning_ring(name,x,y,radius,start,end):
    root=g.empty(name,(x,y,.07))
    pts=[(radius*math.cos(i*math.tau/48),radius*math.sin(i*math.tau/48),0) for i in range(49)]
    g.curve(name+'_circle',pts,'warning',.07,root,False);g.visibility(root,start,end);return root

print('BUILD 1: site and roads',flush=True)
g.ZONE='01_ENTRY';g.cube('Site_terrain',(15,-22,-.6),(260,210,1),'grass',0)
road('Highway',[(-104,-112),(-104,85)],18)
entry=[(-104,-104),(-94,-99),(-83,-93),(-76,-84),(-71,-73),(-64,-62),(-51,-52)]
road('Entry_curved_branch',entry,12)
road('Entry_green_guidance',entry,.32,'green',.075)
for side in (-1,1):
    g.curve('Highway_edge',[(-104+side*8,-108,.06),(-104+side*8,84,.06)],'white',.1)
for y in range(-100,80,9):g.cube('Highway_lane',(-104,y,.07),(.13,4,.02),'white',0)
for side in (-1,1):
    rail=[]
    for i,(x,y) in enumerate(entry):
        prev=Vector(entry[max(0,i-1)]);nxt=Vector(entry[min(len(entry)-1,i+1)]);v=(nxt-prev).normalized();xx=x-v.y*side*7;yy=y+v.x*side*7
        rail.append((xx,yy,.86));g.cube('Guardrail_post',(xx,yy,.43),(.15,.15,.86),'metal',.025)
    g.curve('Entry_guardrail',rail,'metal',.09)
sign('Entry_service_sign','휴게소  ↗',-76,-81,8,12,2.6,'green',1.4)
for dx in (-5.7,5.7):g.cube('Entry_sign_support',(-76+dx,-80.6,4),(.3,.3,8),'metal',.04)
g.cube('Entry_pylon',(-63,-74,4.5),(1.8,1.2,9),'cream',.06);g.text('Entry_pylon_text','휴\n게\n소',(-63,-74.65,4.8),1.1,'red')

g.ZONE='02_PARKING';g.cube('Parking_asphalt',(0,-47,.025),(132,66,.07),'asphalt',0)
for row,y in enumerate((-66,-46,-28)):
    for j in range(14):
        x=-51+j*7
        g.cube('Parking_bay',(x,y,.08),(.08,6,.025),'white',0)
        if j%3==0 and not(row==1 and j<6):g.car('Parked_car', (x+3.1,y,.05),['white','blue','red','charcoal'][j%4],math.pi if row%2 else 0)
for x,y in [(-45,-35),(-4,-35),(27,-35),(50,-60),(44,-16)]:g.tree('Parking_tree',x,y,.8)
g.cube('Parking_pedestrian_strip',(0,-20,.12),(112,3.5,.15),'tile',.035)
for x in range(-6,7,2):g.cube('Zebra_crossing',(x,-24,.095),(1.0,6,.035),'white',0)
for x in (-24,44):
    g.cylinder('Parking_lamp',(x,-49,4.4),.1,8.7,'metal')
    g.cube('Parking_lamp_top',(x,-48.6,8.7),(1.5,1.1,.16),'cream',.08)

print('BUILD 2: architecture',flush=True)
g.ZONE='03_PROMENADE';floor('Promenade',22,-8,94,15,4)
canopy('Arched_front_canopy',-24,66,-6)
for x in (-20,-3,16,38,62):g.tree('Promenade_planter',x,-12,.60)
for x in (-18,12,34,58):g.bench('Rest_bench',x,-10,0)
for x in range(-22,69,6):g.cylinder('Bollard',(x,-15,.6),.12,1.0,'charcoal')
g.curve('Tactile_pavement',[(-24,-11,.19),(0,-11,.19),(0,-1,.19)],'ochre',.14)
for x,label in [(-17,'호두과자'),(-8,'간식')]:food_counter('Outdoor_'+label,x,-1.2,label,7)
vending('Front_vending',12,-1.1)

g.ZONE='04_ENTRY_LOBBY';floor('Main_building_floor',22,22,92,44,4)
# Front glazing has real large openings at the main entry and store entry.
for x0,x1 in [(-24,-22),(-3,3),(48,50),(62,68)]:
    if (x0,x1)==(-3,3):continue
    g.cube('Front_pier',((x0+x1)/2,0,3.75),(x1-x0,.5,7.5),'cream',.08)
for x0,x1 in [(-21,-4),(4,46)]:
    for x in range(x0,x1,3):
        g.cube('Front_glass',(x+1.35,0,3.4),(2.55,.055,6.4),'glass',.025)
        g.cube('Front_mullion',(x,0,3.4),(.12,.15,6.8),'charcoal',.02)
g.cube('Back_wall',(22,44,3.9),(92,.4,7.8),'cream',.04)
for x in (-24,68):
    for y0,y1 in [(0,17),(27,44)]:g.cube('Side_wall',(x,(y0+y1)/2,3.9),(.4,y1-y0,7.8),'cream',.04)
for x in (-22,22,44,66):
    for y in (2,42):g.cube('Structural_column',(x,y,3.7),(.70,.7,7.4),'cream',.05)
roof('Main_roof',-25,45,-1,45,7.8,3.0);roof('Store_roof',44,69,-1,45,7.3,2.1)
sign('Main_name','휴게소',0,-.5,7.0,10,1.7,'ochre',1.4)
portal('Main_entry',0,-.1,6,5.8)
doors=[]
for side in (-1,1):
    r=g.empty('Automatic_door_'+str(side),(side*1.45,-.15,0));doors.append(r)
    g.cube('Door_glass',(0,0,2.7),(2.85,.07,5.3),'glass',.04,r)
    for dx in (-1.39,1.39):g.cube('Door_frame',(dx,0,2.7),(.11,.16,5.4),'charcoal',.02,r)
    g.cube('Door_handle',(-side*.9,-.1,2.5),(.05,.10,.68),'metal',.015,r)
    for t,x in [(0,side*1.45),(84,side*1.45),(87,side*4.4),(100,side*4.4),(102,side*1.45)]:g.key(r,t,'location',(x,-.15,0))

g.ZONE='05_DEFENSE_HALL'
for x in (-16,0,16):food_counter('Hall_service',x,41,'안내' if x==0 else '간식',9)
for x in (-20,20):
    for y in (7,34):g.tree('Hall_corner_tree',x,y,.55)
for x in (-13,13):
    for y in (7,34):g.bench('Hall_bench',x,y,math.pi if y==7 else 0)
for x in (-20,20):
    for y in (17,27):g.cube('Side_approach_pier',(x,y,2.75),(.5,.5,5.5),'cream',.04)
    g.cube('Side_approach_lintel',(x,22,5.5),(.5,10.5,.4),'cream',.04)
g.curve('Hall_border',[(-18,3,.16),(18,3,.16),(18,38,.16),(-18,38,.16),(-18,3,.16)],'wood_light',.07)

g.ZONE='06_DINING'
for x in (28,37):
    for y in (8,14,31,37):g.table('Dining_table',x,y)
food_counter('Korean_food',29,41,'한식',9);food_counter('Noodles',40,41,'우동',9)
sign('Dining_wayfinding','식당',33,1.2,5.1,13,1.2,'wood',1.05)
for x in (24,42):g.cube('Dining_partition_low',(x,11,1.2),(.18,15,2.1),'wood',.05)

g.ZONE='07_CONVENIENCE'
sign('Convenience_name','편의점',56,-.35,6.4,16,1.55,'ochre',1.4)
portal('Store_front_door',56,0,7.5,5.5)
for x in (46,66):g.cube('Store_front_glass',(x,0,3.1),(3,.06,5.8),'glass',.02)
for y in (8,14,32,38):
    for x in (50,58,64):
        w=5.2 if x!=64 else 3.5
        g.cube('Shelf_back',(x,y,1.5),(w,.16,2.8),'wood',.025)
        for z in (.45,1.08,1.71,2.35):
            g.cube('Shelf_tier',(x,y-.05,z),(w,1.0,.08),'cream',.02)
            for j in range(7):
                for side in (-1,1):
                    g.cube('Snack_packet',(x-w*.42+j*w*.14,y+side*.30,z+.22),(.22,.25,.36),'packet'+str((j+int(z*10))%4+1),.025)
for x in range(48,66,3):
    g.cube('Refrigerator',(x,43,1.55),(2.7,1.15,3.0),'charcoal',.06)
    for dx in (-.66,.66):
        g.cube('Fridge_glass',(x+dx,42.40,1.66),(1.23,.055,2.5),'glass',.035)
        g.cube('Fridge_handle',(x+dx+.43,42.33,1.4),(.06,.08,.55),'metal',.02)
        for zz in (.7,1.3,1.9,2.5):
            for j in range(3):g.cylinder('Drink_bottle',(x+dx-.33+j*.3,42.63,zz),.075,.3,'packet'+str((j+int(zz))%4+1))
for x in (49,61):
    g.cube('Checkout',(x,4,1.03),(4.5,1.4,1.85),'cream',.07)
    g.cube('Checkout_wood',(x,3.28,.92),(4.35,.08,1.4),'wood_light',.025)
    g.cube('Checkout_screen',(x,3.8,2.21),(.66,.12,.50),'charcoal',.04)
sign('Restroom_link','화장실 →',65,25,5.3,6.0,.85,'green',.63)

print('BUILD 3: expanded restroom and fuel',flush=True)
g.ZONE='08_RESTROOM';floor('Restroom_600m2',84,28,30,20,2)
g.cube('Restroom_store_connector',(68.5,22,.026),(1.3,8,.24),'tile',.03)
g.cube('Restroom_fuel_walkway',(88,4.5,.026),(7,27,.24),'tile',.04)
for x in (84.4,91.6):g.cube('Restroom_walkway_curb',(x,4.5,.20),(.15,27,.30),'cream',.03)
g.cube('Restroom_back_wall',(84,38,3.1),(30,.35,6.2),'cream',.04)
g.cube('Restroom_east_wall',(99,28,3.1),(.35,20,6.2),'cream',.04)
for x0,x1 in [(69,83),(91,99)]:g.cube('Restroom_front_wall',((x0+x1)/2,18,3.1),(x1-x0,.3,6.2),'cream',.04)
for y0,y1 in [(18,19),(25,38)]:g.cube('Restroom_west_wall',(69,(y0+y1)/2,3.1),(.3,y1-y0,6.2),'cream',.04)
g.cube('Restroom_gender_divider',(84,31.5,2.9),(.25,13,5.8),'cream',.04)
roof('Restroom_roof',68.7,99.3,17.6,38.4,6.3,.45)
portal('Restroom_exit',87,18,7,5.5);sign('Restroom_name','화장실',87,17.7,5.55,9,1.2,'cream',1.0)
for x,label in [(76.5,'여자'),(92,'남자')]:
    sign('Restroom_gender',label,x,24.6,4.3,5,.9,'wood',.7)
    g.cube('Wash_counter',(x,24.9,1.0),(7.2,1.1,1.5),'cream',.08)
    for dx in (-2.4,-.8,.8,2.4):
        # A rim and recessed bowl rather than a solid white box.
        g.uvball('Wash_basin',(x+dx,24.5,1.79),(.53,.43,.13),'white')
        g.uvball('Basin_recess',(x+dx,24.44,1.86),(.39,.28,.055),'tile_dark')
        g.curve('Tap',[(x+dx,24.82,1.8),(x+dx,24.82,2.12),(x+dx,24.57,2.12)],'metal',.035)
        g.cube('Wash_mirror',(x+dx,25.22,2.8),(1.2,.06,1.45),'metal',.025)
for side,xs in [('women',[71,73.1,75.2,77.3,79.4,81.5]),('men',[86.2,88.6,91.0,93.4,95.8,98.0])]:
    for x in xs:
        for yy in ([30.6,35.8] if side=='women' else [35.8]):
            g.cube('Toilet_cubicle_side',(x-1,yy,1.5),(.10,3.3,2.85),'wood_light',.025)
            g.cube('Toilet_cubicle_door',(x,yy-1.67,1.5),(1.87,.10,2.80),'wood_light',.03)
            g.cylinder('Toilet_hinge',(x-.86,yy-1.67,1.4),.045,2.5,'metal')
            g.uvball('Toilet_bowl',(x,yy+.25,.65),(.47,.70,.45),'white')
            g.uvball('Toilet_seat',(x,yy+.05,1.00),(.43,.56,.10),'white')
            g.cube('Toilet_cistern',(x,yy+.78,1.1),(.85,.32,.90),'white',.1)
            g.uvball('Door_handle',(x+.65,yy-1.76,1.4),(.055,.035,.065),'metal')
for x in (86.2,88.4,90.6,92.8,95,97.2):
    g.uvball('Urinal_body',(x,31.2,1.32),(.42,.39,.71),'white')
    g.uvball('Urinal_recess',(x,30.88,1.35),(.29,.075,.43),'tile_dark')
    g.cube('Urinal_divider',(x+1,31.1,1.50),(.065,1.2,1.65),'cream',.04)
g.cube('Restroom_brick_band',(84,37.80,4.9),(29,.08,1.3),'brick',.02)
for x in range(71,99,5):g.cube('Restroom_high_window',(x,37.70,5.3),(3.8,.05,.9),'glass',.025)
# Keep the shared entry aisle clear; privacy screens belong beyond circulation.
g.text('Accessible_sign','♿',(71,18-.2,2.8),.7,'blue')

g.ZONE='09_FUEL';g.cube('Fuel_concrete',(88,-28,.04),(36,38,.12),'tile',.04)
fuelroof=g.cube('Fuel_red_canopy',(89,-31,6.45),(24,23,.65),'red',.12)
fueltop=g.cube('Fuel_canopy_top',(89,-31,6.82),(24.3,23.3,.10),'cream',.03)
for o in (fuelroof,fueltop):
    for t,v in [(0,False),(255,False),(255+1/FPS,True),(277,True),(277+1/FPS,False)]:g.key(o,t,'hide_render',v)
sign('Fuel_sign','주유소',89,-42.7,6.4,11,1,'red',1.05)
for x in (79,99):
    for y in (-22,-40):g.cube('Fuel_canopy_column',(x,y,3.15),(.65,.65,6.3),'white',.06)
for x in (81,97):
    for y in (-26,-36):
        g.cube('Pump_island',(x,y,.26),(2.5,4.8,.5),'cream',.12)
        g.cube('Fuel_pump',(x,y,1.65),(1.4,.80,2.3),'white',.1)
        g.cube('Pump_red_base',(x,y,.78),(1.42,.82,.45),'red',.04)
        g.cube('Pump_screen',(x,y-.43,2.0),(.87,.025,.63),'charcoal',.025)
        g.text('Pump_digits','000.0',(x,y-.45,2.05),.19,'green')
        for dx in (-.82,.82):
            g.curve('Fuel_hose',[(x+dx,y,2.5),(x+dx*1.4,y,2),(x+dx*1.5,y,.65),(x+dx,y-.1,.7),(x+dx,y-.12,1.65)],'black',.042)
            g.cube('Fuel_nozzle',(x+dx,y-.15,1.7),(.13,.18,.32),'red',.035)
        for dx in (-1.25,1.25):g.cylinder('Pump_bollard',(x+dx,y-1.5,.8),.09,1.15,'ochre')
g.cube('Fuel_shop',(77,-11,2.25),(10,10,4.5),'cream',.08)
g.cube('Fuel_shop_window',(77,-16.1,2.25),(7,.055,3.1),'glass',.025)
sign('Fuel_shop_sign','주유 안내',77,-16.2,4.0,8,.8,'red',.68)
g.car('Fuel_customer',(101,-27,.14),'white',0)
g.curve('Fuel_lane',[(88,-8,.12),(88,-43,.12)],'ochre',.07)

g.ZONE='10_EXIT';exitpoints=[(88,-43),(101,-50),(108,-62),(112,-88),(116,-105)]
road('Exit_merge',exitpoints,12)
g.curve('Exit_guidance',[(x,y,.085) for x,y in exitpoints],'white',.1)
sign('Exit_service_sign','출구  ↑',112,-75,7.5,10,2,'green',1.3)
for dx in (-4.6,4.6):g.cube('Exit_sign_post',(112+dx,-74.7,3.75),(.25,.25,7.5),'metal',.025)
g.cube('Exit_barrier_base',(104,-65,1.1),(1.0,1.0,2),'ochre',.1)
gate=g.empty('Exit_gate_hinge',(104,-65,1.9));g.cube('Exit_gate_arm',(4,0,0),(8,.17,.18),'white',.025,gate)
for x in range(1,8,2):g.cube('Exit_gate_red_band',(x,0,0),(.8,.18,.19),'red',.005,gate)
for t,a in [(0,0),(287,0),(290,math.pi/2),(300,math.pi/2)]:g.key(gate,t,'rotation_euler',(0,-a,0))
g.cube('Entry_barrier_base',(-59,-59,1.1),(1,1,2),'ochre',.1)
entrygate=g.empty('Entry_gate_hinge',(-59,-59,1.9));g.cube('Entry_gate_arm',(-4,0,0),(8,.17,.18),'white',.025,entrygate)
for t,a in [(0,0),(15,0),(18,math.pi/2),(30,math.pi/2)]:g.key(entrygate,t,'rotation_euler',(0,a,0))

g.ZONE='LANDSCAPE'
for x,y,sz in [(-72,3,1),(-45,22,1.4),(-45,52,1.3),(-17,59,1.1),(16,62,1.3),(52,60,1.1),(87,62,1.3),(110,30,1.1),(111,-8,1),(55,-82,1.1),(-39,-89,1)]:g.tree('Perimeter_tree',x,y,sz)
for i in range(12):g.uvball('Forest_hill',(-120+i*24,95,-6),(25,30,15+random.random()*8),'grass')
for x in range(-110,145,12):
    g.uvball('Distant_canopy',(x,78,6),(6,6,7),'leaf')

print('BUILD 4: moving actors and mechanics',flush=True)
# Moveable local props have physical pivots and paths, not still-frame replacements.
reverse=g.car('Reversing_car',(-28,-44,.07),'blue',0)
for t,y in [(0,-44),(32,-44),(36,-36),(40,-36),(43,-44),(300,-44)]:g.key(reverse,t,'location',(-28,y,.07))
warning_ring('Reverse_warning',-28,-37,3.3,30,37)
cart=trolley('Snack_cart',-3,-12)
for t,x in [(0,-3),(59,-3),(63,-12),(66,-12),(69,-3)]:g.key(cart,t,'location',(x,-18,.2))
warning_ring('Cart_warning',-11,-18,2.0,57,63)
foodcart=trolley('Dining_cart',28,15)
for t,y in [(0,15),(171,15),(175,26),(180,26),(184,15)]:g.key(foodcart,t,'location',(28,y,.2))
storecart=trolley('Store_cart',53,16,'wood_light')
for t,y in [(0,16),(204,16),(209,27),(213,27),(217,16)]:g.key(storecart,t,'location',(53,y,.2))
clean=trolley('Cleaning_cart',82,23,'yellow')
for t,x in [(0,82),(230,82),(234,88),(237,88),(242,82)]:g.key(clean,t,'location',(x,23,.2))
puddle=g.uvball('Wet_floor',(85,24,.19),(3.5,1.4,.015),'water');warning_ring('Wet_floor_warning',85,24,3.1,228,243)
g.cube('Wet_floor_sign',(81,24,.60),(.6,.20,1.0),'yellow',.04);g.text('Wet_floor_label','주의',(81,23.88,.68),.27,'charcoal')
passing=g.car('Fuel_crossing_car',(114,-45,.1),'red',math.pi/2)
for t,x in [(0,114),(267,114),(271,96),(274,80),(300,80)]:g.key(passing,t,'location',(x,-45,.1))
warning_ring('Fuel_crossing_warning',90,-45,3.2,265,272)

# Environment artists' repeated details are intentional assemblies. Characters are new local meshes.
cast=[]
for name,role,loc in [('Parking_staff','marshal',(-43,-48,.10)),('Snack_chef','chef',(-17,.8,.2)),('Barista','barista',(-8,.9,.2)),('Cashier','clerk',(49,5,.2)),('Cleaner','cleaner',(82,25,.2)),('Fuel_staff','attendant',(81,-21,.15))]:
    p=people.person(name,role,loc,.95);people.animate_idle(p,0,300);cast.append(p)
for i,(x,y) in enumerate([(-21,-9),(-16,-8),(-3,-11),(8,-10)]):
    p=people.person('Fleeing_visitor_'+str(i),'traveler',(x,y,.2),.88)
    people.animate_idle(p,0,53+i);people.animate_walk(p,53+i,67+i,1.5)
    people.move(p,[(0,x,y),(53+i,x,y),(60+i,x-3,y+5),(67+i,x-7,y+8)]);cast.append(p)

print('BUILD 5: canonical shark import',flush=True)
before=set(scene.objects);source=next((ROOT/'Assets/JH/Model/Player/Sharks').rglob('Original.fbx'));bpy.ops.import_scene.fbx(filepath=str(source))
imported=[o for o in scene.objects if o not in before]
for o in imported:o.animation_data_clear()
rig=next(o for o in imported if o.type=='ARMATURE');rig.scale*=300;bpy.context.view_layer.update()
meshes=[o for o in imported if o.type=='MESH'];pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)));hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
factor=3.3/(hi.z-lo.z);rig.scale*=factor;bpy.context.view_layer.update()
pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)));hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
player=g.empty('PLAYER_ROOT');offset=Vector((-(hi.x+lo.x)/2,-(hi.y+lo.y)/2,-lo.z))
for o in imported:
    if o.parent is None:o.location+=offset;o.parent=player
rig.name='Shark_existing_project_rig';player['source']=str(source.relative_to(ROOT));player['turn_rate_limit_deg_s']=90
route_yaw=math.pi
for t in [i/6 for i in range(1801)]:
    x,y=route(t);ground=.08+.13*smooth((y+18)/6)
    g.key(player,t,'location',(x,y,ground))
    a=route(max(0,t-.12));b=route(min(300,t+.12));dx=b[0]-a[0];dy=b[1]-a[1]
    if not 100<=t<=160:
        target=math.atan2(dx,-dy) if abs(dx)+abs(dy)>.0001 else route_yaw
        delta=(target-route_yaw+math.pi)%(2*math.pi)-math.pi
        if t>0:route_yaw+=max(-math.pi/12,min(math.pi/12,delta))
        g.key(player,t,'rotation_euler',(0,0,route_yaw))
    # Small limb swing on the existing mesh/rig, never altering its anatomy.
    moving=not 98<=t<=160;v=math.sin(t*math.tau*1.4)*(.10 if moving else .015)
    for name,sign in [('frontleg',1),('R_frontleg',-1),('backleg',-1),('R_backleg',1)]:
        if name in rig.pose.bones:
            bone=rig.pose.bones[name];bone.rotation_mode='XYZ';bone.rotation_euler.x=v*sign;bone.keyframe_insert(data_path='rotation_euler',frame=round(t*FPS)+1)

d=defense()
for t,yaw in d['yaw']:g.key(player,t,'rotation_euler',(0,0,math.pi-yaw))
# Remove the wrap discontinuity when returning to forward motion after defense.
last_yaw=math.pi-d['yaw'][-1][1]
for t in [160+i/6 for i in range(841)]:
    a=route(max(160,t-.12));b=route(min(300,t+.12));dx=b[0]-a[0];dy=b[1]-a[1]
    target=math.atan2(dx,-dy) if abs(dx)+abs(dy)>.00001 else last_yaw
    delta=(target-last_yaw+math.pi)%(2*math.pi)-math.pi
    if t>160:last_yaw+=max(-math.pi/12,min(math.pi/12,delta))
    g.key(player,t,'rotation_euler',(0,0,last_yaw))
aim=g.empty('Aim_direction',(0,0,.05),player)
g.curve('Aim_circle',[(1.4*math.cos(i*math.tau/48),1.4*math.sin(i*math.tau/48),0) for i in range(49)],'reticle',.035,aim,False)
g.surface('Aim_arrow',[(-.23,-1.6,0),(.23,-1.6,0),(0,-2.3,0)],[(0,1,2)],'reticle',aim,False)

def projectile(name,start,end,a,b):
    root=g.empty(name)
    g.uvball(name+'_water',(0,0,0),(.14,.14,.14),'water',root)
    g.key(root,start,'location',a);g.key(root,end,'location',b);g.visibility(root,start,end)
    flash=g.empty(name+'_impact',b);g.uvball(name+'_burst',(0,0,0),(.4,.4,.4),'reticle',flash);g.visibility(flash,end,end+.12)
    return root

for e in d['enemies']:
    i=e['id'];p=people.person('Defense_police_%02d'%i,'police',(0,0,.21),1.16 if i in (23,27,31) else 1.04)
    poses=e['poses'];spawn=e['spawn'];dead=e['dead'];g.visibility(p['root'],spawn,dead+.5)
    for t,x,y in poses[::4]:
        if t>dead:break
        g.key(p['root'],t,'location',(x,y,.21));g.key(p['root'],t,'rotation_euler',(0,0,-e['angle']))
    last=next((r for r in reversed(poses) if r[0]<=dead),poses[-1]);g.key(p['root'],dead,'location',(last[1],last[2],.21))
    g.key(p['root'],dead,'rotation_euler',(0,0,-e['angle']));g.key(p['root'],dead+.4,'rotation_euler',(math.pi/2,0,-e['angle']))
    people.animate_walk(p,spawn,dead,1.2);warning_ring('Approach_%02d'%i,poses[0][1],poses[0][2],1.15,spawn-.9,spawn)
for i,b in enumerate(d['bullets']):projectile('Defense_water_%02d'%i,b['t'],b['end'],(math.sin(b['yaw'])*1.5,20+math.cos(b['yaw'])*1.5,1.7),(b['x'],b['y'],1.5))

for n,(t,role) in enumerate([(17,'marshal'),(40,'police'),(61,'chef'),(91,'police'),(176,'chef'),(205,'clerk'),(237,'cleaner'),(263,'attendant'),(290,'police')]):
    ex,ey=route(t+6);tx,ty=route(t+3)
    p=people.person('Route_encounter_%02d'%n,role,(ex,ey,.21),1.0)
    people.move(p,[(t,ex,ey),(t+3,tx,ty)]);people.animate_walk(p,t,t+3.2,1.1);g.visibility(p['root'],t-.5,t+4)
    g.key(p['root'],t+3,'rotation_euler',tuple(p['root'].rotation_euler));g.key(p['root'],t+3.7,'rotation_euler',(math.pi/2,0,p['root'].rotation_euler.z))
    px,py=route(t+2);projectile('Route_water_%02d'%n,t+2,t+3,(px,py,1.7),(tx,ty,1.45))

print('BUILD 6: cameras and native timeline',flush=True)
data=bpy.data.cameras.new('Continuous route camera');cam=bpy.data.objects.new('Continuous route camera',data);scene.collection.objects.link(cam);scene.camera=cam;data.lens=34;data.clip_end=1000
def camera_pose(t):
    x,y=route(t)
    target=Vector((x,y+4,1.2));pos=Vector((x+13,y-30,29))
    if t<8:
        u=smooth(t/8);pos=Vector((145,-175,153)).lerp(pos,u);target=Vector((12,-15,0)).lerp(target,u)
    if 96<t<100:
        u=smooth((t-96)/4);pos=pos.lerp(Vector((0,19.99,76)),u);target=target.lerp(Vector((0,20,0)),u)
    elif 100<=t<=160:pos=Vector((0,19.99,76));target=Vector((0,20,0))
    elif 160<t<165:
        u=smooth((t-160)/5);pos=Vector((0,19.99,76)).lerp(pos,u);target=Vector((0,20,0)).lerp(target,u)
    if 215<t<253:
        w=min(smooth((t-215)/3),1-smooth((t-250)/3))
        pos+=Vector((-4,16,4))*w
    return pos,(target-pos).to_track_quat('-Z','Y').to_euler()
for i in range(1201):
    t=i/4;pos,rot=camera_pose(t);g.key(cam,t,'location',pos);g.key(cam,t,'rotation_euler',rot)
for index,label,a,b in STAGES:
    scene.timeline_markers.new(index+' '+label+' '+str(a)+'s',frame=round(a*FPS)+1)
for o in ROOFS:
    for t,v in [(0,False),(81,False),(81+1/FPS,True),(251,True),(251+1/FPS,False)]:g.key(o,t,'hide_render',v)
    o['cutaway']='Removed between 81s and 251s to show interior; same scene geometry retained.'
for o in list(scene.objects):
    if o.name.startswith('Main_name'):
        for t,v in [(0,False),(84,False),(84+1/FPS,True),(101,True),(101+1/FPS,False)]:g.key(o,t,'hide_render',v)
g.linear_all()
scene['total_seconds']=300;scene['defense_seconds']=60;scene['restroom_floor_area_m2']=600;scene['restroom_concept_baseline_m2']=120
scene['production']='Local Blender authoring. No TRELLIS or Seedance generation.'
scene['notes']='Architectural and motion previs; scripted encounters, not a Unity gameplay or balance test.'
scene['restroom_camera_refined']=True
scene['restroom_aisle_cleared']=True;scene['actor_ownership_restored']=True
scene.render.fps=FPS;scene.render.fps_base=1;scene.frame_start=1;scene.frame_end=300*FPS
scene.frame_set(1)

# Merge only static meshes sharing zone/material; leave movable assemblies editable.
print('BUILD 7: static mesh batching',flush=True)
groups={}
for o in list(scene.objects):
    if o.type!='MESH' or o.animation_data:continue
    ancestors=[];p=o.parent
    while p is not None:ancestors.append(p);p=p.parent
    if any(a.animation_data or a==player or a.get('role') for a in ancestors):continue
    if o in ROOFS:continue
    key=(o.get('zone','SITE'),tuple(m.name if m else '' for m in o.data.materials))
    groups.setdefault(key,[]).append(o)
for (zone,mats),objects in groups.items():
    if len(objects)<3:continue
    # Joining into an instanced data block must not expand unrelated mesh users.
    objects[0].data=objects[0].data.copy()
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name=zone+'__'+('_'.join(mats))
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.update()
for img in bpy.data.images:
    if img.filepath and img.size[0]>0:
        try:img.pack()
        except RuntimeError:pass
bpy.ops.file.pack_all()
metrics={'blender':bpy.app.version_string,'objects':len(scene.objects),'mesh_objects':sum(o.type=='MESH' for o in scene.objects),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in scene.objects if o.type=='MESH'),'materials':len(bpy.data.materials),'actions':len(bpy.data.actions),'fps':FPS,'frames':300*FPS,'defense':{'duration':60,'enemies':len(d['enemies']),'shots':d['shots'],'max_degrees_per_second':d['maximum_degrees_per_second']},'restroom_m2':600,'restroom_area_ratio':5,'shark_source':str(source),'stages':STAGES}
(OUT/'build-metrics.json').write_text(json.dumps(metrics,ensure_ascii=False,indent=2),encoding='utf8')
(OUT/'defense-timeline.json').write_text(json.dumps(d,separators=(',',':')),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reststop-master.blend'),compress=True)
print('BUILD_COMPLETE',json.dumps(metrics,ensure_ascii=True),flush=True)
