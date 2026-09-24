"""Shared nine-action authoring; called with the prepared body and skeleton globals."""
scene=bpy.context.scene;scene.render.fps=30;rig.animation_data_create();actions=[];checks=[]
def rotate(name,x=0,y=0,z=0):
    bone=rig.pose.bones[name];rest=bone.bone.matrix_local.to_quaternion()
    q=Quaternion((0,0,1),math.radians(z))@Quaternion((0,1,0),math.radians(y))@Quaternion((1,0,0),math.radians(x))
    bone.rotation_quaternion=rest.inverted()@q@rest
def move(name,world):
    bone=rig.pose.bones[name];bone.location=bone.bone.matrix_local.to_3x3().inverted()@Vector(world)
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def envelope(t,a,b,c):
    return smooth((t-a)/(b-a)) if t<b else 1-smooth((t-b)/(c-b))
attack_seconds=1.2
durations={'idle':2.4,'walk':.9,'run':.65,'attack_loop':attack_seconds,'attack_once':attack_seconds,'hit':.3,'die':1.0}
durations['greet']=1.8
special={'Traveler':'scared','Cleaner':'clean','ParkingMarshal':'signal','SnackChef':'serve','CoffeeVendor':'serve','Cashier':'serve','FuelAttendant':'signal','Police':'guard'}[args.name]
durations[special]=1.8
durations={name:round(seconds*30)/30 for name,seconds in durations.items()}
for action_name,duration in durations.items():
    frames=round(duration*30);action=bpy.data.actions.new(action_name);action.use_fake_user=True;rig.animation_data.action=action;scene.frame_start=1;scene.frame_end=frames+1
    for frame in range(1,frames+2):
        scene.frame_set(frame);t=(frame-1)/frames;wave=math.sin(t*math.tau)
        for bone in rig.pose.bones:bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.location=(0,0,0);bone.scale=(1,1,1)
        rotate('Chest',x=wave*2.5,z=wave*.8);rotate('Head',y=wave*3.5,z=math.sin(t*math.tau+.8)*2)
        for side,sign in [('L',1),('R',-1)]:
            rotate('UpperArm.'+side,x=-8+wave*2.5*sign,y=sign*12);rotate('Forearm.'+side,x=-18+wave*3*sign)
        if action_name in ('walk','run'):
            amplitude=24 if action_name=='walk' else 35;rotate('Chest',x=5 if action_name=='walk' else 9)
            for side,sign in [('L',1),('R',-1)]:
                phase=wave*sign;thigh_angle=phase*amplitude;shin_angle=-max(0,phase)*38
                rotate('Thigh.'+side,x=thigh_angle);rotate('Shin.'+side,x=shin_angle)
                rotate('Foot.'+side,x=-thigh_angle-shin_angle)
                rotate('UpperArm.'+side,x=-8-phase*18,y=sign*12);rotate('Forearm.'+side,x=-25-max(0,-phase)*15)
            # Root grounding owns vertical locomotion; avoid FBX dropping a
            # second pelvis-translation channel on this short connected rig.
        elif action_name.startswith('attack'):
            wind=envelope(t,0,.26,.52);strike=envelope(t,.26,.46,.82)
            rotate('Chest',x=-7*wind+13*strike,z=7*wind-10*strike);rotate('Head',x=4*wind-7*strike,z=-5*wind+5*strike)
            rotate('UpperArm.R',x=-8-98*wind-45*strike,y=-12-10*wind,z=-7*strike)
            rotate('Forearm.R',x=-18-42*wind+7*strike);rotate('Hand.R',x=-5*strike)
            rotate('UpperArm.L',x=-8-35*wind+12*strike,y=12);rotate('Forearm.L',x=-18-25*wind)
            move('Hips',(0,-.07*strike,0))
            if args.name=='AsphaltWorker':
                rotate('Chest',x=15*strike,z=15*wind-18*strike);rotate('UpperArm.R',x=-8-75*wind-55*strike,y=-12,z=10*wind-18*strike)
            elif args.name=='TireBruiser':
                rotate('Chest',x=-12*wind+18*strike,z=18*wind-22*strike);rotate('UpperArm.R',x=-8-110*wind-65*strike,y=-18);move('Hips',(0,-.12*strike,0))
            elif args.name=='TrafficPatrol':
                rotate('Chest',x=-7*strike,z=2*wind);rotate('UpperArm.R',x=-55+12*strike,y=-12);rotate('Forearm.R',x=-28);rotate('UpperArm.L',x=-22,y=15);rotate('Head',x=-3*strike)
            elif args.name=='DeliveryRider':
                rotate('UpperArm.R',x=-8+35*wind-85*strike,y=-12);rotate('Forearm.R',x=-18-12*wind+8*strike);rotate('Chest',x=-6*wind+13*strike,z=9*wind-13*strike)
            elif args.name=='TollgateChief':
                rotate('UpperArm.L',x=-35-35*wind,y=20,z=12*strike);rotate('Chest',x=-5*wind+9*strike,z=14*wind-16*strike)
        elif action_name=='hit':
            hit=envelope(t,0,.23,1);rotate('Chest',x=-15*hit,z=6*hit);rotate('Head',x=-12*hit);move('Hips',(0,.09*hit,0))
        elif action_name=='die':
            fall=smooth(t/.72);rotate('Root',x=-88*fall,z=-8*fall);rotate('Chest',x=-12*fall)
            rotate('UpperArm.R',x=-35*fall,y=-20);rotate('UpperArm.L',x=-45*fall,y=20)
        elif action_name in ('greet','signal'):
            lift=envelope(t,0,.25,1)
            rotate('UpperArm.R',x=-8-65*lift,y=-18,z=-10*lift)
            rotate('Forearm.R',x=-18-30*lift)
            rotate('Hand.R',z=math.sin(t*math.tau*3)*15*lift)
        elif action_name=='scared':
            lift=envelope(t,0,.2,1);rotate('Chest',x=-10*lift);rotate('Head',x=-7*lift)
            for side,sign in [('L',1),('R',-1)]:
                rotate('UpperArm.'+side,x=-8-85*lift,y=sign*22)
                rotate('Forearm.'+side,x=-18-30*lift)
        elif action_name in ('serve','clean','guard'):
            reach=envelope(t,0,.35,1)
            rotate('Chest',x=(12 if action_name=='clean' else 4)*reach)
            for side,sign in [('L',1),('R',-1)]:
                rotate('UpperArm.'+side,x=-8-40*reach+(12*wave if action_name=='clean' else 0),y=sign*8)
                rotate('Forearm.'+side,x=-18-25*reach)
        bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();evaluated=body.evaluated_get(dg);mesh=evaluated.to_mesh();low=min(v.co.z for v in mesh.vertices);evaluated.to_mesh_clear()
        rig.pose.bones['Root'].location+=rig.pose.bones['Root'].bone.matrix_local.to_3x3().inverted()@Vector((0,0,-low))
        bpy.context.view_layer.update()
        for bone in rig.pose.bones:bone.keyframe_insert('rotation_quaternion',frame=frame);bone.keyframe_insert('location',frame=frame)
        evaluated=body.evaluated_get(dg);mesh=evaluated.to_mesh();checked=min(v.co.z for v in mesh.vertices);evaluated.to_mesh_clear();checks.append({'action':action_name,'frame':frame,'ground':checked})
    actions.append(action)
rig.animation_data.action=actions[0];scene.frame_start=1;scene.frame_end=73;scene.frame_set(1)
for side in ('L','R'):
    socket=bpy.data.objects.new('Grip.'+side,None);bpy.context.collection.objects.link(socket)
    socket.parent=rig;socket.parent_type='BONE';socket.parent_bone='Hand.'+side
    socket.location=(0,0,0)
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
for obj in rig.children:
    if obj.type=='EMPTY':obj.select_set(True)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/(args.name+'.blend')))
bpy.ops.export_scene.fbx(filepath=str(out/(args.name+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(out/(args.name+'.glb')),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS')
report={'name':args.name,'source':args.source,'height':height,'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'bones':len(armature.bones),'actions':durations,'skeleton':{n:(physical(a),physical(b),p) for n,(a,b,p) in specs.items()},'maximumGroundError':max(abs(c['ground']) for c in checks),'rigged':True,'equipment_binding':'Separate prop assets; Grip.L/R sockets provided, equipment is not attached','compact_proportions':compact,'materials':'original TRELLIS PBR maps retained'}
(out/'rig-report.json').write_text(json.dumps(report,indent=2));(out/'ground-checks.json').write_text(json.dumps(checks,indent=2))
print(json.dumps(report),flush=True)
