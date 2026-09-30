using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PlayNoryangjinFeedbackV3
{
    const string Root = "outputs/noryangjin-feedback-v3-2026-09-28";
    const string Preview = "tmp/image-previews/noryangjin-feedback-v3-2026-09-28";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    public static object Inside()=>Begin("inside-"+DateTime.Now.ToString("HHmmss"),0,true,1);
    public static object Outside()=>Begin("outside-"+DateTime.Now.ToString("HHmmss"),1,true,1);
    public static object Begin(string label, int route, bool debugReview = false,float reviewTimeScale=1)
    {
        if (!EditorApplication.isPlaying || TimeManager.isGameRunning) throw new Exception("Play start screen required");
        var player = Object.FindFirstObjectByType<PlayerScript>();
        var director = Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        var progression = Object.FindFirstObjectByType<ChapterProgression>();
        var move = player.GetType().GetMethod("PlayerMove",Flags);
        var laneOrigin = player.GetType().GetField("routeLaneOrigin",Flags);
        var routeRight = player.GetType().GetField("routeRight",Flags);
        var waves = director.GetComponentsInChildren<NoryangjinWaveEvent>();
        var hoses = director.GetComponentsInChildren<NoryangjinHoseEvent>();
        var incidents = director.GetComponentsInChildren<NoryangjinMarketIncident>();
        var bonuses = Object.FindObjectsByType<BonusWallChoicePair>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var hazards = GameObject.Find("Noryangjin_MapTool/Props").transform.Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G")).ToArray();
        string folder = Preview + "/play-" + label; Directory.CreateDirectory(folder);
        File.WriteAllText(folder+"/samples.csv","elapsed,hp,maxhp,x,y,z,branch,blocked\n");
        File.WriteAllText(folder+"/frames.csv","frame,elapsed,x,y,z,yaw\n");
        int frame=-1,seen=0,shots=0;float nextShot=0,nextSample=0,nextTargetScan=0;bool done=false,debugObserved=false;double stopAt=0;
        var aimTargets=Array.Empty<EnemyScript_space>();
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
            if(!EditorApplication.isPlaying||player==null){EditorApplication.update-=tick;return;}
            if(done){if(EditorApplication.timeSinceStartup>stopAt){EditorApplication.update-=tick;EditorApplication.isPaused=true;Time.timeScale=1;}return;}
            if(frame==Time.frameCount||EditorApplication.isPaused)return;frame=Time.frameCount;
            debugObserved |= SessionState.GetBool("NoryangjinMapTool.TestPower9999",false) || player.ResolvedAttackDamage >= 9999;
            if(director.Branch.choiceUI.Pending&&director.Branch.choiceUI.Remaining<3)director.Branch.choiceUI.Select(route);
            if(director.Elapsed>=nextSample)
            {
                nextSample+=.5f;var at=player.transform.position;
                File.AppendAllText(folder+"/samples.csv",$"{director.Elapsed:F2},{player.currentHealth:F1},{player.MaxHealth:F1},{at.x:F2},{at.y:F2},{at.z:F2},{director.Branch.Distance:F1},{director.Blocked}\n");
            }
            if(director.Elapsed>=nextShot||director.Timeline.Count>seen)
            {
                nextShot=director.Elapsed+(label.StartsWith("capture-final")?2:debugReview?4:8);seen=director.Timeline.Count;
                string frameName="frame-"+shots++.ToString("D3");
                ScreenCapture.CaptureScreenshot(folder+"/"+frameName+".png");
                var pose=player.transform.position;
                File.AppendAllText(folder+"/frames.csv",$"{frameName},{director.Elapsed:F2},{pose.x:F2},{pose.y:F2},{pose.z:F2},{player.transform.eulerAngles.y:F1}\n");
            }
            if(player.currentHealth<=0||progression.Completed||director.Elapsed>480)
            {
                done=true;stopAt=EditorApplication.timeSinceStartup+1;
                File.WriteAllText(folder+"/timeline.txt",string.Join("\n",director.Timeline));
                var result=new{route,elapsed=director.Elapsed,health=player.currentHealth,maximum=player.MaxHealth,attack=player.ResolvedAttackDamage,
                    completed=progression.Completed,cause=player.LastDamageCause.ToString(),branchSelected=director.Branch.Selected,branchComplete=director.Branch.Complete,
                    suppressedActive=director.GetComponent<NoryangjinRevampSuppressor>().targets.Count(t=>t!=null&&t.activeSelf),
                    debugObserved,fastLateral=SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false),requestedTimeScale=debugReview?reviewTimeScale:3,prefMode=debugObserved?"9999 override; mechanic/presentation coverage only":"saved upgrade levels; no health pin or healing",timeline=director.Timeline.ToArray()};
                File.WriteAllText(folder+"/result.json",Json(result));
                return;
            }
            if(!TimeManager.isGameRunning||player.IsWorldYawTurnActive)return;
            float desired=0,nearest=25;var origin=(Vector3)laneOrigin.GetValue(player);var right=(Vector3)routeRight.GetValue(player);
            var forward=Vector3.ProjectOnPlane(player.transform.forward,Vector3.up).normalized;
            foreach(var pair in bonuses)
            {
                if(pair==null||pair.Left==null||pair.Right==null||pair.Selected!=null||!pair.Left.gameObject.activeInHierarchy)continue;
                Vector3 delta=(pair.Left.transform.position+pair.Right.transform.position)*.5f-player.transform.position;float ahead=Vector3.Dot(delta,forward);
                if(ahead < -1 || ahead>nearest||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,right))>5)continue;
                nearest=ahead;desired=Mathf.Clamp(Vector3.Dot(pair.Left.transform.position-origin,right),-1.8f,1.8f);
            }
            if(debugReview)
            {
                if(director.Elapsed>=nextTargetScan){aimTargets=Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None);nextTargetScan=director.Elapsed+.2f;}
                float best=30,currentLane=Vector3.Dot(player.transform.position-origin,right);
                foreach(var enemy in aimTargets)
                {
                    if(enemy==null||enemy.IsDead||!enemy.gameObject.activeInHierarchy)continue;
                    var delta=enemy.transform.position-player.transform.position;float ahead=Vector3.Dot(delta,forward),lane=Vector3.Dot(enemy.transform.position-origin,right);
                    if(ahead<1||ahead>26||Mathf.Abs(lane)>2.2f||Mathf.Abs(delta.y)>3)continue;
                    float priority=ahead+Mathf.Abs(lane-currentLane)*2;
                    if(priority<best){best=priority;desired=Mathf.Clamp(lane,-1.8f,1.8f);}
                }
            }
            float nearestHazard=16;
            foreach(var hazard in hazards)
            {
                if(hazard==null||!hazard.gameObject.activeInHierarchy)continue;
                var colliders=hazard.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.bounds.size.sqrMagnitude>.001f&&c.bounds.min.y<player.transform.position.y+3&&c.bounds.max.y>player.transform.position.y).ToArray();
                var hazardPoint=hazard.position;
                if(colliders.Length>0){var bounds=colliders[0].bounds;foreach(var c in colliders)bounds.Encapsulate(c.bounds);hazardPoint=bounds.center;}
                var delta=hazardPoint-player.transform.position;float ahead=Vector3.Dot(delta,forward);
                if(ahead< -2||ahead>15||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,right))>5)continue;
                if(ahead>=nearestHazard)continue;nearestHazard=ahead;
                desired=Vector3.Dot(hazardPoint-origin,right)>=0?-1.8f:1.8f;
            }
            foreach(var wave in waves)foreach(var band in wave.waves)
                if(band.warning!=null&&band.warning.gameObject.activeInHierarchy&&Mathf.Abs(wave.Lateral(player.transform.position))<5)desired=-band.side*1.8f;
            foreach(var hose in hoses)if(hose.Triggered&&Mathf.Abs(hose.Lateral(player.transform.position))<5)
                foreach(var jet in hose.jets){float gap=jet.along+hose.Ahead(player.transform.position);if(gap> -1&&gap<9&&jet.visual!=null&&jet.visual.gameObject.activeSelf)desired=-jet.side*1.8f;}
            foreach(var incident in incidents)
                if(incident.Triggered&&incident.Ahead(player.transform.position)>-8&&incident.Ahead(player.transform.position)<24&&Mathf.Abs(incident.Lateral(player.transform.position))<5)
                    desired=incident.lane<0?1.8f:-1.8f;
            float current=Vector3.Dot(player.transform.position-origin,right);float error=desired-current;
            if(Mathf.Abs(error)>.05f)move.Invoke(player,new object[]{Mathf.Sign(error)*Mathf.Min(15,Mathf.Abs(error)*12)});
            }
            catch(Exception error)
            {
                EditorApplication.update-=tick;
                File.WriteAllText(folder+"/harness-error.txt",error.ToString());
                Time.timeScale=1;
            }
        };
        EditorApplication.update+=tick;
        // Synchronize actual saved upgrade levels through the game's own path, never a health pin.
        UpgradeStatManager.S?.SyncFromPurchasedLevels();
        var overrides=typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestOverrides");
        overrides.GetMethod("Select",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{debugReview,debugReview});
        foreach(var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))harness.enabled=false;
        Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();
        Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();Time.timeScale=debugReview?reviewTimeScale:3;
        File.WriteAllText(Root+"/active-run.txt",folder);
        return new{folder,route,health=player.currentHealth,maximum=player.MaxHealth,attack=player.ResolvedAttackDamage};
    }
}
