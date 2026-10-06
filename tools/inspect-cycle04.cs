using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class InspectCycle04 {
 static float[] V(Vector3 x)=>new[]{x.x,x.y,x.z};
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Idle clean editor required");
  string previous=SceneManager.GetActiveScene().path;
  try{
   EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity");var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();
   return new {routeLength=d.route.Length,phones=d.encounters.SelectMany(e=>e.actors).Select(a=>a.GetComponent<Chapter45RoleAction>()).Where(r=>r!=null&&r.role==Chapter45RoleAction.Role.PhoneShopper).Select(r=>{
    var a=r.GetComponentInChildren<Animator>(true);var bind=r.heldProp.GetComponent<Chapter45HandProps>();return new {r.name,encounter=r.encounter.name,r.phaseOffset,r.moveSpeed,r.actionDamage,position=V(r.transform.position),r.encounter.activationDistance,r.encounter.stopDistance,controller=a.runtimeAnimatorController.name,clips=a.runtimeAnimatorController.animationClips.Select(c=>new {c.name,c.length}).ToArray(),bind.useLeftHand,prop=V(r.heldProp.position),bones=a.GetComponentsInChildren<Transform>(true).Where(t=>new[]{"Head","Neck","RightArm","RightForeArm","RightHand","LeftArm","LeftForeArm","LeftHand"}.Contains(t.name)).Select(t=>new {t.name,parent=t.parent.name,position=V(t.position)}).ToArray(),colliders=r.GetComponents<Collider>().Select(c=>EditorJsonUtility.ToJson(c)).ToArray()};
   }).ToArray()};
  }finally{EditorSceneManager.OpenScene(previous);}
 }
}
