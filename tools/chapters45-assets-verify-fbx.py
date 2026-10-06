"""Compatibility wrapper: Blender4.4 exposes a missing wm.fbx_import attribute."""
from pathlib import Path
import bpy,importlib.util
path=Path(r'C:\Users\ljh\.codex\skills\blender-asset-validation\scripts\inspect_asset.py')
spec=importlib.util.spec_from_file_location('asset_inspector',path);mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
original=mod.load_asset
def load(path):
 if path.suffix.lower()=='.fbx':
  bpy.ops.wm.read_factory_settings(use_empty=True)
  bpy.ops.import_scene.fbx(filepath=str(path))
 else:original(path)
mod.load_asset=load
mod.main()
