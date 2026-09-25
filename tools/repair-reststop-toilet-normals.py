"""Smooth only T03's rounded exterior shading; retain geometry, UVs and maps."""
import argparse
from array import array
import hashlib
import json
from pathlib import Path
import runpy
import shutil
import sys

import bpy
import numpy as np
from mathutils import Vector
from mathutils.kdtree import KDTree

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/reststop-production-2026-09-24'
parser=argparse.ArgumentParser();parser.add_argument('--revision',type=int,required=True)
parser.add_argument('--neighbors',type=int,default=96)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
assert 24<=args.neighbors<=256
source=OUT/'manual/T03-r2/texture1/model.glb'
folder=OUT/('manual/T03-r'+str(args.revision))/'source'
assert args.revision>2 and not folder.exists()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');mesh=obj.data
assert len([o for o in bpy.context.scene.objects if o.type=='MESH'])==1
world=np.array([obj.matrix_world@v.co for v in mesh.vertices],dtype=np.float64)
lo,hi=world.min(0),world.max(0);span=hi-lo
unique,inverse=np.unique(world,axis=0,return_inverse=True)
tree=KDTree(len(unique))
for index,point in enumerate(unique):tree.insert(point,index)
tree.balance()
uv_before={}
for layer in mesh.uv_layers:
    values=array('f',[0.])*(len(layer.data)*2);layer.data.foreach_get('uv',values)
    uv_before[layer.name]=hashlib.sha256(values.tobytes()).hexdigest()
positions=[tuple(v.co) for v in mesh.vertices]
normals=[n.vector.copy() for n in mesh.corner_normals]
assert min(n.length for n in normals)>.1
normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
inverse_normal=normal_matrix.inverted()
affected=set()
for polygon in mesh.polygons:
    center=obj.matrix_world@polygon.center;r=(np.array(center)-lo)/span
    normal=(normal_matrix@polygon.normal).normalized()
    if .15<r[2]<.56 and r[1]<.61 and normal.z<.35:
        affected.add(polygon.index)
assert 1000<len(affected)<len(mesh.polygons)*.6,len(affected)
cache={};fallback=0
merged=list(normals)
for polygon in mesh.polygons:
    if polygon.index not in affected:continue
    polygon.use_smooth=True
    for loop_index in polygon.loop_indices:
        vertex_index=mesh.loops[loop_index].vertex_index;key=int(inverse[vertex_index])
        original=(normal_matrix@normals[loop_index]).normalized()
        if key not in cache:
            point=unique[key]
            neighbors=np.array([unique[i] for _,i,_ in tree.find_n(point,args.neighbors)])
            offsets=neighbors-point
            keep=(np.linalg.norm(offsets,axis=1)<span[2]*.04)&(np.abs(offsets@np.array(original))<span[2]*.004)
            neighbors=neighbors[keep]
            if len(neighbors)<12:
                cache[key]=original.copy();fallback+=1
            else:
                centered=neighbors-neighbors.mean(0)
                _,vectors=np.linalg.eigh(centered.T@centered)
                cache[key]=Vector(vectors[:,0]).normalized()
        normal=cache[key].copy()
        if normal.dot(original)<0:normal.negate()
        merged[loop_index]=(inverse_normal@normal).normalized()
regions=[set() for _ in mesh.edges]
for polygon in mesh.polygons:
    for index in polygon.loop_indices:regions[mesh.loops[index].edge_index].add(polygon.index in affected)
for edge,regions_on_edge in zip(mesh.edges,regions):
    if len(regions_on_edge)>1:edge.use_edge_sharp=True
mesh.update();mesh.normals_split_custom_set(merged);mesh.update()
assert positions==[tuple(v.co) for v in mesh.vertices]
for layer in mesh.uv_layers:
    values=array('f',[0.])*(len(layer.data)*2);layer.data.foreach_get('uv',values)
    assert hashlib.sha256(values.tobytes()).hexdigest()==uv_before[layer.name]
mesh.calc_loop_triangles();triangles=len(mesh.loop_triangles)
assert triangles<=300000
folder.mkdir(parents=True)
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.export_scene.gltf(filepath=str(folder/'model.glb'),use_selection=True,export_format='GLB')
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(folder/'model.blend'))
shutil.copy2(source,folder/'original-trellis-source.glb')
(folder/'repair.json').write_text(json.dumps({'source':str(source),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
    'method':'Local PCA normal field on rounded exterior bowl; neighboring points restricted by distance and surface-normal slab; preserve geometry, UVs and all material images',
    'affected_faces':len(affected),'sampled_unique_vertices':len(cache),'fallback_vertices':fallback,
    'neighbors':args.neighbors,'height_range':[.15,.56],'front_fraction_max':.61,
    'geometry_and_uvs_unchanged':True,'original_material_images_retained':True,
    'triangles':triangles,'extra_ai_requests':0,'canonical_reduction_attempts':0,'visual_review_required':True},indent=2),encoding='utf8')
script=ROOT/'tools/reststop-production-review-render.py';sys.argv=[str(script),'--',str(folder)]
runpy.run_path(str(script),run_name='__main__')
print(json.dumps({'folder':str(folder),'triangles':triangles,'affected_faces':len(affected)}),flush=True)
