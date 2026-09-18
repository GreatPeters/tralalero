using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;

public static class Sr18BucketHelperObservation
{
    public static object Main()
    {
        const string folder="tmp/image-previews/sr18-presentation-progression-2026-09-10";
        bool bucketDone=File.Exists(folder+"/bucket-position.json"),helperDone=File.Exists(folder+"/helper-size.json");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Entities/Space/BomBarDino.prefab");
        float originalScale=prefab.transform.Find("bomb_0910060745_texture").localScale.x;
        var serialize=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null).GetMethod("SerializeObject",new[]{typeof(object)});
        double next=0;EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            if(!EditorApplication.isPlaying||(bucketDone&&helperDone)){EditorApplication.update-=tick;return;}
            if(EditorApplication.isPaused||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.1;
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();if(player==null||!TimeManager.isGameRunning)return;
            if(!bucketDone)
            {
                var bucket=UnityEngine.Object.FindObjectsByType<ObstacleStats>(FindObjectsSortMode.None).FirstOrDefault(o=>o.obstaclePattern==ObstaclePattern.Bucket&&o.bucket!=null&&o.bucket.IsChildOf(player.transform));
                if(bucket!=null)
                {
                    var result=new{forward=Vector3.Dot(bucket.bucket.position-player.transform.position,player.transform.forward),configuredForward=bucket.bucketHeadOffset.z,shootBlocked=!player.canShoot};
                    File.WriteAllText(folder+"/bucket-position.json",(string)serialize.Invoke(null,new object[]{result}));ScreenCapture.CaptureScreenshot(folder+"/bucket-front-final.png");bucketDone=true;
                }
            }
            if(!helperDone)
            {
                var helper=UnityEngine.Object.FindObjectsByType<ExtraHelpBuffScript>(FindObjectsSortMode.None).FirstOrDefault(h=>h.helpType==HelpType.Boombardino);
                if(helper!=null)
                {
                    var model=helper.transform.Find("bomb_0910060745_texture");var result=new{modelScaleRatio=model.localScale.x/originalScale,rootScale=helper.transform.localScale.x,prefabRootScale=prefab.transform.localScale.x};
                    File.WriteAllText(folder+"/helper-size.json",(string)serialize.Invoke(null,new object[]{result}));ScreenCapture.CaptureScreenshot(folder+"/boombar-size-final.png");helperDone=true;
                }
            }
        };
        EditorApplication.update+=tick;return new{bucketDone,helperDone,watching=true};
    }
}
