using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Seeded native presentation fixture. Saved scene actors/controllers are used unchanged.
// Prepare fresh Jamsil preferences, enter portrait Play lobby, Begin/Poll, exit Play/Restore.
// Actors are temporarily staged ahead of the native camera and the player component is
// disabled after real Start to prevent movement/fire. This is not an ordinary run.
public static class Chapters45NativeActorAnimation
{
    sealed class Subject
    {
        public EnemyScript_space actor;
        public EnemyEventController events;
        public Animator animator;
        public SkinnedMeshRenderer renderer;
        public Vector3[] attackPose, deathPose;
        public bool sawAttack, sawDie;
        public float attackMotion, deathMotion;
        public string controller;
    }
    static Subject[] subjects;
    static PlayerScript player;
    static bool wasEnabled, active;
    static int state, frame = -1, failures;
    static string folder, phase;
    static double began, stateAt, gameAt;
    static readonly List<object> checks = new();
    static readonly List<string> errors = new(), warnings = new(), images = new();
    static double Age => Time.timeAsDouble-gameAt;
    static string Json(object v) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
    static void Write(string name,object value)=>File.WriteAllText(Path.Combine(folder,name),Json(value));
    static T[] Find<T>() where T:Component=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    static void Require(string name,bool passed,object evidence)
    {
        var item=new{name,passed,evidence};checks.Add(item);File.AppendAllText(Path.Combine(folder,"checks.jsonl"),Json(item)+"\n");
        if(!passed){failures++;throw new InvalidOperationException(name);}
    }
    public static object Begin()
    {
        string prepared=SessionState.GetString("Chapter45.QA.folder","");string snapshot=Path.Combine(prepared,"before-prefs.tsv");
        if(active||!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="Jamsil"||TimeManager.isGameRunning||CanvasScript.isGameOver
            ||!File.Exists(snapshot)||File.Exists(Path.Combine(prepared,"start.json"))||File.Exists(Path.Combine(prepared,"restored.json")))
            throw new InvalidOperationException("Fresh prepared Jamsil lobby and unrestored preference snapshot required.");
        var keys=new HashSet<string>(File.ReadAllLines(snapshot).Select(l=>l.Split('\t')[0]));
        if(!new[]{"coin","jewel","chapter_unlocked"}.All(keys.Contains))throw new InvalidOperationException("Snapshot must cover wallet and campaign.");
        if(SessionState.GetBool("NoryangjinMapTool.TestPower9999",false)||SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1)!=1||Time.timeScale!=1)
            throw new InvalidOperationException("No test power/time overrides allowed.");
        player=Find<PlayerScript>().Single();wasEnabled=player.enabled;
        var overlay=Object.FindFirstObjectByType<CombatHarness>();
        if(overlay!=null)typeof(CombatHarness).GetField("overlayVisible",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.SetValue(overlay,false);
        var director=Find<Chapter45Director>().Single();var chapter=Find<ChapterProgression>().Single();
        if(director.Running||chapter.Completed||chapter.Elapsed>.01)throw new InvalidOperationException("Already used lobby.");
        var encounter=Find<Chapter45Encounter>().Where(e=>e.floor==0&&e.choiceIndex<0).OrderBy(e=>e.activationDistance).First();
        if(encounter.actors.Length!=2)throw new InvalidOperationException("Expected first authored two-actor encounter.");
        subjects=encounter.actors.Select(a=>new Subject{actor=a,events=a.GetComponent<EnemyEventController>(),animator=a.GetComponentInChildren<Animator>(true)}).ToArray();
        foreach(var s in subjects)
        {
            if(s.events==null||s.animator==null||s.animator.runtimeAnimatorController==null)throw new InvalidOperationException("Saved actor binding missing.");
            s.controller=s.animator.runtimeAnimatorController.name;
            s.renderer=s.animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r=>r.sharedMesh!=null?r.sharedMesh.vertexCount:0).First();
        }
        if(!subjects.Select(s=>s.controller).OrderBy(s=>s).SequenceEqual(new[]{"C01_patrol_police","C07_riot_police"}))
            throw new InvalidOperationException("First authored pair must use saved C01 and repaired C07 controllers; no controller substitution allowed.");
        folder=Path.Combine(prepared,"native-actor-animation");if(Directory.Exists(folder))throw new InvalidOperationException("Preserve prior receipt; prepare fresh snapshot.");
        Directory.CreateDirectory(folder);checks.Clear();errors.Clear();warnings.Clear();images.Clear();failures=0;state=0;frame=-1;
        began=stateAt=EditorApplication.timeSinceStartup;gameAt=Time.timeAsDouble;
        Require("Native portrait viewport",Screen.width>0&&Screen.height>Screen.width,new{Screen.width,Screen.height});
        var opening=Find<OpeningStoryUI>().Single();var tutorial=Find<CoastalTutorialUI>().Single();var canvas=Find<CanvasScript>().Single();
        Require("Fresh new-chapter lobby unblocked without Skip",!opening.gameObject.activeInHierarchy&&!OpeningStoryUI.IsBlockingGameplay&&canvas.isActiveAndEnabled
            &&!tutorial.enabled&&tutorial.panel!=null&&!tutorial.panel.activeInHierarchy,new{openingActive=opening.gameObject.activeInHierarchy,tutorial.enabled,canvasActive=canvas.isActiveAndEnabled});
        foreach(var s in subjects)Require(s.controller+" uses saved controller and skinned mesh",s.renderer.sharedMesh!=null,
            new{s.actor.name,s.controller,controllerAsset=AssetDatabase.GetAssetPath(s.animator.runtimeAnimatorController),modelAsset=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(s.animator.gameObject),s.renderer.sharedMesh.vertexCount});
        Write("conditions.json",new{mode="staged native actor-animation fixture",ordinaryGameplay=false,touchInput=false,snapshot,
            mutations=new[]{"Real lobby Start callback; then disable only player component to hold its position and prevent firing","First authored C01/C07 roots temporarily repositioned 11m ahead of player in existing camera view","Real EnemyEventController.ActivateFromSpot; actual EnemyScript_space.EnemyDeath, without direct Animator.Play or controller substitution"},
            persistence="No asset/scene writes; native death may award ordinary coins/score under prepared snapshot. Root must exit Play and Restore.",encounter=encounter.name,watchdogSeconds=45});
        active=true;phase="native start";Application.logMessageReceived+=OnLog;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        try
        {
            canvas.PlayerPressedStartButton();Require("Real Start begins living run",TimeManager.isGameRunning&&director.Running&&player.currentHealth>0,new{TimeManager.isGameRunning,director.Running,player.currentHealth});
            player.enabled=false;
            for(int i=0;i<subjects.Length;i++)
            {
                var s=subjects[i];s.actor.gameObject.SetActive(false);
                s.events.PrepareSpawnAt(player.transform.position+player.transform.forward*11+player.transform.right*(i==0?-2.6f:2.6f),Quaternion.LookRotation(-player.transform.forward));
                s.actor.gameObject.SetActive(true);s.actor.ApplyStat(encounter.damage,encounter.health,EnemyTier.Normal);
                Require(s.controller+" activated by real event API",s.events.ActivateFromSpot(),new{s.events.RuntimeState});
            }
            Next(0,"observe real attack_loop");
        }
        catch(Exception e){Finish(e.ToString());}
        return new{active,phase,folder};
    }
    static void Next(int next,string label){state=next;phase=label;gameAt=Time.timeAsDouble;stateAt=EditorApplication.timeSinceStartup;Status();}
    static Vector3[] Bake(Subject s)
    {
        var mesh=new Mesh();try{s.renderer.BakeMesh(mesh);var v=mesh.vertices;
            if(v.Length==0||v.Any(p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)))throw new InvalidOperationException(s.controller+" has invalid baked vertices");
            return v;}finally{Object.DestroyImmediate(mesh);}
    }
    static float Motion(Subject s,Vector3[] before)
    {var after=Bake(s);if(before.Length!=after.Length)throw new InvalidOperationException("Baked topology changed.");float max=0;for(int i=0;i<after.Length;i++)max=Mathf.Max(max,s.renderer.transform.TransformVector(after[i]-before[i]).magnitude);return max;}
    static bool State(Subject s,string name)=>s.animator.GetCurrentAnimatorStateInfo(0).shortNameHash==Animator.StringToHash(name);
    static void Capture(string label){string path=Path.Combine(folder,images.Count.ToString("D2")+"-"+label+".png");images.Add(path);ScreenCapture.CaptureScreenshot(path);}
    static void Tick()
    {
        if(!active)return;
        try
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play stopped before fixture completed.");
            if(EditorApplication.timeSinceStartup-began>45)throw new TimeoutException("Native animation fixture exceeded45s.");
            if(EditorApplication.isPaused||EditorApplication.isCompiling||frame==Time.frameCount)return;frame=Time.frameCount;
            foreach(var s in subjects)
            {
                if(s.actor.gameObject.activeInHierarchy){s.sawAttack|=State(s,"attack_loop");s.sawDie|=State(s,"die");}
            }
            if(state==0&&Age>=.35)
            {
                foreach(var s in subjects){Require(s.controller+" active animator contains real combat states",new[]{"idle","attack_loop","die"}.All(n=>s.animator.HasState(0,Animator.StringToHash(n))),new{s.controller,s.animator.enabled});
                    Require(s.controller+" reaches attack_loop",s.sawAttack&&s.animator.enabled&&s.events.RuntimeState==EnemyEventRuntimeState.Attacking,
                    new{s.animator.enabled,s.sawAttack,s.events.RuntimeState,currentHash=s.animator.GetCurrentAnimatorStateInfo(0).shortNameHash});s.attackPose=Bake(s);}
                Next(1,"measure attack deformation");
            }
            else if(state==1&&Age>=.35)
            {
                foreach(var s in subjects){s.attackMotion=Motion(s,s.attackPose);Require(s.controller+" attack visibly deforms rendered mesh",s.attackMotion>.001f,
                    new{maxVertexMotionMetres=s.attackMotion,boundsCenter=s.renderer.bounds.center.ToString(),boundsSize=s.renderer.bounds.size.ToString()});}
                Capture("C01-C07-real-attack");Next(2,"flush attack capture");
            }
            else if(state==2&&Age>=.3)
            {
                foreach(var s in subjects){s.deathPose=Bake(s);s.actor.EnemyDeath();Require(s.controller+" real death sets combat state",s.actor.IsDead&&s.events.RuntimeState==EnemyEventRuntimeState.Dead,
                    new{s.actor.IsDead,s.events.RuntimeState});}
                Next(3,"observe actual half-second death window");
            }
            else if(state==3&&Age>=.20)
            {
                foreach(var s in subjects){s.deathMotion=Motion(s,s.deathPose);Require(s.controller+" enters die and deforms before despawn",s.actor.gameObject.activeInHierarchy&&s.sawDie&&s.deathMotion>.001f,
                    new{s.sawDie,active=s.actor.gameObject.activeInHierarchy,maxVertexMotionMetres=s.deathMotion,secondsSinceDeath=Age,currentHash=s.animator.GetCurrentAnimatorStateInfo(0).shortNameHash});}
                Capture("C01-C07-real-death");Next(4,"observe native despawn");
            }
            else if(state==4&&Age>=.6)
            {
                foreach(var s in subjects)Require(s.controller+" native DeathFlow deactivates actor",!s.actor.gameObject.activeInHierarchy,new{s.actor.IsDead,active=s.actor.gameObject.activeInHierarchy});
                Require("Both native images reached disk",images.Count==2&&images.All(p=>File.Exists(p)&&new FileInfo(p).Length>1000),new{images=images.ToArray()});
                Require("No native errors or missing-state warnings",errors.Count==0&&!warnings.Any(w=>w.IndexOf("state",StringComparison.OrdinalIgnoreCase)>=0||w.IndexOf("animator",StringComparison.OrdinalIgnoreCase)>=0),
                    new{errors=errors.ToArray(),warnings=warnings.ToArray()});Finish(null);
            }
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void OnLog(string message,string stack,LogType kind)
    {
        if(kind==LogType.Warning)warnings.Add(message);
        if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)errors.Add(message);
        if(kind!=LogType.Log)File.AppendAllText(Path.Combine(folder,"logs.txt"),kind+": "+message+"\n"+stack+"\n");
    }
    static void Status()=>Write("status.json",new{active,phase,state,failures,errors=errors.Count,warnings=warnings.Count,seconds=EditorApplication.timeSinceStartup-began,folder});
    public static object Poll(){if(folder!=null)Status();return new{active,phase,state,failures,folder};}
    public static object Stop(){if(active)Finish("Stopped by root before completion");return Poll();}
    static void Finish(string exception)
    {
        active=false;EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;if(exception!=null&&failures==0)failures++;
        TimeManager.isGameRunning=false;TimeManager.timeFactor=0;if(player!=null)player.enabled=wasEnabled;phase=exception==null?"complete":"failed";
        Write("summary.json",new{passed=failures==0&&errors.Count==0&&exception==null,checks=checks.Count,failures,exception,ordinaryGameplay=false,
            errors=errors.ToArray(),warnings=warnings.ToArray(),subjects=subjects.Select(s=>new{s.actor.name,s.controller,s.sawAttack,s.sawDie,s.attackMotion,s.deathMotion}).ToArray(),
            images=images.ToArray(),restorePending=true,next="Exit Play, Restore, verify zero mismatches; independently review native PNGs."});Status();
    }
}
