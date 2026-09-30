"""Render actual Meshy FBX action extrema, and save measured action bounds/poses."""
from pathlib import Path
import argparse
import json
import sys
import bpy
from mathutils import Vector

p=argparse.ArgumentParser();p.add_argument('--folder',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(a.folder).resolve();out=Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,'C:/Users/ljh/.codex/skills/blender-asset-validation/scripts')
import render_evidence as evidence

report=[]
for name in ('bid','call'):
    path=source/f'anim_{name}.fbx'
    if not path.exists(): continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path.resolve()))
    scene=bpy.context.scene
    rigs=[o for o in scene.objects if o.type=='ARMATURE']
    rig=rigs[0]
    action=rig.animation_data.action
    first,last=action.frame_range
    meshes=[o for o in scene.objects if o.type=='MESH']
    bpy.context.view_layer.update()
    deps=bpy.context.evaluated_depsgraph_get();points=[]
    for obj in meshes:
        evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh()
        points.extend(evaluated.matrix_world@v.co for v in mesh.vertices);evaluated.to_mesh_clear()
    lo=Vector(tuple(min(q[i] for q in points) for i in range(3)));hi=Vector(tuple(max(q[i] for q in points) for i in range(3)))
    center=(lo+hi)/2;extent=max(hi-lo)
    evidence.configure_scene(center,extent,lo.z,False)
    # The Meshy FBX is freshly imported for each action, with its own skin and animation.
    phases=[];images=[]
    for i,t in enumerate((0,.25,.5,.75,1)):
        frame=first+(last-first)*t;scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
        deps=bpy.context.evaluated_depsgraph_get();vertices=[]
        for obj in meshes:
            evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh()
            vertices.extend(evaluated.matrix_world@v.co for v in mesh.vertices)
            evaluated.to_mesh_clear()
        minimum=min(v.z for v in vertices);maximum=max(v.z for v in vertices)
        phases.append({'phase':t,'frame':frame,'min_z':minimum,'max_z':maximum})
        # Use the skill's renderer with a consistent front three-quarter view.
        file=out/f'{name}-{i}.png'
        scene.render.resolution_x=scene.render.resolution_y=400
        camera=scene.camera
        if camera is None:
            bpy.ops.object.camera_add(location=center+Vector((extent*1.5,-extent*2.5,extent*.8)))
            camera=bpy.context.object;scene.camera=camera
        pose_center=Vector(tuple((min(v[j] for v in vertices)+max(v[j] for v in vertices))*.5 for j in range(3)))
        camera.location=pose_center+Vector((extent*1.1,-extent*2.9,extent*.55))
        camera.rotation_euler=(pose_center-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.type='ORTHO';camera.data.ortho_scale=extent*1.65
        scene.render.filepath=str(file);bpy.ops.render.render(write_still=True);images.append(file)
    evidence.create_contact_sheet(images,out/f'{name}-phases.png',400)
    report.append({'action':name,'frame_range':[first,last],'fps':scene.render.fps,'phases':phases})
(out/'motion-metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
