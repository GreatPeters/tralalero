"""Remove the unneeded privacy screen crossing the common restroom approach."""
import bpy,bmesh,json,shutil
from pathlib import Path
from mathutils import Vector
OUT=Path.cwd()/'outputs/reststop-blender-300s-2026-09-23';source=OUT/'reststop-master.blend'
backup=OUT/'reststop-master-before-aisle-clearance.blend'
if not backup.exists():shutil.copy2(source,backup)
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.context.scene;s.frame_set(1)
if s.get('restroom_aisle_cleared'):raise RuntimeError('Aisle correction already applied')
o=bpy.data.objects['08_RESTROOM__wood_light'];selected=[];world=[]
for v in o.data.vertices:
    p=o.matrix_world@v.co
    if 71.949<p.x<72.051 and 18.999<p.y<24.001 and .074<p.z<2.926:selected.append(v.index);world.append(p)
assert 8<=len(selected)<=100,len(selected)
extents=[max(p[i] for p in world)-min(p[i] for p in world) for i in range(3)]
assert all(abs(a-b)<.0001 for a,b in zip(extents,(.1,5,2.85))),extents
o.data=o.data.copy();bm=bmesh.new();bm.from_mesh(o.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.verts[i] for i in selected],context='VERTS');bm.to_mesh(o.data);bm.free();o.data.update()
s['restroom_aisle_cleared']=True;s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True)
report={'removed_vertices':len(selected),'removed_component_extents':extents,'reason':'Clear the continuous store/restroom common aisle; preserve cubicles and room scale.'}
(OUT/'restroom-aisle-correction.json').write_text(json.dumps(report,indent=2))
patch={'frames':list(range(4561,6073)),'count':1512,'stages':['07','08','09'],'reason':'Final steeper restroom camera plus removal of a screen that blocked the common aisle. Entire store and restroom intervals, and first3s of fuel interval, are regenerated.'}
(OUT/'restroom-final-patch.json').write_text(json.dumps(patch,indent=2));print('AISLE_CLEAR',json.dumps(report),flush=True)
