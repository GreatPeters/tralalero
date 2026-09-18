var player = UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var bullets = UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.BulletScript>(UnityEngine.FindObjectsSortMode.None);
var enemies = UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.EnemyScript_space>(UnityEngine.FindObjectsSortMode.None)
    .OrderBy(e=>UnityEngine.Vector3.Distance(e.transform.position,player.transform.position)).Take(4).ToArray();
return new { player = player.transform.position.ToString(),
    bullets = bullets.Take(5).Select(b=>new {p=b.transform.position.ToString(), bounds=b.GetComponent<UnityEngine.Collider>()?.bounds.ToString()}).ToArray(),
    enemies = enemies.Select(e=>new {name=e.name,p=e.transform.position.ToString(),collider=e.GetComponent<UnityEngine.Collider>().bounds.ToString(),renderers=e.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Select(r=>r.bounds.ToString()).ToArray()}).ToArray() };
