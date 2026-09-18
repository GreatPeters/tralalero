var label=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMPro.TMP_Text>(true)).SingleOrDefault(t=>t.text=="광고보기");
if(label==null)return "not present";
var result=new System.Collections.Generic.List<object>();
for(var t=label.transform;t!=null;t=t.parent)result.Add(new{name=t.name,components=t.GetComponents<UnityEngine.Component>().Where(c=>c!=null).Select(c=>c.GetType().Name).ToArray()});
return result;
