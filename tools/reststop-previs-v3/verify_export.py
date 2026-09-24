"""Fresh-import the environment and render it independently of the master file."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
OUT=Path.cwd()/'outputs/reststop-blender-v3-2026-09-23'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(OUT/'reststop-v3-environment.glb'))
s=bpy.context.scene;meshes=[o for o in s.objects if o.type=='MESH'];bpy.context.view_layer.update()
pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=[min(p[i] for p in pts) for i in range(3)];hi=[max(p[i] for p in pts) for i in range(3)]
assert all(math.isfinite(c) for p in pts for c in p)
zones={z:any(o.name.startswith(z) for o in meshes) for z in ['01_','02_','03_','04_05_','06_','07_','08_','09_','10_']}
zones['10_']=any(o.name.startswith('Exit_road') for o in meshes)
assert all(zones.values()),zones
report={'fresh_import':True,'mesh_objects':len(meshes),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),'materials':len(bpy.data.materials),'min':lo,'max':hi,'required_zones':zones,'animation':'Static environment only'}
(OUT/'fresh-glb-validation.json').write_text(json.dumps(report,indent=2));print('FRESH_GLB_VALID',json.dumps(report),flush=True)
s.render.engine='BLENDER_EEVEE';s.eevee.taa_render_samples=8;s.render.resolution_x=1600;s.render.resolution_y=1100;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.threads_mode='FIXED';s.render.threads=4
s.world=bpy.data.worlds.new('Review sky');s.world.color=(.55,.68,.8)
d=bpy.data.lights.new('Sun','SUN');d.energy=3;d.angle=.15;l=bpy.data.objects.new('Sun',d);s.collection.objects.link(l);l.rotation_euler=(.44,-.56,-.4)
data=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',data);s.collection.objects.link(cam);s.camera=cam;cam.location=(820,-870,740);target=Vector((65,-40,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=800;data.clip_end=2500
s.render.filepath=str(OUT/'fresh-glb-overview.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reststop-v3-environment-reimport.blend'),compress=True)
