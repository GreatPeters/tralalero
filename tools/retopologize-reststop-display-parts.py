"""Reduce S02 parts independently and bake only the TRELLIS metal body."""
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
root=Path(__file__).resolve().parents[1];out=root/'outputs/reststop-production-2026-09-24'
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
highs={}
for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
    assert len(obj.data.materials)==1
    mat=obj.data.materials[0]
    shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    if shader.inputs['Base Color'].is_linked:kind='body'
    elif shader.inputs['Alpha'].default_value<.1:kind='glass'
    else:kind='shelves'
    assert kind not in highs
    highs[kind]=obj
    world=obj.matrix_world.copy();obj.parent=None;obj.matrix_world=world
    refiner.active(obj);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
assert set(highs)=={'body','glass','shelves'}
points=[v.co for obj in highs.values() for v in obj.data.vertices]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
diagonal=(hi-lo).length;offset=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for obj in highs.values():
    for vertex in obj.data.vertices:vertex.co-=offset
    obj.hide_render=True;obj.hide_set(True)
budgets={'body':11800,'glass':1400,'shelves':1400}
lows={};reports={}
for kind,high in highs.items():
    low=high.copy();low.data=high.data.copy();low.name='Display_low_'+kind
    bpy.context.collection.objects.link(low)
    low.hide_set(False);low.hide_render=False;refiner.active(low)
    refiner.clean(low,diagonal*2e-6,merge=True)
    before=refiner.inspect(low)
    positions={tuple(v.co) for v in low.data.vertices}
    bm=bmesh.new();bm.from_mesh(low.data)
    bad=[edge for edge in bm.edges if len(edge.link_faces)>2]
    split_count=len(bad)
    if bad:bmesh.ops.split_edges(bm,edges=bad)
    bm.to_mesh(low.data);bm.free();low.data.update()
    assert positions=={tuple(v.co) for v in low.data.vertices}
    refiner.clean(low,diagonal*3e-7,merge=False)
    history=[]
    for _ in range(3):
        count=refiner.tris(low)
        if count<=budgets[kind]:break
        mod=low.modifiers.new('Part budget after edge repair','DECIMATE')
        mod.ratio=max(.001,(budgets[kind]-64)/count);mod.use_collapse_triangulate=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
        refiner.clean(low,diagonal*3e-7,merge=False)
        history.append(refiner.inspect(low))
    metric=refiner.inspect(low)
    reports[kind]={'before':before,'split_edges':split_count,'budget':budgets[kind],
                   'reduction_metrics':history,'after':metric}
    (output/'parts-reduction.json').write_text(json.dumps(reports,indent=2),encoding='utf8')
    if metric['triangles']>budgets[kind]:
        bpy.ops.wm.save_as_mainfile(filepath=str(output/'failed-state.blend'))
        raise RuntimeError('Part still exceeds its budget: '+kind)
    assert metric['triangles']>100 and metric['finite'] and not metric['degenerate_faces']
    assert not metric['loose_vertices'] and not metric['nonmanifold_edges']
    for polygon in low.data.polygons:polygon.use_smooth=True
    low.data.set_sharp_from_angle(angle=math.radians(50))
    low.data.normals_split_custom_set([(0.,0.,0.)]*len(low.data.loops))
    if kind=='shelves':
        mod=low.modifiers.new('Planar stainless shelf normals','WEIGHTED_NORMAL')
        mod.keep_sharp=True;mod.weight=50
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for layer in list(low.data.uv_layers):low.data.uv_layers.remove(layer)
    low.data.uv_layers.new(name='UVMap')
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006)
    bpy.ops.object.mode_set(mode='OBJECT')
    low.hide_set(True);low.hide_render=True
    lows[kind]=low
    print('PART_REDUCTION '+kind+' '+json.dumps(metric),flush=True)

# Only opaque body geometry participates in the texture/normal bake. Clear
# panes and uniform stainless retain their own reviewed constant PBR materials.
for obj in (highs['body'],lows['body']):
    obj.hide_set(False);obj.hide_render=False
refiner.bake(highs['body'],lows['body'],output,2048,diagonal)
bpy.ops.object.select_all(action='DESELECT')
for obj in lows.values():obj.hide_set(False);obj.hide_render=False;obj.select_set(True)
bpy.context.view_layer.objects.active=lows['body'];bpy.ops.object.join()
low=bpy.context.object;low.name='Asset_Optimized'
stats=refiner.inspect(low)
assert stats['triangles']<=15000 and stats['finite'] and stats['uv']
assert not stats['degenerate_faces'] and not stats['loose_vertices']
assert len(low.data.materials)==3
bpy.ops.object.select_all(action='DESELECT')
for obj in highs.values():obj.hide_set(False);obj.select_set(True)
bpy.context.view_layer.objects.active=highs['body'];bpy.ops.object.join()
high=bpy.context.object;high.name='Detailed_Source';high.hide_set(True);high.hide_render=True
refiner.active(low)
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'),use_selection=True,export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output/'model.fbx'),use_selection=True,object_types={'MESH'},
    axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,add_leaf_bones=False)
refiner.studio(low,output)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(output/'model.blend'))
shutil.copy2(source,output/'trellis_source.glb')
shutil.copy2(source.parent/'repair.json',output/'assembly-repair.json')
for name in ('original-textured-body.glb','recovered-shape-before-materials.glb','partition.json'):
    shutil.copy2(source.parent/name,output/name)
report={'source':str(source),'method':'Independently repair/reduce semantic body, glass and shelf parts; bake only the opaque TRELLIS body, retain separate constant glass and stainless materials',
        'parts':reports,'final_metrics':stats,'materials':3,'original_canonical_attempts_preserved':2,
        'extra_ai_requests':0,'requires_fresh_import_and_visibility_review':True}
(output/'repair.json').write_text(json.dumps(report,indent=2),encoding='utf8')
script=root/'tools/reststop-production-review-render.py';sys.argv=[str(script),'--',str(output)]
runpy.run_path(str(script),run_name='__main__')
shutil.copy2(output/'quality/opposite.png',output/'preview.png')
print(json.dumps(report),flush=True)
