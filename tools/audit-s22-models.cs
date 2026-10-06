using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Read-only binding audit. Use S22QualityAudit.Subject for live model captures.
// Rejected instant animation/bake fixtures are retained under audit outputs.
public static class S22ModelReview
{
    static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
    public static object RigAudit()
    {
        var rows=new List<object>();
        foreach(var label in new[]{"N13_merchant_male","N14_merchant_female","N20_ajumma_boss"}){
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/"+label+".prefab");
            var animator=source.GetComponentInChildren<Animator>(true);var skin=source.GetComponentInChildren<SkinnedMeshRenderer>(true);
            foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct()){
                var bindings=AnimationUtility.GetCurveBindings(clip);var paths=bindings.Select(b=>b.path).Distinct().ToArray();
                rows.Add(new{label,clip=clip.name,path=AssetDatabase.GetAssetPath(clip),animator=animator.name,avatar=AssetDatabase.GetAssetPath(animator.avatar),bones=skin.bones.Length,skinRoot=skin.rootBone!=null?skin.rootBone.name:"null",bindingCount=bindings.Length,bindingPaths=paths.Length,missing=paths.Where(p=>p!=""&&animator.transform.Find(p)==null).ToArray(),boneNames=skin.bones.Select(b=>b!=null?b.name:"null").ToArray(),paths=paths.Take(12).ToArray()});
            }
        }
        File.WriteAllText("outputs/s22-quality-audit-2026-10-01/rig-audit.json",Json(rows));return rows;
    }
}
