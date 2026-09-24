"""Sequential native Unity play attempts; CLI discovers the live Pipeline endpoint."""
from pathlib import Path
import argparse,json,subprocess,time,datetime

root=Path(__file__).resolve().parents[1]
work=root/'tmp/review-fixes-20-runs-2026-09-22'
captures=root/'tmp/image-previews/review-fixes-20-runs-2026-09-22/runs'
cli=Path.home()/'AppData/Local/Microsoft/WindowsApps/unity.exe'
parser=argparse.ArgumentParser();parser.add_argument('--from-run',type=int,default=6);parser.add_argument('--dry-run',action='store_true');args=parser.parse_args()

def command(name,**values):
    argv=[str(cli),'command','--project-path',str(root),name,'--format','json','--timeout','60']
    for key,value in values.items():argv.extend(['--'+key,str(value).lower() if isinstance(value,bool) else str(value)])
    result=subprocess.run(argv,cwd=root,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=80)
    try:report=json.loads(result.stdout.lstrip('\ufeff'))
    except Exception:raise RuntimeError(result.stdout[-1500:]+result.stderr[-1000:])
    payload=report.get('data',{});payload=(payload or {}).get('result',{})
    if result.returncode or not report.get('success') or isinstance(payload,dict) and payload.get('success') is False:
        raise RuntimeError(json.dumps(report,ensure_ascii=False))
    return payload

if args.dry_run:
    print(command('run_script',file='tools/ten-run-review.cs',entry='TenRunReview.Main',args=json.dumps([99,'health',360,'tmp/unused',3,'current']),dry_run=True));raise SystemExit()

configs=json.loads((work/'run-configs.json').read_text())
def progress(**values):
    values['utc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
    (work/'batch-status.json').write_text(json.dumps(values,ensure_ascii=False,indent=2),encoding='utf-8')

try:
    for number,scene,profile,policy in configs:
        if number<args.from_run:continue
        folder=captures/f'run-{number:02}'
        if (folder/'summary.json').exists():continue
        if folder.exists():raise RuntimeError(f'Incomplete existing attempt requires review: {folder}')
        command('editor_stop')
        code='if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.Exception("Unsaved scene; preserve it first");UnityEditor.EditorApplication.isPaused=false;return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/'+scene+'.unity").path;'
        command('eval',code=code)
        command('run_script',file='tools/prepare-review-entry-profile.cs',entry='PrepareReviewEntryProfile.Main',args=json.dumps([profile,3]))
        command('editor_play')
        command('run_script',file='tools/ten-run-review.cs',entry='TenRunReview.Main',args=json.dumps([number,policy,360,'tmp/image-previews/review-fixes-20-runs-2026-09-22/runs',3,profile]))
        progress(run=number,scene=scene,profile=profile,state='playing');print(f'START {number:02} {scene} {profile} {policy}',flush=True)
        began=time.monotonic();last_pause_probe=0;paused_at=None
        while not (folder/'summary.json').exists():
            if (folder/'error.txt').exists():raise RuntimeError((folder/'error.txt').read_text(encoding='utf-8'))
            now=time.monotonic()
            if now-last_pause_probe>10:
                last_pause_probe=now
                paused=command('eval',code='return UnityEditor.EditorApplication.isPaused;').get('result',False)
                if paused and paused_at is None:
                    paused_at=now;progress(run=number,scene=scene,profile=profile,state='editor-paused');print(f'PAUSED {number:02}',flush=True)
                elif not paused and paused_at is not None:
                    began+=now-paused_at;paused_at=None;progress(run=number,scene=scene,profile=profile,state='playing')
            if paused_at is None and now-began>460:raise RuntimeError(f'No terminal outcome for attempt {number}')
            time.sleep(1)
        time.sleep(2)
        report=json.loads((folder/'summary.json').read_text(encoding='utf-8'))
        progress(run=number,scene=scene,profile=profile,state='completed',outcome=report['outcome'],seconds=report['seconds'])
        print(f"END {number:02} {report['outcome']} {report['seconds']:.1f}s {report['fatalCause']}",flush=True)
    progress(state='all-completed',count=len(list(captures.glob('run-*/summary.json'))))
except Exception as error:
    (work/'batch-error.txt').write_text(str(error),encoding='utf-8');progress(state='error',error=str(error));raise
finally:
    try:
        command('editor_stop')
        command('eval',code='UnityEditor.EditorApplication.isPaused=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");var result=ChapterPlaytestPreferences.RestoreAt("tmp/review-fixes-20-runs-2026-09-22/before-prefs.tsv");foreach(var line in System.IO.File.ReadAllLines("tmp/review-fixes-20-runs-2026-09-22/extra-prefs.tsv")){var p=line.Split(\'\\t\');if(bool.Parse(p[1]))UnityEngine.PlayerPrefs.SetInt(p[0],int.Parse(p[2]));else UnityEngine.PlayerPrefs.DeleteKey(p[0]);}UnityEngine.PlayerPrefs.Save();return result;')
        (work/'batch-restored.txt').write_text('Restored purchased-level preferences and tutorial key; stopped Play Mode and reopened SR18.',encoding='utf-8')
    except Exception as error:
        (work/'restore-pending.txt').write_text(str(error),encoding='utf-8');print('RESTORE NEEDS REVIEW',flush=True)
