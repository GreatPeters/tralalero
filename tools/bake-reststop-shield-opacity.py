"""Preserve reviewed P01/S02/S10 opacity through the canonical low UV atlas."""
import argparse
from array import array
import json
import runpy
import shutil
import struct
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--asset', choices=('P01','S02','S10'), default='P01')
parser.add_argument('--source-blend', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(args.source_blend).resolve()
output = Path(args.output).resolve()
assert any(part.startswith(args.asset) for part in source.parts)
assert not output.exists(), 'Keep previous opacity candidates'
output.mkdir(parents=True)
textures = output / 'textures'
textures.mkdir()
bpy.ops.wm.open_mainfile(filepath=str(source))
high = bpy.data.objects['Detailed_Source']
low = bpy.data.objects['Asset_Optimized']
before = [tuple(vertex.co) for vertex in low.data.vertices]
material = low.data.materials[0]
assert len(low.data.materials) == 1
shader = next(node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
base_node = shader.inputs['Base Color'].links[0].from_node
assert base_node.type == 'TEX_IMAGE'
original_base = base_node.image
assert tuple(original_base.size) == (2048, 2048)
original_pixels = array('f', [0.]) * len(original_base.pixels)
original_base.pixels.foreach_get(original_pixels)
opacity = bpy.data.images.new('Baked source opacity', 2048, 2048, alpha=False)
opacity.colorspace_settings.name = 'Non-Color'
target = material.node_tree.nodes.new('ShaderNodeTexImage')
target.image = opacity
material.node_tree.nodes.active = target
temporary = []
alpha_values = []
linked_alpha_ranges = []
try:
    for high_material in high.data.materials:
        if high_material is None:
            continue
        high_shader = next(node for node in high_material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
        surface = next(node for node in high_material.node_tree.nodes if node.type == 'OUTPUT_MATERIAL' and node.is_active_output)
        original_socket = surface.inputs['Surface'].links[0].from_socket
        emission = high_material.node_tree.nodes.new('ShaderNodeEmission')
        alpha = high_shader.inputs['Alpha']
        if alpha.is_linked:
            high_material.node_tree.links.new(alpha.links[0].from_socket, emission.inputs['Color'])
            if args.asset == 'S10':
                # Fresh GLB import represents the inspected .28 factor as
                # texture Alpha multiplied by the factor, not a socket default.
                node = alpha.links[0].from_node
                assert node.type == 'MATH' and node.operation == 'MULTIPLY'
                assert node.inputs[0].is_linked and not node.inputs[1].is_linked
                link = node.inputs[0].links[0]
                assert link.from_node.type == 'TEX_IMAGE' and link.from_socket.name == 'Alpha'
                factor = float(node.inputs[1].default_value)
                assert abs(factor - .28) < 1e-5
                pixels = array('f', [0.]) * len(link.from_node.image.pixels)
                link.from_node.image.pixels.foreach_get(pixels)
                values = pixels[3::4]
                alpha_range = [min(values) * factor, max(values) * factor]
                assert 0 <= alpha_range[0] <= alpha_range[1] <= .3
                linked_alpha_ranges.append({'material': high_material.name, 'range': alpha_range})
                alpha_values.append(alpha_range[1])
        else:
            value = float(alpha.default_value)
            alpha_values.append(value)
            emission.inputs['Color'].default_value = (value, value, value, 1)
        high_material.node_tree.links.new(emission.outputs[0], surface.inputs['Surface'])
        temporary.append((high_material, surface, emission, original_socket))
    assert any(value < .3 for value in alpha_values) and any(value > .99 for value in alpha_values)
    points = [low.matrix_world @ vertex.co for vertex in low.data.vertices]
    lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
    hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
    diagonal = (hi - lo).length
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 8
    scene.render.bake.use_selected_to_active = True
    scene.render.bake.cage_extrusion = diagonal * .004
    scene.render.bake.max_ray_distance = diagonal * .02
    scene.render.bake.margin = 16
    bpy.ops.object.select_all(action='DESELECT')
    low.hide_set(False)
    high.hide_set(False)
    high.hide_render = False
    high.select_set(True)
    low.select_set(True)
    bpy.context.view_layer.objects.active = low
    bpy.ops.object.bake(type='EMIT')
finally:
    for high_material, surface, emission, original_socket in temporary:
        high_material.node_tree.nodes.remove(emission)
        high_material.node_tree.links.new(original_socket, surface.inputs['Surface'])
    high.hide_render = True
    high.hide_set(True)
opacity.filepath_raw = str(textures / 'Opacity.png')
opacity.file_format = 'PNG'
opacity.save()
alpha_pixels = array('f', [0.]) * len(opacity.pixels)
opacity.pixels.foreach_get(alpha_pixels)
combined = array('f', original_pixels)
for index in range(3, len(combined), 4):
    combined[index] = min(1., max(0., alpha_pixels[index - 3]))
base = bpy.data.images.new('BaseColor with preserved opacity', 2048, 2048, alpha=True)
base.colorspace_settings.name = original_base.colorspace_settings.name
base.alpha_mode = 'STRAIGHT'
base.pixels.foreach_set(combined)
base.filepath_raw = str(textures / 'BaseColor.png')
base.file_format = 'PNG'
base.save()
base_node.image = base
material.node_tree.links.new(base_node.outputs['Alpha'], shader.inputs['Alpha'])
if hasattr(material, 'surface_render_method'):
    material.surface_render_method = 'DITHERED'
material.use_backface_culling = False
for name in ('Roughness.png', 'Metallic.png', 'Normal.png'):
    shutil.copy2(source.parent / 'textures' / name, textures / name)
assert before == [tuple(vertex.co) for vertex in low.data.vertices]
bpy.ops.object.select_all(action='DESELECT')
low.select_set(True)
bpy.context.view_layer.objects.active = low
scene.render.bake.use_selected_to_active = False
bpy.ops.export_scene.gltf(filepath=str(output / 'model.glb'), use_selection=True, export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output / 'model.fbx'), use_selection=True, object_types={'MESH'},
                         axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True, add_leaf_bones=False)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'model.blend'))
