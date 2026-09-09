var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var cols=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
var samples=new System.Collections.Generic.List<object>();
foreach(float z in new[]{-80f,-76f,-73f,-70.57f,-68f,-65f,-62f,-59f,-56f,-53f,-48f}){var hits=new System.Collections.Generic.List<object>();foreach(var c in cols){RaycastHit h;if(c.Raycast(new Ray(new Vector3(-10.8f,60,z),Vector3.down),out h,90))hits.Add(new{road=c.transform.parent.name,c.name,y=h.point.y,normal=h.normal.ToString("F3")});}samples.Add(new{z,hits});}
return new{scene.isDirty,children=map.Cast<Transform>().Select(t=>new{t.name,t.childCount}),samples};
