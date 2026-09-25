using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CaptureProductionAssets
{
    public static object Main(string keys="",string revision="v2")
    {
        string output="tmp/image-previews/reststop-scene-integration-2026-09-25/imported-"+revision;Directory.CreateDirectory(output);
        bool fog=RenderSettings.fog;RenderSettings.fog=false;
        try{foreach(var key in keys.Length>0?keys.Split(','):new[]{"Assemblies/Hall","Assemblies/Store","Assemblies/Restroom","Assemblies/Shelter","Assemblies/Fuel","V01","V06","H01","H08","S02","S10","T06"})Capture(key,output);}
        finally{RenderSettings.fog=fog;}
        return output;
    }
    static void Capture(string key,string output)
    {
        var stage=new GameObject("Production review"){hideFlags=HideFlags.HideAndDontSave};stage.transform.position=Vector3.one*10000;
        RenderTexture target=null;Material floorMat=null;
        try
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/"+key+".prefab");
            var model=UnityEngine.Object.Instantiate(asset,stage.transform,false);
            foreach(var lod in model.GetComponentsInChildren<LODGroup>())lod.enabled=false;
            foreach(var a in model.GetComponentsInChildren<Animator>()){a.enabled=false;var clip=a.runtimeAnimatorController.animationClips.First(c=>c.name=="idle"||c.name.EndsWith("|idle"));clip.SampleAnimation(a.gameObject,.4f);}
            foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            var rs=model.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(stage.transform,false);floor.layer=31;
            floor.transform.position=new Vector3(bounds.center.x,bounds.min.y-.12f,bounds.center.z);floor.transform.localScale=new Vector3(bounds.size.x*2,.15f,bounds.size.z*2);
            floorMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));floorMat.SetColor("_BaseColor",new Color(.72f,.74f,.72f));floor.GetComponent<Renderer>().sharedMaterial=floorMat;
            var camera=new GameObject("Capture camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,false);camera.enabled=false;
            float radius=bounds.size.magnitude;camera.transform.position=bounds.center+new Vector3(.7f,.5f,1).normalized*radius*1.7f;camera.transform.LookAt(bounds.center);
            camera.fieldOfView=38;camera.aspect=1.5f;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.2f,.23f);camera.farClipPlane=radius*5;camera.nearClipPlane=.05f;
            var light=new GameObject("Fill").AddComponent<Light>();light.transform.SetParent(stage.transform,false);light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);light.intensity=1.1f;light.cullingMask=1<<31;
            target=new RenderTexture(1200,800,24);target.Create();camera.targetTexture=target;
            camera.Render();var before=RenderTexture.active;RenderTexture.active=target;
            var png=new Texture2D(1200,800,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,1200,800),0,0);png.Apply();File.WriteAllBytes(output+"/"+key.Replace('/','-')+".png",png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);RenderTexture.active=before;
        }
        finally{if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}UnityEngine.Object.DestroyImmediate(stage);if(floorMat!=null)UnityEngine.Object.DestroyImmediate(floorMat);}
    }
}
