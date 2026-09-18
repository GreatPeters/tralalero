"""Use the shared evidence renderer with CPU Cycles so TRELLIS owns the GPU."""
import importlib.util
from pathlib import Path
import sys
import bpy

action_name = None
if '--action' in sys.argv:
    index = sys.argv.index('--action')
    action_name = sys.argv[index + 1]
    del sys.argv[index:index + 2]
neutral = '--neutral' in sys.argv
if neutral:
    sys.argv.remove('--neutral')
source = Path('C:/Users/ljh/.codex/skills/blender-asset-validation/scripts/render_evidence.py')
spec = importlib.util.spec_from_file_location('task_evidence', source)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
original_configure = module.configure_scene
original_load = module.load_asset

def configure(*args):
    original_configure(*args)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 12
    scene.cycles.use_denoising = True
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 2
    # The shared studio places lights proportional to extent but keeps wattage fixed.
    # Normalize illuminance for counter modules much larger than the human pilot.
    extent = args[1]
    if extent > 3:
        for obj in scene.objects:
            if obj.type == 'LIGHT' and obj.name.startswith('BAS_'):
                obj.data.energy *= (extent / 2.1) ** 2

def load(path):
    original_load(path)
    rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
    for rig in rigs:
        for bone in rig.pose.bones:
            if bone.custom_shape:
                bone.custom_shape.hide_render = True
        if action_name:
            rig.animation_data.action = next(a for a in bpy.data.actions if a.name.split('|')[-1] == action_name)
    bpy.context.scene.frame_set(1)
    if neutral:
        material = bpy.data.materials.new('Neutral proportion review')
        material.use_nodes = True
        material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.45, .47, .50, 1)
        for obj in bpy.context.scene.objects:
            if obj.type == 'MESH' and obj.name.endswith('_Body'):
                obj.data.materials.clear()
                obj.data.materials.append(material)
module.configure_scene = configure
module.load_asset = load
module.main()
