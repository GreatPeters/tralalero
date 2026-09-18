"""Fresh GLB/FBX verification of every re-proportioned role and all six actions."""
import json
from pathlib import Path
import sys
import bpy
import numpy as np

root = Path(sys.argv[sys.argv.index('--') + 1])
folder = root / 'outputs/approved-road-concepts-2026-09-13/humans'
reports = []
for actor in sorted(folder.iterdir()):
    if not actor.is_dir() or not (actor / 'proportion-report.json').exists():
        continue
    name = actor.name
    for kind in ('glb', 'fbx'):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.context.scene.render.fps = 30
        path = actor / (name + ('.glb' if kind == 'glb' else '-full-keys.fbx'))
        if kind == 'glb':
            bpy.ops.import_scene.gltf(filepath=str(path))
        else:
            bpy.ops.import_scene.fbx(filepath=str(path))
        rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
        body = next(o for o in bpy.context.scene.objects if o.type == 'MESH' and o.name == name + '_Body')
        actions = list(bpy.data.actions)
        if len(actions) != 6 or len(rig.data.bones) != 18 or not body.data.uv_layers:
            raise RuntimeError('Fresh rig/UV contract failed: ' + str(path))
        poses = []
        for action in actions:
            rig.animation_data.action = action
            for frame in (1, 8, 16, 24, 31):
                bpy.context.scene.frame_set(frame)
                bpy.context.view_layer.update()
                obj = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
                mesh = obj.to_mesh()
                points = np.array([obj.matrix_world @ v.co for v in mesh.vertices])
                obj.to_mesh_clear()
                if not np.isfinite(points).all():
                    raise RuntimeError('Non-finite posed coordinates: ' + name)
                poses.append({'action': action.name, 'frame': frame,
                              'minimumZ': float(points[:, 2].min()), 'maximumZ': float(points[:, 2].max())})
        report = {'name': name, 'format': kind, 'bones': len(rig.data.bones),
                  'actions': [a.name for a in actions], 'uvLayers': len(body.data.uv_layers),
                  'triangles': sum(len(p.vertices) - 2 for p in body.data.polygons),
                  'maximumGroundError': max(abs(p['minimumZ']) for p in poses), 'poses': poses}
        (actor / ('fresh-' + kind + '-verification.json')).write_text(json.dumps(report, indent=2), encoding='utf-8')
        reports.append({k: v for k, v in report.items() if k != 'poses'})
        print(json.dumps(reports[-1]), flush=True)
        if report['maximumGroundError'] > .025:
            raise RuntimeError('Fresh foot contact error exceeds25mm: ' + name + '/' + kind)
(folder / 'fresh-verification-summary.json').write_text(json.dumps(reports, indent=2), encoding='utf-8')
if len(reports) != 18:
    raise RuntimeError('Expected nine roles in both formats.')
