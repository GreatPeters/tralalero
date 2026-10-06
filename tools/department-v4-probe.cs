using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
public static class DepartmentV4Probe {
 public static object Main(){
  var goal=Object.FindObjectsByType<Chapter45Goal>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
  var relevant=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(m=>m.name.IndexOf("F10",StringComparison.OrdinalIgnoreCase)>=0||m.name.IndexOf("plant",StringComparison.OrdinalIgnoreCase)>=0||m.transform.IsChildOf(goal.transform));
  return new{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,goal=goal.name,
   goalVisuals=goal.GetComponentsInChildren<Renderer>(true).Select(r=>new{name=r.name,path=AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh),bounds=r.bounds.size.ToString("R"),position=r.transform.position.ToString("R"),materials=r.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()}),
   propCandidates=AssetDatabase.FindAssets("F10 t:GameObject",new[]{"Assets/ShooterSurvival"}).Select(AssetDatabase.GUIDToAssetPath).Take(16),
   nativeRelevant=relevant.Take(24).Select(m=>new{name=m.name,mesh=AssetDatabase.GetAssetPath(m.sharedMesh),triangles=m.sharedMesh!=null?m.sharedMesh.triangles.Length/3:0}),
   pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().GetProperties().Where(p=>p.Name=="additionalLightsRenderingMode"||p.Name=="maxAdditionalLightsCount").Select(p=>new{p.Name,value=p.GetValue(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline).ToString()}),
   lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(l=>new{l.name,type=l.type.ToString(),l.intensity,shadows=l.shadows.ToString(),color=l.color.ToString(),rotation=l.transform.eulerAngles.ToString()}),
   plant=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/F10.prefab").GetComponentsInChildren<MeshFilter>(true).Select(m=>new{m.name,mesh=AssetDatabase.GetAssetPath(m.sharedMesh),bounds=m.sharedMesh.bounds.size.ToString("R"),triangles=m.sharedMesh.triangles.Length/3,materials=m.GetComponent<Renderer>().sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()})};
 }
}
