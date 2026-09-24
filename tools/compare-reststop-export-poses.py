"""Compare deformed surface samples and joint positions to the authored blend."""
import argparse,json,math,sys
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.kdtree import KDTree

parser=argparse.ArgumentParser();parser.add_argument('--folder',required=True);parser.add_argument('--name',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);folder=Path(args.folder).resolve()
factors=(0,.25,.5,.75,1)
def actors():
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    bodies=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.find_armature()==rig]
    return rig,bodies
def pose(rig,bodies,action,factor,sampled):
    rig.animation_data_create();rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    first,last=action.frame_range;frame=first+(last-first)*factor
    bpy.context.scene.frame_set(math.floor(frame),subframe=frame-math.floor(frame));bpy.context.view_layer.update()
    points=[];dg=bpy.context.evaluated_depsgraph_get()
    for body in bodies:
        obj=body.evaluated_get(dg);mesh=obj.to_mesh()
        step=max(1,len(mesh.vertices)//512) if sampled else 1
        points.extend(tuple(obj.matrix_world@mesh.vertices[i].co) for i in range(0,len(mesh.vertices),step))
        obj.to_mesh_clear()
    joints={bone.name:tuple(rig.matrix_world@bone.head) for bone in rig.pose.bones}
    return points,joints
bpy.ops.wm.open_mainfile(filepath=str(folder/(args.name+'.blend')))
bpy.context.scene.render.fps=30;rig,bodies=actors();reference={}
for action in list(bpy.data.actions):
    for factor in factors:reference[(action.name.split('|')[-1],factor)]=pose(rig,bodies,action,factor,True)
reports=[]
for suffix in ('fbx','glb'):
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.render.fps=30
    if suffix=='fbx':bpy.ops.import_scene.fbx(filepath=str(folder/(args.name+'.fbx')))
    else:bpy.ops.import_scene.gltf(filepath=str(folder/(args.name+'.glb')))
    bpy.context.scene.render.fps=30;rig,bodies=actors()
    for action in list(bpy.data.actions):
        name=action.name.split('|')[-1]
        for factor in factors:
            expected,joints=reference[(name,factor)];actual,actual_joints=pose(rig,bodies,action,factor,False)
            tree=KDTree(len(actual))
            for i,point in enumerate(actual):tree.insert(point,i)
            tree.balance();distances=[tree.find(point)[2] for point in expected]
            joint_error=max((Vector(position)-Vector(actual_joints[n])).length for n,position in joints.items())
            reports.append({'format':suffix,'action':name,'phase':factor,'surface_max':max(distances),
                            'surface_rms':math.sqrt(sum(d*d for d in distances)/len(distances)),
                            'joint_position_max':joint_error,'sampled_vertices':len(expected)})
ok=all(r['surface_max']<.02 and r['joint_position_max']<.02 for r in reports)
result={'ok':ok,'tolerance_world_units':.02,'checked':'native blend versus fresh FBX/GLB at five phases of every action',
        'maximum_surface_error':max(r['surface_max'] for r in reports),
        'maximum_joint_error':max(r['joint_position_max'] for r in reports),'samples':reports}
(folder/'export-pose-comparison.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print(json.dumps({k:v for k,v in result.items() if k!='samples'}),flush=True)
assert ok,'Export pose differs from authored native pose; inspect comparison receipt'
