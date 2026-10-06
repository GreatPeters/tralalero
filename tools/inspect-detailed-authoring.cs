using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;
public static class InspectDetailedAuthoring {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Idle required");
  var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();
  return new{scene=SceneManager.GetActiveScene().name,roots=SceneManager.GetActiveScene().GetRootGameObjects().Select(x=>new{x.name,active=x.activeSelf,children=x.transform.Cast<Transform>().Select(y=>new{y.name,active=y.gameObject.activeSelf,children=y.childCount}).ToArray()}).ToArray(),director=d.name,route=d.route.segments.Select(s=>new{s.floor,s.label,s.length,start=new[]{s.start.x,s.start.y,s.start.z},end=new[]{s.end.x,s.end.y,s.end.z}}).ToArray(),arrays=new{encounters=d.encounters.Length,choices=d.choices.Length,targets=d.targets.Length,hazards=d.hazards.Length,lifts=d.lifts.Length,goals=d.goals.Length},actors=d.encounters.Take(3).SelectMany(e=>e.actors).Select(e=>new{e.name,position=new[]{e.transform.position.x,e.transform.position.y,e.transform.position.z},events=e.GetComponent<EnemyEventController>()?.EventMode.ToString(),animators=e.GetComponentsInChildren<Animator>(true).Select(a=>new{controller=AssetDatabase.GetAssetPath(a.runtimeAnimatorController),avatar=AssetDatabase.GetAssetPath(a.avatar),a.applyRootMotion}).ToArray()}).ToArray()};
 }
}
