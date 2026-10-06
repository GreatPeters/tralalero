using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;
public static class S22Presentation
{
 const string Out="outputs/s22-polish-2026-10-01";
 const string Audio="Assets/ShooterSurvival/Audio/NoryangjinRevamp/S22Polish";
 static void Folder(string p){string parent="Assets";foreach(var part in p.Split('/').Skip(1)){if(!AssetDatabase.IsValidFolder(parent+"/"+part))AssetDatabase.CreateFolder(parent,part);parent+="/"+part;}}
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 public static object Main(){if(File.Exists(Out+"/presentation-install.json"))return "Already applied; use a targeted revision for further changes.";if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");Folder(Audio);var voices=new Dictionary<string,AudioClip>();foreach(var file in Directory.GetFiles(Out+"/voices","*.wav")){string dest=Audio+"/"+Path.GetFileName(file);if(!File.Exists(dest))File.Copy(file,dest);AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceSynchronousImport);var importer=(AudioImporter)AssetImporter.GetAtPath(dest);importer.forceToMono=true;var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.65f;importer.defaultSampleSettings=sample;importer.SaveAndReimport();voices[Path.GetFileNameWithoutExtension(file)]=AssetDatabase.LoadAssetAtPath<AudioClip>(dest);}
  var setup=EditorSceneManager.GetSceneManagerSetup();var report=new List<object>();
  try{foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18_Revamp","HighWay","RestStop"}){
   var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");int references=0,aligned=0,removed=0;
   foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null&&c.GetType().Name.StartsWith("Noryangjin"))){using var so=new SerializedObject(component);var p=so.GetIterator();bool dirty=false;while(p.Next(true)){if(p.propertyType!=SerializedPropertyType.ObjectReference)continue;var clip=p.objectReferenceValue as AudioClip;if(clip==null||!AssetDatabase.GetAssetPath(clip).Contains("Audio/NoryangjinRevamp"))continue;if(voices.TryGetValue(clip.name,out var replacement)&&clip!=replacement){p.objectReferenceValue=replacement;dirty=true;references++;}}if(dirty){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(component);}}
   if(name.Contains("Revamp")){
    var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
    var signs=all.Where(t=>t.name=="NumberedStoreSign").OrderBy(t=>t.localPosition.z).ToArray();
    for(int i=0;i<signs.Length;i++){
     var sign=signs[i];if((i/2)%2==1){Object.DestroyImmediate(sign.gameObject);removed++;continue;}
     var frame=sign.Find("Frame").GetComponent<Renderer>();foreach(var text in sign.GetComponentsInChildren<TMP_Text>(true)){float y=text.name=="Category"?.4f:-.2f;text.transform.position=frame.bounds.center+sign.up*y-sign.forward*.115f;if(text.name=="Category")text.text="활어 · 선어";text.ForceMeshUpdate(true,true);EditorUtility.SetDirty(text);aligned++;}
    }
    var detail=Object.FindFirstObjectByType<NoryangjinInteriorDetailVisibility>();if(detail!=null){detail.bays=detail.bays.Where(t=>t!=null).ToArray();detail.hangingDisplays=detail.hangingDisplays.Where(t=>t!=null).ToArray();detail.liftedDetails=detail.liftedDetails.Where(t=>t!=null).ToArray();EditorUtility.SetDirty(detail);}
    var view=Object.FindFirstObjectByType<NoryangjinCameraOcclusion>();if(view!=null){var so=new SerializedObject(view);var list=so.FindProperty("clearViewGroups");var groups=Enumerable.Range(0,list.arraySize).Select(i=>list.GetArrayElementAtIndex(i).objectReferenceValue as Transform).Where(t=>t!=null).ToArray();view.ConfigureFeedbackTransparency(groups);EditorUtility.SetDirty(view);}
    foreach(var cap in all.Where(t=>t!=null&&t.name=="BidderCap"))Object.DestroyImmediate(cap.gameObject);
    var auction=all.FirstOrDefault(t=>t!=null&&t.name=="LiveAuction");if(auction!=null){int n=0;foreach(var animator in auction.GetComponentsInChildren<Animator>(true)){animator.cullingMode=AnimatorCullingMode.CullCompletely;var root=animator.transform.parent.parent;float scale=.95f+(n%5)*.025f;root.localScale*=scale;var tint=root.GetComponent<NoryangjinShopTint>()??root.gameObject.AddComponent<NoryangjinShopTint>();tint.color=new[]{new Color(1,.96f,.94f),new Color(.94f,1,.97f),new Color(.95f,.97f,1),Color.white}[n++%4];EditorUtility.SetDirty(tint);EditorUtility.SetDirty(animator);}}
    foreach(var wave in Object.FindObjectsByType<NoryangjinWaveEvent>(FindObjectsInactive.Include,FindObjectsSortMode.None)){wave.foamScale=1.75f;wave.crestScale=2.9f;EditorUtility.SetDirty(wave);}
   }
   if(name=="RestStop"){
    var building=GameObject.Find("Noryangjin_MapTool/KoreanRestStop_20260925/04_MainBuilding");var visibility=Object.FindFirstObjectByType<RestStopBuildingVisibility>();
    if(building!=null&&visibility!=null){var signs=building.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Sign"&&t.position.y>3).SelectMany(t=>t.GetComponentsInChildren<Renderer>(true));visibility.roof=visibility.roof.Concat(signs).Where(r=>r!=null).Distinct().ToArray();var zone=visibility.hideZone;zone.Expand(new Vector3(0,0,24));visibility.hideZone=zone;EditorUtility.SetDirty(visibility);}
    var floor=GameObject.Find("Noryangjin_MapTool/Roads/FoodHall_Floor");if(floor!=null){var source=floor.GetComponent<Renderer>().sharedMaterial;string dest="Assets/ShooterSurvival/Materials/Generated/S22Polish/FoodHallReadable.mat";var tinted=AssetDatabase.LoadAssetAtPath<Material>(dest);if(tinted==null){tinted=new Material(source){name="Food hall readable floor"};if(tinted.HasProperty("_BaseColor"))tinted.SetColor("_BaseColor",new Color(.72f,.77f,.76f));AssetDatabase.CreateAsset(tinted,dest);}foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))){var values=r.sharedMaterials;bool changed=false;for(int i=0;i<values.Length;i++)if(values[i]==source){values[i]=tinted;changed=true;}if(changed){r.sharedMaterials=values;EditorUtility.SetDirty(r);}}}
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(new{scene=name,voiceReferences=references,signLabelsAligned=aligned,decorativeSignsRemoved=removed});
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}File.WriteAllText(Out+"/presentation-install.json",Json(report));return report;
 }
}
