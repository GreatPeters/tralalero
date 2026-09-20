using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
public static class InstallSyncedCrateThrow
{
 const string Prefab="Assets/JH/Model/Prefab/Enemy_FatMan.prefab";
 const string MaterialPath="Assets/ShooterSurvival/Materials/Generated/FatMan_StableSurface.mat";
 public static string Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  var scene=SceneManager.GetActiveScene();if(scene.name!="Noryangjin_MapTool_Mode_SR18"||scene.isDirty)throw new Exception("Clean saved SR18 required");
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterial;
  // Always derive from the original skin, never from a previous candidate.
  source=AssetDatabase.LoadAssetAtPath<Material>("Assets/JH/Model/Enemy/Fat_Throw/던짐_스킨/Material.001.mat");
  var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
  if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,MaterialPath);}else material.CopyPropertiesFromMaterial(source);
  int queue=source.rawRenderQueue;material.shader=Shader.Find("FlatKit/Stylized Surface");material.renderQueue=queue;
  material.SetOverrideTag("RenderType","Opaque");material.SetFloat("_OutlineEnabled",0);material.DisableKeyword("DR_OUTLINE_ON");material.SetShaderPassEnabled("SRPDEFAULTUNLIT",false);EditorUtility.SetDirty(material);
  var prefab=PrefabUtility.LoadPrefabContents(Prefab);
  try{Configure(prefab,material);PrefabUtility.SaveAsPrefabAsset(prefab,Prefab);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  var actors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EnemyEventController>(true)).Where(e=>e.name.Contains("FatMan")).ToArray();
  foreach(var actor in actors)Configure(actor.gameObject,material);
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();
  return "Saved stable reference pose, current FlatKit surface and synchronized throw on "+actors.Length+" actors";
 }
 static void Configure(GameObject go,Material material)
 {
  var pose=go.GetComponent<FatManCratePose>();var model=go.GetComponentInChildren<Animator>(true);
  var prop=go.GetComponentsInChildren<SimpleProjectile>(true).Single(p=>p.name=="Arrow2").transform;
  var transforms=model.GetComponentsInChildren<Transform>(true).Select(t=>(t,p:t.localPosition,q:t.localRotation,s:t.localScale)).ToArray();
  pose.Configure(model,prop);
  var idle=model.runtimeAnimatorController.animationClips.First(c=>c.name=="Fatman Idle");
  idle.SampleAnimation(model.gameObject,0);pose.CaptureReferencePose();
  foreach(var entry in transforms){entry.t.SetLocalPositionAndRotation(entry.p,entry.q);entry.t.localScale=entry.s;}
  pose.Configure(model,prop);
  foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
  {skin.sharedMaterial=material;Record(skin);}
  Record(pose);Record(prop);
 }
 static void Record(UnityEngine.Object value){EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
}
