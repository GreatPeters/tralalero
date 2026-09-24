"""Small Blender construction helpers with reusable evaluated mesh geometry."""
import bpy, bmesh, math, random
from mathutils import Vector
from timeline import visibility_frames

M={};CACHE={};ZONE='SITE';STATIC=[]

def material(name,color,rough=.65,metal=0,alpha=1,emission=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,alpha);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,alpha)
    bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
    bs.inputs['Alpha'].default_value=alpha
    if alpha<1:m.surface_render_method='BLENDED'
    if emission:
        bs.inputs['Emission Color'].default_value=(*color,1);bs.inputs['Emission Strength'].default_value=emission
    M[name]=m;return m

def setup_materials():
    for name,c in {
        'cream':(.72,.66,.54),'tile':(.66,.64,.57),'tile_dark':(.27,.3,.29),
        'wood':(.35,.16,.065),'wood_light':(.55,.30,.11),'charcoal':(.055,.075,.082),
        'ochre':(.91,.52,.09),'red':(.68,.07,.038),'green':(.12,.26,.10),
        'grass':(.20,.34,.105),'leaf':(.14,.3,.07),'leaf_light':(.28,.43,.10),
        'asphalt':(.095,.12,.13),'white':(.89,.88,.81),'skin':(.67,.38,.20),
        'hair':(.045,.025,.012),'navy':(.025,.068,.14),'teal':(.075,.28,.26),
        'blue':(.02,.40,.73),'olive':(.29,.35,.12),'pink':(.74,.22,.26),
        'yellow':(.97,.68,.06),'black':(.009,.014,.02),'orange':(.98,.23,.028),
        'packet1':(.86,.16,.05),'packet2':(.85,.58,.04),'packet3':(.18,.43,.19),
        'packet4':(.04,.32,.58),'brick':(.36,.16,.07),
    }.items():material(name,c)
    material('metal',(.36,.43,.46),.3,.7)
    material('glass',(.50,.78,.82),.19,.15,.23)
    material('water',(.02,.51,.91),.3,0,.72,1.2)
    material('light',(.94,.79,.45),.3,0,1,2.5)
    material('warning',(.96,.3,.01),.5,0,.5,.8)
    material('reticle',(.035,.78,.95),.5,0,1,1.3)

def empty(name,loc=(0,0,0),parent=None):
    o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=loc;o.parent=parent;return o

def mesh_object(name,mesh,loc,mat,parent=None,static=True):
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.location=loc;o.parent=parent
    if not mesh.materials:mesh.materials.append(M[mat])
    o.color=M[mat].diffuse_color;o['zone']=ZONE
    if static and parent is None:STATIC.append(o)
    return o

def cube(name,loc,size,mat,bevel=.035,parent=None,static=True):
    key=('box',tuple(round(x,4) for x in size),mat,round(bevel,4))
    if key not in CACHE:
        bm=bmesh.new();bmesh.ops.create_cube(bm,size=1)
        for v in bm.verts:v.co=Vector((v.co.x*size[0],v.co.y*size[1],v.co.z*size[2]))
        if bevel>0:
            bmesh.ops.bevel(bm,geom=list(bm.edges),offset=min(bevel,min(size)*.22),segments=2,affect='EDGES')
        me=bpy.data.meshes.new('cuboid');bm.to_mesh(me);bm.free();me.update();CACHE[key]=me
    return mesh_object(name,CACHE[key],loc,mat,parent,static)

def uvball(name,loc,scale,mat,parent=None,static=True):
    key=('ball',tuple(round(x,4) for x in scale),mat)
    if key not in CACHE:
        bm=bmesh.new();bmesh.ops.create_uvsphere(bm,u_segments=20,v_segments=12,radius=1)
        for v in bm.verts:v.co=Vector((v.co.x*scale[0],v.co.y*scale[1],v.co.z*scale[2]))
        me=bpy.data.meshes.new('rounded_surface');bm.to_mesh(me);bm.free()
        for p in me.polygons:p.use_smooth=True
        CACHE[key]=me
    return mesh_object(name,CACHE[key],loc,mat,parent,static)

