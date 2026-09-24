"""Read-only source inspection and renderer timing for the rest-stop previs."""
import bpy, json, time
from pathlib import Path
from mathutils import Vector
ROOT=Path.cwd(); OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23'
bpy.ops.wm.read_factory_settings(use_empty=True)
source=next((ROOT/'Assets/JH/Model/Player/Sharks').rglob('Original.fbx'))
bpy.ops.import_scene.fbx(filepath=str(source))
for o in bpy.context.scene.objects:
    o.animation_data_clear()
    if o.parent is None:o.scale*=300
bpy.context.view_layer.update()
records=[]
for o in bpy.context.scene.objects:
    records.append({'name':o.name,'type':o.type,'dimensions':list(o.dimensions),'location':list(o.location),'rotation':list(o.rotation_euler),'scale':list(o.scale),'vertices':len(o.data.vertices) if o.type=='MESH' else 0,'bones':[b.name for b in o.data.bones] if o.type=='ARMATURE' else []})
images=[{'name':i.name,'path':i.filepath,'size':list(i.size)} for i in bpy.data.images]
(OUT/'source-probe.json').write_text(json.dumps({'source':str(source),'objects':records,'images':images,'actions':[a.name for a in bpy.data.actions]},indent=2),encoding='utf8')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=(lo+hi)/2;size=max(hi-lo)
camdata=bpy.data.cameras.new('probe');cam=bpy.data.objects.new('probe',camdata);bpy.context.collection.objects.link(cam)
cam.location=center+Vector((1.5,-2.5,1.1))*size;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=size*1.5
s=bpy.context.scene;s.camera=cam;s.render.resolution_x=720;s.render.resolution_y=540;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG'
s.world=bpy.data.worlds.new('World');s.world.color=(.6,.6,.6)
sun_data=bpy.data.lights.new('sun','SUN');sun_data.energy=2;sun=bpy.data.objects.new('sun',sun_data);bpy.context.collection.objects.link(sun);sun.rotation_euler=(.4,-.7,-.4)
results=[]
for engine in ['BLENDER_WORKBENCH','BLENDER_EEVEE']:
    s.render.engine=engine
    if engine=='BLENDER_WORKBENCH':
        s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.display.shading.cavity_type='BOTH';s.display.shading.background_type='WORLD';s.display.shading.background_color=(.7,.7,.7)
    else:
        print('EEVEE_SETTINGS',[(p.identifier,p.type) for p in s.eevee.bl_rna.properties],flush=True)
        if hasattr(s.eevee,'taa_render_samples'):s.eevee.taa_render_samples=16
    t=time.time();s.render.filepath=str(OUT/('shark-'+engine+'.png'));bpy.ops.render.render(write_still=True);results.append({'engine':engine,'seconds':time.time()-t})
(OUT/'render-benchmark.json').write_text(json.dumps(results,indent=2),encoding='utf8')
print('PROBE',json.dumps(results),flush=True)
