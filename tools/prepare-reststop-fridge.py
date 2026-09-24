"""Keep the TRELLIS fridge exterior and fit a real cavity, door and five shelves."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import runpy
import shutil
import sys

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=Path(__file__).resolve().parents[1]
source,output=Path(args.source).resolve(),Path(args.output).resolve()
assert 'S08' in str(source) and output.is_relative_to(root/'outputs') and not output.exists()
output.mkdir(parents=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(objects)==1
body=objects[0];bpy.context.view_layer.objects.active=body;body.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
mesh=body.data
vertices=[v.co.copy() for v in mesh.vertices]
lo=Vector(tuple(min(p[i] for p in vertices) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in vertices) for i in range(3)))
dim=hi-lo;mid=(hi+lo)/2
tree=BVHTree.FromPolygons(vertices,[tuple(p.vertices) for p in mesh.polygons])
def front(x,z):return tree.ray_cast(Vector((x,lo.y-dim.length,z)),Vector((0,1,0)))
center_hit=front(mid.x,mid.z);assert center_hit[0] is not None
pane_y=center_hit[0].y
def panel_run(axis):
    flags=[];samples=[]
    for i in range(201):
        value=lo[axis]+dim[axis]*i/200
        hit=front(value,mid.z) if axis==0 else front(mid.x,value)
        good=hit[0] is not None and abs(hit[0].y-pane_y)<dim.y*.006 and abs(hit[1].y)>.9
        flags.append(good);samples.append(value)
    assert flags[100]
    a=b=100
    while a>0 and flags[a-1]:a-=1
    while b<200 and flags[b+1]:b+=1
    return samples[a],samples[b]
x0,x1=panel_run(0);z0,z1=panel_run(2)
x0+=dim.x*.006;x1-=dim.x*.006;z0+=dim.z*.004;z1-=dim.z*.004
assert .4<(x1-x0)/dim.x<.95 and .5<(z1-z0)/dim.z<.85
fit={'bounds_min':list(lo),'bounds_max':list(hi),'pane_y':pane_y,
     'opening_x':[x0,x1],'opening_z':[z0,z1],'method':'Central front depth and contiguous planar runs with conservative inset'}
(output/'frame-fit.json').write_text(json.dumps(fit,indent=2),encoding='utf8')
before=len(mesh.polygons)
bm=bmesh.new();bm.from_mesh(mesh)
for axis,value in ((0,x0),(0,x1),(2,z0),(2,z1)):
    normal=Vector((0,0,0));normal[axis]=1;point=Vector((0,0,0));point[axis]=value
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
        plane_co=point,plane_no=normal,dist=dim.length*1e-8)
remove=[f for f in bm.faces if x0<f.calc_center_median().x<x1 and z0<f.calc_center_median().z<z1
        and f.calc_center_median().y<pane_y+dim.y*.035]
assert remove
bmesh.ops.delete(bm,geom=remove,context='FACES_ONLY')
wire=[e for e in bm.edges if not e.link_faces]
if wire:bmesh.ops.delete(bm,geom=wire,context='EDGES')
loose=[v for v in bm.verts if not v.link_edges]
if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
bmesh.ops.triangulate(bm,faces=list(bm.faces))
bad=[f for f in bm.faces if f.calc_area()<dim.length**2*1e-14]
if bad:bmesh.ops.delete(bm,geom=bad,context='FACES_ONLY')
wire=[e for e in bm.edges if not e.link_faces]
if wire:bmesh.ops.delete(bm,geom=wire,context='EDGES')
loose=[v for v in bm.verts if not v.link_edges]
if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
bm.normal_update();bm.to_mesh(mesh);bm.free();mesh.update();mesh.validate()
mesh.normals_split_custom_set([(0.,0.,0.)]*len(mesh.loops))
for p in mesh.polygons:p.use_smooth=True
mesh.set_sharp_from_angle(angle=math.radians(50))
body.name='Fridge_TRELLIS_body'
bpy.ops.wm.save_as_mainfile(filepath=str(output/'model.blend'))
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'),use_selection=True,export_format='GLB')
shutil.copy2(source,output/'original-shape.glb')

def material(name,color,metallic,roughness,alpha=1.):
    mat=bpy.data.materials.new(name);mat.use_nodes=True;mat.use_backface_culling=False
    shader=mat.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value=(*color,1)
    shader.inputs['Metallic'].default_value=metallic;shader.inputs['Roughness'].default_value=roughness
    shader.inputs['Alpha'].default_value=alpha;mat.diffuse_color=(*color,alpha)
    if alpha<1:mat.surface_render_method='DITHERED'
    return mat
glass=material('Fridge clear door',(.76,.84,.90),0,.12,.06)
steel=material('Fridge stainless interior',(.42,.48,.55),.72,.29)
light=material('Fridge warm internal strip',(1,.83,.54),0,.3)
shader=light.node_tree.nodes['Principled BSDF'];shader.inputs['Emission Color'].default_value=(1,.73,.35,1);shader.inputs['Emission Strength'].default_value=2
fixed=[]
def make(name,points,faces,mat):
    data=bpy.data.meshes.new(name);data.from_pydata(points,[],faces);data.update();assert not data.validate()
    data.materials.append(mat);uv=data.uv_layers.new(name='UVMap')
    for p in data.polygons:
        for i in p.loop_indices:
            v=data.vertices[data.loops[i].vertex_index].co
            uv.data[i].uv=((v.x-lo.x)/dim.x,(v.y-lo.y)/dim.y)
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);fixed.append(obj);return obj
def box(name,a,b,mat):
    points=[(x,y,z) for z in (a[2],b[2]) for y in (a[1],b[1]) for x in (a[0],b[0])]
    return make(name,points,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],mat)

door_y=pane_y+dim.y*.006
make('Fridge fitted door glazing',[(x0,door_y,z0),(x1,door_y,z0),(x1,door_y,z1),(x0,door_y,z1)],[(0,1,2,3)],glass)
ix0,ix1=x0-dim.x*.025,x1+dim.x*.025
iy0,iy1=door_y+dim.y*.035,hi.y-dim.y*.09
iz0,iz1=z0+dim.z*.009,z1-dim.z*.009
assert lo.x<ix0<ix1<hi.x and iy0<iy1
points=[(ix0,iy0,iz0),(ix1,iy0,iz0),(ix1,iy1,iz0),(ix0,iy1,iz0),
        (ix0,iy0,iz1),(ix1,iy0,iz1),(ix1,iy1,iz1),(ix0,iy1,iz1)]
make('Fridge fitted interior liner',points,[(0,1,2,3),(4,7,6,5),(3,2,6,7),(0,3,7,4),(1,5,6,2)],steel)
shelf_heights=[];shelf_names=[]
sx0,sx1=ix0+dim.x*.018,ix1-dim.x*.018
sy0,sy1=iy0+dim.y*.025,iy1-dim.y*.018
for number,fraction in enumerate((.09,.27,.45,.63,.81),1):
    z=iz0+(iz1-iz0)*fraction;shelf_heights.append(z)
    points=[];faces=[];columns,rows=8,5
    dx,dy=(sx1-sx0)/columns,(sy1-sy0)/rows
    for row in range(rows):
        for col in range(columns):
            a,b=sx0+col*dx,sy0+row*dy;cx,cy=a+dx/2,b+dy/2
            radius=min(dx,dy)*.17;base=len(points)
            points.extend(((a,b,z),(a+dx,b,z),(a+dx,b+dy,z),(a,b+dy,z)))
            points.extend((cx+radius*math.cos(i*math.pi/4),cy+radius*math.sin(i*math.pi/4),z) for i in range(8))
            for face in ((0,1,11,10,9),(1,2,5,4,11),(2,3,7,6,5),(3,0,9,8,7)):
                faces.append(tuple(base+i for i in face))
    name='Fridge perforated shelf '+str(number);shelf_names.append(name)
    make(name,points,faces,steel)
    thick=dim.z*.004;border=dim.x*.008
    box(name+' front rim',(sx0,sy0-border,z-thick),(sx1,sy0,z+thick*.3),steel)
    box(name+' rear rim',(sx0,sy1,z-thick),(sx1,sy1+border,z+thick*.3),steel)
    for x in (sx0,sx1-border):
        box(name+' side rim',(x,sy0,z-thick),(x+border,sy1,z),steel)
box('Fridge decorative light strip',(x1-dim.x*.035,iy0+dim.y*.01,iz0+(iz1-iz0)*.04),
    (x1-dim.x*.02,iy0+dim.y*.018,iz1-(iz1-iz0)*.04),light)
bpy.ops.object.select_all(action='DESELECT')
for obj in fixed:obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(output/'fixed-parts.glb'),use_selection=True,export_format='GLB')
fixed_triangles=sum(len(p.vertices)-2 for obj in fixed for p in obj.data.polygons)
assert fixed_triangles<3500
scope={'texture_scope':'opaque body only','reference_is_full_asset':True,'preserved_parts':['fixed-parts.glb'],
       'body_sha256':hashlib.sha256((output/'model.glb').read_bytes()).hexdigest(),
       'fixed_triangles':fixed_triangles,'shelf_heights':shelf_heights,'shelf_names':shelf_names,
       'holes_per_shelf':40,'pane_alpha':.06,'interior_bounds':[[ix0,iy0,iz0],[ix1,iy1,iz1]],
       'source_bounds':[list(lo),list(hi)],'front_opening':fit}
(output/'texture-scope.json').write_text(json.dumps(scope,indent=2),encoding='utf8')
(output/'repair.json').write_text(json.dumps({'source':str(source),'method':'Keep the TRELLIS exterior; open only the measured central front panel and fit a clear door, interior liner and five perforated shelves from the reference',
    'source_faces':before,'removed_front_faces':len(remove),'body_triangles':sum(len(p.vertices)-2 for p in mesh.polygons),
    'fixed_triangles':fixed_triangles,'extra_ai_requests':0,'prototype_only':True},indent=2),encoding='utf8')
prototype=output/'prototype';prototype.mkdir()
body.data.materials.clear();body.data.materials.append(material('Prototype dark cabinet',(.035,.04,.045),.4,.4))
for p in body.data.polygons:p.material_index=0
bpy.ops.object.select_all(action='DESELECT')
for obj in [body]+fixed:obj.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.export_scene.gltf(filepath=str(prototype/'model.glb'),use_selection=True,export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(prototype/'model.blend'))
script=root/'tools/reststop-production-review-render.py';sys.argv=[str(script),'--',str(prototype)]
runpy.run_path(str(script),run_name='__main__')
print(json.dumps(scope),flush=True)
