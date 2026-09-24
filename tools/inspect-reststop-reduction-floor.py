"""Read-only component evidence for a failed reduction; never runs Decimate."""
import argparse
import importlib.util
import json
import sys
from pathlib import Path

import bmesh
import bpy

parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(args.source).resolve()
output = Path(args.output).resolve()
assert not output.exists(), 'Preserve prior diagnostic evidence'
app = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
spec = importlib.util.spec_from_file_location('task_refiner', app / 'blender_refine.py')
refiner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(refiner)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:
    obj.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
obj = bpy.context.object
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
lo, hi = refiner.bounds(obj)
diagonal = (hi - lo).length
warnings = []
refiner.remove_floor_if_clear(obj, warnings)
refiner.clean(obj, diagonal * 2e-6, merge=True)
refiner.reduction_priority(obj, 'parts', warnings)
bm = bmesh.new()
bm.from_mesh(obj.data)
bm.verts.ensure_lookup_table()
bm.verts.index_update()
seen = set()
components = []
for start in bm.verts:
    if start.index in seen:
        continue
    seen.add(start.index)
    stack = [start]
    vertices = []
    while stack:
        vertex = stack.pop()
        vertices.append(vertex)
        for edge in vertex.link_edges:
            other = edge.other_vert(vertex)
            if other.index not in seen:
                seen.add(other.index)
                stack.append(other)
    faces = {face for vertex in vertices for face in vertex.link_faces}
    edges = {edge for vertex in vertices for edge in vertex.link_edges}
    components.append({
        'vertices': len(vertices),
        'triangles': sum(len(face.verts) - 2 for face in faces),
        'boundary_edges': sum(edge.is_boundary for edge in edges),
        'bounds_min': [min(v.co[i] for v in vertices) for i in range(3)],
        'bounds_max': [max(v.co[i] for v in vertices) for i in range(3)],
    })
bm.free()
components.sort(key=lambda item: item['triangles'], reverse=True)
report = {
    'source': str(source), 'no_decimation_performed': True,
    'cleaned_triangles': refiner.tris(obj), 'component_count': len(components),
    'cleaned_mesh_metrics': refiner.inspect(obj),
    'closed_component_count': sum(c['boundary_edges'] == 0 for c in components),
    'small_components_under_12_vertices': sum(c['vertices'] < 12 for c in components),
    'warnings': warnings, 'components': components,
}
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(report, indent=2), encoding='utf8')
print(json.dumps({**{k: v for k, v in report.items() if k != 'components'}, 'largest_components': components[:12]}), flush=True)
