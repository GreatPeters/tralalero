using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class CaptureReviewCharacterAssets
{
    public static string Main(string tag="before")
    {
        string folder="tmp/image-previews/review-fixes-20-runs-2026-09-22/characters-"+tag;
        if(Directory.Exists(folder))throw new Exception("Preserve old capture");Directory.CreateDirectory(folder);
        var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ShooterSurvival/Prefabs/Highway/Enemies"}).Select(AssetDatabase.GUIDToAssetPath).Concat(new[]{"Assets/JH/Model/Prefab/Enemy_Woman.prefab"});
        var report=new List<object>();
        foreach(string path in paths)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var root=UnityEngine.Object.Instantiate(source);root.hideFlags=HideFlags.HideAndDontSave;root.transform.position=new Vector3(10000,0,10000);
            var animator=root.GetComponentInChildren<Animator>(true);
            try
            {
                foreach(var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))text.gameObject.SetActive(false);
                var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.Contains("Idle")||c.name.Contains("Carry"))??animator.runtimeAnimatorController.animationClips[0];
                clip.SampleAnimation(animator.gameObject,clip.length*.35f);
                root.GetComponent<IndianOceanAssets.ShooterSurvival.WomanKnifeGrip>()?.ApplyGrip();
                var proportions=animator.GetComponent<IndianOceanAssets.ShooterSurvival.HighwayCharacterProportions>();
                if(proportions!=null)typeof(IndianOceanAssets.ShooterSurvival.HighwayCharacterProportions).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(proportions,null);
                var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();var bounds=skin.bounds;
                var cameraObject=new GameObject("Character review camera"){hideFlags=HideFlags.HideAndDontSave};var camera=cameraObject.AddComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.21f,.26f);camera.nearClipPlane=.05f;camera.farClipPlane=30;camera.fieldOfView=32;
                camera.transform.position=bounds.center+animator.transform.rotation*new Vector3(.55f,.18f,1).normalized*Mathf.Max(4,bounds.size.y*2.2f);camera.transform.LookAt(bounds.center);
                var rt=new RenderTexture(720,840,24);var texture=new Texture2D(720,840,TextureFormat.RGB24,false);var old=RenderTexture.active;
                try{camera.targetTexture=rt;camera.aspect=720f/840;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,720,840),0,0);texture.Apply();File.WriteAllBytes(folder+"/"+source.name+".png",texture.EncodeToPNG());}
                finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);}
                report.Add(new{path,height=bounds.size.y,scale=root.transform.localScale.ToString(),model=animator.name,modelScale=animator.transform.localScale.ToString(),skin=skin.name,materials=skin.sharedMaterials.Select(m=>new{m.name,shader=m.shader.name,path=AssetDatabase.GetAssetPath(m)}).ToArray()});
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)report});File.WriteAllText(folder+"/report.json",json);return folder;
    }
}
