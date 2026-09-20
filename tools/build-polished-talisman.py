"""Blender 4.4: reference-shaped folded talisman and raised upgrade emblems."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/talisman-polish-2026-09-20'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def mat(name,color,metal=0,rough=.4):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    return m
paper=mat('PaperIvory',(.92,.825,.63),0,.65)
edge=mat('PaperEdge',(.65,.43,.21),0,.72)
ink=mat('NavyPrint',(.018,.050,.103),0,.48)
enamel=mat('NavyEnamel',(.018,.056,.14),.38,.24)
silver=mat('SilverRivet',(.74,.77,.79),.8,.22)
gold=mat('GoldFace',(1,.56,.055),.55,.23)
bronze=mat('GoldEdge',(.56,.22,.015),.7,.27)
teal=mat('TealFace',(.012,.76,.57),.28,.18)
teal_edge=mat('TealEdge',(.005,.23,.19),.4,.25)

def finish(o, bevel=.015, segments=3, smooth=True):
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    if bevel:
        m=o.modifiers.new('Crafted bevel','BEVEL');m.width=bevel;m.segments=segments
        bpy.ops.object.modifier_apply(modifier=m.name)
    for p in o.data.polygons:p.use_smooth=smooth
    n=o.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL');n.keep_sharp=True;n.weight=40
    bpy.ops.object.modifier_apply(modifier=n.name)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
    o.select_set(False);return o

def solid(name,points,depth,material,bevel=.018,front=-.04):
    n=len(points);vs=[(x,front,z) for x,z in points]+[(x,front+depth,z) for x,z in points]
    fs=[tuple(reversed(range(n))),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],fs);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.data.materials.append(material)
    # Correct the closed shell independent of 2D winding.
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
    return finish(o,bevel)

def box(name,loc,scale,material,bevel=.04):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
    return finish(o,bevel,4)

def sphere(name,loc,scale,material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
    for p in o.data.polygons:p.use_smooth=True
    o.select_set(False);return o

def line(name,points,material,width=.007):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=width;curve.bevel_resolution=2;curve.resolution_u=1
    s=curve.splines.new('POLY');s.points.add(len(points)-1)
    for p,co in zip(s.points,points):p.co=(*co,1)
    o=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(o);o.data.materials.append(material)
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.select_set(False);return o

def join(name,objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.select_set(False);return o

body=[]
outline=[(-.48,-1.12),(-.09,-1.10),(-.075,-.87),(.075,-.87),(.09,-1.10),(.48,-1.12),(.53,.98),(-.53,.98)]
main=solid('CenterPaper',outline,.065,paper,.025);body.append(main)
# The printed border is broken into deliberate corner motifs, not a stock rectangular frame.
for sign in [-1,1]:
    pts=[(sign*.30,-.08,.83),(sign*.43,-.08,.89),(sign*.425,-.08,.61),(sign*.37,-.08,.68)]
    body.append(line('CornerPrint',pts,ink,.012))
    pts=[(sign*.421,-.078,.76),(sign*.391,-.078,-.75),(sign*.22,-.078,-.84)]
    body.append(line('LongPrint',pts,ink,.010))
body.append(line('FootDiamond',[(0,-.082,-.80),(.075,-.082,-.72),(0,-.082,-.64),(-.075,-.082,-.72),(0,-.082,-.80)],ink,.010))
main=join('CenterPaper',body);body=[main]
for side,sign in [('Left',-1),('Right',1)]:
    pts=[(0,-1.02),(sign*.39,-.86),(sign*.43,.84),(0,1.01)]
    w=solid(side+'Paper',pts,.048,paper,.022,front=.02)
    details=[w]
    details.append(line(side+'Border',[(sign*.06,-.01,.83),(sign*.34,-.01,.70),(sign*.32,-.01,-.73),(sign*.07,-.01,-.86)],ink,.009))
    details.append(line(side+'Corner',[(sign*.18,-.012,.70),(sign*.32,-.012,.69),(sign*.30,-.012,.52)],ink,.012))
    w=join(side+'Paper',details);w.location.x=sign*.49
    # Keep the source mesh separated at its real fold pivot; Unity animates this same hinge.
    body.append(w)

# Rounded folded metal clip, with visible top bend and a separate domed rivet.
clip=box('ClipFront',(0,-.145,1.03),(.31,.12,.48),enamel,.055)
bridge=box('ClipBend',(0,-.025,1.26),(.31,.31,.16),enamel,.06)
back=box('ClipBack',(0,.115,1.14),(.31,.09,.30),enamel,.04)
rivet=sphere('Rivet',(0,-.223,.99),(.072,.036,.072),silver)
body.extend([clip,bridge,back,rivet])

icons={}
blade=solid('Blade',[(-.14,-.22),(.14,-.22),(.18,.50),(0,.79),(-.18,.50)],.14,gold,.045,front=-.10)
guard=box('CrossGuard',(0,-.06,-.26),(.62,.18,.14),gold,.06)
grip=box('SwordGrip',(0,-.04,-.49),(.16,.16,.34),bronze,.06)
pommel=sphere('Pommel',(0,-.06,-.70),(.12,.10,.12),gold)
sword=join('Icon_Attack',[blade,guard,grip,pommel]);sword.rotation_euler.y=math.radians(38)
bpy.context.view_layer.objects.active=sword;sword.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=False);sword.select_set(False);icons['WallBonus_Attack']=sword

heart_points=[]
for i in range(64):
    t=2*math.pi*i/64
    heart_points.append((16*math.sin(t)**3/18,(13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t))/18+.13))
rings=[(0,-.03),(.6,-.07),(.95,-.08),(1,-.13),(.92,-.22),(.65,-.31),(.25,-.35),(0,-.36)]
vs=[];fs=[]
for factor,y in rings:
    vs.extend([(x*factor,y,z*factor) for x,z in heart_points])
for r in range(len(rings)-1):
    for i in range(64):fs.append((r*64+i,r*64+(i+1)%64,(r+1)*64+(i+1)%64,(r+1)*64+i))
mesh=bpy.data.meshes.new('Heart');mesh.from_pydata(vs,[],fs);mesh.update();heart=bpy.data.objects.new('Icon_Health',mesh);bpy.context.collection.objects.link(heart);heart.data.materials.append(teal);heart.data.materials.append(teal_edge)
for p in heart.data.polygons:p.use_smooth=True;p.material_index=1 if p.index<64*3 else 0
# Merge pole rings to avoid zero-area end faces.
bpy.context.view_layer.objects.active=heart;heart.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00001);bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT');heart.select_set(False);icons['WallBonus_Health']=heart

parts=[]
for i in range(3):
    x=(i-1)*.32;z=(1-abs(i-1))*.16
    parts.append(box('Cartridge',(x,0,z),(.21,.18,.76),gold,.065))
    parts.append(box('BaseBand',(x,-.01,z-.23),(.24,.20,.12),bronze,.03))
icons['WallBonus_AttackSpeed']=join('Icon_AttackSpeed',parts)
arrow=solid('RangeArrow',[(-.62,-.10),(.05,-.10),(.05,-.32),(.65,0),(.05,.32),(.05,.10),(-.62,.10)],.17,gold,.038,front=-.12)
mark=solid('RangeMark',[(-.7,-.28),(-.51,-.28),(-.28,0),(-.51,.28),(-.7,.28),(-.47,0)],.14,bronze,.025,front=-.04)
icons['WallBonus_MissileDuration']=join('Icon_MissileDuration',[arrow,mark])
parts=[]
for x,z in [(-.31,-.18),(.0,.28),(.31,-.18)]:parts.append(sphere('Shot',(x,-.01,z),(.23,.15,.23),gold))
parts.extend([box('PlusH',(.43,-.20,.43),(.39,.1,.11),gold,.025),box('PlusV',(.43,-.20,.43),(.11,.1,.39),gold,.025)])
icons['WallBonus_MissileAdd']=join('Icon_MissileAdd',parts)

# Export every icon separately at origin. Real meshes, not baked screenshots.
for key,o in icons.items():
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False,mesh_smooth_type='FACE')
    bpy.ops.export_scene.gltf(filepath=str(OUT/(key+'.glb')),export_format='GLB',use_selection=True)
    o.hide_render=True;o.hide_viewport=True;o.select_set(False)

bpy.ops.object.select_all(action='DESELECT')
for o in body:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'TalismanBody.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False,mesh_smooth_type='FACE')

# Studio source holds a real default emblem and the closed fold pose.
for o in body:
    if o.name=='LeftPaper':o.rotation_euler.z=math.radians(-58)
    elif o.name=='RightPaper':o.rotation_euler.z=math.radians(58)
hero=sword.copy();hero.data=sword.data.copy();hero.name='DefaultEmblem';bpy.context.collection.objects.link(hero);hero.hide_render=False;hero.hide_viewport=False;hero.scale=(.72,)*3;hero.location=(0,-.20,-.01)
for o in body:o.select_set(True)
hero.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'Talisman.glb'),export_format='GLB',use_selection=True)

scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=900;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world.color=(.3,.3,.3)
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
def area(name,loc,power,size):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.1))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key',(-3,-5,5),650,4);area('Fill',(4,-2,2),400,3);area('Rim',(0,3,3),550,3)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-1.35));floor=bpy.context.object;floor.name='StudioGround';floor.data.materials.append(mat('Studio',(.66,.69,.72),0,.9))
cameraData=bpy.data.cameras.new('ReferenceCamera');cam=bpy.data.objects.new('ReferenceCamera',cameraData);bpy.context.collection.objects.link(cam);scene.camera=cam;cam.location=(2.5,-7.5,2.6);cam.rotation_euler=(Vector((0,0,.08))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=3.35
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Talisman.blend'))
for name,pos in [('hero',(2.5,-7.5,2.6)),('front',(0,-8,.1)),('side',(7,-.1,1.4)),('back',(0,8,.3)),('top',(0,-.1,8))]:
    cam.location=pos;cam.rotation_euler=(Vector((0,0,.08))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
metrics={}
for key,objects in [('talisman',body+[hero])]+[(k,[v]) for k,v in icons.items()]:
    tris=0
    for o in objects:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
    metrics[key]={'triangles':tris,'meshes':len(objects)}
(OUT/'source-metrics.json').write_text(json.dumps(metrics,indent=2))
print(json.dumps(metrics))
