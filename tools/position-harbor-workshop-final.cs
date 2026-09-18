using System;using System.Linq;using UnityEditor;using UnityEngine;using UnityEditor.SceneManagement;
public static class PositionHarborWorkshopFinal{public static object Main(){
 var map=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
 var props=map.Find("Props");var departure=props.Find("HarborDeparture_20260916");
 var workshop=departure.Find("ShoeWorkshop");workshop.position=new Vector3(24,.05f,-121.8f);workshop.rotation=Quaternion.Euler(0,60,0);
 workshop.Find("WorkshopSign").localPosition=new Vector3(0,3.55f,1.81f);
 departure.Find("MerchantStation").position=new Vector3(28,.05f,-119.3f);
 map.Find("Roads/HarborWorkshopSideDeck").position=new Vector3(18.07f,0,-119.5f);
 var signal=props.Cast<Transform>().Single(t=>t.name.Contains("Harbor_lane_signal_gantry"));signal.position=new Vector3(18,.02f,-115.11f);
 if(!EditorApplication.isPlaying){foreach(var t in new[]{workshop,departure.Find("MerchantStation"),map.Find("Roads/HarborWorkshopSideDeck"),signal}){EditorUtility.SetDirty(t);if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);}EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveOpenScenes();}
 return string.Join("\n",workshop.GetComponentsInChildren<Renderer>().Select(r=>r.name+" "+Camera.main.WorldToViewportPoint(r.bounds.center)));
}}
