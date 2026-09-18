using System;using System.Linq;using UnityEditor;using UnityEngine;using IndianOceanAssets.ShooterSurvival;
public static class InspectOpeningGround{public static object Main(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
 var roads=map.Find("Roads");var lines=new System.Collections.Generic.List<string>();
 foreach(var collider in roads.GetComponentsInChildren<MeshCollider>(true))if(collider.Raycast(new Ray(player.transform.position+Vector3.up*5,Vector3.down),out var hit,10))lines.Add("FLOOR\t"+collider.name+"\t"+collider.transform.position+"\t"+collider.bounds+"\t"+hit.point);
 foreach(Transform t in roads.Cast<Transform>().OrderBy(t=>Vector3.ProjectOnPlane(t.position-player.transform.position,Vector3.up).sqrMagnitude).Take(12))lines.Add("ROAD\t"+t.name+"\t"+t.position+"\t"+t.eulerAngles+"\t"+t.localScale);
 foreach(var e in map.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true).Where(e=>e.name.Contains("E01_"))) {var a=e.GetComponentInChildren<Animator>(true);lines.Add("ANIM\t"+e.name+"\t"+a.runtimeAnimatorController.name+"\t"+string.Join(",",a.runtimeAnimatorController.animationClips.Select(c=>c.name)));}
 System.IO.File.WriteAllLines("map-concepts/harbor-opening-refinement-2026-09-16/ground-before.txt",lines);return string.Join("\n",lines);
}}
