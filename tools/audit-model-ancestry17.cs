using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;
public static class AuditModelAncestry17 {
 static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
 static object[] Ancestors(Transform t){var rows=new List<object>();for(;t!=null;t=t.parent)rows.Add(new{t.name,t.gameObject.activeSelf,t.gameObject.activeInHierarchy});return rows.ToArray();}
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Idle only");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Dirty scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();var result=new List<object>();
  try {foreach(string name in new[]{"Jamsil","ShoeTower"}){
   var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");var roots=scene.GetRootGameObjects();var director=roots.SelectMany(g=>g.GetComponentsInChildren<Chapter45Director>(true)).Single();
   var meshes=roots.SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Where(m=>m.sharedMesh!=null&&AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith(".fbx",StringComparison.OrdinalIgnoreCase));
   var rows=meshes.Select(m=>{var role=m.GetComponentInParent<Chapter45RoleAction>(true);var enemy=m.GetComponentInParent<EnemyScript_space>(true);return new{asset=AssetDatabase.GetAssetPath(m.sharedMesh),path=Path(m.transform),m.gameObject.activeSelf,m.gameObject.activeInHierarchy,ancestors=Ancestors(m.transform),role=role==null?null:role.role.ToString(),registeredActor=enemy!=null&&director.encounters.Any(e=>e.actors.Contains(enemy)),underRegisteredHeldProp=role!=null&&role.heldProp!=null&&(m.transform==role.heldProp||m.transform.IsChildOf(role.heldProp))};}).ToArray();result.Add(new{scene=name,rows});
  }} finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
  return new{utc=DateTime.UtcNow.ToString("o"),readOnly=true,savedScenes=result};
 }
}
