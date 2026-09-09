var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(UnityEditor.EditorApplication.isPlaying || scene.isDirty) throw new InvalidOperationException("Clean Edit Mode required");
scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
EncounterPlacementTables.Reload();
var rows=EncounterPlacementTables.Rows.Where(r=>r.kind=="적 배치" && r.mode==IndianOceanAssets.ShooterSurvival.EnemyEventMode.AmbushMoveThenShoot).ToDictionary(r=>r.id);
var enemies=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>(true)).Where(e=>rows.ContainsKey(e.name));
string backup="tmp/backups/sr18-polish-2026-09-09/short-walk-"+DateTime.Now.ToString("HHmmss")+".unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,backup,true);
foreach(var e in enemies) {
 var before=e.transform.position;
 e.transform.position=e.TargetPoint.position+Vector3.ProjectOnPlane(e.transform.forward,Vector3.up).normalized*rows[e.name].moveDistance+e.transform.right*e.AmbushEntrySide;
 e.RefreshPlacementAfterAuthoringChange(before,false);
 UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(e.transform);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{scene=scene.name,shortWalks=2,backup};
