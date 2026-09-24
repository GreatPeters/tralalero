"""Read-only cross-section evidence for a generated slatted panel."""
import argparse,json,sys
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser();parser.add_argument('--source',required=True);parser.add_argument('--output',required=True)
parser.add_argument('--ray-axis',choices=('X','Y'),default='Y')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(args.source).resolve();output=Path(args.output).resolve()
bpy.ops.wm.read_factory_settings(use_empty=True)
if source.suffix.lower()=='.fbx':bpy.ops.import_scene.fbx(filepath=str(source))
else:bpy.ops.import_scene.gltf(filepath=str(source))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];vertices=[];triangles=[]
for obj in meshes:
    obj.data.calc_loop_triangles();offset=len(vertices)
    vertices.extend(obj.matrix_world@v.co for v in obj.data.vertices)
    triangles.extend(tuple(i+offset for i in t.vertices) for t in obj.data.loop_triangles)
lo=Vector(tuple(min(v[i] for v in vertices) for i in range(3)));hi=Vector(tuple(max(v[i] for v in vertices) for i in range(3)))
tree=BVHTree.FromPolygons(vertices,triangles,all_triangles=True);profiles=[]
ray_axis='XY'.index(args.ray_axis);scan_axis=1-ray_axis
for fraction in (.15,.25,.35,.45,.55,.65,.75,.85):
    z=lo.z+(hi.z-lo.z)*fraction
    for side in (-1,1):
        points=[]
        for i in range(513):
            x=lo[scan_axis]+(hi[scan_axis]-lo[scan_axis])*(.075+.85*i/512)
            start=Vector((0,0,z));start[scan_axis]=x;start[ray_axis]=lo[ray_axis]-1 if side==-1 else hi[ray_axis]+1
            direction=Vector((0,0,0));direction[ray_axis]=-side
            point,normal,index,distance=tree.ray_cast(start,direction)
            points.append([x,point[ray_axis] if point else None])
        values=[v for x,v in points if v is not None]
        profiles.append({'height_fraction':fraction,'side':side,'points':points,'depth_range':max(values)-min(values) if values else None})
data={'source':str(source),'ray_axis':args.ray_axis,'bounds_min':list(lo),'bounds_max':list(hi),'dimensions':list(hi-lo),'triangles':len(triangles),'profiles':profiles}
output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(data,indent=2),encoding='utf8')
print(json.dumps({'dimensions':data['dimensions'],'triangles':data['triangles'],'profiles':[{k:v for k,v in p.items() if k!='points'} for p in profiles]}),flush=True)
