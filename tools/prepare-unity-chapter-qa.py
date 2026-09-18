"""Create a real-file Unity QA copy; no shared writable Assets or Library databases."""
from hashlib import sha256
from pathlib import Path
import json
import shutil
import time
import os

root=Path(__file__).resolve().parent.parent
qa=root/'tmp/q'
if not qa.resolve().is_relative_to((root/'tmp').resolve()): raise RuntimeError('QA path escaped workspace tmp')
qa.mkdir(parents=True,exist_ok=True)
record=root/'map-concepts/chapters-polish-2026-09-12/qa-copy.json'
baseline={}
tracked=[root/'Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity',
         root/'Assets/ShooterSurvival/Scenes/Tools/HighWay.unity',root/'Assets/ShooterSurvival/GameData/Editor/Data.xlsx']
for folder in ['Assets/ShooterSurvival/Prefabs/Highway','Assets/ShooterSurvival/Resources/GameData']:
    tracked.extend(p for p in (root/folder).rglob('*') if p.is_file())
for path in tracked:
    if path.exists(): baseline[str(path.relative_to(root)).replace('\\','/')]=sha256(path.read_bytes()).hexdigest()
manifest=json.loads(record.read_text(encoding='utf-8')) if record.exists() else {'root':str(root),'qa':str(qa),'started':time.time(),'baselineHashes':baseline,'copied':[],'ready':False}
if manifest['ready']: raise RuntimeError('QA copy is already ready; do not overwrite it')
manifest['qa']=str(qa)
record.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
def long_path(path): return '\\\\?\\'+str(Path(path).absolute())
def copy_changed(source,destination):
    source_stat=os.stat(source)
    if os.path.exists(destination):
        target_stat=os.stat(destination)
        if source_stat.st_size==target_stat.st_size and source_stat.st_mtime_ns==target_stat.st_mtime_ns:return destination
    return shutil.copy2(source,destination)
def copy_folder(source,destination):
    shutil.copytree(long_path(source),long_path(destination),symlinks=False,dirs_exist_ok=True,copy_function=copy_changed)
for folder in ['Assets','ProjectSettings','Packages','GooglePackages','Library/PackageCache']:
    print('Copying '+folder,flush=True)
    copy_folder(root/folder,qa/folder)
    if folder not in manifest['copied']:manifest['copied'].append(folder)
    record.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
# Scene builders need these recorded inputs. Keep their writes inside the QA copy.
for folder in ['map-concepts/chapters-polish-2026-09-12','map-concepts/skins-reststop-2026-09-12','map-concepts/highway-chapter-2026-09-11']:
    copy_folder(root/folder,qa/folder)
for path in (root/'outputs/chapters-polish-2026-09-12').rglob('*'):
    if not path.is_file(): continue
    relative=path.relative_to(root)
    if path.name.endswith('-full-keys.fbx') or path.name in ['rig-report.json','fresh-inspection.json','fresh-pose-report.json','fresh-fbx-fullkeys-pose-report.json'] or (path.name=='Model.fbx' and 'props-v' in str(relative)) or (path.name=='BaseColor.png' and 'textures' in path.parts) or 'balance' in path.parts:
        destination=qa/relative;destination.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,destination)
copy_folder(root/'tools',qa/'tools')
# The legacy HTTP connector is unrelated to this supported Pipeline session.
package=qa/'Packages/manifest.json';data=json.loads(package.read_text(encoding='utf-8-sig'))
data['dependencies'].pop('com.youngwoocho02.unity-cli-connector',None)
package.write_text(json.dumps(data,indent=2),encoding='utf-8')
bootstrap=qa/'Assets/ShooterSurvival/Editor/ChapterQaBootstrap.cs'
bootstrap.write_text('''using System.IO;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival.Analytics;
public static class ChapterQaBootstrap
{
    public static void Initialize()
    {
        PlayerSettings.companyName = "TralaleroQA";
        PlayerSettings.productName = "Chapters_20260913";
        PlayerPrefs.SetInt(FirebaseAnalyticsRuntime.CollectionEnabledPlayerPrefsKey, 0);
        PlayerPrefs.Save();
        AssetDatabase.SaveAssets();
        File.WriteAllText("qa-ready.json", "{\\"isolatedPreferences\\":true,\\"analyticsEnabled\\":false}");
    }
}
''',encoding='utf-8')
manifest['ready']=True;manifest['finished']=time.time();record.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps({'qa':str(qa),'ready':True,'seconds':manifest['finished']-manifest['started']}),flush=True)
