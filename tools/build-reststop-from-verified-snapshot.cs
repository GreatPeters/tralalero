var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var source=System.IO.File.ReadAllText(scene.path).Replace("\r\n","\n");var copy=System.IO.File.ReadAllText("tmp/backups/skins-progression-2026-09-12/dirty-before-reststop.unity").Replace("\r\n","\n");
if(source!=copy)throw new System.InvalidOperationException("Review changed scene before saving");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RestStopSceneBuilder")).First(t=>t!=null);
try{return type.GetMethod("Build").Invoke(null,null);}catch(System.Reflection.TargetInvocationException error){throw error.InnerException??error;}
