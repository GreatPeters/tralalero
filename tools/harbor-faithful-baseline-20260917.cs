using System;using System.Linq;using System.IO;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using TMPro;
public static class HarborFaithfulBaseline20260917{public static object Main(){
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
 string folder="tmp/backups/harbor-faithful-art-2026-09-17";if(Directory.Exists(folder))throw new Exception("Preserve existing baseline");Directory.CreateDirectory(folder);
 ChapterPlaytestPreferences.SnapshotAt(folder+"/playerprefs.tsv");
 File.WriteAllLines(folder+"/extra-prefs.tsv",new[]{"TutorialDone","soundEnabled","vibrationEnabled"}.Select(k=>k+"\tint\t"+PlayerPrefs.HasKey(k)+"\t"+PlayerPrefs.GetInt(k)));
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene,folder+"/open-before.unity",true);if(scene.isDirty)EditorSceneManager.SaveScene(scene);
 foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"})File.Copy("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity",folder+"/"+name+".unity");
 foreach(string dir in new[]{"Assets/ShooterSurvival/Editor","Assets/ShooterSurvival/Scripts/UI","Assets/ShooterSurvival/Resources/UI/HarborMaterials"})foreach(string source in Directory.GetFiles(dir,"*",SearchOption.AllDirectories)){if(!source.EndsWith(".cs")&&!source.EndsWith(".mat"))continue;string target=folder+"/files/"+source;Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target);}
 var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas");var lines=new List<string>();
 foreach(var t in canvas.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.transform.IsChildOf(canvas.transform.Find("UI/Main"))||t.transform.IsChildOf(canvas.transform.Find("UI/Top/Resource")))){
 var m=t.fontSharedMaterial;lines.Add(t.name+" text="+t.text.Replace('\n','|')+" font="+t.font.name+" size="+t.fontSize+" scale="+t.transform.lossyScale+" material="+m.name+" outline="+m.GetFloat("_OutlineWidth")+" enabled="+m.IsKeywordEnabled("OUTLINE_ON"));}
 var merchant=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HarborMerchantGreeting>(true)).Single();lines.Add("Merchant="+merchant.transform.position+" scale="+merchant.transform.lossyScale);
 foreach(var r in merchant.GetComponentsInChildren<Renderer>(true))lines.Add(r.name+" bounds="+r.bounds+" viewport="+Camera.main.WorldToViewportPoint(r.bounds.center));
 Directory.CreateDirectory("map-concepts/harbor-faithful-art-2026-09-17");File.WriteAllLines("map-concepts/harbor-faithful-art-2026-09-17/baseline.txt",lines);return string.Join("\n",lines);
}}

