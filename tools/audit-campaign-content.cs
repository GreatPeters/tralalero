using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

// Read-only native scene inspection. Native frames remain the visual authority.
public static class AuditCampaignContent
{
    public static string Main(string label)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(current.isDirty)throw new InvalidOperationException("Preserve unsaved authoring before scene inspection");
        string original=current.path;
        var records=new List<object>();
        try
        {
            foreach(var scene in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"})
            {
                EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+scene+".unity");
                var enemies=Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                var actors=enemies.Select(e=>
                {
                    var animations=e.GetComponentsInChildren<Animator>(true);var capsule=e.GetComponent<CapsuleCollider>();
                    var marker=e.GetComponent<HighwayEnemyAnimation>();var events=e.GetComponent<EnemyEventController>();
                    var body=e.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault();
                    return new{e.name,enabled=e.gameObject.activeSelf,animators=animations.Length,
                        body=body?.name,bounds=body?.bounds.size.ToString(),radius=capsule!=null?capsule.radius:0,
                        height=capsule!=null?capsule.height:0,recoil=marker!=null,eventMode=events?.EventMode.ToString(),
                        clips=animations.SelectMany(a=>a.runtimeAnimatorController!=null?a.runtimeAnimatorController.animationClips:Array.Empty<AnimationClip>()).Select(c=>new{c.name,c.length}).ToArray()};
                }).ToArray();
                var props=Object.FindObjectsByType<ObstacleStats>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(o=>
                {
                    var collider=o.GetComponent<Collider>();var hazard=o.GetComponent<HighwayHazard>();
                    return new{o.name,type=o.obstaclePattern.ToString(),enabled=o.gameObject.activeSelf,value=o.value,
                        position=o.transform.position.ToString(),direction=o.transform.forward.ToString(),
                        contact=collider!=null?collider.bounds.size.ToString():"child-only",
                        warning=hazard!=null?hazard.warningSeconds:0,cycle=hazard!=null?hazard.cycleSeconds:0};
                }).ToArray();
                records.Add(new{scene,actors,props});
            }
        }
        finally{EditorSceneManager.OpenScene(original);}
        string path="tmp/campaign-balance-2026-09-23/content-"+label+".json";
        if(File.Exists(path))throw new IOException("Preserve prior audit");
        string json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{records});
        File.WriteAllText(path,json);return path;
    }
}
