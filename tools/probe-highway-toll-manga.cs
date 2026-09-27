using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class ProbeHighwayTollManga
{
    const string Output="outputs/highway-toll-manga-2026-09-27";
    static string shot { get=>SessionState.GetString("HighwayTollManga.Shot",""); set=>SessionState.SetString("HighwayTollManga.Shot",value); }
    static HighwayChapter2Controller chapter;
    static object State()=>new{playing=EditorApplication.isPlaying,dirty=SceneManager.GetActiveScene().isDirty,speedLineCount=HighwayChapter2Data.Count("speedLineCount"),pickups=Object.FindFirstObjectByType<HighwayChapter2Controller>().exitPickups.Length};
    public static object Inspect()=>State();
    public static object TollAudit()
    {
        var c=Object.FindFirstObjectByType<HighwayChapter2Controller>();var toll=c.transform.Find("HighwayChapter2_20260927/Toll exit without barriers");
        return new{names=toll.Cast<Transform>().Where(t=>t.name.Contains("ribbon")).Select(t=>t.name).ToArray(),meshes=toll.GetComponentsInChildren<MeshFilter>().Where(t=>t.name.Contains("Combined Toll LED green")||t.name.Contains("Combined Hi-pass")).Select(t=>new{name=t.name,mesh=t.sharedMesh.name,bounds=t.sharedMesh.bounds.ToString(),color=t.GetComponent<Renderer>().sharedMaterial.color.ToString(),shader=t.GetComponent<Renderer>().sharedMaterial.shader.name,normals=t.sharedMesh.normals.Take(9).Select(n=>n.ToString()).ToArray()}).ToArray()};
    }
    public static object ExistingTestsDirect()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        var results=new System.Collections.Generic.List<string>();int passed=0,failed=0;
        foreach(string name in new[]{"HighwayCombatRevisionTests","GameDataWorkbookTests"})
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);var instance=Activator.CreateInstance(type);
            foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))
            {
                var attrs=method.GetCustomAttributes(false);var cases=attrs.Where(a=>a.GetType().Name=="TestCaseAttribute").ToArray();
                if(cases.Length==0&&!attrs.Any(a=>a.GetType().Name=="TestAttribute"))continue;
                var inputs=cases.Length==0?new[]{Array.Empty<object>()}:cases.Select(a=>(object[])a.GetType().GetProperty("Arguments").GetValue(a)).ToArray();
                foreach(var args in inputs)try{method.Invoke(instance,args);passed++;results.Add("PASS "+name+"."+method.Name+" "+string.Join(",",args));}
                catch(Exception e){failed++;results.Add("FAIL "+name+"."+method.Name+" "+(e.InnerException??e));}
            }
        }
        File.WriteAllText(Output+"/existing-tests-direct.txt","Direct invocation of existing NUnit cases after asynchronous runner stalled.\n"+string.Join("\n",results));
        if(failed>0)throw new Exception(failed+" existing test cases failed; inspect report");return new{passed,failed,mode="Direct existing NUnit method invocation; not an asynchronous runner completion"};
    }
    static void Start()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        if(!TimeManager.isGameRunning){OpeningStoryUI.Instance?.Skip();Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();}
        foreach(var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))harness.enabled=false;
        chapter=HighwayChapter2Controller.Active;var player=chapter.Player;
        foreach(var car in chapter.Vehicles)car.Retire();typeof(HighwayChapter2Controller).GetProperty("Running").SetValue(chapter,false);
        foreach(var weapon in player.GetComponentsInChildren<WeaponScript>()){weapon.CancelInvoke("ShootBullet");weapon.enabled=false;}player.canShoot=false;player.enabled=false;
    }
    static void Pose(float distance,bool bypass)
    {
        chapter=HighwayChapter2Controller.Active;
        var player=chapter.Player;var camera=Camera.main;var oldPosition=player.transform.position;var oldForward=player.transform.forward;
        var offset=camera.transform.position-oldPosition;var oldRotation=camera.transform.rotation;
        chapter.Route.Sample(distance,bypass,out var p,out var f);var turn=Quaternion.FromToRotation(oldForward,f);
        player.ApplyContinuousRoutePose(p+Vector3.up*.12f,f,0);typeof(HighwayRoute).GetProperty("Distance").SetValue(chapter.Route,distance);
        var stable=camera.GetComponent<StableGameplayCamera>();if(stable!=null)stable.enabled=false;
        camera.transform.SetPositionAndRotation(player.transform.position+turn*offset,turn*oldRotation);
        Physics.SyncTransforms();
    }
    public static object Toll()
    {
        Start();chapter.ui.SetRush(false);Pose(1888,false);shot="toll-game.png";
        return new{scope="Directed native visual capture; not route play",station=1888};
    }
    public static object TollClose()
    {
        Pose(1917,false);shot="toll-close.png";return new{station=1917};
    }
    public static object Rush()
    {
        Start();Pose(650,true);chapter.ui.SetRush(true);shot="rush-game.png";
        for(int i=0;i<3;i++)
        {
            var car=chapter.Vehicles.Where(c=>c.kind==HighwayVehicleKind.Sedan).ElementAt(i);
            car.ResetForRun();car.Activate(true,1);car.enabled=false;
            chapter.Route.Sample(670+i*8,true,out var p,out var f);car.transform.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*(i-1)*4.4f,Quaternion.LookRotation(-f));
        }
        return new{scope="Directed native rush overlay capture; vehicles frozen for visual comparison",station=650};
    }
    public static object RushOff()
    {
        chapter=HighwayChapter2Controller.Active;chapter.ui.SetRush(false);shot="rush-off.png";return new{rush=chapter.ui.RushShown};
    }
    public static object Capture()
    {
        if(string.IsNullOrEmpty(shot))throw new Exception("Select a native capture pose first");
        ScreenCapture.CaptureScreenshot(Output+"/"+shot);return new{output=Output+"/"+shot};
    }
    public static object Contracts()
    {
        Start();var checks=new System.Collections.Generic.List<string>();
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);checks.Add(label);};
        foreach(int side in new[]{0,1})
        {
            chapter.Route.BeginRun();chapter.BeginRun();typeof(HighwayRoute).GetProperty("Distance").SetValue(chapter.Route,1936f);
            chapter.Route.Sample(1936,false,out var p,out var f);chapter.Player.transform.position=p+Vector3.Cross(Vector3.up,f)*(side==0?-2.2f:2.2f)+Vector3.up*.12f;Physics.SyncTransforms();
            check(chapter.TryCollectExit(side,chapter.Player),"Physical toll reward "+side);
            check(!chapter.TryCollectExit(1-side,chapter.Player),"Exactly one exit reward "+side);
            check(chapter.ui.PopupCount==0,"No toll popup "+side);
        }
        chapter.Route.BeginRun();chapter.BeginRun();typeof(HighwayChapter2Controller).GetProperty("RoadChoice").SetValue(chapter,1);
        typeof(HighwayRoute).GetProperty("Distance").SetValue(chapter.Route,650f);chapter.ui.SetRush(chapter.RushActive);
        check(chapter.RushActive&&chapter.ui.RushShown&&Mathf.Approximately(chapter.SpeedMultiplier,1.5f),"Rush segment on and multiplier 1.5");
        var graphic=chapter.ui.GetComponentInChildren<HighwayRushLines>(true);
        check(!graphic.raycastTarget,"Overlay cannot intercept input");
        check(graphic.rectTransform.anchorMax.y<=.88f,"Effect geometry excludes the upper HUD strip");
        using(var mesh=new VertexHelper())
        {
            typeof(HighwayRushLines).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null).Invoke(graphic,new object[]{mesh});
            check(mesh.currentVertCount==HighwayChapter2Data.Count("speedLineCount")*10,"All perimeter rays have both ink and light companions");
            var vertex=new UIVertex();var rect=graphic.rectTransform.rect;var focus=rect.center+Vector2.up*rect.height*.08f;
            bool finite=true,clear=true;
            for(int i=0;i<mesh.currentVertCount;i++){mesh.PopulateUIVertex(ref vertex,i);var local=(Vector2)vertex.position-focus;finite&=!float.IsNaN(local.x)&&!float.IsNaN(local.y);clear&=Mathf.Abs(local.x)>rect.width*.20f||Mathf.Abs(local.y)>rect.height*.20f;}
            check(finite,"Finite vertices");check(clear,"Central aiming rectangle remains clear");
        }
        typeof(HighwayRoute).GetProperty("Distance").SetValue(chapter.Route,1170f);chapter.ui.SetRush(chapter.RushActive);
        check(!chapter.RushActive&&!chapter.ui.RushShown&&Mathf.Approximately(chapter.SpeedMultiplier,1),"Merge removes speed and effect");
        chapter.ui.SetRush(true);chapter.ui.enabled=false;check(!chapter.ui.RushShown,"Disabling UI clears effect");chapter.ui.enabled=true;
        typeof(HighwayChapter2Controller).GetProperty("Running").SetValue(chapter,false);
        File.WriteAllText(Output+"/contracts.txt",string.Join("\n",checks));return new{passed=checks.Count,scope="Directed assertions; no full route clear claim"};
    }
}
