using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class RestStopPrevisGameScale
{
    static float[] V(Vector3 v) => new[]{v.x,v.y,v.z};
    static object Box(Transform root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
        if(renderers.Length==0)return null;
        var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);
        return new{size=V(b.size),center=V(b.center),rendererCount=renderers.Length};
    }
    static object Inspect(UnityEngine.SceneManagement.Scene scene)
    {
        var objects=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var player=objects.Select(t=>t.GetComponent<PlayerScript>()).FirstOrDefault(p=>p!=null);
        var camera=objects.Select(t=>t.GetComponent<Camera>()).FirstOrDefault(c=>c!=null && c.CompareTag("MainCamera"));
        if(camera==null)camera=objects.Select(t=>t.GetComponent<Camera>()).FirstOrDefault(c=>c!=null);
        var visual=player!=null?player.transform.Find("Original"):null;
        var cap=player!=null?player.GetComponent<CapsuleCollider>():null;
        var hold=objects.Select(t=>t.GetComponent<RestStopHoldout>()).FirstOrDefault(p=>p!=null);
        return new{
            scene=scene.path,
            player=player==null?null:new{position=V(player.transform.position),scale=V(player.transform.lossyScale),rotation=V(player.transform.eulerAngles),visualScale=visual!=null?V(visual.lossyScale):null,visualBounds=visual!=null?Box(visual):Box(player.transform),capsule=cap==null?null:new{cap.radius,cap.height,center=V(cap.center)},serialized=EditorJsonUtility.ToJson(player)},
            camera=camera==null?null:new{position=V(camera.transform.position),rotation=V(camera.transform.eulerAngles),offset=player!=null?V(camera.transform.position-player.transform.position):null,camera.fieldOfView,camera.orthographic,camera.orthographicSize,camera.nearClipPlane,camera.farClipPlane},
            holdout=hold==null?null:new{center=hold.center!=null?V(hold.center.position):null,doors=hold.entrances?.Where(t=>t!=null).Select(t=>V(t.position)).ToArray(),police=hold.police?.Where(e=>e!=null).Take(1).Select(e=>new{e.MoveSpeed,bounds=Box(e.transform)}).ToArray()},
            vehicles=objects.Where(t=>t.name.IndexOf("car",StringComparison.OrdinalIgnoreCase)>=0 || t.name.IndexOf("vehicle",StringComparison.OrdinalIgnoreCase)>=0).Take(10).Select(t=>new{name=t.name,scale=V(t.lossyScale),bounds=Box(t)}).ToArray()
        };
    }
    public static object Main(string output)
    {
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var before=active.path;bool dirty=active.isDirty;
        var live=Inspect(active);object rest=null;
        var preview=EditorSceneManager.OpenPreviewScene("Assets/ShooterSurvival/Scenes/Tools/RestStop.unity");
        try{rest=Inspect(preview);}finally{EditorSceneManager.ClosePreviewScene(preview);}
        var result=new{readOnly=true,editorPlaying=EditorApplication.isPlaying,activeSceneBefore=before,activeSceneAfter=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirtyBefore=dirty,dirtyAfter=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,live,restStop=rest};
        return result;
    }
}
