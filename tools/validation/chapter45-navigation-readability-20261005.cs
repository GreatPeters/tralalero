using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
// Focused native regression checks. No production scene, preferences, assets or
// global test fixtures are modified. Temporary objects live in a preview scene.
public static class NavigationRegression25 {
 static readonly List<object> rows=new();
 static void Require(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Check(string name,Action run){try{run();rows.Add(new{name,passed=true,error=""});}catch(Exception e){rows.Add(new{name,passed=false,error=e.Message});}}
 static Chapter45Route.Segment S(Vector3 a,Vector3 b,int floor=1)=>new(){start=a,end=b,floor=floor};
 static string Json(object o)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
 public static object Main(string receipt){
  if(EditorApplication.isPlayingOrWillChangePlaymode||File.Exists(receipt))throw new Exception("Fresh Edit check only");
  var preview=EditorSceneManager.NewPreviewScene();var go=new GameObject("Temporary route regression25");SceneManager.MoveGameObjectToScene(go,preview);var r=go.AddComponent<Chapter45Route>();rows.Clear();
  try {
   Action corner=()=>{r.cornerBlendDistance=4;r.segments=new[]{S(Vector3.zero,new Vector3(0,0,20)),S(new Vector3(0,0,20),new Vector3(20,0,20))};};
   Check("Corner joins position and firing direction continuously",()=>{corner();Vector3 last=Vector3.zero,previous=Vector3.forward;for(int i=0;i<=800;i++){float distance=16+i*.01f;r.Sample(distance,out var p,out var f);Require(Mathf.Abs(p.y)<.0001f&&Mathf.Abs(f.y)<.0001f,"Leaves combat plane");Require(Mathf.Abs(f.magnitude-1)<.0001f,"Non-unit aim");if(i>0){Require(Vector3.Distance(last,p)<.011f,"Position jump");Require(Vector3.Angle(previous,f)<.2f,"Aim jump");}last=p;previous=f;}r.SampleSegment(0,20,out var a,out var af);r.SampleSegment(1,20,out var b,out var bf);Require(Vector3.Distance(a,b)<.0001f&&Vector3.Angle(af,bf)<.01f,"Segment seam");});
   Check("Default zero preserves original straight segment sampling",()=>{corner();r.cornerBlendDistance=0;r.SampleSegment(0,20,out var p,out var f);Require(p==new Vector3(0,0,20)&&f==Vector3.forward,"Legacy behavior changed");});
   Check("Corners never bridge distinct floors",()=>{corner();r.segments[1].floor=2;r.SampleSegment(0,20,out var p,out var f);Require(p==new Vector3(0,0,20)&&f==Vector3.forward,"Cross-floor blend");});
   Check("Corners never bridge disconnected segments",()=>{corner();r.segments[1].start+=Vector3.right;r.SampleSegment(0,20,out var p,out var f);Require(p==new Vector3(0,0,20)&&f==Vector3.forward,"Cross-gap blend");});
   Check("Short exit legs retain stable endpoints",()=>{corner();r.segments[1].end=new Vector3(1,0,20);r.Sample(21,out var p,out var f);Require(Vector3.Distance(p,r.segments[1].end)<.0001f&&Vector3.Angle(f,Vector3.right)<.01f,"Short leg blend overruns endpoint");});
   Check("Escalator has exact endpoints and two flat comb plates",()=>{var a=new Vector3(-26,.12f,22);var b=new Vector3(-26,9.12f,4);Require(Chapter45Rules.EscalatorPosition(a,b,-1)==a&&Chapter45Rules.EscalatorPosition(a,b,2)==b,"Progress clamp");foreach(float t in new[]{0f,.05f,.1f})Require(Mathf.Abs(Chapter45Rules.EscalatorPosition(a,b,t).y-a.y)<.0001f,"Entry not flat");foreach(float t in new[]{.9f,.95f,1f})Require(Mathf.Abs(Chapter45Rules.EscalatorPosition(a,b,t).y-b.y)<.0001f,"Exit not flat");});
   Check("Escalator travel is monotonic and laterally aligned",()=>{var a=new Vector3(-26,.12f,22);var b=new Vector3(-26,9.12f,4);var last=a;for(int i=1;i<=1000;i++){var p=Chapter45Rules.EscalatorPosition(a,b,i/1000f);Require(Mathf.Abs(p.x+26)<.0001f&&p.y>=last.y&&p.z<=last.z,"Reversed or lateral travel");Require(Vector3.Distance(p,last)<.025f,"Escalator discontinuity");last=p;}});
   Check("Zero horizontal travel remains finite",()=>{var p=Chapter45Rules.EscalatorPosition(Vector3.zero,Vector3.up*9,.5f);Require(!float.IsNaN(p.y)&&!float.IsInfinity(p.y)&&Mathf.Abs(p.y-4.5f)<.001f,"Degenerate path");});
  }finally{EditorSceneManager.ClosePreviewScene(preview);}
  string result=Json(new{scope="Isolated native preview-scene regression checks",rows});File.WriteAllText(receipt,result);return result;
 }
}
