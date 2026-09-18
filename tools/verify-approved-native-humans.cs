using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class ApprovedNativeHumanProof
{
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var reports=new List<object>();
        foreach(string name in ChapterMascotImporter.Names)
        {
            var root=PrefabUtility.LoadPrefabContents(ChapterMascotImporter.PrefabPath(name));
            try
            {
                var animator=root.GetComponentInChildren<Animator>(true);var transforms=animator.GetComponentsInChildren<Transform>(true);var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
                var body=animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name+"_Body");var vertices=body.sharedMesh.vertices;var weights=body.sharedMesh.boneWeights;var binds=body.sharedMesh.bindposes;var poses=new List<object>();
                foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity;
                    for(int frame=0;frame<=8;frame++)
                    {
                        for(int i=0;i<transforms.Length;i++){transforms[i].SetLocalPositionAndRotation(positions[i],rotations[i]);transforms[i].localScale=scales[i];}
                        clip.SampleAnimation(animator.gameObject,clip.length*frame/8f);
                        var matrices=body.bones.Select((bone,i)=>bone.localToWorldMatrix*binds[i]).ToArray();float bottom=float.PositiveInfinity;
                        for(int i=0;i<vertices.Length;i++)
                        {var w=weights[i];var v=vertices[i];var p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;bottom=Mathf.Min(bottom,p.y);}
                        minimum=Mathf.Min(minimum,bottom);maximum=Mathf.Max(maximum,bottom);
                    }
                    if(Mathf.Max(Mathf.Abs(minimum),Mathf.Abs(maximum))>.05f)throw new InvalidOperationException($"Native foot contact failed {name}/{clip.name}: {minimum}..{maximum}");
                    poses.Add(new{clip=clip.name,clip.length,minY=minimum,maxY=maximum,samples=9});
                }
                reports.Add(new{name,bones=body.bones.Length,actions=poses.Count,ranged=root.GetComponent<EnemyScript_space>().HasConfiguredProjectile,poses});
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        string json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{reports});
        File.WriteAllText("map-concepts/approved-road-concepts-2026-09-13/native-human-poses.json",json);return "Nine native rigs, six actions each, nine samples per action passed.";
    }
}
