"""Clip actual shared room geometry into fair same-scale comparison views."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-v2-2026-09-23/comparison';OUT.mkdir(exist_ok=True)
records=[]
for version,file in [('v1','outputs/reststop-blender-300s-2026-09-23/reststop-master.blend'),('v2','outputs/reststop-blender-v2-2026-09-23/reststop-v2.blend')]:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/file));s=bpy.context.scene;s.frame_set(5521);originals=list(s.objects);dg=bpy.context.evaluated_depsgraph_get()
 for o in originals:
  if o.type in ('MESH','CURVE','FONT'):o.animation_data_clear()
 s.camera.animation_data_clear();s.camera.data.animation_data_clear();s.camera.data.shift_y=0;s.camera.data.type='ORTHO';s.camera.data.ortho_scale=142;s.camera.data.clip_end=2000
 s.render.engine='BLENDER_EEVEE';s.eevee.taa_render_samples=16;s.render.resolution_x=1200;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG'
 for subject,bounds in [('store',(44,68,0,44) if version=='v1' else (118,206,0,92)),('restroom',(69,99,18,38) if version=='v1' else (206,278,36,120))]:
  for o in s.objects:
   if o.type in ('MESH','CURVE','FONT'):o.hide_render=True
  xmin,xmax,ymin,ymax=bounds;prefixes=('07_','04_') if version=='v1' and subject=='store' else (('07_',) if subject=='store' else ('08_',));count=0
  for o in originals:
   if o.type not in ('MESH','CURVE','FONT') or not o.get('zone','').startswith(prefixes) or 'roof' in o.name.lower():continue
   chain=[o];a=o.parent
   while a is not None:chain.append(a);a=a.parent
   if any(a.get('role') or a.get('assembly') in ('actor','player') for a in chain):continue
   ev=o.evaluated_get(dg);mesh=bpy.data.meshes.new_from_object(ev,depsgraph=dg);bm=bmesh.new();bm.from_mesh(mesh);bm.transform(ev.matrix_world)
   if bm.verts and min(v.co.z for v in bm.verts)>7.5:
    bm.free();bpy.data.meshes.remove(mesh);continue
   for point,normal in [((xmin-.3,0,0),(-1,0,0)),((xmax+.3,0,0),(1,0,0)),((0,ymin-.3,0),(0,-1,0)),((0,ymax+.3,0),(0,1,0)),((0,0,10),(0,0,1))]:
    if bm.verts:bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.0001,plane_co=point,plane_no=normal,clear_outer=True,clear_inner=False)
   if bm.faces:
    bm.to_mesh(mesh);temp=bpy.data.objects.new('Comparison_'+o.name,mesh);s.collection.objects.link(temp);count+=1
   else:bpy.data.meshes.remove(mesh)
   bm.free()
  target=Vector(((xmin+xmax)/2,(ymin+ymax)/2,0));s.camera.location=target+Vector((110,-150,185));s.camera.rotation_euler=(target-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(OUT/f'{version}-{subject}.png');bpy.ops.render.render(write_still=True)
  records.append({'version':version,'subject':subject,'ortho_scale':142,'relative_camera':[110,-150,185],'target':list(target),'frame':5521,'resolution':[1200,1000],'actual_geometry_clipped_to_room_bounds':list(bounds),'included_parts':count,'shared_v1_floor_included':True})
(OUT/'manifest.json').write_text(json.dumps(records,indent=2))
