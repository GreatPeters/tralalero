"""Keep S02's textured low body; fit clean glass and trays to its high source."""
import argparse
import importlib.util
import json
import math
from pathlib import Path
import runpy
import shutil
import sys

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser()
parser.add_argument('--source-folder',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=Path(__file__).resolve().parents[1]
source,output=Path(args.source_folder).resolve(),Path(args.output).resolve()
assert 'S02' in str(source) and not output.exists()
assert json.loads((source/'visual-review.json').read_text(encoding='utf8'))['verdict']=='mesh'
output.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(source/'model.blend'))
old=bpy.data.objects['Asset_Optimized'];high=bpy.data.objects['Detailed_Source']

def kind(material):
    shader=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    if shader.inputs['Base Color'].is_linked:return 'body'
    if shader.inputs['Alpha'].default_value<.2:return 'glass'
    return 'shelves'

body_index=next(i for i,m in enumerate(old.data.materials) if kind(m)=='body')
polygons=[p for p in old.data.polygons if p.material_index==body_index]
used=sorted({v for p in polygons for v in p.vertices});mapping={v:i for i,v in enumerate(used)}
data=bpy.data.meshes.new('Preserved TRELLIS low body')
data.from_pydata([old.data.vertices[i].co for i in used],[],[[mapping[i] for i in p.vertices] for p in polygons]);data.update()
data.materials.append(old.data.materials[body_index])
uv=data.uv_layers.new(name='UVMap')
normals=[]
for before,after in zip(polygons,data.polygons):
    after.use_smooth=before.use_smooth
    for a,b in zip(before.loop_indices,after.loop_indices):
        uv.data[b].uv=old.data.uv_layers.active.data[a].uv
        normals.append(old.data.corner_normals[a].vector.copy())
data.normals_split_custom_set(normals)
body=bpy.data.objects.new('Asset_Optimized',data);bpy.context.collection.objects.link(body)
body.matrix_world=old.matrix_world
assert all(tuple(data.vertices[mapping[i]].co)==tuple(old.data.vertices[i].co) for i in used)
body_triangles=sum(len(p.vertices)-2 for p in data.polygons)
assert body_triangles==11541
bpy.data.objects.remove(old,do_unlink=True)
vertices=[high.matrix_world@v.co for v in high.data.vertices]
lo=Vector(tuple(min(p[i] for p in vertices) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in vertices) for i in range(3)))
dim=hi-lo;mid=(hi+lo)/2
glass_faces=[tuple(p.vertices) for p in high.data.polygons if kind(high.data.materials[p.material_index])=='glass']
tree=BVHTree.FromPolygons(vertices,glass_faces)
curve=[]
for i in range(33):
    z=lo.z+dim.z*(.245+(.977-.245)*i/32)
    origin=Vector((mid.x,lo.y-dim.length,z));hits=[]
    for _ in range(8):
        hit=tree.ray_cast(origin,Vector((0,1,0)))[0]
        if hit is None:break
        if hits and hit.y-hits[0]>.08*dim.y:break
        hits.append(hit.y);origin=hit+Vector((0,dim.y*.0001,0))
    if hits:
        y=sum(hits)/len(hits)
        # The decoded lower pane has gaps. A ray through such a gap can hit a
        # rear pane fragment; reject that crossing before monotonic fitting.
        fraction=(z-lo.z)/dim.z
        if (y-lo.y)/dim.y>.05+.65*fraction**3:
            continue
        if curve:y=max(y,curve[-1][0]+dim.y*1e-6)
        curve.append((y,z))
assert len(curve)>=22
(output/'curve-fit.json').write_text(json.dumps({'profile_before_roof_yz':curve,'bounds':[list(lo),list(hi)]},indent=2),encoding='utf8')
assert max(b[0]-a[0] for a,b in zip(curve,curve[1:]) if b[1]<lo.z+dim.z*.9)<dim.y*.14
roof_y=lo.y+dim.y*.57
roof_hit=tree.ray_cast(Vector((mid.x,roof_y,hi.z+dim.length)),Vector((0,0,-1)))[0]
roof_z=max(curve[-1][1],roof_hit.z if roof_hit else lo.z+dim.z*.985)
if roof_y>curve[-1][0]:curve.append((roof_y,roof_z))
side_x=[]
for side in (-1,1):
    origin=Vector((mid.x+side*dim.x*2,lo.y+dim.y*.3,lo.z+dim.z*.5))
    hit=tree.ray_cast(origin,Vector((-side,0,0)))[0]
    assert hit is not None
    side_x.append(hit.x)

glass=bpy.data.materials.new('Clean clear glazing')
glass.use_nodes=True;glass.use_backface_culling=False;glass.surface_render_method='DITHERED'
shader=glass.node_tree.nodes['Principled BSDF']
shader.inputs['Base Color'].default_value=(.78,.84,.88,1)
shader.inputs['Roughness'].default_value=.13;shader.inputs['Metallic'].default_value=0
shader.inputs['IOR'].default_value=1.5;shader.inputs['Alpha'].default_value=.13
glass.diffuse_color=(.78,.84,.88,.13)
steel=next(m for m in high.data.materials if kind(m)=='shelves')
made=[]

def make(name,points,faces,material,smooth=False):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(points,[],faces);mesh.update()
    assert not mesh.validate()
    mesh.materials.append(material)
    uv=mesh.uv_layers.new(name='UVMap')
    for polygon in mesh.polygons:
        polygon.use_smooth=smooth
        for index in polygon.loop_indices:
            point=mesh.vertices[mesh.loops[index].vertex_index].co
            uv.data[index].uv=((point.x-lo.x)/dim.x,(point.z-lo.z)/dim.z)
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);made.append(obj)
    return obj

