using System;using System.Linq;using System.IO;using UnityEngine;using UnityEngine.UI;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;
public static class ValidationInspect13 {
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 static object ButtonRow(Button b)=>new{path=PathOf(b.transform),active=b.gameObject.activeInHierarchy,b.interactable,text=b.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t=>t.text).ToArray(),events=Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Select(i=>new{method=b.onClick.GetPersistentMethodName(i),target=b.onClick.GetPersistentTarget(i)?.GetType().FullName,state=b.onClick.GetPersistentListenerState(i).ToString()}).ToArray()};
 public static object Main(){var cp=UnityEngine.Object.FindFirstObjectByType<ChapterProgression>();var c=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();return new{scene=SceneManager.GetActiveScene().name,playing=Application.isPlaying,chapter=cp.chapter,cp.nextScene,movie=cp.nextChapterMovie!=null?cp.nextChapterMovie.name:null,buttons=c.GetComponentsInChildren<Button>(true).Select(ButtonRow).ToArray()};}
}
