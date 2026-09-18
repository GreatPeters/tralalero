"""Add a compact generic skeleton/actions to reviewed TRELLIS meshes; preserve sources."""
import argparse
import json
import math
from pathlib import Path
import sys
from statistics import median
import bpy
import bmesh
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('--root', required=True)
parser.add_argument('--only', default='')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root = Path(args.root)
sources = {
    'ConeMechanic': 'outputs/highway-enemies-2026-09-10/01-mechanic_4a97f10f4c/model.glb',
    'TrafficPatrol': 'outputs/highway-enemies-2026-09-10/02-patrol_cd22a7a411/model.glb',
    'TollgateChief': 'outputs/highway-enemies-2026-09-10/03-chief_49f79d0151/model.glb',
    'TireBruiser': 'outputs/highway-enemies-2026-09-11/04-TireBruiser_445d3dcc08/model.glb',
    'AsphaltWorker': 'outputs/highway-enemies-2026-09-11/05-AsphaltWorker_1c1da4d221/model.glb',
    'DeliveryRider': 'outputs/highway-enemies-2026-09-11/06-DeliveryRider_77af4c3dee/model.glb',
}
for character in ('TireBruiser','AsphaltWorker','DeliveryRider'):
    candidates=list((root/'outputs/highway-bodies-2026-09-11').glob(character+'_*/model.glb'))
    if len(candidates)!=1:raise RuntimeError('Expected one body-only source for '+character)
    sources[character]=str(candidates[0].relative_to(root))

