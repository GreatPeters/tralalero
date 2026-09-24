"""Fresh-import geometry, deformations and fixed-camera action evidence."""
import argparse,json,math,sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector

p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--output',required=True);p.add_argument('--render',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
src=Path(a.input).resolve();out=Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.render.fps=30
if src.suffix=='.blend':bpy.ops.wm.open_mainfile(filepath=str(src))
elif src.suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(src))
else:bpy.ops.import_scene.gltf(filepath=str(src))
scene=bpy.context.scene;scene.render.fps=30
rig=next(o for o in scene.objects if o.type=='ARMATURE');helpers={b.custom_shape for b in rig.pose.bones if b.custom_shape is not None}
for helper in helpers:helper.hide_render=True
meshes=[o for o in scene.objects if o.type=='MESH' and o not in helpers];body=next(o for o in meshes if o.name.endswith('_Body'))
actions=list(bpy.data.actions);report={'source':str(src),'bones':len(rig.data.bones),'triangles':sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),'actions':[],'poses':[],'materials':{}}
for o in meshes:
    report['materials'][o.name]=[{'name':m.name,'textured':any(n.type=='TEX_IMAGE' and n.image for n in m.node_tree.nodes)} for m in o.data.materials if m]
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16;scene.render.resolution_x=480;scene.render.resolution_y=560;scene.render.resolution_percentage=100
if scene.world is None:scene.world=bpy.data.worlds.new('EvidenceWorld')
scene.world.color=(.25,.25,.25);scene.view_settings.view_transform='Standard';scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
world=scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.17,.21,1);world.node_tree.nodes['Background'].inputs[1].default_value=.7
for name,location,power,size in [('Key',(4,-5,7),850,5),('Fill',(-4,-2,4),550,4),('Rim',(1,4,6),950,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;light=bpy.data.objects.new(name,data);scene.collection.objects.link(light);light.location=location;light.rotation_euler=(Vector((0,0,1.5))-light.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('Evidence');cam=bpy.data.objects.new('Evidence',camdata);scene.collection.objects.link(cam);scene.camera=cam;camdata.type='ORTHO';camdata.ortho_scale=3.9
cam.location=(4,-7,3.5);cam.rotation_euler=(Vector((0,0,1.5))-cam.location).to_track_quat('-Z','Y').to_euler()
for action in actions:
    rig.animation_data.action=action;start,end=action.frame_range;report['actions'].append({'name':action.name,'seconds':(end-start)/30})
    first=None;motion=0
    for i,t in enumerate([0,.2,.4,.6,.8,1]):
        frame=start+(end-start)*t;scene.frame_set(math.floor(frame),subframe=float(frame-math.floor(frame)));bpy.context.view_layer.update();ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();pts=np.array([ev.matrix_world@v.co for v in mesh.vertices]);ev.to_mesh_clear()
        if not np.isfinite(pts).all():raise RuntimeError('Nonfinite skin coordinates')
        if first is None:first=pts.copy()
        else:motion=max(motion,float(np.max(np.linalg.norm(pts-first,axis=1))))
        report['poses'].append({'action':action.name,'t':t,'minZ':float(pts[:,2].min()),'maxZ':float(pts[:,2].max())})
        if a.render:
            scene.render.filepath=str(out/(action.name.replace('|','_')+f'-{i}.png'));bpy.ops.render.render(write_still=True)
    report['actions'][-1]['maximumVertexMotion']=motion
if len(actions)<7:raise RuntimeError('Animation export lost actions')
if any(x['maximumVertexMotion']<.03 for x in report['actions']):raise RuntimeError('An exported action is effectively still')
(out/'inspection.json').write_text(json.dumps(report,indent=2));print(json.dumps({'bones':report['bones'],'triangles':report['triangles'],'actions':report['actions']}))
