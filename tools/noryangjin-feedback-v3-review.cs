using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class NoryangjinFeedbackV3Review
{
    const string Root="outputs/noryangjin-feedback-v3-2026-09-28";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    static void Select(string type,string method,params object[] args)=>typeof(ChapterPlaytestPreferences).Assembly.GetType(type).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    public static object Prepare()
    {
        if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit scene required; do not enter a hidden save dialog.");
        if(!File.Exists(Root+"/before/playerprefs.tsv"))ChapterPlaytestPreferences.SnapshotAt(Root+"/before/playerprefs.tsv");
        Select("NoryangjinMapToolTestStartStage","SelectStage",1);Select("NoryangjinMapToolTestOverrides","Select",true,true);Select("NoryangjinMapToolTestSpeed","SelectTimeScale",1f);
        return Audit();
    }
    public static object Finish()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
        ChapterPlaytestPreferences.RestoreAt(Root+"/before/playerprefs.tsv");Prepare();
        var errors=new List<string>();int count=0;
        foreach(var line in File.ReadAllLines(Root+"/before/playerprefs.tsv"))
        {
            count++;var f=line.Split('\t');bool existed=bool.Parse(f[2]);
            if(PlayerPrefs.HasKey(f[0])!=existed){errors.Add(f[0]);continue;}if(!existed)continue;
            string v=f[1]=="int"?PlayerPrefs.GetInt(f[0]).ToString(System.Globalization.CultureInfo.InvariantCulture):f[1]=="float"?PlayerPrefs.GetFloat(f[0]).ToString("R",System.Globalization.CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(f[0])));
            if(v!=f[3])errors.Add(f[0]);
        }
        var result=new{count,errors,preserved=errors.Count==0,power9999=SessionState.GetBool("NoryangjinMapTool.TestPower9999",false),fastLateral=SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false),stage=SessionState.GetInt("NoryangjinMapTool.TestStartStage",0),dirty=EditorSceneManager.GetActiveScene().isDirty};File.WriteAllText(Root+"/restoration.json",Json(result));return result;
    }
    public static object Audit()
    {
        var scene=EditorSceneManager.GetActiveScene();var d=Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        if(d==null||!scene.name.EndsWith("SR18_Revamp"))throw new Exception("Expected D project safe-copy scene");
        var events=d.GetComponentsInChildren<NoryangjinRevampEvent>(true);var gate=d.GetComponentInChildren<NoryangjinShutterEvent>();var auction=d.GetComponentInChildren<NoryangjinAuctionActivity>();
        var roads=GameObject.Find("Noryangjin_MapTool/Roads");
        var result=new{scene=scene.path,scene.isDirty,power9999=SessionState.GetBool("NoryangjinMapTool.TestPower9999",false),fastLateral=SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false),roadsPosition=roads.transform.position.ToString(),roadsScale=roads.transform.lossyScale.ToString(),events=events.Select(e=>e.name).ToArray(),numberedPickups=d.GetComponentsInChildren<NoryangjinNumberedHat>(true).Length,boxWalls=gate.walls.Length,lastBoxGap=Vector3.Distance(gate.walls.Last().transform.position,gate.shutter.transform.position),roller=gate.roller!=null,auctionPeople=auction.bidders.Length+1,auctionColliders=auction.GetComponentsInChildren<Collider>(true).Length,floors=roads.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.Contains("Connector")||r.name=="ColdAuctionFloor").Select(r=>new{r.name,bounds=r.bounds.ToString()}).ToArray()};
        File.WriteAllText(Root+"/scene-audit.json",Json(result));
        var surface=roads.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="ColdAuctionFloorSurface");
        File.WriteAllText(Root+"/claude-fixes-audit.json",Json(new{truckCount=events.Count(e=>e.name=="J_ReversingLiveFishTruck"),floorColliderTop=roads.GetComponentsInChildren<MeshCollider>(true).First(c=>c.name=="ColdAuctionFloor").bounds.max.y,visibleFloorTop=surface.bounds.max.y,visibleFloorStart=surface.bounds.min.z,connectorTop=roads.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="ContinuousConnector2").bounds.max.y}));return result;
    }
    public static object SplitAudit()
    {
        var rows=Object.FindObjectsByType<NoryangjinRoadScenerySplit>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(s=>new{s.name,source=s.source.name,collisionUnchanged=s.GetComponent<MeshCollider>().sharedMesh==s.source,floorMaxY=s.GetComponent<Renderer>().bounds.max.y,overhead=s.overhead!=null}).ToArray();File.WriteAllText(Root+"/split-audit.json",Json(rows));return new{count=rows.Length,valid=rows.All(r=>r.collisionUnchanged&&r.floorMaxY<=.66f&&r.overhead)};
    }
    public static object FinalizeNotice()
    {
        var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.name!="Noryangjin_MapTool_Mode_SR18_Revamp")throw new Exception("Saved safe-copy Edit scene required");
        var banner=Object.FindObjectsByType<NoryangjinBannerEvent>(FindObjectsSortMode.None).Single(e=>e.name=="D_ColdAuction");banner.banner="경매 중! 가운데로 지나가세요";
        EditorUtility.SetDirty(banner);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{banner.banner,saved=!scene.isDirty};
    }
    public static object Capture()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        EditorApplication.isPaused=false;
        string folder="tmp/image-previews/noryangjin-feedback-v3-2026-09-28/native-"+DateTime.Now.ToString("HHmmss");Directory.CreateDirectory(folder);
        var p=Object.FindFirstObjectByType<PlayerScript>();var d=Object.FindFirstObjectByType<NoryangjinRevampDirector>();var cam=GameObject.Find("MapTool_Camera").GetComponent<Camera>();
        var owner=p.GetType().GetField("stationaryCombatOwner",Hidden);var point=p.GetType().GetField("stationaryCombatPosition",Hidden);var hold=new GameObject("V3CaptureAnchor");
        string[] names={"approach","water-on","water-remains","shutter-boxes","bridge-exit","bridge-turn","cold-front","auction","auction-close","container-shadow","container-impact"};
        Vector3[] positions={new(80,.22f,-7.15f),new(124.3f,.22f,-93),new(124.3f,.22f,-100),new(124.3f,.22f,-316),new(124.3f,.22f,-340),new(110,.22f,-348),new(-67,.22f,-340),new(-67,.22f,-302),new(-65.8f,.22f,-286),new(225.5f,.22f,-40),new(225.5f,.22f,-40)};
        Vector3[] headings={Vector3.right,Vector3.back,Vector3.back,Vector3.back,Vector3.back,Vector3.left,Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward};
        int step=-1;double due=EditorApplication.timeSinceStartup+1;bool started=false,waiting=false,requested=false;string file="";var records=new List<object>();
        var hose=d.GetComponentInChildren<NoryangjinHoseEvent>();var container=d.GetComponentsInChildren<NoryangjinMarketIncident>().First(e=>e.kind==NoryangjinMarketIncident.Kind.ContainerDrop);
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
                if(!EditorApplication.isPlaying||p==null){EditorApplication.update-=tick;return;}if(EditorApplication.timeSinceStartup<due)return;
                if(!started)
                {
                    Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();if(!TimeManager.isGameRunning)Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
                    if(!TimeManager.isGameRunning){due=EditorApplication.timeSinceStartup+.5;return;}started=true;Time.timeScale=1;
                    foreach(var w in Object.FindObjectsByType<WeaponScript>(FindObjectsSortMode.None)){w.CancelInvoke();w.StopAllCoroutines();w.enabled=false;}
                    foreach(var b in Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None))b.gameObject.SetActive(false);
                    foreach(var h in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))h.enabled=false;
                    var tutorial=Object.FindFirstObjectByType<CoastalTutorialUI>();if(tutorial!=null){tutorial.Close();tutorial.enabled=false;}
                    foreach(var e in Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
                    foreach(var e in d.GetComponentsInChildren<NoryangjinRevampEvent>())e.enabled=false;
                    typeof(NoryangjinMarketBranch).GetProperty("Selected").SetValue(d.Branch,true);typeof(NoryangjinMarketBranch).GetProperty("Outside").SetValue(d.Branch,false);
                    due=EditorApplication.timeSinceStartup+.3;return;
                }
                if(waiting&&!requested)
                {
                    if(step==0||step==4||step==9)
                    {
                        var oc=cam.GetComponent<NoryangjinCameraOcclusion>();var rays=new[]{.25f,.5f,.7f}.Select(y=>cam.ViewportPointToRay(new Vector3(.5f,y,0))).ToArray();
                        var rows=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.forceRenderingOff&&rays.Any(ray=>r.bounds.IntersectRay(ray,out float at)&&at<65)).Select(r=>new{r.name,parent=r.transform.parent.name,bounds=r.bounds.ToString(),opacity=oc.SceneryOpacity(r),shader=r.sharedMaterial?.shader.name,active=r.gameObject.activeInHierarchy}).ToArray();
                        File.WriteAllText(folder+"/"+names[step]+"-occluders.json",Json(rows));
                    }
                    if(step==9)
                    {
                        var r=container.warning.GetComponent<Renderer>();var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);
                        File.WriteAllText(folder+"/shadow-state.json",Json(new{container.Triggered,clock=typeof(NoryangjinMarketIncident).GetField("clock",Hidden).GetValue(container),container.warningSeconds,container.dropSeconds,active=container.warning.gameObject.activeInHierarchy,r.enabled,position=container.warning.position.ToString(),rotation=container.warning.eulerAngles.ToString(),scale=container.warning.lossyScale.ToString(),bounds=r.bounds.ToString(),shader=r.sharedMaterial.shader.name,queue=r.sharedMaterial.renderQueue,color=b.GetColor("_BaseColor").ToString()}));
                    }
                    ScreenCapture.CaptureScreenshot(file);requested=true;
                    var occlusion=cam.GetComponent<NoryangjinCameraOcclusion>();
                    records.Add(new{name=names[step],file,kind="stationary native visual fixture; firing disabled",position=p.transform.position.ToString(),camera=cam.transform.position.ToString(),faded=occlusion.FadedCount,hidden=occlusion.HiddenCount,opening=d.GetComponentInChildren<NoryangjinShutterEvent>().CurrentOpening,gestures=d.GetComponentInChildren<NoryangjinAuctionActivity>().GestureChanges});return;
                }
                if(requested)
                {
                    if(!File.Exists(file))return;requested=waiting=false;
                    if(step==names.Length-1){File.WriteAllText(folder+"/captures.json",Json(records));File.WriteAllText(Root+"/latest-capture.txt",folder);EditorApplication.update-=tick;EditorApplication.isPaused=true;return;}
                }
                step++;p.ApplyContinuousRoutePose(positions[step],headings[step],0);owner.SetValue(p,hold);point.SetValue(p,p.transform.position);
                if(step==1)foreach(var j in hose.jets){j.visual.gameObject.SetActive(true);j.wetPatch.SetSpraying(true);}
                if(step==2)foreach(var j in hose.jets){j.visual.gameObject.SetActive(false);j.wetPatch.SetSpraying(false);}
                if(step==3)typeof(NoryangjinShutterEvent).GetMethod("OnTriggered",Hidden).Invoke(d.GetComponentInChildren<NoryangjinShutterEvent>(),null);
                if(step==9){container.ResetForRun();container.enabled=true;}
                file=folder+"/"+names[step]+".png";waiting=true;due=EditorApplication.timeSinceStartup+(step==9?.55:1.25);
            }
            catch(Exception error){EditorApplication.update-=tick;File.WriteAllText(folder+"/error.txt",error.ToString());EditorApplication.isPaused=true;}
        };
        EditorApplication.update+=tick;return new{folder,scheduled=true};
    }
}