raw = (output / 'model.glb').read_bytes()
length, kind = struct.unpack_from('<II', raw, 12)
gltf = json.loads(raw[20:20+length])
assert any(value.get('alphaMode') == 'BLEND' for value in gltf['materials'])
shutil.copy2(source.parent / 'trellis_source.glb', output / 'trellis_source.glb')
(output / 'repair.json').write_text(json.dumps({'source_blend': str(source),
    'method': 'Bake only high-source opacity to the canonical low UV atlas; combine it with unchanged BaseColor RGB and retain the existing low geometry/PBR channels',
    'source_alpha_values': alpha_values, 'opacity_map': 'textures/Opacity.png', 'texture_size': 2048,
    'linked_source_alpha_ranges': linked_alpha_ranges,
    'geometry_and_uvs_unchanged': True, 'extra_ai_requests': 0, 'extra_reduction_attempts': 0,
    'material_note': {'P01': 'Use alpha blending with BaseColor alpha. The center reinforcement/rim/grip remain opaque in the map.',
                     'S02': 'Use alpha blending with BaseColor alpha. The frame, base and both shelves must remain opaque; recheck visibility after fresh GLB/FBX import.',
                     'S10': 'Use alpha blending with BaseColor alpha. The cap and label remain opaque; exposed PET is nonmetallic and translucent. Verify both formats on contrasting backgrounds.'}[args.asset]}, indent=2), encoding='utf8')
script = ROOT / 'tools/reststop-production-review-render.py'
sys.argv = [str(script), '--', str(output)]
runpy.run_path(str(script), run_name='__main__')
shutil.copy2(output / 'quality/front.png', output / 'preview.png')
