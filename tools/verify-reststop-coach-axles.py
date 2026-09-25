"""Verify three low tire silhouettes on each side after fresh import."""
import argparse
import json
from pathlib import Path
import sys

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser();parser.add_argument('--folder',type=Path,required=True)
parser.add_argument('--formats',nargs='+',choices=('glb','fbx'),default=('glb','fbx'))
parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
folder=args.folder.resolve();output=args.output.resolve();assert not output.exists()
report={'asset':'V06','height_fraction':.04,'minimum_run_width_fraction':.025,
        'expected_centers':[.23,.69744,.81744],'center_tolerance':.035,'formats':{},
        'method':'Fresh-import side rays restricted to each near-side half; count broad tire silhouettes near ground',
        'limitation':'Verifies axle count and placement, not wheel rotation or suspension physics'}
for suffix in args.formats:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source=folder/('model.'+suffix)
    if suffix=='fbx':bpy.ops.import_scene.fbx(filepath=str(source))
    else:bpy.ops.import_scene.gltf(filepath=str(source))
    vertices=[];triangles=[]
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        offset=len(vertices);vertices.extend(obj.matrix_world@v.co for v in obj.data.vertices)
        obj.data.calc_loop_triangles();triangles.extend(tuple(offset+i for i in t.vertices) for t in obj.data.loop_triangles)
    lo=Vector(tuple(min(v[i] for v in vertices) for i in range(3)))
    hi=Vector(tuple(max(v[i] for v in vertices) for i in range(3)));span=hi-lo
    assert span.y>span.x*2,'Inspect a changed orientation'
    tree=BVHTree.FromPolygons(vertices,triangles,all_triangles=True)
    result={'source':str(source),'sides':{}}
    for side in ('left','right'):
        direction=Vector((1,0,0)) if side=='left' else Vector((-1,0,0))
        x=lo.x-span.x if side=='left' else hi.x+span.x
        runs=[];start=None
        for index in range(2049):
            along=.08+.87*index/2048
            point,_,_,_=tree.ray_cast(Vector((x,lo.y+span.y*along,lo.z+span.z*.04)),direction,span.x*1.45)
            hit=point is not None
            if hit and start is None:start=along
            if not hit and start is not None:runs.append([start,along]);start=None
        if start is not None:runs.append([start,.95])
        broad=[run for run in runs if run[1]-run[0]>=.025]
        centers=[sum(run)/2 for run in broad]
        ok=len(centers)==3 and all(abs(a-b)<.035 for a,b in zip(centers,report['expected_centers']))
        result['sides'][side]={'all_runs':runs,'wheel_runs':broad,'centers':centers,'ok':ok}
    result['ok']=all(s['ok'] for s in result['sides'].values());report['formats'][suffix]=result
report['ok']=all(r['ok'] for r in report['formats'].values())
output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report),flush=True)
assert report['ok'],'Fresh imported wheel silhouettes do not show the required three axles'
