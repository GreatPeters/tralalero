"""Fresh-import technical and standard-view evidence for the chapter candidates."""
import argparse
import importlib.util
import sys
from pathlib import Path
import bpy

parser = argparse.ArgumentParser()
parser.add_argument('--root', required=True)
parser.add_argument('--kind', choices=['rigs', 'props'], required=True)
parser.add_argument('--evidence-tag', default='fresh')
parser.add_argument('--props-revision', default='v3')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
root = Path(args.root)
skill = Path('C:/Users/ljh/.codex/skills/blender-asset-validation/scripts')

def module(name):
    spec = importlib.util.spec_from_file_location(name, skill/(name+'.py'))
    loaded = importlib.util.module_from_spec(spec)
    sys.modules[name] = loaded
    spec.loader.exec_module(loaded)
    return loaded

inspect = module('inspect_asset')
render = module('render_evidence')
configure = render.configure_scene
load = render.load_asset
action_name = ''

def bounded_scene(*values):
    configure(*values)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 12
    scene.cycles.use_denoising = True
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 2

def load_pose(path):
    if action_name:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.context.scene.render.fps = 30
        bpy.ops.import_scene.gltf(filepath=str(path))
    else:
        load(path)
    if action_name:
        rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
        rig.animation_data.action = bpy.data.actions[action_name]
        bpy.context.scene.frame_set(1)
        bpy.context.view_layer.update()
        # glTF's bone-display helper is created on pose evaluation. It is not
        # exported geometry and must not move the diagnostic floor down to -1m.
        for bone in rig.pose.bones:
            if bone.custom_shape is not None:
                bone.custom_shape.hide_render = True

render.configure_scene = bounded_scene
render.load_asset = load_pose
folder = root/'outputs/chapters-polish-2026-09-12'/('rigged/v2' if args.kind == 'rigs' else 'props-'+args.props_revision)
for candidate in sorted(folder.iterdir()):
    if not candidate.is_dir():
        continue
    asset = candidate/((candidate.name if args.kind == 'rigs' else 'Model')+'.glb')
    if not asset.exists():
        continue
    report = candidate/'fresh-inspection.json'
    if not report.exists():
        sys.argv = ['inspect_asset', '--', '--input', str(asset), '--output', str(report)]
        inspect.main()
    for action_name in (['walk', 'attack_once'] if args.kind == 'rigs' else ['']):
        evidence = candidate/('evidence-'+args.evidence_tag+'-'+(action_name or 'model'))
        if (evidence/'evidence.json').exists():
            continue
        sys.argv = ['render_evidence', '--', '--input', str(asset), '--output-dir', str(evidence), '--resolution', '384']
        if action_name:
            sys.argv += ['--frames', '1,8,16,24,31']
        render.main()
        print('REVIEW_READY '+str(evidence), flush=True)
