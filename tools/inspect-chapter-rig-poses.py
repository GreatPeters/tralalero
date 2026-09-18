"""Inspect evaluated, fresh-import mascot poses and export per-slot materials."""
import argparse
import json
import sys
from pathlib import Path
import bpy
import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument('--root', required=True)
parser.add_argument('--only', default='')
parser.add_argument('--revision', default='v2')
parser.add_argument('--format', choices=['glb', 'fbx'], default='glb')
parser.add_argument('--full-keys', action='store_true')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
root = Path(args.root)
for folder in sorted((root/'outputs/chapters-polish-2026-09-12/rigged'/args.revision).iterdir()):
    name = folder.name
    if not folder.is_dir() or (args.only and name != args.only):
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 30
    if args.format == 'glb':
        bpy.ops.import_scene.gltf(filepath=str(folder/(name+'.glb')))
    else:
        bpy.ops.import_scene.fbx(filepath=str(folder/(name+('-full-keys' if args.full_keys else '')+'.fbx')))
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    body = next(o for o in bpy.context.scene.objects if o.type == 'MESH' and o.name.endswith('_Body'))
    helpers = {b.custom_shape for b in rig.pose.bones if b.custom_shape is not None}
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o not in helpers]
    materials = {}
    for mesh in meshes:
        slots = []
        for material in mesh.data.materials:
            shader = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
            slots.append({'name': material.name, 'color': list(shader.inputs['Base Color'].default_value),
                          'textured': shader.inputs['Base Color'].is_linked})
        materials[mesh.name] = slots
    actions = list(bpy.data.actions)
    poses = []
    for action in actions:
        rig.animation_data.action = action
        for frame in [1, 8, 16, 24, 31]:
            bpy.context.scene.frame_set(frame)
            bpy.context.view_layer.update()
            evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
            mesh = evaluated.to_mesh()
            points = np.array([evaluated.matrix_world @ v.co for v in mesh.vertices])
            evaluated.to_mesh_clear()
            if not np.isfinite(points).all():
                raise RuntimeError(name+' has nonfinite posed coordinates')
            poses.append({'action': action.name, 'frame': frame, 'minZ': float(points[:, 2].min()),
                          'percentileZ': [float(x) for x in np.percentile(points[:, 2], [1, 5, 10])],
                          'maxZ': float(points[:, 2].max()),
                          'rootLocation': list(rig.pose.bones['Root'].location)})
    report = {'name': name, 'bones': len(rig.data.bones), 'actions': [a.name for a in actions],
              'materials': materials, 'poses': poses}
    report_path = 'fresh-pose-report.json' if args.format == 'glb' else 'fresh-fbx-pose-report.json'
    if args.full_keys: report_path = 'fresh-fbx-fullkeys-pose-report.json'
    (folder/report_path).write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps({'name': name, 'bones': report['bones'], 'actions': report['actions'],
                      'minZRange': [min(p['minZ'] for p in poses), max(p['minZ'] for p in poses)]}), flush=True)
