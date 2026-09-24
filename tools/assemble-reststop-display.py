"""Recombine a reviewed textured cabinet body with its separate fixed parts."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import shutil
import sys

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser()
parser.add_argument('--asset',choices=('S02','S08'),default='S02')
parser.add_argument('--source-folder',required=True)
parser.add_argument('--body-texture',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=Path(__file__).resolve().parents[1]
source,texture,output=(Path(value).resolve() for value in (args.source_folder,args.body_texture,args.output))
assert all(p.is_relative_to(root/'outputs') for p in (source,texture,output))
assert args.asset in str(source) and not output.exists()
scope=json.loads((source/'texture-scope.json').read_text(encoding='utf8'))
assert scope['texture_scope']=='opaque body only'
assert hashlib.sha256((source/'model.glb').read_bytes()).hexdigest()==scope['body_sha256']
assert json.loads((texture.parent/'visual-review.json').read_text(encoding='utf8'))['verdict']=='pass'
assert json.loads((texture.parent/'trellis_history.json').read_text(encoding='utf8'))['status']['status_str']=='success'
submitted=json.loads((texture.parent/'submitted_prompt.json').read_text(encoding='utf8'))
assert Path(submitted['700']['inputs']['glb_path']).resolve()==source/'model.glb'
output.mkdir(parents=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def load(path):
    before=set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(path))
    objects=[o for o in set(bpy.data.objects)-before if o.type=='MESH']
    assert objects
    transforms={obj:obj.matrix_world.copy() for obj in objects}
    for obj in objects:
        obj.parent=None
        obj.matrix_world=transforms[obj]
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    return objects

def bounds(objects):
    points=[v.co.copy() for o in objects for v in o.data.vertices]
    lo=Vector(tuple(min(v[i] for v in points) for i in range(3)))
    hi=Vector(tuple(max(v[i] for v in points) for i in range(3)))
    return lo,hi,points

original=load(source/'model.glb')
old_lo,old_hi,old_points=bounds(original)
original_body_triangles=sum(len(p.vertices)-2 for obj in original for p in obj.data.polygons)
body=load(texture)
textured_body_triangles=sum(len(p.vertices)-2 for obj in body for p in obj.data.polygons)
cleanup_proof=None
if textured_body_triangles!=original_body_triangles:
    cleanup_proof=json.loads((texture.parent/'geometry-normalization-proof.json').read_text(encoding='utf8'))
    assert cleanup_proof['ok'] and cleanup_proof['extra_triangle_count']==0
    assert cleanup_proof['source_sha256']==scope['body_sha256']
    assert cleanup_proof['textured_sha256']==hashlib.sha256(texture.read_bytes()).hexdigest()
    assert cleanup_proof['source_triangles']==original_body_triangles and cleanup_proof['textured_triangles']==textured_body_triangles
    assert cleanup_proof['max_missing_area']<cleanup_proof['diagonal']**2*1e-10
new_lo,new_hi,_=bounds(body)
ratios=[(new_hi[i]-new_lo[i])/(old_hi[i]-old_lo[i]) for i in range(3)]
scale=sum(ratios)/3
assert scale>0 and max(abs(value/scale-1) for value in ratios)<1e-4, ratios
old_center,new_center=(old_lo+old_hi)/2,(new_lo+new_hi)/2
verts,faces=[],[]
for obj in body:
    offset=len(verts)
    verts.extend(v.co.copy() for v in obj.data.vertices)
    faces.extend(tuple(i+offset for i in p.vertices) for p in obj.data.polygons)
tree=BVHTree.FromPolygons(verts,faces)
sample=old_points[::max(1,len(old_points)//1000)]
residuals=[tree.find_nearest((v-old_center)*scale+new_center)[3] for v in sample]
assert max(residuals)<(new_hi-new_lo).length*1e-4, max(residuals)
for obj in original:
    bpy.data.objects.remove(obj,do_unlink=True)
parts=[]
part_inputs=[('fixed','fixed-parts.glb')] if args.asset=='S08' else [('glass','glass-preserved.glb'),('shelves','shelves-preserved.glb')]
for name,filename in part_inputs:
    objects=load(source/filename)
    for obj in objects:
        for vertex in obj.data.vertices:
            vertex.co=(vertex.co-old_center)*scale+new_center
        obj.data.update()
        if args.asset=='S02':obj.name='Display_'+name
        if name=='shelves':
            material=bpy.data.materials.new('Stainless shelf surfaces - local PBR')
            material.use_nodes=True
            shader=material.node_tree.nodes['Principled BSDF']
            shader.inputs['Base Color'].default_value=(.42,.44,.46,1)
            shader.inputs['Metallic'].default_value=.85
            shader.inputs['Roughness'].default_value=.26
            obj.data.materials.clear()
            obj.data.materials.append(material)
            for p in obj.data.polygons:p.material_index=0
    parts+=objects
objects=body+parts
triangles=sum(len(p.vertices)-2 for o in objects for p in o.data.polygons)
expected=(textured_body_triangles+scope['fixed_triangles'] if args.asset=='S08'
          else json.loads((source/'partition.json').read_text(encoding='utf8'))['triangles'])
assert triangles==expected and triangles<=300000
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:obj.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output/'model.blend'))
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'),use_selection=True,export_format='GLB')
shutil.copy2(texture,output/'original-textured-body.glb')
if cleanup_proof:shutil.copy2(texture.parent/'geometry-normalization-proof.json',output/'geometry-normalization-proof.json')
if args.asset=='S08':
    shutil.copy2(source/'prototype/model.glb',output/'recovered-shape-before-materials.glb')
    shutil.copy2(source/'original-shape.glb',output/'original-shape.glb')
    layout=json.loads((source/'fridge-layout.json').read_text(encoding='utf8'))
    layout['coordinate_transform']={'source_center':list(old_center),'target_center':list(new_center),'scale':scale,'floor_offset':[0,0,0]}
    (output/'fridge-layout.json').write_text(json.dumps(layout,indent=2),encoding='utf8')
else:
    shutil.copy2(source/'combined-before-texture.glb',output/'recovered-shape-before-materials.glb')
    shutil.copy2(source/'partition.json',output/'partition.json')
report={'source_folder':str(source),'textured_body':str(texture),'triangles':triangles,
        'body_source_bounds':[list(old_lo),list(old_hi)],'body_textured_bounds':[list(new_lo),list(new_hi)],
        'uniform_scale':scale,'body_fit_samples':len(sample),'max_body_fit_residual':max(residuals),
        'glass_and_shelves_recombined':True,'original_face_count_preserved':cleanup_proof is None,
        'numerically_collapsed_body_faces':original_body_triangles-textured_body_triangles,
        'materials':('TRELLIS body PBR, fitted clear door, perforated stainless shelves/liner and emissive strip' if args.asset=='S08'
                     else 'TRELLIS body PBR, locally assigned clear glazing and stainless shelves'),
        'extra_shape_generations':0,'extra_texture_generations_in_this_assembly':0,
        'note':'High-detail assembly awaiting visual review and bounded canonical reduction; not a final game asset.'}
(output/'repair.json').write_text(json.dumps(report,indent=2),encoding='utf8')
script=root/'tools/reststop-production-review-render.py'
sys.argv=[str(script),'--',str(output)]
runpy.run_path(str(script),run_name='__main__')
shutil.copy2(output/'quality/opposite.png',output/'preview.png')
print(json.dumps(report),flush=True)
