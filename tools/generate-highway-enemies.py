"""Use the installed TRELLIS automation engine without changing its UI settings.

The normal engine owns queueing, cooldowns, source retention, Blender baking and
fresh-FBX validation. This run enables its existing background-removal node and
leaves visual review to the current task rather than launching another agent.
"""
import argparse
import importlib
import json
from pathlib import Path
import sys
import time

parser = argparse.ArgumentParser()
parser.add_argument('--app-root', required=True)
parser.add_argument('--input', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args()
sys.path.insert(0, args.app_root)
engine = importlib.import_module('engine')
build_prompt = engine.build_prompt

def prompt_with_background_removal(*values):
    prompt = build_prompt(*values)
    prompt['194']['inputs']['remove_background'] = True
    return prompt

engine.build_prompt = prompt_with_background_removal
opts = engine.settings({
    'input_dir': args.input, 'output_dir': args.output, 'recursive': False,
    'resolution': 1536, 'texture_size': 2048, 'target_triangles': 25000,
    'keep_models_loaded': False, 'use_tiled_decoder': True,
    'quality_retry': False, 'stage_quality': False, 'remove_floor': False,
    'continue_on_error': True, 'longest_side': 2.2,
})
batch = engine.BatchEngine()
print(json.dumps(batch.start(opts), ensure_ascii=True), flush=True)
output = Path(args.output)
while batch.active:
    snapshot = batch.snapshot()
    engine.atomic_json(output / 'task-status.json', snapshot)
    print(json.dumps({'active': snapshot['active'], 'message': snapshot['message']}, ensure_ascii=True), flush=True)
    time.sleep(20)
engine.atomic_json(output / 'task-status.json', batch.snapshot())
print(json.dumps(batch.snapshot(), ensure_ascii=True), flush=True)
