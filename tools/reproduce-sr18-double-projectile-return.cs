if(!UnityEditor.EditorApplication.isPlaying)throw new Exception("Play required");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var poolObject=new GameObject("Playtest isolated projectile pool");
var root=new GameObject("Playtest isolated projectile");
try {
 var pool=poolObject.AddComponent<IndianOceanAssets.ShooterSurvival.BulletPooler>();
 var child=new GameObject("GFX");child.transform.SetParent(root.transform);
 var bullet=child.AddComponent<IndianOceanAssets.ShooterSurvival.BulletScript>();
 typeof(IndianOceanAssets.ShooterSurvival.BulletScript).GetField("bulletPooler",flags).SetValue(bullet,pool);
 var reverse=(System.Collections.Generic.Dictionary<GameObject,IndianOceanAssets.ShooterSurvival.BulletKind>)typeof(IndianOceanAssets.ShooterSurvival.BulletPooler).GetField("reverse",flags).GetValue(pool);
 reverse.Add(root,IndianOceanAssets.ShooterSurvival.BulletKind.Water);
 bullet.SetDirection(Vector3.forward);
 var giveBack=typeof(IndianOceanAssets.ShooterSurvival.BulletScript).GetMethod("ReturnToPool",flags);
 giveBack.Invoke(bullet,null);
 bool firstKeptScript=root.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.BulletScript>(true)!=null;
 giveBack.Invoke(bullet,null);
 bool secondLostScript=root.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.BulletScript>(true)==null;
 return new{firstKeptScript,secondLostScript,queuedRootNowEmpty=root.transform.childCount==0,diagnosis="First return clears projectileRoot in OnDisable; second return treats GFX as an unregistered projectile and removes it from the pooled root."};
}finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(poolObject);}
