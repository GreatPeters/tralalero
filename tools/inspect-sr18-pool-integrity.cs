var pool=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.BulletPooler>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var registered=(System.Collections.Generic.Dictionary<GameObject,IndianOceanAssets.ShooterSurvival.BulletKind>)pool.GetType().GetField("reverse",flags).GetValue(pool);
var queues=new[]{"poolWater","poolBomb"}.Select(name=>{
 var q=(System.Collections.Generic.Queue<GameObject>)pool.GetType().GetField(name,flags).GetValue(pool);
 return new{kind=name,count=q.Count,distinct=q.Distinct().Count(),missingScripts=q.Where(g=>g!=null&&g.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.BulletScript>(true)==null).Select(g=>new{name=g.name,id=g.GetInstanceID(),children=g.transform.Cast<Transform>().Select(t=>t.name).ToArray()}).ToArray(),activeQueued=q.Count(g=>g!=null&&g.activeInHierarchy)};
}).ToArray();
return new{queues,broken=registered.Where(k=>k.Key!=null&&k.Key.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.BulletScript>(true)==null).Select(k=>new{k.Key.name,id=k.Key.GetInstanceID(),kind=k.Value.ToString(),k.Key.activeSelf,children=k.Key.transform.Cast<Transform>().Select(t=>t.name).ToArray()}).ToArray()};
