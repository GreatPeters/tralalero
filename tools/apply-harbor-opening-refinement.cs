using System;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
public static class ApplyHarborOpeningRefinement{public static object Main(){
 EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
 var opening=HarborOpeningRefinementInstaller.ApplyOpening();
 var idles=HarborOpeningRefinementInstaller.ApplyCalmIdles();
 var bonus=HarborBonusPresentationInstaller.ApplyAll();
 return new{opening,idles,bonus};
}}
