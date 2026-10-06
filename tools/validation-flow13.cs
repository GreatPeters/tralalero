using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEditor;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class ValidationFlow13 {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static CanvasScript c;static PlayerScript p;static ChapterProgression cp,departing;static Chapter45Director d;static Chapter45Goal goal;
 static string folder,mode,destination;static int phase,frame,handle,coin,jewel,transCoin,transJewel,failures;static double began,at;static bool active,transitionCaptured,transitionSkip;
 static float elapsed,distance;static Vector3 position;static readonly List<object> checks=new();static readonly List<string> errors=new();static readonly List<object> transitions=new();
 static string Json(object x)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{x});
 static void Write(string n,object x)=>File.WriteAllText(Path.Combine(folder,n),Json(x));
 static float[] V(Vector3 x)=>new[]{x.x,x.y,x.z};
 static void Check(bool ok,string name,object evidence=null){checks.Add(new{pass=ok,name,evidence});Write("checks.json",checks);if(!ok){failures++;throw new Exception(name);}}
 static void Prop(object o,string n,object value)=>o.GetType().GetProperty(n).GetSetMethod(true).Invoke(o,new[]{value});
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 static void Bind(){c=Object.FindFirstObjectByType<CanvasScript>();p=Object.FindFirstObjectByType<PlayerScript>();cp=Object.FindFirstObjectByType<ChapterProgression>();d=Object.FindFirstObjectByType<Chapter45Director>();}
 static object State()=>new{scene=SceneManager.GetActiveScene().name,handle=SceneManager.GetActiveScene().handle,hp=p.currentHealth,attack=p.ResolvedAttackDamage,gameRunning=TimeManager.isGameRunning,factor=TimeManager.timeFactor,gameOver=CanvasScript.isGameOver,cp.Completed,cp.Elapsed,distance=d!=null?d.Distance:-1,directorRunning=d!=null&&d.Running,goalClaimed=d!=null&&d.GoalClaimed,lifts=d!=null?d.LiftCount:0,startArea=c.IsStartAreaActive(),coin=MoneyScript.S.Coin,jewel=MoneyScript.S.Jewel,unlocked=PlayerPrefs.GetInt("chapter_unlocked")};
 static void Inventory(string n){Write(n,new{state=State(),buttons=c.GetComponentsInChildren<Button>(true).Select(b=>new{path=PathOf(b.transform),active=b.gameObject.activeInHierarchy,b.interactable,text=b.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t=>t.text).ToArray(),events=Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Select(i=>new{method=b.onClick.GetPersistentMethodName(i),target=b.onClick.GetPersistentTarget(i)!=null?b.onClick.GetPersistentTarget(i).GetType().FullName:null}).ToArray()}).ToArray()});}
 static void Shot(string n)=>ScreenCapture.CaptureScreenshot(Path.Combine(folder,n+".png"));
 static void Next(int n){phase=n;at=EditorApplication.timeSinceStartup;Write("status.json",new{phase,elapsed=EditorApplication.timeSinceStartup-began,state=State()});}
 static Button ButtonAt(string path)=>c.transform.Find(path).GetComponent<Button>();
 static void Click(Button b,string label){
  Check(b!=null&&b.isActiveAndEnabled&&b.interactable,"Visible interactable UI: "+label);
  var rt=(RectTransform)b.transform;var root=b.GetComponentInParent<Canvas>().rootCanvas;var cam=root.renderMode==RenderMode.ScreenSpaceOverlay?null:root.worldCamera;
  var ev=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(cam,rt.TransformPoint(rt.rect.center)),button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(ev,hits);
  Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,"UI center raycast reaches: "+label,new{target=PathOf(b.transform),top=hits.Count>0?PathOf(hits[0].gameObject.transform):null});
  ExecuteEvents.Execute(b.gameObject,ev,ExecuteEvents.pointerClickHandler);
 }
 static void Lobby(string label){Check(!TimeManager.isGameRunning&&!CanvasScript.isGameOver&&!cp.Completed&&cp.Elapsed<.01f&&p.currentHealth>0&&c.IsStartAreaActive(),label+" fresh living lobby",State());if(d!=null)Check(!d.Running&&d.Elapsed==0&&d.Distance==0&&!d.GoalClaimed&&d.LiftCount==0&&d.choices.All(x=>!x.Selected&&!x.Rewarded),label+" fresh route state",State());}
 static void Start(string label){c.PlayerPressedStartButton();Check(TimeManager.isGameRunning&&TimeManager.timeFactor==1&&!CanvasScript.isGameOver&&!cp.Completed&&p.currentHealth>0,label+" existing start callback accepted",State());}
 static void BeginTransition(string next){
  destination=next;departing=cp;handle=SceneManager.GetActiveScene().handle;transitionCaptured=false;transitionSkip=false;
  Check(cp.nextScene==next,"Authored chapter destination: "+next,new{cp.chapter,cp.nextScene,movie=cp.nextChapterMovie!=null?cp.nextChapterMovie.name:null});
  c.YouWin();transCoin=MoneyScript.S.Coin;transJewel=MoneyScript.S.Jewel;
  Check(cp.Completed&&cp.IsAdvancing&&!TimeManager.isGameRunning,"Native win starts chapter transition",State());
  c.YouWin();cp.LoadNextChapter();Check(MoneyScript.S.Coin==transCoin&&MoneyScript.S.Jewel==transJewel,"Duplicate advance cannot repeat reward");
 }
 static bool TransitionArrived(){
  if(SceneManager.GetActiveScene().name==destination&&SceneManager.GetActiveScene().handle!=handle){
   Bind();if(c==null||p==null||cp==null)return false;
   Check(MoneyScript.S.Coin==transCoin&&MoneyScript.S.Jewel==transJewel,"Wallet persists across transition to "+destination,State());
   Check(!OpeningStoryUI.IsBlockingGameplay,"New chapter has no inherited blocking intro: "+destination);
   Check(Camera.main!=null&&Camera.main.enabled&&Camera.main.cullingMask!=0,"New chapter camera draws world: "+destination);
   Check(!Resources.FindObjectsOfTypeAll<RenderTexture>().Any(t=>t.name=="Chapter transition"&&t.IsCreated()),"Transition render texture released: "+destination);
   Lobby(destination);transitions.Add(new{destination,screenCaptured=transitionCaptured,skipPressed=transitionSkip,arrived=true});return true;
  }
  if(departing!=null&&departing.transitionUI!=null&&departing.transitionUI.IsPresenting){
   var t=departing.transitionUI;if(t.player!=null&&t.player.frame>0&&!transitionCaptured){Shot("transition-to-"+destination);Write("transition-to-"+destination+".json",new{movie=departing.nextChapterMovie!=null?departing.nextChapterMovie.name:null,t.player.frame,t.player.time,title=t.title.text,caption=t.caption.text,status=t.status.text});transitionCaptured=true;}
   if(destination=="Jamsil"&&transitionCaptured&&t.player.time>1&&!transitionSkip){Click(t.skipButton,"Ch3 transition skip");transitionSkip=true;}
  }
  return false;
 }
 static void SeedPhysicalOffering(){
  c.PauseGame();foreach(var e in d.encounters)Prop(e,"Complete",true);foreach(var x in d.targets){Prop(x,"Health",0f);typeof(Chapter45Target).GetField("opening",F).SetValue(x,1f);if(x.hitCollider!=null)x.hitCollider.enabled=false;if(x.panelCollider!=null)x.panelCollider.enabled=false;}foreach(var choice in d.choices)choice.Commit(-1);
  Check(d.RequiredEncountersComplete,"Existing directed goal fixture prerequisites seeded");goal=d.goals.Single(g=>g.offering);var pc=p.GetComponent<Collider>();var gc=goal.GetComponent<Collider>();int seg=-1;float dist=0,best=float.PositiveInfinity,start=0;
  for(int i=0;i<d.route.segments.Length;i++){var s=d.route.segments[i];if(s.floor==goal.floor&&start+s.Length>=d.route.Length-40){var span=s.end-s.start;float f=Mathf.Clamp01(Vector3.Dot(gc.bounds.center-s.start,span)/Mathf.Max(.0001f,span.sqrMagnitude));float sq=Vector3.ProjectOnPlane(gc.bounds.center-Vector3.Lerp(s.start,s.end,f),Vector3.up).sqrMagnitude;float progress=start+s.Length*f;if(sq<best-.0001f||Mathf.Abs(sq-best)<=.0001f&&progress>dist){best=sq;dist=progress;seg=i;}}start+=s.Length;}
  Check(seg>=0&&best<25,"Authored offering projects to final route");Prop(d,"CurrentSegment",seg);Prop(d,"Distance",dist);d.route.SampleSegment(seg,dist,out var center,out var forward);p.ApplyContinuousRoutePose(center+Vector3.up*d.footOffset,forward,0);Physics.SyncTransforms();p.transform.position+=gc.bounds.center-pc.bounds.center;p.GetComponent<Rigidbody>().position=p.transform.position;Physics.SyncTransforms();
  coin=MoneyScript.S.Coin;jewel=MoneyScript.S.Jewel;Check(gc.bounds.Intersects(pc.bounds)&&!goal.Claimed&&!d.GoalClaimed,"Paused physical goal overlap starts unclaimed");
 }
 public static object Begin(string output){
  if(!EditorApplication.isPlaying||Directory.Exists(output)||SceneManager.GetActiveScene().name!="Jamsil")throw new Exception("Fresh Jamsil Play and output required");
  folder=output;Directory.CreateDirectory(folder);mode="entry-exit-connectors";Bind();checks.Clear();errors.Clear();transitions.Clear();failures=0;frame=-1;began=EditorApplication.timeSinceStartup;active=true;
  Write("conditions.json",new{directedFixture=true,ordinaryFullRoute=false,startInput="Existing Canvas start callback; no hardware gesture claim",menuInput="Visible buttons via EventSystem center raycast and pointer click",chapter34="Existing native win callback fixture, no ordinary combat claim",chapter5="Existing prerequisites/position seed followed by real physics offering callback",savePolicy="Fresh seven named game-item snapshot and 78-item final comparison; no engine key writes",paidOrAdClick=false});
  Application.logMessageReceived+=Log;EditorApplication.update+=Tick;Next(0);return new{started=true,folder};
 }
 static void Log(string m,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m+"\n"+trace);}
 static void Tick(){if(!active||!EditorApplication.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;double age=EditorApplication.timeSinceStartup-at;
  try{if(EditorApplication.timeSinceStartup-began>180)throw new TimeoutException("Entry/exit phase "+phase);
   switch(phase){
    case 0:if(age<.5)return;Lobby("Initial Ch4");coin=MoneyScript.S.Coin;jewel=MoneyScript.S.Jewel;Inventory("01-lobby-buttons.json");Shot("01-ch4-lobby");Next(1);return;
    case 1:if(age<.4)return;Click(ButtonAt("UI/Top/Setting/Image"),"lobby settings");Next(2);return;
    case 2:if(age<.4)return;var settings=c.settingsMenuUI.GetComponent<HarborSettingsPanel>();Check(c.settingsMenuUI.activeInHierarchy&&!settings.runActions.activeInHierarchy,"Lobby settings do not expose unsupported continue/retry");c.PlayerPressedStartButton();Check(!TimeManager.isGameRunning&&cp.Elapsed==0,"Start remains blocked under lobby settings");Inventory("02-lobby-settings.json");Shot("02-lobby-settings");Next(3);return;
    case 3:if(age<.4)return;Click(ButtonAt("SettingsMenu/Panel/Close"),"close lobby settings");Check(!TimeManager.isGameRunning&&c.IsStartAreaActive(),"Closing lobby settings does not start a run");Start("Ch4 fresh run");Next(4);return;
    case 4:if(age<.6)return;Check(cp.Elapsed>0&&d.Distance>0,"Fresh run advances before pause",State());Click(c.pauseButton.GetComponent<Button>(),"pause button");elapsed=cp.Elapsed;distance=d.Distance;position=p.transform.position;handle=SceneManager.GetActiveScene().handle;Next(5);return;
    case 5:if(age<.6)return;Check(!TimeManager.isGameRunning&&cp.Elapsed==elapsed&&d.Distance==distance&&Vector3.Distance(p.transform.position,position)<.001f,"Visible pause freezes exact run position/time");c.PlayerPressedStartButton();Check(!TimeManager.isGameRunning&&cp.Elapsed==elapsed,"Start callback cannot reset paused run");Inventory("03-pause-buttons.json");Shot("03-supported-continue");Next(6);return;
    case 6:if(age<.4)return;Click(ButtonAt("SettingsMenu/Panel/RunActions/Resume"),"supported continue");Next(7);return;
    case 7:if(age<.6)return;Check(TimeManager.isGameRunning&&cp.Elapsed>elapsed&&d.Distance>distance&&SceneManager.GetActiveScene().handle==handle,"Continue resumes existing scene/time rather than restarting",State());Check(MoneyScript.S.Coin==coin&&MoneyScript.S.Jewel==jewel,"Start and continue preserve wallet");Click(c.pauseButton.GetComponent<Button>(),"pause before retry");Next(8);return;
    case 8:if(age<.4)return;Click(ButtonAt("SettingsMenu/Panel/RunActions/Retry"),"pause retry to lobby");Next(9);return;
    case 9:if(age<1||SceneManager.GetActiveScene().handle==handle)return;Bind();Lobby("Pause retry");Check(MoneyScript.S.Coin==coin&&MoneyScript.S.Jewel==jewel,"Retry preserves persistent wallet");Shot("04-retry-lobby");Next(10);return;
    case 10:if(age<.4)return;SceneManager.LoadSceneAsync("RestStop");Next(11);return;
    case 11:if(age<1||SceneManager.GetActiveScene().name!="RestStop")return;Bind();if(OpeningStoryUI.IsBlockingGameplay)OpeningStoryUI.Instance.Skip();Lobby("Ch3 directed bootstrap");Start("Ch3");Next(12);return;
    case 12:if(age<.5)return;Inventory("05-ch3-before-transition.json");BeginTransition("Jamsil");Next(13);return;
    case 13:if(!TransitionArrived())return;Shot("05-ch3-to-ch4-lobby");Inventory("06-ch3-to-ch4-arrived.json");Next(14);return;
    case 14:if(age<.5)return;Start("Ch4 after Ch3 transition");Next(15);return;
    case 15:if(age<.5)return;BeginTransition("ShoeTower");Next(16);return;
    case 16:if(!TransitionArrived())return;Shot("06-ch4-to-ch5-lobby");Inventory("07-ch4-to-ch5-arrived.json");Next(17);return;
    case 17:if(age<.5)return;Start("Ch5 after Ch4 transition");Next(18);return;
    case 18:if(age<.5)return;Check(string.IsNullOrEmpty(cp.nextScene),"Final chapter has no invented next chapter");SeedPhysicalOffering();Next(19);return;
    case 19:if(age<.5)return;Check(!goal.Claimed&&!d.GoalClaimed&&!cp.Completed&&MoneyScript.S.Coin==coin&&MoneyScript.S.Jewel==jewel,"Paused goal overlap cannot finish/pay");Click(ButtonAt("SettingsMenu/Panel/RunActions/Resume"),"continue at seeded goal");Next(20);return;
    case 20:if(!goal.Claimed&&age<4)return;Check(goal.Claimed&&d.GoalClaimed&&cp.Completed&&CanvasScript.isGameOver&&!cp.IsAdvancing,"Actual physical shoe collection reaches final terminal state",State());coin=MoneyScript.S.Coin;jewel=MoneyScript.S.Jewel;Next(21);return;
    case 21:if(age<2.7)return;Check(c.youWinUI.activeInHierarchy&&!goal.collectionVisual.gameObject.activeSelf,"Shoe collection ends with visible result and hidden collected visual");var text=c.youWinUI.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t=>t.gameObject.activeInHierarchy).Select(t=>t.text).ToArray();Write("08-result-text.json",text);Check(text.Any(t=>t.Contains("공물을 찾았다"))&&text.Any(t=>t.Contains("더 좋은 신발")),"Final result uses offering-specific text",text);c.YouWin();cp.LoadNextChapter();Check(MoneyScript.S.Coin==coin&&MoneyScript.S.Jewel==jewel&&!cp.IsAdvancing,"Terminal repeat callbacks cannot pay or invent a next chapter");Inventory("08-final-result-buttons.json");Shot("07-shoe-result");Next(22);return;
    case 22:if(age<.5)return;handle=SceneManager.GetActiveScene().handle;Click(ButtonAt("GameOverScreen/VictoryCard/Continue"),"final result return to lobby");Next(23);return;
    case 23:if(age<1||SceneManager.GetActiveScene().handle==handle)return;Bind();Lobby("Final result return");Check(SceneManager.GetActiveScene().name=="ShoeTower"&&MoneyScript.S.Coin==coin&&MoneyScript.S.Jewel==jewel&&PlayerPrefs.GetInt("chapter_rewarded_5")==1&&PlayerPrefs.GetInt("chapter_unlocked")==5,"Return preserves earned progression/wallet",State());Check(d.goals.All(g=>!g.Claimed)&&d.goals.Single(g=>g.offering).collectionVisual.gameObject.activeSelf,"Returned lobby restores goal visual and fresh claim latch");Inventory("09-returned-lobby-buttons.json");Shot("08-result-return-lobby");Next(24);return;
    case 24:if(age<.5)return;Click(ButtonAt("UI/Top/Setting/Image"),"returned lobby settings");Next(25);return;
    case 25:if(age<.4)return;Check(!c.settingsMenuUI.GetComponent<HarborSettingsPanel>().runActions.activeInHierarchy,"Returned menu does not expose stale continue");Click(ButtonAt("SettingsMenu/Panel/Close"),"returned lobby settings close");Start("New run after final result");Next(26);return;
    case 26:if(age<.6)return;Check(d.Running&&d.Elapsed>0&&d.Distance>0&&d.Distance<15&&!cp.Completed&&!d.GoalClaimed&&p.currentHealth==p.MaxHealth,"Post-result fresh run starts from authored beginning",State());Check(MoneyScript.S.Coin==coin&&MoneyScript.S.Jewel==jewel,"New run preserves completion reward without repeat payout");Shot("09-new-run-after-result");Next(27);return;
    case 27:if(age<.5)return;Finish();return;
   }
  }catch(Exception e){errors.Add(e.ToString());Finish();}
 }
 static void Finish(){active=false;EditorApplication.update-=Tick;Application.logMessageReceived-=Log;Write("summary.json",new{complete=true,passed=checks.Count-failures,failed=failures,checks,errors,transitions,seconds=EditorApplication.timeSinceStartup-began,directedFixture=true,ordinaryFullRoutesRepeated=0,hardwareGestureTested=false,menuPointerAndRaycastTested=true,newMidLevelSaveFeature=false,restorationRequired=true});}
}
