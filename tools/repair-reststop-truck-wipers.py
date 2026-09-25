"""Locally reconstruct V05 m2's damaged lower glass and supported wipers."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import shutil
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/reststop-production-2026-09-24'
parser = argparse.ArgumentParser()
parser.add_argument('--revision', type=int, required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
folder = OUT / ('manual/V05-r' + str(args.revision)) / 'source'
assert args.revision > 0 and not folder.exists()
source = OUT / 'assets/V05_c9399962dc/stages/m2/model.glb'
profile = json.loads((OUT / 'reviews/V05-m2-front-profile-r1.json').read_text(encoding='utf8'))
lo, hi = Vector(profile['bounds_min']), Vector(profile['bounds_max'])
span = hi - lo
samples = [(hit['x'], row['height'], hit['front_fraction']) for row in profile['rows']
           if .60 <= row['height'] <= .70 for hit in row['hits']
           if .24 <= hit['x'] <= .76 and hit['point'] is not None and hit['normal'][1] < -.8]
matrix = np.array([[1., h, h*h, (x-.5)**2] for x, h, _ in samples])
values = np.array([y for _, _, y in samples])
coefficients = np.linalg.lstsq(matrix, values, rcond=None)[0]
residual = float(np.max(np.abs(matrix @ coefficients-values)))
assert len(samples) > 200 and residual < .002, (len(samples), residual)

def glass(x, h):
    fraction = float(np.dot(coefficients, [1., h, h*h, (x-.5)**2]))
    return Vector((lo.x+span.x*x, lo.y+span.y*fraction, lo.z+span.z*h))

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
assert len([o for o in bpy.context.scene.objects if o.type == 'MESH']) == 1
assert not obj.data.uv_layers and not obj.data.materials
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bm = bmesh.new()
bm.from_mesh(obj.data)
before = len(bm.faces)
threshold = (span.length*1e-7)**2*.04
degenerate = [face for face in bm.faces if face.calc_area() < threshold]
degenerate_count = len(degenerate)
if degenerate:
    bmesh.ops.delete(bm, geom=degenerate, context='FACES_ONLY')
initial_unused_vertices = sum(not v.link_faces for v in bm.verts)
protected = {tuple(v.co) for v in bm.verts
             if v.link_faces and not (.17 < (v.co.x-lo.x)/span.x < .83 and .475 < (v.co.z-lo.z)/span.z < .815
                     and (v.co.y-lo.y)/span.y < .12)}
remove = []
for face in bm.faces:
    p = face.calc_center_median()
    x, h, depth = (p.x-lo.x)/span.x, (p.z-lo.z)/span.z, (p.y-lo.y)/span.y
    if .20 < x < .80 and .495 < h < .785 and depth < .11:
        remove.append(face)
assert 100 < len(remove) < before*.1, len(remove)
bmesh.ops.delete(bm, geom=remove, context='FACES_ONLY')
wire = [e for e in bm.edges if not e.link_faces]
if wire:
    bmesh.ops.delete(bm, geom=wire, context='EDGES')
loose = [v for v in bm.verts if not v.link_faces]
if loose:
    bmesh.ops.delete(bm, geom=loose, context='VERTS')
missing = protected - {tuple(v.co) for v in bm.verts}
assert not missing, {'lost_protected_surface_vertices':len(missing), 'examples':list(missing)[:5],
                     'initial_unused_vertices':initial_unused_vertices}
bm.normal_update()
bm.to_mesh(obj.data)
bm.free()
obj.data.update()

# The source has complex overlapping boundaries: automatic hole fill closed
# only a tiny seven-corner loop, not the deleted strip. Use a measured, closed
# curved insert whose perimeter extends into the retained glass/body instead.
columns, rows = 40, 24
vertices = []
for back in (False, True):
    for row in range(rows+1):
        for column in range(columns+1):
            point = glass(.19+.62*column/columns, .48+.315*row/rows)
            point.y += span.z*(.002 if back else -.0008)
            vertices.append(tuple(point))
layer = (columns+1)*(rows+1)
faces = []
for row in range(rows):
    for column in range(columns):
        a = row*(columns+1)+column
        quad = (a,a+1,a+columns+2,a+columns+1)
        faces.extend((quad, tuple(index+layer for index in reversed(quad))))
perimeter = (list(range(columns+1))
             +[row*(columns+1)+columns for row in range(1,rows+1)]
             +[rows*(columns+1)+column for column in range(columns-1,-1,-1)]
             +[row*(columns+1) for row in range(rows-1,0,-1)])
for index, a in enumerate(perimeter):
    b = perimeter[(index+1)%len(perimeter)]
    faces.append((a,a+layer,b+layer,b))
pane_mesh = bpy.data.meshes.new('Measured_windshield')
pane_mesh.from_pydata(vertices, [], faces)
pane_mesh.update()
pane = bpy.data.objects.new('Measured_windshield',pane_mesh)
bpy.context.collection.objects.link(pane)

def rod(start, end, radius, name):
    direction = end-start
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=direction.length,
                                       end_fill_type='NGON', location=(start+end)*.5)
    rod_obj = bpy.context.object
    rod_obj.name = name
    rod_obj.rotation_euler = direction.to_track_quat('Z', 'Y').to_euler()
    return rod_obj

added = []
for index, shift in enumerate((0., .29), 1):
    base, joint = glass(.42+shift, .50), glass(.35+shift, .535)
    base.y -= span.z*.0007
    joint.y -= span.z*.002
    added.append(rod(base, joint, span.z*.00125, 'Wiper_arm_'+str(index)))
    left, right = glass(.245+shift, .537), glass(.465+shift, .543)
    left.y -= span.z*.002
    right.y -= span.z*.002
    added.append(rod(left, right, span.z*.0022, 'Wiper_blade_'+str(index)))
bpy.ops.object.select_all(action='DESELECT')
for item in [obj, pane, *added]:
    item.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.object.join()
for layer in list(obj.data.uv_layers):
    obj.data.uv_layers.remove(layer)
obj.data.polygons.foreach_set('use_smooth', [True]*len(obj.data.polygons))
obj.data.update()
obj.data.calc_loop_triangles()
triangles = len(obj.data.loop_triangles)
assert triangles <= 300000
folder.mkdir(parents=True)
bpy.ops.export_scene.gltf(filepath=str(folder/'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(folder/'model.blend'))
shutil.copy2(source, folder/'original-trellis-source.glb')
(folder/'repair.json').write_text(json.dumps({'source':str(source),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
    'method':'Replace the front-glass region with a thin closed curved panel fitted to measured intact glass; extend to the glass boundary to avoid a mid-pane seam; retain cab/cargo and add two slim wipers',
    'clean_glass_fit_coefficients':coefficients.tolist(),'fit_max_normalized_depth_residual':residual,
    'removed_faces':len(remove),'triangles':triangles,'removed_source_degenerate_faces':degenerate_count,
    'glass_insert_grid':[columns,rows],'glass_insert_x':[.19,.81],'glass_insert_height':[.48,.795],
    'initial_unused_vertices_removed':initial_unused_vertices,
    'positions_outside_local_region_retained':True,'extra_ai_shape_requests':0,'canonical_reduction_attempts':0,
    'visual_and_fresh_intermediate_validation_required':True},indent=2),encoding='utf8')
script=ROOT/'tools/reststop-production-review-render.py'
sys.argv=[str(script),'--',str(folder),'--shape']
runpy.run_path(str(script),run_name='__main__')
print(json.dumps({'folder':str(folder),'triangles':triangles}),flush=True)
