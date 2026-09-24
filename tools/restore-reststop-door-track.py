"""Restore a continuous guide track from the generated frame's own end profile."""
import argparse,json,math,runpy,statistics,sys
from pathlib import Path
import bpy,bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser();parser.add_argument('--source',required=True);parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(args.source).resolve();out=Path(args.output).resolve()
assert not (out/'model.blend').exists(),'Choose a new correction revision'
out.mkdir(parents=True,exist_ok=True);bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];bpy.ops.object.select_all(action='DESELECT')
for obj in objects:obj.select_set(True)
bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();body=bpy.context.object
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
def bounds():return (Vector(tuple(min(v.co[i] for v in body.data.vertices) for i in range(3))),Vector(tuple(max(v.co[i] for v in body.data.vertices) for i in range(3))))
lo,hi=bounds();rotated=False
if hi.x-lo.x<hi.y-lo.y:
    body.rotation_mode='XYZ';body.rotation_euler.z=math.pi/2;bpy.context.view_layer.update()
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);rotated=True
lo,hi=bounds();offset=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for vertex in body.data.vertices:vertex.co-=offset
body.data.update()
lo,hi=bounds();dims=hi-lo;w=dims.x;h=dims.z;diagonal=dims.length
bpy.context.view_layer.update();tree=BVHTree.FromObject(body,bpy.context.evaluated_depsgraph_get())
xs=[lo.x+w*i/1024 for i in range(1025)];empty=[]
for x in xs:
    point,normal,index,distance=tree.ray_cast(Vector((x,hi.y+diagonal,lo.z+h*.45)),Vector((0,-1,0)))
    empty.append(point is None)
middle=min(range(len(xs)),key=lambda i:abs(xs[i]))
if not empty[middle]:
    point=tree.ray_cast(Vector((xs[middle],hi.y+diagonal,lo.z+h*.45)),Vector((0,-1,0)))[0]
    print(json.dumps({'diagnostic':'center ray unexpectedly hit','bounds':[list(lo),list(hi)],'matrix':[list(r) for r in body.matrix_world],'parent':body.parent.name if body.parent else None,'rotated':rotated,'hit':list(point) if point else None}),flush=True)
assert empty[middle],'The selected source must have an open center'
left=middle;right=middle
while left>0 and empty[left-1]:left-=1
while right<len(xs)-1 and empty[right+1]:right+=1
xleft=xs[left];xright=xs[right];assert xright-xleft>w*.55
probe=xleft+w*.035;zcut=lo.z+h*.065
jamb=[v.co for v in body.data.vertices if lo.z+h*.2<v.co.z<lo.z+h*.7 and (v.co.x<xleft or v.co.x>xright)]
ymin=min(v.y for v in jamb);ymax=max(v.y for v in jamb)
sample=[]
for i in range(257):
    y=ymin+(ymax-ymin)*i/256
    point,normal,index,distance=tree.ray_cast(Vector((probe,y,zcut+h*.01)),Vector((0,0,-1)))
    if point is not None and point.z<=zcut:sample.append((y,point.z))
assert len(sample)>8,'No usable original end-track profile'
zlo=min(z for y,z in sample);zhi=max(z for y,z in sample)
profile=[(y,lo.z+h*(.004+.004*(z-zlo)/max(zhi-zlo,1e-8))) for y,z in sample]
def simplify(points,tolerance):
    if len(points)<=2:return points
    a=Vector(points[0]);d=Vector(points[-1])-a
    errors=[abs(d.x*(Vector(p)-a).y-d.y*(Vector(p)-a).x)/max(d.length,1e-12) for p in points[1:-1]]
    error=max(errors)
    if error<=tolerance:return [points[0],points[-1]]
    i=errors.index(error)+1;return simplify(points[:i+1],tolerance)[:-1]+simplify(points[i:],tolerance)
knots=simplify(profile,h*.00008)
bm=bmesh.new();bm.from_mesh(body.data)
for axis,value in ((0,xleft),(0,xright),(2,zcut)):
    point=Vector((0,0,0));point[axis]=value;normal=Vector((0,0,0));normal[axis]=1
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=h*1e-7,plane_co=point,plane_no=normal,clear_inner=False,clear_outer=False)
faces=[f for f in bm.faces if xleft-1e-7<f.calc_center_median().x<xright+1e-7 and f.calc_center_median().z<zcut-1e-7]
removed=len(faces);assert removed;bmesh.ops.delete(bm,geom=faces,context='FACES')
for boundary in (xleft,xright):
    edges=[e for e in bm.edges if e.is_boundary and all(abs(v.co.x-boundary)<h*1e-5 and v.co.z<zcut+h*1e-5 for v in e.verts)]
    if edges:bmesh.ops.holes_fill(bm,edges=edges,sides=0)
loose=[v for v in bm.verts if not v.link_faces]
if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
bm.to_mesh(body.data);bm.free();body.data.update()
ring=knots+[(knots[-1][0],lo.z),(knots[0][0],lo.z)];n=len(ring)
verts=[(x,y,z) for x in (xleft-w*.004,xright+w*.004) for y,z in ring]
polys=[tuple(range(n)),tuple(reversed(range(n,2*n)))]+[(i,i+n,((i+1)%n)+n,(i+1)%n) for i in range(n)]
mesh=bpy.data.meshes.new('Generated_profile_guide_track');mesh.from_pydata(verts,[],polys);mesh.update()
rail=bpy.data.objects.new('Generated_profile_guide_track',mesh);bpy.context.collection.objects.link(rail)
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rail.select_set(True);bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
body.data.calc_loop_triangles();triangles=len(body.data.loop_triangles)
bpy.context.view_layer.update();check=BVHTree.FromObject(body,bpy.context.evaluated_depsgraph_get())
center_open=check.ray_cast(Vector((0,hi.y+diagonal,lo.z+h*.45)),Vector((0,-1,0)))[0] is None
rail_hits=[];ymid=(knots[0][0]+knots[-1][0])/2
for fraction in (.1,.3,.5,.7,.9):
    x=xleft+(xright-xleft)*fraction;p=check.ray_cast(Vector((x,ymid,zcut)),Vector((0,0,-1)))[0]
    rail_hits.append(p is not None and p.z<lo.z+h*.012)
assert center_open and all(rail_hits)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/'model.blend'))
bpy.ops.export_scene.gltf(filepath=str(out/'model.glb'),export_format='GLB',use_selection=True)
(out/'shape-repair.json').write_text(json.dumps({'source':str(source),'method':'retain generated jambs/header/sensor; remove malformed end fragments and sweep their measured profile into one thin continuous guide rail',
    'rotated_z_90_for_width_x':rotated,'opening_x':[xleft,xright],'profile_knots':knots,'source_profile_samples':len(sample),
    'removed_fragment_faces':removed,'triangles':triangles,'rail_height_fraction':.008,'center_open':center_open,'continuous_rail_samples':rail_hits},indent=2),encoding='utf8')
script=Path(__file__).with_name('reststop-production-review-render.py');sys.argv=[str(script),'--',str(out),'--shape'];runpy.run_path(str(script),run_name='__main__')
