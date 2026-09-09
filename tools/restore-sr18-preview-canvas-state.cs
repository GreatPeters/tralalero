// Restore only the single Canvas active flag proven different from the clean pre-capture scene.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
string folder="tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/actual-map/";
string before=System.IO.File.ReadAllText(scene.path),copy=System.IO.File.ReadAllText(folder+"source-state-copy.unity");
int start=copy.IndexOf("--- !u!1 &702235921"),end=copy.IndexOf("--- !u!114 &702235922",start);
if(start<0||end<0)throw new System.InvalidOperationException("Missing exact Canvas serialization block");
string corrected=copy.Substring(0,start)+copy.Substring(start,end-start).Replace("m_IsActive: 0","m_IsActive: 1")+copy.Substring(end);
if(corrected!=before)throw new System.InvalidOperationException("Other source changes exist; do not restore or clear dirty state");
var target=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Transform>(true)).Select(t=>t.gameObject).Single(g=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(g).targetObjectId==702235921UL);
target.SetActive(true);
string afterPath=folder+"source-state-restored.unity";
if(System.IO.File.Exists(afterPath))throw new System.InvalidOperationException("Refusing overwrite");
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,afterPath,true))throw new System.InvalidOperationException("Copy capture failed");
bool equal=System.IO.File.ReadAllBytes(afterPath).SequenceEqual(System.IO.File.ReadAllBytes(scene.path));
if(equal)typeof(UnityEditor.SceneManagement.EditorSceneManager).GetMethod("ClearSceneDirtiness",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(UnityEngine.SceneManagement.Scene)},null).Invoke(null,new object[]{scene});
return new{identicalToDisk=equal,scene.isDirty,sourceSaved=false};
