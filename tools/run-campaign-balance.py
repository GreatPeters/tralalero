"""Sequential real gameplay, earned coins and normal shop purchases via official Pipeline."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import time

ROOT=Path(__file__).resolve().parents[1]
CLI=Path.home()/'AppData/Local/Microsoft/WindowsApps/unity.exe'
def command(name,**values):
    argv=[str(CLI),'command','--project-path',str(ROOT),name,'--format','json','--timeout','60']
    for key,value in values.items():argv += ['--'+key,str(value).lower() if isinstance(value,bool) else str(value)]
    p=subprocess.run(argv,cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=80)
    r=json.loads(p.stdout.lstrip('\ufeff'));result=(r.get('data') or {}).get('result')
    if p.returncode or not r.get('success') or isinstance(result,dict) and result.get('success') is False:raise RuntimeError(json.dumps(r,ensure_ascii=False))
    return result

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('label');parser.add_argument('--runs',type=int,default=5);parser.add_argument('--seed',type=int,default=20260923);parser.add_argument('--speed',type=float,default=3);parser.add_argument('--policy',default='campaign');parser.add_argument('--resume',action='store_true');parser.add_argument('--stop-chapter',type=int,default=3)
    parser.add_argument('--save-root',default='tmp/campaign-balance-2026-09-23');parser.add_argument('--render-cap',type=int,choices=[0,60,120,240],default=0)
    args=parser.parse_args()
    folder=ROOT/'tmp/image-previews/campaign-balance-2026-09-23'/args.label
    folder.mkdir(parents=True,exist_ok=args.resume);relative=folder.relative_to(ROOT).as_posix()
    save_record=folder/'save-root.txt'
    if save_record.exists() and save_record.read_text(encoding='utf-8')!=args.save_root:
        raise ValueError('Resume with the recorded --save-root; never restore an older task snapshot')
    if not save_record.exists():save_record.write_text(args.save_root,encoding='utf-8')
    records=json.loads((folder/'cohort.json').read_text(encoding='utf-8')) if args.resume else [];chapter=1+sum(r['outcome']=='clear' for r in records)
    scenes=['Noryangjin_MapTool_Mode_SR18','HighWay','RestStop']
    try:
        command('editor_stop');command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Reset',args=json.dumps([args.save_root]))
        if args.resume:command('eval',code='return ChapterPlaytestPreferences.RestoreAt("'+relative+f'/state-{len(records):02}.tsv");')
        for run in range(len(records)+1,args.runs+1):
            scene=scenes[chapter-1]
            command('editor_stop')
            command('eval',code='UnityEditor.EditorApplication.isPaused=false;return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/'+scene+'.unity").path;')
            if args.render_cap:
                command('eval',code='var type=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");var view=UnityEngine.Resources.FindObjectsOfTypeAll(type).Cast<UnityEditor.EditorWindow>().First();view.maximized=true;view.Focus();return true;')
            command('editor_play')
            command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Buy',args=json.dumps([relative+f'/purchases-{run:02}.json']))
            (folder/f'conditions-{run:02}.json').write_text(json.dumps({'seed':args.seed+run,'speed':args.speed,'gameFps':30,'renderFrameCap':args.render_cap,'policy':args.policy,'workbookSha256':hashlib.sha256((ROOT/'Assets/ShooterSurvival/GameData/Editor/Data.xlsx').read_bytes()).hexdigest(),'driverSha256':hashlib.sha256((ROOT/'tools/ten-run-review.cs').read_bytes()).hexdigest(),'statOverrides':False,'advertisements':False,'defaultCosmetics':True}),encoding='utf-8')
            command('run_script',file='tools/ten-run-review.cs',entry='TenRunReview.Main',args=json.dumps([run,args.policy,360,relative,args.speed,'earned-progression',args.seed+run,12,30,args.render_cap]))
            print(f'START {run} CH{chapter}',flush=True)
            run_folder=folder/f'run-{run:02}';began=time.monotonic()
            while not (run_folder/'summary.json').exists():
                if (run_folder/'error.txt').exists():raise RuntimeError((run_folder/'error.txt').read_text())
                if time.monotonic()-began>500:raise TimeoutError('Run did not terminate')
                time.sleep(1)
            time.sleep(3.5)
            report=json.loads((run_folder/'summary.json').read_text(encoding='utf-8'));records.append(report)
            (folder/'cohort.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
            print(f"END {run} CH{chapter} {report['outcome']} {report['seconds']:.1f}s {report['fatalCause']} coins={report['earnedCoins']} bank={report['bank']}",flush=True)
            command('editor_stop');command('eval',code='return ChapterPlaytestPreferences.SnapshotAt("'+relative+f'/state-{run:02}.tsv");')
            if report['outcome']=='clear':
                chapter+=1
                if chapter>args.stop_chapter:break
            elif report['outcome']!='death':raise RuntimeError('Nonterminal gameplay outcome requires inspection')
            if (folder/'stop-after-run').exists():break
        (folder/'completed.json').write_text(json.dumps({'runs':len(records),'clearedChapters':chapter-1}),encoding='utf-8')
    finally:
        command('editor_stop');command('run_script',file='tools/campaign-save.cs',entry='CampaignSave.Restore',args=json.dumps([args.save_root]))
        original=ROOT/args.save_root/'scene-before.txt';scene=original.read_text(encoding='utf-8') if original.exists() else 'Assets/ShooterSurvival/Scenes/Tools/HighWay.unity'
        command('eval',code='UnityEditor.EditorApplication.isPaused=false;return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("'+scene+'").path;')
        (folder/'restored.txt').write_text('Original user state restored',encoding='utf-8')
