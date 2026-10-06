"""Refine a task-generated sneaker crown, export Unity FBX + portable GLB + LODs."""
import argparse,json,math,sys
from pathlib import Path
import bpy,bmesh
from mathutils import Vector

def args():
 p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--output',required=True);p.add_argument('--trim-below',type=float);p.add_argument('--rotate-z',type=float,default=0);return p.parse_args(sys.argv[sys.argv.index('--')+1:])
def active(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def bounds(o):
 vs=[o.matrix_world@v.co for v in o.data.vertices];return Vector(tuple(min(v[i] for v in vs) for i in range(3))),Vector(tuple(max(v[i] for v in vs) for i in range(3)))
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
def stats(o):
 bm=bmesh.new();bm.from_mesh(o.data);lo,hi=bounds(o)
 result={'name':o.name,'triangles':tris(o),'vertices':len(bm.verts),'dimensions':list(hi-lo),'bounds_min':list(lo),'bounds_max':list(hi),'uv_layers':len(o.data.uv_layers),'materials':len(o.data.materials),'nonfinite':sum(not math.isfinite(x) for v in bm.verts for x in v.co),'degenerate_faces':sum(f.calc_area()<1e-12 for f in bm.faces),'loose_vertices':sum(not v.link_edges for v in bm.verts),'boundary_edges':sum(e.is_boundary for e in bm.edges),'nonmanifold_junctions':sum(len(e.link_faces)>2 for e in bm.edges)};bm.free();return result
def export(o,path):
 active(o);bpy.ops.export_scene.gltf(filepath=str(path.with_suffix('.glb')),export_format='GLB',use_selection=True,export_yup=True,export_apply=True)
 bpy.ops.export_scene.fbx(filepath=str(path.with_suffix('.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=False,add_leaf_bones=False)

a=args();out=Path(a.output);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=a.input)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes:active(o);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();shoe=meshes[0];shoe.name='ShoeCrown_LOD0'
before=stats(shoe)
bm=bmesh.new();bm.from_mesh(shoe.data)
if a.trim_below is not None:
 bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=(0,0,a.trim_below),plane_no=(0,0,1),clear_inner=True,dist=1e-6)
 edges=[e for e in bm.edges if e.is_boundary and all(abs(v.co.z-a.trim_below)<1e-4 for v in e.verts)]
 if edges:bmesh.ops.holes_fill(bm,edges=edges,sides=0)
bad=[f for f in bm.faces if f.calc_area()<1e-12]
if bad:bmesh.ops.delete(bm,geom=bad,context='FACES_ONLY')
loose=[v for v in bm.verts if not v.link_faces]
if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(shoe.data);bm.free();shoe.data.update()
shoe.rotation_euler.z=math.radians(a.rotate_z);active(shoe);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
lo,hi=bounds(shoe);scale=1/max(hi.x-lo.x,hi.y-lo.y)
for v in shoe.data.vertices:v.co=(v.co-Vector(((hi.x+lo.x)/2,(hi.y+lo.y)/2,lo.z)))*scale
for p in shoe.data.polygons:p.use_smooth=True
for m in shoe.data.materials:
 if m and m.use_nodes:
  for n in m.node_tree.nodes:
   if n.type=='BSDF_PRINCIPLED':n.inputs['Metallic'].default_value=0.12;n.inputs['Roughness'].default_value=0.62
   if n.type=='TEX_IMAGE' and n.image:
    img=n.image
    if max(img.size)>2048:img.scale(2048,2048)
    if img.packed_file:img.unpack(method='REMOVE')
    img.filepath_raw=str(out/(img.name.replace('.','_')+'.png'));img.file_format='PNG';img.save()
lod_stats=[]
for index,budget in enumerate([15000,6000,2000]):
 o=shoe if index==0 else shoe.copy()
 if index:o.data=shoe.data.copy();bpy.context.collection.objects.link(o)
 o.name='ShoeCrown_LOD'+str(index);active(o)
 if tris(o)>budget:
  mod=o.modifiers.new('SilhouettePreservingLOD','DECIMATE');mod.ratio=budget/tris(o);mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 lod_stats.append(stats(o));export(o,out/o.name)
 if index:o.hide_set(True);o.hide_render=True
active(shoe);bpy.ops.wm.save_as_mainfile(filepath=str(out/'ShoeCrown.blend'))
(out/'refinement-metrics.json').write_text(json.dumps({'source':a.input,'source_metrics':before,'lods':lod_stats,'source_triangle_reduction':before['triangles']-lod_stats[0]['triangles'],'trim_below':a.trim_below,'rotation_z':a.rotate_z,'pivot':'bottom_center','normalized_horizontal_extent':1.0},indent=2),encoding='utf-8')
print(json.dumps(lod_stats),flush=True)
