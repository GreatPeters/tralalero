using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using TMPro;
public static class RepairDetailedMall {
 public static object Main(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Preserve dirty scene");var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();if(scene.name!="ShoeTower")throw new Exception("ShoeTower required");
  d.useExplicitRegistrations=true;d.targets=Array.Empty<Chapter45Target>();d.hazards=Array.Empty<Chapter45Hazard>();var root=d.transform.Find("DetailedMall_20261003");if(root.Find("Architectural enclosure revision")!=null)throw new Exception("Already repaired");new GameObject("Architectural enclosure revision").transform.SetParent(root,false);
  var assets="Assets/ShooterSurvival/Models/Chapters/Chapters45/DetailedMall-20261003T092859667/";var stone=AssetDatabase.LoadAssetAtPath<Material>(assets+"Porcelain.mat");var frame=AssetDatabase.LoadAssetAtPath<Material>(assets+"Satin champagne metal.mat");var glass=AssetDatabase.LoadAssetAtPath<Material>(assets+"Clear blue glass.mat");
  int signs=0,wallPanels=0;foreach(var floor in root.Find("Scenery").Cast<Transform>().Where(x=>x.GetComponent<Chapter45SceneryGroup>()?.floor>=0)){
   int f=floor.GetComponent<Chapter45SceneryGroup>().floor;
   foreach(var shop in floor.Cast<Transform>().Where(x=>x.name.Contains("storefront"))){shop.localPosition=shop.localPosition.normalized*41;foreach(var t in shop.GetComponentsInChildren<TMP_Text>(true)){t.transform.localRotation=Quaternion.Euler(0,180,0);signs++;}}
   if(f==6)continue;
   for(int i=0;i<72;i++){float angle=i*5*Mathf.Deg2Rad;var p=new Vector3(Mathf.Cos(angle)*49,6,Mathf.Sin(angle)*49);var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.name="Enclosing clerestory wall";panel.transform.SetParent(floor,false);panel.transform.localPosition=p;panel.transform.localRotation=Quaternion.Euler(0,-i*5,0);panel.transform.localScale=new Vector3(4.36f,12,.22f);panel.GetComponent<Renderer>().sharedMaterial=i%3==0?frame:stone;UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());wallPanels++;
    var window=GameObject.CreatePrimitive(PrimitiveType.Cube);window.name="Upper daylight glazing";window.transform.SetParent(floor,false);window.transform.localPosition=new Vector3(p.x*.995f,8.4f,p.z*.995f);window.transform.localRotation=panel.transform.localRotation;window.transform.localScale=new Vector3(3.55f,3,.05f);window.GetComponent<Renderer>().sharedMaterial=glass;UnityEngine.Object.DestroyImmediate(window.GetComponent<Collider>());
   }
  }
  // Physical support follows each inclined visible tread; only new geometry is touched.
  int support=0;foreach(var lift in d.lifts){if(lift.escalator){foreach(var t in lift.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="Metal escalator tread")){if(t.GetComponent<BoxCollider>()==null)t.gameObject.AddComponent<BoxCollider>();support++;}}else{var p=lift.platform.Find("Elevator visible cabin floor");if(p!=null&&p.GetComponent<BoxCollider>()==null)p.gameObject.AddComponent<BoxCollider>();}}
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");return new{explicitRegistry=d.useExplicitRegistrations,targets=d.targets.Length,hazards=d.hazards.Length,signs,wallPanels,escalatorSupports=support};
 }
}
