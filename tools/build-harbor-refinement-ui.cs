using System;using System.Linq;using UnityEditor;using UnityEngine;
public static class BuildHarborRefinementUI{public static object Main(){
 HarborRefinementFontBuilder.Build();
 var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GameDataWorkbookEditor")).First(t=>t!=null);
 type.GetMethod("EnsureRuntimeArchiveCurrent").Invoke(null,new object[]{true});
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())throw new Exception("Could not preserve scene state after font/data import");
 return HarborGameUIInstaller.ApplyCoastalAll();
}}
