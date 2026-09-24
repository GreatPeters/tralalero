"""Preview the installed refiner's high-source preparation without decimation."""
import argparse
import importlib.util
import json
import math
from pathlib import Path
import runpy
import sys

import bpy

parser=argparse.ArgumentParser();parser.add_argument('--source',required=True);parser.add_argument('--output',required=True)
parser.add_argument('--reset-custom-normals',action='store_true',help='Explicit local diagnostic beyond the standard preparation')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=Path(__file__).resolve().parents[1];source,output=Path(args.source).resolve(),Path(args.output).resolve()
assert source.is_relative_to(root/'outputs') and output.is_relative_to(root/'outputs') and not output.exists()
output.mkdir(parents=True)
app=next(p for p in (Path.home()/'Desktop').glob('AI */Trellis */*') if (p/'engine.py').is_file())
spec=importlib.util.spec_from_file_location('source_refiner',app/'blender_refine.py')
refiner=importlib.util.module_from_spec(spec);spec.loader.exec_module(refiner)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:obj.select_set(True)
bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();obj=bpy.context.object
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
lo,hi=refiner.bounds(obj);before=refiner.inspect(obj)
refiner.clean(obj,(hi-lo).length*2e-6,merge=True)
for p in obj.data.polygons:p.use_smooth=True
obj.data.set_sharp_from_angle(angle=math.radians(50))
if args.reset_custom_normals:obj.data.normals_split_custom_set([(0.,0.,0.)]*len(obj.data.loops))
after=refiner.inspect(obj)
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'),use_selection=True,export_format='GLB')
(output/'diagnostic.json').write_text(json.dumps({'source':str(source),'scope':'High-source cleanup and smooth/50degree sharp diagnostic; no decimation, no AI call and no asset selection','reset_custom_normals':args.reset_custom_normals,'before':before,'after':after},indent=2),encoding='utf8')
script=root/'tools/reststop-production-review-render.py';sys.argv=[str(script),'--',str(output)]
runpy.run_path(str(script),run_name='__main__')
print(json.dumps({'before':before,'after':after}),flush=True)
