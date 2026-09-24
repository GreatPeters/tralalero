"""Repair S02 edge fans before local reduction; keep both failed canonical runs."""
import argparse
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import runpy
import shutil
import sys

import bmesh
import bpy
from mathutils import Vector

parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=Path(__file__).resolve().parents[1]
out=root/'outputs/reststop-production-2026-09-24'
source,output=Path(args.source).resolve(),Path(args.output).resolve()
assert source.is_relative_to(out/'manual') and 'S02' in str(source)
assert output.is_relative_to(out/'manual') and not output.exists()
assert json.loads((source.parent/'visual-review.json').read_text(encoding='utf8'))['verdict']=='pass'
key=hashlib.sha256((out/'inputs/S02.png').read_bytes()).hexdigest()
ledger=json.loads((out/'manual-refinement-ledgers'/(key+'.json')).read_text(encoding='utf8'))
assert len(ledger['lows'])==2 and all(r['state']=='failed' for r in ledger['lows'])
assert ledger['source_sha256']==hashlib.sha256(source.read_bytes()).hexdigest()
output.mkdir(parents=True)
app=next(p for p in (Path.home()/'Desktop').glob('AI */Trellis */*') if (p/'engine.py').is_file())
spec=importlib.util.spec_from_file_location('asset_refiner',app/'blender_refine.py')
refiner=importlib.util.module_from_spec(spec);spec.loader.exec_module(refiner)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]
bpy.ops.object.join()
high=bpy.context.object;high.name='Detailed_Source'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
lo,hi=refiner.bounds(high);diagonal=(hi-lo).length
offset=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for vertex in high.data.vertices:vertex.co-=offset
low=high.copy();low.data=high.data.copy();low.name='Asset_Optimized'
bpy.context.collection.objects.link(low);refiner.active(low)
refiner.clean(low,diagonal*2e-6,merge=True)
before=refiner.inspect(low)
assert before['nonmanifold_edges']>0
positions={tuple(v.co) for v in low.data.vertices}
bm=bmesh.new();bm.from_mesh(low.data)
bad=[edge for edge in bm.edges if len(edge.link_faces)>2]
split_count=len(bad);assert split_count
bmesh.ops.split_edges(bm,edges=bad)
bm.to_mesh(low.data);bm.free();low.data.update()
assert positions=={tuple(v.co) for v in low.data.vertices}
refiner.clean(low,diagonal*3e-7,merge=False)
after_split=refiner.inspect(low)
assert after_split['nonmanifold_edges']==0
report={'source':str(source),'method':'Keep the reviewed high PBR source intact; weld only the low copy, split observed non-manifold edge fans, reduce without re-welding, then bake a new atlas',
        'before':before,'split_edges':split_count,'after_split':after_split,
        'split_did_not_move_surface':True,'original_canonical_attempts_preserved':2,
        'extra_ai_requests':0,'reduction_metrics':[]}
for index in range(3):
    count=refiner.tris(low)
    if count<=15000:break
    modifier=low.modifiers.new('Budget after edge-fan correction','DECIMATE')
    modifier.ratio=max(.001,14872/count)
    modifier.use_collapse_triangulate=True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    refiner.clean(low,diagonal*3e-7,merge=False)
    metric=refiner.inspect(low);report['reduction_metrics'].append(metric)
    print('DISPLAY_REDUCTION '+json.dumps(metric),flush=True)
(output/'repair.json').write_text(json.dumps(report,indent=2),encoding='utf8')
if refiner.tris(low)>15000:
    bpy.ops.wm.save_as_mainfile(filepath=str(output/'failed-state.blend'))
    raise RuntimeError('Corrected display still exceeds the saved triangle budget')
for polygon in low.data.polygons:polygon.use_smooth=True
low.data.set_sharp_from_angle(angle=math.radians(50))
low.data.normals_split_custom_set([(0.,0.,0.)]*len(low.data.loops))
for layer in list(low.data.uv_layers):low.data.uv_layers.remove(layer)
low.data.uv_layers.new(name='UVMap')
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006)
bpy.ops.object.mode_set(mode='OBJECT')
refiner.bake(high,low,output,2048,diagonal)
refiner.active(low)
metric=refiner.inspect(low)
assert metric['finite'] and metric['uv'] and metric['triangles']<=15000
assert not metric['degenerate_faces'] and not metric['loose_vertices']
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'),use_selection=True,export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output/'model.fbx'),use_selection=True,object_types={'MESH'},
    axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,add_leaf_bones=False)
refiner.studio(low,output)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(output/'model.blend'))
shutil.copy2(source,output/'trellis_source.glb')
shutil.copy2(source.parent/'repair.json',output/'assembly-repair.json')
for name in ('original-textured-body.glb','recovered-shape-before-materials.glb','partition.json'):
    shutil.copy2(source.parent/name,output/name)
report.update(final_metrics=metric,opacity_restoration_required=True)
(output/'repair.json').write_text(json.dumps(report,indent=2),encoding='utf8')
script=root/'tools/reststop-production-review-render.py'
sys.argv=[str(script),'--',str(output)]
runpy.run_path(str(script),run_name='__main__')
shutil.copy2(output/'quality/opposite.png',output/'preview.png')
print(json.dumps(report),flush=True)
