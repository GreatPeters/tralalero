import bpy,json
from pathlib import Path
from mathutils import Vector
OUT=Path.cwd()/'outputs/reststop-blender-300s-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-master.blend'));s=bpy.context.scene;results=[]
for t in (218,222,231,237,247,251):
    s.frame_set(t*24+1);deps=bpy.context.evaluated_depsgraph_get();p=bpy.data.objects['PLAYER_ROOT'].matrix_world.translation
    probes=[]
    for z in (1.2,2.0,2.7):
        origin=s.camera.matrix_world.translation.copy();target=p+Vector((0,0,z));direction=(target-origin).normalized();blocked=None
        for _ in range(24):
            remaining=(target-origin).length
            hit,point,normal,index,obj,matrix=s.ray_cast(deps,origin,direction,distance=remaining)
            if not hit:break
            transparent=obj.hide_render or all(m and m.diffuse_color[3]<.5 for m in obj.data.materials)
            chain=[obj];parent=obj.parent
            while parent is not None:chain.append(parent);parent=parent.parent
            if any(o.name=='PLAYER_ROOT' for o in chain):break
            if not transparent:blocked=obj.name;break
            origin=point+direction*.002
        probes.append({'height':z,'blocker':blocked})
    visible=any(r['blocker'] is None for r in probes)
    results.append({'time':t,'camera':list(s.camera.location),'probes':probes,'player_body_visible':visible})
assert all(r['player_body_visible'] for r in results),results
(OUT/'restroom-camera-visibility.json').write_text(json.dumps(results,indent=2));print('RESTROOM_CAMERA_VISIBILITY_PASS',flush=True)
