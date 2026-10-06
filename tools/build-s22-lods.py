"""Generate two UV-preserving mesh LODs from existing static FBX sources."""
from pathlib import Path
import bpy, bmesh, json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ShooterSurvival/Models/Generated/S22Polish/LODs'
OUT.mkdir(parents=True,exist_ok=True)
selection=[('014_STAGE01','shop'),('016_STAGE01','display'),('015_STAGE01','restaurant'),('040_STAGE01','scatter'),('065_STAGE02','skyline')]
records=[]
for prefix,label in selection:
 sources=list((ROOT/'Assets/ShooterSurvival/Models/MeshyAI').rglob(prefix+'*.fbx'))
 if len(sources)!=1:raise RuntimeError((prefix,sources))
 src=sources[0]
 for level,cap in [(1,5000 if label!='skyline' else 8000),(2,1200 if label!='skyline' else 2000)]:
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(src))
  meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];before=after=0
  for o in meshes:
   if level==2:
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=max(o.dimensions)*.00001);bm.normal_update();bm.to_mesh(o.data);bm.free();o.data.validate()
   o.data.calc_loop_triangles();n=len(o.data.loop_triangles);before+=n
   mod=o.modifiers.new('mobile silhouette LOD','DECIMATE');mod.ratio=min(1,cap/n);mod.use_collapse_triangulate=True
   bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name);o.data.calc_loop_triangles();after+=len(o.data.loop_triangles)
  bpy.ops.object.select_all(action='DESELECT')
  for o in meshes:o.select_set(True)
  dest=OUT/f'{label}_LOD{level}.fbx'
  bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
  records.append({'source':str(src.relative_to(ROOT)).replace('\\','/'),'label':label,'level':level,'path':str(dest.relative_to(ROOT)).replace('\\','/'),'before':before,'after':after})
(ROOT/'outputs/s22-polish-2026-10-01/lods.json').write_text(json.dumps(records,indent=2),encoding='utf-8');print(json.dumps(records))
