if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
int count=0;
foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay"})
{
 string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 try
 {
  var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
  foreach(var enemy in map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyScript_space>(true))
  {
   var label=enemy.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);if(label==null)continue;
   float scale=UnityEngine.Mathf.Abs(label.transform.lossyScale.x);label.rectTransform.SetSizeWithCurrentAnchors(UnityEngine.RectTransform.Axis.Horizontal,1.8f/scale);
   label.enableAutoSizing=true;label.fontSizeMax=.60f;label.fontSizeMin=.36f;label.fontSize=.60f;label.alignment=TMPro.TextAlignmentOptions.Center;
   UnityEditor.EditorUtility.SetDirty(label);UnityEditor.EditorUtility.SetDirty(label.rectTransform);
   if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(label)){UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(label);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(label.rectTransform);}count++;
  }
  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 }
 finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
}
return new{count,maxWorldWidth=1.8f};
