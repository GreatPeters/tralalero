"""Report low horizontal surface bands for a visually flagged missed floor."""
import json
import sys
from pathlib import Path
import bpy

root = Path(sys.argv[sys.argv.index('--')+1])
source = root/'Assets/ShooterSurvival/Models/Highway/Props/064/Model.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source))
rows = []
for obj in bpy.context.scene.objects:
    if obj.type != 'MESH':
        continue
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    low = min(v.co.z for v in obj.data.vertices)
    high = max(v.co.z for v in obj.data.vertices)
    height = high-low
    bins = {}
    for face in obj.data.polygons:
        z = max(obj.data.vertices[i].co.z for i in face.vertices)
        if z > low+height*.15 or abs(face.normal.z) < .8:
            continue
        key = round((z-low)/height, 2)
        bucket = bins.setdefault(key, {'count': 0, 'area': 0})
        bucket['count'] += 1
        bucket['area'] += face.area
    rows.append({'object': obj.name, 'height': height, 'low': low, 'bands': bins})
print(json.dumps(rows, indent=2))
