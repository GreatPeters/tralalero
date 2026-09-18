"""Repair small open roof loops on the generated service hall, preserving source UVs."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent.parent
source=root/'outputs/skins-reststop-2026-09-12/production/reststop/reststop_hall_6beaa75417'
out=root/'outputs/reststop-repairs-2026-09-12/reststop_hall';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source/'model.blend'))
records=[]
for obj in [o for o in bpy.context.scene.objects if o.type=='MESH' and o.name=='Asset_Optimized']:
    bm=bmesh.new();bm.from_mesh(obj.data);uv=bm.loops.layers.uv.active
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
    low=min(v.co.z for v in bm.verts);high=max(v.co.z for v in bm.verts);cut=low+(high-low)*.62
    original={v:[loop[uv].uv.copy() for loop in v.link_loops] for v in bm.verts}
    roof_edges=[edge for edge in bm.edges if edge.is_boundary and min(v.co.z for v in edge.verts)>cut]
    before=len(roof_edges);result=bmesh.ops.holes_fill(bm,edges=roof_edges,sides=48) if roof_edges else {'faces':[]}
    for face in result['faces']:
        for loop in face.loops:
            options=original.get(loop.vert,[])
            if options:loop[uv].uv=options[0]
    if result['faces']:bmesh.ops.triangulate(bm,faces=result['faces'])
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    records.append({'mesh':obj.name,'roofBoundaryEdgesBefore':before,'filledFaces':len(result['faces']),'polygons':len(obj.data.polygons)})
bpy.ops.wm.save_as_mainfile(filepath=str(out/'repaired.blend'))
bpy.ops.object.select_all(action='DESELECT');bpy.data.objects['Asset_Optimized'].select_set(True);bpy.context.view_layer.objects.active=bpy.data.objects['Asset_Optimized']
bpy.ops.export_scene.fbx(filepath=str(out/'model.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,bake_space_transform=False,add_leaf_bones=False,path_mode='AUTO',use_mesh_modifiers=True)
(out/'repair-report.json').write_text(json.dumps(records,indent=2),encoding='utf-8');print(json.dumps(records),flush=True)
scene=bpy.context.scene
if scene.camera:
    if scene.render.engine=='CYCLES':scene.cycles.device='CPU';scene.cycles.samples=12
    scene.render.resolution_x=1024;scene.render.resolution_y=768;scene.render.resolution_percentage=100;scene.render.filepath=str(out/'preview.png');bpy.ops.render.render(write_still=True)
