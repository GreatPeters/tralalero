"""Render real rig actions into a labeled silent MP4 and critical-pose PNGs."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
import bpy
from mathutils import Vector

parser=argparse.ArgumentParser()
parser.add_argument('--folder',required=True)
parser.add_argument('--name',required=True)
parser.add_argument('--stills-only',action='store_true')
parser.add_argument('--cpu',action='store_true')
parser.add_argument('--actions',default='')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
folder=Path(args.folder).resolve()
bpy.ops.wm.open_mainfile(filepath=str(folder/(args.name+'.blend')))
scene=bpy.context.scene;scene.render.fps=30
rig=next(o for o in scene.objects if o.type=='ARMATURE')
body=[o for o in scene.objects if o.type=='MESH' and o.find_armature()==rig]
actions=list(bpy.data.actions)
if args.actions:actions=[a for a in actions if a.name in args.actions.split(',')]
scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=24
if args.cpu:
    scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=12
    scene.cycles.use_denoising=True
scene.render.resolution_x=960;scene.render.resolution_y=960;scene.render.resolution_percentage=100
scene.render.threads_mode='FIXED';scene.render.threads=4
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Animation_review_world');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.15,.19,.16,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.55
target=Vector((0,0,1.5))
for location,power in [((-4,-5,7),650),((4,-2,5),400),((2,4,7),700)]:
    bpy.ops.object.light_add(type='AREA',location=location);light=bpy.context.object
    light.data.energy=power;light.data.shape='DISK';light.data.size=4
    light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=30,location=(0,0,-.02));floor=bpy.context.object
floor.name='REVIEW_FLOOR';material=bpy.data.materials.new('Review floor');material.diffuse_color=(.25,.29,.23,1);floor.data.materials.append(material)
bpy.ops.object.camera_add(location=(4,-8,3.5));camera=bpy.context.object;scene.camera=camera
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=4.65
bpy.ops.object.text_add();label=bpy.context.object;label.name='REVIEW_CLIP_LABEL';label.parent=camera
label.location=(-2.1,2.04,-4);label.rotation_euler=(0,0,0);label.data.size=.17
label_material=bpy.data.materials.new('Review text');label_material.use_nodes=True
shader=label_material.node_tree.nodes.get('Principled BSDF')
shader.inputs['Base Color'].default_value=(.95,.93,.84,1)
shader.inputs['Emission Color'].default_value=(.95,.93,.84,1);shader.inputs['Emission Strength'].default_value=1
label.data.materials.append(label_material)
frames=folder/'preview-frames';stills=folder/'poses'
frames.mkdir(exist_ok=True);stills.mkdir(exist_ok=True)
manifest=[];index=1
for action in actions:
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    first,last=map(round,action.frame_range)
    name=action.name.split('|')[-1]
    label.data.body=args.name+' / '+name
    key_frames=sorted(set(round(first+(last-first)*t) for t in (0,.25,.5,.75,1)))
    chosen=key_frames if args.stills_only else range(first,last)
    for frame in chosen:
        scene.frame_set(frame);bpy.context.view_layer.update()
        path=stills/(name+f'-{frame:03}.png') if args.stills_only else frames/f'{index:05}.png'
        if not path.exists():
            scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
        if not args.stills_only:
            if frame in key_frames:
                import shutil
                shutil.copy2(path,stills/(name+f'-{frame:03}.png'))
            index+=1
    manifest.append({'action':name,'first_frame':first,'last_frame':last,'duration':(last-first)/30})
    print('REVIEW_ACTION '+name,flush=True)
(folder/'animation-preview.json').write_text(json.dumps({'fps':30,'actions':manifest,'video_frames':index-1},indent=2),encoding='utf8')
if not args.stills_only:
    ffmpeg=Path.home()/'AppData/Roaming/Python/Python312/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
    subprocess.run([str(ffmpeg),'-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(frames/'%05d.png'),
        '-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart','-threads','4',str(folder/'animation-preview.mp4')],check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
