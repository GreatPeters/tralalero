using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;
public static class S22MarketView
{
 public static object Main(){if(EditorApplication.isPlaying)throw new Exception("Edit required");var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18_Revamp.unity");var hall=GameObject.Find("NoryangjinRevamp/MarketHall");var floor=GameObject.Find("Noryangjin_MapTool/Roads/NoryangjinRevampSurfaces/IndoorTileSurface");var view=Object.FindFirstObjectByType<NoryangjinCameraOcclusion>();var visuals=hall.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="WetFloor"||r.name=="CrossingUndersideSlab").ToArray();view.ConfigureWalkingFloor(floor.transform,visuals);EditorUtility.SetDirty(view);
  int backs=0;foreach(var sign in hall.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="NumberedStoreSign").ToArray()){var panel=sign.Find("Frame").GetComponent<Renderer>();foreach(var label in sign.GetComponentsInChildren<TMP_Text>(true).Where(t=>!t.name.EndsWith("_Back")).ToArray()){if(sign.Find(label.name+"_Back")!=null)continue;var copy=Object.Instantiate(label.gameObject,sign).GetComponent<TMP_Text>();copy.name=label.name+"_Back";copy.transform.position=label.transform.position+sign.forward*.25f;copy.transform.rotation=Quaternion.AngleAxis(180,sign.up)*label.transform.rotation;copy.ForceMeshUpdate(true,true);backs++;}}
  // Keep the two sides and the panel in the same visibility/occlusion group.
  var serialized=new SerializedObject(view);var list=serialized.FindProperty("clearViewGroups");var groups=Enumerable.Range(0,list.arraySize).Select(i=>list.GetArrayElementAtIndex(i).objectReferenceValue as Transform).Where(t=>t!=null).ToArray();view.ConfigureFeedbackTransparency(groups);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("outputs/s22-polish-2026-10-01/market-view-refinement.txt",$"{visuals.Length} floor visuals preserve opacity on their support collider; {backs} sign reverse-face labels added.");return new{floorVisuals=visuals.Length,backs};}
}
