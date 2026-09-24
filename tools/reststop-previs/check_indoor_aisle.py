"""Verify the actual native route center does not cross fixed interior geometry."""
import bpy,json
from pathlib import Path
from mathutils import Vector
OUT=Path.cwd()/'outputs/reststop-blender-300s-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-master.blend'));s=bpy.context.scene;p=bpy.data.objects['PLAYER_ROOT'];hits=[];prior=None
for f in range(190*24+1,253*24+2,3):
    s.frame_set(f);target=p.matrix_world.translation+Vector((0,0,1.2))
    if prior is not None and (target-prior).length>.0001:
        origin=prior.copy();direction=(target-origin).normalized();deps=bpy.context.evaluated_depsgraph_get()
        for _ in range(16):
            hit,point,normal,index,obj,matrix=s.ray_cast(deps,origin,direction,distance=(target-origin).length)
            if not hit:break
            chain=[obj];parent=obj.parent
            while parent is not None:chain.append(parent);parent=parent.parent
            ignored=obj.hide_render or any(a.get('role') or a.name=='PLAYER_ROOT' or a.animation_data for a in chain) or all(m and m.diffuse_color[3]<.5 for m in obj.data.materials)
            if not ignored:hits.append({'frame':f,'object':obj.name,'hit':list(point)});break
            origin=point+direction*.001
    prior=target.copy()
report={'frames_sampled':505,'height_above_player_root':1.2,'range_seconds':[190,253],'static_centerline_blockers':hits,'scope':'Centerline/static architectural check; not a swept player collision hull or game physics test.'}
(OUT/'indoor-aisle-check.json').write_text(json.dumps(report,indent=2));print('AISLE_CHECK',json.dumps(report),flush=True)
assert not hits,hits
