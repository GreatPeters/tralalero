"""Read-only S02 front-to-back surface/material crossings."""
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

source = Path(sys.argv[sys.argv.index('--')+1]).resolve()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
vertices, faces, materials = [], [], []
for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
    offset = len(vertices)
    vertices += [obj.matrix_world@v.co for v in obj.data.vertices]
    for p in obj.data.polygons:
        faces.append(tuple(i+offset for i in p.vertices))
        materials.append(obj.data.materials[p.material_index].name)
lo = Vector(tuple(min(p[i] for p in vertices) for i in range(3)))
hi = Vector(tuple(max(p[i] for p in vertices) for i in range(3)))
dim = hi-lo
tree = BVHTree.FromPolygons(vertices,faces)
rows=[]
for fx in (.2,.5,.8):
    for fz in (.4,.5,.75,.9,.96):
        origin = Vector((lo.x+dim.x*fx,lo.y-dim.y,lo.z+dim.z*fz))
        crossings=[]
        for _ in range(50):
            point, normal, index, distance = tree.ray_cast(origin,Vector((0,1,0)))
            if point is None:break
            crossings.append({'y_fraction':(point.y-lo.y)/dim.y,'normal':list(normal),'material':materials[index]})
            origin=point+Vector((0,dim.y*.0001,0))
        rows.append({'x_fraction':fx,'z_fraction':fz,'crossings':crossings})
report={'source':str(source),'rays':rows}
target=source.parent/'ray-crossings.json'
assert not target.exists()
target.write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report),flush=True)
