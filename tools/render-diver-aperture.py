import bpy
import json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent.parent;folder=root/'map-concepts/skins-reststop-2026-09-12'
data=json.loads((folder/'diver-aperture-geometry.json').read_text(encoding='utf-8'));selected=set(json.loads((folder/'diver-aperture-candidates.json').read_text()))
bpy.ops.wm.read_factory_settings(use_empty=True)
mesh=bpy.data.meshes.new('Diver aperture');faces=[(data['triangles'][i],data['triangles'][i+2],data['triangles'][i+1]) for i in range(0,len(data['triangles']),3)]
mesh.from_pydata([(v[0],v[2],v[1]) for v in data['vertices']],[],faces);mesh.update();layer=mesh.uv_layers.new(name='UVMap')
for loop in mesh.loops:layer.data[loop.index].uv=data['uv'][loop.vertex_index]
obj=bpy.data.objects.new('Diver aperture',mesh);bpy.context.collection.objects.link(obj)
def emission(name,color=None):
    material=bpy.data.materials.new(name);material.use_nodes=True;nodes=material.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');shader=nodes.new('ShaderNodeEmission');material.node_tree.links.new(shader.outputs[0],out.inputs['Surface'])
    if color:shader.inputs[0].default_value=color
    else:
        image=nodes.new('ShaderNodeTexImage');image.image=bpy.data.images.load(str(root/data['texture']));material.node_tree.links.new(image.outputs['Color'],shader.inputs['Color'])
    return material
obj.data.materials.append(emission('Texture'));obj.data.materials.append(emission('Selected',(1,.03,.02,1)))
for p in mesh.polygons:p.material_index=1 if p.index in selected else 0
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=4;scene.render.resolution_x=640;scene.render.resolution_y=768;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('White');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(1,1,1,1)
camera_data=bpy.data.cameras.new('Front');camera=bpy.data.objects.new('Front',camera_data);bpy.context.collection.objects.link(camera);camera.location=(0,3,.55);camera.rotation_euler=(Vector((0,0,.55))-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.type='ORTHO';camera_data.ortho_scale=1.32;scene.camera=camera
scene.render.filepath=str(folder/'diver-aperture-analysis.png');bpy.ops.render.render(write_still=True)