def cylinder(name,loc,radius,depth,mat,parent=None,vertices=20,static=True):
    key=('cylinder',round(radius,4),round(depth,4),mat,vertices)
    if key not in CACHE:
        bm=bmesh.new();bmesh.ops.create_cone(bm,cap_ends=True,cap_tris=False,segments=vertices,radius1=radius,radius2=radius,depth=depth)
        me=bpy.data.meshes.new('cylinder');bm.to_mesh(me);bm.free()
        for p in me.polygons:p.use_smooth=len(p.vertices)==4
        CACHE[key]=me
    return mesh_object(name,CACHE[key],loc,mat,parent,static)

def beam(name,a,b,radius,mat,parent=None,static=True):
    a=Vector(a);b=Vector(b);o=cylinder(name,(a+b)/2,radius,(b-a).length,mat,parent,static=static)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o

def curve(name,pts,mat,radius=.05,parent=None,static=True):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=1;cu.bevel_depth=radius;cu.bevel_resolution=2
    sp=cu.splines.new('POLY');sp.points.add(len(pts)-1)
    for p,co in zip(sp.points,pts):p.co=(*co,1)
    cu.materials.append(M[mat]);o=bpy.data.objects.new(name,cu);bpy.context.collection.objects.link(o);o.parent=parent;o['zone']=ZONE
    if static and parent is None:STATIC.append(o)
    return o

def surface(name,vertices,faces,mat,parent=None,static=True,smooth=False):
    me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.update()
    if smooth:
        for p in me.polygons:p.use_smooth=True
    return mesh_object(name,me,(0,0,0),mat,parent,static)

def loft(name,sections,mat,parent=None,segments=24):
    verts=[]
    for z,rx,ry,cy in sections:
        for i in range(segments):
            a=i*math.tau/segments;verts.append((rx*math.cos(a),cy+ry*math.sin(a),z))
    faces=[]
    for j in range(len(sections)-1):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments;faces.append((a,b,b+segments,a+segments))
    faces.extend([tuple(reversed(range(segments))),tuple((len(sections)-1)*segments+i for i in range(segments))])
    return surface(name,verts,faces,mat,parent,False,True)

FONT=None
def text(name,body,loc,size=1,mat='charcoal',rot=(math.pi/2,0,0),parent=None):
    global FONT
    if FONT is None:FONT=bpy.data.fonts.load('C:/Windows/Fonts/malgunbd.ttf')
    cu=bpy.data.curves.new(name,'FONT');cu.body=body;cu.font=FONT;cu.size=size;cu.align_x='CENTER';cu.align_y='CENTER';cu.extrude=.012;cu.bevel_depth=.004;cu.bevel_resolution=1;cu.materials.append(M[mat])
    o=bpy.data.objects.new(name,cu);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=rot;o.parent=parent;o['zone']=ZONE
    if parent is None:STATIC.append(o)
    return o

def tree(name,x,y,scale=1):
    cylinder(name+'_bed',(x,y,.22),(1.8*scale),.45,'cream')
    cylinder(name+'_soil',(x,y,.47),1.65*scale,.10,'wood')
    beam(name+'_trunk',(x,y,.5),(x+.13,y,4.1*scale),.22*scale,'wood')
    for j,(dx,dy,z) in enumerate([(-.7,0,4),(1,.2,4.4),(0,.8,5.2),(.1,-.6,4.8)]):
        uvball(name+'_crown'+str(j),(x+dx*scale,y+dy*scale,z*scale),(1.5*scale,1.25*scale,1.25*scale),'leaf' if j%2 else 'leaf_light')

def bench(name,x,y,rot=0):
    root=empty(name,(x,y,0));root.rotation_euler.z=rot
    for dx in (-1,1):
        cube(name+'_leg',(dx,0,.46),(.12,.72,.8),'charcoal',parent=root)
        cube(name+'_back',(dx,.30,.95),(.10,.10,1.1),'charcoal',parent=root)
    for i in range(4):cube(name+'_seat',(0,-.24+i*.16,.85),(2.55,.13,.08),'wood_light',parent=root)
    for z in (1.05,1.27,1.49):cube(name+'_slat',(0,.35,z),(2.55,.08,.17),'wood_light',parent=root)
    return root

