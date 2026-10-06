using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Development players only. One CSV row per second, no per-frame I/O or release UI.
public sealed class MobileFrameTelemetry : MonoBehaviour
{
    private readonly FrameTiming[] timings=new FrameTiming[1];
    private readonly float[] samples=new float[120];
    private readonly float[] sorted=new float[120];
    private int count,writeAt;
    private float nextReport;
    private string path,label;
    private GUIStyle style;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if(!Debug.isDebugBuild||Application.isEditor)return;
        var root=new GameObject("Mobile frame telemetry");DontDestroyOnLoad(root);root.AddComponent<MobileFrameTelemetry>();
    }
    private void Awake()
    {
        path=Path.Combine(Application.persistentDataPath,"frame-times-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".csv");
        File.WriteAllText(path,"utc,scene,frames,median_ms,p95_ms,cpu_ms,gpu_ms\n");
        Debug.Log("Performance CSV: "+path+"; GPU="+SystemInfo.graphicsDeviceName+"; API="+SystemInfo.graphicsDeviceType);
    }
    private void Update()
    {
        samples[writeAt++%samples.Length]=Time.unscaledDeltaTime*1000f;count=Mathf.Min(count+1,samples.Length);
        FrameTimingManager.CaptureFrameTimings();
        if(Time.unscaledTime<nextReport||count<30)return;nextReport=Time.unscaledTime+1;
        Array.Copy(samples,sorted,count);Array.Sort(sorted,0,count);float median=sorted[count/2],p95=sorted[Mathf.Min(count-1,(int)(count*.95f))];
        uint available=FrameTimingManager.GetLatestTimings(1,timings);double cpu=available>0?timings[0].cpuFrameTime:0,gpu=available>0?timings[0].gpuFrameTime:0;
        label=$"{1000f/Mathf.Max(.01f,median):F0} FPS  |  {median:F1} ms  |  p95 {p95:F1} ms";
        File.AppendAllText(path,string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:O},{1},{2},{3:F3},{4:F3},{5:F3},{6:F3}\n",DateTime.UtcNow,SceneManager.GetActiveScene().name,count,median,p95,cpu,gpu));
    }
    private void OnGUI()
    {
        if(style==null)style=new GUIStyle(GUI.skin.box){fontSize=Mathf.Max(14,Screen.width/45),alignment=TextAnchor.MiddleCenter};
        GUI.Box(new Rect(10,Screen.height-42,Screen.width-20,32),label??"Measuring frames...",style);
    }
}
