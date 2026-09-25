"""Read-only front surface samples for the inspected V05 second shape."""
import argparse
import json
import sys
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

root = Path(__file__).resolve().parents[1]
out = root / 'outputs/reststop-production-2026-09-24'
source = out / 'assets/V05_c9399962dc/stages/m2/model.glb'
receipt = out / 'reviews/V05-m2-front-profile-r1.json'
parser = argparse.ArgumentParser()
parser.add_argument('--source', type=Path, default=source)
parser.add_argument('--output', type=Path, default=receipt)
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
source, receipt = args.source.resolve(), args.output.resolve()
assert not receipt.exists()
bpy.ops.wm.read_factory_settings(use_empty=True)
if source.suffix.lower()=='.fbx':bpy.ops.import_scene.fbx(filepath=str(source))
else:bpy.ops.import_scene.gltf(filepath=str(source))
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
world = [obj.matrix_world @ v.co for v in obj.data.vertices]
obj.data.calc_loop_triangles()
tree = BVHTree.FromPolygons(world, [tuple(t.vertices) for t in obj.data.loop_triangles], all_triangles=True)
lo = Vector(tuple(min(v[i] for v in world) for i in range(3)))
hi = Vector(tuple(max(v[i] for v in world) for i in range(3)))
span = hi - lo
rows = []
for row in range(65):
    h = .2 + row * .01
    hits = []
    for col in range(81):
        x = .1 + col * .01
        point, normal, index, _ = tree.ray_cast(Vector((lo.x + span.x*x, lo.y-span.y, lo.z + span.z*h)), Vector((0,1,0)), span.y*3)
        hits.append({'x':x,'point':list(point) if point else None,'normal':list(normal) if normal else None,
                     'front_fraction':(point.y-lo.y)/span.y if point else None})
    rows.append({'height':h,'hits':hits})
receipt.write_text(json.dumps({'source':str(source),'bounds_min':list(lo),'bounds_max':list(hi),
                               'triangles':len(obj.data.loop_triangles),'rows':rows},indent=2),encoding='utf8')
print(json.dumps({'bounds_min':list(lo),'bounds_max':list(hi),'sample_rows':[
    {'height':r['height'],'front_fractions':[r['hits'][i]['front_fraction'] for i in (5,20,40,60,75)]} for r in rows[::2]]}),flush=True)
