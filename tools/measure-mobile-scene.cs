using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class MeasureMobileScene
{
    public static string Main(string output,bool compareBaked=false)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required.");
        if(File.Exists(output))throw new IOException("Preserve earlier measurements.");
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas,output,compareBaked));return output;
    }
    static IEnumerator Run(CanvasScript canvas,string output,bool compareBaked)
    {
        UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include)?.Skip();
        var tutorial=UnityEngine.Object.FindFirstObjectByType<CoastalTutorialUI>(FindObjectsInactive.Include);
        if(tutorial!=null){tutorial.enabled=false;tutorial.Close();}
        canvas.PlayerPressedStartButton();yield return null;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();player.canShoot=false;
        foreach(var weapon in player.GetComponentsInChildren<WeaponScript>(true)){weapon.StopAllCoroutines();weapon.enabled=false;}
        var holdout=new GameObject("Performance fixture stationary owner").AddComponent<RestStopHoldout>();holdout.enabled=false;holdout.police=Array.Empty<EnemyEventController>();
        player.SetStationaryCombat(holdout,true);player.currentHealth=100000;
        var camera=Camera.main;var occlusion=camera.GetComponent<NoryangjinCameraOcclusion>();
        int oldRate=Application.targetFrameRate,oldSync=QualitySettings.vSyncCount;float oldCapture=Time.captureDeltaTime;
        float oldFar=camera.farClipPlane;bool oldBaked=camera.useOcclusionCulling;
        var animationModes=UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).ToDictionary(a=>a,a=>a.cullingMode);
        Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;Time.captureDeltaTime=0;TimeManager.isGameRunning=true;TimeManager.timeFactor=1;
        var samples=new List<object>();
        try
        {
            foreach(string stage in compareBaked?new[]{"current","baked-occlusion-disabled"}:new[]{"current","road-occlusion-disabled","far-90","far-90-animation-culling"})
            {
                if(occlusion!=null)occlusion.enabled=stage!="road-occlusion-disabled";
                camera.farClipPlane=stage.StartsWith("far-90")?90f:oldFar;
                camera.useOcclusionCulling=stage!="baked-occlusion-disabled"&&oldBaked;
                if(stage.EndsWith("animation-culling"))foreach(var pair in animationModes)
                    if(pair.Key.GetComponentInParent<EnemyEventController>()!=null)pair.Key.cullingMode=AnimatorCullingMode.CullCompletely;
                for(int i=0;i<45;i++)yield return new WaitForEndOfFrame();
                var ms=new List<float>();var batches=new List<int>();var passes=new List<int>();var triangles=new List<int>();
                for(int i=0;i<120;i++)
                {
                    yield return new WaitForEndOfFrame();
                    ms.Add(Time.unscaledDeltaTime*1000);batches.Add(UnityStats.batches);passes.Add(UnityStats.setPassCalls);triangles.Add(UnityStats.triangles);
                }
                ms.Sort();
                var animators=UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
                var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                samples.Add(new{stage,meanMs=ms.Average(),medianMs=ms[ms.Count/2],p95Ms=ms[(int)(ms.Count*.95f)],fps=1000/ms.Average(),batches=batches.Average(),setPass=passes.Average(),triangles=triangles.Average(),animators=animators.Count(a=>a.enabled),staticBatched=renderers.Count(r=>r.isPartOfStaticBatch),visibleRenderers=renderers.Count(r=>r.isVisible&&r.enabled),targetFps=Application.targetFrameRate,focus=Application.isFocused,resolution=$"{Screen.width}x{Screen.height}",position=player.transform.position.ToString(),cameraFar=camera.farClipPlane});
                Write(output,samples);
                var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(output),Path.GetFileNameWithoutExtension(output)+"-"+stage+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);
            }
        }
        finally
        {
            if(occlusion!=null)occlusion.enabled=true;camera.farClipPlane=oldFar;camera.useOcclusionCulling=oldBaked;foreach(var pair in animationModes)if(pair.Key!=null)pair.Key.cullingMode=pair.Value;Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldSync;Time.captureDeltaTime=oldCapture;
            TimeManager.isGameRunning=false;player.SetStationaryCombat(holdout,false);UnityEngine.Object.Destroy(holdout.gameObject);
        }
    }
    static void Write(string output,object result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{result});File.WriteAllText(output,json);
    }
}
