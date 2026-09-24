"""Validate the saved scene's motion and export the static environment separately."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-master.blend'));s=bpy.context.scene
p=bpy.data.objects['PLAYER_ROOT'];cam=s.camera;measure=[];last=None
for f in range(2401,3842):
    s.frame_set(f);loc=p.matrix_world.translation;angle=p.rotation_euler.z
    if last is not None:measure.append(abs(angle-last)*24*180/math.pi)
    last=angle
    if abs(loc.x)>.0001 or abs(loc.y-20)>.0001:raise AssertionError(('defense position',f,tuple(loc)))
    if (cam.location-Vector((0,19.99,76))).length>.002:raise AssertionError(('defense camera',f,tuple(cam.location)))
maximum=max(measure)
assert maximum<=90.01, maximum
errors=[];warnings=[];total=0
s.frame_set(1)
for o in s.objects:
    if any(not math.isfinite(v) for row in o.matrix_world for v in row):errors.append('nonfinite transform '+o.name)
    if o.type!='MESH':continue
    total+=sum(len(q.vertices)-2 for q in o.data.polygons)
    if any(not math.isfinite(c) for v in o.data.vertices for c in v.co):errors.append('nonfinite mesh '+o.name)
    if not o.data.materials:warnings.append('no material '+o.name)
assert not errors,errors
report={'fps':s.render.fps,'frames':s.frame_end,'duration':s.frame_end/s.render.fps,'defense_frames_checked':1441,'defense_max_deg_s':maximum,'defense_fixed_position':[0,20],'top_view_camera':[0,19.99,76],'restroom_m2':s['restroom_floor_area_m2'],'restroom_area_ratio':s['restroom_floor_area_m2']/s['restroom_concept_baseline_m2'],'triangles':total,'errors':errors,'warnings':warnings}
print('TIMING_CONTRACT',json.dumps(report),flush=True)
assert report['duration']==300 and report['restroom_area_ratio']==5
(OUT/'scene-validation.json').write_text(json.dumps(report,indent=2),encoding='utf8');print('SCENE_VALIDATED',json.dumps(report),flush=True)
# Export the architectural environment; animation remains in the native .blend.
bpy.ops.object.select_all(action='DESELECT');selected=[]
for o in s.objects:
    if o.type not in ('MESH','CURVE','FONT'):continue
    chain=[o];p2=o.parent
    while p2 is not None:chain.append(p2);p2=p2.parent
    if any(a.get('role') for a in chain):continue
    if any(a.animation_data and not a.name.startswith(('Main_roof','Store_roof','Restroom_roof','Arched_front_canopy','Fuel_','Main_name')) for a in chain):continue
    if any(a.name in ('PLAYER_ROOT','Shark_existing_project_rig') for a in chain):continue
    o.select_set(True);selected.append(o.name)
bpy.ops.export_scene.gltf(filepath=str(OUT/'reststop-environment.glb'),use_selection=True,export_format='GLB',export_animations=False,export_apply=True,export_cameras=False,export_lights=False)
(OUT/'export-manifest.json').write_text(json.dumps({'objects':selected,'animation':'Native master .blend only; this GLB is the static environment.'},indent=2))
print('ENVIRONMENT_EXPORTED',len(selected),flush=True)
