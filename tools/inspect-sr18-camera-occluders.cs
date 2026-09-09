var map=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var c=Camera.main;
return new{camera=c.name,parent=c.transform.parent.name,offset=(Quaternion.Inverse(p.transform.rotation)*(c.transform.position-p.transform.position)).ToString(),renderers=map.Find("Roads").GetComponentsInChildren<Renderer>(true).Where(r=>r.bounds.min.y>5 && r.bounds.min.x<5&&r.bounds.max.x> -25).Select(r=>new{name=r.name,root=r.transform.parent.name,min=r.bounds.min.ToString(),max=r.bounds.max.ToString()}).Take(18).ToArray()};
