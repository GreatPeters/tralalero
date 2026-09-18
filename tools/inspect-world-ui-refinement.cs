using System.Linq;using UnityEngine;using UnityEditor;using TMPro;using IndianOceanAssets.ShooterSurvival;
public static class InspectWorldUIRefinement{public static object Main(){
 var player=Object.FindFirstObjectByType<PlayerScript>();var enemy=Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(e=>e.name=="SR18_L_E01_T005_Enemy_OldMan");
 var so=new SerializedObject(player);var bar=so.FindProperty("healthBar").objectReferenceValue as UnityEngine.UI.Image;
 return string.Join("\n",enemy.GetComponentsInChildren<TMP_Text>(true).Select(t=>t.name+" font="+t.font.name+" size="+t.fontSize+" scale="+t.transform.lossyScale+" rect="+t.rectTransform.rect+" path="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t)))+"\nPLAYER BAR "+(bar!=null?bar.name+" parent="+bar.transform.parent.name+" root="+bar.transform.root.name:"null")+"\nPLAYER CANVASES "+string.Join(",",player.GetComponentsInChildren<Canvas>(true).Select(c=>c.name+" active="+c.gameObject.activeSelf));
}}
