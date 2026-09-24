"""Fresh-import P01 opacity checks and a colored background visibility proof."""
import argparse
from array import array
import json
from pathlib import Path
import sys

import bpy
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('--folder', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
folder = Path(args.folder).resolve()
assert folder.name.startswith('P01')
report = {'formats': {}, 'ok': True}
for extension in ('glb', 'fbx'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path = str(folder / ('model.' + extension))
    if extension == 'glb':
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        bpy.ops.import_scene.fbx(filepath=path)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    materials = {mat for obj in meshes for mat in obj.data.materials if mat}
    evidence = []
    for material in materials:
        shader = next(node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
        alpha = shader.inputs['Alpha']
        assert alpha.is_linked, (extension, material.name, 'Alpha not linked')
        texture = alpha.links[0].from_node
        assert texture.type == 'TEX_IMAGE' and texture.image is not None
        pixels = array('f', [0.]) * len(texture.image.pixels)
        texture.image.pixels.foreach_get(pixels)
        values = pixels[3::4]
        clear_pixels = sum(.15 < value < .21 for value in values)
        opaque_pixels = sum(value > .99 for value in values)
        assert clear_pixels > 100000 and opaque_pixels > 100000
        evidence.append({'material': material.name, 'alpha_socket': alpha.links[0].from_socket.name,
                         'size': list(texture.image.size), 'clear_pixels': clear_pixels,
                         'opaque_pixels': opaque_pixels})
    bpy.context.view_layer.update()
    points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
    hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
    center = (lo + hi) * .5
    size = (hi - lo).length
    height = hi.z - lo.z
    for index, color in enumerate(((.025, .45, .1, 1), (.55, .035, .07, 1))):
        material = bpy.data.materials.new('Visibility background ' + str(index))
        material.use_nodes = True
        nodes = material.node_tree.nodes
        nodes.clear()
        emission = nodes.new('ShaderNodeEmission')
        emission.inputs['Color'].default_value = color
        output = nodes.new('ShaderNodeOutputMaterial')
        material.node_tree.links.new(emission.outputs[0], output.inputs['Surface'])
        bpy.ops.mesh.primitive_cube_add(size=1, location=(center.x + (index - .5) * height * .7,
                                                        hi.y + height * .25, center.z))
        panel = bpy.context.object
        panel.scale = (height * .7, height * .015, height * 1.15)
        panel.data.materials.append(material)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.cycles.transparent_max_bounces = 16
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = 'AgX'
    world = bpy.data.worlds.new('Opacity proof world')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .25
    scene.world = world
    bpy.ops.object.light_add(type='AREA', location=center + Vector((-1, -2, 2)) * size)
    light = bpy.context.object
    light.data.energy = 100 * size * size
    light.data.size = size * 2
    light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.ops.object.camera_add(location=center + Vector((0, -2.3, 0)) * size)
    camera = bpy.context.object
    camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = height * 1.3
    scene.camera = camera
    scene.render.filepath = str(folder / 'quality' / ('opacity-proof-' + extension + '.png'))
    bpy.ops.render.render(write_still=True)
    report['formats'][extension] = {'materials': evidence, 'proof': scene.render.filepath}
(folder / 'transparency-validation.json').write_text(json.dumps(report, indent=2), encoding='utf8')
print(json.dumps(report), flush=True)
