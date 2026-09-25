"""Recover a solid T03 surface from unsigned occupancy, independent of winding."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import numpy as np
from scipy import ndimage
from skimage import measure
import trimesh

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/reststop-production-2026-09-24'
parser=argparse.ArgumentParser();parser.add_argument('--revision',type=int,required=True)
parser.add_argument('--pitch-fraction',type=float,default=.0025)
parser.add_argument('--closing-iterations',type=int,default=2)
parser.add_argument('--dilation-iterations',type=int,default=0)
args=parser.parse_args();assert args.revision>0 and .0015<=args.pitch_fraction<=.004
assert 1<=args.closing_iterations<=6
assert 0<=args.dilation_iterations<=2
source=OUT/'manual/T03-r3/source/model.glb';folder=OUT/('manual/T03-volume-r'+str(args.revision))
assert not folder.exists();folder.mkdir()
mesh=trimesh.load(source,force='mesh',process=False);pitch=float(mesh.extents.max())*args.pitch_fraction
largest_edge=float(mesh.edges_unique_length.max())
subdivision_limit=max(1,math.ceil(math.log2(largest_edge/(pitch/1.5)))+1)
assert subdivision_limit<=12,subdivision_limit
grid=mesh.voxelized(pitch,method='subdivide',max_iter=subdivision_limit,edge_factor=1.5)
padding=args.closing_iterations+2
surface=np.pad(grid.matrix,padding);assert surface.size<100000000
structure=ndimage.generate_binary_structure(3,1)
thickened=ndimage.binary_dilation(surface,structure=structure,iterations=args.dilation_iterations) if args.dilation_iterations else surface
closed=ndimage.binary_closing(thickened,structure=structure,iterations=args.closing_iterations)
solid=ndimage.binary_fill_holes(closed)
field=ndimage.gaussian_filter(solid.astype(np.float32),sigma=.65)
vertices,faces,_,_=measure.marching_cubes(field,level=.5,spacing=(pitch,pitch,pitch),allow_degenerate=False)
result=trimesh.Trimesh(vertices=vertices+grid.transform[:3,3]-padding*pitch,faces=faces,process=True)
result.fix_normals(multibody=True);assert result.is_watertight
result.export(folder/'model.glb')
(folder/'reconstruction.json').write_text(json.dumps({'source':str(source),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
    'method':'Unsigned surface occupancy, recorded closing radius, exterior fill and 0.65-cell smoothing; source winding is not trusted',
    'closing_iterations':args.closing_iterations,'padding_cells':padding,'euler_number':int(result.euler_number),
    'dilation_iterations':args.dilation_iterations,'thickened_surface_voxels':int(thickened.sum()),
    'source_watertight':bool(mesh.is_watertight),'source_winding_consistent':bool(mesh.is_winding_consistent),
    'pitch':pitch,'grid_shape':list(surface.shape),'surface_voxels':int(surface.sum()),'solid_voxels':int(solid.sum()),
    'largest_source_edge':largest_edge,'subdivision_limit':subdivision_limit,
    'triangles':len(result.faces),'watertight':bool(result.is_watertight),'extra_ai_requests':0,
    'canonical_attempts_remain_consumed':2,'actual_cavity_and_visual_review_required':True},indent=2),encoding='utf8')
print(json.dumps({'folder':str(folder),'triangles':len(result.faces),'watertight':bool(result.is_watertight)}),flush=True)
