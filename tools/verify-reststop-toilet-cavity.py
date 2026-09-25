"""Compare T03 bowl depth with the inspected open source in fresh imports.

Run in Blender. The region is calibrated from T03's saved open/capped profiles;
the R12 inspector supplies raw rays, but its drainage-slot counts are not used.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import runpy
import statistics
import sys

ALONG_RANGE = (.15, .45)  # Lower-Y bowl, excluding the tank/lid and rim edges.
CROSS_FRACTIONS = (.35, .5, .65)
MAX_DEPTH_DRIFT = .03  # Fraction of the full asset height.
MIN_COVERAGE = .99


def dimensions(profile):
    spans = [hi - lo for lo, hi in zip(profile['bounds_min'], profile['bounds_max'])]
    assert len(spans) == 3 and all(math.isfinite(v) and v > 0 for v in spans)
    return [v / spans[2] for v in spans]


def bowl_samples(row):
    return [depth for along, depth in row['samples'] if ALONG_RANGE[0] <= along <= ALONG_RANGE[1]]


parser = argparse.ArgumentParser()
parser.add_argument('--folder', required=True)
parser.add_argument('--reference-profile', required=True)
parser.add_argument('--output', required=True)
parser.add_argument('--formats', nargs='+', choices=('glb', 'fbx'), default=('glb', 'fbx'))
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
folder = Path(args.folder).resolve()
reference_path = Path(args.reference_profile).resolve()
receipt = Path(args.output).resolve()
assert any(part.startswith('T03') for part in folder.parts), 'T03-specific calibration'
assert not receipt.exists(), 'Keep previous verification evidence'
reference = json.loads(reference_path.read_text(encoding='utf8'))
assert reference['long_axis'] == 'Y'
assert [p['cross_fraction'] for p in reference['profiles']] == list(CROSS_FRACTIONS)
reference_dimensions = dimensions(reference)
for row in reference['profiles']:
    values = bowl_samples(row)
    assert values and all(v is not None and math.isfinite(v) for v in values)
    assert statistics.median(values) > .65, 'Reference must be the inspected open bowl'

receipt.parent.mkdir(parents=True, exist_ok=True)
report = {'asset': 'T03', 'reference_profile': str(reference_path),
          'reference_sha256': hashlib.sha256(reference_path.read_bytes()).hexdigest(),
          'along_range': ALONG_RANGE, 'cross_fractions': CROSS_FRACTIONS,
          'max_median_depth_drift': MAX_DEPTH_DRIFT, 'minimum_ray_coverage': MIN_COVERAGE,
          'method': 'Three vertical-ray rows in the inspected bowl region; depth normalized by asset height',
          'limitation': 'Sampled cavity-depth preservation, not a proof of watertightness or fluid flow',
          'formats': {}, 'ok': True}
script = Path(__file__).with_name('inspect-reststop-grating-depth.py')
for extension in args.formats:
    profile_path = receipt.parent / ('opening-depth-' + extension + '.json')
    assert not profile_path.exists(), 'Keep previous ray profiles'
    sys.argv = [str(script), '--', '--source', str(folder / ('model.' + extension)),
                '--output', str(profile_path)]
    runpy.run_path(str(script), run_name='__main__')
    candidate = json.loads(profile_path.read_text(encoding='utf8'))
    axis_ok = candidate['long_axis'] == 'Y'
    candidate_dimensions = dimensions(candidate)
    proportions_ok = all(abs(a - b) <= .03 for a, b in zip(candidate_dimensions, reference_dimensions))
    assert [p['cross_fraction'] for p in candidate['profiles']] == list(CROSS_FRACTIONS)
    rows = []
    for expected, actual in zip(reference['profiles'], candidate['profiles']):
        baseline, samples = bowl_samples(expected), bowl_samples(actual)
        assert len(samples) == len(baseline)
        finite = [v for v in samples if v is not None and math.isfinite(v)]
        coverage = len(finite) / len(samples)
        median = statistics.median(finite) if finite else None
        reference_median = statistics.median(baseline)
        drift = abs(median - reference_median) if median is not None else None
        ok = axis_ok and proportions_ok and coverage >= MIN_COVERAGE and drift is not None and drift <= MAX_DEPTH_DRIFT
        rows.append({'cross_fraction': actual['cross_fraction'], 'samples': len(samples),
                     'coverage': coverage, 'reference_median_depth': reference_median,
                     'candidate_median_depth': median, 'median_depth_drift': drift, 'ok': ok})
    passed = all(row['ok'] for row in rows)
    report['formats'][extension] = {'source': candidate['source'], 'profile': profile_path.name,
                                     'canonical_axis_ok': axis_ok, 'proportions_ok': proportions_ok,
                                     'rows': rows, 'ok': passed}
    report['ok'] = report['ok'] and passed
receipt.write_text(json.dumps(report, indent=2), encoding='utf8')
print(json.dumps({'ok': report['ok'], 'receipt': str(receipt), 'formats': report['formats']}), flush=True)
if not report['ok']:
    raise SystemExit(1)
