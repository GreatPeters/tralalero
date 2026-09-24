"""Read-only vertical ray profile of R12's repeated drainage openings."""
import argparse
import json
from pathlib import Path
import sys

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source, output = Path(args.source).resolve(), Path(args.output).resolve()
assert not output.exists()
bpy.ops.wm.read_factory_settings(use_empty=True)
if source.suffix.lower() == '.fbx':
    bpy.ops.import_scene.fbx(filepath=str(source))
else:
    bpy.ops.import_scene.gltf(filepath=str(source))
vertices, triangles = [], []
for obj in bpy.context.scene.objects:
    if obj.type != 'MESH':
        continue
    offset = len(vertices)
    vertices.extend(obj.matrix_world @ vertex.co for vertex in obj.data.vertices)
    obj.data.calc_loop_triangles()
    triangles.extend(tuple(index + offset for index in triangle.vertices) for triangle in obj.data.loop_triangles)
lo = Vector(tuple(min(vertex[i] for vertex in vertices) for i in range(3)))
hi = Vector(tuple(max(vertex[i] for vertex in vertices) for i in range(3)))
long_axis = 0 if hi.x - lo.x > hi.y - lo.y else 1
cross_axis = 1 - long_axis
height = hi.z - lo.z
tree = BVHTree.FromPolygons(vertices, triangles, all_triangles=True)
profiles = []
for cross_fraction in (.35, .5, .65):
    samples = []
    runs = []
    start = None
    for index in range(2049):
        along = .035 + .93 * index / 2048
        origin = Vector((0, 0, hi.z + height))
        origin[long_axis] = lo[long_axis] + (hi[long_axis] - lo[long_axis]) * along
        origin[cross_axis] = lo[cross_axis] + (hi[cross_axis] - lo[cross_axis]) * cross_fraction
        point, _, _, _ = tree.ray_cast(origin, Vector((0, 0, -1)))
        depth = (hi.z - point.z) / height if point is not None else None
        deep = depth is None or depth > .3
        samples.append([along, depth])
        if deep and start is None:
            start = along
        if not deep and start is not None:
            runs.append([start, along])
            start = None
    if start is not None:
        runs.append([start, samples[-1][0]])
    profiles.append({'cross_fraction': cross_fraction, 'deep_runs': runs, 'deep_count': len(runs), 'samples': samples})
report = {'source': str(source), 'bounds_min': list(lo), 'bounds_max': list(hi),
          'long_axis': 'XY'[long_axis], 'threshold_height_fraction': .3, 'profiles': profiles}
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(report, indent=2), encoding='utf8')
print(json.dumps({'source': str(source), 'dimensions': list(hi - lo), 'profiles':
    [{'cross_fraction': profile['cross_fraction'], 'deep_count': profile['deep_count']}
     for profile in profiles]}), flush=True)
