"""Final architectural multiviews and a native Blender cast portrait."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path.cwd();OUT=ROOT/'outputs/reststop-blender-300s-2026-09-23';VIEWS=OUT/'asset-evidence';VIEWS.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-environment-reimport.blend'))
s=bpy.context.scene;cam=s.camera;target=Vector((10,-15,3));cam.data.type='ORTHO';cam.data.ortho_scale=320;s.render.resolution_x=1280;s.render.resolution_y=800;s.eevee.taa_render_samples=8
views={'front':(10,-270,22),'back':(10,260,22),'left':(-270,-5,22),'right':(280,-5,22),'top':(10,-5,300)}
for name,pos in views.items():
    cam.location=pos;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(VIEWS/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-master.blend'));s=bpy.context.scene;s.frame_set(1)
# Keep the authored file pleasant to open and play, without requiring script trust.
visibility_count=0
for o in list(s.objects):
    if not o.animation_data or not o.animation_data.action:continue
    key_values=[]
    for layer in o.animation_data.action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    if fc.data_path=='hide_render':key_values.extend((k.co.x,bool(k.co.y)) for k in fc.keyframe_points)
    for frame,value in key_values:
        o.hide_viewport=value;o.keyframe_insert(data_path='hide_viewport',frame=frame)
    if key_values:visibility_count+=1
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active
            space.overlay.show_overlays=False;space.shading.type='SOLID';space.shading.color_type='TEXTURE'
            if space.region_3d:space.region_3d.view_perspective='CAMERA'
s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reststop-master.blend'),compress=True)
(OUT/'viewport-presentation.json').write_text(__import__('json').dumps({'camera_view_on_open':True,'cutaway_objects_with_native_viewport_keys':visibility_count,'frame':1,'scripts_required_for_playback':False},indent=2))
names=['Parking_staff','Snack_chef','Barista','Cashier','Cleaner','Fuel_staff','Defense_police_00','Fleeing_visitor_0']
for o in s.objects:
    o.animation_data_clear();o.hide_render=True
for i,name in enumerate(names):
    root=bpy.data.objects[name];root.animation_data_clear();root.location=(i*3.0,0,0);root.rotation_euler=(0,0,0);root.scale=(1,1,1)
    for o in [root]+list(root.children_recursive):o.hide_render=False
for o in s.objects:
    if o.type=='LIGHT':o.hide_render=False
bpy.ops.mesh.primitive_plane_add(size=200,location=(10,0,-.01));ground=bpy.context.object;ground.data.materials.append(bpy.data.materials['cream'])
cam=s.camera;cam.hide_render=False;cam.animation_data_clear();cam.location=(11,-25,9);target=Vector((10.5,0,1.2));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=25
s.render.resolution_x=1920;s.render.resolution_y=720;s.render.resolution_percentage=100;s.render.engine='BLENDER_EEVEE';s.eevee.taa_render_samples=24;s.render.image_settings.file_format='PNG';s.render.filepath=str(OUT/'people-native-blender.png');bpy.ops.render.render(write_still=True)
print('ASSET_EVIDENCE_COMPLETE',flush=True)
