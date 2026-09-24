"""Keep S08 inserts exact; allocate the remaining game budget to the AI body."""
import argparse
import importlib.util
import json
import math
from pathlib import Path
import shutil
import sys

import bmesh
import bpy
from mathutils import Vector

parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True);parser.add_argument('--output',required=True)
parser.add_argument('--target',type=int,required=True);parser.add_argument('--texture-size',type=int,required=True)
parser.add_argument('--profile',choices=('parts','preserve_details'),required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
source,output=Path(args.source).resolve(),Path(args.output).resolve()
assert 'S08' in str(source) and args.target==15000 and args.texture_size==2048
assert not (output/'model.blend').exists();output.mkdir(parents=True,exist_ok=True)
layout=json.loads((source.parent/'fridge-layout.json').read_text(encoding='utf8'))
app=next(p for p in (Path.home()/'Desktop').glob('AI */Trellis */*') if (p/'engine.py').is_file())
spec=importlib.util.spec_from_file_location('asset_refiner',app/'blender_refine.py')
refiner=importlib.util.module_from_spec(spec);spec.loader.exec_module(refiner)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
transforms={obj:obj.matrix_world.copy() for obj in objects}
for obj in objects:
    obj.parent=None;obj.matrix_world=transforms[obj];refiner.active(obj)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
def textured(obj):
    return any(next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].is_linked for mat in obj.data.materials)
bodies=[obj for obj in objects if textured(obj)];assert len(bodies)==1
high=bodies[0];fixed=[obj for obj in objects if obj!=high]
fixed_triangles=sum(refiner.tris(obj) for obj in fixed)
assert fixed_triangles==layout['fixed_triangles']==2664
points=[v.co for obj in objects for v in obj.data.vertices]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
diagonal=(hi-lo).length;offset=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for obj in objects:
    for vertex in obj.data.vertices:vertex.co-=offset
    obj.hide_set(True);obj.hide_render=True
layout['coordinate_transform']['floor_offset']=list(Vector(layout['coordinate_transform']['floor_offset'])+offset)
low=high.copy();low.data=high.data.copy();bpy.context.collection.objects.link(low)
low.name='Fridge_low_body';low.hide_set(False);low.hide_render=False;refiner.active(low)
refiner.clean(low,diagonal*2e-6,merge=True)
before=refiner.inspect(low);warnings=[]
priority=refiner.reduction_priority(low,args.profile,warnings)
positions={tuple(v.co) for v in low.data.vertices}
bm=bmesh.new();bm.from_mesh(low.data)
bad=[edge for edge in bm.edges if len(edge.link_faces)>2];split_count=len(bad)
if bad:bmesh.ops.split_edges(bm,edges=bad)
bm.to_mesh(low.data);bm.free();low.data.update()
assert positions=={tuple(v.co) for v in low.data.vertices}
refiner.clean(low,diagonal*3e-7,merge=False)
body_budget=args.target-fixed_triangles-128
history=[]
for _ in range(4):
    count=refiner.tris(low)
    if count<=body_budget:break
    mod=low.modifiers.new('Body budget preserving fixed fridge inserts','DECIMATE')
    mod.ratio=max(.001,(body_budget-64)/count);mod.use_collapse_triangulate=True
    if priority:mod.vertex_group=priority;mod.vertex_group_factor=.035 if args.profile=='preserve_details' else .02
    bpy.ops.object.modifier_apply(modifier=mod.name);refiner.clean(low,diagonal*3e-7,merge=False)
    metric=refiner.inspect(low);history.append(metric);print('BODY_REDUCTION '+json.dumps(metric),flush=True)
report={'source':str(source),'method':'Repair/reduce only the TRELLIS body; retain all fitted transparent/liner/perforated/light insert vertices and faces',
        'target_triangles':args.target,'body_budget':body_budget,'fixed_triangles':fixed_triangles,'before':before,
        'split_edges':split_count,'reduction_metrics':history,'profile':args.profile,'warnings':warnings,'extra_ai_requests':0}
(output/'repair.json').write_text(json.dumps(report,indent=2),encoding='utf8')
if refiner.tris(low)>body_budget:
    bpy.ops.wm.save_as_mainfile(filepath=str(output/'failed-state.blend'))
    raise RuntimeError('Fridge body exceeds its share of the preserved whole-asset budget')
for p in low.data.polygons:p.use_smooth=True
low.data.set_sharp_from_angle(angle=math.radians(50));low.data.normals_split_custom_set([(0.,0.,0.)]*len(low.data.loops))
for layer in list(low.data.uv_layers):low.data.uv_layers.remove(layer)
low.data.uv_layers.new(name='UVMap');refiner.active(low)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT')
for obj in (high,low):obj.hide_set(False);obj.hide_render=False
refiner.bake(high,low,output,args.texture_size,diagonal)
high_fixed=[]
for obj in fixed:
    duplicate=obj.copy();duplicate.data=obj.data.copy();bpy.context.collection.objects.link(duplicate)
    duplicate.hide_set(True);duplicate.hide_render=True;high_fixed.append(duplicate)
bpy.ops.object.select_all(action='DESELECT')
for obj in [low]+fixed:obj.hide_set(False);obj.hide_render=False;obj.select_set(True)
bpy.context.view_layer.objects.active=low;bpy.ops.object.join();low.name='Asset_Optimized'
stats=refiner.inspect(low);assert stats['triangles']<=args.target and stats['finite'] and stats['uv']
assert not stats['degenerate_faces'] and not stats['loose_vertices']
assert len(low.data.materials)==4
bpy.ops.object.select_all(action='DESELECT')
for obj in [high]+high_fixed:obj.hide_set(False);obj.select_set(True)
bpy.context.view_layer.objects.active=high;bpy.ops.object.join();high.name='Detailed_Source';high.hide_set(True);high.hide_render=True
refiner.active(low)
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'),use_selection=True,export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output/'model.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,add_leaf_bones=False)
refiner.studio(low,output);bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(output/'model.blend'))
(output/'fridge-layout.json').write_text(json.dumps(layout,indent=2),encoding='utf8')
for name in ('original-textured-body.glb','recovered-shape-before-materials.glb','original-shape.glb','geometry-normalization-proof.json','cleanup.json'):
    if (source.parent/name).exists():shutil.copy2(source.parent/name,output/name)
report.update(final_metrics=stats,materials=4,fixed_inserts_not_decimated=True)
(output/'repair.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report),flush=True)
