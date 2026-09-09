var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity")throw new System.InvalidOperationException("Clean SR18 Edit Mode required");
var root=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var buckets=root.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G")&&t.name.EndsWith("_Bucket")).ToArray();
if(buckets.Length!=9)throw new System.InvalidOperationException("Expected 9 buckets");
foreach(var t in buckets){UnityEditor.Undo.RecordObject(t.gameObject,"Activate authored bucket");t.gameObject.SetActive(true);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{activated=buckets.Length};
