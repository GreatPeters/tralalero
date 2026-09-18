"""Repack the live Unity mesh UVs and transfer existing shark skin appearances.

Source geometry, original FBXs and animation rigs are not modified. The exported
corner-UV map lets Unity rebuild the mesh while retaining its own skin weights.
"""
import argparse
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Matrix, Vector

parser=argparse.ArgumentParser()
parser.add_argument('--skins',nargs='+',default=['skin_original','skin_coral','skin_ice','skin_sand','skin_armor','skin_raider','skin_relic','skin_diver'])
parser.add_argument('--size',type=int,default=2048)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
ROOT=Path.cwd();OUT=ROOT/'outputs/skin-surfaces-2026-09-12';OUT.mkdir(parents=True,exist_ok=True)
DATA=json.loads((ROOT/'map-concepts/skins-reststop-2026-09-12/unity-shark-mesh.json').read_text(encoding='utf-8'))
AUDIT=json.loads((ROOT/'map-concepts/skins-reststop-2026-09-12/existing-shark-uv-audit.json').read_text(encoding='utf-8'))
FILES={Path(row['file']).stem:ROOT/row['file'] for row in AUDIT}
STYLES={'skin_original':'Original','skin_coral':'Pink','skin_ice':'Ice','skin_sand':'Wood','skin_armor':'Army','skin_raider':'Zombie','skin_relic':'Fire','skin_diver':'Cyber'}
SCALE=300.0
def converted(v):return Vector((v[0],-v[2],v[1]))*SCALE
verts=[converted(v) for v in DATA['vertices']]
normals=[converted(v).normalized() for v in DATA['normals']]
body_indices=DATA['submeshes'][0]
original_faces=[tuple(body_indices[i:i+3]) for i in range(0,len(body_indices),3)]
signs=[(verts[b]-verts[a]).cross(verts[c]-verts[a]).dot(normals[a]+normals[b]+normals[c]) for a,b,c in original_faces]
flip=np.median(signs)<0
faces=[(a,c,b) if flip else (a,b,c) for a,b,c in original_faces]
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=1
scene.render.threads_mode='FIXED';scene.render.threads=8
mesh=bpy.data.meshes.new('Unity body UV target');mesh.from_pydata(verts,[],faces);mesh.update()
target=bpy.data.objects.new('Body',mesh);scene.collection.objects.link(target)
uv=mesh.uv_layers.new(name='BodyAtlas')
for loop in mesh.loops:uv.data[loop.index].uv=DATA['uv'][loop.vertex_index]
for polygon in mesh.polygons:polygon.use_smooth=True
bpy.context.view_layer.objects.active=target;target.select_set(True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.select_all(action='SELECT')
bpy.ops.uv.pack_islands(rotate=True,margin=.012)
bpy.ops.object.mode_set(mode='OBJECT')
corner_uv=[]
for polygon in mesh.polygons:
    values=[list(uv.data[i].uv) for i in polygon.loop_indices]
    if flip:values=[values[0],values[2],values[1]]
    corner_uv.extend(values)
(OUT/'body-corner-uv.json').write_text(json.dumps({'sourceMesh':DATA['mesh'],'sourceVertexCount':len(verts),'triangleIndices':body_indices,'cornerUV':corner_uv,'blenderWindingReversed':bool(flip)},separators=(',',':')),encoding='utf-8')
bpy.ops.uv.export_layout(filepath=str(OUT/'body-uv-layout.svg'),mode='SVG',size=(2048,2048),opacity=.2)

def color_material(name,image,emission=False):
    mat=bpy.data.materials.new(name);mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear()
    output=nodes.new('ShaderNodeOutputMaterial');shader=nodes.new('ShaderNodeEmission' if emission else 'ShaderNodeBsdfPrincipled')
    texture=nodes.new('ShaderNodeTexImage');texture.image=image
    mat.node_tree.links.new(texture.outputs['Color'],shader.inputs['Color' if emission else 'Base Color'])
    mat.node_tree.links.new(shader.outputs[0],output.inputs['Surface'])
    if not emission:shader.inputs['Roughness'].default_value=.55
    return mat

# Show the unchanged original footwear in body-skin renders.
shoe_faces=[tuple(DATA['submeshes'][1][i:i+3]) for i in range(0,len(DATA['submeshes'][1]),3)]
if flip:shoe_faces=[(a,c,b) for a,b,c in shoe_faces]
shoe_mesh=bpy.data.meshes.new('Original shoe geometry');shoe_mesh.from_pydata(verts,[],shoe_faces);shoe_mesh.update()
shoes=bpy.data.objects.new('Default footwear',shoe_mesh);scene.collection.objects.link(shoes)
shoe_uv=shoe_mesh.uv_layers.new(name='OriginalUV')
for loop in shoe_mesh.loops:shoe_uv.data[loop.index].uv=DATA['uv'][loop.vertex_index]
for polygon in shoe_mesh.polygons:polygon.use_smooth=True
original_image=bpy.data.images.load(str(FILES['Original'].parent/'texture_0.png'),check_existing=True)
shoe_mesh.materials.append(color_material('Default footwear',original_image))

bone_target={row['name']:converted(row['position']) for row in DATA['bones']}
records=[]
for key in args.skins:
    folder=OUT/key;folder.mkdir(exist_ok=True)
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(FILES[STYLES[key]]))
    source_objects=set(bpy.data.objects)-before
    source=next(obj for obj in source_objects if obj.type=='MESH')
    armature=next(obj for obj in source_objects if obj.type=='ARMATURE')
    common=[bone for bone in armature.data.bones if bone.name in bone_target]
    if len(common)<8:raise RuntimeError('Too few matching skeleton landmarks')
    from_points=np.array([list(armature.matrix_world@bone.head_local)+[1.] for bone in common])
    to_points=np.array([list(bone_target[bone.name]) for bone in common])
    transform,_,_,_=np.linalg.lstsq(from_points,to_points,rcond=None)
    rms=float(np.sqrt(np.mean((from_points@transform-to_points)**2)))
    positions=np.array([list(source.matrix_world@v.co)+[1.] for v in source.data.vertices])@transform
    source.parent=None;source.matrix_world=Matrix.Identity(4);source.modifiers.clear()
    for vertex,position in zip(source.data.vertices,positions):vertex.co=position
    source.data.update()
    # Let Blender restore consistent outside orientation after possible reflection.
    import bmesh
    bm=bmesh.new();bm.from_mesh(source.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(source.data);bm.free()
    source_image=bpy.data.images.load(str(FILES[STYLES[key]].parent/'texture_0.png'),check_existing=True)
    source_material=color_material('Source '+key,source_image,True)
    source.data.materials.clear();source.data.materials.append(source_material)
    for polygon in source.data.polygons:polygon.material_index=0;polygon.use_smooth=True
    image=bpy.data.images.new(key+' albedo',width=args.size,height=args.size,alpha=False)
    image.filepath_raw=str(folder/'Albedo.png');image.file_format='PNG'
    material=bpy.data.materials.new(key);material.use_nodes=True
    image_node=material.node_tree.nodes.new('ShaderNodeTexImage');image_node.image=image
    material.node_tree.nodes.active=image_node
    mesh.materials.clear();mesh.materials.append(material)
    scene.render.bake.use_selected_to_active=True;scene.render.bake.use_cage=False
    scene.render.bake.cage_extrusion=.15;scene.render.bake.max_ray_distance=.30;scene.render.bake.margin=16
    bpy.ops.object.select_all(action='DESELECT');source.select_set(True);target.select_set(True);bpy.context.view_layer.objects.active=target
    bpy.ops.object.bake(type='EMIT');image.save()
    maps={}
    source_texture=next(n for n in source_material.node_tree.nodes if n.type=='TEX_IMAGE')
    for channel in ('roughness','metallic'):
        paths=list(FILES[STYLES[key]].parent.glob('*_'+channel+'.png'))
        if not paths:continue
        source_texture.image=bpy.data.images.load(str(paths[0]),check_existing=True);source_texture.image.colorspace_settings.name='Non-Color'
        baked=bpy.data.images.new(key+' '+channel,width=args.size,height=args.size,alpha=False)
        baked.colorspace_settings.name='Non-Color';baked.filepath_raw=str(folder/(channel.title()+'.png'));baked.file_format='PNG'
        image_node.image=baked;bpy.ops.object.bake(type='EMIT');baked.save();maps[channel]=baked
    normal_paths=list(FILES[STYLES[key]].parent.glob('*_normal.png'))
    normal_image=None
    if normal_paths:
        nodes=source_material.node_tree.nodes;links=source_material.node_tree.links
        source_bsdf=nodes.new('ShaderNodeBsdfPrincipled');normal_tex=nodes.new('ShaderNodeTexImage');normal_tex.image=bpy.data.images.load(str(normal_paths[0]),check_existing=True);normal_tex.image.colorspace_settings.name='Non-Color'
        normal_node=nodes.new('ShaderNodeNormalMap');links.new(normal_tex.outputs['Color'],normal_node.inputs['Color']);links.new(normal_node.outputs['Normal'],source_bsdf.inputs['Normal']);links.new(source_bsdf.outputs[0],next(n for n in nodes if n.type=='OUTPUT_MATERIAL').inputs['Surface'])
        normal_image=bpy.data.images.new(key+' normal',width=args.size,height=args.size,alpha=False);normal_image.colorspace_settings.name='Non-Color';normal_image.filepath_raw=str(folder/'Normal.png');normal_image.file_format='PNG'
        image_node.image=normal_image;scene.render.bake.normal_space='TANGENT';bpy.ops.object.bake(type='NORMAL');normal_image.save()
    image_node.image=image
    shader=material.node_tree.nodes.get('Principled BSDF');material.node_tree.links.new(image_node.outputs['Color'],shader.inputs['Base Color']);shader.inputs['Roughness'].default_value=.55
    for channel,baked in maps.items():
        texture=material.node_tree.nodes.new('ShaderNodeTexImage');texture.image=baked;material.node_tree.links.new(texture.outputs['Color'],shader.inputs[channel.title()])
    if normal_image:
        texture=material.node_tree.nodes.new('ShaderNodeTexImage');texture.image=normal_image
        normal_node=material.node_tree.nodes.new('ShaderNodeNormalMap');normal_node.inputs['Strength'].default_value=.7
        material.node_tree.links.new(texture.outputs['Color'],normal_node.inputs['Color']);material.node_tree.links.new(normal_node.outputs['Normal'],shader.inputs['Normal'])
    packed=np.zeros((args.size*args.size,4),np.float32);packed[:,3]=.45
    for channel,component in [('metallic',0),('roughness',3)]:
        if channel in maps:
            values=np.empty(args.size*args.size*4,np.float32);maps[channel].pixels.foreach_get(values);values=values.reshape(-1,4)[:,0]
            packed[:,component]=1-values if channel=='roughness' else values
    mask=bpy.data.images.new(key+' mask',width=args.size,height=args.size,alpha=True);mask.colorspace_settings.name='Non-Color';mask.pixels.foreach_set(packed.reshape(-1));mask.filepath_raw=str(folder/'Mask.png');mask.file_format='PNG';mask.save()
    for obj in source_objects:bpy.data.objects.remove(obj,do_unlink=True)
    # Neutral studio evidence; use the same camera for every skin.
    if scene.camera is None:
        minimum=Vector(tuple(min(v[i] for v in verts) for i in range(3)));maximum=Vector(tuple(max(v[i] for v in verts) for i in range(3)));center=(minimum+maximum)/2
        forward=bone_target['headend']-bone_target['head'];forward.z=0;forward.normalize();right=forward.cross(Vector((0,0,1)))
        cam_data=bpy.data.cameras.new('Review');camera=bpy.data.objects.new('Review',cam_data);scene.collection.objects.link(camera)
        camera.location=center+forward*4+right*3+Vector((0,0,2.1));camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();cam_data.type='ORTHO';cam_data.ortho_scale=max(maximum-minimum)*1.35;scene.camera=camera
        for name,position,energy,size in [('Key',center+Vector((3,-4,6)),850,5),('Fill',center+Vector((-4,1,3)),450,4)]:
            light_data=bpy.data.lights.new(name,'AREA');light_data.energy=energy;light_data.shape='DISK';light_data.size=size
            light=bpy.data.objects.new(name,light_data);scene.collection.objects.link(light);light.location=position;light.rotation_euler=(center-position).to_track_quat('-Z','Y').to_euler()
        scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.19,.22,1)
        scene.render.resolution_x=768;scene.render.resolution_y=768;scene.render.resolution_percentage=100;scene.render.film_transparent=True
        scene.view_settings.view_transform='AgX'
    scene.cycles.samples=24;scene.render.filepath=str(folder/'hero.png');bpy.ops.render.render(write_still=True);scene.cycles.samples=1
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/'skin.blend'),compress=True)
    records.append({'key':key,'source':str(FILES[STYLES[key]].relative_to(ROOT)),'matchedBones':len(common),'registrationRmsMeters':rms,'albedo':str(folder/'Albedo.png'),'hero':str(folder/'hero.png')})
    print('SKIN COMPLETE',key,'registration RMS',rms,flush=True)
(OUT/'bake-report.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
