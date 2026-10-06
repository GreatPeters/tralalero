using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class RepairEscalatorFloor {
 public static object Main(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");var scene=SceneManager.GetActiveScene();if(scene.name!="ShoeTower"||scene.isDirty)throw new Exception("Clean ShoeTower required");var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();var applied=new List<object>();
  foreach(var lift in d.lifts.Where(x=>x.escalator)){
   int floor=d.route.segments[lift.Destination(lift.afterSegment)].floor;var group=d.transform.Find("DetailedMall_20261003/Scenery").Cast<Transform>().Single(x=>x.GetComponent<Chapter45SceneryGroup>()?.floor==floor);
   var slab=group.Find(floor+"F curved supported floor");var old=slab.GetComponent<MeshFilter>().sharedMesh;var path=AssetDatabase.GetAssetPath(old).Replace(".asset","-escalator-opening.asset");if(AssetDatabase.LoadAssetAtPath<Mesh>(path)!=null)throw new Exception("Already applied");
   Vector3 a=group.InverseTransformPoint(d.route.segments[lift.afterSegment].end),b=group.InverseTransformPoint(d.route.segments[lift.Destination(lift.afterSegment)].start);a.y=b.y=0;Vector3 ab=b-a;float lengthSquared=ab.sqrMagnitude;
   var vertices=new List<Vector3>();var triangles=new List<int>();int cellsRemoved=0;
   bool Opening(Vector3 p){float t=Vector3.Dot(p-a,ab)/lengthSquared;return t>=-.05f&&t<1f&&(p-(a+ab*t)).sqrMagnitude<3.6f*3.6f;}
   Vector3 P(float radius,float angle){float v=angle*Mathf.Deg2Rad;return new Vector3(Mathf.Cos(v)*radius,0,Mathf.Sin(v)*radius);}
   for(int ang=0;ang<180;ang++)for(int radial=0;radial<37;radial++){
    float lo=Mathf.Lerp(19.5f,56,radial/37f),hi=Mathf.Lerp(19.5f,56,(radial+1)/37f);Vector3 p0=P(lo,ang*2),p1=P(lo,(ang+1)*2),p2=P(hi,ang*2),p3=P(hi,(ang+1)*2);
    if(Opening((p0+p1+p2+p3)*.25f)){cellsRemoved++;continue;}int i=vertices.Count;vertices.AddRange(new[]{p0,p1,p2,p3});triangles.AddRange(new[]{i,i+1,i+2,i+2,i+1,i+3});
   }
   var mesh=new Mesh{name=floor+"F floor with real escalator opening"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);AssetDatabase.SaveAssetIfDirty(mesh);slab.GetComponent<MeshFilter>().sharedMesh=mesh;slab.GetComponent<MeshCollider>().sharedMesh=mesh;
   // Level landing fills only the final tread/upper-route seam.
   var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="Upper escalator landing";platform.transform.SetParent(group,false);var node=d.route.segments[lift.Destination(lift.afterSegment)];var forward=(node.end-node.start).normalized;platform.transform.position=node.start+forward*.75f-Vector3.up*.05f;platform.transform.rotation=Quaternion.LookRotation(forward);platform.transform.localScale=new Vector3(8,.1f,1.8f);platform.GetComponent<Renderer>().sharedMaterial=slab.GetComponent<Renderer>().sharedMaterial;
   applied.Add(new{floor,cellsRemoved,triangles=triangles.Count/3,sourceMeshRetained=AssetDatabase.GetAssetPath(old),newMesh=path});
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return applied.ToArray();
 }
}
