// Never save the source scene. Only compare a retained copy with its existing disk bytes.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
string copy="tmp/image-previews/sr18-full-enemy-bonus-flow-2026-09-06/actual-map/source-state-copy.unity";
if(System.IO.File.Exists(copy))throw new System.InvalidOperationException("Refusing to overwrite retained scene copy");
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,copy,true))throw new System.InvalidOperationException("Could not capture source copy");
bool equal=System.IO.File.ReadAllBytes(copy).SequenceEqual(System.IO.File.ReadAllBytes(scene.path));
var method=typeof(UnityEditor.SceneManagement.EditorSceneManager).GetMethod("ClearSceneDirtiness",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(UnityEngine.SceneManagement.Scene)},null);
if(equal && method!=null)method.Invoke(null,new object[]{scene});
return new{copy,identicalToDisk=equal,scene.isDirty,clearMethodAvailable=method!=null};
