using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Native prefab materials and actual imported clips. CPU skin matrices avoid
// stale GPU poses when several frames are rendered within one Editor call.
public static class CaptureHighwayRebuildShowcase
{
    public static string Main(string name,string label="final",string assetFolder="Assets/ShooterSurvival/Prefabs/Highway/Enemies",string evidenceRoot="tmp/image-previews/highway-enemy-rebuild-2026-09-23")
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        string folder=evidenceRoot+"/showcase-"+label+"/"+name;if(Directory.Exists(folder))throw new Exception("Preserve previous capture");Directory.CreateDirectory(folder);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(assetFolder+"/"+name+".prefab");
        var root=UnityEngine.Object.Instantiate(source);root.hideFlags=HideFlags.HideAndDontSave;root.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        foreach(var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))text.gameObject.SetActive(false);
        var animator=root.GetComponentInChildren<Animator>();var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
        var vertices=mesh.vertices;var normals=mesh.normals;var weights=mesh.boneWeights;var poses=mesh.bindposes;var matrices=new Matrix4x4[skin.bones.Length];
        var baked=UnityEngine.Object.Instantiate(mesh);var proxy=new GameObject("EvaluatedBody",typeof(MeshFilter),typeof(MeshRenderer));proxy.transform.SetParent(root.transform,false);proxy.layer=31;proxy.GetComponent<MeshFilter>().sharedMesh=baked;proxy.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
        var cameraObject=new GameObject("ShowcaseCamera"){hideFlags=HideFlags.HideAndDontSave};var camera=cameraObject.AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.18f,.23f);camera.orthographic=true;camera.orthographicSize=2.15f;camera.nearClipPlane=.05f;camera.farClipPlane=20;
        var target=root.transform.position+Vector3.up*1.55f;camera.transform.position=target+new Vector3(0,.5f,8);camera.transform.LookAt(target);
        var rt=new RenderTexture(640,800,24);var texture=new Texture2D(640,800,TextureFormat.RGB24,false);var old=RenderTexture.active;int frame=0;
        var clips=animator.runtimeAnimatorController.animationClips;
        try
        {
            camera.targetTexture=rt;camera.aspect=.8f;
            foreach(string action in assetFolder.Contains("RestStop")?new[]{"idle","walk","attack_once","hit","die"}:new[]{"idle","attack_once","attack_once","hit","die"})
            {
                var clip=clips.Single(c=>c.name==action||c.name.EndsWith("|"+action));int count=Mathf.CeilToInt(clip.length*30);
                for(int f=0;f<=count;f++)
                {
                    clip.SampleAnimation(animator.gameObject,Mathf.Min(clip.length-.0001f,f/30f));
                    for(int b=0;b<matrices.Length;b++)matrices[b]=root.transform.worldToLocalMatrix*skin.bones[b].localToWorldMatrix*poses[b];
                    var points=new Vector3[vertices.Length];var ns=new Vector3[vertices.Length];
                    for(int v=0;v<vertices.Length;v++)
                    {
                        var w=weights[v];var p=vertices[v];var n=normals[v];
                        points[v]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                        ns[v]=(matrices[w.boneIndex0].MultiplyVector(n)*w.weight0+matrices[w.boneIndex1].MultiplyVector(n)*w.weight1+matrices[w.boneIndex2].MultiplyVector(n)*w.weight2+matrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;
                    }
                    baked.vertices=points;baked.normals=ns;baked.RecalculateBounds();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,640,800),0,0);texture.Apply();File.WriteAllBytes(folder+"/frame-"+frame++.ToString("D4")+".jpg",texture.EncodeToJPG(90));
                    if(f==count/2&&!File.Exists(folder+"/"+action+".png"))File.WriteAllBytes(folder+"/"+action+".png",texture.EncodeToPNG());
                }
            }
            File.WriteAllText(folder+"/capture.txt",$"Native prefab clip/material preview, fixed camera, CPU evaluated skin matrices. {frame} frames at30fps. This is an animation inspection stage, not a gameplay run.");
        }
        finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(baked);}
        return folder;
    }
}
