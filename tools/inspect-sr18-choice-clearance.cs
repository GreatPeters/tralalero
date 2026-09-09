var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
Physics.SyncTransforms();
float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
var player=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_Player").GetComponent<IndianOceanAssets.ShooterSurvival.PlayerScript>();
return new{scene=scene.path,scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying,roads=map.Find("Roads").childCount,props=map.Find("Props").childCount,
playerRadius=player.GetComponent<CapsuleCollider>().radius*player.transform.lossyScale.x,
altars=map.Find("Bonuses").Cast<Transform>().Select(t=>new{t.name,position=V(t.position),scale=V(t.localScale),rotation=V(t.eulerAngles),boxes=t.GetComponentsInChildren<BoxCollider>(true).Select(c=>new{size=V(c.size),worldSize=V(c.bounds.size),center=V(c.bounds.center)}).ToArray()}).ToArray(),
enemies=map.Find("Enemies").Cast<Transform>().Select(t=>new{t.name,position=V(t.position),mode=t.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventController>().EventMode.ToString()}).ToArray()};
