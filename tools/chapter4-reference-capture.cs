using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

// Native camera composition survey in unsaved Edit Mode. Not ordinary play evidence.
public static class Chapter4ReferenceCapture
{
    public static object Main(string sceneName="Jamsil",string label="before")
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Idle editor required.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Dirty scene.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        string folder="outputs/chapter4-reference-2026-10-02/"+label+"-"+sceneName+"-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
        Directory.CreateDirectory(folder);
        try
        {
            EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity");
            var d=Object.FindFirstObjectByType<Chapter45Director>();var p=Object.FindFirstObjectByType<PlayerScript>();var c=Camera.main;
            var offset=c.transform.position-p.transform.position;var rotation=c.transform.rotation;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            foreach(float station in sceneName=="Jamsil"?new[]{230f,390f,990f,1410f}:new[]{0f,45f,175f,320f})
            {
                d.route.Sample(station,out var center,out var forward);var yaw=Quaternion.LookRotation(forward);
                p.transform.SetPositionAndRotation(center+Vector3.up*d.footOffset,yaw);
                c.transform.SetPositionAndRotation(p.transform.position+yaw*offset,yaw*rotation);
                foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))
                    g.SetVisible((g.floor<0||g.floor==0)&&(g.alwaysVisible||g.floor<0&&g.endDistance<=g.startDistance||station>=g.startDistance-100&&station<=g.endDistance+100));
                foreach(var life in d.GetComponentsInChildren<Chapter4StreetLife>(true))
                    foreach(var w in life.walkers)w.body.gameObject.SetActive(station>=life.station-95&&station<=life.station+45);
                var rt=RenderTexture.GetTemporary(720,1280,24,RenderTextureFormat.ARGB32);var oldTarget=c.targetTexture;var oldActive=RenderTexture.active;float aspect=c.aspect;
                try
                {
                    c.targetTexture=rt;c.aspect=720f/1280;c.Render();RenderTexture.active=rt;
                    var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();
                    File.WriteAllBytes(folder+"/"+station.ToString("0000")+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
                }
                finally{c.targetTexture=oldTarget;c.aspect=aspect;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(rt);}
            }
            File.WriteAllText(folder+"/conditions.txt","Native saved-scene geometry, existing camera lens and relative pose. Directed edit-mode poses. Overlay UI hidden. No gameplay, performance or completion claim. Scene reopened without saving after capture.");
            return new{folder,images=Directory.GetFiles(folder,"*.png")};
        }
        finally
        {
            EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity");
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
