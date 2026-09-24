"""Repair an inspected planar prop's normals, preserving geometry and UVs.

Use only for reviewed rigid panels, not people or upholstered/organic surfaces.
"""
import argparse,json,runpy,shutil,sys
from pathlib import Path
import bpy

parser=argparse.ArgumentParser();parser.add_argument('--source',required=True);parser.add_argument('--output',required=True)
parser.add_argument('--flat-axis',choices=('X','Y','Z'),required=True)
parser.add_argument('--all-faces',action='store_true',help='Use only when the entire inspected prop is rigid')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(args.source).resolve();out=Path(args.output).resolve()
assert not (out/'model.blend').exists(),'Choose a new repair revision'
out.mkdir(parents=True,exist_ok=True);bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source));axis='XYZ'.index(args.flat_axis)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];before=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
affected=0
for obj in meshes:
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    original=[tuple(v.co) for v in obj.data.vertices]
    modifier=obj.modifiers.new('Rigid_surface_normals','WEIGHTED_NORMAL')
    modifier.mode='FACE_AREA_WITH_ANGLE';modifier.keep_sharp=True;modifier.weight=60
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    assert original==[tuple(v.co) for v in obj.data.vertices], 'Normal correction must not move vertices'
    replacements={}
    for polygon in obj.data.polygons:
        if not args.all_faces and abs(polygon.normal[axis])<.85:continue
        old=polygon.material_index
        if old not in replacements:
            material=obj.data.materials[old].copy();material.name='Planar_PBR_repaired'
            shader=next((n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
            if shader:
                for link in list(shader.inputs['Normal'].links):material.node_tree.links.remove(link)
            obj.data.materials.append(material);replacements[old]=len(obj.data.materials)-1
        polygon.material_index=replacements[old];affected+=1
after=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
assert before==after and after<=15000
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/'model.blend'))
bpy.ops.export_scene.gltf(filepath=str(out/'model.glb'),export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(out/'model.fbx'),object_types={'MESH'},add_leaf_bones=False,path_mode='COPY',embed_textures=True)
if (source.parent/'textures').exists():shutil.copytree(source.parent/'textures',out/'textures',dirs_exist_ok=True)
(out/'repair.json').write_text(json.dumps({'source':str(source),'method':'area-angle weighted normals plus removal of baked normal link on '+('all faces of the reviewed fully rigid prop' if args.all_faces else 'reviewed planar faces only'),
    'flat_axis':args.flat_axis,'all_faces':args.all_faces,'affected_faces':affected,'triangles_before':before,'triangles_after':after,'vertex_positions_unchanged':True,'uvs_and_other_pbr_channels':'retained',
    'normal_map_note':'Original Normal.png is retained for provenance; repaired materials intentionally have no Normal input. Do not reapply that map to repaired materials.'},indent=2),encoding='utf8')
script=Path(__file__).with_name('reststop-production-review-render.py')
sys.argv=[str(script),'--',str(out)];runpy.run_path(str(script),run_name='__main__')
shutil.copy2(out/'quality/hero.png',out/'preview.png')
