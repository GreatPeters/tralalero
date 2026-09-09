var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
Physics.SyncTransforms();
return map.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G")).Select(t=>{
var s=t.GetComponentsInChildren<ObstacleStats>().Single();var box=s.GetComponent<BoxCollider>();
var center=box.transform.TransformPoint(box.center);var p=center+t.right*2.8f;
var local=box.transform.InverseTransformPoint(p)-box.center;var ext=box.size*.5f;
var q=box.transform.TransformPoint(box.center+new Vector3(Mathf.Clamp(local.x,-ext.x,ext.x),Mathf.Clamp(local.y,-ext.y,ext.y),Mathf.Clamp(local.z,-ext.z,ext.z)));
return new{t.name,scale=t.lossyScale.ToString(),size=box.size.ToString("F6"),bounds=box.bounds.size.ToString(),actualCenter=center.ToString(),physicsDistance=Vector3.Distance(p,box.ClosestPoint(p)),analyticDistance=Vector3.Distance(p,q),box.enabled,box.isTrigger};}).ToArray();
