using System;using UnityEngine;using UnityEditor;using UnityEngine.Localization.Settings;
public static class UiFinalCycle07 {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Clean Edit required");
  string locale=LocalizationSettings.SelectedLocale.Identifier.Code;
  if(locale!="ko-KR")throw new Exception("Original locale not restored");
  return new{pid=System.Diagnostics.Process.GetCurrentProcess().Id,selectedLocale=locale,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),readOnly=true};
 }
}
