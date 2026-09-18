using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class InspectHarborOpening {
 [Serializable] public class Node {public string name,mode;public Vector3 position,rotation,scale,boundsCenter,boundsSize,target;}
 [Serializable] public class Report {public Vector3 player,forward;public Node[] roads,props,enemies;}
 static Node Read(Transform t){var r=t.GetComponentInChildren<Renderer>();return new Node{name=t.name,position=t.position,rotation=t.eulerAngles,scale=t.localScale,boundsCenter=r!=null?r.bounds.center:Vector3.zero,boundsSize=r!=null?r.bounds.size:Vector3.zero};}
 public static object Main(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
  var data=new{player=new{position=player.transform.position,forward=player.transform.forward},map=map.position,
   roads=map.Find("Roads").Cast<Transform>().Take(10).Select(t=>new{t.name,position=t.position,rotation=t.eulerAngles,scale=t.localScale,bounds=t.GetComponentInChildren<Renderer>()?.bounds}).ToArray(),
   props=map.Find("Props").Cast<Transform>().Where(t=>t.name.Contains("signal")||t.name.Contains("Bucket")||Vector3.Distance(t.position,player.transform.position)<60).Select(t=>new{t.name,position=t.position,scale=t.localScale,bounds=t.GetComponentInChildren<Renderer>()?.bounds}).ToArray(),
   enemies=map.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true).OrderBy(t=>Vector3.Distance(t.transform.position,player.transform.position)).Take(8).Select(e=>new{e.name,position=e.transform.position,e.EventMode,e.HideWhileWaiting,target=e.TargetPoint!=null?e.TargetPoint.position:Vector3.zero}).ToArray()};
  var report=new Report{player=player.transform.position,forward=player.transform.forward,roads=map.Find("Roads").Cast<Transform>().Take(12).Select(Read).ToArray(),props=map.Find("Props").Cast<Transform>().Where(t=>t.name.ToLowerInvariant().Contains("signal")||t.name.Contains("Bucket")||Vector3.Distance(t.position,player.transform.position)<75).Select(Read).ToArray(),enemies=map.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true).OrderBy(t=>Vector3.Distance(t.transform.position,player.transform.position)).Take(12).Select(e=>{var n=Read(e.transform);n.mode=e.EventMode.ToString();n.target=e.TargetPoint!=null?e.TargetPoint.position:Vector3.zero;return n;}).ToArray()};
  string Format(Node n)=>n.name+"\t"+n.position.ToString("F2")+"\t"+n.rotation.ToString("F2")+"\t"+n.scale.ToString("F2")+"\t"+n.boundsCenter.ToString("F2")+"\t"+n.boundsSize.ToString("F2")+"\t"+n.mode+"\t"+n.target.ToString("F2");
  var lines=new[]{"PLAYER\t"+report.player.ToString("F2")+"\t"+report.forward.ToString("F2"),"ROADS"}.Concat(report.roads.Select(Format)).Concat(new[]{"PROPS"}).Concat(report.props.Select(Format)).Concat(new[]{"ENEMIES"}).Concat(report.enemies.Select(Format));
  System.IO.File.WriteAllLines("map-concepts/harbor-opening-refinement-2026-09-16/opening-layout.tsv",lines);return "map-concepts/harbor-opening-refinement-2026-09-16/opening-layout.tsv";
 }
}
