using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class VerifyEditorReentryCycle05 {
 static readonly FieldInfo SceneUnloaded=typeof(SceneManager).GetField("sceneUnloaded",BindingFlags.NonPublic|BindingFlags.Static);
 static EventInfo forwarded;static string output;static int phase,iteration,handle,localCalls,forwardedCalls,failed,frame=-1;static double at,began;static string[] otherBefore;
 static readonly List<object> checks=new();static readonly List<string> errors=new();
 static string Json(object o)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
 static Delegate[] Subscriptions()=>(SceneUnloaded.GetValue(null) as Delegate)?.GetInvocationList()??Array.Empty<Delegate>();
 static bool IsCollector(Delegate d)=>d.Method.DeclaringType?.DeclaringType?.FullName=="Unity.VisualScripting.ReferenceCollector"&&d.Method.Name.StartsWith("<Initialize>",StringComparison.Ordinal);
 static string[] Others()=>Subscriptions().Where(d=>!IsCollector(d)).Select(d=>d.Method.DeclaringType.FullName+"."+d.Method.Name+"|"+d.Target?.GetType().FullName).OrderBy(x=>x).ToArray();
 static void Check(bool pass,string label,object evidence=null){checks.Add(new{pass,label,iteration,evidence});if(!pass)failed++;}
 static void OnForwarded()=>forwardedCalls++;
 static void OnLocal(Scene s)=>localCalls++;
 static void Log(string m,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);}
 static void Next(int n){phase=n;at=EditorApplication.timeSinceStartup;}
 public static object Main(string root){
  if(!EditorApplication.isPlaying)throw new Exception("Play required");output=root;Directory.CreateDirectory(root);checks.Clear();errors.Clear();failed=phase=iteration=localCalls=forwardedCalls=0;frame=-1;began=EditorApplication.timeSinceStartup;
  Check(EditorSettings.enterPlayModeOptionsEnabled&&(EditorSettings.enterPlayModeOptions&EnterPlayModeOptions.DisableDomainReload)!=0,"Existing no-domain-reload setting retained");
  Check(Subscriptions().Count(IsCollector)==1,"Play entry retains exactly one Visual Scripting collector callback",new{count=Subscriptions().Count(IsCollector)});
  var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.VisualScripting.ReferenceCollector")).First(t=>t!=null);forwarded=type.GetEvent("onSceneUnloaded",BindingFlags.Public|BindingFlags.Static);forwarded.AddEventHandler(null,(Action)OnForwarded);SceneManager.sceneUnloaded+=OnLocal;
  otherBefore=Others();Application.logMessageReceived+=Log;EditorApplication.update+=Tick;Next(0);return new{started=true,output};
 }
 static void Tick(){EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;try{
  double age=EditorApplication.timeSinceStartup-at;if(EditorApplication.timeSinceStartup-began>30)throw new TimeoutException("Replay callback verification");
  if(phase==0){if(age<.4)return;OpeningStoryUI.Instance?.Skip();handle=SceneManager.GetActiveScene().handle;localCalls=forwardedCalls=0;Object.FindFirstObjectByType<ChapterProgression>().Replay();Next(1);return;}
  if(phase==1){if(age<.8||SceneManager.GetActiveScene().handle==handle)return;
   Check(localCalls==1&&forwardedCalls==1,"Actual scene Replay dispatches the local and Visual Scripting unload signals once",new{localCalls,forwardedCalls});
   Check(Subscriptions().Count(IsCollector)==1,"Scene reload does not duplicate the collector");
   Check(Others().SequenceEqual(otherBefore),"Non-Visual-Scripting scene-unload subscribers preserved",new{before=otherBefore,after=Others()});
   var d=Object.FindFirstObjectByType<Chapter45Director>();Check(d!=null&&!d.Running&&d.Elapsed==0&&!d.IsTransferring&&d.Player.currentHealth==60&&!CanvasScript.isGameOver,"Replay returns a fresh alive lobby without stale game clocks");
   iteration++;if(iteration>=2){Finish();return;}Next(0);
  }
 }catch(Exception e){errors.Add(e.ToString());Finish();}}
 static void Finish(){EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SceneManager.sceneUnloaded-=OnLocal;forwarded?.RemoveEventHandler(null,(Action)OnForwarded);File.WriteAllText(Path.Combine(output,"summary.json"),Json(new{passed=checks.Count-failed,failed,checks,errors,actualSceneReplays=iteration,collectorCount=Subscriptions().Count(IsCollector),remainingSubscribers=Others()}));}
}
