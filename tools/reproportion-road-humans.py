"""Re-proportion reviewed human meshes/rest bones; retain UVs, role props and six actions."""
import argparse
import json
from pathlib import Path
import sys

import bpy
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('--root', required=True)
parser.add_argument('--only', default='')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
root = Path(args.root)
source_root = root / 'outputs/chapters-polish-2026-09-12/rigged/v2'
output_root = root / 'outputs/approved-road-concepts-2026-09-13/humans'
roles = ['ConeMechanic', 'TrafficPatrol', 'TollgateChief', 'TireBruiser', 'AsphaltWorker',
         'DeliveryRider', 'SnackChef', 'CoffeeVendor', 'ParkingMarshal']
if args.only and args.only not in roles:
    raise ValueError('Unknown human role: ' + args.only)

for name in roles:
    if args.only and args.only != name:
        continue
    output = output_root / name
    if output.exists():
        raise RuntimeError('Preserve the existing output: ' + str(output))
    source = source_root / name / (name + '.blend')
    bpy.ops.wm.open_mainfile(filepath=str(source))
    scene = bpy.context.scene
    scene.render.fps = 30
    rig = next(o for o in scene.objects if o.type == 'ARMATURE')
    body = next(o for o in scene.objects if o.type == 'MESH' and o.name == name + '_Body')
    equipment = [o for o in scene.objects if o.type == 'MESH' and o.name == name + '_Equipment']
    actions = list(bpy.data.actions)
    if len(actions) != 6 or len(rig.data.bones) != 18:
        raise RuntimeError('Unexpected rig contract: ' + name)
    rig.data.pose_position = 'REST'
    scene.frame_set(1)
    minimum = min(v.co.z for v in body.data.vertices)
    maximum = max(v.co.z for v in body.data.vertices)
    neck = rig.data.bones['Neck'].head_local.z
    head_span = maximum - neck
    # Head/hat volume keeps its shape; the region below the neck grows to two head units.
    stretch = 2 * head_span / (neck - minimum)
    target_height = 2.25 if name == 'TireBruiser' else 2.10
    scale = target_height / (3 * head_span)

    def reshape(point):
        p = point.copy()
        p.z = ((p.z - minimum) * stretch if p.z <= neck
               else (neck - minimum) * stretch + p.z - neck)
        return p * scale

    old_rest = {b.name: b.matrix_local.copy() for b in rig.data.bones}
    vertex_count = len(body.data.vertices)
    triangle_count = sum(len(p.vertices) - 2 for p in body.data.polygons)
    for vertex in body.data.vertices:
        vertex.co = reshape(vertex.co)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    for bone in rig.data.edit_bones:
        bone.head = reshape(bone.head)
        bone.tail = reshape(bone.tail)
    bpy.ops.object.mode_set(mode='OBJECT')
    # Carried equipment is rigid: move/rotate with its hand instead of stretching tires/cups.
    for obj in equipment:
        hand = 'Hand.L' if name == 'AsphaltWorker' else 'Hand.R'
        old_matrix = old_rest[hand]
        new_matrix = rig.data.bones[hand].matrix_local
        delta_rotation = new_matrix.to_3x3() @ old_matrix.to_3x3().inverted()
        old_grip, new_grip = old_matrix.translation, new_matrix.translation
        for vertex in obj.data.vertices:
            vertex.co = new_grip + delta_rotation @ ((vertex.co - old_grip) * scale)
    rig.data.pose_position = 'POSE'
    ground = []
    for action in actions:
        rig.animation_data.action = action
        for frame in range(1, 32):
            scene.frame_set(frame)
            bpy.context.view_layer.update()
            evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
            mesh = evaluated.to_mesh()
            min_z = min((evaluated.matrix_world @ v.co).z for v in mesh.vertices)
            evaluated.to_mesh_clear()
            root_bone = rig.pose.bones['Root']
            local_up = (rig.matrix_world @ root_bone.bone.matrix_local).to_3x3().inverted() @ Vector((0, 0, -min_z))
            root_bone.location += local_up
            root_bone.keyframe_insert('location', frame=frame)
            bpy.context.view_layer.update()
            evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
            mesh = evaluated.to_mesh()
            checked = min((evaluated.matrix_world @ v.co).z for v in mesh.vertices)
            evaluated.to_mesh_clear()
            if abs(checked) > .002:
                raise RuntimeError(f'Ground check failed: {name}/{action.name}/{frame}: {checked}')
            ground.append({'action': action.name, 'frame': frame, 'minZ': checked})
    rig.animation_data.action = next(a for a in actions if a.name == 'idle')
    scene.frame_set(1)
    output.mkdir(parents=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(output / (name + '.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [rig, body, *equipment]:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.gltf(filepath=str(output / (name + '.glb')), export_format='GLB',
                            use_selection=True, export_animations=True, export_animation_mode='ACTIONS')
    bpy.ops.export_scene.fbx(filepath=str(output / (name + '-full-keys.fbx')), use_selection=True,
                            add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
                            bake_anim_use_nla_strips=False, bake_anim_step=1, bake_anim_simplify_factor=0,
                            path_mode='COPY', embed_textures=True)
    report = {'name': name, 'source': str(source), 'height': target_height,
              'headRegionBoundary': neck, 'oldHeadHeightRatio': (maximum - minimum) / head_span,
              'newHeadHeightRatio': 3, 'bodyStretch': stretch, 'globalScale': scale,
              'vertices': vertex_count, 'triangles': triangle_count, 'bones': len(rig.data.bones),
              'actions': [a.name for a in actions], 'uvLayers': len(body.data.uv_layers),
              'equipmentRigid': True, 'groundMaxError': max(abs(p['minZ']) for p in ground)}
    (output / 'proportion-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    (output / 'ground-checks.json').write_text(json.dumps(ground, indent=2), encoding='utf-8')
    print(json.dumps(report), flush=True)
