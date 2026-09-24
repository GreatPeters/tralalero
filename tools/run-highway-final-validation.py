"""Run isolated edge/center contact checks plus ordinary gameplay through Unity CLI."""
from pathlib import Path
import json,subprocess,time
root=Path(__file__).resolve().parents[1];cli=Path.home()/'AppData/Local/Microsoft/WindowsApps/unity.exe'
base=root/'tmp/image-previews/highway-enemy-rebuild-2026-09-23';work=root/'tmp/highway-enemy-rebuild-2026-09-23'
def command(name,**kwargs):
    args=[str(cli),'command','--project-path',str(root),name,'--format','json','--timeout','60']
    for k,v in kwargs.items():args+=['--'+k,str(v)]
    p=subprocess.run(args,cwd=root,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=80);r=json.loads(p.stdout.lstrip('\ufeff'));data=(r.get('data') or {}).get('result')
    if p.returncode or not r.get('success') or isinstance(data,dict) and data.get('success') is False:raise RuntimeError(json.dumps(r,ensure_ascii=False))
    return data
try:
    for label,side,isolated in [('final-left',-15,True),('final-right',15,True),('final-center',0,True),('gameplay-final',-15,False)]:
        command('editor_stop');command('eval',code='UnityEditor.EditorApplication.isPaused=false;UnityEngine.Time.captureDeltaTime=0;return true;');command('editor_play')
        command('run_script',file='tools/record-highway-rebuild.cs',entry='RecordHighwayRebuild.Main',args=json.dumps([label,side,11,isolated]))
        started=time.monotonic();(work/'final-validation-status.json').write_text(json.dumps({'label':label,'state':'playing'}))
        while not (base/label/'complete.txt').exists():
            if time.monotonic()-started>180:raise RuntimeError('No terminal recording: '+label)
            time.sleep(1)
        print(label+': '+(base/label/'complete.txt').read_text(),flush=True)
    (work/'final-validation-status.json').write_text(json.dumps({'state':'completed','count':4}))
finally:
    command('editor_stop');command('eval',code='UnityEditor.EditorApplication.isPaused=false;UnityEngine.Time.captureDeltaTime=0;ChapterPlaytestPreferences.RestoreAt("tmp/highway-enemy-rebuild-2026-09-23/before-prefs.tsv");var p=System.IO.File.ReadAllText("tmp/highway-enemy-rebuild-2026-09-23/tutorial-before.txt").Split(\'\\t\');if(bool.Parse(p[0]))UnityEngine.PlayerPrefs.SetInt("TutorialDone",int.Parse(p[1]));else UnityEngine.PlayerPrefs.DeleteKey("TutorialDone");UnityEngine.PlayerPrefs.Save();return true;')
