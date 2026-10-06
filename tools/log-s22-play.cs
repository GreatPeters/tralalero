using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class S22PlayLog
{
 public static object Main(string label){if(!EditorApplication.isPlaying)throw new Exception("Play required");string path="outputs/s22-polish-2026-10-01/"+label;File.WriteAllText(path+"-errors.txt","");File.WriteAllText(path+"-render.csv","realtime,batches,setpass,triangles\n");int count=0;float next=0;Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert){count++;File.AppendAllText(path+"-errors.txt",t+": "+m+"\n"+s+"\n");}};Application.logMessageReceived+=log;EditorApplication.CallbackFunction tick=null;tick=()=>{if(!EditorApplication.isPlaying){Application.logMessageReceived-=log;EditorApplication.update-=tick;File.WriteAllText(path+"-log-complete.txt","Error/Exception/Assert="+count);return;}if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+2;File.AppendAllText(path+"-render.csv",$"{Time.realtimeSinceStartup:F3},{UnityStats.batches},{UnityStats.setPassCalls},{UnityStats.triangles}\n");}};EditorApplication.update+=tick;return path;}
}
