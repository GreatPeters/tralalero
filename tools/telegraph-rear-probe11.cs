using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using IndianOceanAssets.ShooterSurvival;
public static class TelegraphRearProbe11 {
 static Chapter45Director d;static Chapter45RoleAction r;static PlayerScript p;static string folder;static bool active,placed;static int before,frame;static float time;static object placement;
 static string Json(object x)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{x});
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 public static object Begin(string roleName,string output){d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();p=d.Player;r=d.encounters.Where(e=>e.floor==d.CurrentFloor&&!e.Complete).SelectMany(e=>e.actors).Select(a=>a.GetComponent<Chapter45RoleAction>()).First(a=>a!=null&&a.role.ToString()==roleName);folder=output;active=true;placed=false;EditorApplication.update+=Tick;return new{attached=true};}
 static void Tick(){if(!active||!EditorApplication.isPlaying)return;
  if(!placed&&r.Phase==Chapter45RoleAction.ActionPhase.Acting&&r.Contacts==0){before=r.Contacts;time=d.Elapsed;frame=Time.frameCount;Vector3 target=r.transform.position-r.transform.forward*.98f;target.y=p.transform.position.y;p.transform.position=target;var body=p.GetComponent<Rigidbody>();if(body!=null)body.position=target;Physics.SyncTransforms();placement=new{player=V(target),actor=V(r.transform.position),forward=V(r.transform.forward),vertices=r.warningMarker.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>V(r.warningMarker.TransformPoint(v))).ToArray(),hp=p.currentHealth,contacts=r.Contacts,distanceBehind=.98f};placed=true;}
  if(!placed||Time.frameCount<frame+3)return;active=false;EditorApplication.update-=Tick;File.WriteAllText(Path.Combine(folder,"rear-probe.json"),Json(new{role=r.role.ToString(),placement,after=new{elapsed=d.Elapsed-time,contacts=r.Contacts,hp=p.currentHealth,phase=r.Phase.ToString(),player=V(p.transform.position)},actualNativeContact=r.Contacts>before,manualPlayerPlacementAtFirstActing=true,ordinaryRouteSample=false,manualDamageOrContactOrTick=false}));
 }
 public static object Stop(){active=false;EditorApplication.update-=Tick;return new{stopped=true};}
}
