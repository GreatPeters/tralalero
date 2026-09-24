"""Verify repeated open slots in fresh GLB and FBX imports; never visual-approve."""
import argparse
import json
from pathlib import Path
import runpy
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--folder', required=True)
parser.add_argument('--expected-slots', type=int, required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
folder = Path(args.folder).resolve()
assert args.expected_slots > 0
receipt = folder / 'opening-validation.json'
assert not receipt.exists(), 'Keep earlier verification evidence'
script = Path(__file__).with_name('inspect-reststop-grating-depth.py')
report = {'expected_slots': args.expected_slots, 'formats': {}, 'ok': True,
          'method': 'Three transverse rows of 2049 vertical rays per fresh import; count intervals deeper than 30% of height'}
for extension in ('glb', 'fbx'):
    profile_path = folder / ('opening-depth-' + extension + '.json')
    sys.argv = [str(script), '--', '--source', str(folder / ('model.' + extension)), '--output', str(profile_path)]
    runpy.run_path(str(script), run_name='__main__')
    profile = json.loads(profile_path.read_text(encoding='utf8'))
    counts = [row['deep_count'] for row in profile['profiles']]
    report['formats'][extension] = {'deep_counts': counts, 'profile': profile_path.name}
    report['ok'] = report['ok'] and counts == [args.expected_slots] * 3
receipt.write_text(json.dumps(report, indent=2), encoding='utf8')
assert report['ok'], report
print(json.dumps(report), flush=True)
