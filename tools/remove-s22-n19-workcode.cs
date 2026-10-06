using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class S22BasinLabel
{
 const string Source="Assets/ShooterSurvival/Models/MeshyRestStop20260925/N19_live_fish_tub/N19_live_fish_tub.fbx";
 const string Target="Assets/ShooterSurvival/Models/Generated/S22Polish/Props/LiveFishTubBlankLabel.asset";
 static bool InLabelIsland(Vector2 p)=>(p.x>=.34f&&p.x<=.43f&&p.y>=.855f&&p.y<=.95f)||(p.x>=.575f&&p.x<=.81f&&p.y>=.38f&&p.y<=.575f)||(p.x>=.60f&&p.x<=.75f&&p.y>=.075f&&p.y<=.19f);
 static bool White(Texture2D t,Vector2 p){var c=t.GetPixelBilinear(p.x,p.y);return Mathf.Min(c.r,c.g,c.b)>.7f&&Mathf.Max(c.r,c.g,c.b)-Mathf.Min(c.r,c.g,c.b)<.18f;}
 public static object Main(){if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");var replacement=AssetDatabase.LoadAssetAtPath<Mesh>(Target);int seeds=0,blanked=0;
  if(replacement==null){var importer=(ModelImporter)AssetImporter.GetAtPath(Source);bool readable=importer.isReadable;string backup="outputs/s22-polish-2026-10-01/before/"+Source+".meta";Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(Source+".meta",backup);
   try{importer.isReadable=true;importer.SaveAndReimport();var mesh=AssetDatabase.LoadAssetAtPath<GameObject>(Source).GetComponentInChildren<MeshFilter>().sharedMesh;if(mesh.subMeshCount!=1)throw new Exception("Unexpected basin material topology");var vertices=mesh.vertices;var uv=mesh.uv;var normals=mesh.normals;var tangents=mesh.tangents;var triangles=mesh.triangles;var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(Path.GetDirectoryName(Source)+"/texture_0.png"));var region=new Bounds();bool first=true;
    for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];if(!InLabelIsland(uv[a])||!InLabelIsland(uv[b])||!InLabelIsland(uv[c]))continue;var samples=new[]{uv[a],uv[b],uv[c],(uv[a]+uv[b]+uv[c])/3,(uv[a]+uv[b])*.5f,(uv[b]+uv[c])*.5f,(uv[c]+uv[a])*.5f};if(samples.Count(p=>White(image,p))<4)continue;seeds++;foreach(int v in new[]{a,b,c}){if(first){region=new Bounds(vertices[v],Vector3.zero);first=false;}else region.Encapsulate(vertices[v]);}}
    if(seeds<10||seeds>80||region.size.x*region.size.y*region.size.z>mesh.bounds.size.x*mesh.bounds.size.y*mesh.bounds.size.z*.03f)throw new Exception("Ambiguous label selection: "+seeds);
    var whiteUv=new Vector2(.38f,.92f);if(!White(image,whiteUv))throw new Exception("Blank UV sample must be white");Object.DestroyImmediate(image);region.Expand(region.size*.18f);
    var vv=vertices.ToList();var uu=uv.ToList();var nn=normals.ToList();var tt=tangents.ToList();
    for(int i=0;i<triangles.Length;i+=3){var center=(vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3;if(!region.Contains(center))continue;blanked++;for(int j=0;j<3;j++){int old=triangles[i+j];triangles[i+j]=vv.Count;vv.Add(vertices[old]);uu.Add(whiteUv);nn.Add(normals[old]);if(tangents.Length==vertices.Length)tt.Add(tangents[old]);}}
    replacement=new Mesh{name="Live fish tub - blank auction label"};replacement.SetVertices(vv);replacement.SetUVs(0,uu);replacement.SetNormals(nn);if(tt.Count==vv.Count)replacement.SetTangents(tt);replacement.triangles=triangles;replacement.RecalculateBounds();AssetDatabase.CreateAsset(replacement,Target);
   }finally{importer.isReadable=readable;importer.SaveAndReimport();}
  }
  var setup=EditorSceneManager.GetSceneManagerSetup();int count=0;try{foreach(var name in new[]{"Noryangjin_MapTool_Mode_SR18_Revamp","HighWay","RestStop"}){var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");bool dirty=false;foreach(var f in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)))if(f.sharedMesh!=null&&AssetDatabase.GetAssetPath(f.sharedMesh)==Source){f.sharedMesh=replacement;EditorUtility.SetDirty(f);count++;dirty=true;}if(dirty){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}}}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  File.WriteAllText("outputs/s22-polish-2026-10-01/basin-label.txt",$"white seed triangles={seeds}; blanked label triangles={blanked}; replaced instances={count}; texture unchanged; geometry/indices retained except UV seam duplication");return new{seeds,blanked,instances=count};
 }
}
