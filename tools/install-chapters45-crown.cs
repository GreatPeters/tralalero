// Final pass dependency: after any rebuild here, run RepairCrownInteriorReadability.Main and PolishTowerFinalText.Main; verify native portrait views before acceptance.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
public static class InstallChapters45Crown
{
 const string AssetsRoot="Assets/ShooterSurvival/Models/Chapters/Chapters45/Crown";
 static Material ceramic,glass,brass;
 static Material Material(string name,Color color){string p=AssetsRoot+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.35f);m.enableInstancing=true;return m;}
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve dirty scene");
  Directory.CreateDirectory(AssetsRoot);
  foreach(int i in new[]{0,1,2}){string p=AssetsRoot+"/ShoeCrown_LOD"+i+".fbx";if(!File.Exists(p))File.Copy("outputs/chapters45-2026-10-02/assets/shoe-crown/local-refinement-v2/ShoeCrown_LOD"+i+".fbx",p);AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);var imp=(ModelImporter)AssetImporter.GetAtPath(p);imp.importAnimation=false;imp.isReadable=false;imp.addCollider=false;imp.SaveAndReimport();}
  ceramic=Material("OffwhiteCeramic",new Color(.92f,.93f,.89f));glass=Material("BlueGrayGlazing",new Color(.17f,.4f,.51f));brass=Material("ChampagneFrames",new Color(.73f,.62f,.43f));
  var setup=EditorSceneManager.GetSceneManagerSetup();
  try
  {
   foreach(string name in new[]{"Jamsil","ShoeTower"})
   {
    var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
    if(name=="Jamsil")
    {
     var anchors=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene&&t.name=="GeneratedCrownAnchor").ToArray();
     var visible=anchors.Where(t=>t.gameObject.activeInHierarchy&&t.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy)).ToArray();
     var anchor=visible.Length==1?visible[0]:anchors.Length==1&&anchors[0].GetComponentsInChildren<Renderer>(true).Length==0?anchors[0]:throw new InvalidOperationException("Expected one renderable city crown, or one empty first-install anchor.");
     float length=84;Vector3 placement=anchor.position;var oldLod=anchor.Find("Crown_LOD0");
     if(oldLod!=null){var rs=oldLod.GetComponentsInChildren<Renderer>(true);if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);length=Mathf.Max(b.size.x,b.size.z);placement=new Vector3(b.center.x,b.min.y,b.center.z);}}
     Crown(anchor,length,0,false,placement);
    }
    else
    {
     var exhibit=GameObject.Find("InsideTheSneaker").transform;
     var director=Object.FindFirstObjectByType<Chapter45Director>();var last=director.route.segments.Last();var forward=(last.end-last.start).normalized;
     var anchor=exhibit.Find("CrownShell");
     if(anchor==null){anchor=new GameObject("CrownShell").transform;anchor.SetParent(exhibit,false);anchor.position=last.end-forward*39-Vector3.up*2.3f;}
     Crown(anchor,91,-90,true);
     var goal=Object.FindFirstObjectByType<Chapter45Goal>();
     var offerings=goal.transform.Cast<Transform>().Where(t=>t.name=="WhiteShoeOffering").ToArray();
     var collect=offerings.FirstOrDefault();
     if(collect==null){collect=new GameObject("WhiteShoeOffering").transform;collect.SetParent(goal.transform,false);collect.position=goal.transform.position+Vector3.up*.5f-forward*4;}
     foreach(var duplicate in offerings.Skip(1))Object.DestroyImmediate(duplicate.gameObject);
     Crown(collect,2.7f,-90,false);goal.collectionVisual=collect;
    }
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
  }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  AssetDatabase.SaveAssets();return new{installed=true,source="locally rebuilt from failed Trellis / independent revision2 acceptance",levels=new[]{11139,5999,1999}};
 }
 static void Crown(Transform anchor,float length,float yaw,bool cutaway,Vector3? placement=null)
 {
  // Preserve scene placement (including folded routes), replace only generated LODs.
  foreach(var old in anchor.Cast<Transform>().Where(t=>t.name.StartsWith("Crown_LOD")).ToArray())Object.DestroyImmediate(old.gameObject);
  var lods=new List<LOD>();
  for(int i=0;i<3;i++)
  {
   var src=AssetDatabase.LoadAssetAtPath<GameObject>(AssetsRoot+"/ShoeCrown_LOD"+i+".fbx");var go=(GameObject)PrefabUtility.InstantiatePrefab(src);go.transform.SetParent(anchor,false);go.name="Crown_LOD"+i;
   go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(0,yaw,0)*src.transform.localRotation;go.transform.localScale=src.transform.localScale;
   var renders=go.GetComponentsInChildren<Renderer>(true);
   Bounds WorldBounds(){var b=renders[0].bounds;foreach(var r in renders)b.Encapsulate(r.bounds);return b;}
   var bounds=WorldBounds();go.transform.localScale*=length/Mathf.Max(bounds.size.x,bounds.size.z);bounds=WorldBounds();
   Vector3 destination=placement??anchor.position;
   go.transform.position+=new Vector3(destination.x-bounds.center.x,destination.y-bounds.min.y,destination.z-bounds.center.z);
   foreach(var r in renders){r.sharedMaterials=r.sharedMaterials.Select(m=>m.name.Contains("Glazing")?glass:m.name.Contains("Frames")?brass:ceramic).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;}
   if(cutaway)
   {
    foreach(var filter in go.GetComponentsInChildren<MeshFilter>())
    {
     var mesh=Object.Instantiate(filter.sharedMesh);var v=mesh.vertices;float floor=anchor.position.y+2.3f;
     for(int sub=0;sub<mesh.subMeshCount;sub++){var tri=mesh.GetTriangles(sub);var kept=new List<int>();for(int k=0;k<tri.Length;k+=3){var a=filter.transform.TransformPoint(v[tri[k]]);var b=filter.transform.TransformPoint(v[tri[k+1]]);var c=filter.transform.TransformPoint(v[tri[k+2]]);var centroid=(a+b+c)/3;if(centroid.y>floor+6.5f||centroid.y>floor+.08f&&Mathf.Abs(centroid.x-anchor.position.x)<8.5f)continue;kept.Add(tri[k]);kept.Add(tri[k+1]);kept.Add(tri[k+2]);}mesh.SetTriangles(kept,sub);}
     mesh.RecalculateBounds();string p=AssetsRoot+"/WorldCutaway_"+i+"_"+filter.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(existing==null)AssetDatabase.CreateAsset(mesh,p);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}filter.sharedMesh=mesh;
    }
   }
   lods.Add(new LOD(i==0?.22f:i==1?.08f:.001f,renders));
  }
  var group=anchor.GetComponent<LODGroup>();if(group==null)group=anchor.gameObject.AddComponent<LODGroup>();group.SetLODs(lods.ToArray());group.RecalculateBounds();
 }
}
