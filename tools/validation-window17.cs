using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using IndianOceanAssets.ShooterSurvival;
// Short native capture after an explicitly directed starting pose. No manual
// hazard Tick/Hit, damage, win, stat override, or reward call. Saves no scene.
public static class ValidationWindow17 {
 static Chapter45Director d;static PlayerScript p;static string folder;static float begin,next,end;static int frame,index;static bool active;static readonly List<object> frames=new();static readonly List<string> errors=new();
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static string Json(object x)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{x});
 static void Set(object o,string key,object value)=>o.GetType().GetField("<"+key+">k__BackingField",F).SetValue(o,value);
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 public static object Begin(float distance,float lane,float seconds,int branch,string output){
  if(!EditorApplication.isPlaying||active)throw new Exception("Fresh Play lobby required");
  if(Directory.Exists(output))throw new Exception("Preserve existing frames");Directory.CreateDirectory(output);folder=output;
  d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();p=d.Player;
  UnityEngine.Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
  var harness=UnityEngine.Object.FindFirstObjectByType<CombatHarness>();if(harness!=null)typeof(CombatHarness).GetField("overlayVisible",F).SetValue(harness,false);
  UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale=UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.Locales.Single(l=>l.Identifier.Code=="en-US");
  var retired=new List<string>();
  foreach(var e in d.encounters)if(e.stopDistance<distance){e.Retire();Set(e,"Complete",true);retired.Add(e.name);}
  foreach(var h in d.hazards)if(h.distance<distance){Set(h,"Triggered",true);h.Cancel();}
  foreach(var choice in d.choices)if(choice.distance<distance){
   int choiceSegment=Array.FindIndex(d.route.segments,s=>s.floor==choice.floor);if(choiceSegment>=0)Set(d,"CurrentSegment",choiceSegment);
   choice.Commit(branch==0?-3:3);
  }
  Set(d,"Distance",distance);Set(d,"CurrentSegment",d.route.SegmentAt(distance));
  d.SampleOnCurrentDeck(distance,out var center,out var forward);p.ApplyContinuousRoutePose(center+Vector3.up*d.footOffset,forward,lane);Physics.SyncTransforms();
  typeof(Chapter45Director).GetMethod("RefreshVisibility",F).Invoke(d,null);
  begin=d.Elapsed;next=0;end=seconds;frame=-1;index=0;frames.Clear();errors.Clear();active=true;
  EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived+=Log;
  File.WriteAllText(Path.Combine(folder,"conditions.json"),Json(new{directedStartingPose=true,ordinaryPlaythrough=false,distance,lane,seconds,health=p.currentHealth,attack=p.ResolvedAttackDamage,gameplayClock="native Update/FixedUpdate 1x",manualDamage=false,manualHazardTick=false,manualHit=false,retiredEarlierEncounters=retired.ToArray(),upcomingActorsDisabled=false,shootingEnabled=p.canShoot}));
  return new{started=true,folder};
 }
 static void Log(string msg,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(msg);}
 static void Tick(){
  if(!active||!EditorApplication.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;float t=d.Elapsed-begin;
  if(t>=next){next=t+.25f;string file=(index++).ToString("D3")+".png";ScreenCapture.CaptureScreenshot(Path.Combine(folder,file));
   frames.Add(new{projectiles=UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Where(x=>x.name.StartsWith("PopcornThrower active projectile")).Select(x=>new{id=x.GetInstanceID(),position=V(x.transform.position),scale=V(x.transform.lossyScale)}).ToArray(),file,t,d.Distance,d.CurrentFloor,lane=d.Lane,health=p.currentHealth,player=V(p.transform.position),camera=Camera.main==null?null:new{position=V(Camera.main.transform.position),forward=V(Camera.main.transform.forward)},targets=d.targets.Select(x=>new{x.name,x.Health,x.Open,colliderMin=x.hitCollider==null?null:V(x.hitCollider.bounds.min),colliderMax=x.hitCollider==null?null:V(x.hitCollider.bounds.max)}).ToArray(),weapons=p.GetComponentsInChildren<WeaponScript>().Select(w=>new{w.TotalProjectilesSpawned,spawn=V(w.LastProjectileSpawnPosition)}).ToArray(),hazards=d.hazards.Where(h=>h.Triggered&&!h.Finished).Select(h=>new{h.name,h.Triggered,h.Active,h.Contacts,body=h.body==null?null:V(h.body.position),footprint=h.footprint!=null&&h.footprint.activeInHierarchy,renderers=h.body==null?null:h.body.GetComponentsInChildren<Renderer>(true).Select(RenderInfo).ToArray()}).ToArray(),roles=d.encounters.Where(e=>e.Activated&&!e.Complete&&e.floor==d.CurrentFloor).SelectMany(e=>e.actors).Where(a=>a!=null&&a.gameObject.activeInHierarchy&&!a.IsDead).Select(a=>new{a.name,position=V(a.transform.position),role=a.GetComponent<Chapter45RoleAction>()?.role.ToString(),phase=a.GetComponent<Chapter45RoleAction>()?.Phase.ToString(),actions=a.GetComponent<Chapter45RoleAction>()?.Actions}).ToArray()});
  }
  if(t<end&&p.currentHealth>0)return;active=false;EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
  File.WriteAllText(Path.Combine(folder,"result.json"),Json(new{complete=true,elapsed=t,hp=p.currentHealth,frames=frames.ToArray(),errors=errors.ToArray(),timeline=d.Timeline.ToArray(),manualDamage=false,manualHazardTick=false,manualHit=false}));
 }
 public static object Stop(){active=false;EditorApplication.update-=Tick;Application.logMessageReceived-=Log;return new{stopped=true};}
 static object RenderInfo(Renderer r)=>new{r.name,r.enabled,r.isVisible,r.forceRenderingOff,r.gameObject.activeInHierarchy,layer=r.gameObject.layer,shadow=r.shadowCastingMode.ToString(),boundsMin=V(r.bounds.min),boundsMax=V(r.bounds.max),scale=V(r.transform.lossyScale),screenCenter=Camera.main==null?null:V(Camera.main.WorldToScreenPoint(r.bounds.center)),materials=r.sharedMaterials.Select(m=>m==null?null:new{m.name,shader=m.shader.name,color=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():"unknown"}).ToArray()};
}
