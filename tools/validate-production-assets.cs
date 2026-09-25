using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ValidateProductionAssets
{
    public static object Main()
    {
        const string folder="Assets/ShooterSurvival/Prefabs/RestStopProduction20260925";var reports=new List<object>();int materialCount=0,clipCount=0;
        foreach(var path in Directory.GetFiles(folder,"*.prefab").OrderBy(p=>p))
        {
            string id=Path.GetFileNameWithoutExtension(path);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var meshes=prefab.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh).Concat(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s=>s.sharedMesh)).ToArray();
            int triangles=meshes.Sum(m=>m.triangles.Length/3);if(triangles>15000||triangles==0)throw new Exception("Triangle budget "+id);
            var materials=prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();materialCount+=materials.Length;
            foreach(var m in materials)if(m==null||m.shader.name!="FlatKit/Stylized Surface"||!m.shader.isSupported)throw new Exception("Surface "+id);
            if(id=="S02"&&materials.Length!=3||id=="S08"&&materials.Length!=4||id=="T06"&&materials.Length!=2)throw new Exception("Material regions lost "+id);
            if(id=="S10"&&materials[0].GetFloat("_Surface")!=1)throw new Exception("PET opacity lost");
            if(id=="T06"&&materials.Single(m=>m.name.Contains("Exterior")).GetTexture("_BumpMap")!=null)throw new Exception("Exterior normal was reconnected");
            var actions=new List<object>();
            var root=UnityEngine.Object.Instantiate(prefab);root.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                var animator=root.GetComponentInChildren<Animator>();
                if(animator!=null)
                {
                    animator.enabled=false;var bones=animator.GetComponentInChildren<SkinnedMeshRenderer>().bones;
                    if(bones.Length!=18)throw new Exception("Bones "+id);
                    var clips=animator.runtimeAnimatorController.animationClips;if(clips.Length!=9)throw new Exception("Clips "+id);
                    foreach(var clip in clips)
                    {
                        var bindings=AnimationUtility.GetCurveBindings(clip);
                        if(bindings.Length==0||bindings.Any(b=>b.path.Length>0&&animator.transform.Find(b.path)==null))throw new Exception("Unbound motion "+id+"/"+clip.name);
                        clip.SampleAnimation(animator.gameObject,clip.length*.13f);var rotations=bones.Select(b=>b.localRotation).ToArray();var positions=bones.Select(b=>b.localPosition).ToArray();
                        clip.SampleAnimation(animator.gameObject,clip.length*.63f);float angle=bones.Select((b,i)=>Quaternion.Angle(rotations[i],b.localRotation)).Max();float distance=bones.Select((b,i)=>Vector3.Distance(positions[i],b.localPosition)).Max();
                        if(angle<.001f&&distance<.00001f)throw new Exception("Static motion "+id+"/"+clip.name);
                        actions.Add(new{clip.name,clip.length,bindings=bindings.Length,maxBoneDegrees=angle,maxBoneTravel=distance});clipCount++;
                    }
                }
            }finally{UnityEngine.Object.DestroyImmediate(root);}
            reports.Add(new{id,triangles,materials=materials.Length,actions});
        }
        if(reports.Count!=90||materialCount!=99||clipCount!=72)throw new Exception("Library totals");
        File.WriteAllText("outputs/reststop-scene-integration-2026-09-25/native-asset-validation.json",Json(new{models=reports.Count,materialCount,clipCount,passed=true,reports}));
        return new{models=reports.Count,materialCount,clipCount,passed=true};
    }
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
}
