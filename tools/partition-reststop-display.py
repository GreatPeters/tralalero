"""Prepare an exact face partition of recovered S02 as a fallback, no AI call."""
import argparse
import json
from pathlib import Path
import runpy
import sys

import bpy
from mathutils import Vector

parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=Path(__file__).resolve().parents[1]
source,output=Path(args.source).resolve(),Path(args.output).resolve()
assert 'S02' in str(source) and source.suffix=='.blend'
assert not output.exists() and output.is_relative_to(root/'outputs')
proof=json.loads((source.parent/'glass-separation.json').read_text(encoding='utf8'))
assert json.loads((source.parent/'visibility-validation.json').read_text(encoding='utf8'))['ok']
output.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
originals=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert len(originals)==1
original=originals[0]
mesh=original.data
lo,hi=Vector(proof['bounds_min']),Vector(proof['bounds_max'])
dim=hi-lo
groups={'body':[],'glass':[],'shelves':[]}
for polygon in mesh.polygons:
    c=polygon.center
    fx,fy,fz=((c[i]-lo[i])/dim[i] for i in range(3))
    if mesh.materials[polygon.material_index].name=='Display clear glazing':
        key='glass'
    elif .055<fx<.945 and .06<fy and c.y<proof['case_back']+dim.y*.012 and (.29<fz<.405 or .575<fz<.695):
        key='shelves'
    else:
        key='body'
    groups[key].append(polygon.index)
assert all(groups.values())
assert sorted(i for group in groups.values() for i in group)==list(range(len(mesh.polygons)))
objects={}
for name,indices in groups.items():
    polygons=[mesh.polygons[i] for i in indices]
    used=sorted({i for p in polygons for i in p.vertices})
    mapping={index:i for i,index in enumerate(used)}
    data=bpy.data.meshes.new(name+' exact source faces')
    data.from_pydata([mesh.vertices[i].co for i in used],[],[[mapping[i] for i in p.vertices] for p in polygons])
    data.update()
    for material in mesh.materials:
        data.materials.append(material)
    for before,after in zip(polygons,data.polygons):
        after.material_index=before.material_index
        after.use_smooth=before.use_smooth
    if mesh.uv_layers.active:
        uv=data.uv_layers.new(name=mesh.uv_layers.active.name)
        for before,after in zip(polygons,data.polygons):
            for old,new in zip(before.loop_indices,after.loop_indices):
                uv.data[new].uv=mesh.uv_layers.active.data[old].uv
    normals=[mesh.corner_normals[i].vector for p in polygons for i in p.loop_indices]
    data.normals_split_custom_set(normals)
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.matrix_world=original.matrix_world
    objects[name]=obj
    assert len(data.polygons)==len(indices)
    assert all(tuple(data.vertices[mapping[i]].co)==tuple(mesh.vertices[i].co) for i in used)
bpy.data.objects.remove(original,do_unlink=True)
for name,obj in objects.items():
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    bpy.ops.export_scene.gltf(filepath=str(output/(name+'.glb')),use_selection=True,export_format='GLB')
bpy.ops.object.select_all(action='DESELECT')
for obj in objects.values():
    obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'),use_selection=True,export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(output/'model.blend'))
report={'source':str(source),'fallback_only':True,'generated_texture':False,
        'face_counts':{key:len(value) for key,value in groups.items()},
        'triangles':sum(len(p.vertices)-2 for obj in objects.values() for p in obj.data.polygons),
        'every_source_face_once':True,'vertex_positions_uvs_corner_normals_preserved':True,
        'source_bounds_min':list(lo),'source_bounds_max':list(hi),'source_face_indices':groups,
        'note':'Exact geometry partition. No AI submission, resampling, or completed-asset selection.'}
(output/'partition.json').write_text(json.dumps(report,indent=2),encoding='utf8')
script=root/'tools/reststop-production-review-render.py'
sys.argv=[str(script),'--',str(output)]
runpy.run_path(str(script),run_name='__main__')
print(json.dumps({key:value for key,value in report.items() if key!='source_face_indices'}),flush=True)
