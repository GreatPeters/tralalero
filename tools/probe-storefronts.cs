using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using Object=UnityEngine.Object;
public static class ProbeStorefronts {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();try{
   EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity");var d=Object.FindFirstObjectByType<Chapter45Director>();
   var rows=d.transform.Find("Scenery/Jamsil_ReferenceStreet_20261002").GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Shop bay ")).Select((t,i)=>{
    float best=float.MaxValue,station=0,nearest=0;foreach(var s in d.route.segments){var delta=s.end-s.start;float f=Mathf.Clamp01(Vector3.Dot(t.position-s.start,delta)/delta.sqrMagnitude);float dist=(t.position-Vector3.Lerp(s.start,s.end,f)).sqrMagnitude;if(dist<best){best=dist;nearest=station+s.Length*f;}station+=s.Length;}
    return new{index=i,name=t.name,parent=t.parent.name,station=nearest,position=t.position.ToString("R"),yaw=t.eulerAngles.y};
   }).ToArray();return rows.Where(r=>r.station>=200&&r.station<=450).ToArray();
  }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
}
