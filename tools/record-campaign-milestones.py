"""Observe an active cohort and retain native clips without changing gameplay."""
import argparse
import json
from pathlib import Path
import runpy
import time

command=runpy.run_path(str(Path(__file__).with_name('run-campaign-balance.py')))['command']
root=Path('tmp/image-previews/campaign-balance-2026-09-23/segments')
parser=argparse.ArgumentParser();parser.add_argument('--corners',action='store_true');args=parser.parse_args()
labels=['휴게소-코너안내'] if args.corners else ['휴게소-최종배치','휴게소-식당방어전','휴게소-후반전투']
deadline=time.monotonic()+1800
while time.monotonic()<deadline and not all((root/label/'capture.txt').exists() for label in labels):
    try:
        result=command('eval',code='var c=UnityEngine.Object.FindFirstObjectByType<ChapterProgression>();var h=UnityEngine.Object.FindFirstObjectByType<RestStopHoldout>();return new {running=UnityEditor.EditorApplication.isPlaying&&IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning,seconds=c==null?0:c.Elapsed,holdout=h!=null&&h.Active,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name};')
        state=result.get('result',{})
        if state.get('running') and state.get('scene')=='RestStop':
            if args.corners:label=labels[0] if 44<state['seconds']<54 else None
            else:label=labels[1] if state['holdout'] else labels[2] if state['seconds']>267 else labels[0] if 25<state['seconds']<150 else None
            if label and not (root/label).exists():
                command('run_script',file='tools/record-campaign-segment.cs',entry='RecordCampaignSegment.Main',args=json.dumps([label,12]))
                print('Recording '+label,flush=True)
    except (RuntimeError,KeyError,AttributeError) as error:
        print(type(error).__name__,flush=True)
    time.sleep(8)
