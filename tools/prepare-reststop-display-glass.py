"""Separate S02 glass by inspected outer envelope; preserve every mesh face/UV.

Prototype on the recovered clay first. Reuse with the textured source only after
checking the prototype. A prototype is never a completed production asset.
"""
import argparse
import json
from pathlib import Path
import runpy
import shutil
import sys

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
parser.add_argument('--prototype', action='store_true')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
root = Path(__file__).resolve().parents[1]
source, output = Path(args.source).resolve(), Path(args.output).resolve()
assert source.is_relative_to(root/'outputs') and output.is_relative_to(root/'outputs')
assert 'S02' in str(source) and not output.exists()
output.mkdir(parents=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(objects) == 1
obj = objects[0]
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
mesh = obj.data
positions = [tuple(vertex.co) for vertex in mesh.vertices]
vertices = [vertex.co.copy() for vertex in mesh.vertices]
lo = Vector(tuple(min(p[i] for p in vertices) for i in range(3)))
hi = Vector(tuple(max(p[i] for p in vertices) for i in range(3)))
dim, mid = hi-lo, (hi+lo)/2
tree = BVHTree.FromPolygons(vertices, [tuple(p.vertices) for p in mesh.polygons])
origin = Vector((mid.x,lo.y-dim.length,lo.z+dim.z*.5))
crossings = []
for _ in range(50):
    hit = tree.ray_cast(origin,Vector((0,1,0)))[0]
    if hit is None:
        break
    crossings.append(hit.y)
    origin = hit+Vector((0,dim.y*.0001,0))
assert len(crossings) >= 4
gap_index = max(range(len(crossings)-1),key=lambda i:crossings[i+1]-crossings[i])
assert crossings[gap_index+1]-crossings[gap_index] > dim.y*.3
case_back = crossings[gap_index+1]

if args.prototype:
    metal = bpy.data.materials.new('Prototype neutral metal, not generated texture')
    metal.use_nodes = True
    shader = metal.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value = (.38,.40,.42,1)
    shader.inputs['Metallic'].default_value = .65
    shader.inputs['Roughness'].default_value = .32
    mesh.materials.clear()
    mesh.materials.append(metal)

glass = bpy.data.materials.new('Display clear glazing')
glass.use_nodes = True
glass.diffuse_color = (.78,.84,.88,.035)
glass.use_backface_culling = False
glass.surface_render_method = 'DITHERED'
shader = glass.node_tree.nodes['Principled BSDF']
shader.inputs['Base Color'].default_value = (.78,.84,.88,1)
shader.inputs['Metallic'].default_value = 0
shader.inputs['Roughness'].default_value = .13
shader.inputs['IOR'].default_value = 1.5
shader.inputs['Alpha'].default_value = .035
mesh.materials.append(glass)
glass_index = len(mesh.materials)-1
selected = {'curved_front': [], 'top': [], 'side': []}
for polygon in mesh.polygons:
    c, n = polygon.center, polygon.normal
    fx, fy, fz = ((c[i]-lo[i])/dim[i] for i in range(3))
    if not .235 < fz < .992 or c.y > case_back-dim.y*.035:
        continue
    front = tree.ray_cast(Vector((c.x, lo.y-dim.length, c.z)), Vector((0,1,0)))[0]
    if front is None:
        continue
    region = None
    if .055 < fx < .945:
        if -.001*dim.y < c.y-front.y < .030*dim.y and abs(n.y) > .20:
            region = 'curved_front'
        elif c.y < case_back-dim.y*.07 and fz > .83:
            top = tree.ray_cast(Vector((c.x,c.y,hi.z+dim.length)), Vector((0,0,-1)))[0]
            if top is not None and -.001*dim.z < top.z-c.z < .030*dim.z:
                region = 'top'
    elif .27 < fz < .94 and c.y > front.y+dim.y*.035 and c.y < case_back-dim.y*.065:
        region = 'side'
    if region:
        polygon.material_index = glass_index
        selected[region].append(polygon.index)
assert all(selected.values()), {key:len(value) for key,value in selected.items()}
assert positions == [tuple(vertex.co) for vertex in mesh.vertices]
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output/'model.blend'))
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'), use_selection=True, export_format='GLB')
report = {'source':str(source),'prototype_only':args.prototype,'algorithm_revision':4,
    'bounds_min':list(lo),'bounds_max':list(hi),'case_back':case_back,
    'pane_faces':{key:len(value) for key,value in selected.items()},'selected_face_indices':selected,
    'pane_alpha':.035,'vertex_positions_and_uvs_unchanged':True,
    'method':'Front/top ray envelope and side-panel bounds, preserving metal borders and both shelves',
    'extra_ai_requests':0}
(output/'glass-separation.json').write_text(json.dumps(report,indent=2),encoding='utf8')
script = root/'tools/reststop-production-review-render.py'
sys.argv = [str(script),'--',str(output)]
runpy.run_path(str(script),run_name='__main__')
shutil.copy2(output/'quality/opposite.png',output/'preview.png')
print(json.dumps({key:value for key,value in report.items() if key!='selected_face_indices'}),flush=True)
