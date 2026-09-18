var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var map = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Noryangjin_MapTool");
return new { scene = scene.name, path = scene.path, dirty = scene.isDirty,
    groups = map != null ? map.transform.Cast<UnityEngine.Transform>().Select(t=>new {name=t.name,children=t.childCount}).ToArray() : null,
    players = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.PlayerScript>(true)).Select(p=>p.name).ToArray(),
    cameras = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Camera>(true)).Select(c=>new {name=c.name,tag=c.tag,enabled=c.enabled,active=c.gameObject.activeInHierarchy}).ToArray(),
    mainCamera = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.name : null };
