using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class PrototypeWomanGripReview
{
    public static string Main(string tag="closed")
    {
        string folder="tmp/image-previews/review-fixes-20-runs-2026-09-22/woman-grip-"+tag;if(Directory.Exists(folder))throw new Exception("Preserve evidence");Directory.CreateDirectory(folder);
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_Woman.prefab"));root.hideFlags=HideFlags.HideAndDontSave;root.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);
        var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var material=new Material(Shader.Find("FlatKit/Stylized Surface"));
        material.SetTexture("_BaseMap",skin.sharedMaterial.GetTexture("_BaseMap"));material.SetColor("_BaseColor",Color.white);material.SetColor("_ColorDim",new Color(.66f,.70f,.77f));material.SetFloat("_TextureImpact",1);material.EnableKeyword("_CELPRIMARYMODE_SINGLE");material.EnableKeyword("_TEXTUREBLENDINGMODE_MULTIPLY");skin.sharedMaterial=material;
        try
        {
            var a=root.GetComponentInChildren<Animator>();a.transform.localRotation=Quaternion.identity;
            var knife=root.GetComponentsInChildren<MeshFilter>().First(f=>f.name.Contains("Chef_s_Steel")).transform;
            var hand=a.GetBoneTransform(HumanBodyBones.RightHand);
            var idle=a.runtimeAnimatorController.animationClips.First(c=>c.name.Contains("Carry"));idle.SampleAnimation(a.gameObject,idle.length*.35f);
            var fingers=hand.GetComponentsInChildren<Transform>().Where(t=>t!=hand).Select(t=>new{t,q=t.localRotation,p=t.localPosition}).ToArray();
            foreach(var clip in a.runtimeAnimatorController.animationClips.Where(c=>c.name.Contains("Carry")||c.name.Contains("ControlledSlash")).Distinct())
            for(int variant=0;variant<3;variant++)
            {
                clip.SampleAnimation(a.gameObject,clip.length*.35f);
                foreach(var finger in fingers){finger.t.localRotation=finger.q;finger.t.localPosition=finger.p;if(finger.t.name.Contains("RightHandIndex")&&!finger.t.name.EndsWith("4"))finger.t.localRotation*=Quaternion.Euler(variant==0?20:variant==1?35:50,0,0);}
                knife.localRotation=Quaternion.Euler(0,180,0);
                knife.localPosition=new Vector3(-.027f,.127f,.055f)-knife.localRotation*Vector3.Scale(new Vector3(-.0065f,-.0015f,0),knife.localScale);
                var baked=new Mesh();skin.BakeMesh(baked,true);
                var body=new GameObject("Evaluated pose");body.transform.SetParent(skin.transform,false);body.AddComponent<MeshFilter>().sharedMesh=baked;body.AddComponent<MeshRenderer>().sharedMaterial=material;skin.enabled=false;
                var go=new GameObject("Grip camera");var cam=go.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.21f,.26f);cam.fieldOfView=32;cam.nearClipPlane=.01f;cam.farClipPlane=30;
                var target=hand.position+Vector3.up*.2f;cam.transform.position=target+new Vector3(.6f,.25f,1).normalized*4;cam.transform.LookAt(target);
                var rt=new RenderTexture(900,900,24);var texture=new Texture2D(900,900,TextureFormat.RGB24,false);var old=RenderTexture.active;
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,900,900),0,0);texture.Apply();File.WriteAllBytes(folder+"/"+clip.name+"-"+variant+".png",texture.EncodeToPNG());}
                finally{cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(body);UnityEngine.Object.DestroyImmediate(baked);skin.enabled=true;}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(material);}return folder;
    }
}
