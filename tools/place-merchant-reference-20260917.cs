using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class PlaceMerchantReference20260917{public static object Main(){
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="Noryangjin_MapTool_Mode_SR18")throw new Exception("SR18 required");
 var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var departure=map.Find("Props/HarborDeparture_20260916");
 var shop=departure.Find("ShoeWorkshop");shop.position=new Vector3(49,.07f,-121.5f);shop.rotation=Quaternion.Euler(0,65,0);shop.localScale=Vector3.one*1.1f;
 var merchant=departure.Find("MerchantStation");merchant.position=new Vector3(52.7f,.07f,-118.9f);merchant.localScale=Vector3.one*1.8f;merchant.rotation=Quaternion.Euler(0,70,0);
 var deck=map.Find("Roads/HarborWorkshopSideDeck");deck.position=new Vector3(44.07f,0,-118.8f);
 Physics.SyncTransforms();var roadColliders=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
 foreach(var point in new[]{merchant.position,shop.position}){
  bool supported=roadColliders.Any(c=>c.Raycast(new Ray(point+Vector3.up*2,Vector3.down),out var hit,3)&&Mathf.Abs(hit.point.y-point.y)<.3f);
  if(!supported)throw new Exception("Workshop object needs deck support: "+point);
 }
 foreach(var t in new[]{shop,merchant,deck}){EditorUtility.SetDirty(t);if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{merchant=merchant.position.ToString(),scale=merchant.localScale.x,shop=shop.position.ToString(),floorSupport=true};
}}
