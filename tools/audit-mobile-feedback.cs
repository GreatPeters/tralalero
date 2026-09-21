using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class AuditMobileFeedback
{
    public static string Main()
    {
        const string folder="tmp/mobile-feedback-2026-09-20";Directory.CreateDirectory(folder);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var enemies=all.Select(t=>t.GetComponent<EnemyScript_space>()).Where(e=>e!=null).ToArray();
        var report=new {
            scene=scene.path,
            enemies=enemies.Where(e=>e.name.Contains("Woman")||e.name.Contains("Guard")||e.name.Contains("FatMan")).Select(e=>{
                var data=new SerializedObject(e);var animator=e.GetComponentInChildren<Animator>(true);
                return new{name=e.name,prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.gameObject),position=e.transform.position.ToString(),model=animator?.name,
                    projectile=data.FindProperty("heldProjectile")?.objectReferenceValue?.name,hide=data.FindProperty("hideHeldProjectile")?.boolValue,
                    speed=data.FindProperty("throwSpeed")?.floatValue,delay=data.FindProperty("throwReleaseDelay")?.floatValue,
                    meshes=e.GetComponentsInChildren<MeshFilter>(true).Select(m=>new{name=m.name,parent=m.transform.parent?.name,localPosition=m.transform.localPosition.ToString(),localRotation=m.transform.localEulerAngles.ToString(),bounds=m.sharedMesh?.bounds.ToString(),triangles=m.sharedMesh!=null?m.sharedMesh.triangles.Length/3:0}),
                    clips=animator?.runtimeAnimatorController?.animationClips.Select(c=>c.name).Distinct().ToArray()};
            }).ToArray(),
            databases=AssetDatabase.FindAssets("t:SpriteDatabase").Select(AssetDatabase.GUIDToAssetPath).Select(p=>new{path=p,entries=AssetDatabase.LoadAssetAtPath<SpriteDatabase>(p).Entries.Select(e=>new{e.key,path=AssetDatabase.GetAssetPath(e.sprite)})}).ToArray(),
            cameras=all.Select(t=>t.GetComponent<Camera>()).Where(c=>c!=null).Select(c=>new{c.name,c.enabled,c.farClipPlane,c.useOcclusionCulling,c.cullingMask}).ToArray(),
            renderers=all.Select(t=>t.GetComponent<Renderer>()).Where(r=>r!=null).GroupBy(r=>r.GetType().Name).Select(g=>new{type=g.Key,total=g.Count(),active=g.Count(r=>r.gameObject.activeInHierarchy&&r.enabled),staticCount=g.Count(r=>GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject,StaticEditorFlags.BatchingStatic))}).ToArray()
        };
        var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)report});
        File.WriteAllText(folder+"/audit.json",json);return $"Saved audit: {enemies.Length} enemy owners, {all.Length} transforms.";
    }
}
