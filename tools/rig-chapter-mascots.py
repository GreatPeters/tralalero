"""Fit short-limb mascot skeletons/actions to the new reviewed TRELLIS bodies."""
import argparse
import json
import math
from pathlib import Path
import sys
from statistics import median
import bpy
import bmesh
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
from mascot_equipment import add_equipment as mascot_equipment

parser = argparse.ArgumentParser()
parser.add_argument('--root', required=True)
parser.add_argument('--only', default='')
parser.add_argument('--revision', default='v1')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root = Path(args.root)
roles = ['ConeMechanic','TrafficPatrol','TollgateChief','TireBruiser','AsphaltWorker','DeliveryRider','SnackChef','CoffeeVendor','ParkingMarshal']
production = root/'outputs/chapters-polish-2026-09-12/enemies'
sources = {}
for index, name in enumerate(roles, 1):
    if args.only and name != args.only: continue
    candidates = list(production.glob(f'{index:02d}_{name}_*/model.glb'))
    if len(candidates) != 1 or not candidates[0].with_name('validation.json').exists():
        if args.only: raise RuntimeError('Validated TRELLIS source not ready: '+name)
        continue
    sources[name] = str(candidates[0].relative_to(root))

def segment_distance(point, a, b):
    delta=b-a
    t=max(0,min(1,(point-a).dot(delta)/max(delta.length_squared,1e-8)))
    return (point-(a+delta*t)).length

def clean_degenerate(mesh):
    bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges))
    faces=[f for f in bm.faces if f.calc_area()<1e-10]
    if faces:bmesh.ops.delete(bm,geom=faces,context='FACES')
    loose=[v for v in bm.verts if not v.link_faces]
    if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    bm.to_mesh(mesh);bm.free();mesh.update()

