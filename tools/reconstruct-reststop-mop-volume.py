"""Build a solid occupancy surface without relying on P04's inconsistent winding."""
import argparse
import json
from pathlib import Path
import sys

import numpy as np
from scipy import ndimage
import trimesh
from skimage import measure

root = Path(__file__).resolve().parents[1]
out = root / 'outputs/reststop-production-2026-09-24'
source = out / 'reviews/P04-cached-stages/stage161/model.glb'
parser = argparse.ArgumentParser()
parser.add_argument('--revision', type=int, required=True)
parser.add_argument('--pitch-fraction', type=float, default=.003)
args = parser.parse_args()
assert args.revision >= 2 and .002 <= args.pitch_fraction <= .004
folder = out / ('manual/P04-volume-r' + str(args.revision))
assert not folder.exists()
folder.mkdir()
mesh = trimesh.load(source, force='mesh', process=False)
pitch = float(mesh.extents.max()) * args.pitch_fraction
grid = mesh.voxelized(pitch, method='subdivide', max_iter=4, edge_factor=1.5)
surface = np.pad(grid.matrix, 4)
assert surface.size < 100000000
closed = ndimage.binary_closing(surface, structure=ndimage.generate_binary_structure(3, 1), iterations=2)
solid = ndimage.binary_fill_holes(closed)
field = ndimage.gaussian_filter(solid.astype(np.float32), sigma=.65)
vertices, faces, _, _ = measure.marching_cubes(field, level=.5,
    spacing=(pitch, pitch, pitch), allow_degenerate=False)
result = trimesh.Trimesh(vertices=vertices + grid.transform[:3, 3] - 4 * pitch, faces=faces, process=True)
result.fix_normals(multibody=True)
result.export(folder / 'model.glb')
(folder / 'reconstruction.json').write_text(json.dumps({'source': str(source), 'pitch': pitch,
    'grid_shape': list(surface.shape), 'surface_voxels': int(surface.sum()),
    'solid_voxels': int(solid.sum()), 'triangles': len(result.faces),
    'watertight': bool(result.is_watertight), 'method': 'Unsigned occupancy, two-cell closing, exterior flood fill, 0.65-cell field smoothing and nondegenerate marching cubes',
    'extra_ai_generations': 0}, indent=2), encoding='utf8')
assert result.is_watertight
print(json.dumps({'folder': str(folder), 'triangles': len(result.faces)}), flush=True)
