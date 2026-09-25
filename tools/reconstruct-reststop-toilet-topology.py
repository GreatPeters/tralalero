"""Explicit local T03 topology reconstruction after both canonical failures.

Prepare geometry for real review first, then bake the retained accepted high
source onto that reviewed mesh. Original generation/reduction ledgers stay intact.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import runpy
import shutil
import sys

import bmesh
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/reststop-production-2026-09-24'
APP=next(p for p in (Path.home()/'Desktop').glob('AI */Trellis */*') if (p/'blender_refine.py').is_file())
sys.path.insert(0,str(APP))
from blender_refine import active,bake,bounds,tris,inspect
parser=argparse.ArgumentParser();parser.add_argument('--revision',type=int,required=True)
parser.add_argument('--mode',choices=('prepare','verify','bake'),required=True)
parser.add_argument('--voxel-divisions',type=int,default=400)
parser.add_argument('--surface',type=Path,help='Reviewed reconstruction input from unsigned surface occupancy; original high PBR remains the bake source')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
assert args.revision>=4 and 250<=args.voxel_divisions<=700
source=OUT/'manual/T03-r3/source/model.glb'
key=hashlib.sha256((OUT/'inputs/T03.png').read_bytes()).hexdigest()
ledger_path=OUT/'manual-refinement-ledgers'/(key+'.json')
ledger=json.loads(ledger_path.read_text(encoding='utf8'))
assert len(ledger['lows'])==2 and ledger['source_sha256']==hashlib.sha256(source.read_bytes()).hexdigest()
assert all(json.loads((Path(low['folder'])/'visual-review.json').read_text(encoding='utf8'))['verdict']=='mesh' for low in ledger['lows'])
root=OUT/('manual/T03-r'+str(args.revision));shape=root/'shape';final=root/'final'

if args.mode=='prepare':
    assert not root.exists(),'Retain earlier reconstruction candidates'
    shape.mkdir(parents=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    high=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    high.name='Detailed_Source';active(high)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    lo,hi=bounds(high);height=hi.z-lo.z;diagonal=(hi-lo).length
    unsigned_receipt=None
    if args.surface:
        surface=args.surface.resolve();assert surface.is_relative_to(OUT/'manual')
        unsigned_receipt=json.loads((surface.parent/'reconstruction.json').read_text(encoding='utf8'))
        assert unsigned_receipt['watertight'] and unsigned_receipt['source_sha256']==hashlib.sha256(source.read_bytes()).hexdigest()
        previous=set(bpy.context.scene.objects)
        bpy.ops.import_scene.gltf(filepath=str(surface))
        imported=[o for o in bpy.context.scene.objects if o not in previous and o.type=='MESH'];assert len(imported)==1
        low=imported[0];low.name='Asset_Optimized';active(low)
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    else:
        low=high.copy();low.data=high.data.copy();bpy.context.collection.objects.link(low)
        low.name='Asset_Optimized';active(low)
        remesh=low.modifiers.new('Reconstruct fragmented source topology','REMESH')
        remesh.mode='VOXEL';remesh.voxel_size=height/args.voxel_divisions;remesh.adaptivity=0.
        remesh.use_smooth_shade=True
        if hasattr(remesh,'use_remove_disconnected'):remesh.use_remove_disconnected=False
        bpy.ops.object.modifier_apply(modifier=remesh.name)
    for layer in list(low.data.uv_layers):low.data.uv_layers.remove(layer)
    low.data.materials.clear()
    reconstructed_triangles=tris(low)
    assert reconstructed_triangles>15000
    decimate=low.modifiers.new('Local reconstructed topology budget','DECIMATE')
    decimate.ratio=14750/reconstructed_triangles;decimate.use_collapse_triangulate=True
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    mesh=low.data;bm=bmesh.new();bm.from_mesh(mesh)
    bad=[face for face in bm.faces if face.calc_area()<(diagonal*1e-7)**2*.04]
    removed_area=sum(face.calc_area() for face in bad);removed_count=len(bad)
    if bad:bmesh.ops.delete(bm,geom=bad,context='FACES_ONLY')
    wire=[e for e in bm.edges if not e.link_faces]
    if wire:bmesh.ops.delete(bm,geom=wire,context='EDGES')
    loose=[v for v in bm.verts if not v.link_faces]
    if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    bm.normal_update();bm.to_mesh(mesh);bm.free();mesh.update()
    mesh.polygons.foreach_set('use_smooth',[True]*len(mesh.polygons));mesh.update()
    mesh.set_sharp_from_angle(angle=math.radians(50))
    triangles=tris(low);assert 0<triangles<=15000,triangles
    high.hide_render=True;high.hide_set(True);active(low)
    bpy.ops.export_scene.gltf(filepath=str(shape/'model.glb'),use_selection=True,export_format='GLB')
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(shape/'model.blend'))
    receipt={'source':str(source),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
        'method':'Local voxel surface reconstruction of fragmented accepted source, then unweighted reduction of rebuilt topology; geometry review precedes PBR bake',
        'canonical_attempts_already_consumed':2,'canonical_ledger':str(ledger_path),
        'additional_canonical_runs':0,'local_topology_reconstruction_passes':1,'local_rebuilt_mesh_reduction_passes':1,
        'extra_ai_requests':0,'voxel_divisions':args.voxel_divisions,'voxel_size':height/args.voxel_divisions,
        'reconstructed_triangles':reconstructed_triangles,'triangles':triangles,
        'removed_collapsed_faces':removed_count,'removed_surface_area':removed_area,
        'unsigned_surface':str(args.surface.resolve()) if args.surface else None,
        'unsigned_surface_receipt':unsigned_receipt,
        'visual_and_cavity_review_required':True}
    (shape/'repair.json').write_text(json.dumps(receipt,indent=2),encoding='utf8')
    script=ROOT/'tools/reststop-production-review-render.py'
    sys.argv=[str(script),'--',str(shape),'--shape'];runpy.run_path(str(script),run_name='__main__')
    print(json.dumps({'folder':str(shape),'triangles':triangles,'requires_actual_geometry_review':True}),flush=True)
elif args.mode=='verify':
    reports={}
    bpy.ops.wm.open_mainfile(filepath=str(shape/'model.blend'))
    assert bpy.data.objects['Detailed_Source'].hide_render
    reports['blend_optimized_object']=inspect(bpy.data.objects['Asset_Optimized'])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(shape/'model.glb'))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(meshes)==1
    reports['glb']=inspect(meshes[0])
    ok=all(0<r['triangles']<=15000 and r['finite'] and not r['degenerate_faces'] and not r['loose_vertices'] for r in reports.values())
    ok=ok and len({r['triangles'] for r in reports.values()})==1
    proof={'ok':ok,'formats':reports,'note':'Working BLEND keeps the accepted high source hidden; only Asset_Optimized is the deliverable geometry. UV is intentionally absent before baking.'}
    path=shape/'geometry-validation.json';assert not path.exists();path.write_text(json.dumps(proof,indent=2),encoding='utf8')
    print(json.dumps(proof),flush=True);assert ok
else:
    assert not final.exists()
    assert json.loads((shape/'visual-review.json').read_text(encoding='utf8'))['verdict']=='pass'
    assert json.loads((shape/'opening-validation.json').read_text(encoding='utf8'))['ok']
    assert json.loads((shape/'geometry-validation.json').read_text(encoding='utf8'))['ok']
    final.mkdir()
    bpy.ops.wm.open_mainfile(filepath=str(shape/'model.blend'))
    high=bpy.data.objects['Detailed_Source'];low=bpy.data.objects['Asset_Optimized'];active(low)
    lo,hi=bounds(high);diagonal=(hi-lo).length;before=tris(low)
    low.data.uv_layers.new(name='UVMap')
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006)
    bpy.ops.object.mode_set(mode='OBJECT')
    bake(high,low,final,2048,diagonal)
    assert tris(low)==before
    active(low)
    bpy.ops.export_scene.gltf(filepath=str(final/'model.glb'),use_selection=True,export_format='GLB')
    bpy.ops.export_scene.fbx(filepath=str(final/'model.fbx'),use_selection=True,object_types={'MESH'},
        axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,add_leaf_bones=False)
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(final/'model.blend'))
    shutil.copy2(source,final/'trellis_source.glb')
    shutil.copy2(OUT/'manual/T03-r2/texture1/model.glb',final/'original-trellis-source.glb')
    shutil.copy2(OUT/'manual/T03-r2/source/model.glb',final/'recovered-shape-before-materials.glb')
    receipt=json.loads((shape/'repair.json').read_text(encoding='utf8'))
    receipt.update(bake='Retained accepted high-source BaseColor, Roughness, Metallic and tangent normals; no further geometry reduction',texture_size=2048)
    (final/'repair.json').write_text(json.dumps(receipt,indent=2),encoding='utf8')
    script=ROOT/'tools/reststop-production-review-render.py'
    sys.argv=[str(script),'--',str(final)];runpy.run_path(str(script),run_name='__main__')
    shutil.copy2(final/'quality/opposite.png',final/'preview.png')
    print(json.dumps({'folder':str(final),'triangles':before,'fresh_format_and_visual_validation_required':True}),flush=True)
