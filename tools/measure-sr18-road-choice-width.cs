var map=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
Physics.SyncTransforms();
var p=map.Find("Bonuses").GetChild(1).position;var cols=map.Find("Roads").GetComponentsInChildren<MeshCollider>();
return new{roadSamples=Enumerable.Range(-10,21).Select(i=>{float offset=i*.5f;bool supported=cols.Any(c=>c.Raycast(new Ray(p+Vector3.forward*offset+Vector3.up,Vector3.down),out var hit,2));return new{offset,supported};}).ToArray(),nearbyRoadBounds=map.Find("Roads").Cast<Transform>().Where(t=>t.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.SqrDistance(p)<1)).Take(2).Select(t=>new{t.name,size=t.GetComponentsInChildren<Renderer>().First().bounds.size.ToString()}).ToArray()};