for name, source in sources.items():
    if args.only and name != args.only: continue
    output=root/'outputs/chapters-polish-2026-09-12/rigged'/args.revision/name
    if all((output/item).exists() for item in (name+'.blend',name+'.fbx',name+'.glb','rig-report.json','ground-checks.json')):
        print('Preserving completed rig: '+name,flush=True);continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(root/source))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for mesh in meshes: mesh.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0]
    bpy.ops.object.join(); mesh=bpy.context.object;mesh.name=name+'_Body'
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    for material in mesh.data.materials:
        if material is None or not material.use_nodes: continue
        shader=material.node_tree.nodes.get('Principled BSDF')
        if shader is None: continue
        for link in list(material.node_tree.links):
            if link.to_node==shader and link.to_socket.name in ('Roughness','Metallic','Normal'):
                material.node_tree.links.remove(link)
        shader.inputs['Roughness'].default_value=.85
        shader.inputs['Metallic'].default_value=0
        if 'Specular IOR Level' in shader.inputs: shader.inputs['Specular IOR Level'].default_value=.15
    points=[v.co for v in mesh.data.vertices]
    minimum=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    maximum=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    h=1.75 if name=='TireBruiser' else 1.6;raw_height=maximum.z-minimum.z
    upper=[p for p in points if minimum.z+raw_height*.70 < p.z < minimum.z+raw_height*.93]
    body_x=median(p.x for p in upper)
    head=[p for p in upper if abs(p.x-body_x)<raw_height*.16]
    body_y=median(p.y for p in head)
    body_top=max(p.z for p in points if abs(p.x-body_x)<raw_height*.14)
    scale=h/(body_top-minimum.z)
    center=Vector((body_x,body_y,minimum.z))
    for vertex in mesh.data.vertices: vertex.co=(vertex.co-center)*scale
    clean_degenerate(mesh.data)
    armature=bpy.data.armatures.new(name+'_Skeleton');rig=bpy.data.objects.new('Rig',armature)
    bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig
    mesh.select_set(False);rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    specs={
        'Root':((0,0,0),(0,0,.08),None),
        'Hips':((0,0,.30),(0,0,.37),'Root'),
        'Spine':((0,0,.37),(0,0,.45),'Hips'),
        'Chest':((0,0,.45),(0,0,.53),'Spine'),
        'Neck':((0,0,.53),(0,0,.58),'Chest'),
        'Head':((0,0,.58),(0,0,1),'Neck'),
    }
    for side,sign in [('L',1),('R',-1)]:
        specs.update({
            'UpperArm.'+side:((sign*.16,0,.49),(sign*.245,0,.375),'Chest'),
            'Forearm.'+side:((sign*.245,0,.375),(sign*.31,-.01,.29),'UpperArm.'+side),
            'Hand.'+side:((sign*.31,-.01,.29),(sign*.34,-.01,.23),'Forearm.'+side),
            'Thigh.'+side:((sign*.10,0,.30),(sign*.13,0,.17),'Hips'),
            'Shin.'+side:((sign*.13,0,.17),(sign*.13,0,.08),'Thigh.'+side),
            'Foot.'+side:((sign*.13,0,.08),(sign*.13,-.13,.045),'Shin.'+side),
        })
    if name in roles:
        for side,sign in [('L',1),('R',-1)]:
            hand_points=[v.co/h for v in mesh.data.vertices if .22<v.co.z/h<.34 and v.co.x/h*sign>.23]
            if hand_points:
                wrist=tuple(median(p[i] for p in hand_points) for i in range(3))
                a,_,parent=specs['Forearm.'+side];specs['Forearm.'+side]=(a,wrist,parent)
                specs['Hand.'+side]=(wrist,(wrist[0],wrist[1],wrist[2]-.045),'Forearm.'+side)
    for bone_name,(a,b,parent) in specs.items():
        bone=armature.edit_bones.new(bone_name);bone.head=Vector(a)*h;bone.tail=Vector(b)*h
        if parent:bone.parent=armature.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    mesh.parent=rig
    modifier=mesh.modifiers.new('Highway deform','ARMATURE');modifier.object=rig
    groups={n:mesh.vertex_groups.new(name=n) for n in specs if n!='Root'}
    # Weld UV/normal duplicates for skin-distance calculations only; preserve the mesh.
    import numpy as np
    import heapq
    coords=np.array([v.co[:] for v in mesh.data.vertices],dtype=float)/h
    _, representatives, inverse=np.unique(np.round(coords/1e-5).astype(np.int64),axis=0,return_index=True,return_inverse=True)
    points=coords[representatives];adj=[{} for _ in points]
    for polygon in mesh.data.polygons:
        ids=[int(inverse[v]) for v in polygon.vertices]
        for a,b in zip(ids,ids[1:]+ids[:1]):
            if a!=b:
                distance=float(np.linalg.norm(points[a]-points[b]))
                adj[a][b]=distance;adj[b][a]=distance
    component_ids=np.full(len(points),-1,dtype=int);components=[]
    for seed in range(len(points)):
        if component_ids[seed]>=0:continue
        cid=len(components);stack=[seed];members=[];component_ids[seed]=cid
        while stack:
            current=stack.pop();members.append(current)
            for neighbor in adj[current]:
                if component_ids[neighbor]<0:component_ids[neighbor]=cid;stack.append(neighbor)
        components.append(members)
    names=list(groups);fields=[]
    for bone_name in names:
        a,b,_=specs[bone_name];a=np.array(a);b=np.array(b)
        distances=np.full(len(points),np.inf);queue=[]
        for t in [.2,.5,.8]:
            sample=a+(b-a)*t;euclidean=np.linalg.norm(points-sample,axis=1)
            for seed in np.argsort(euclidean)[:6]:
                initial=float(euclidean[seed])
                if initial<distances[seed]:
                    distances[seed]=initial;heapq.heappush(queue,(initial,int(seed)))
        while queue:
            distance,current=heapq.heappop(queue)
            if distance>distances[current]:continue
            for neighbor,cost in adj[current].items():
                candidate=distance+cost
                if candidate<distances[neighbor]:
                    distances[neighbor]=candidate;heapq.heappush(queue,(candidate,neighbor))
        fields.append(distances)
    fields=np.array(fields);fallback={}
    for cid,members in enumerate(components):
        center=Vector(points[members].mean(axis=0))
        fallback[cid]=min(names,key=lambda n:segment_distance(center,Vector(specs[n][0]),Vector(specs[n][1])))
    for vertex in mesh.data.vertices:
        if vertex.co.z / h > .58:
            groups['Head'].add([vertex.index], 1, 'REPLACE'); continue
        key=int(inverse[vertex.index]);d=fields[:,key];order=np.argsort(d)[:2]
        if not np.isfinite(d[order[0]]):
            groups[fallback[int(component_ids[key])]].add([vertex.index],1,'REPLACE');continue
        dominant=names[order[0]]
        # Carried props and shoe soles are rigid, even when near a different limb.
        if (dominant.startswith('Hand') and coords[vertex.index,2]<.40) or (dominant.startswith('Foot') and coords[vertex.index,2]<.13):
            groups[dominant].add([vertex.index],1,'REPLACE');continue
        weights=[1/max(float(d[i]),.015)**4 if np.isfinite(d[i]) else 0 for i in order];total=sum(weights)
        for index,weight in zip(order,weights):
            if weight>0:groups[names[index]].add([vertex.index],weight/total,'REPLACE')
    carried=mascot_equipment(name,rig,specs,h)
    scene=bpy.context.scene;scene.render.fps=30;scene.frame_start=1;scene.frame_end=31
    rig.animation_data_create();actions=[];ground_checks=[]
    for action_name in ['idle','walk','run','attack_loop','attack_once','die']:
        action=bpy.data.actions.new(action_name);action.use_fake_user=True;rig.animation_data.action=action
        for frame in range(1,32):
            scene.frame_set(frame)
            t=(frame-1)/30;wave=math.sin(t*math.tau)
            for bone in rig.pose.bones:bone.rotation_mode='XYZ';bone.rotation_euler=(0,0,0);bone.location=(0,0,0)
            rig.pose.bones['Chest'].rotation_euler.x=math.radians(wave*1.2)
            if action_name in ('walk','run'):
                strength=18 if action_name=='walk' else 27
                for side,sign in [('L',1),('R',-1)]:
                    phase=wave*sign
                    rig.pose.bones['Thigh.'+side].rotation_euler.x=math.radians(phase*strength)
                    rig.pose.bones['Shin.'+side].rotation_euler.x=math.radians(-max(0,-phase)*28)
                    rig.pose.bones['Foot.'+side].rotation_euler.x=math.radians(-phase*5)
                    rig.pose.bones['UpperArm.'+side].rotation_euler.x=math.radians(-phase*7)
                rig.pose.bones['Hips'].location.y=abs(wave)*.008
            elif action_name.startswith('attack'):
                swing=math.sin(math.pi*t)**2
                rig.pose.bones['Chest'].rotation_euler.x=math.radians(-swing*9)
                side='L' if name=='AsphaltWorker' else 'R'
                anticipation=-18*math.sin(math.pi*t/.3) if t < .3 else 55*math.sin(math.pi*(t-.3)/.7)
                rig.pose.bones['UpperArm.'+side].rotation_euler.x=math.radians(anticipation)
                rig.pose.bones['Forearm.'+side].rotation_euler.x=math.radians(swing*24)
            elif action_name=='die':
                fall=min(1,t*1.7);fall=fall*fall*(3-2*fall)
                rig.pose.bones['Root'].rotation_euler.x=math.radians(fall*88)
                rig.pose.bones['Root'].location.z=fall*.25
            bpy.context.view_layer.update()
            evaluated=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
            posed=evaluated.to_mesh()
            min_z=min(vertex.co.z for vertex in posed.vertices)
            evaluated.to_mesh_clear()
            root_bone=rig.pose.bones['Root']
            root_bone.location += root_bone.bone.matrix_local.to_3x3().inverted() @ Vector((0,0,-min_z))
            bpy.context.view_layer.update()
            checked=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
            checked_mesh=checked.to_mesh()
            corrected_min=min(vertex.co.z for vertex in checked_mesh.vertices)
            checked.to_mesh_clear()
            ground_checks.append({'action':action_name,'frame':frame,'minZ':corrected_min})
            if abs(corrected_min)>.002: raise RuntimeError(f'Ground correction failed: {name}/{action_name}/{frame}: {corrected_min}')
            for bone in rig.pose.bones:
                bone.keyframe_insert('rotation_euler',frame=frame);bone.keyframe_insert('location',frame=frame)
        actions.append(action)
    rig.animation_data.action=actions[0];scene.frame_set(1)
    output.mkdir(parents=True,exist_ok=True)
    if (output/(name+'.blend')).exists(): raise RuntimeError('Keep prior rig evidence; use another --revision')
    bpy.ops.wm.save_as_mainfile(filepath=str(output/(name+'.blend')))
    bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True)
    for part in carried:part.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(output/(name+'.fbx')),use_selection=True,add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
    bpy.ops.export_scene.gltf(filepath=str(output/(name+'.glb')),export_format='GLB',use_selection=True,
        export_animations=True,export_animation_mode='ACTIONS')
    report={'name':name,'source':source,'height':h,'vertices':len(mesh.data.vertices),'triangles':sum(len(p.vertices)-2 for p in mesh.data.polygons),
        'bones':len(armature.bones),'actions':[a.name for a in actions],'separateCarriedParts':[p.name for p in carried]}
    (output/'rig-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    (output/'ground-checks.json').write_text(json.dumps(ground_checks,indent=2),encoding='utf-8')
    rig.animation_data.action=actions[1];scene.frame_set(8)
    bpy.ops.wm.save_as_mainfile(filepath=str(output/'walk-preview.blend'))
    print(json.dumps(report),flush=True)
