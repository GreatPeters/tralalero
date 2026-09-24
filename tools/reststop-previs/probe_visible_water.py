import bpy, json
from pathlib import Path
OUT=Path.cwd()/'outputs/reststop-blender-300s-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'reststop-master.blend'));s=bpy.context.scene
result={}
for t in (90,99.5,112):
    s.frame_set(round(t*24)+1);rows=[]
    for o in s.objects:
        if o.type!='MESH' or not any(m and m.name in ('water','reticle') for m in o.data.materials):continue
        scale=o.matrix_world.to_scale()
        if min(abs(v) for v in scale)>.01 and not o.hide_render:
            rows.append({'name':o.name,'position':list(o.matrix_world.translation),'scale':list(scale),'parent':o.parent.name if o.parent else None})
    result[str(t)]=rows
(OUT/'water-visibility-probe.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
