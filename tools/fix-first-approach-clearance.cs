using System;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using IndianOceanAssets.ShooterSurvival;
public static class FixFirstApproachClearance {public static object Main(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
 var enemy=map.Find("Enemies/SR18_L_E01_T005_Enemy_OldMan").GetComponent<EnemyEventController>();var old=enemy.transform.position;var next=old;next.z=-82.75f;enemy.transform.position=next;enemy.RefreshPlacementAfterAuthoringChange(old,false);enemy.TargetPoint.position=next-enemy.transform.forward*2;
 enemy.GetComponent<EnemyCornerClearance>().ValidateMovement(next,enemy.TargetPoint.position);
 foreach(var spot in map.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>(true).Where(s=>s.Targets.Contains(enemy)))spot.transform.position=next-enemy.transform.forward*12;
 EditorUtility.SetDirty(enemy.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GameDataWorkbookEditor")).First(t=>t!=null);type.GetMethod("EnsureRuntimeArchiveCurrent").Invoke(null,new object[]{true});return next.ToString();
}}
