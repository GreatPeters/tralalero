"""Sample bidirectional surface distance after an explicit topology change."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser();parser.add_argument('--source',type=Path,required=True)
parser.add_argument('--candidate',type=Path,required=True);parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);output=args.output.resolve();assert not output.exists()
def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(path.resolve()))
    vertices=[];faces=[]
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        start=len(vertices);vertices.extend(obj.matrix_world@v.co for v in obj.data.vertices)
        obj.data.calc_loop_triangles();faces.extend(tuple(start+i for i in f.vertices) for f in obj.data.loop_triangles)
    tree=BVHTree.FromPolygons(vertices,faces,all_triangles=True)
    step=max(1,len(faces)//12000)
    points=[sum((vertices[i] for i in faces[index]),Vector())/3 for index in range(0,len(faces),step)]
    return vertices,points,tree
source_vertices,source_points,source_tree=load(args.source)
candidate_vertices,candidate_points,candidate_tree=load(args.candidate)
height=max(v.z for v in source_vertices)-min(v.z for v in source_vertices)
report={'source':str(args.source.resolve()),'candidate':str(args.candidate.resolve()),
    'source_sha256':hashlib.sha256(args.source.read_bytes()).hexdigest(),
    'candidate_sha256':hashlib.sha256(args.candidate.read_bytes()).hexdigest(),
    'method':'Deterministic triangle-centroid samples in both directions and every candidate vertex, nearest triangle distance normalized by source height',
    'limitation':'Sampled surface fidelity; not an exhaustive Hausdorff bound or substitute for visual review'}
for name,points,tree in [('source_to_candidate',source_points,candidate_tree),
                         ('candidate_to_source',candidate_points+candidate_vertices,source_tree)]:
    values=np.array([tree.find_nearest(point)[3]/height for point in points])
    assert np.isfinite(values).all()
    report[name]={'samples':len(values),'median':float(np.median(values)),
                  'p95':float(np.quantile(values,.95)),'p99':float(np.quantile(values,.99)),'maximum':float(values.max())}
output.write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report),flush=True)
