import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent.parent;source=root/'outputs/skins-reststop-2026-09-12/production/reststop/reststop_hall_6beaa75417';out=root/'outputs/reststop-repairs-2026-09-12/reststop_hall'
bpy.ops.wm.open_mainfile(filepath=str(source/'model.blend'));high=bpy.data.objects['Detailed_Source'];low=bpy.data.objects['Asset_Optimized']
def bounds(obj):
    points=[obj.matrix_world@Vector(v) for v in obj.bound_box]
    return Vector([min(p[i] for p in points) for i in range(3)]),Vector([max(p[i] for p in points) for i in range(3)])
a,b=bounds(low);c,d=bounds(high);high.scale*=(b-a).length/(d-c).length;bpy.context.view_layer.update();c,d=bounds(high);high.location+=(a+b-c-d)*.5
high.hide_render=False;high.hide_set(False);low.hide_render=True
scene=bpy.context.scene
if scene.render.engine=='CYCLES':scene.cycles.device='CPU';scene.cycles.samples=12
scene.render.resolution_x=1024;scene.render.resolution_y=768;scene.render.resolution_percentage=100;scene.render.filepath=str(out/'detailed-preview.png');bpy.ops.render.render(write_still=True)
