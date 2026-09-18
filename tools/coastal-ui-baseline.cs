if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Clean Edit Mode required");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("Preserve dirty scene first");
string folder="tmp/backups/coastal-enamel-ui-2026-09-16";
System.IO.Directory.CreateDirectory(folder+"/scenes");
ChapterPlaytestPreferences.SnapshotAt(folder+"/playerprefs.tsv");
var extra=new System.Collections.Generic.List<string>();
for(int i=1;i<=3;i++)foreach(string key in new[]{ChapterUpgradeService.LevelKey(i),ChapterUpgradeService.OwnedKey(i)})extra.Add(key+"\t"+PlayerPrefs.HasKey(key)+"\t"+PlayerPrefs.GetInt(key));
System.IO.File.WriteAllLines(folder+"/chapter-prefs.tsv",extra);
var report=new System.Collections.Generic.List<string>();
try{
 foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
  var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
  if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,folder+"/scenes/"+name+".unity",true))throw new System.IO.IOException("Backup failed");
  var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas");
  report.Add("SCENE "+name);
  foreach(var t in canvas.GetComponentsInChildren<Transform>(true))if(t.GetComponent<Canvas>()!=null||t.GetComponent<OpeningStoryUI>()!=null||t.GetComponent<ChapterTransitionUI>()!=null)report.Add(t.name+" / "+string.Join(",",t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name)));
 }
}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
System.IO.File.WriteAllLines(folder+"/screen-inventory.txt",report);
return report;
