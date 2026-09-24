"""Verify left, center and right approaches using ordinary movement and real contact."""
import json
from pathlib import Path
import runpy
import time

command=runpy.run_path(str(Path(__file__).with_name('run-campaign-balance.py')))['command']
folder=Path('tmp/image-previews/later-chapters-30-2026-09-23')
save='tmp/later-chapters-30-2026-09-23'
try:
    for label,steering in [('contact-left',-20),('contact-center',0),('contact-right',20)]:
        command('editor_stop');command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Restore',args=json.dumps([save]))
        command('eval',code='UnityEditor.EditorApplication.isPaused=false;return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/RestStop.unity").path;')
        command('editor_play');command('run_script',file='tools/probe-reststop-passages.cs',entry='ProbeRestStopPassages.Main',args=json.dumps([label,steering]))
        began=time.monotonic()
        while not (folder/label/'result.json').exists():
            if time.monotonic()-began>60:raise TimeoutError('Contact fixture did not finish')
            time.sleep(1)
        report=json.loads((folder/label/'result.json').read_text(encoding='utf-8'))
        assert report['finalHp']==0 and report['cause']=='EnemyContact',report
        assert abs(report['finalLane'])<=1.851,report
        print(label,report,flush=True)
finally:
    command('editor_stop');command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Restore',args=json.dumps([save]))
    command('eval',code='UnityEditor.EditorApplication.isPaused=false;return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity").path;')
