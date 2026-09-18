using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class FinalizeApprovedRoadConcepts
{
    const string Record="map-concepts/approved-road-concepts-2026-09-13";
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
    public static string Inspect()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");var scene=SceneManager.GetActiveScene();var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerScript>(true)).Single();var camera=player.GetComponentInChildren<Camera>(true);
        var report=new{scene=scene.name,scene.path,scene.isDirty,sceneHash=Hash(scene.path),enemies=map.Find("Enemies").childCount,bonuses=map.Find("Bonuses").childCount,missingScripts=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))),cameraLocalPosition=camera.transform.localPosition.ToString("R"),cameraLocalRotation=camera.transform.localRotation.ToString("R"),camera.fieldOfView,playerScale=player.transform.lossyScale.ToString(),hallBounds=map.Find("Roads/FoodHall_Floor")?.GetComponent<Renderer>().bounds.size.ToString()};File.WriteAllText(Record+"/"+scene.name+"-final-scene.json",Json(report));return Json(report);
    }
    public static string HallOverview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().name!="RestStop")throw new InvalidOperationException("RestStop Edit Mode required");
        string path="tmp/image-previews/approved-road-concepts-2026-09-13/reststop-layout-review.png";if(File.Exists(path))throw new InvalidOperationException("Preserve earlier overview");
        var hall=GameObject.Find("Noryangjin_MapTool/Approved_Open_Hall_20260913").transform;var renderers=hall.Find("Covered hall shell").GetComponentsInChildren<Renderer>().Where(r=>r.bounds.min.y>6).ToArray();var states=renderers.Select(r=>r.forceRenderingOff).ToArray();
        var go=new GameObject("Temporary layout evidence camera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.aspect=16f/9;camera.fieldOfView=48;camera.nearClipPlane=.1f;camera.farClipPlane=200;camera.transform.position=hall.position+new Vector3(52,69,-67);camera.transform.LookAt(hall.position+Vector3.forward*9);
        var target=new RenderTexture(1600,900,24);target.Create();camera.targetTexture=target;var previous=RenderTexture.active;
        try{foreach(var r in renderers)r.forceRenderingOff=true;camera.Render();CosmeticPresentationBuilder.Save(target,path);}
        finally{for(int i=0;i<renderers.Length;i++)renderers[i].forceRenderingOff=states[i];RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(go);}
        return path+" (layout review only; gameplay camera was not changed)";
    }
    public static string Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");string snapshot=Record+"/before/playerprefs.tsv";ChapterPlaytestPreferences.RestoreAt(snapshot);var mismatches=new List<string>();var lines=File.ReadAllLines(snapshot);
        foreach(var line in lines)
        {
            var p=line.Split('\t');bool exists=bool.Parse(p[2]);if(PlayerPrefs.HasKey(p[0])!=exists){mismatches.Add(p[0]);continue;}if(!exists)continue;
            bool same=p[1]=="int"?PlayerPrefs.GetInt(p[0])==int.Parse(p[3],CultureInfo.InvariantCulture):p[1]=="float"?PlayerPrefs.GetFloat(p[0])==float.Parse(p[3],CultureInfo.InvariantCulture):PlayerPrefs.GetString(p[0])==Encoding.UTF8.GetString(Convert.FromBase64String(p[3]));if(!same)mismatches.Add(p[0]);
        }
        var files=new[]{"Assets/ShooterSurvival/GameData/Editor/Data.xlsx","Assets/ShooterSurvival/Resources/GameData/Data.bytes","Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity"}.Select(path=>new{path,unchanged=Hash(path)==Hash(Record+"/before/"+path),sha256=Hash(path)}).ToArray();
        var result=new{keys=lines.Length,mismatches,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),files};File.WriteAllText(Record+"/user-state-restored.json",Json(result));if(mismatches.Count>0||files.Any(f=>!f.unchanged))throw new InvalidOperationException("Preservation verification failed");return Json(result);
    }
}
