using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using IndianOceanAssets.ShooterSurvival.Analytics;
using Object=UnityEngine.Object;
public static class IntegrateChapters45
{
 const string Record="outputs/chapters45-2026-10-02";
 static string Hash(byte[] bytes){using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 public static object Workbook()
 {
  string receipt=File.ReadAllText(Record+"/workbook/verification.json");var candidate=File.ReadAllBytes(Record+"/workbook/Data.xlsx");var source=File.ReadAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath());
  if(!receipt.Contains(Hash(source))||!receipt.Contains(Hash(candidate)))throw new Exception("Concurrent workbook change; review before install.");
  GameDataWorkbookSchema.Validate(candidate);File.WriteAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath(),candidate);
  AssetDatabase.ImportAsset("Assets/ShooterSurvival/GameData/Editor/Data.xlsx",ImportAssetOptions.ForceSynchronousImport);GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(false);GameDataWorkbookEditor.ValidateRuntimeArchiveOrThrow();EnvironmentVariableTables.Reload();
  return new{workbook=Hash(candidate),runtimeArchiveVerified=true};
 }
 public static object Scenes()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Dirty scene must be preserved");
  var setup=EditorSceneManager.GetSceneManagerSetup();
  try
  {
   foreach(string name in new[]{"RestStop","Jamsil","ShoeTower"})
   {
    string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";
    if(name=="RestStop"&&!File.Exists(Record+"/RestStop-before.unity"))File.Copy(path,Record+"/RestStop-before.unity");
    var scene=EditorSceneManager.OpenScene(path);var progression=Object.FindFirstObjectByType<ChapterProgression>();
    if(name=="RestStop"){progression.nextScene="Jamsil";progression.nextChapterMovie=null;progression.nextChapterTitle="잠실 · 하늘의 신발";progression.nextChapterCaption="휴게소를 지나 서울로. 흰 신발이 있는 탑을 향해.";EditorUtility.SetDirty(progression);}
    else
    {
     var director=Object.FindFirstObjectByType<Chapter45Director>();director.choices=director.GetComponentsInChildren<Chapter45Choice>(true).OrderBy(x=>x.distance).ToArray();
     director.targets=director.GetComponentsInChildren<Chapter45Target>(true);foreach(var target in director.targets){target.requiredForGoal=target.blocksAllLanes;EditorUtility.SetDirty(target);}
     var context=director.gameObject.GetComponent<GameplayAnalyticsSceneContext>()??director.gameObject.AddComponent<GameplayAnalyticsSceneContext>();context.Configure(name=="Jamsil"?4:5,1,1);
     EditorUtility.SetDirty(director);
    }
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   var scenes=EditorBuildSettings.scenes.ToList();foreach(string name in new[]{"Jamsil","ShoeTower"}){string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";if(!scenes.Any(x=>x.path==path))scenes.Add(new EditorBuildSettingsScene(path,true));}
   EditorBuildSettings.scenes=scenes.ToArray();
  }
  finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  return new{chain="RestStop -> Jamsil -> ShoeTower",buildScenes=EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToArray()};
 }
}
