"""Same camera and character scale, with only vehicle scale changing."""
import bpy,sys,json
from pathlib import Path
from mathutils import Vector,Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent));import geometry as g
from vehicles import SPECS
OUT=Path.cwd()/'outputs/reststop-blender-v4-2026-09-23';bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-v4.blend'));s=bpy.context.scene;s.frame_set(1);g.M.update({m.name:m for m in bpy.data.materials});dg=bpy.context.evaluated_depsgraph_get()
originals=list(s.objects);copies=[]
for i,n in enumerate(SPECS):
 src=next(o for o in originals if o.type=='MESH' and o.parent and o.parent.get('source_model')==n and '_body_' in o.name);o=bpy.data.objects.new('Compare_vehicle_'+n,src.data);s.collection.objects.link(o);o.location=((i%3)*18,(i//3)*27,0);copies.append(o)
 g.text('Compare_label_'+n,SPECS[n][0],(o.location.x,o.location.y-11,.02),.70,'charcoal',rot=(0,0,0))
player=bpy.data.objects['PLAYER_ROOT'];src=next(o for o in player.children_recursive if o.type=='MESH' and o.name.startswith('char1'));ev=src.evaluated_get(dg);mesh=bpy.data.meshes.new_from_object(ev,depsgraph=dg);mesh.transform(player.matrix_world.inverted()@ev.matrix_world);o=bpy.data.objects.new('Reference_shark',mesh);s.collection.objects.link(o);o.location=(12,-17,0)
staff=bpy.data.objects['Parking_staff'];delta=Vector((5,-17,0))-staff.matrix_world.translation
for src in staff.children_recursive:
 if src.type!='MESH':continue
 ev=src.evaluated_get(dg);mesh=bpy.data.meshes.new_from_object(ev,depsgraph=dg);mesh.transform(Matrix.Translation(delta)@ev.matrix_world);o=bpy.data.objects.new('Reference_person',mesh);s.collection.objects.link(o)
g.text('Shark_label','상어',(12,-21,.03),.72,'charcoal',rot=(0,0,0));g.text('Human_label','사람',(5,-21,.03),.72,'charcoal',rot=(0,0,0));g.cube('Compare_ground',(18,5,-.18),(63,66,.22),'cream',0)
for o in originals:
 if o.type in ('MESH','CURVE','FONT'):o.animation_data_clear();o.hide_render=True
cam=s.camera;cam.animation_data_clear();cam.data.animation_data_clear();cam.data.shift_y=0;cam.data.type='ORTHO';cam.data.ortho_scale=77;cam.data.clip_end=2000;cam.location=(77,-94,91);target=Vector((18,7,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
s.render.resolution_x=1800;s.render.resolution_y=1400;s.render.resolution_percentage=100;s.render.engine='BLENDER_EEVEE';s.eevee.taa_render_samples=24;s.render.image_settings.file_format='PNG'
for factor,name in [(1,'vehicle-before.png'),(1.8,'vehicle-lineup.png')]:
 for o in copies:o.scale=(factor,)*3
 s.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
(OUT/'comparison.json').write_text(json.dumps({'camera':[77,-94,91],'target':[18,7,0],'ortho_scale':77,'resolution':[1800,1400],'vehicle_scale':[1,1.8],'shark_and_person_scale_unchanged':True},indent=2));print('SCALE_COMPARISON_READY',flush=True)
