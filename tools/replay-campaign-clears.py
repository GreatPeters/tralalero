"""Replay measured clear builds/seeds through the real delayed victory UI."""
import json
from pathlib import Path
import runpy
import time

command=runpy.run_path(str(Path(__file__).with_name('run-campaign-balance.py')))['command']
cohort=Path('tmp/image-previews/campaign-balance-2026-09-23/r8-final-rewards')
root=cohort.parent/'clear-replays';root.mkdir(exist_ok=False)
reports=[]
try:
    for chapter,checkpoint,scene in [(1,29,'Noryangjin_MapTool_Mode_SR18'),(2,48,'HighWay'),(3,55,'RestStop')]:
        folder=root/f'chapter-{chapter}';folder.mkdir();relative=folder.as_posix()
        command('editor_stop');command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Reset')
        command('eval',code='return ChapterPlaytestPreferences.RestoreAt("'+(cohort/f'state-{checkpoint:02}.tsv').as_posix()+'");')
        command('eval',code='UnityEditor.EditorApplication.isPaused=false;return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/'+scene+'.unity").path;')
        command('editor_play')
        command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Buy',args=json.dumps([relative+'/purchases.json']))
        command('run_script',file='tools/ten-run-review.cs',entry='TenRunReview.Main',args=json.dumps([1,'campaign',360,relative,3,'earned-clear-replay',20260924+checkpoint,20,30]))
        print('START clear replay CH'+str(chapter),flush=True);began=time.monotonic()
        while not (folder/'run-01/summary.json').exists():
            if (folder/'run-01/error.txt').exists():raise RuntimeError((folder/'run-01/error.txt').read_text())
            if time.monotonic()-began>500:raise TimeoutError('Clear replay did not end')
            time.sleep(1)
        time.sleep(4)
        summary=json.loads((folder/'run-01/summary.json').read_text(encoding='utf-8'))
        verification=json.loads((folder/'run-01/run-verification.json').read_text(encoding='utf-8'))
        if summary['outcome']!='clear' or not verification['completed']:raise RuntimeError('Measured clear did not repeat: CH'+str(chapter))
        results=list((folder/'run-01').glob('*-result.png'))
        if len(results)!=1:raise RuntimeError('Result frame missing')
        reports.append({'chapter':chapter,'checkpoint':checkpoint,'seed':20260924+checkpoint,'seconds':summary['seconds'],'completed':True,'result':results[0].as_posix()})
        (root/'results.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
        print('END clear replay CH'+str(chapter)+' '+str(round(summary['seconds'],1)),flush=True)
finally:
    command('editor_stop');command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Restore')
    command('eval',code='UnityEditor.EditorApplication.isPaused=false;return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity").path;')
