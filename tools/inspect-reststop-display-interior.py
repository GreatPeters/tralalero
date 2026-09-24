"""Read-only cutaway views and interior horizontal-surface areas for S02."""
import argparse
import gc
import json
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source, output = Path(args.source).resolve(), Path(args.output).resolve()
assert not output.exists()
output.mkdir(parents=True)
report = {'source': str(source), 'views': {}}
for axis in (0, 1):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    for obj in meshes:
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    points = [vertex.co for obj in meshes for vertex in obj.data.vertices]
    lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
    hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
    center, extent = (lo + hi) * .5, hi - lo
    size = extent.length
    if axis == 0:
        areas = [0.] * 20
        for obj in meshes:
            obj.data.calc_loop_triangles()
            for triangle in obj.data.loop_triangles:
                a, b, c = (obj.data.vertices[index].co for index in triangle.vertices)
                mid = (a + b + c) / 3
                normal = (b - a).cross(c - a)
                if not normal.length or abs(normal.normalized().z) < .9:
                    continue
                if not all(lo[i] + extent[i] * .2 < mid[i] < hi[i] - extent[i] * .2 for i in (0, 1)):
                    continue
                fraction = (mid.z - lo.z) / extent.z
                if .15 < fraction < .85:
                    areas[min(19, int(fraction * 20))] += normal.length * .5
        report.update(bounds_min=list(lo), bounds_max=list(hi), horizontal_interior_area_by_5pct_height=areas)
    material = bpy.data.materials.new('Cutaway clay')
    material.use_nodes = True
    shader = material.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value = (.42, .42, .42, 1)
    shader.inputs['Roughness'].default_value = .75
    plane_normal = Vector((0, 0, 0))
    plane_normal[axis] = 1
    for obj in meshes:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts) + list(bm.edges) + list(bm.faces),
            plane_co=center, plane_no=plane_normal, clear_outer=True, dist=size * 1e-7)
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.materials.clear()
        obj.data.materials.append(material)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 16
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 896, 768
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = 'AgX'
    world = bpy.data.worlds.new('Cutaway world')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .6
    scene.world = world
    for position, power in (((1.5, 1.5, 2), 180), ((-1, -1, 1.5), 100)):
        bpy.ops.object.light_add(type='AREA', location=center + Vector(position) * size)
        light = bpy.context.object
        light.data.energy = power * size * size
        light.data.size = size * 1.5
        light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()
    direction = Vector((.1, .1, .08))
    direction[axis] = 2.3
    lamp_direction = direction.copy()
    lamp_direction.z = .4
    bpy.ops.object.light_add(type='AREA', location=center + lamp_direction * size)
    light = bpy.context.object
    light.data.energy = 180 * size * size
    light.data.size = size * .8
    light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.ops.object.camera_add(location=center + direction * size)
    camera = bpy.context.object
    camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = size * 1.1
    scene.camera = camera
    image_path = output / ('cut-' + 'XY'[axis] + '.png')
    scene.render.filepath = str(image_path)
    bpy.ops.render.render(write_still=True)
    report['views']['XY'[axis]] = str(image_path)
    del points, meshes
    gc.collect()
(output / 'interior-report.json').write_text(json.dumps(report, indent=2), encoding='utf8')
print(json.dumps({key: value for key, value in report.items() if key != 'views'}), flush=True)
