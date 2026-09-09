var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity")throw new System.InvalidOperationException("Clean SR18 Edit Mode required");
var root=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var buckets=root.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G")&&t.name.EndsWith("_Bucket")).Select(t=>t.GetComponent<Rigidbody>()).ToArray();
if(buckets.Length!=9||buckets.Any(b=>b==null))throw new System.InvalidOperationException("Unexpected buckets");
string folder="tmp/backups/sr18-latest-elements-2026-09-07/bucket-physics-"+System.DateTime.Now.ToString("HHmmss");System.IO.Directory.CreateDirectory(folder);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,folder+"/before.unity",true))throw new System.IO.IOException("Backup failed");
foreach(var rb in buckets){UnityEditor.Undo.RecordObject(rb,"Stationary authored bucket");rb.isKinematic=true;rb.useGravity=false;rb.detectCollisions=true;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(rb);}
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return buckets.Select(rb=>new{rb.name,rb.isKinematic,rb.detectCollisions,size=rb.GetComponent<BoxCollider>().bounds.size.ToString()}).ToArray();
