using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Play Mode captures for the reference-fit player. Mirrors the S22 snapshot /
// restore protocol (tools/s22-polish-verification.cs) with its own snapshot so
// the user's saves are restored byte-for-byte after Play.
public static class TralaleroReferenceVerification
{
    const string Root="outputs/tralalero-reference-2026-10-01";
    const string Images="tmp/image-previews/tralalero-reference-2026-10-01";
    static string RegistryPath=>"Software\\Unity\\UnityEditor\\"+PlayerSettings.companyName+"\\"+PlayerSettings.productName;
    static string RegistrySnapshot()
    {
        using var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryPath,false);
        return string.Join("\n",key.GetValueNames().OrderBy(n=>n,StringComparer.Ordinal).Select(n=>{
            var kind=key.GetValueKind(n);var value=key.GetValue(n,null,Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames);byte[] b;
            if(kind==Microsoft.Win32.RegistryValueKind.DWord)b=BitConverter.GetBytes(unchecked((int)Convert.ToInt64(value)));
            else if(kind==Microsoft.Win32.RegistryValueKind.QWord)b=BitConverter.GetBytes(Convert.ToInt64(value));
            else if(value is byte[] raw)b=raw;else if(value is string[] many)b=System.Text.Encoding.Unicode.GetBytes(string.Join("\0",many)+"\0");
            else b=System.Text.Encoding.Unicode.GetBytes(Convert.ToString(value)??"");
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(n))+"\t"+(int)kind+"\t"+Convert.ToBase64String(b);
        }));
    }
    static void SelectStage(int stage)=>typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage").GetMethod("SelectStage",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{stage});

    public static object Snapshot()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        Directory.CreateDirectory(Root);Directory.CreateDirectory(Images);
        if(File.Exists(Root+"/registry-before.txt"))throw new Exception("Snapshot already exists");
        File.WriteAllText(Root+"/registry-before.txt",RegistrySnapshot());
        File.WriteAllText(Root+"/session-before.txt",string.Join("\n",new[]{SceneManager.GetActiveScene().path,AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),SessionState.GetInt("NoryangjinMapTool.TestStartStage",0).ToString(),SessionState.GetBool("NoryangjinMapTool.TestPower9999",false).ToString(),SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false).ToString(),SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1).ToString(System.Globalization.CultureInfo.InvariantCulture),OpeningStoryUI.EditorAutoPlayEnabled.ToString()}));
        return new{coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel")};
    }
    public static object Prepare(string scene="Noryangjin_MapTool_Mode_SR18_Revamp")
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        if(!File.Exists(Root+"/registry-before.txt"))throw new Exception("Snapshot required");
        SelectStage(0);var path="Assets/ShooterSurvival/Scenes/Tools/"+scene+".unity";EditorSceneManager.OpenScene(path);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",true);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",true);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);OpeningStoryUI.EditorAutoPlayEnabled=false;
        return new{scene};
    }
    // Six orthogonal-ish views of the live (cosmetics applied) player.
    public static object Subject(string label="player",string path="Noryangjin_Player/Original")
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        var root=GameObject.Find(path);if(root==null)throw new Exception(path);var rs=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
        var ts=root.GetComponentsInChildren<Transform>(true);var layers=ts.Select(t=>t.gameObject.layer).ToArray();var go=new GameObject("Temporary subject camera"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.19f,.24f);cam.cullingMask=1<<31;cam.fieldOfView=30;cam.nearClipPlane=.01f;
        var rt=new RenderTexture(900,900,24);rt.Create();var old=RenderTexture.active;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);Directory.CreateDirectory(Images);
        var dirs=new[]{new Vector3(.75f,.3f,1),Vector3.forward,Vector3.back,Vector3.left,Vector3.right,new Vector3(0,1.1f,-1.25f)};var names=new[]{"hero","front","back","left","right","rear-high"};
        try{foreach(var t in ts)t.gameObject.layer=31;for(int i=0;i<dirs.Length;i++){cam.transform.position=b.center+root.transform.rotation*dirs[i].normalized*b.size.magnitude*1.6f;cam.transform.LookAt(b.center,Vector3.up);cam.aspect=1;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(Images+"/"+label+"-"+names[i]+".png",tex.EncodeToPNG());}}
        finally{for(int i=0;i<ts.Length;i++)ts[i].gameObject.layer=layers[i];cam.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);}
        return new{label,bounds=b.size.ToString()};
    }
    public static object Start()
    {
        OpeningStoryUI.Instance?.Skip();
        Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        var t=Object.FindFirstObjectByType<CoastalTutorialUI>();if(t!=null)t.Close();
        foreach(var h in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))h.enabled=false;
        Time.timeScale=1;return new{TimeManager.isGameRunning};
    }
    public static object Capture(string label)
    {
        Directory.CreateDirectory(Images);string path=Images+"/"+label+".png";
        if(File.Exists(path))throw new Exception("Do not overwrite evidence "+path);
        ScreenCapture.CaptureScreenshot(path);return path;
    }
    // Capture every fitted shoe style on the live rig, then restore the equipped look.
    public static object Shoes(string label="shoes")
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        var host=new GameObject("Reference shoe review"){hideFlags=HideFlags.HideAndDontSave}.AddComponent<ShoeReviewHost>();host.StartCoroutine(ShoeRoutine(host,label));return new{label};
    }
    sealed class ShoeReviewHost:MonoBehaviour{}
    static IEnumerator ShoeRoutine(ShoeReviewHost host,string label)
    {
        var root=GameObject.Find("Noryangjin_Player/Original").transform;var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        foreach(var key in new[]{"shoes_original","shoes_ruby","shoes_mint","shoes_gold","shoes_steel","shoes_spring","shoes_relic","shoes_salvage"})
        {
            CosmeticAppearance.Apply(root,catalog,"skin_original",key,"hat_none");yield return null;yield return null;yield return new WaitForEndOfFrame();
            var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.GetComponent<SharkTailFootRig>()!=null);
            var camGo=new GameObject("Shoe fit review camera");var cam=camGo.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.fieldOfView=32;cam.transform.position=skin.bounds.center+root.rotation*new Vector3(.55f,.15f,1).normalized*6.5f;cam.transform.LookAt(skin.bounds.center);
            var rt=new RenderTexture(800,800,24);cam.targetTexture=rt;cam.aspect=1;yield return new WaitForEndOfFrame();
            var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(800,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,800),0,0);image.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Images+"/"+label+"-"+key+".png",image.EncodeToPNG());cam.targetTexture=null;rt.Release();Object.Destroy(camGo);Object.Destroy(rt);Object.Destroy(image);
        }
        root.GetComponentInParent<PlayerCosmeticCustomizer>()?.RefreshAppearance();
        File.WriteAllText(Root+"/"+label+"-complete.txt","8 shoe styles captured");Object.Destroy(host.gameObject);
    }
    // Side-view frames of the live, animated player while the run is going.
    public static object Stride(string label="stride",int frames=12,float interval=.06f)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        var host=new GameObject("Reference stride review"){hideFlags=HideFlags.HideAndDontSave}.AddComponent<ShoeReviewHost>();host.StartCoroutine(StrideRoutine(host,label,frames,interval));return new{label,frames};
    }
    static IEnumerator StrideRoutine(ShoeReviewHost host,string label,int frames,float interval)
    {
        var root=GameObject.Find("Noryangjin_Player/Original");var ts=root.GetComponentsInChildren<Transform>(true);
        var camGo=new GameObject("Stride review camera");var cam=camGo.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.19f,.24f);cam.cullingMask=1<<31;cam.fieldOfView=30;cam.aspect=1;
        var rt=new RenderTexture(700,700,24);rt.Create();var tex=new Texture2D(700,700,TextureFormat.RGB24,false);
        for(int i=0;i<frames;i++)
        {
            yield return new WaitForEndOfFrame();
            var layers=ts.Select(t=>t.gameObject.layer).ToArray();
            var rs=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            foreach(var t in ts)t.gameObject.layer=31;
            cam.transform.position=b.center-root.transform.right*b.size.magnitude*1.5f+Vector3.up*.3f;cam.transform.LookAt(b.center,Vector3.up);cam.targetTexture=rt;cam.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,700,700),0,0);tex.Apply();RenderTexture.active=old;
            for(int k=0;k<ts.Length;k++)ts[k].gameObject.layer=layers[k];
            File.WriteAllBytes(Images+"/"+label+"-"+i.ToString("00")+".png",tex.EncodeToPNG());
            yield return new WaitForSeconds(interval);
        }
        cam.targetTexture=null;rt.Release();Object.Destroy(camGo);Object.Destroy(rt);Object.Destroy(tex);
        File.WriteAllText(Root+"/"+label+"-complete.txt",frames+" frames");Object.Destroy(host.gameObject);
    }
    // Jump the highway run to just before the single-log event, then measure every
    // frame how far the log's lowest vertex sits below the asphalt (route + .12).
    public static object LogWatch(string label="log",float seconds=9f,float lead=12f)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        var c=Object.FindFirstObjectByType<HighwayChapter2Controller>();if(c==null||!c.Running)throw new Exception("Highway run required");
        var route=c.Route;float start=HighwayChapter2Data.Value("logStart");
        float to=Mathf.Max(route.Distance,start-lead);typeof(HighwayRoute).GetProperty("Distance").SetValue(route,to);
        // Move the player with the route, otherwise the next Advance reads a huge lateral offset.
        route.Sample(to,false,out var centre,out var forward);c.Player.transform.SetPositionAndRotation(centre+Vector3.up*.12f,Quaternion.LookRotation(forward));
        var host=new GameObject("Reference log review"){hideFlags=HideFlags.HideAndDontSave}.AddComponent<ShoeReviewHost>();host.StartCoroutine(LogRoutine(host,c,label,seconds));
        return new{label,from=route.Distance,logStart=start,logSeconds=HighwayChapter2Data.Value("logSeconds"),logSpeed=HighwayChapter2Data.Value("logSpeed")};
    }
    static IEnumerator LogRoutine(ShoeReviewHost host,HighwayChapter2Controller c,string label,float seconds)
    {
        var log=c.singleLog;var mf=log.GetComponentInChildren<MeshFilter>(true);var local=mf.sharedMesh.vertices;var world=new Vector3[local.Length];
        var rows=new List<string>{"time,active,maxPenetration,centreY,roadY"};float began=Time.time,nextShot=0;int shot=0;float worst=float.NegativeInfinity;
        var camGo=new GameObject("Log review camera");var cam=camGo.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.fieldOfView=35;cam.aspect=1;
        var rt=new RenderTexture(640,640,24);rt.Create();var tex=new Texture2D(640,640,TextureFormat.RGB24,false);
        while(Time.time-began<seconds&&EditorApplication.isPlaying)
        {
            yield return new WaitForEndOfFrame();
            if(!log.gameObject.activeInHierarchy){rows.Add((Time.time-began).ToString("F2")+",0,,,");continue;}
            var m=mf.transform.localToWorldMatrix;float minY=float.PositiveInfinity;for(int i=0;i<local.Length;i++){float y=m.MultiplyPoint3x4(local[i]).y;if(y<minY)minY=y;}
            // Nearest route station to the log (ignoring its lateral lane offset).
            float best=float.PositiveInfinity,bestD=0;var centre=log.GetComponent<BoxCollider>().bounds.center;
            for(float d=Mathf.Max(0,c.Route.Distance-40);d<=c.Route.Distance+160;d+=.5f){var q=c.Route.Point(d);var fwd=c.Route.Point(d+.5f)-q;fwd.y=0;var delta=centre-q;delta.y=0;float along=Mathf.Abs(Vector3.Dot(delta,fwd.normalized));if(along<best){best=along;bestD=d;}}
            float road=c.Route.Point(bestD).y+.12f;float pen=road-minY;worst=Mathf.Max(worst,pen);
            rows.Add((Time.time-began).ToString("F2")+",1,"+pen.ToString("F3")+","+centre.y.ToString("F3")+","+road.ToString("F3"));
            if(Time.time-began>=nextShot&&shot<16)
            {
                nextShot+=.45f;var side=Vector3.Cross(Vector3.up,(c.Route.Point(bestD+1)-c.Route.Point(bestD)).normalized);
                cam.transform.position=centre+side*9f+Vector3.up*.4f;cam.transform.LookAt(centre-Vector3.up*.4f);if(shot%2==0)ScreenCapture.CaptureScreenshot(Images+"/"+label+"-game-"+shot.ToString("00")+".png");cam.targetTexture=rt;cam.Render();
                var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();RenderTexture.active=old;
                File.WriteAllBytes(Images+"/"+label+"-"+shot.ToString("00")+".png",tex.EncodeToPNG());shot++;
            }
        }
        cam.targetTexture=null;rt.Release();Object.Destroy(camGo);Object.Destroy(rt);Object.Destroy(tex);
        File.WriteAllLines(Root+"/"+label+".csv",rows);File.WriteAllText(Root+"/"+label+"-complete.txt","worstPenetration="+worst.ToString("F3")+" frames="+(rows.Count-1));Object.Destroy(host.gameObject);
    }
    public static object Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        var saved=File.ReadAllLines(Root+"/session-before.txt");
        SelectStage(0);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(saved[1]);EditorSceneManager.OpenScene(saved[0]);
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",bool.Parse(saved[3]));SessionState.SetBool("NoryangjinMapTool.TestFastLateral",bool.Parse(saved[4]));SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",float.Parse(saved[5],System.Globalization.CultureInfo.InvariantCulture));OpeningStoryUI.EditorAutoPlayEnabled=bool.Parse(saved[6]);
        SelectStage(int.Parse(saved[2]));
        var snapshot=File.ReadAllText(Root+"/registry-before.txt");var rows=snapshot.Split('\n').Where(s=>s.Length>0).Select(s=>s.Split('\t')).ToArray();
        using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryPath,true)){
            var names=new HashSet<string>(rows.Select(r=>System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(r[0]))));
            foreach(var name in key.GetValueNames())if(!names.Contains(name)){var m=System.Text.RegularExpressions.Regex.Match(name,@"^(.*)_h\d+$");if(m.Success)PlayerPrefs.DeleteKey(m.Groups[1].Value);}
            PlayerPrefs.Save();foreach(var name in key.GetValueNames())if(!names.Contains(name))key.DeleteValue(name,false);
            foreach(var row in rows){var name=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(row[0]));var kind=(Microsoft.Win32.RegistryValueKind)int.Parse(row[1]);var b=Convert.FromBase64String(row[2]);object v;
                if(kind==Microsoft.Win32.RegistryValueKind.DWord)v=BitConverter.ToInt32(b,0);else if(kind==Microsoft.Win32.RegistryValueKind.QWord)v=BitConverter.ToInt64(b,0);else if(kind==Microsoft.Win32.RegistryValueKind.MultiString)v=System.Text.Encoding.Unicode.GetString(b).TrimEnd('\0').Split('\0');else if(kind==Microsoft.Win32.RegistryValueKind.String||kind==Microsoft.Win32.RegistryValueKind.ExpandString)v=System.Text.Encoding.Unicode.GetString(b);else v=b;key.SetValue(name,v,kind);}
            key.Flush();
        }
        CosmeticService.ReloadCatalog();
        var result=new{registryByteIdentical=snapshot==RegistrySnapshot(),coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty};
        File.WriteAllText(Root+"/restored.json",result.ToString());return result;
    }
}
