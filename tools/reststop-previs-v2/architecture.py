"""Build a single coherent 300-second rest-stop previs scene in Blender."""
import bpy, math, json, sys, random, time
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import geometry as g
import people
from timeline import FPS, STAGES, route, smooth, defense

ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23';OUT.mkdir(parents=True,exist_ok=True)
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

