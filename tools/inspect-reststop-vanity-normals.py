"""Diagnose custom-normal round trips without exporting or approving a model."""
import json
from pathlib import Path
import bpy

root = Path(__file__).resolve().parents[1]
source = root / 'outputs/reststop-production-2026-09-24/assets/T06_9469647183/stages/m3_t1/low2/model.glb'
report_path = root / 'outputs/reststop-production-2026-09-24/reviews/T06-normal-roundtrip-r2.json'
assert not report_path.exists()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
obj = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
mesh = obj.data
original = [normal.vector.copy() for normal in mesh.corner_normals]
report = {'source': str(source), 'loops': len(original),
          'normal_lengths': [min(n.length for n in original), max(n.length for n in original)],
          'smooth_faces': sum(p.use_smooth for p in mesh.polygons),
          'sharp_edges': sum(e.use_edge_sharp for e in mesh.edges)}

def errors(label):
    values = [(n.vector - old).length for n, old in zip(mesh.corner_normals, original)]
    worst = max(range(len(values)), key=values.__getitem__)
    report[label] = {'max_error': values[worst], 'loop': worst,
                     'before': list(original[worst]), 'after': list(mesh.corner_normals[worst].vector),
                     'max_direction_error': max((n.vector.normalized() - old.normalized()).length
                                                for n, old in zip(mesh.corner_normals, original)),
                     'changed_loops': sum(error > .001 for error in values)}

mesh.normals_split_custom_set(original)
mesh.update()
errors('noop_set')
for edge in mesh.edges:
    edge.use_edge_sharp = True
mesh.update()
mesh.normals_split_custom_set(original)
mesh.update()
errors('all_sharp_set')
report_path.write_text(json.dumps(report, indent=2), encoding='utf8')
print(json.dumps(report), flush=True)
