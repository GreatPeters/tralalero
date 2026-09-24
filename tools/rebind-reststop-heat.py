"""Rejected H01-r6 proxy experiment with unfitted donor bones; retained as evidence."""
import json,math
from pathlib import Path
import bpy,bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'outputs/reststop-production-2026-09-24/rigged/H01-r4/ParkingMarshal.blend'
OUT=ROOT/'outputs/reststop-production-2026-09-24/rigged/H01-r6'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene
rig=next(o for o in scene.objects if o.type=='ARMATURE')
body=next(o for o in scene.objects if o.type=='MESH' and o.find_armature()==rig)
height=3.05;center=rig.data.bones['Hips'].head_local
def diagnostic():
    values=[]
    for v in body.data.vertices:
        if abs(v.co.x-center.x)<.11*height and .37*height<v.co.z<.51*height:
            arm=sum(g.weight for g in v.groups if body.vertex_groups[g.group].name.startswith(('UpperArm','Forearm','Hand')))
            values.append(arm)
    return {'central_torso_vertices':len(values),'arm_weight_max':max(values,default=0),'arm_weight_mean':sum(values)/max(1,len(values)),'arm_weight_gt_20pct':sum(v>.2 for v in values)}
before=diagnostic()
rig.data.pose_position='REST';bpy.context.view_layer.update()
proxy=body.copy();proxy.data=body.data.copy();bpy.context.collection.objects.link(proxy)
proxy.name='WEIGHT_PROXY';proxy.parent=None;proxy.modifiers.clear();proxy.vertex_groups.clear()
bpy.ops.object.select_all(action='DESELECT');proxy.select_set(True);bpy.context.view_layer.objects.active=proxy
proxy.data.remesh_voxel_size=.025;bpy.ops.object.voxel_remesh()
rig.data.bones['Root'].use_deform=False
bpy.ops.object.select_all(action='DESELECT');proxy.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
assert all(v.groups for v in proxy.data.vertices), 'Proxy bone heat left unweighted vertices'
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
from collections import defaultdict
proxy.data.calc_loop_triangles();triangles=[tuple(t.vertices) for t in proxy.data.loop_triangles]
positions=[proxy.matrix_world@v.co for v in proxy.data.vertices]
tree=BVHTree.FromPolygons(positions,triangles,all_triangles=True)
body.vertex_groups.clear();groups={g.name:body.vertex_groups.new(name=g.name) for g in proxy.vertex_groups}
cache={}
for v in body.data.vertices:
    position=body.matrix_world@v.co;key=tuple(round(x,5) for x in position)
    if key not in cache:
        point,normal,face,distance=tree.find_nearest(position);ids=triangles[face]
        bary=barycentric_transform(point,*(positions[i] for i in ids),Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
        weights=defaultdict(float)
        for index,w in zip(ids,bary):
            for g in proxy.data.vertices[index].groups:weights[proxy.vertex_groups[g.group].name]+=max(0,w)*g.weight
        selected=sorted(weights,key=weights.get,reverse=True)[:4];total=sum(weights[n] for n in selected);assert total>0
        cache[key]={n:weights[n]/total for n in selected if weights[n]/total>.00001}
    for name,w in cache[key].items():groups[name].add([v.index],w,'REPLACE')
bpy.data.objects.remove(proxy,do_unlink=True)
assert all(v.groups for v in body.data.vertices)
after=diagnostic();rig.data.pose_position='POSE'
actions=list(bpy.data.actions);checks=[]
for action in actions:
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    for frame in range(round(action.frame_range[0]),round(action.frame_range[1])+1):
        scene.frame_set(frame);root=rig.pose.bones['Root'];root.location=(0,0,0);bpy.context.view_layer.update()
        dg=bpy.context.evaluated_depsgraph_get();evaluated=body.evaluated_get(dg);mesh=evaluated.to_mesh()
        low=min((evaluated.matrix_world@v.co).z for v in mesh.vertices);evaluated.to_mesh_clear()
        root.location=(rig.matrix_world@root.bone.matrix_local).to_3x3().inverted()@Vector((0,0,-low));root.keyframe_insert('location',frame=frame)
rig.animation_data.action=next(a for a in actions if a.name=='idle');scene.frame_set(1)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ParkingMarshal.blend'))
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rig.select_set(True)
for child in rig.children:
    if child.type=='EMPTY':child.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'ParkingMarshal.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'ParkingMarshal.glb'),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS')
(OUT/'rebind-diagnostic.json').write_text(json.dumps({'before':before,'after':after,'source':str(SOURCE)},indent=2),encoding='utf8')
report=json.loads((SOURCE.parent/'rig-report.json').read_text());report['skin_method']='bone heat on watertight voxel proxy transferred to untouched original surface; Root is control-only';report['triangles']=sum(len(p.vertices)-2 for p in body.data.polygons)
(OUT/'rig-report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps({'before':before,'after':after}),flush=True)
