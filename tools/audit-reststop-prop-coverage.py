"""Read the v4 Blender scene for the PNG-asset coverage audit; never save it."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import sys

import bpy
from mathutils import Vector

ROOT = Path.cwd()
SOURCE = ROOT / 'outputs/reststop-blender-v4-2026-09-23/reststop-v4.blend'
OUT = ROOT / 'map-concepts/reststop-prop-audit-2026-09-24'
PREVIEW = ROOT / 'tmp/image-previews/reststop-trellis-images-2026-09-23/audit'
OUT.mkdir(parents=True, exist_ok=True)
PREVIEW.mkdir(parents=True, exist_ok=True)
parser = argparse.ArgumentParser()
parser.add_argument('--render', action='store_true')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
before = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.frame_set(1)
rows = []
for obj in scene.objects:
    ancestors = []
    parent = obj.parent
    while parent:
        ancestors.append(parent.name)
        parent = parent.parent
    row = {
        'name': obj.name, 'type': obj.type,
        'parent': obj.parent.name if obj.parent else None,
        'ancestors': ancestors, 'zone': obj.get('zone'),
        'assembly': obj.get('assembly'), 'role': obj.get('role'),
        'location_frame1': [round(v, 4) for v in obj.matrix_world.translation],
        'dimensions_frame1': [round(v, 4) for v in obj.dimensions],
        'animated': bool(obj.animation_data),
        'materials': [slot.material.name for slot in obj.material_slots if slot.material],
    }
    if obj.type == 'FONT':
        row['text'] = obj.data.body
    if obj.type == 'MESH':
        row['mesh'] = obj.data.name
        row['vertices'] = len(obj.data.vertices)
    rows.append(row)
summary = {
    'source': str(SOURCE), 'source_sha256': before,
    'objects': len(rows), 'types': dict(Counter(r['type'] for r in rows)),
    'assemblies': dict(Counter(r['assembly'] for r in rows if r['assembly'])),
    'roles': dict(Counter(r['role'] for r in rows if r['role'])),
    'cart_objects': [r['name'] for r in rows if 'cart' in r['name'].lower()],
    'fps': scene.render.fps, 'frames': [scene.frame_start, scene.frame_end],
    'scene_properties': {k: str(scene[k]) for k in scene.keys()},
    'named_prop_families': dict(Counter(re.sub(r'\.\d+$', '', r['name']) for r in rows
        if any(s in r['name'].lower() for s in ['vending','lamp','robot','pipe','vent','display','shield','wand','mop','backpack','sign','identity','canopy','privacy']))),
    'scope': 'Object/assembly coverage only. No mesh-quality, GLB re-export or Unity checks.',
}
(OUT / 'blender-inventory.json').write_text(json.dumps({'summary': summary, 'objects': rows}, ensure_ascii=False, indent=2), encoding='utf8')
print(json.dumps(summary, ensure_ascii=True), flush=True)

if args.render:
    camera_data = bpy.data.cameras.new('AUDIT_CAMERA_TEMP')
    camera = bpy.data.objects.new('AUDIT_CAMERA_TEMP', camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera_data.type = 'ORTHO'
    scene.render.engine = 'BLENDER_EEVEE'
    scene.eevee.taa_render_samples = 20
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 4
    scene.render.image_settings.file_format = 'PNG'
    shots = [
        ('01-vending', 56, (-75, -1.3, 1.6), (5, -10, 6), 8),
        ('02-parking-light', 26, (-124, -116, 5), (10, -16, 9), 18),
        ('03-cleaning-robot', 230, 'Cleaning_robot', (5, -8, 6), 10),
        ('04-leaking-pipe', 228, 'Leaking_pipe_0', (7, -10, 7), 11),
        ('05-tipping-display', 205.7, 'Domino_display_hinge', (9, -14, 10), 12),
        ('06-food-signs', 56, (-77, 2, 1.8), (9, -16, 11), 13),
    ]
    renders = []
    for name, seconds, target, offset, scale in shots:
        scene.frame_set(round(seconds * 24) + 1)
        if isinstance(target, str):
            target = scene.objects[target].matrix_world.translation + Vector((0, 0, 1))
        else:
            target = Vector(target)
        camera.location = target + Vector(offset)
        camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
        camera_data.ortho_scale = scale
        camera_data.clip_end = 1500
        scene.render.filepath = str(PREVIEW / (name + '.png'))
        bpy.ops.render.render(write_still=True)
        renders.append({'file': name + '.png', 'seconds': seconds, 'camera': list(camera.location), 'target': list(target), 'orthographic_scale': scale})
        print('AUDIT_RENDER ' + name, flush=True)
    (OUT / 'native-render-evidence.json').write_text(json.dumps(renders, indent=2), encoding='utf8')
after = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
assert before == after, 'Audit must not change the Blender source'
(OUT / 'source-preservation.json').write_text(json.dumps({'before': before, 'after': after, 'unchanged': True}, indent=2), encoding='utf8')