def chair(name,x,y,rot=0,parent=None):
    r=empty(name,(x,y,0),parent);r.rotation_euler.z=rot
    for dx in (-.26,.26):
        for dy in (-.25,.25):cube(name+'_leg',(dx,dy,.48),(.065,.065,.90),'charcoal',.018,r)
    cube(name+'_seat',(0,0,.94),(.68,.66,.12),'ochre',.07,r)
    cube(name+'_back',(0,.28,1.40),(.66,.13,.85),'olive',.075,r)
    return r

def table(name,x,y):
    r=empty(name,(x,y,0))
    cube(name+'_top',(0,0,1.05),(2.65,1.5,.16),'wood_light',.07,r)
    for dx in (-1.08,1.08):
        for dy in (-.52,.52):cube(name+'_leg',(dx,dy,.53),(.12,.12,1.05),'charcoal',.022,r)
    for dx in (-.72,.72):
        chair(name+'_chair',dx,-1.15,0,r);chair(name+'_chair',dx,1.15,math.pi,r)
    cylinder(name+'_vase',(0,0,1.24),.14,.24,'cream',r)
    uvball(name+'_plant',(0,0,1.48),(.26,.22,.25),'leaf',r)
    return r

def car(name,loc,color='white',rot=0):
    r=empty(name,loc);r.rotation_euler.z=rot
    cube(name+'_body',(0,0,.83),(2.0,4.5,.88),color,.28,r)
    cube(name+'_cabin',(0,.10,1.55),(1.72,2.25,.85),color,.24,r)
    cube(name+'_windshield',(0,-.95,1.65),(1.48,.06,.59),'navy',.08,r)
    cube(name+'_rearwindow',(0,1.19,1.65),(1.45,.04,.55),'navy',.08,r)
    for dx in (-.87,.87):
        for dy in (-.45,.65):cube(name+'_window',(dx,dy,1.65),(.035,.86,.51),'navy',.035,r)
    for dx in (-1,1):
        for dy in (-1.40,1.35):
            w=cylinder(name+'_wheel',(dx,dy,.57),.46,.26,'black',r);w.rotation_euler.y=math.pi/2
            h=cylinder(name+'_rim',(dx*1.15,dy,.57),.23,.015,'metal',r);h.rotation_euler.y=math.pi/2
    for dx in (-.61,.61):
        cube(name+'_headlight',(dx,-2.24,1.0),(.5,.05,.22),'light',.03,r)
        cube(name+'_tail',(dx,2.24,1.0),(.5,.05,.18),'red',.03,r)
    cube(name+'_bumper',(0,-2.26,.57),(1.68,.08,.14),'charcoal',.03,r)
    return r

def key(o,t,path,value):
    setattr(o,path,value);o.keyframe_insert(data_path=path,frame=round(t*24)+1)
    if path=='hide_render':
        o.hide_viewport=value;o.keyframe_insert(data_path='hide_viewport',frame=round(t*24)+1)

def linear_all():
    for a in bpy.data.actions:
        for layer in a.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for fc in bag.fcurves:
                        for k in fc.keyframe_points:k.interpolation='LINEAR'

def descendants(o):
    return [o]+list(o.children_recursive)

def visibility(o,start,end):
    # Scale animation is supported by native Blender playback and glTF.
    scale=tuple(o.scale)
    hidden,first,last,after=visibility_frames(start,end)
    # Round once, then offset integer frames. Half-frame banker's rounding can
    # otherwise collapse the hidden key and the first visible key together.
    for frame,value in [(1,(0,0,0)),(hidden,(0,0,0)),(first,scale),(last,scale),(after,(0,0,0))]:
        o.scale=value;o.keyframe_insert(data_path='scale',frame=frame)
