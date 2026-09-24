"""Independently inspect saved humanoid FBX and GLB, including animated ground contact."""
import argparse
import json
import math
from pathlib import Path
import sys
import bpy

parser=argparse.ArgumentParser()
parser.add_argument('--folder',required=True)
parser.add_argument('--name',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
folder=Path(args.folder).resolve()
reports=[]
for suffix in ('fbx','glb'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene=bpy.context.scene;scene.render.fps=30
    source=folder/(args.name+'.'+suffix)
    if suffix=='fbx':bpy.ops.import_scene.fbx(filepath=str(source))
    else:bpy.ops.import_scene.gltf(filepath=str(source))
    rigs=[o for o in scene.objects if o.type=='ARMATURE']
    assert len(rigs)==1, 'Expected one humanoid rig'
    rig=rigs[0]
    bodies=[o for o in scene.objects if o.type=='MESH' and o.find_armature()==rig]
    assert bodies
    triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bodies)
    assert 0<triangles<=15000
    required={'Root','Hips','Spine','Chest','Neck','Head','Hand.L','Hand.R','Foot.L','Foot.R'}
    assert required.issubset(set(rig.data.bones.keys()))
    actions=list(bpy.data.actions)
    required_actions=['idle','walk','run','attack_once','hit','die','greet']
    assert all(any(a.name.split('|')[-1]==name for a in actions) for name in required_actions)
    weights=[]
    for body in bodies:
        assert body.data.uv_layers
        for vertex in body.data.vertices:
            assert all(math.isfinite(x) for x in vertex.co)
            total=sum(g.weight for g in vertex.groups)
            weights.append(abs(total-1))
        assert body.data.materials
    assert max(weights)<.005
    samples=[];loop_seams={}
    for action in actions:
        rig.animation_data_create();rig.animation_data.action=action
        if action.slots:rig.animation_data.action_slot=action.slots[0]
        first,last=action.frame_range
        poses=[]
        for factor in (0,.25,.5,.75,1):
            frame=first+(last-first)*factor
            scene.frame_set(math.floor(frame),subframe=frame-math.floor(frame))
            bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
            minimum=math.inf
            for body in bodies:
                evaluated=body.evaluated_get(dg);mesh=evaluated.to_mesh()
                minimum=min(minimum,min((evaluated.matrix_world@v.co).z for v in mesh.vertices))
                evaluated.to_mesh_clear()
            samples.append({'action':action.name,'frame':frame,'ground':minimum})
            poses.append([value for name in ['Head','UpperArm.R','Thigh.L'] for row in rig.pose.bones[name].matrix for value in row])
        variation=max(abs(a-b) for pose in poses[1:] for a,b in zip(poses[0],pose))
        assert variation>1e-6, 'Static exported action: '+action.name
        if action.name.split('|')[-1] in ('idle','walk','run','attack_loop'):
            seam=max(abs(a-b) for a,b in zip(poses[0],poses[-1]))
            loop_seams[action.name]=seam
            assert seam<.001, 'Loop endpoint discontinuity: '+action.name
    ground_error=max(abs(s['ground']) for s in samples)
    reports.append({'format':suffix,'triangles':triangles,'bones':len(rig.data.bones),'actions':[a.name for a in actions],
                    'max_weight_sum_error':max(weights),'maximum_ground_error':ground_error,
                    'loop_endpoint_errors':loop_seams,'samples':samples})
ok=all(r['maximum_ground_error']<.02 for r in reports)
(folder/'fresh-rig-validation.json').write_text(json.dumps({'ok':ok,'checked':'fresh FBX and GLB at 30fps','reports':reports},indent=2),encoding='utf8')
assert ok, [(r['format'],r['maximum_ground_error']) for r in reports]
print(json.dumps({'ok':True,'formats':len(reports),'actions':[len(r['actions']) for r in reports]}),flush=True)
