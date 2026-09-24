import bpy,json
from pathlib import Path
from mathutils import Vector
out=Path.cwd()/'outputs/reststop-blender-v2-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(out/'reststop-v2.blend'));s=bpy.context.scene;s.frame_set(1)
p=bpy.data.objects['PLAYER_ROOT'];inv=p.matrix_world.inverted();rows=[]
for o in p.children_recursive:
 if o.type in ('MESH','ARMATURE') or 'calibration' in o.name:
  row={'name':o.name,'parent':o.parent.name,'scale':list(o.scale),'dimensions':list(o.dimensions)}
  if o.type=='MESH':
   pts=[inv@o.matrix_world@v.co for v in o.data.vertices];row['vertex_size']=[max(v[i] for v in pts)-min(v[i] for v in pts) for i in range(3)]
   ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());pts=[inv@ev.matrix_world@v.co for v in ev.data.vertices];row['evaluated_size']=[max(v[i] for v in pts)-min(v[i] for v in pts) for i in range(3)]
  rows.append(row)
print('PLAYER_INSPECT',json.dumps(rows),flush=True)
