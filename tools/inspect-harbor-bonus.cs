using System;using System.Linq;using UnityEditor;using UnityEngine;using IndianOceanAssets.ShooterSurvival;
public static class InspectHarborBonus{public static object Main(){
 var root=UnityEngine.Object.FindObjectsByType<AuthoredBonusWall>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.name).First();
 var lines=root.GetComponentsInChildren<Transform>(true).Select(t=>t.name+"\t"+t.localPosition+"\t"+t.localScale+"\t"+string.Join(",",t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name))).ToList();
 lines.Insert(0,"ROOT="+root.name+" source="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root));
 foreach(string path in new[]{"Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magical Source.prefab","Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magic Poof.prefab"}){
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);lines.Add(path+"\t"+string.Join(",",prefab.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null).Select(c=>c.GetType().Name))+"\tparticles="+prefab.GetComponentsInChildren<ParticleSystem>(true).Length);}
 System.IO.File.WriteAllLines("map-concepts/harbor-opening-refinement-2026-09-16/bonus-before.txt",lines);return string.Join("\n",lines.Take(42));
}}
