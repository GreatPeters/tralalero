using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class CaptureWomanGrip
{
    public static string Main(string suffix,int variant=0)
    {
        string folder="tmp/mobile-feedback-2026-09-20/woman-"+suffix;Directory.CreateDirectory(folder);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_Woman.prefab");
        var go=UnityEngine.Object.Instantiate(source);go.hideFlags=HideFlags.HideAndDontSave;go.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);
        try
        {
            foreach(var canvas in go.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
            var animator=go.GetComponentInChildren<Animator>(true);animator.transform.localRotation=Quaternion.identity;
            foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct().Where(c=>c.name.Contains("Carry")||c.name.Contains("Slash")))
            foreach(float fraction in new[]{0f,.35f,.7f})
            {
                clip.SampleAnimation(animator.gameObject,clip.length*fraction);
                if(variant>0)
                {
                    var weapon=go.GetComponentInChildren<MeshFilter>().transform;
                    if(variant==2)weapon.localRotation=Quaternion.Euler(90,0,90);
                    if(variant>=3)weapon.localRotation=Quaternion.Euler(0,90,0);
                    var palm=variant==4?new Vector3(-.025f,.11f,.075f):new Vector3(0,.075f,-.015f);
                    weapon.localPosition=palm-weapon.localRotation*Vector3.Scale(new Vector3(-.0065f,-.0015f,0),weapon.localScale);
                }
                var cameraObject=new GameObject("Grip proof camera"){hideFlags=HideFlags.HideAndDontSave};var camera=cameraObject.AddComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.20f,.25f,.31f);camera.nearClipPlane=.05f;camera.farClipPlane=20;camera.fieldOfView=38;
                var target=go.transform.position+Vector3.up*1.5f;camera.transform.position=target+new Vector3(1,.4f,2).normalized*7;camera.transform.LookAt(target);
                var rt=new RenderTexture(720,720,24);var texture=new Texture2D(720,720,TextureFormat.RGB24,false);var old=RenderTexture.active;
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,720,720),0,0);texture.Apply();
                File.WriteAllBytes(folder+"/"+clip.name+"-"+(int)(fraction*100)+".png",texture.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
        return folder;
    }
}
