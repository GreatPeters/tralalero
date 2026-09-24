"""Audit every triangle after TRELLIS texture normalization, without mutation."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path

import numpy as np
from scipy.spatial import cKDTree
import trimesh

parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True);parser.add_argument('--textured',required=True);parser.add_argument('--output',required=True)
args=parser.parse_args();source,textured,output=(Path(v).resolve() for v in (args.source,args.textured,args.output))
assert not output.exists()
a=trimesh.load(source,force='mesh',process=False);b=trimesh.load(textured,force='mesh',process=False)
oldlo,oldhi=a.bounds;newlo,newhi=b.bounds
ratios=(newhi-newlo)/(oldhi-oldlo);scale=float(ratios.mean())
assert np.max(np.abs(ratios/scale-1))<1e-4
mapped=(a.vertices-(oldlo+oldhi)/2)*scale+(newlo+newhi)/2
unique,inverse=np.unique(b.vertices,axis=0,return_inverse=True)
distances,indices=cKDTree(unique).query(mapped)
source_keys=np.sort(indices[a.faces],axis=1)
target_keys=np.sort(inverse[b.faces],axis=1)
source_counts=Counter(map(tuple,source_keys));target_counts=Counter(map(tuple,target_keys))
missing=source_counts-target_counts;extra=target_counts-source_counts
remaining=missing.copy();areas=[]
for face,key in zip(a.faces,map(tuple,source_keys)):
    if remaining.get(key,0):
        p=mapped[face];areas.append(float(np.linalg.norm(np.cross(p[1]-p[0],p[2]-p[0]))*.5));remaining[key]-=1
diagonal=float(np.linalg.norm(newhi-newlo));max_distance=float(distances.max())
max_area=max(areas,default=0.)
report={'source':str(source),'textured':str(textured),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
        'textured_sha256':hashlib.sha256(textured.read_bytes()).hexdigest(),
        'source_triangles':len(a.faces),'textured_triangles':len(b.faces),'scale':scale,
        'all_source_vertices_checked':len(mapped),'max_normalized_vertex_distance':max_distance,
        'missing_triangle_count':sum(missing.values()),'extra_triangle_count':sum(extra.values()),
        'missing_triangle_areas_normalized':areas,'max_missing_area':max_area,'missing_area_total':sum(areas),
        'diagonal':diagonal,'method':'Nearest exact target position IDs for every source vertex, then triangle multiset comparison after uniform normalization; no geometry or file edits'}
report['ok']=max_distance<diagonal*1e-6 and not extra and max_area<diagonal**2*1e-10
output.write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report),flush=True)
assert report['ok'],'Texture changed more than numerically collapsed faces; inspect before assembly'
