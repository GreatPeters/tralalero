var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var canvas = scene.GetRootGameObjects().Single(g=>g.name=="Canvas");
return new { scene=scene.name, buttons=canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b=>b.name.Contains("Ad")||b.name.Contains("Start")).Select(b=>new {
    name=b.name, active=b.gameObject.activeSelf,
    texts=b.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t=>t.text).ToArray(),
    listeners=Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Select(i=>new {method=b.onClick.GetPersistentMethodName(i),target=b.onClick.GetPersistentTarget(i)?.name}).ToArray()
}).ToArray(), fonts=canvas.GetComponentsInChildren<TMPro.TMP_Text>(true).GroupBy(t=>t.font!=null?t.font.name:"missing").Select(g=>new {font=g.Key,count=g.Count()}).ToArray() };
