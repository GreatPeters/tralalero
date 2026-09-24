"""Apply the reproducible glass-quality correction to a built master."""
import bpy
from pathlib import Path
out=Path.cwd()/'outputs/reststop-blender-300s-2026-09-23'
bpy.ops.wm.open_mainfile(filepath=str(out/'reststop-master.blend'))
scene=bpy.context.scene
print('IMPORTED_TIMING',scene.render.fps,scene.render.fps_base,scene.frame_end,flush=True)
scene.render.fps=24;scene.render.fps_base=1;scene.frame_start=1;scene.frame_end=7200
for m in bpy.data.materials:
    if m.name in ('glass','water','warning'):m.surface_render_method='BLENDED'
for o in list(scene.objects):
    if o.name.startswith('Main_name'):
        for t,v in [(0,False),(84,False),(84+1/24,True),(101,True),(101+1/24,False)]:
            o.hide_render=v;o.keyframe_insert(data_path='hide_render',frame=round(t*24)+1)
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'reststop-master.blend'),compress=True)
print('POLISH_COMPLETE',flush=True)
