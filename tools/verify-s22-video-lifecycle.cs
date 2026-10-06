using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;
public static class S22VideoLifecycle
{
 const string Root="outputs/s22-polish-2026-10-01";
 public static object Main(){if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new Exception("Play lobby required");Object.FindFirstObjectByType<CanvasScript>().StartCoroutine(Run());return "Verifying opening replay/skip, transition EOF/skip, render target and world camera restoration";}
 static IEnumerator Run(){var observations=new List<string>();var errors=new List<string>();Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message);};Application.logMessageReceived+=log;
  var opening=Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);var world=Camera.main;var owner=Object.FindFirstObjectByType<CanvasScript>();
  try{
   opening.Open();yield return new WaitForSecondsRealtime(4);observations.Add($"opening playing={opening.IsMoviePlaying}, frame={opening.video.frame}, worldEnabled={world.enabled}, worldMask={world.cullingMask}");ScreenCapture.CaptureScreenshot("tmp/image-previews/s22-polish-2026-10-01/video-world-culled-playing.png");yield return new WaitForEndOfFrame();
   opening.Skip();yield return null;observations.Add($"skip worldEnabled={world.enabled}, worldMask={world.cullingMask}, targetReleased={opening.video.targetTexture==null}, playing={opening.video.isPlaying}");
   opening.Open();yield return new WaitForSecondsRealtime(3);observations.Add($"replay playing={opening.IsMoviePlaying}, frame={opening.video.frame}, worldEnabled={world.enabled}, worldMask={world.cullingMask}");for(int i=0;i<3;i++){opening.Next();float until=Time.realtimeSinceStartup+3;while(opening.IsSeeking&&Time.realtimeSinceStartup<until)yield return null;}yield return new WaitForSecondsRealtime(1);observations.Add($"last scene page={opening.CurrentPage}, frame={opening.video.frame}, movie={opening.movie.name}");ScreenCapture.CaptureScreenshot("tmp/image-previews/s22-polish-2026-10-01/video-market-aligned.png");yield return new WaitForEndOfFrame();opening.gameObject.SetActive(false);yield return null;observations.Add($"disable worldEnabled={world.enabled}, worldMask={world.cullingMask}, targetReleased={opening.video.targetTexture==null}");
   var transition=Object.FindFirstObjectByType<ChapterTransitionUI>(FindObjectsInactive.Include);var clip=Object.FindFirstObjectByType<ChapterProgression>().nextChapterMovie;
   yield return owner.StartCoroutine(transition.Present(clip,"검증","자동 종료"));observations.Add($"transition EOF worldEnabled={world.enabled}, worldMask={world.cullingMask}, targetReleased={transition.player.targetTexture==null}, presenting={transition.IsPresenting}");
   owner.StartCoroutine(transition.Present(clip,"검증","건너뛰기"));yield return new WaitForSecondsRealtime(1);observations.Add($"transition active worldEnabled={world.enabled}, worldMask={world.cullingMask}, presenting={transition.IsPresenting}");transition.Skip();yield return new WaitForSecondsRealtime(.5f);observations.Add($"transition skip worldEnabled={world.enabled}, worldMask={world.cullingMask}, targetReleased={transition.player.targetTexture==null}, presenting={transition.IsPresenting}");
  }finally{opening.Skip();Application.logMessageReceived-=log;File.WriteAllLines(Root+"/video-lifecycle-culling.txt",observations.Concat(new[]{"Errors:"}).Concat(errors));}
 }
}
