"""Retopologize only the front slat field from the retained TRELLIS surface profile."""
import argparse,json,math,runpy,statistics,sys
from pathlib import Path
import bpy,bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser();parser.add_argument('--source-blend',required=True);parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(args.source_blend).resolve();out=Path(args.output).resolve()
assert not (out/'model.blend').exists(),'Preserve prior candidates'
out.mkdir(parents=True,exist_ok=True)
app=next(p for p in (Path.home()/'Desktop').glob('AI */Trellis */*') if (p/'blender_refine.py').is_file())
lib=runpy.run_path(str(app/'blender_refine.py'),run_name='reststop_refine_library')
bpy.ops.wm.open_mainfile(filepath=str(source));high=bpy.data.objects['Detailed_Source'];low=bpy.data.objects['Asset_Optimized']
for obj in list(bpy.context.scene.objects):
    if obj not in (high,low):bpy.data.objects.remove(obj,do_unlink=True)
high.hide_set(False);high.hide_render=False
lo,hi=lib['bounds'](high);dims=hi-lo;height=dims.z;diagonal=dims.length
xmin=lo.x+dims.x*.055;xmax=hi.x-dims.x*.055;zmin=lo.z+height*.035;zmax=hi.z-height*.035
zsample=lo.z+height*.65
tree=BVHTree.FromObject(high,bpy.context.evaluated_depsgraph_get())
raw=[]
for i in range(1025):
    x=xmin+(xmax-xmin)*i/1024
    point,normal,index,distance=tree.ray_cast(Vector((x,hi.y+diagonal,zsample)),Vector((0,-1,0)))
    assert point is not None,'Reference profile has an uncovered ray'
    raw.append((x,point.y))
baseline=statistics.median(y for x,y in raw)
profile=[(x,max(baseline-height*.008,min(baseline+height*.002,y))) for x,y in raw]
clamped=sum(abs(a[1]-b[1])>1e-9 for a,b in zip(raw,profile))
def simplify(points,tolerance):
    if len(points)<=2:return points
    a=Vector(points[0]);b=Vector(points[-1]);delta=b-a
    distances=[abs(delta.x*(Vector(p)-a).y-delta.y*(Vector(p)-a).x)/max(delta.length,1e-12) for p in points[1:-1]]
    largest=max(distances)
    if largest<=tolerance:return [points[0],points[-1]]
    index=distances.index(largest)+1
    return simplify(points[:index+1],tolerance)[:-1]+simplify(points[index:],tolerance)
tolerance=height*.00010;knots=simplify(profile,tolerance)
back_y=min(y for x,y in knots)-height*.002
original_triangles=lib['tris'](low);original_bounds=lib['bounds'](low)
low.data=low.data.copy();bm=bmesh.new();bm.from_mesh(low.data)
for axis,value in ((0,xmin),(0,xmax),(2,zmin),(2,zmax)):
    point=Vector((0,0,0));point[axis]=value;normal=Vector((0,0,0));normal[axis]=1
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=height*1e-7,
        plane_co=point,plane_no=normal,clear_inner=False,clear_outer=False)
bm.normal_update();to_remove=[]
for face in bm.faces:
    c=face.calc_center_median()
    if xmin-1e-7<c.x<xmax+1e-7 and zmin-1e-7<c.z<zmax+1e-7 and c.y>back_y-height*.002 and face.normal.y>.15:
        to_remove.append(face)
assert to_remove,'No original front surface selected'
removed=len(to_remove);bmesh.ops.delete(bm,geom=to_remove,context='FACES')
loose=[v for v in bm.verts if not v.link_faces]
if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
bm.to_mesh(low.data);bm.free();low.data.update()
ring=knots+[(xmax,back_y),(xmin,back_y)];n=len(ring)
verts=[(x,y,z) for z in (zmin,zmax) for x,y in ring]
faces=[tuple(range(n)),tuple(reversed(range(n,2*n)))]
faces += [(i,i+n,((i+1)%n)+n,(i+1)%n) for i in range(n)]
mesh=bpy.data.meshes.new('Source_profile_slat_field');mesh.from_pydata(verts,[],faces);mesh.update()
patch=bpy.data.objects.new('Source_profile_slat_field',mesh);bpy.context.collection.objects.link(patch)
lib['active'](low);patch.select_set(True);bpy.ops.object.join()
lib['clean'](low,diagonal*3e-7,merge=True)
for face in low.data.polygons:face.use_smooth=True
low.data.set_sharp_from_angle(angle=math.radians(50))
lib['active'](low);modifier=low.modifiers.new('Rigid_retopped_normals','WEIGHTED_NORMAL')
modifier.mode='FACE_AREA_WITH_ANGLE';modifier.keep_sharp=True;modifier.weight=60
bpy.ops.object.modifier_apply(modifier=modifier.name)
low.data.calc_loop_triangles();after=len(low.data.loop_triangles)
assert after<=15000,('Retopology budget exceeded',after)
for layer in list(low.data.uv_layers):low.data.uv_layers.remove(layer)
low.data.uv_layers.new(name='UVMap');bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.view_layer.update();test_tree=BVHTree.FromObject(low,bpy.context.evaluated_depsgraph_get());errors=[]
for x,y in profile[2:-2]:
    point,normal,index,distance=test_tree.ray_cast(Vector((x,hi.y+diagonal,zsample)),Vector((0,-1,0)))
    assert point is not None
    errors.append(abs(point.y-y))
maximum=max(errors);assert maximum<height*.0005,('Profile is occluded or deviates',maximum)
lib['bake'](high,low,out,2048,diagonal)
lib['active'](low);bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/'model.blend'))
bpy.ops.export_scene.fbx(filepath=str(out/'model.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,add_leaf_bones=False)
bpy.ops.export_scene.gltf(filepath=str(out/'model.glb'),use_selection=True,export_format='GLB')
(out/'repair.json').write_text(json.dumps({'source':str(source),'method':'front-only retopology extruded from a measured retained TRELLIS high-density cross section; frame/rear source geometry retained; fresh 2048 PBR bake',
    'triangles_before':original_triangles,'triangles_after':after,'removed_front_faces':removed,'profile_knots':len(knots),'profile_samples':len(profile),'profile_outliers_clamped':clamped,
    'profile_max_error':maximum,'profile_error_limit':height*.0005,'profile_height_fraction':.65,'field_bounds':[xmin,xmax,zmin,zmax],
    'profile':knots,'original_bounds':[list(v) for v in original_bounds],'final_bounds':[list(v) for v in lib['bounds'](low)]},indent=2),encoding='utf8')
script=Path(__file__).with_name('reststop-production-review-render.py');sys.argv=[str(script),'--',str(out)];runpy.run_path(str(script),run_name='__main__')
import shutil
shutil.copy2(out/'quality/hero.png',out/'preview.png')
