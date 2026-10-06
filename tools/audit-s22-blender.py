"""Compatibility wrapper for Blender 4.4's registered FBX importer."""
import importlib.util
from pathlib import Path
import bpy

skill=Path('C:/Users/ljh/.codex/skills/blender-asset-validation/scripts/inspect_asset.py')
spec=importlib.util.spec_from_file_location('asset_inspector',skill)
module=importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
original=module.load_asset
def load_asset(path):
    if path.suffix.lower()=='.fbx':
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(path))
    else:
        original(path)
module.load_asset=load_asset
module.main()
