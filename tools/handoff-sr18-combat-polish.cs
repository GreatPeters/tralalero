if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new InvalidOperationException("Do not discard pending changes");
const string path="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
if(scene.path!=path)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var rows=EncounterPlacementTables.Rows;
return new{scene=scene.path,scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying,roads=map.Find("Roads").childCount,props=map.Find("Props").childCount,enemyRows=rows.Count(r=>r.kind=="적 배치"&&r.hasCombatStats),bonusPairs=map.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.BonusWallChoicePair>(true).Length,buckets=map.GetComponentsInChildren<ObstacleStats>(true).Count(o=>o.obstaclePattern==ObstaclePattern.Bucket),canvasActive=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").activeSelf};
