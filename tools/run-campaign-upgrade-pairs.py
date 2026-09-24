"""Same-seed before/after purchase probes using real earned checkpoint wallets."""
import argparse
import json
from pathlib import Path
import runpy
import time

command=runpy.run_path(str(Path(__file__).with_name('run-campaign-balance.py')))['command']
parser=argparse.ArgumentParser();parser.add_argument('cohort');args=parser.parse_args()
cohort=Path(args.cohort);root=cohort.parent/'paired-upgrade-checks';root.mkdir(exist_ok=False)
cases=[(1,12,'Noryangjin_MapTool_Mode_SR18'),(2,38,'HighWay'),(3,53,'RestStop')]
reports=[]
try:
    for chapter,checkpoint,scene in cases:
        samples=[]
        for buy in [False,True]:
            folder=root/('chapter-'+str(chapter)+('-after' if buy else '-before'))
            folder.mkdir();relative=folder.as_posix()
            command('editor_stop');command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Reset')
            command('eval',code='return ChapterPlaytestPreferences.RestoreAt("'+(cohort/f'state-{checkpoint:02}.tsv').as_posix()+'");')
            command('eval',code='UnityEditor.EditorApplication.isPaused=false;return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/'+scene+'.unity").path;')
            command('editor_play')
            if buy:command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Buy',args=json.dumps([relative+'/purchases.json']))
            command('run_script',file='tools/ten-run-review.cs',entry='TenRunReview.Main',args=json.dumps([1,'campaign',360,relative,3,'same-seed-earned-purchase',20260923+chapter,30,30]))
            print(f'START CH{chapter} buy={buy}',flush=True);began=time.monotonic()
            while not (folder/'run-01/summary.json').exists():
                if (folder/'run-01/error.txt').exists():raise RuntimeError((folder/'run-01/error.txt').read_text())
                if time.monotonic()-began>500:raise TimeoutError('Paired run did not end')
                time.sleep(1)
            summary=json.loads((folder/'run-01/summary.json').read_text(encoding='utf-8'))
            if summary['outcome'] not in ['death','clear']:raise RuntimeError('Nonterminal paired run')
            samples.append({'buy':buy,'seconds':summary['seconds'],'outcome':summary['outcome'],'start':next(e for e in summary['events'] if e['kind']=='start'),'folder':relative})
            print(f'END CH{chapter} buy={buy} {summary["seconds"]:.1f}s',flush=True)
        reports.append({'chapter':chapter,'checkpoint':checkpoint,'seed':20260923+chapter,'samples':samples,'secondsAdded':samples[1]['seconds']-samples[0]['seconds'],'note':'One same-seed shop visit per chapter; a visit can buy multiple upgrades. Not a per-click guarantee or population estimate.'})
        (root/'results.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2),encoding='utf-8')
finally:
    command('editor_stop');command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Restore')
    command('eval',code='UnityEditor.EditorApplication.isPaused=false;return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity").path;')
