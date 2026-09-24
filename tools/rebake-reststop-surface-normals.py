"""Rejected B03 nearest-normal experiment; thin repeated surfaces need geometric checks."""
import argparse,json,runpy,sys
from pathlib import Path
import bpy

parser=argparse.ArgumentParser();parser.add_argument('--source-blend',required=True);parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(args.source_blend).resolve();out=Path(args.output).resolve()
assert not (out/'model.blend').exists(),'Choose a new repair revision'
out.mkdir(parents=True,exist_ok=True)
app=next(p for p in (Path.home()/'Desktop').glob('AI */Trellis */*') if (p/'blender_refine.py').is_file())
library=runpy.run_path(str(app/'blender_refine.py'),run_name='reststop_refine_library')
bpy.ops.wm.open_mainfile(filepath=str(source))
high=bpy.data.objects['Detailed_Source'];low=bpy.data.objects['Asset_Optimized']
for obj in list(bpy.context.scene.objects):
    if obj not in (high,low):bpy.data.objects.remove(obj,do_unlink=True)
high.hide_set(False);high.hide_render=False
library['active'](low)
positions=[tuple(v.co) for v in low.data.vertices]
uvs=[tuple(v.uv) for v in low.data.uv_layers.active.data]
before=library['tris'](low)
modifier=low.modifiers.new('Original_surface_normals','DATA_TRANSFER');modifier.object=high
modifier.use_loop_data=True;modifier.data_types_loops={'CUSTOM_NORMAL'};modifier.loop_mapping='POLYINTERP_NEAREST'
bpy.ops.object.modifier_apply(modifier=modifier.name)
assert positions==[tuple(v.co) for v in low.data.vertices]
assert uvs==[tuple(v.uv) for v in low.data.uv_layers.active.data]
lo,hi=library['bounds'](high)
library['bake'](high,low,out,2048,(hi-lo).length)
for obj in bpy.context.scene.objects:obj.select_set(obj==low)
bpy.context.view_layer.objects.active=low
assert library['tris'](low)==before and before<=15000
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/'model.blend'))
bpy.ops.export_scene.fbx(filepath=str(out/'model.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,add_leaf_bones=False)
bpy.ops.export_scene.gltf(filepath=str(out/'model.glb'),use_selection=True,export_format='GLB')
(out/'repair.json').write_text(json.dumps({'source':str(source),'method':'interpolated custom loop-normal transfer from retained Detailed_Source followed by fresh 2048 PBR/tangent-normal bake on unchanged UVs',
    'triangles_before':before,'triangles_after':library['tris'](low),'vertex_positions_unchanged':True,'uvs_unchanged':True,'normal_map':'new bake matches transferred normals'},indent=2),encoding='utf8')
script=Path(__file__).with_name('reststop-production-review-render.py');sys.argv=[str(script),'--',str(out)]
runpy.run_path(str(script),run_name='__main__')
import shutil
shutil.copy2(out/'quality/hero.png',out/'preview.png')
