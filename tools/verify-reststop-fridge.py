"""Fresh-import S08 transparency, five shelf planes and 200 physical holes."""
import argparse
import json
from pathlib import Path
import sys

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser()
parser.add_argument('--folder',required=True)
parser.add_argument('--layout',required=True)
parser.add_argument('--glb-only',action='store_true')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
folder=Path(args.folder).resolve();layout=json.loads(Path(args.layout).read_text(encoding='utf8'))
target=folder/'visibility-validation.json';assert not target.exists()
transform=layout.get('coordinate_transform')
def mapped(point):
    p=Vector(point)
    if transform:
        p=(p-Vector(transform['source_center']))*transform['scale']+Vector(transform['target_center'])-Vector(transform['floor_offset'])
    return p
scale=transform['scale'] if transform else 1.
lo,hi=map(Vector,layout['source_bounds']);dim=hi-lo;height=dim.z*scale
ix0,iy0,iz0=layout['interior_bounds'][0];ix1,iy1,iz1=layout['interior_bounds'][1]
sx0,sx1,sy0,sy1=layout['shelf_bounds_xy'];columns,rows=layout['hole_grid']
dx,dy=(sx1-sx0)/columns,(sy1-sy0)/rows
levels=layout['shelf_heights'];assert len(levels)==5 and columns*rows==40
formats=('glb',) if args.glb_only else ('glb','fbx');reports={}
for extension in formats:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if extension=='glb':bpy.ops.import_scene.gltf(filepath=str(folder/'model.glb'))
    else:bpy.ops.import_scene.fbx(filepath=str(folder/'model.fbx'))
    vertices,faces,alphas=[],[],[]
    for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        offset=len(vertices);vertices.extend(obj.matrix_world@v.co for v in obj.data.vertices)
        for p in obj.data.polygons:
            mat=obj.data.materials[p.material_index]
            shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            assert not shader.inputs['Alpha'].is_linked,'Inspect unexpected alpha mapping explicitly'
            faces.append(tuple(offset+i for i in p.vertices));alphas.append(float(shader.inputs['Alpha'].default_value))
    tree=BVHTree.FromPolygons(vertices,faces)
    front=[]
    limits=[iz0]+levels+[iz1]
    ox0,ox1=layout['front_opening']['opening_x']
    for fx in (.25,.5,.75):
        for a,b in zip(limits,limits[1:]):
            origin=mapped((ox0+(ox1-ox0)*fx,lo.y-dim.length,(a+b)/2))
            transmission=1.;clear=0;opaque=None
            for _ in range(80):
                point,normal,index,distance=tree.ray_cast(origin,Vector((0,1,0)))
                if point is None:break
                alpha=alphas[index]
                if alpha>=.95:
                    opaque=(point.y-mapped(lo).y)/(dim.y*scale);break
                transmission*=1-alpha;clear+=1
                origin=point+Vector((0,height*.00003,0))
            front.append({'x_fraction':fx,'z_source':(a+b)/2,'clear_surfaces':clear,
                          'transmission':transmission,'opaque_y_fraction':opaque,
                          'ok':clear>0 and transmission>.85 and opaque is not None and opaque>.6})
    shelves=[]
    for number,z in enumerate(levels,1):
        support=[];holes=[]
        for col in (2,4,6):
            center=mapped((sx0+(col+.1)*dx,sy0+2.1*dy,z))
            point,normal,index,distance=tree.ray_cast(center+Vector((0,0,height*.008)),Vector((0,0,-1)),height*.016)
            support.append(point is not None and abs(point.z-center.z)<height*.0002
                           and abs(normal.z)>.95 and alphas[index]>.99)
        for row in range(rows):
            for col in range(columns):
                center=mapped((sx0+(col+.5)*dx,sy0+(row+.5)*dy,z))
                point,normal,index,distance=tree.ray_cast(center+Vector((0,0,height*.008)),Vector((0,0,-1)),height*.016)
                holes.append(point is None)
        shelves.append({'number':number,'solid_support_rays':support,'open_holes':sum(holes),
                        'expected_holes':columns*rows,'ok':all(support) and all(holes)})
    reports[extension]={'front':front,'shelves':shelves,'ok':all(r['ok'] for r in front+shelves)}
report={'ok':all(r['ok'] for r in reports.values()),'required_formats':formats,
        'complete_export_check':not args.glb_only,'formats':reports,
        'limits':'Explicit geometric and alpha probes; actual rendered review remains required.'}
target.write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps({'ok':report['ok'],'formats':{key:{'front_passed':sum(r['ok'] for r in value['front']),
    'front_total':len(value['front']),'shelves_passed':sum(r['ok'] for r in value['shelves']),
    'open_holes':[r['open_holes'] for r in value['shelves']]} for key,value in reports.items()}}),flush=True)
assert report['ok'],'Fridge transmission/shelf/hole probe failed; inspect retained report'