front=[]
for y,z in curve:
    front.extend(((lo.x+dim.x*.055,y,z),(hi.x-dim.x*.055,y,z)))
make('Fitted curved front glazing',front,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(curve)-1)],glass,True)
back_y=lo.y+dim.y*.577
for index,x in enumerate(side_x):
    points=[(x,y+dim.y*.004,z) for y,z in curve]
    points.extend(((x,back_y,curve[-1][1]),(x,back_y,curve[0][1])))
    face=list(range(len(points)))
    if index:face.reverse()
    make('Fitted side glazing '+str(index),points,[face],glass)

def ring(x0,x1,y0,y1,radius,z):
    result=[]
    for cx,cy,start in ((x0+radius,y0+radius,180),(x1-radius,y0+radius,270),
                        (x1-radius,y1-radius,0),(x0+radius,y1-radius,90)):
        for i in range(5):
            a=math.radians(start+i*22.5)
            result.append((cx+radius*math.cos(a),cy+radius*math.sin(a),z))
    return result

shelf_proof=[]
for number,band in enumerate(((.29,.405),(.575,.695))):
    candidates=[];bins={}
    for polygon in high.data.polygons:
        if kind(high.data.materials[polygon.material_index])!='shelves':continue
        fraction=(polygon.center.z-lo.z)/dim.z
        if not band[0]<fraction<band[1] or abs(polygon.normal.z)<.95:continue
        candidates.append(polygon)
        key=round(fraction/.002);area=polygon.area
        item=bins.setdefault(key,[0.,0.]);item[0]+=area;item[1]+=area*polygon.center.z
    maximum=max(v[0] for v in bins.values())
    key=max(k for k,v in bins.items() if v[0]>.6*maximum)
    plane=bins[key][1]/bins[key][0]
    points=[vertices[i] for p in candidates if abs(p.center.z-plane)<dim.z*.004 for i in p.vertices]
    x0,x1=min(p.x for p in points),max(p.x for p in points)
    y0,y1=min(p.y for p in points),max(p.y for p in points)
    assert x1-x0>dim.x*.6 and y1-y0>dim.y*.3
    inset=dim.x*.006;radius=min(dim.x*.02,(y1-y0)*.06)
    bottom,top=plane-dim.z*.012,plane+dim.z*.008
    rings=[ring(x0,x1,y0,y1,radius,bottom),ring(x0,x1,y0,y1,radius,top),
           ring(x0+inset,x1-inset,y0+inset,y1-inset,radius-inset,top),
           ring(x0+inset,x1-inset,y0+inset,y1-inset,radius-inset,plane)]
    n=len(rings[0]);points=sum(rings,[]);faces=[list(reversed(range(n))),list(range(3*n,4*n))]
    for layer in range(3):
        for i in range(n):
            j=(i+1)%n;faces.append((layer*n+i,layer*n+j,(layer+1)*n+j,(layer+1)*n+i))
    obj=make('Fitted stainless tray '+str(number+1),points,faces,steel)
    shelf_proof.append({'floor_z':plane,'outer_bounds_xy':[x0,x1,y0,y1],'bottom_z':bottom,'rim_z':top,'corner_radius':radius})
for obj in made:
    assert all(all(lo[i]-dim.length*1e-5<=v.co[i]<=hi[i]+dim.length*1e-5 for i in range(3)) for v in obj.data.vertices)
bpy.ops.object.select_all(action='DESELECT')
for obj in [body]+made:obj.hide_set(False);obj.hide_render=False;obj.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join();body.name='Asset_Optimized'
triangles=sum(len(p.vertices)-2 for p in body.data.polygons)
assert triangles<=15000
high.hide_set(True);high.hide_render=True
app=next(p for p in (Path.home()/'Desktop').glob('AI */Trellis */*') if (p/'engine.py').is_file())
spec=importlib.util.spec_from_file_location('asset_refiner',app/'blender_refine.py')
refiner=importlib.util.module_from_spec(spec);spec.loader.exec_module(refiner)
stats=refiner.inspect(body);assert stats['finite'] and stats['uv'] and not stats['degenerate_faces'] and not stats['loose_vertices']
refiner.active(body)
bpy.ops.export_scene.gltf(filepath=str(output/'model.glb'),use_selection=True,export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(output/'model.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,add_leaf_bones=False)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(output/'model.blend'))
shutil.copytree(source/'textures',output/'textures')
for name in ('trellis_source.glb','original-textured-body.glb','recovered-shape-before-materials.glb','assembly-repair.json','partition.json'):
    shutil.copy2(source/name,output/name)
report={'source_folder':str(source),'method':'Preserve exact low TRELLIS body positions/UVs/PBR/normals; replace unstable glazing and shelf decimation with clean surfaces fitted to the original high glass profile and shelf planes/footprints',
        'body_triangles_unchanged':body_triangles,'curved_glass_profile_yz':curve,'side_glass_x':side_x,'pane_alpha':.13,
        'fitted_shelves':shelf_proof,'final_metrics':stats,'new_insert_vertices_inside_source_bounds':True,
        'original_canonical_attempts_preserved':2,'extra_ai_requests':0}
(output/'repair.json').write_text(json.dumps(report,indent=2),encoding='utf8')
script=root/'tools/reststop-production-review-render.py';sys.argv=[str(script),'--',str(output)]
runpy.run_path(str(script),run_name='__main__')
shutil.copy2(output/'quality/opposite.png',output/'preview.png')
print(json.dumps(report),flush=True)
