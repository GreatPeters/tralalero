"""Exercise the real geometry audit with retained surfaces and discarded lines."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import numpy as np
import trimesh

ROOT=Path(__file__).resolve().parents[1]
class GeometryAuditTests(unittest.TestCase):
    def run_audit(self,source,target):
        with tempfile.TemporaryDirectory(prefix='reststop-geometry-',dir=ROOT/'tmp') as name:
            folder=Path(name)
            source.export(folder/'source.glb');target.export(folder/'target.glb')
            result=subprocess.run([sys.executable,str(ROOT/'tools/verify-reststop-texture-geometry.py'),
                '--source',str(folder/'source.glb'),'--textured',str(folder/'target.glb'),
                '--output',str(folder/'proof.json')],capture_output=True,text=True,encoding='utf8')
            report=json.loads((folder/'proof.json').read_text(encoding='utf8'))
            return result.returncode,report
    def test_uniform_normalization_preserves_real_surface(self):
        source=trimesh.creation.box();target=source.copy();target.vertices=target.vertices*2+np.array([.1,-.2,.4])
        code,report=self.run_audit(source,target)
        self.assertEqual(code,0);self.assertTrue(report['ok'])
    def test_discarded_zero_area_line_does_not_reject_unchanged_surface(self):
        cube=trimesh.creation.box()
        vertices=np.vstack([cube.vertices,[[.1,.1,.1],[.2,.1,.1],[.3,.1,.1]]])
        source=trimesh.Trimesh(vertices=vertices,faces=np.vstack([cube.faces,[[8,9,10]]]),process=False)
        code,report=self.run_audit(source,cube)
        self.assertEqual(code,0);self.assertTrue(report['ok'])
        self.assertGreater(report['max_normalized_vertex_distance'],.01)
        self.assertEqual(report['zero_area_or_unused_vertices_excluded_from_surface_gate'],3)
    def test_positive_area_vertex_deformation_still_fails(self):
        source=trimesh.creation.box();target=source.copy();target.vertices[0]*=.7
        code,report=self.run_audit(source,target)
        self.assertNotEqual(code,0);self.assertFalse(report['ok'])
        self.assertGreater(report['max_surface_vertex_distance'],.01)

if __name__=='__main__':unittest.main()
