"""Rejected donor-fit experiment retained as evidence; use rig-reststop-proxy.py."""
import argparse,json,math,runpy,sys
from pathlib import Path
from collections import defaultdict
import bpy
from mathutils import Vector,Matrix,Quaternion
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True)
parser.add_argument('--donor',required=True)
parser.add_argument('--name',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
out=Path(args.output).resolve();out.mkdir(parents=True,exist_ok=True)
assert not (out/(args.name+'.blend')).exists(), 'Preserve the existing revision'
bpy.ops.wm.open_mainfile(filepath=str(Path(args.donor).resolve()))
oldrig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
oldrig.data.pose_position='REST';bpy.context.view_layer.update()
donor=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.endswith('_Body'))
old_vertices=[donor.matrix_world@v.co for v in donor.data.vertices]
old_lo=Vector(tuple(min(v[i] for v in old_vertices) for i in range(3)))
old_hi=Vector(tuple(max(v[i] for v in old_vertices) for i in range(3)))
donor.data.calc_loop_triangles()
triangles=[tuple(t.vertices) for t in donor.data.loop_triangles]
old_weights=[{donor.vertex_groups[g.group].name:g.weight for g in v.groups if donor.vertex_groups[g.group].name in oldrig.data.bones} for v in donor.data.vertices]
bone_data={b.name:{'head':oldrig.matrix_world@b.head_local,'tail':oldrig.matrix_world@b.tail_local,
                         'up':(oldrig.matrix_world@b.matrix_local).to_3x3()@Vector((0,0,1)),
                         'parent':b.parent.name if b.parent else None} for b in oldrig.data.bones}
old_objects=set(bpy.context.scene.objects)
bpy.ops.import_scene.gltf(filepath=str(Path(args.source).resolve()))
new_meshes=[o for o in bpy.context.scene.objects if o not in old_objects and o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for obj in new_meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=new_meshes[0];bpy.ops.object.join();body=bpy.context.object
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);body.name=args.name+'_Body'
height=2.85 if args.name=='Traveler' else 3.05
points=[v.co for v in body.data.vertices]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
scale=height/(hi.z-lo.z);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for v in body.data.vertices:v.co=(v.co-center)*scale
points=[v.co for v in body.data.vertices]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
axis_scale=Vector(tuple((hi[i]-lo[i])/(old_hi[i]-old_lo[i]) for i in range(3)))
def mapped(v):return Vector(tuple(lo[i]+(v[i]-old_lo[i])*axis_scale[i] for i in range(3)))
mapped_vertices=[mapped(v) for v in old_vertices]
tree=BVHTree.FromPolygons(mapped_vertices,triangles,all_triangles=True)
armature=bpy.data.armatures.new(args.name+'_Skeleton');rig=bpy.data.objects.new('Rig',armature)
bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
specs={}
for name,data in bone_data.items():
    bone=armature.edit_bones.new(name);bone.head=mapped(data['head']);bone.tail=mapped(data['tail'])
    bone.align_roll(Vector(tuple(data['up'][i]*axis_scale[i] for i in range(3))))
    if data['parent']:bone.parent=armature.edit_bones[data['parent']]
    specs[name]=(tuple(bone.head/height),tuple(bone.tail/height),data['parent'])
bpy.ops.object.mode_set(mode='OBJECT')
groups={n:body.vertex_groups.new(name=n) for n in bone_data if n!='Root'}
cache={};distances=[]
for vertex in body.data.vertices:
    key=tuple(round(v,5) for v in vertex.co)
    if key not in cache:
        nearest,normal,face,distance=tree.find_nearest(vertex.co)
        assert face is not None
        ids=triangles[face];a,b,c=[mapped_vertices[i] for i in ids]
        bary=barycentric_transform(nearest,a,b,c,Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
        weights=defaultdict(float)
        for index,amount in zip(ids,bary):
            for name,w in old_weights[index].items():
                if name in groups:weights[name]+=max(0,amount)*w
        chosen=sorted(weights,key=weights.get,reverse=True)[:4];total=sum(weights[n] for n in chosen)
        assert total>0
        cache[key]={n:weights[n]/total for n in chosen if weights[n]/total>.00001}
        distances.append(distance)
    for name,w in cache[key].items():groups[name].add([vertex.index],w,'REPLACE')
body.parent=rig;body.modifiers.new('Transferred_Deformation','ARMATURE').object=rig
for obj in list(bpy.context.scene.objects):
    if obj not in (body,rig):bpy.data.objects.remove(obj,do_unlink=True)
rig.data.pose_position='POSE'
for action in list(bpy.data.actions):bpy.data.actions.remove(action)
errors=[abs(sum(g.weight for g in v.groups)-1) for v in body.data.vertices]
assert max(errors)<.002
(out/'skin-transfer.json').write_text(json.dumps({'donor':args.donor,'source':args.source,'method':'nearest source triangle with barycentric weights; identical UV seam positions share weights','max_surface_distance':max(distances),'average_surface_distance':sum(distances)/len(distances),'max_weight_error':max(errors),'axis_scale':list(axis_scale)},indent=2),encoding='utf8')
compact=False
def physical(p):return p
runpy.run_path(str(Path(__file__).with_name('reststop-rig-actions.py')),init_globals=globals(),run_name='__main__')
