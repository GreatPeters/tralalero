"""Deterministic Blender 4.4 source for manufactured mobile props."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/s22-polish-2026-10-01/props'
GAME=ROOT/'Assets/ShooterSurvival/Models/Generated/S22Polish/Props'
OUT.mkdir(parents=True,exist_ok=True);GAME.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
mats={}
def mat(name,color,metal=0):
 if name in mats:return mats[name]
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=.42
 vertex=m.node_tree.nodes.new('ShaderNodeVertexColor');vertex.layer_name='Color';m.node_tree.links.new(vertex.outputs['Color'],p.inputs['Base Color']);mats[name]=m;return m
steel=mat('galvanized steel',(.56,.64,.69),.6);dark=mat('recess',(.025,.045,.06));ivory=mat('painted ivory',(.82,.87,.84));blue=mat('enamel blue',(.025,.20,.36));black=mat('hole interior',(.012,.018,.026));stone=mat('broken rim',(.20,.23,.24));white=mat('reflector white',(.94,.94,.88));amber=mat('reflector amber',(.96,.46,.06));red=mat('coral drink',(.76,.14,.09));teal=mat('teal drink',(.04,.61,.60));yellow=mat('lemon drink',(.94,.71,.08))
objects=[]
def add_material(obj,m):obj.data.materials.append(m);objects.append(obj);return obj
def box(name,pos,size,m,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('manufactured edge','BEVEL');mod.width=bevel;mod.segments=2;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
  mod=o.modifiers.new('weighted corner normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=mod.name)
 return add_material(o,m)
def cylinder(name,pos,radius,depth,m,vertices=10,rotation=None):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=pos);o=bpy.context.object;o.name=name
 if rotation:o.rotation_euler=rotation
 return add_material(o,m)
def mesh(name,vertices,faces,m):
 data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update();o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);return add_material(o,m)
def finish(name):
 # Bake each region's color into vertex attributes before joining/exporting.
 for o in objects:
  attr=o.data.color_attributes.new(name='Color',type='BYTE_COLOR',domain='CORNER')
  for polygon in o.data.polygons:
   color=o.data.materials[polygon.material_index].diffuse_color
   for li in polygon.loop_indices:attr.data[li].color=color
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 # Retain materials in the editable source; Unity installer uses vertex colors and one material.
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
 bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),export_format='GLB',use_selection=True,export_apply=True)
 bpy.ops.export_scene.fbx(filepath=str(GAME/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False)
 o.data.calc_loop_triangles();result={'asset':name,'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'size':list(o.dimensions),'stage':'finished candidate; native review pending'}
 (OUT/(name+'-metrics.json')).write_text(json.dumps(result,indent=2),encoding='utf-8');bpy.ops.object.delete();objects.clear();return result

# Continuous folded steel W profile with real supporting posts.
profile=[(-.08,.68),(-.12,.73),(-.03,.81),(-.09,.90),(-.13,.99),(-.03,1.07),(-.065,1.12)]
verts=[(x,y,z) for x in (-3,3) for y,z in profile]+[(x,y+.026,z) for x in (-3,3) for y,z in profile]
n=len(profile);faces=[]
for i in range(n-1):faces.extend([(i,i+1,n+i+1,n+i),(2*n+i,3*n+i,3*n+i+1,2*n+i+1)])
for start in (0,n):
 for i in range(n-1):faces.append((start+i,start+i+2*n,start+i+1+2*n,start+i+1))
faces.extend([(0,n,3*n,2*n),(n-1,3*n-1,4*n-1,2*n-1)])
mesh('continuous W beam',verts,faces,steel)
for x in (-2.25,2.25):
 box('post web',(x,.04,.49),(.085,.12,.98),steel,.008);box('post flange',(x,.07,.48),(.18,.045,.96),steel,.005)
 box('support bracket',(x,-.005,.88),(.20,.20,.22),dark,.007)
 for z in (.78,1.03):cylinder('bolt',(x,-.139,z),.025,.018,steel,8,(math.pi/2,0,0))
 box('reflector plate',(x,-.153,1.00),(.115,.025,.16),white,.006);box('amber face',(x,-.170,1.00),(.066,.007,.115),amber,.002)
results=[finish('MobileGuardrail')]

# Dark opening, bevelled inner lip and irregular rim; no hidden dense surface.
count=48;verts=[]
for ring in range(4):
 for i in range(count):
  angle=2*math.pi*i/count;jitter=1+.035*math.sin(i*3.7)+.018*math.cos(i*1.3)
  radius=[.75,.82,.99,1.055][ring]*jitter;z=[-.07,-.025,.023,.002][ring]
  verts.append((math.cos(angle)*radius,math.sin(angle)*radius*.72,z))
faces=[]
for ring in range(3):
 for i in range(count):faces.append((ring*count+i,(ring+1)*count+i,(ring+1)*count+(i+1)%count,ring*count+(i+1)%count))
mesh('broken opening rim',verts,faces,stone);mesh('dark recess',[(0,0,-.074)]+verts[:count],[(0,i+1,(i+1)%count+1) for i in range(count)],black)
for i in range(9):
 a=i*2*math.pi/9+.1;chip=box('edge chip',(math.cos(a)*1.08,math.sin(a)*.78,.025),(.10,.09,.045),stone);chip.rotation_euler.z=a+.3
results.append(finish('MobileHole'))

# Cabinet has a framed recess, separate shelves, payment and retrieval opening.
box('cabinet',(0,.02,1.05),(1.18,.78,2.10),blue,.035)
box('back of display',(-.16,-.383,1.17),(.73,.027,1.30),dark,.01)
for x in (-.57,.25):box('display upright',(x,-.438,1.18),(.07,.12,1.39),ivory,.01)
for z in (.48,1.88):box('display crosspiece',(-.16,-.438,z),(.90,.12,.075),ivory,.008)
box('header',(0,-.422,1.985),(1.10,.085,.16),ivory,.015)
for row in range(3):
 z=.74+row*.36;box('shelf',(-.16,-.449,z-.09),(.73,.13,.026),steel)
 for col in range(3):
  x=-.42+col*.255;m=(red,teal,yellow)[(row+col)%3]
  cylinder('drink bottle',(x,-.43,z),.06,.20,m,10);cylinder('bottle cap',(x,-.43,z+.12),.032,.035,ivory,8)
box('payment panel',(.43,-.427,1.35),(.23,.10,.63),dark,.018)
box('price display',(.43,-.483,1.51),(.15,.012,.10),teal,.005)
box('coin slot',(.43,-.487,1.36),(.095,.012,.018),steel)
for z in (1.2,1.09):cylinder('selection button',(.43,-.49,z),.032,.017,amber,10,(math.pi/2,0,0))
box('retrieval recess',(-.10,-.415,.27),(.76,.065,.19),dark,.018)
box('retrieval ledge',(-.10,-.468,.165),(.80,.16,.04),ivory,.008)
for x in (-.46,.46):
 for y in (-.26,.27):cylinder('rubber foot',(x,y,-.022),.066,.045,dark,10)
results.append(finish('MobileVending'))
(OUT/'summary.json').write_text(json.dumps(results,indent=2),encoding='utf-8');print(json.dumps(results))
