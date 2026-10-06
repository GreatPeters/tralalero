using System;using System.Threading;using System.Collections.Concurrent;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Runtime.InteropServices;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEditor;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class ValidationInput15 {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.FlattenHierarchy;
 [StructLayout(LayoutKind.Sequential)]struct Pt{public int x,y;}
 [StructLayout(LayoutKind.Sequential)]struct Mouse{public int dx,dy;public uint data,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)]struct Keyboard{public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Explicit)]struct InputUnion{[FieldOffset(0)]public Mouse mouse;[FieldOffset(0)]public Keyboard keyboard;}
 [StructLayout(LayoutKind.Sequential)]struct NativeInput{public uint type;public InputUnion data;}
 [DllImport("user32.dll",SetLastError=true)]static extern uint SendInput(uint n,NativeInput[] events,int size);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll")]static extern bool GetCursorPos(out Pt p);
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Pt p);
 [DllImport("user32.dll")]static extern bool ClientToScreen(IntPtr h,ref Pt p);
 [DllImport("user32.dll")]static extern int GetSystemMetrics(int n);
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int n);
 static CanvasScript c;static PlayerScript p;static Chapter45Director d;static ChapterProgression cp;
 static EditorWindow view;static object zoom;static string folder,label;static bool active,held,slowOnly,burstOnly,burstStarted;static volatile bool burstActive,burstHeld;static readonly ConcurrentQueue<object> burstEvents=new();static readonly ConcurrentQueue<string> burstErrors=new();static AsyncOperation pendingScene;static int frame=-1,failed,step;static double began,at;static Pt expectedCursor;static Vector2 expectedGame,lastObserved;static Vector2 calibration;static int expectedHeight=2340;static string prefix="";static int handleBefore,loadsBefore,savedCoin,savedJewel;static float pausedElapsed,pausedDistance,pauseLane;static Chapter45Choice testedChoice;static readonly List<object> sceneLoads=new();static bool mousePrimed;
 static readonly List<object> checks=new(),observations=new(),sent=new(),inputDeferrals=new();static readonly List<string> errors=new();
 sealed class Step{public string name;public double delay;public Action action;}
 static readonly List<Step> steps=new();
 static Type J=>AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
 static string Json(object x)=>(string)J.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{x});
 static void Write(string n,object x)=>File.WriteAllText(Path.Combine(folder,n),Json(x));
 static object Get(object o,string n)=>o.GetType().GetProperty(n,F)?.GetValue(o)??o.GetType().GetField(n,F)?.GetValue(o);
 static void Bind(){c=Object.FindFirstObjectByType<CanvasScript>();p=Object.FindFirstObjectByType<PlayerScript>();d=Object.FindFirstObjectByType<Chapter45Director>();cp=Object.FindFirstObjectByType<ChapterProgression>();}
 static float[] V(Vector2 a)=>new[]{a.x,a.y};
 static object State()=>new{scene=SceneManager.GetActiveScene().name,handle=SceneManager.GetActiveScene().handle,screen=new[]{Screen.width,Screen.height},running=TimeManager.isGameRunning,factor=TimeManager.timeFactor,gameover=CanvasScript.isGameOver,elapsed=cp!=null?cp.Elapsed:-1,distance=d!=null?d.Distance:-1,transferring=d!=null&&d.IsTransferring,lane=d!=null?d.Lane:0,hp=p!=null?p.currentHealth:0,armed=p!=null?Get(p,"startGestureArmed"):null,triggered=p!=null?Get(p,"startGestureTriggered"):null,settings=c!=null&&c.settingsMenuUI.activeInHierarchy,coin=MoneyScript.S!=null?MoneyScript.S.Coin:0,jewel=MoneyScript.S!=null?MoneyScript.S.Jewel:0};
 static void Check(bool ok,string name,object evidence=null){checks.Add(new{pass=ok,name,evidence});Write("checks.json",checks);if(!ok){failed++;throw new Exception(name);}}
 static void Add(string n,double delay,Action a)=>steps.Add(new Step{name=n,delay=delay,action=a});
 static Rect RenderRect(){var target=(Vector2)Get(view,"targetSize");var v=(Rect)Get(view,"viewInWindow");var scale=(Vector2)Get(zoom,"scale");var shift=(Vector2)Get(zoom,"translation");return new Rect(view.position.x+v.x+shift.x-target.x*scale.x/2,view.position.y+v.y+shift.y-target.y*scale.y/2,target.x*scale.x,target.y*scale.y);}
 static void Guard(bool requireGame=true){GetWindowThreadProcessId(GetForegroundWindow(),out var pid);if(pid!=System.Diagnostics.Process.GetCurrentProcess().Id||(requireGame&&EditorWindow.focusedWindow!=view))throw new Exception("Stop: Unity Game View lost foreground/focus");GetCursorPos(out var pos);if(Math.Abs(pos.x-expectedCursor.x)>1||Math.Abs(pos.y-expectedCursor.y)>1)throw new Exception("Stop: cursor changed outside owned input; expected "+expectedCursor.x+","+expectedCursor.y+" observed "+pos.x+","+pos.y+" lock="+Cursor.lockState);if((GetAsyncKeyState(2)&0x8000)!=0||(GetAsyncKeyState(4)&0x8000)!=0)throw new Exception("Stop: other mouse button held");}
 static void Send(uint flags,Vector2? game=null){Guard();var sampledBefore=new{frame=Time.frameCount,mouse=V(Input.mousePosition),held=Input.GetMouseButton(0),down=Input.GetMouseButtonDown(0),up=Input.GetMouseButtonUp(0),anchor=Get(p,"startPos") is Vector3 anchor?new[]{anchor.x,anchor.y,anchor.z}:null};if(Screen.width!=1080||Screen.height!=expectedHeight)throw new Exception("Unexpected Game View resolution during input");var pos=expectedCursor;var rect=RenderRect();rect.position+=calibration;if(game.HasValue){var s=game.Value;if(s.x<2||s.y<2||s.x>Screen.width-2||s.y>Screen.height-2)throw new Exception("Input outside rendered game");pos=new Pt{x=Mathf.RoundToInt(rect.x+s.x/Screen.width*rect.width),y=Mathf.RoundToInt(rect.y+(1-s.y/Screen.height)*rect.height)};if(!view.position.Contains(new Vector2(pos.x,pos.y)))throw new Exception("Input outside Game View");GetWindowThreadProcessId(WindowFromPoint(pos),out var pointPid);if(pointPid!=System.Diagnostics.Process.GetCurrentProcess().Id)throw new Exception("Input target occluded by another application");flags|=0xC001;expectedGame=s;mousePrimed=true;}
  int vx=GetSystemMetrics(76),vy=GetSystemMetrics(77),vw=GetSystemMetrics(78),vh=GetSystemMetrics(79);var e=new NativeInput{type=0,data=new InputUnion{mouse=new Mouse{dx=Mathf.RoundToInt((pos.x-vx)*65535f/(vw-1)),dy=Mathf.RoundToInt((pos.y-vy)*65535f/(vh-1)),flags=flags,extra=new UIntPtr(0xC015)}}};uint count=SendInput(1,new[]{e},Marshal.SizeOf<NativeInput>());if(count!=1)throw new Exception("SendInput failed: "+Marshal.GetLastWin32Error());expectedCursor=pos;SessionState.SetString("Chapter45.Validation15.Cursor",pos.x+","+pos.y);if((flags&2)!=0)held=true;if((flags&4)!=0)held=false;File.AppendAllText(Path.Combine(folder,"sent-input.jsonl"),Json(new{step=label,frame=Time.frameCount,flags,desktop=new[]{pos.x,pos.y},game=game.HasValue?V(game.Value):null,inserted=count,utc=DateTime.UtcNow.ToString("O"),sampledBefore})+"\n");
 }
 static Vector2 Center(RectTransform rt){var canvas=rt.GetComponentInParent<Canvas>().rootCanvas;return RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rt.TransformPoint(rt.rect.center));}
 static Rect ScreenRect(RectTransform rt){var corners=new Vector3[4];rt.GetWorldCorners(corners);var canvas=rt.GetComponentInParent<Canvas>().rootCanvas;var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;var a=RectTransformUtility.WorldToScreenPoint(cam,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(cam,corners[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}
 static Button ButtonAt(string path)=>c.transform.Find(path).GetComponent<Button>();
 static void InspectButton(Button b,string name){var r=ScreenRect((RectTransform)b.transform);var center=Center((RectTransform)b.transform);var ev=new PointerEventData(EventSystem.current){position=center};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(ev,hits);Check(b.isActiveAndEnabled&&b.interactable&&r.xMin>=0&&r.yMin>=0&&r.xMax<=Screen.width&&r.yMax<=Screen.height&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,"Visible button geometry: "+name,new{rect=new[]{r.x,r.y,r.width,r.height},center=V(center),top=hits.Count>0?hits[0].gameObject.name:null});}
 static void Click(string name,Func<Button> resolve,bool twice=false,bool cancel=false){Add(name+" position",.15,()=>{var b=resolve();InspectButton(b,name);Send(0,Center((RectTransform)b.transform));});Add(name+" down",.12,()=>Send(2));if(cancel)Add(name+" cancel drag outside",.12,()=>Send(0,new Vector2(35,Screen.height*.55f)));Add(name+" up",.12,()=>Send(4));if(twice){Add(name+" repeated down",.06,()=>Send(2));Add(name+" repeated up",.06,()=>Send(4));}}
 static Vector2 start;
 static void Gesture(string name,Vector2 delta,bool startInside=true){Add(name+" position",.15,()=>{start=startInside?Center(c.startAreaImg):new Vector2(40,Screen.height*.9f);Send(0,start);});Add(name+" down",.15,()=>Send(2));Add(name+" move",.18,()=>Send(0,start+delta));Add(name+" up",.18,()=>Send(4));}
 static void Shot(string n)=>ScreenCapture.CaptureScreenshot(Path.Combine(folder,prefix+n+".png"));

 static int lastFloor;static bool recenterPending;static EditorWindow other;static bool keyHeld,driving,postAdded,wasTransfer,focusMode,probeOnly;static int images,events,sceneHandle,initialFloor;static double nextDrive,nextSample,doneAt;static float initialHp,initialAttack,beforeLane,rightLane,beforeDistance,beforeElapsed,lastLane,lastDistance,maxLaneStep;static Vector2 drivePointer;static readonly List<object> discontinuities=new();static string reason="running",policy="starting";
 static void Key(bool down){Guard();var input=new NativeInput{type=1,data=new InputUnion{keyboard=new Keyboard{vk=13,flags=down?0u:2u,extra=new UIntPtr(0xC015)}}};if(SendInput(1,new[]{input},Marshal.SizeOf<NativeInput>())!=1)throw new Exception("Owned Return SendInput failed");keyHeld=down;File.AppendAllText(Path.Combine(folder,"sent-input.jsonl"),Json(new{label,key="Return",down,frame=Time.frameCount})+"\n");}
 static void CleanupGuard(){GetWindowThreadProcessId(GetForegroundWindow(),out var pid);if(pid!=System.Diagnostics.Process.GetCurrentProcess().Id)throw new Exception("Owned release requires Unity foreground; external app untouched");}
 static void Release(){CleanupGuard();if(SendInput(1,new[]{new NativeInput{type=0,data=new InputUnion{mouse=new Mouse{flags=4,extra=new UIntPtr(0xC015)}}}},Marshal.SizeOf<NativeInput>())!=1)throw new Exception("Owned mouse release failed");held=false;File.AppendAllText(Path.Combine(folder,"sent-input.jsonl"),Json(new{label,flags=4,releaseOutsideGame=EditorWindow.focusedWindow!=view,frame=Time.frameCount})+"\n");}
 static void Capture(string n){Shot((images++).ToString("D2")+"-"+n);}
 static object Detailed()=>new{state=State(),wall=EditorApplication.timeSinceStartup-began,floor=d.CurrentFloor,lifts=d.LiftCount,attack=p.ResolvedAttackDamage,position=new[]{p.transform.position.x,p.transform.position.y,p.transform.position.z},input=V(Input.mousePosition),unityHeld=Input.GetMouseButton(0),nativeHeld=held,focused=Application.isFocused,window=EditorWindow.focusedWindow?.GetType().Name,policy,projectiles=p.GetComponentsInChildren<WeaponScript>(true).Sum(w=>w.TotalProjectilesSpawned),choices=d.choices.Select(x=>new{x.name,x.Selected,x.Selection}).ToArray()};
 static void SlowAndReverse(string n){
  Add(n+" release previous",.15,()=>{if(held)Send(4);});
  Add(n+" position",.15,()=>Send(0,new Vector2(450,Screen.height*.4f)));
  Add(n+" down",.15,()=>{beforeLane=d.Lane;Send(2);});
  for(int i=1;i<=10;i++){int j=i;Add(n+" slow right "+j,.1,()=>Send(0,new Vector2(450+j*10,Screen.height*.4f)));}
  Add(n+" right check",.25,()=>{rightLane=d.Lane;Check(rightLane>beforeLane+.03f,n+" slow drag moves right",new{beforeLane,rightLane});});
  for(int i=1;i<=10;i++){int j=i;Add(n+" reverse left "+j,.1,()=>Send(0,new Vector2(550-j*10,Screen.height*.4f)));}
  Add(n+" reverse check",.25,()=>{Check(d.Lane<rightLane-.03f,n+" held direction reversal moves left",new{rightLane,after=d.Lane});Send(4);Capture(n+"-reversed");});
 }
 static void FocusBoundary(string n,bool lobby){
  Add(n+" pointer",.15,()=>{if(held)Send(4);Send(0,lobby?Center(c.startAreaImg):new Vector2(530,Screen.height*.4f));});
  Add(n+" down",.18,()=>Send(2));
  Add(n+" focus other Editor panel",.25,()=>{Guard();beforeLane=d.Lane;beforeDistance=d.Distance;beforeElapsed=cp.Elapsed;other.Focus();focusMode=true;Write(n+"-focus-out.json",Detailed());});
  Add(n+" release while out",.35,()=>{Check(EditorWindow.focusedWindow==other,n+" other Editor panel receives focus",Detailed());Release();});
  Add(n+" return GameView",.35,()=>{Guard(false);view.Focus();focusMode=false;});
  Add(n+" check focus return",.35,()=>{Check(EditorWindow.focusedWindow==view&&Mathf.Abs(d.Lane-beforeLane)<.02f&&SceneManager.GetActiveScene().handle==sceneHandle,n+" release and focus return preserves lane and scene",new{beforeLane,after=Detailed()});Check(lobby?!TimeManager.isGameRunning&&cp.Elapsed==0:TimeManager.isGameRunning,n+" no unintended start/stop",Detailed());Capture(n+"-returned");});
  Add(n+" new press at different position",.15,()=>Send(0,new Vector2(700,Screen.height*.4f)));
  Add(n+" new down",.15,()=>{beforeLane=d.Lane;Send(2);});
  Add(n+" new stationary press check",.25,()=>{Check(Mathf.Abs(d.Lane-beforeLane)<.02f,n+" new press does not reuse old anchor",new{beforeLane,after=d.Lane});Send(4);});
 }
 static void HeldPause(string n){
  Add(n+" pointer",.2,()=>{if(held)Send(4);InspectButton(c.pauseButton.GetComponent<Button>(),"pause");Send(0,Center((RectTransform)c.pauseButton.transform));});
  Add(n+" pointer down",.15,()=>{beforeLane=d.Lane;Send(2);});
  Add(n+" real UI selection",.2,()=>{Check(EventSystem.current.currentSelectedGameObject==c.pauseButton,n+" OS press selects existing pause button",new{selected=EventSystem.current.currentSelectedGameObject?.name});Key(true);});
  Add(n+" submit up",.15,()=>Key(false));
  Add(n+" held pause check",.3,()=>{Check(held&&(GetAsyncKeyState(1)&0x8000)!=0&&!TimeManager.isGameRunning&&c.settingsMenuUI.activeInHierarchy,n+" keyboard UI submit pauses while pointer stays held",Detailed());Check(Mathf.Abs(d.Lane-beforeLane)<.02f,n+" button press does not move lane",new{beforeLane,after=d.Lane});beforeDistance=d.Distance;beforeElapsed=cp.Elapsed;Capture(n+"-held");Send(0,new Vector2(600,Screen.height*.4f));});
  Add(n+" release while paused",.35,()=>Send(4));
  Add(n+" remains paused",.3,()=>Check(!TimeManager.isGameRunning&&d.Distance==beforeDistance&&cp.Elapsed==beforeElapsed&&Mathf.Abs(d.Lane-beforeLane)<.02f,n+" held move and release keep game frozen",Detailed()));
  Click(n+" resume",()=>ButtonAt("SettingsMenu/Panel/RunActions/Resume"));
  Add(n+" resumed stable",.35,()=>Check(TimeManager.isGameRunning&&Mathf.Abs(d.Lane-beforeLane)<.02f&&SceneManager.GetActiveScene().handle==sceneHandle,n+" resume preserves lane and same run",Detailed()));
 }
 static void Setup(){
  Add("calibrate",.5,()=>Send(0,new Vector2(Screen.width*.5f,Screen.height*.5f)));
  Add("calibration second point",.25,()=>{var observed=(Vector2)Input.mousePosition;var r=RenderRect();var diff=expectedGame-observed;calibration=new Vector2(diff.x/Screen.width*r.width,-diff.y/Screen.height*r.height);Write("calibration.json",new{desired=V(expectedGame),observed=V(observed),offset=V(calibration)});Send(0,new Vector2(Screen.width*.6f,Screen.height*.6f));});
  Add("calibration verify",.25,()=>Check(Vector2.Distance((Vector2)Input.mousePosition,expectedGame)<6,"Independent game/desktop calibration",new{desired=V(expectedGame),observed=V(Input.mousePosition)}));
  Add("fresh lobby",.4,()=>{Check(!TimeManager.isGameRunning&&c.IsStartAreaActive()&&d.Distance==0,"Natural run begins at fresh lobby",Detailed());Capture("lobby");});
  FocusBoundary("lobby",true);
  Add("slow start pointer",.2,()=>{start=Center(c.startAreaImg);Send(0,start);});Add("slow start down",.15,()=>Send(2));
  for(int i=1;i<=12;i++){int j=i;Add("slow start "+j,.1,()=>Send(0,start+new Vector2(j*4,0)));}
  Add("slow start release",.15,()=>Send(4));
  Add("start confirmed",.3,()=>{Check(TimeManager.isGameRunning&&cp.Elapsed>0&&d.Distance>0,"Actual slow lobby drag begins forward travel",Detailed());Capture("started");});
  SlowAndReverse("before-route");HeldPause("before-route");FocusBoundary("running",false);
  Add("begin natural route",.25,()=>{Check(p.currentHealth>0&&!d.IsTransferring,"Begin ordinary natural travel",Detailed());driving=true;nextDrive=0;drivePointer=new Vector2(540,Screen.height*.4f);if(held)Send(4);Send(0,drivePointer);});
 }
    sealed class Seen { public float at,lane,ahead,width,pixels,propPixels;public int id;public string name,kind; }
    sealed class Observation { public float at; public Seen[] items; }
    static readonly Queue<Observation> seenQueue=new();
    static float observeAt,decideAt,heldLane,heldYaw;
    static readonly HashSet<string> seenCaptures=new();
    static Camera GameCamera()=>Camera.main;
    static bool ScreenBounds(Renderer[] renderers,out Bounds bounds,out float pixels){
        bounds=default;pixels=0;var rr=renderers.Where(r=>r!=null&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rr.Length==0||GameCamera()==null)return false;bounds=rr[0].bounds;foreach(var r in rr.Skip(1))bounds.Encapsulate(r.bounds);
        var cam=GameCamera();float left=2,right=-1,low=2,high=-1;bool front=false;
        for(int i=0;i<8;i++){var p=cam.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(p.z<=0)continue;front=true;left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);low=Mathf.Min(low,p.y);high=Mathf.Max(high,p.y);}
        pixels=Mathf.Max(0,Mathf.Min(1,high)-Mathf.Max(0,low))*Screen.height;
        return front&&right>.02f&&left<.98f&&high>.02f&&low<.94f&&pixels>=12;
    }
    static Seen[] ObserveVisible(Vector3 origin,Vector3 right){
        var result=new List<Seen>();Vector3 forward=Vector3.ProjectOnPlane(p.transform.forward,Vector3.up).normalized;
        foreach(var encounter in d.encounters){
            if(!encounter.Activated||encounter.Complete||encounter.floor!=d.CurrentFloor||!encounter.Eligible)continue;
            foreach(var actor in encounter.actors){
                if(actor==null||!actor.gameObject.activeInHierarchy||actor.IsDead)continue;
                var animator=actor.GetComponentInChildren<Animator>();var mesh=animator!=null?animator.GetComponentsInChildren<Renderer>():actor.GetComponentsInChildren<Renderer>();
                if(!ScreenBounds(mesh,out var b,out float px))continue;
                float ahead=Vector3.Dot(actor.transform.position-p.transform.position,forward);if(ahead<0||ahead>32)continue;
                var role=actor.GetComponent<Chapter45RoleAction>();float pp=0;if(role?.heldProp!=null)ScreenBounds(role.heldProp.GetComponentsInChildren<Renderer>(),out var pb,out pp);
                result.Add(new Seen{at=d.Elapsed,id=actor.GetInstanceID(),name=actor.name,kind=role==null?"actor":role.role.ToString(),lane=Vector3.Dot(actor.transform.position-origin,right),ahead=ahead,width=b.size.x,pixels=px,propPixels=pp});
                if(role?.warningMarker!=null&&role.warningMarker.gameObject.activeInHierarchy&&ScreenBounds(role.warningMarker.GetComponentsInChildren<Renderer>(),out var wb,out var wp)){
                    var aheadMarker=Vector3.Dot(role.warningMarker.position-p.transform.position,forward);
                    if(aheadMarker>=-3&&aheadMarker<14)result.Add(new Seen{at=d.Elapsed,id=role.GetInstanceID(),name=role.name,kind="windup",lane=Vector3.Dot(wb.center-origin,right),ahead=aheadMarker,width=Mathf.Abs(right.x)*wb.size.x+Mathf.Abs(right.z)*wb.size.z,pixels=wp});
                }
            }
        }
        foreach(var target in d.targets){
            if(target.floor!=d.CurrentFloor||!target.Eligible||target.Open||target.hitCollider==null||!target.hitCollider.enabled)continue;
            // Presentation meshes can be children even when the legacy renderer list is empty.
            // Observe actual enabled geometry, excluding the health-label text.
            var panelMeshes=target.targetRenderers.Concat(target.GetComponentsInChildren<Renderer>()).Where(r=>r!=null&&r.GetComponent<TMPro.TMP_Text>()==null).Distinct().ToArray();
            if(!ScreenBounds(panelMeshes,out var bounds,out var px))continue;
            var position=target.hitCollider.bounds.center;float ahead=Vector3.Dot(position-p.transform.position,forward);if(ahead<0||ahead>32)continue;
            result.Add(new Seen{at=d.Elapsed,id=target.GetInstanceID(),name=target.name,kind="visible panel",lane=Vector3.Dot(position-origin,right),ahead=ahead,width=bounds.size.x,pixels=px});
        }
        foreach(var h in d.hazards){
            if(h.floor!=d.CurrentFloor||h.footprint==null||!h.footprint.activeInHierarchy)continue;
            if(ScreenBounds(h.footprint.GetComponentsInChildren<Renderer>(),out var b,out float px))result.Add(new Seen{at=d.Elapsed,id=h.GetInstanceID(),name=h.name,kind="hazard footprint",lane=Vector3.Dot(b.center-origin,right),ahead=Vector3.Dot(b.center-p.transform.position,forward),width=Mathf.Abs(right.x)*b.size.x+Mathf.Abs(right.z)*b.size.z,pixels=px});
        }
        if(p.IsStationaryCombat&&p.HoldoutAim!=null){
            foreach(var a in p.HoldoutAim.police){
                if(a==null||!a.gameObject.activeInHierarchy||a.RuntimeState==EnemyEventRuntimeState.Dead)continue;
                var animator=a.GetComponentInChildren<Animator>();if(!ScreenBounds(animator!=null?animator.GetComponentsInChildren<Renderer>():a.GetComponentsInChildren<Renderer>(),out var b,out var px))continue;
                var delta=a.transform.position-p.transform.position;result.Add(new Seen{at=d.Elapsed,id=a.GetInstanceID(),name=a.name,kind="audience",lane=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,ahead=delta.magnitude,width=b.size.x,pixels=px});
            }
        }
        return result.ToArray();
    }

 static void Drive(){
  double now=EditorApplication.timeSinceStartup;if(now<nextDrive)return;nextDrive=now+.1;
  if(d.IsTransferring){if(held)Send(4);return;}
  if(p.IsStationaryCombat)throw new Exception("Natural scope unexpectedly reached stationary combat; no direct aim callbacks allowed");
  var origin=(Vector3)Get(p,"routeLaneOrigin");var right=(Vector3)Get(p,"routeRight");var range=(Vector2)Get(p,"xRange");
  var seen=ObserveVisible(origin,right);seenQueue.Enqueue(new Observation{at=d.Elapsed,items=seen});Seen[] matured=null;
  while(seenQueue.Count>0&&d.Elapsed-seenQueue.Peek().at>=.35f)matured=seenQueue.Dequeue().items;
  if(matured!=null){var threats=matured.Where(x=>x.kind=="windup"||x.kind=="hazard footprint").ToArray();var target=matured.Where(x=>x.kind!="windup"&&x.kind!="hazard footprint"&&x.kind!="audience").OrderBy(x=>x.ahead).ThenBy(x=>Mathf.Abs(x.lane-d.Lane)).FirstOrDefault();
   if(threats.Length>0){float best=float.NegativeInfinity;foreach(float candidate in new[]{range.x+.65f,range.x*.5f,0,range.y*.5f,range.y-.65f}){float clearance=threats.Min(x=>Mathf.Abs(candidate-x.lane)-x.width*.5f);float score=Mathf.Min(clearance,2.5f)-Mathf.Abs(candidate-d.Lane)*.1f;if(score>best){best=score;heldLane=candidate;}}policy="delayed visible warning";}
   else{heldLane=target!=null?target.lane:0;policy=target!=null?"delayed visible target":"center";}
  }
  foreach(var ch in d.choices)if(!ch.Selected&&ch.OnCurrentFloor&&d.Distance>=ch.distance-35&&d.Distance<ch.distance){heldLane=-3;policy="visible first fork left";}
  float desired=Mathf.Clamp(heldLane,range.x,range.y);float error=desired-d.Lane;
  File.AppendAllText(Path.Combine(folder,"policy.jsonl"),Json(new{at=d.Elapsed,distance=d.Distance,lane=d.Lane,desired,error,policy,seen})+"\n");
  if(recenterPending){if(Input.GetMouseButton(0))return;drivePointer.x=540;Send(0,drivePointer);recenterPending=false;return;}
  if(!held){Send(2);return;}
  float dx=Mathf.Clamp(error*55,-75,75);
  if(Mathf.Abs(error)<.08f)return;
  if(drivePointer.x+dx<100||drivePointer.x+dx>980){Send(4);recenterPending=true;return;}
  drivePointer.x+=dx;Send(0,drivePointer);
 }
 static void PostRoute(){
  postAdded=true;driving=false;if(held)Send(4);Capture("natural-route-milestone");
  if(SceneManager.GetActiveScene().name=="Jamsil"){var choice=d.choices.OrderBy(x=>x.distance).First();Check(choice.Selected&&choice.Selection==0,"Natural Ch4 first fork selected left from lobby",new{choice.name,choice.distance,choice.Selection,state=Detailed()});}
  else Check(d.LiftCount>=1&&!d.IsTransferring&&d.CurrentFloor!=initialFloor,"Natural Ch5 first floor connection landed",Detailed());
  SlowAndReverse("after-milestone");
  if(SceneManager.GetActiveScene().name=="ShoeTower"){HeldPause("after-milestone");FocusBoundary("after-milestone",false);}
  else Add("first fork remains selected once",.2,()=>{var choice=d.choices.OrderBy(x=>x.distance).First();Check(choice.Selected&&choice.Selection==0&&d.Timeline.Count(x=>x.Contains("choice committed "+choice.name+"="))==1,"Natural first fork stays committed after reverse input",Detailed());});
  Add("post milestone final",.4,()=>{Check(p.currentHealth>0&&SceneManager.GetActiveScene().handle==sceneHandle&&p.ResolvedAttackDamage==initialAttack,"Ordinary statistics and one continuous scene preserved",Detailed());Check(discontinuities.Count==0,"No unexplained lateral step over 0.8m outside transfer",new{maxLaneStep,discontinuities});reason="requested-natural-boundary-covered";Capture("final");});
 }
 public static object BeginProbe(string output){probeOnly=true;return Begin(output);}
 public static object Begin(string output){
  if(!EditorApplication.isPlaying||Directory.Exists(output))throw new Exception("Fresh Play/output required");folder=output;Directory.CreateDirectory(folder);view=Resources.FindObjectsOfTypeAll(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Cast<EditorWindow>().Single();zoom=Get(view,"m_ZoomArea");other=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x=>x.GetType().Name=="InspectorWindow");if(other==null)throw new Exception("Existing Inspector required; do not create/rearrange windows");
  GetWindowThreadProcessId(GetForegroundWindow(),out var pid);if(pid!=System.Diagnostics.Process.GetCurrentProcess().Id)throw new Exception("Unity foreground required before focusing");view.Focus();GetCursorPos(out expectedCursor);SessionState.SetString("Chapter45.Validation15.Cursor",expectedCursor.x+","+expectedCursor.y);if((GetAsyncKeyState(1)&0x8000)!=0||(GetAsyncKeyState(13)&0x8000)!=0)throw new Exception("Initial test key/button already held");
  Bind();sceneHandle=SceneManager.GetActiveScene().handle;initialFloor=lastFloor=d.CurrentFloor;recenterPending=false;initialHp=p.currentHealth;initialAttack=p.ResolvedAttackDamage;frame=-1;steps.Clear();checks.Clear();errors.Clear();discontinuities.Clear();seenQueue.Clear();step=failed=images=events=0;held=keyHeld=driving=postAdded=wasTransfer=focusMode=false;calibration=Vector2.zero;began=at=EditorApplication.timeSinceStartup;lastLane=d.Lane;lastDistance=d.Distance;nextSample=0;reason="running";Setup();
  Write("conditions.json",new{scene=SceneManager.GetActiveScene().name,sceneHandle,initialHp,initialAttack,initialFloor,positionFixtures=false,directControlCallbacks=false,statOverrides=false,healthOverrides=false,enemyOverrides=false,engineKeyWrites=false,input="Windows SendInput / existing UI EventSystem; existing Inspector Focus only",focusLimit="Unity GameView versus another panel in same Editor; no other application activated",ordinaryTimeScale=Time.timeScale,otherWindow=other.GetInstanceID(),windows=Resources.FindObjectsOfTypeAll<EditorWindow>().Select(w=>new{type=w.GetType().Name,id=w.GetInstanceID(),rect=w.position.ToString()}).ToArray(),lifts=d.lifts.Select(l=>new{l.name,l.afterSegment,l.destinationSegment,l.minimumFloorSeconds,l.requireFloorClear,l.duration}).ToArray()});
  active=true;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;return new{started=true,folder,steps=steps.Count};
 }
 static void Log(string m,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m+"\n"+stack);}
 static void Tick(){
  if(!active||!EditorApplication.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();
  try{
   Guard(!focusMode);double now=EditorApplication.timeSinceStartup;if(now-began>580)throw new TimeoutException("Natural OS run time limit");
   if(p.currentHealth<=0||CanvasScript.isGameOver)throw new Exception("Ordinary run ended before requested boundary: "+Json(Detailed()));
   if(frame!=Time.frameCount){frame=Time.frameCount;float laneDelta=Mathf.Abs(d.Lane-lastLane);if(TimeManager.isGameRunning&&!d.IsTransferring&&!wasTransfer&&d.CurrentFloor==lastFloor){maxLaneStep=Mathf.Max(maxLaneStep,laneDelta);if(laneDelta>.8f)discontinuities.Add(new{frame,label,before=lastLane,after=d.Lane,distance=d.Distance,held,mouse=V(Input.mousePosition)});}lastLane=d.Lane;lastDistance=d.Distance;lastFloor=d.CurrentFloor;wasTransfer=d.IsTransferring;
    if(now>=nextSample){nextSample=now+.15;File.AppendAllText(Path.Combine(folder,"trajectory.jsonl"),Json(new{label,state=Detailed()})+"\n");}
    while(events<d.Timeline.Count){string e=d.Timeline[events++];File.AppendAllText(Path.Combine(folder,"director-timeline.txt"),e+"\n");if(e.Contains("lift board")||e.Contains("lift land")||e.Contains("choice committed")||e.Contains("encounter cleared"))Capture("event-"+events);}
   }
   if(driving){if(Screen.width!=1080||Screen.height!=2340)return;if(probeOnly){reason="boundary-probe-covered";Finish();return;}if(!postAdded&&((SceneManager.GetActiveScene().name=="Jamsil"&&d.choices.OrderBy(x=>x.distance).First().Selected&&d.Distance>d.choices.OrderBy(x=>x.distance).First().distance+8)||(SceneManager.GetActiveScene().name=="ShoeTower"&&d.LiftCount>=1&&!d.IsTransferring&&d.CurrentFloor!=initialFloor))){PostRoute();at=now;}else{label="natural route";Drive();return;}}
   if(step>=steps.Count){Finish();return;}var s=steps[step];if(now-at<s.delay)return;if(Screen.width!=1080||Screen.height!=2340)return;label=s.name;s.action();step++;at=EditorApplication.timeSinceStartup;Write("status.json",new{step,label,total=steps.Count,state=Detailed()});
  }catch(Exception e){errors.Add(e.ToString());Finish();}
 }
 static void Finish(){
  try{if(keyHeld){CleanupGuard();var input=new NativeInput{type=1,data=new InputUnion{keyboard=new Keyboard{vk=13,flags=2,extra=new UIntPtr(0xC015)}}};SendInput(1,new[]{input},Marshal.SizeOf<NativeInput>());keyHeld=false;}if(held)Release();if(focusMode){Guard(false);view.Focus();focusMode=false;}}catch(Exception e){errors.Add("Input cleanup: "+e.Message);}
  active=false;EditorApplication.update-=Tick;Application.logMessageReceived-=Log;Write("summary.json",new{complete=true,reason,passed=checks.Count-failed,failed,errors,checks,stepsCompleted=step,stepsTotal=steps.Count,seconds=EditorApplication.timeSinceStartup-began,final=Detailed(),maxLaneStep,discontinuities,actualOsInput=true,positionFixtures=false,directControlCallbackInvocation=false,statOverrides=false,restorationRequired=true});
 }
}
