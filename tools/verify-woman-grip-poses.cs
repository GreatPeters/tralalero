using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

// Evaluated skinned geometry avoids stale GPU poses when sampling several
// animation times in one Editor frame. The authored prefab is never changed.
public static class VerifyWomanGripPoses
{
    public static string Main()
    {
        const string folder="tmp/image-previews/review-fixes-20-runs-2026-09-22/woman-motion-final";
        if(Directory.Exists(folder))throw new Exception("Preserve prior evidence");Directory.CreateDirectory(folder);
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_Woman.prefab"));
        root.hideFlags=HideFlags.HideAndDontSave;root.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);
        try
        {
            foreach(var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))text.gameObject.SetActive(false);
            var animator=root.GetComponentInChildren<Animator>();var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var grip=root.GetComponent<WomanKnifeGrip>();
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            foreach(var clip in animator.runtimeAnimatorController.animationClips.Where(c=>c.name.Contains("Carry")||c.name.Contains("ControlledSlash")).Distinct())
            foreach(float time in new[]{0f,.15f,.3f,.45f,.6f,.75f,.9f,1f})
            {
                clip.SampleAnimation(animator.gameObject,Mathf.Min(clip.length-.001f,clip.length*time));grip.ApplyGrip();
                var actual=grip.knife.TransformPoint(grip.handlePoint);var expected=hand.TransformPoint(grip.gripInHand);
                if(Vector3.Distance(actual,expected)>.005f)throw new Exception("Handle slipped from hand");
                var mesh=new Mesh();skin.BakeMesh(mesh,true);
                var body=new GameObject("Evaluated pose");body.transform.SetParent(skin.transform,false);body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
                var go=new GameObject("Pose camera");var camera=go.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.21f,.26f);camera.fieldOfView=32;camera.nearClipPlane=.01f;camera.farClipPlane=30;
                var target=skin.bounds.center;camera.transform.position=target+animator.transform.rotation*new Vector3(.55f,.18f,1).normalized*Mathf.Max(4,skin.bounds.size.y*2.2f);camera.transform.LookAt(target);
                var rt=new RenderTexture(720,840,24);var texture=new Texture2D(720,840,TextureFormat.RGB24,false);var old=RenderTexture.active;
                try{camera.targetTexture=rt;camera.aspect=720f/840;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,720,840),0,0);texture.Apply();File.WriteAllBytes(folder+"/"+clip.name+"-"+time.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png",texture.EncodeToPNG());}
                finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(body);UnityEngine.Object.DestroyImmediate(mesh);skin.enabled=true;}
            }
            File.WriteAllText(folder+"/verification.txt","Two authored clips, eight sample times each; native evaluated mesh and actual grip. Handle error <= 0.005m. Samples are pose inspection, not a continuous motion video.");
            return folder;
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
