"""Add V06's missing tandem axle using its measured original rear wheel parts."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import shutil
import sys

import bmesh
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/reststop-production-2026-09-24'
parser=argparse.ArgumentParser()
parser.add_argument('--revision',type=int,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
folder=OUT/('manual/V06-r'+str(args.revision))/'source'
assert args.revision>0 and not folder.exists()
source=OUT/'assets/V06_bd6306f277/stages/m1/model.glb'
assert hashlib.sha256(source.read_bytes()).hexdigest()=='4abf7979d3d2e1ded2bb9815f58e26ebc54d67f85fda5abeaa99995e2da622f7'
profile=json.loads((OUT/'reviews/V06-m1-wheel-profile-r1.json').read_text(encoding='utf8'))
fit=json.loads((OUT/'reviews/V06-m1-rear-wheel-fit-r1.json').read_text(encoding='utf8'))
lo,hi=Vector(profile['bounds_min']),Vector(profile['bounds_max']);span=hi-lo
center_y,center_z=fit['world_yz_center'];radius=fit['radius'];shift=span.y*.12
assert fit['max_radial_fit_error']<radius*.02 and .65<fit['center_along_fraction']<.74
assert 2*radius<shift<2.5*radius
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
assert len([o for o in bpy.context.scene.objects if o.type=='MESH'])==1
assert not obj.data.uv_layers and not obj.data.materials
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bm=bmesh.new();bm.from_mesh(obj.data)
threshold=(span.length*1e-7)**2*.04
degenerate=[f for f in bm.faces if f.calc_area()<threshold]
degenerate_count=len(degenerate)
if degenerate:bmesh.ops.delete(bm,geom=degenerate,context='FACES_ONLY')
donor=[];remove=[]
for face in bm.faces:
    p=face.calc_center_median();x=(p.x-lo.x)/span.x;h=(p.z-lo.z)/span.z;along=(p.y-lo.y)/span.y
    side=x<.25 or x>.75
    donor_distance=((p.y-center_y)**2+(p.z-center_z)**2)**.5
    destination_distance=((p.y-center_y-shift)**2+(p.z-center_z)**2)**.5
    old_flap=side and .752<along<.785 and h<.11 and donor_distance>radius*1.05
    if side and donor_distance<radius*1.20 and not old_flap:
        donor.append(face)
    if side and destination_distance<radius*1.10 or old_flap:
        remove.append(face)
assert 200<len(donor)<20000 and 50<len(remove)<10000,(len(donor),len(remove))
assert not set(donor)&set(remove),'Do not cut the donor wheel assembly'
copy_geometry=[tuple(tuple(vertex.co) for vertex in face.verts) for face in donor]
remove_set=set(remove)
protected={tuple(v.co) for v in bm.verts if v.link_faces and any(f not in remove_set for f in v.link_faces)}
bmesh.ops.delete(bm,geom=remove,context='FACES_ONLY')
wire=[e for e in bm.edges if not e.link_faces]
if wire:bmesh.ops.delete(bm,geom=wire,context='EDGES')
loose=[v for v in bm.verts if not v.link_faces]
if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
assert protected<={tuple(v.co) for v in bm.verts}
copied={}
duplicate_copied_faces=0
for corners in copy_geometry:
    vertices=[]
    for point in corners:
        if point not in copied:
            copied[point]=bm.verts.new((point[0],point[1]+shift,point[2]))
        vertices.append(copied[point])
    if bm.faces.get(vertices) is not None:
        duplicate_copied_faces+=1
        continue
    bm.faces.new(vertices)
bm.normal_update();bm.to_mesh(obj.data);bm.free();obj.data.update()
bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=radius*.12,depth=span.x*.76,
    location=((lo.x+hi.x)*.5,center_y+shift,center_z),rotation=(0,1.5707963267948966,0))
axle=bpy.context.object;axle.name='Additional_rear_axle_support'
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);axle.select_set(True)
bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
cleanup=bmesh.new();cleanup.from_mesh(obj.data)
collapsed=[face for face in cleanup.faces if face.calc_area()<threshold]
post_copy_collapsed_faces=len(collapsed)
post_copy_removed_area=sum(face.calc_area() for face in collapsed)
assert post_copy_collapsed_faces<=10 and post_copy_removed_area<span.length**2*1e-10
if collapsed:bmesh.ops.delete(cleanup,geom=collapsed,context='FACES_ONLY')
wire=[edge for edge in cleanup.edges if not edge.link_faces]
if wire:bmesh.ops.delete(cleanup,geom=wire,context='EDGES')
loose=[vertex for vertex in cleanup.verts if not vertex.link_faces]
if loose:bmesh.ops.delete(cleanup,geom=loose,context='VERTS')
cleanup.normal_update();cleanup.to_mesh(obj.data);cleanup.free()
for layer in list(obj.data.uv_layers):obj.data.uv_layers.remove(layer)
obj.data.polygons.foreach_set('use_smooth',[True]*len(obj.data.polygons));obj.data.update()
obj.data.calc_loop_triangles();triangles=len(obj.data.loop_triangles)
assert triangles<=300000,triangles
folder.mkdir(parents=True)
bpy.ops.export_scene.gltf(filepath=str(folder/'model.glb'),use_selection=True,export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(folder/'model.blend'))
shutil.copy2(source,folder/'original-trellis-source.glb')
(folder/'repair.json').write_text(json.dumps({'source':str(source),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
    'method':'Transplant both measured rear-wheel and arch surface patches to a second rear axle; clear corresponding lower side panels and interfering old mudflap; add supported internal axle',
    'original_rear_axle_yz':[center_y,center_z],'new_rear_axle_yz':[center_y+shift,center_z],
    'wheel_radius':radius,'axle_shift':shift,'axle_shift_length_fraction':.12,
    'copied_faces':len(copy_geometry),'removed_destination_and_flap_faces':len(remove),
    'duplicate_copied_faces_omitted':duplicate_copied_faces,
    'source_degenerate_faces_removed':degenerate_count,'triangles':triangles,
    'post_copy_collapsed_faces_removed':post_copy_collapsed_faces,'post_copy_removed_surface_area':post_copy_removed_area,
    'retained_original_surface_positions_unchanged':True,'extra_ai_shape_requests':0,
    'canonical_reduction_attempts':0,'visual_and_three_axle_verification_required':True},indent=2),encoding='utf8')
script=ROOT/'tools/reststop-production-review-render.py'
sys.argv=[str(script),'--',str(folder),'--shape'];runpy.run_path(str(script),run_name='__main__')
print(json.dumps({'folder':str(folder),'triangles':triangles}),flush=True)
