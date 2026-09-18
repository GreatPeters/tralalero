"""Use the standard evidence cameras/lights with a bounded CPU render engine."""
import importlib.util
from pathlib import Path
import bpy

path = Path('C:/Users/ljh/.codex/skills/blender-asset-validation/scripts/render_evidence.py')
spec = importlib.util.spec_from_file_location('asset_evidence', path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
original = module.configure_scene
def configure(*args):
    original(*args)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 12
    scene.cycles.use_denoising = True
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 2
module.configure_scene = configure
module.main()
