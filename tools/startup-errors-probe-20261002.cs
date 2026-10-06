using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class StartupErrorsProbe20261002
{
 const string Root="outputs/startup-errors-2026-10-02";
 static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
 public static object Probe(string label)
 {
  Directory.CreateDirectory(Root);var rows=new List<object>();
  foreach(var shaderName in new[]{"FlatKit/Stylized Surface","FlatKit/Stylized Surface With Outline"})
  {
   var messages=new List<string>();Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Assert||t==LogType.Exception)messages.Add(m.Split('\n')[0]);};Application.logMessageReceived+=log;
   var shader=Shader.Find(shaderName);var mat=new Material(shader);var results=new List<object>();var target=new RenderTexture(32,32,24);target.Create();var previous=RenderTexture.active;RenderTexture.active=target;
   try{for(int i=0;i<mat.passCount;i++)results.Add(new{pass=mat.GetPassName(i),ok=mat.SetPass(i)});rows.Add(new{shaderName,keywords=shader.keywordSpace.keywordCount,passes=results,errors=messages.ToArray()});}
   finally{Application.logMessageReceived-=log;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(mat);}
  }
  File.WriteAllText(Root+"/"+label+".json",Json(rows));return new{rows};
 }
 public static object Snapshot()
 {
  Directory.CreateDirectory(Root);var status=ChapterPlaytestPreferences.SnapshotAt(Root+"/preferences-before.tsv");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();File.WriteAllText(Root+"/scene-before.txt",scene.path);return new{status,scene=scene.path,scene.isDirty,play=EditorApplication.isPlaying};
 }
 public static object Status()=>new{EditorApplication.isCompiling,EditorApplication.isUpdating,EditorApplication.isPlaying,EditorApplication.isPaused,EditorUtility.scriptCompilationFailed,newTestLoaded=AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetType("LegacyOutlineShaderTests")!=null)};
 public static object Live(string label)
 {
  var story=Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);var shader=Shader.Find("FlatKit/Stylized Surface With Outline");var method=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
  var data=new{EditorApplication.isPlaying,EditorApplication.isPaused,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,movie=AssetDatabase.GetAssetPath(story.movie),story.CurrentPage,story.IsSeeking,videoPlaying=story.video.isPlaying,videoPrepared=story.video.isPrepared,videoFrame=story.video.frame,videoTime=story.video.time,frames=story.movie.frameCount,seconds=story.movie.length,srpBatcherCode=(int)method.Invoke(null,new object[]{shader,0}),shaderErrors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray(),batches=UnityStats.batches,setPass=UnityStats.setPassCalls};
  File.WriteAllText(Root+"/"+label+".json",Json(data));return data;
 }
 public static object Story(string action)
 {
  var story=Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);
  if(action=="open")story.Open();else if(action=="next")story.Next();else if(action=="previous")story.Previous();else if(action=="skip")story.Skip();else throw new ArgumentException(action);return new{action,story.CurrentPage,story.IsSeeking};
 }
 public static object MovieNavigation()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Play required");
  var story=Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);var rows=new List<object>();int phase=0;double deadline=EditorApplication.timeSinceStartup+35,next=0;story.Open();
  EditorApplication.CallbackFunction tick=null;tick=()=>{try{
   if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}
   if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Movie navigation timeout at phase "+phase);
   if(EditorApplication.timeSinceStartup<next||!story.IsMoviePlaying||story.IsSeeking)return;
   if(phase<4){if(story.CurrentPage!=phase||story.video.time<OpeningStoryUI.GetMoviePageStart(phase))return;rows.Add(new{action="scene"+phase,page=story.CurrentPage,frame=story.video.frame,time=story.video.time});if(phase<3)story.Next();else{string folder="tmp/image-previews/startup-errors-2026-10-02";Directory.CreateDirectory(folder);ScreenCapture.CaptureScreenshot(folder+"/original-scene4.png");next=EditorApplication.timeSinceStartup+.5;}phase++;return;}
   if(phase==4){story.Previous();phase++;return;}
   if(phase==5){if(story.CurrentPage!=2)throw new Exception("Previous did not select scene 3");rows.Add(new{action="previous",page=story.CurrentPage,frame=story.video.frame,time=story.video.time});story.Skip();rows.Add(new{action="skip",released=story.video.targetTexture==null,playing=story.video.isPlaying});story.Open();phase++;return;}
   rows.Add(new{action="reopen",page=story.CurrentPage,frame=story.video.frame,time=story.video.time});story.Skip();File.WriteAllText(Root+"/movie-navigation.json",Json(rows));EditorApplication.update-=tick;
  }catch(Exception ex){story.Skip();EditorApplication.update-=tick;File.WriteAllText(Root+"/movie-navigation-error.txt",ex.ToString());}};EditorApplication.update+=tick;return "Checking four scenes, previous, skip and reopen";
 }
 public static object RestartCleanEditor()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Do not discard a dirty scene");
  var saved=new{power=SessionState.GetBool("NoryangjinMapTool.TestPower9999",false),lateral=SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false),speed=SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1),opening=OpeningStoryUI.EditorAutoPlayEnabled,stage=SessionState.GetInt("NoryangjinMapTool.TestStartStage",0),startScene=AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene),target=EditorUserBuildSettings.activeBuildTarget.ToString()};
  if(!File.Exists(Root+"/session-before.json"))File.WriteAllText(Root+"/session-before.json",Json(saved));
  EditorApplication.delayCall+=()=>EditorApplication.Exit(0);return new{closing=true,saved};
 }
 [Serializable] private sealed class SavedSession{public bool power=false,lateral=false,opening=true;public float speed=1;public int stage=0;public string startScene="",target="";}
 public static object Restore()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Do not discard a dirty scene");
  var prefs=ChapterPlaytestPreferences.RestoreAt(Root+"/preferences-before.tsv");var s=JsonUtility.FromJson<SavedSession>(File.ReadAllText(Root+"/session-before.json"));
  SessionState.SetBool("NoryangjinMapTool.TestPower9999",s.power);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",s.lateral);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",s.speed);SessionState.SetInt("NoryangjinMapTool.TestStartStage",s.stage);OpeningStoryUI.EditorAutoPlayEnabled=s.opening;
  UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(s.startScene)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(s.startScene);
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene(File.ReadAllText(Root+"/scene-before.txt"));
  var result=new{prefs,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,play=EditorApplication.isPlaying,power=SessionState.GetBool("NoryangjinMapTool.TestPower9999",false),lateral=SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false),speed=SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1),opening=OpeningStoryUI.EditorAutoPlayEnabled};File.WriteAllText(Root+"/restored.json",Json(result));return result;
 }
}