def carry_parts(name, rig, specs, height):
    if name not in ('TireBruiser','AsphaltWorker','DeliveryRider'):return []
    hand='Hand.L' if name=='AsphaltWorker' else 'Hand.R'
    grip=Vector(specs[hand][0])*height
    parts=[]
    colors={'Rubber':(.028,.033,.038,1),'Tread':(.045,.051,.055,1),'Steel':(.22,.27,.30,1),
            'Orange':(.83,.25,.04,1),'Cardboard':(.55,.35,.16,1),'Tape':(.85,.64,.26,1)}
    materials={}
    def finish(obj,kind):
        obj.name='Carry_'+kind
        if kind not in materials:
            material=bpy.data.materials.new('Carry_'+kind);material.use_nodes=True
            shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=colors[kind]
            shader.inputs['Roughness'].default_value=.78 if kind in ('Rubber','Tread','Cardboard') else .42
            materials[kind]=material
        obj.data.materials.append(materials[kind])
        bpy.context.view_layer.objects.active=obj
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        clean_degenerate(obj.data)
        for polygon in obj.data.polygons:polygon.use_smooth=True
        obj.parent=rig;group=obj.vertex_groups.new(name=hand);group.add(range(len(obj.data.vertices)),1,'REPLACE')
        modifier=obj.modifiers.new('Rigid carried part','ARMATURE');modifier.object=rig
        parts.append(obj);return obj
    def box(kind,position,size,angle=0):
        bpy.ops.mesh.primitive_cube_add(size=1,location=position);obj=bpy.context.object;obj.scale=size;obj.rotation_euler.x=angle
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        bevel=obj.modifiers.new('Manufactured edges','BEVEL');bevel.width=min(.008,min(size)*.2);bevel.segments=2;bpy.ops.object.modifier_apply(modifier=bevel.name)
        return finish(obj,kind)
    def ring(kind,position,major,minor,axis_x=False):
        bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=48,minor_segments=12,location=position)
        obj=bpy.context.object
        if axis_x:obj.rotation_euler.y=math.pi/2
        return finish(obj,kind)
    if name=='TireBruiser':
        center=grip+Vector((0,0,-.49));ring('Rubber',center,.38,.11,True)
        for side in (-1,1):ring('Rubber',center+Vector((side*.105,0,0)),.34,.018,True)
        for i in range(40):
            angle=i*math.tau/40
            for side in (-1,1):
                position=center+Vector((side*.053,math.sin(angle)*.48,math.cos(angle)*.48))
                box('Tread',position,(.11,.075,.025),-angle)
    elif name=='AsphaltWorker':
        box('Orange',grip+Vector((0,0,-.25)),(.23,.25,.38))
        box('Steel',grip+Vector((0,0,-.57)),(.055,.055,.45))
        box('Steel',grip+Vector((0,0,-.79)),(.34,.42,.045))
        for i in range(5):ring('Rubber',grip+Vector((0,0,-.45-i*.045)),.095,.018)
        for side in (-1,1):box('Steel',grip+Vector((side*.13,0,-.04)),(.035,.035,.29))
        box('Rubber',grip+Vector((0,0,.105)),(.29,.055,.055))
    else:
        center=grip+Vector((-.055,-.06,-.16));box('Cardboard',center,(.38,.38,.32))
        box('Tape',center+Vector((0,0,.164)),(.065,.384,.008));box('Tape',center+Vector((0,-.194,0)),(.065,.008,.32))
    # Join pieces with the same surface into one draw per material.
    joined=[]
    grouped={kind:[p for p in parts if p.data.materials[0]==material] for kind,material in materials.items()}
    for kind,objects in grouped.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        if len(objects)>1:bpy.ops.object.join()
        joined.append(bpy.context.object)
    return joined

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
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(root/source))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for mesh in meshes: mesh.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0]
    bpy.ops.object.join(); mesh=bpy.context.object;mesh.name=name+'_Body'
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    points=[v.co for v in mesh.data.vertices]
    minimum=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    maximum=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    h=2.2;raw_height=maximum.z-minimum.z
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
        'Hips':((0,0,.49),(0,0,.57),'Root'),
        'Spine':((0,0,.57),(0,0,.67),'Hips'),
        'Chest':((0,0,.67),(0,0,.78),'Spine'),
        'Neck':((0,0,.78),(0,0,.84),'Chest'),
        'Head':((0,0,.84),(0,0,1),'Neck'),
    }
    for side,sign in [('L',1),('R',-1)]:
        specs.update({
            'UpperArm.'+side:((sign*.16,0,.755),(sign*.235,0,.59),'Chest'),
            'Forearm.'+side:((sign*.235,0,.59),(sign*.30,-.01,.455),'UpperArm.'+side),
            'Hand.'+side:((sign*.30,-.01,.455),(sign*.32,-.01,.40),'Forearm.'+side),
            'Thigh.'+side:((sign*.09,0,.49),(sign*.10,0,.265),'Hips'),
            'Shin.'+side:((sign*.10,0,.265),(sign*.115,0,.07),'Thigh.'+side),
            'Foot.'+side:((sign*.115,0,.07),(sign*.115,-.12,.04),'Shin.'+side),
        })
    if name in ('TireBruiser','AsphaltWorker','DeliveryRider'):
        for side,sign in [('L',1),('R',-1)]:
            hand_points=[v.co/h for v in mesh.data.vertices if .38<v.co.z/h<.53 and v.co.x/h*sign>.23]
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
    carried=carry_parts(name,rig,specs,h)
    scene=bpy.context.scene;scene.render.fps=30;scene.frame_start=1;scene.frame_end=31
    rig.animation_data_create();actions=[]
    for action_name in ['idle','walk','run','attack_loop','attack_once','die']:
        action=bpy.data.actions.new(action_name);action.use_fake_user=True;rig.animation_data.action=action
        for frame in range(1,32):
            t=(frame-1)/30;wave=math.sin(t*math.tau)
            for bone in rig.pose.bones:bone.rotation_mode='XYZ';bone.rotation_euler=(0,0,0);bone.location=(0,0,0)
            rig.pose.bones['Chest'].rotation_euler.x=math.radians(wave*1.2)
            if action_name in ('walk','run'):
                strength=20 if action_name=='walk' else 30
                for side,sign in [('L',1),('R',-1)]:
                    phase=wave*sign
                    rig.pose.bones['Thigh.'+side].rotation_euler.x=math.radians(phase*strength)
                    rig.pose.bones['Shin.'+side].rotation_euler.x=math.radians(-max(0,-phase)*28)
                    rig.pose.bones['Foot.'+side].rotation_euler.x=math.radians(-phase*5)
                    rig.pose.bones['UpperArm.'+side].rotation_euler.x=math.radians(-phase*7)
                rig.pose.bones['Hips'].location.z=abs(wave)*.018
            elif action_name.startswith('attack'):
                swing=math.sin(math.pi*t)**2
                rig.pose.bones['Chest'].rotation_euler.x=math.radians(-swing*9)
                rig.pose.bones['UpperArm.R'].rotation_euler.x=math.radians(swing*38)
                rig.pose.bones['Forearm.R'].rotation_euler.x=math.radians(swing*24)
            elif action_name=='die':
                fall=min(1,t*1.7);fall=fall*fall*(3-2*fall)
                rig.pose.bones['Root'].rotation_euler.x=math.radians(fall*88)
                rig.pose.bones['Root'].location.z=fall*.25
            for bone in rig.pose.bones:
                bone.keyframe_insert('rotation_euler',frame=frame);bone.keyframe_insert('location',frame=frame)
        actions.append(action)
    rig.animation_data.action=actions[0];scene.frame_set(1)
    output=root/'outputs/highway-rigged-2026-09-11'/name;output.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(output/(name+'.blend')))
    bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True)
    for part in carried:part.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(output/(name+'.fbx')),use_selection=True,add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,path_mode='COPY',embed_textures=True)
    bpy.ops.export_scene.gltf(filepath=str(output/(name+'.glb')),export_format='GLB',use_selection=True,
        export_animations=True,export_animation_mode='ACTIONS')
    report={'name':name,'source':source,'height':h,'vertices':len(mesh.data.vertices),'triangles':sum(len(p.vertices)-2 for p in mesh.data.polygons),
        'bones':len(armature.bones),'actions':[a.name for a in actions],'separateCarriedParts':[p.name for p in carried]}
    (output/'rig-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    rig.animation_data.action=actions[1];scene.frame_set(8)
    bpy.ops.wm.save_as_mainfile(filepath=str(output/'walk-preview.blend'))
    print(json.dumps(report),flush=True)
