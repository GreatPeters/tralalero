"""Exercise pause gates without importing or starting the production runner."""
import ast
import json
from pathlib import Path
import tempfile
import types
import unittest
from unittest.mock import Mock

SCRIPT = Path(__file__).with_name('run-reststop-trellis-production.py')
tree = ast.parse(SCRIPT.read_text(encoding='utf-8'))
functions = ast.Module(body=[n for n in tree.body if isinstance(n, ast.FunctionDef)
                            and n.name in ('pause_at_boundary', 'review_gate')], type_ignores=[])

class Paused(Exception):
    pass

class PauseTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.folder = self.root / 'shape'
        self.quality = self.folder / 'quality'
        self.quality.mkdir(parents=True)
        for name in ('hero', 'top', 'opposite', 'front', 'neutral'):
            (self.quality / (name + '.png')).write_bytes(b'already-rendered')
        self.result = self.quality / 'main-agent-review.json'
        self.pause = self.root / 'pause-request.json'
        def atomic(path, value):
            path.write_text(json.dumps(value), encoding='utf-8')
        self.env = dict(Path=Path, json=json, ROOT=self.root, OUT=self.root,
                        pause_path=self.pause, Paused=Paused, batch=Mock(),
                        engine=types.SimpleNamespace(atomic_json=atomic),
                        normalize_review=lambda value: value, time=Mock(), subprocess=Mock())
        exec(compile(functions, str(SCRIPT), 'exec'), self.env)
    def gate(self):
        return self.env['review_gate']('S10.png', self.folder, 'unused', Mock(), 'shape')
    def test_existing_pause_does_not_render_or_review(self):
        self.pause.write_text('{}')
        with self.assertRaises(Paused):
            self.gate()
        self.assertFalse(self.result.exists())
        self.env['subprocess'].run.assert_not_called()
        self.env['batch'].pause.assert_called_once()
    def test_stop_during_review_wait_preserves_unanswered_gate(self):
        self.env['time'].sleep.side_effect = lambda _: self.pause.write_text('{}')
        with self.assertRaises(Paused):
            self.gate()
        self.assertEqual(json.loads((self.root / 'review-pending.json').read_text())['state'], 'pending')
        self.assertFalse(self.result.exists())
    def test_pause_wins_when_verdict_arrives_in_same_wait(self):
        def finish(_):
            self.result.write_text('{"verdict":"pass"}')
            self.pause.write_text('{}')
        self.env['time'].sleep.side_effect = finish
        with self.assertRaises(Paused):
            self.gate()
        self.assertEqual(json.loads((self.root / 'review-pending.json').read_text())['state'], 'pending')
    def test_pause_wins_over_preexisting_verdict(self):
        self.result.write_text('{"verdict":"pass"}')
        self.pause.write_text('{}')
        with self.assertRaises(Paused):
            self.gate()
    def test_existing_review_remains_usable_without_pause(self):
        self.result.write_text('{"verdict":"pass"}')
        self.assertEqual(self.gate()['verdict'], 'pass')
        self.env['batch'].pause.assert_not_called()

if __name__ == '__main__':
    unittest.main()
