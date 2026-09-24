"""Bounded final-asset views for semantic review; runs in Blender."""
import bpy,sys,math
from pathlib import Path
from mathutils import Vector
folder=Path(sys.argv[sys.argv.index('--')+1]).resolve();out=folder/('quality-fbx' if '--fbx' in sys.argv else 'quality');out.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
if '--fbx' in sys.argv:bpy.ops.import_scene.fbx(filepath=str(folder/'model.fbx'))
else:bpy.ops.import_scene.gltf(filepath=str(folder/'model.glb'))
bpy.context.view_layer.update()
objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];points=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
if '--shape' in sys.argv:
    mat=bpy.data.materials.new('Pre-texture geometry');mat.use_nodes=True;mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.35,.35,.35,1)
    mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.75
    for obj in objects:
        obj.data.materials.clear();obj.data.materials.append(mat)
if not points:raise RuntimeError('Empty inspection mesh')
if '--basecolor-only' in sys.argv:
    for mat in {mat for obj in objects for mat in obj.data.materials if mat}:
        shader=next((node for node in mat.node_tree.nodes if node.type=='BSDF_PRINCIPLED'),None)
        output=next((node for node in mat.node_tree.nodes if node.type=='OUTPUT_MATERIAL' and node.is_active_output),None)
        if shader is None or output is None:raise RuntimeError('Missing source PBR material')
        emission=mat.node_tree.nodes.new('ShaderNodeEmission');base=shader.inputs['Base Color']
        if base.is_linked:mat.node_tree.links.new(base.links[0].from_socket,emission.inputs['Color'])
        else:emission.inputs['Color'].default_value=base.default_value
        mat.node_tree.links.new(emission.outputs[0],output.inputs['Surface'])
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
center=(hi+lo)*.5;size=max((hi-lo).length,.001)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.cycles.seed=12345
scene.render.resolution_x=896;scene.render.resolution_y=768;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
world=bpy.data.worlds.new('Review studio');world.use_nodes=True;world.node_tree.nodes['Background'].inputs['Strength'].default_value=.6;scene.world=world
def aim(o):o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
underside='--underside-only' in sys.argv
for position,energy in [((-1,-1.5,2),120),((1.5,-.1,1),70),((.3,1.5,2),150)]:
    if underside:position=(position[0],position[1],-position[2])
    bpy.ops.object.light_add(type='AREA',location=center+Vector(position)*size);lamp=bpy.context.object;lamp.data.energy=energy*size*size;lamp.data.size=size*1.5;aim(lamp)
bpy.ops.object.camera_add();cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=size*1.50;scene.camera=cam
views=[('hero',(1.8,1.3,1.3)),('top',(0,-.001,2)),('opposite',(-1.8,-1.3,1.3)),('front',(0,-2.3,.25)),('neutral',(1.8,1.3,1.3))]
if '--front-only' in sys.argv:views=[('front',(0,-2.3,.25))]
if '--top-only' in sys.argv:views=[('top',(0,-.001,2))]
if '--opposite-only' in sys.argv:views=[('opposite',(-1.8,-1.3,1.3))]
if underside:views=[('bottom',(1.8,1.3,-1.3)),('bottom-flat',(0,.001,-2)),('bottom-neutral',(1.8,1.3,-1.3))]
for name,direction in views:
    if name in ('neutral','bottom-neutral'):
        m=bpy.data.materials.new('Geometry only');m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.35,.35,.35,1);p.inputs['Roughness'].default_value=.75;scene.view_layers[0].material_override=m
    cam.location=center+Vector(direction)*size;aim(cam);scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
