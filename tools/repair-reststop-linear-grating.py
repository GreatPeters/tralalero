"""Keep the generated lower channel; replace the capped upper grate with open slots."""
import argparse
import json
import math
from pathlib import Path
import runpy
import shutil
import sys

import bmesh
import bpy
from mathutils import Vector
from reststop_mesh_cleanup import clean_degenerate_preserving_normals

root = Path(__file__).resolve().parents[1]
out = root / 'outputs/reststop-production-2026-09-24'
source = out / 'assets/R12_a403249561/stages/m1/model.glb'
parser = argparse.ArgumentParser()
parser.add_argument('--revision', type=int, required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
assert args.revision >= 2
folder = out / ('manual/R12-r' + str(args.revision)) / 'source'
assert not folder.exists()
folder.mkdir(parents=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
body = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
bpy.context.view_layer.objects.active = body
body.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
points = [vertex.co.copy() for vertex in body.data.vertices]
lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
long_axis = 0 if hi.x - lo.x > hi.y - lo.y else 1
cross_axis = 1 - long_axis
length, width, height = hi[long_axis] - lo[long_axis], hi[cross_axis] - lo[cross_axis], hi.z - lo.z
center_u, center_v = (lo[long_axis] + hi[long_axis]) * .5, (lo[cross_axis] + hi[cross_axis]) * .5
cut_z = lo.z + height * .60
bm = bmesh.new()
bm.from_mesh(body.data)
bmesh.ops.bisect_plane(bm, geom=list(bm.verts) + list(bm.edges) + list(bm.faces),
    plane_co=(0, 0, cut_z), plane_no=(0, 0, 1), clear_outer=True, dist=height * 1e-6)
wire = [edge for edge in bm.edges if not edge.link_faces]
if wire:
    bmesh.ops.delete(bm, geom=wire, context='EDGES')
loose = [vertex for vertex in bm.verts if not vertex.link_edges]
if loose:
    bmesh.ops.delete(bm, geom=loose, context='VERTS')
bm.to_mesh(body.data)
bm.free()
body.data.update()
body.name = 'Retained TRELLIS lower channel and supports'

def xyz(u, v, z):
    value = [0., 0., z]
    value[long_axis], value[cross_axis] = center_u + u, center_v + v
    return value

for side in (-1, 1):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz(0, side * width * .455, (cut_z + hi.z) * .5))
    rail = bpy.context.object
    rail.name = 'Restored upper channel rim'
    dimensions = [0., 0., hi.z - cut_z]
    dimensions[long_axis], dimensions[cross_axis] = length, width * .09
    rail.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = rail.modifiers.new('Small rim bevel', 'BEVEL')
    modifier.width, modifier.segments = height * .012, 2
    bpy.ops.object.modifier_apply(modifier=modifier.name)

slots = 35
plate_length, plate_half_width = length * .94, width * .43
pitch = plate_length / slots
radius, straight_half = pitch * .24, width * .30 - pitch * .24
assert straight_half > 0
top_z, bottom_z = hi.z - height * .025, hi.z - height * .10
for side in (-1, 1):
    margin = (length - plate_length) * .5
    bpy.ops.mesh.primitive_cube_add(size=1,
        location=xyz(side * (plate_length * .5 + margin * .5), 0, (top_z + bottom_z) * .5))
    cap = bpy.context.object
    cap.name = 'Flush grate end border'
    dimensions = [0., 0., top_z - bottom_z]
    dimensions[long_axis], dimensions[cross_axis] = margin, plate_half_width * 2
    cap.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = cap.modifiers.new('End border bevel', 'BEVEL')
    modifier.width, modifier.segments = height * .006, 2
    bpy.ops.object.modifier_apply(modifier=modifier.name)
angles = []
for index in range(9):
    theta = math.pi * index / 8
    angles.append(math.atan2(straight_half + radius * math.sin(theta), radius * math.cos(theta)) % (2 * math.pi))
    theta += math.pi
    angles.append(math.atan2(-straight_half + radius * math.sin(theta), radius * math.cos(theta)) % (2 * math.pi))
for x in (-pitch * .5, pitch * .5):
    for y in (-plate_half_width, plate_half_width):
        angles.append(math.atan2(y, x) % (2 * math.pi))
angles = sorted(set(round(angle, 12) for angle in angles))
vertices, faces, index_by_position = [], [], {}

def vertex(point):
    key = tuple(round(value, 10) for value in point)
    if key not in index_by_position:
        index_by_position[key] = len(vertices)
        vertices.append(point)
    return index_by_position[key]

for slot in range(slots):
    u = -plate_length * .5 + pitch * (slot + .5)
    outer, inner = [], []
    for angle in angles:
        dx, dy = math.cos(angle), math.sin(angle)
        outer_distance = min(pitch * .5 / max(abs(dx), 1e-12), plate_half_width / max(abs(dy), 1e-12))
        if abs(dy) * radius <= straight_half * abs(dx):
            inner_distance = radius / max(abs(dx), 1e-12)
        else:
            inner_distance = abs(dy) * straight_half + math.sqrt(max(0., radius * radius - straight_half * straight_half * dx * dx))
        outer.append((u + outer_distance * dx, outer_distance * dy))
        inner.append((u + inner_distance * dx, inner_distance * dy))
    ot = [vertex(xyz(x, y, top_z)) for x, y in outer]
    ob = [vertex(xyz(x, y, bottom_z)) for x, y in outer]
    it = [vertex(xyz(x, y, top_z)) for x, y in inner]
    ib = [vertex(xyz(x, y, bottom_z)) for x, y in inner]
    for i in range(len(angles)):
        j = (i + 1) % len(angles)
        faces.extend(((ot[i], ot[j], it[j], it[i]), (ob[j], ob[i], ib[i], ib[j]), (it[i], it[j], ib[j], ib[i])))
        x1, y1 = outer[i]
        x2, y2 = outer[j]
        side_edge = abs(abs(y1) - plate_half_width) < 1e-8 and abs(y1 - y2) < 1e-8
        end_edge = abs(abs(x1) - plate_length * .5) < 1e-8 and abs(x1 - x2) < 1e-8
        if side_edge or end_edge:
            faces.append((ot[i], ob[i], ob[j], ot[j]))
mesh = bpy.data.meshes.new('Open repeated capsule slots')
mesh.from_pydata(vertices, [], faces)
mesh.update()
plate = bpy.data.objects.new('Restored 35-slot grate', mesh)
bpy.context.collection.objects.link(plate)
bpy.ops.object.select_all(action='DESELECT')
plate.select_set(True)
bpy.context.view_layer.objects.active = plate
bm = bmesh.new()
bm.from_mesh(mesh)
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(mesh)
bm.free()
modifier = plate.modifiers.new('Rounded slot rims', 'BEVEL')
modifier.width, modifier.segments = min(height * .006, pitch * .04), 2
bpy.ops.object.modifier_apply(modifier=modifier.name)
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = body
bpy.ops.object.join()
body.name = 'Recovered linear drain'
body.data.validate(clean_customdata=False)
cleanup = clean_degenerate_preserving_normals(body.data)
bpy.ops.export_scene.gltf(filepath=str(folder / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'model.blend'))
shutil.copy2(source, folder / 'original-trellis-source.glb')
(folder / 'repair.json').write_text(json.dumps({'source': str(source),
    'method': 'Retain generated lower channel/supports; rebuild upper rim and 35 through-slots after postprocessing capped most openings',
    'slots': slots, 'source_dimensions': [length, width, height], 'retained_below_height_fraction': .60,
    'cleanup': cleanup,
    'extra_ai_shapes': 0, 'original_shape_attempts': 1}, indent=2), encoding='utf8')
script = root / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(folder), '--shape']
runpy.run_path(str(script), run_name='__main__')
