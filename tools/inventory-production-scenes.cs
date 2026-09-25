using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class InventoryProductionScenes
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) throw new Exception("Clean Edit Mode required");
        var result = new System.Collections.Generic.List<object>();
        foreach(var name in new[]{"RestStop", "HighWay"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var props=all.Where(t=>t.name=="Props").ToArray();
            var entries=all.Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)||t.parent==null||props.Contains(t.parent)||t.GetComponent<EnemyScript_space>()!=null).Select(t=>new {
                path=PathOf(t), name=t.name, active=t.gameObject.activeInHierarchy,
                position=V(t.position), rotation=V(t.eulerAngles), scale=V(t.lossyScale),
                prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject),
                components=t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name).ToArray(),
                bounds=BoundsOf(t.gameObject),
                animators=t.GetComponentsInChildren<Animator>(true).Select(a=>new{path=PathOf(a.transform), controller=AssetDatabase.GetAssetPath(a.runtimeAnimatorController)}).ToArray()
            }).ToArray();
            var data=new{scene=name, entries, enemies=all.Count(t=>t.GetComponent<EnemyScript_space>()!=null), roots=scene.GetRootGameObjects().Select(g=>g.name).ToArray()};
            var json=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
            File.WriteAllText("outputs/reststop-scene-integration-2026-09-25/"+name+"-before.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{data}));
            result.Add(new{scene=name, entries=entries.Length, enemies=data.enemies, roots=data.roots});
        }
        return result;
    }
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    static object BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return null;var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return new{center=V(b.center),size=V(b.size)};}
}
