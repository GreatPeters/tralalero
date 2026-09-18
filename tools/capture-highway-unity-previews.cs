using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HighwayUnityPreviews
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        const string output="tmp/image-previews/highway-enemies-2026-09-10";
        Directory.CreateDirectory(output);
        foreach(string name in new[]{"ConeMechanic","TrafficPatrol","TollgateChief"})
        {
            var stage=new GameObject("Highway preview stage"){hideFlags=HideFlags.HideAndDontSave};stage.transform.position=Vector3.one*10000;
            RenderTexture texture=null;
            try
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Models/Highway/"+name+"/Highway_"+name+"_Visual.prefab");
                var instance=UnityEngine.Object.Instantiate(prefab,stage.transform,false);
                foreach(var t in instance.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var renderers=instance.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                var cameraObject=new GameObject("Preview camera");cameraObject.transform.SetParent(stage.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
                camera.transform.position=bounds.center+new Vector3(-2.1f,1.1f,4.2f);camera.transform.LookAt(bounds.center);camera.fieldOfView=34;
                camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.085f,.10f);camera.allowHDR=false;
                texture=new RenderTexture(768,768,24,RenderTextureFormat.ARGB32);texture.Create();camera.targetTexture=texture;
                var lightObject=new GameObject("Preview fill");lightObject.transform.SetParent(stage.transform,false);lightObject.transform.position=bounds.center+new Vector3(-2,3,3);
                var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.range=15;light.intensity=5;light.cullingMask=1<<31;
                camera.Render();var previous=RenderTexture.active;RenderTexture.active=texture;
                var image=new Texture2D(768,768,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,768,768),0,0);image.Apply();
                File.WriteAllBytes(output+"/unity-"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);
            }
            finally{if(texture!=null){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}UnityEngine.Object.DestroyImmediate(stage);}
        }
        return new{output,previews=3};
    }
}
