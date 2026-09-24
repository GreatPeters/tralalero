import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent));import geometry as g;import vehicles
OUT=Path.cwd()/'outputs/reststop-blender-v3-2026-09-23';bpy.ops.wm.read_factory_settings(use_empty=True);g.setup_materials();s=bpy.context.scene
s.world=bpy.data.worlds.new('Vehicle review');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.68,.8,1);s.world.node_tree.nodes['Background'].inputs[1].default_value=.65
d=bpy.data.lights.new('Sun','SUN');d.energy=2.5;d.angle=.15;o=bpy.data.objects.new('Sun',d);s.collection.objects.link(o);o.rotation_euler=(.44,-.56,-.4)
stats=[]
for i,n in enumerate(vehicles.SPECS):
 data=vehicles.load(n);r=g.empty('Vehicle_'+n,((i%3)*12,(i//3)*17,0));vehicles.attach(r,n,data);stats.append({'model':n,'label':vehicles.SPECS[n][0],'dimensions':vehicles.SPECS[n][1],'triangles':sum(len(p.vertices)-2 for p in data.polygons)})
 g.text('Vehicle_label_'+n,n+' '+vehicles.SPECS[n][0],(r.location.x,r.location.y-6,.05),.70,'charcoal',rot=(0,0,0))
g.cube('Review_ground',(12,8,-.12),(42,40,.20),'cream',0)
data=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',data);s.collection.objects.link(cam);s.camera=cam;cam.location=(43,-48,51);target=Vector((12,8,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=44
s.render.engine='BLENDER_EEVEE';s.eevee.taa_render_samples=24;s.render.resolution_x=1600;s.render.resolution_y=1100;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.filepath=str(OUT/'vehicle-lineup.png');s.render.fps=24;bpy.ops.render.render(write_still=True)
(OUT/'vehicle-models.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2),encoding='utf8');print('VEHICLES_REVIEW_READY',flush=True)
