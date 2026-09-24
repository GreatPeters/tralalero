"""Read-only topology evidence for P04 cached stages after seam welding."""
import importlib.util
import json
from pathlib import Path

import bpy

root = Path(__file__).resolve().parents[1]
folder = root / 'outputs/reststop-production-2026-09-24/reviews/P04-cached-stages'
app = next(p for p in (Path.home() / 'Desktop').glob('AI */Trellis */*') if (p / 'engine.py').is_file())
spec = importlib.util.spec_from_file_location('topology_inspection', app / 'blender_refine.py')
refiner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(refiner)
report = {}
for stage in ('217', '193', '161'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(folder / ('stage' + stage) / 'model.glb'))
    obj = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
    before = refiner.inspect(obj)
    lo, hi = refiner.bounds(obj)
    refiner.clean(obj, (hi - lo).length * 2e-6, merge=True)
    after = refiner.inspect(obj)
    report[stage] = {'before': before, 'after_weld_and_normal_recalculation': after}
    print(json.dumps({stage: report[stage]}), flush=True)
(folder / 'topology-diagnosis.json').write_text(json.dumps(report, indent=2), encoding='utf8')
