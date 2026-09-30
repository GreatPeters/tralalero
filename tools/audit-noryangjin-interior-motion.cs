using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class AuditNoryangjinInteriorMotion
{
    public static object Bindings()=>AnimationUtility.GetCurveBindings(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ShooterSurvival/Models/MeshyRestStop20260925/N13_merchant_male/N13_merchant_male_die.anim")).Where(b=>b.propertyName=="m_LocalPosition.y").Select(b=>new{b.path,b.propertyName}).ToArray();
    public static object Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var records=new List<object>();
        foreach(string id in new[]{"N13_merchant_male","N14_merchant_female"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/"+id+".prefab");
            var instance=Object.Instantiate(prefab);instance.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                var animator=instance.GetComponentInChildren<Animator>();animator.enabled=false;
                foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    foreach(float phase in new[]{0,.25f,.5f,.75f,.99f})
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*phase);
                        bool first=true;var bounds=new Bounds();
                        foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var mesh=new Mesh();renderer.BakeMesh(mesh,false);
                            foreach(var vertex in mesh.vertices){var p=renderer.transform.TransformPoint(vertex);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                            Object.DestroyImmediate(mesh);
                        }
                        records.Add(new{id,clip=clip.name,phase,length=clip.length,minY=bounds.min.y,maxY=bounds.max.y,width=bounds.size.x,depth=bounds.size.z});
                    }
                }
            }
            finally{Object.DestroyImmediate(instance);}
        }
        var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{records});
        File.WriteAllText("outputs/noryangjin-interior-v2-2026-09-28/native-motion.json",json);
        return new{samples=records.Count,saved=true};
    }
}
