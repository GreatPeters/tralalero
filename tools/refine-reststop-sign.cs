if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var root=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Props/Highway_RestStop/reststop_wayfinding");
foreach(var label in root.GetComponentsInChildren<TMPro.TextMeshPro>()){
 bool title=label.text=="바다쉼";label.fontSize=title?6.2f:2.5f;label.fontSizeMin=label.fontSizeMax=label.fontSize;label.rectTransform.sizeDelta=new UnityEngine.Vector2(3.4f,1);
 if(!title){label.color=new UnityEngine.Color(.96f,.94f,.83f);var point=label.transform.position;point.y=6.62f;label.transform.position=point;}
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return "Rest-stop sign type enlarged";
