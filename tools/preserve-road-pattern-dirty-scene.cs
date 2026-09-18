var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
const string path = "Assets/ShooterSurvival/Scenes/Tools/RoadPatterns_DirtyBackup_20260913.unity";
if (System.IO.File.Exists(path)) throw new System.InvalidOperationException("Preserve existing dirty-scene backup");
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path, true);
if (!saved || !System.IO.File.Exists(path)) throw new System.IO.IOException("Dirty-scene backup failed");
return new {backup=path,originalScene=scene.path,originalStillDirty=scene.isDirty,bytes=new System.IO.FileInfo(path).Length};
