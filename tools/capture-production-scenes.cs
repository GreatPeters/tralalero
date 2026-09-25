using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CaptureProductionScenes
{
    const string Art="Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/";
    public static object Main(string revision="v1")
    {
        string folder="tmp/image-previews/reststop-scene-integration-2026-09-25/scenes-"+revision;Directory.CreateDirectory(folder);
        foreach(var name in new[]{"RestStop","HighWay"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
            foreach(var animator in map.GetComponentsInChildren<Animator>(true))if(animator.runtimeAnimatorController!=null&&AssetDatabase.GetAssetPath(animator.runtimeAnimatorController).Contains("RestStopProduction"))
            {var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name=="idle"||c.name.EndsWith("|idle"));clip.SampleAnimation(animator.gameObject,.5f);}
            var reference=Camera.main;
            var stage=new GameObject("Temporary capture camera"){hideFlags=HideFlags.HideAndDontSave};var camera=stage.AddComponent<Camera>();camera.CopyFrom(reference);camera.enabled=false;camera.aspect=16f/9;
            try
            {
                camera.transform.SetPositionAndRotation(reference.transform.position,reference.transform.rotation);Render(camera,folder+"/"+name+"-start.png");
                var all=map.GetComponentsInChildren<Transform>(true).Where(t=>t.gameObject.activeInHierarchy&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray();
                foreach(var key in new[]{"Assemblies/Hall","Assemblies/Store","Assemblies/Restroom","Assemblies/Fuel"})
                {
                    var target=all.FirstOrDefault(t=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==Art+key+".prefab");if(target==null)continue;
                    var bounds=BoundsOf(target);float distance=Mathf.Max(12,bounds.size.magnitude*1.5f);
                    camera.fieldOfView=42;camera.farClipPlane=260;camera.transform.position=bounds.center+target.rotation*new Vector3(.6f,.55f,1).normalized*distance;camera.transform.LookAt(bounds.center);Render(camera,folder+"/"+name+"-"+Path.GetFileName(key)+".png");
                }
                var marker=map.Find("ProductionInstallation20260925");camera.fieldOfView=45;var focus=marker.TransformPoint(new Vector3(23,0,0));camera.transform.position=focus+marker.rotation*(name=="HighWay"?new Vector3(-5,33,34):new Vector3(27,28,-35));camera.transform.LookAt(focus);Render(camera,folder+"/"+name+"-parking.png");
            }
            finally{UnityEngine.Object.DestroyImmediate(stage);}
        }
        EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/RestStop.unity");return folder;
    }
    static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Render(Camera camera,string path)
    {
        var target=new RenderTexture(1600,900,24);target.Create();var previous=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var png=new Texture2D(1600,900,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,1600,900),0,0);png.Apply();File.WriteAllBytes(path,png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);}
        finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
