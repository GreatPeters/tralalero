using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Read-only asset audit and temporary Play Mode probes. Never saves scenes/assets.
public static class S22PolishVerification
{
    const string Root = "outputs/s22-polish-2026-10-01";
    const string Images = "tmp/image-previews/s22-polish-2026-10-01";
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    static void Write(string name, object value) { Directory.CreateDirectory(Root); File.WriteAllText(Root+"/"+name+".json",Json(value)); }
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent)+"/"+t.name;
    static Mesh MeshOf(Renderer r) { if(r is SkinnedMeshRenderer s)return s.sharedMesh;var f=r.GetComponent<MeshFilter>();return f!=null?f.sharedMesh:null; }
    static long Triangles(Mesh m) { if(m==null)return 0;long n=0;for(int i=0;i<m.subMeshCount;i++)if(m.GetTopology(i)==MeshTopology.Triangles)n+=(long)m.GetIndexCount(i)/3;return n; }
    static string RegistryPath => "Software\\Unity\\UnityEditor\\"+PlayerSettings.companyName+"\\"+PlayerSettings.productName;
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
    public static object Snapshot()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        Directory.CreateDirectory(Root); Directory.CreateDirectory(Images);
        if(File.Exists(Root+"/registry-before.txt"))throw new Exception("Snapshot already exists");
        File.WriteAllText(Root+"/registry-before.txt",RegistrySnapshot());
        File.WriteAllText(Root+"/session-before.txt",string.Join("\n",new[]{SceneManager.GetActiveScene().path,AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),SessionState.GetInt("NoryangjinMapTool.TestStartStage",0).ToString(),SessionState.GetBool("NoryangjinMapTool.TestPower9999",false).ToString(),SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false).ToString(),SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1).ToString(System.Globalization.CultureInfo.InvariantCulture),OpeningStoryUI.EditorAutoPlayEnabled.ToString()}));
        Write("environment",new{utc=DateTime.UtcNow,Application.unityVersion,SystemInfo.graphicsDeviceName,SystemInfo.processorType,SystemInfo.systemMemorySize,SystemInfo.graphicsMemorySize,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],pipeline=AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline),width=Screen.width,height=Screen.height,Application.targetFrameRate,EditorUserBuildSettings.activeBuildTarget});
        return "Preferences and session preserved";
    }
    public static object Inventory()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        var report=new List<object>();
        try {
            foreach(var name in new[]{"Noryangjin_MapTool_Mode_SR18_Revamp","Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
                string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";
                var scene=EditorSceneManager.OpenScene(path);
                var roots=scene.GetRootGameObjects();var rs=roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).ToArray();
                var active=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                var materials=active.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
                var textures=materials.SelectMany(m=>m.GetTexturePropertyNames().Select(p=>m.GetTexture(p))).OfType<Texture2D>().Distinct().ToArray();
                var animators=roots.SelectMany(g=>g.GetComponentsInChildren<Animator>(true)).ToArray();
                var rows=active.Select(r=>new{path=PathOf(r.transform),mesh=AssetDatabase.GetAssetPath(MeshOf(r)),triangles=Triangles(MeshOf(r)),type=r.GetType().Name,materials=r.sharedMaterials.Length,shadow=r.shadowCastingMode.ToString(),lod=r.GetComponentInParent<LODGroup>()!=null,size=r.bounds.size.ToString(),scale=r.transform.lossyScale.ToString(),readable=MeshOf(r)?.isReadable??false}).OrderByDescending(x=>x.triangles).ToArray();
                Write(name+"-renderers",rows);
                Write(name+"-textures",textures.Select(t=>{var imp=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) as TextureImporter;var android=imp?.GetPlatformTextureSettings("Android");return new{path=AssetDatabase.GetAssetPath(t),t.width,t.height,format=t.format.ToString(),bytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t),streaming=t.streamingMipmaps,readable=t.isReadable,mipmaps=t.mipmapCount,androidOverride=android?.overridden,maxSize=android?.maxTextureSize,androidFormat=android?.format.ToString()};}).OrderByDescending(x=>x.bytes).ToArray());
                report.Add(new{scene=name,renderers=rs.Length,activeRenderers=active.Length,triangles=rows.Sum(x=>x.triangles),skinned=active.Count(r=>r is SkinnedMeshRenderer),materials=materials.Length,textures=textures.Length,textureEditorBytes=textures.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)),animators=animators.Length,activeAnimators=animators.Count(a=>a.isActiveAndEnabled),lodGroups=roots.Sum(g=>g.GetComponentsInChildren<LODGroup>(true).Length),lights=roots.Sum(g=>g.GetComponentsInChildren<Light>(true).Count(l=>l.isActiveAndEnabled)),meshColliders=roots.Sum(g=>g.GetComponentsInChildren<MeshCollider>(true).Count(c=>c.enabled&&c.gameObject.activeInHierarchy)),topMeshes=rows.Take(18)});
            }
        } finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        Write("scene-inventory",report);
        var clips=AssetDatabase.FindAssets("t:VideoClip").Select(AssetDatabase.GUIDToAssetPath).Select(p=>{var c=AssetDatabase.LoadAssetAtPath<VideoClip>(p);var im=AssetImporter.GetAtPath(p) as VideoClipImporter;return new{path=p,bytes=new FileInfo(p).Length,c.width,c.height,c.frameRate,c.length,c.frameCount,importer=im==null?null:EditorJsonUtility.ToJson(im)};}).ToArray();
        Write("videos",clips);
        var build=BuildReport.GetLatestReport();
        if(build!=null){Write("build-assets",build.packedAssets.SelectMany(p=>p.contents).GroupBy(c=>c.sourceAssetPath).Select(g=>new{path=g.Key,bytes=g.Sum(c=>(long)c.packedSize),copies=g.Count()}).OrderByDescending(x=>x.bytes).ToArray());Write("build-summary",new{build.summary.outputPath,build.summary.totalSize,build.summary.buildStartedAt,build.summary.totalErrors,build.summary.totalWarnings});}
        return new{scenes=report.Count,videos=clips.Length,folder=Root};
    }
    public static object Prepare(string scene="Noryangjin_MapTool_Mode_SR18_Revamp",bool opening=false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        if(!File.Exists(Root+"/registry-before.txt"))throw new Exception("Snapshot required");
        typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage").GetMethod("SelectStage",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{0});
        var path="Assets/ShooterSurvival/Scenes/Tools/"+scene+".unity";EditorSceneManager.OpenScene(path);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",true);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",true);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);OpeningStoryUI.EditorAutoPlayEnabled=opening;
        return new{scene,opening,testStats9999=true};
    }
    public static object Live(string label="live")
    {
        var rs=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();var cam=Camera.main;var planes=cam!=null?GeometryUtility.CalculateFrustumPlanes(cam):null;
        var videos=Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(v=>new{path=PathOf(v.transform),v.isPlaying,v.isPrepared,active=v.isActiveAndEnabled,texture=v.targetTexture!=null,clip=AssetDatabase.GetAssetPath(v.clip)}).ToArray();
        var result=new{scene=SceneManager.GetActiveScene().name,frame=Time.frameCount,Time.timeScale,TimeManager.isGameRunning,Application.isFocused,Application.targetFrameRate,Screen.width,Screen.height,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],pipeline=AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline),drawCalls=UnityStats.drawCalls,batches=UnityStats.batches,setPass=UnityStats.setPassCalls,triangles=UnityStats.triangles,vertices=UnityStats.vertices,activeRenderers=rs.Length,frustumRenderers=planes==null?0:rs.Count(r=>GeometryUtility.TestPlanesAABB(planes,r.bounds)),activeAnimators=Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Count(a=>a.isActiveAndEnabled),skinAlways=Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Count(a=>a.enabled&&a.updateWhenOffscreen),videos,player=Object.FindFirstObjectByType<PlayerScript>()?.transform.position.ToString(),camera=cam?.transform.position.ToString(),topVisible=rs.Where(r=>planes!=null&&GeometryUtility.TestPlanesAABB(planes,r.bounds)).OrderByDescending(r=>Triangles(MeshOf(r))).Take(35).Select(r=>new{path=PathOf(r.transform),triangles=Triangles(MeshOf(r)),mesh=AssetDatabase.GetAssetPath(MeshOf(r))}).ToArray()};
        Write(label,result);return result;
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
        if(File.Exists(path))throw new Exception("Do not overwrite evidence");
        ScreenCapture.CaptureScreenshot(path);return path;
    }
    public static object UiReview()
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new Exception("Fresh lobby required");
        foreach(var h in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))h.enabled=false;
        var canvas=Object.FindFirstObjectByType<CanvasScript>();var upgrades=canvas.transform.Find("UI/Upgrade2").gameObject;
        var shop=Object.FindFirstObjectByType<CosmeticShopUI>(FindObjectsInactive.Include);var settings=Object.FindFirstObjectByType<HarborSettingsPanel>(FindObjectsInactive.Include);var opening=Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);
        opening.Skip();string[] names={"ui-lobby","ui-upgrades","ui-shoes","ui-hats","ui-skins","ui-settings","ui-story"};int phase=0;bool changed=true;double due=EditorApplication.timeSinceStartup+1;
        EditorApplication.CallbackFunction tick=null;tick=()=>{
            if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}if(EditorApplication.timeSinceStartup<due)return;
            if(changed){Capture(names[phase]);phase++;changed=false;due=EditorApplication.timeSinceStartup+.3;return;}
            if(phase==names.Length){opening.Skip();EditorApplication.update-=tick;File.WriteAllText(Root+"/ui-complete.txt","Seven native runtime screens, no purchases or equipment changes.");return;}
            upgrades.SetActive(false);shop.Close();settings.Close();
            if(phase==1)upgrades.SetActive(true);if(phase==2){shop.Open();shop.SelectSlot(CosmeticSlot.Shoes);}if(phase==3){shop.Open();shop.SelectSlot(CosmeticSlot.Hat);}if(phase==4){shop.Open();shop.SelectSlot(CosmeticSlot.Skin);}if(phase==5)settings.Open(false);if(phase==6)opening.Open();
            changed=true;due=EditorApplication.timeSinceStartup+1.4;
        };EditorApplication.update+=tick;return names;
    }
    public static object Watch(string label,int seconds=120)
    {
        string folder=Images+"/"+label;Directory.CreateDirectory(folder);var p=Object.FindFirstObjectByType<PlayerScript>();var chapter=Object.FindFirstObjectByType<ChapterProgression>();
        float began=Time.time,next=0;int frame=-1;var samples=new List<object>();
        EditorApplication.CallbackFunction tick=null;tick=()=>{
            if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}if(frame==Time.frameCount)return;frame=Time.frameCount;
            float elapsed=Time.time-began;
            var ui=Object.FindFirstObjectByType<HighwayChapter2UI>();if(ui!=null&&ui.Pending&&ui.Remaining<3)ui.Select(0);
            if(elapsed>=next){next+=4;ScreenCapture.CaptureScreenshot(folder+"/frame-"+elapsed.ToString("000.0")+".png");samples.Add(new{elapsed,hp=p.currentHealth,pos=p.transform.position.ToString(),batches=UnityStats.batches,triangles=UnityStats.triangles,setPass=UnityStats.setPassCalls});}
            if(elapsed>=seconds||p.currentHealth<=0||chapter.Completed){EditorApplication.update-=tick;Write(label+"-watch",new{scene=SceneManager.GetActiveScene().name,elapsed,hp=p.currentHealth,chapter.Completed,cause=p.LastDamageCause.ToString(),samples});}
        };EditorApplication.update+=tick;return new{folder,seconds};
    }
    public static object Subject(string path="Noryangjin_Player/Original",string label="player-live")
    {
        var root=GameObject.Find(path);if(root==null)throw new Exception(path);var rs=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
        var ts=root.GetComponentsInChildren<Transform>(true);var layers=ts.Select(t=>t.gameObject.layer).ToArray();var go=new GameObject("Temporary subject camera"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.19f,.24f);cam.cullingMask=1<<31;cam.fieldOfView=30;cam.nearClipPlane=.01f;
        var rt=new RenderTexture(900,900,24);rt.Create();var old=RenderTexture.active;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);
        var dirs=new[]{new Vector3(.75f,.3f,1),Vector3.forward,Vector3.back,Vector3.left,Vector3.right,Vector3.up};var names=new[]{"hero","front","back","left","right","top"};
        try{foreach(var t in ts)t.gameObject.layer=31;for(int i=0;i<dirs.Length;i++){cam.transform.position=b.center+root.transform.rotation*dirs[i].normalized*b.size.magnitude*1.6f;cam.transform.LookAt(b.center,i==5?root.transform.forward:Vector3.up);cam.aspect=1;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(Images+"/"+label+"-"+names[i]+".png",tex.EncodeToPNG());}}
        finally{for(int i=0;i<ts.Length;i++)ts[i].gameObject.layer=layers[i];cam.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);}
        return new{label,bounds=b.size.ToString()};
    }
    public static object Compare()
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new Exception("Play lobby required");
        var cam=Camera.main;var opening=Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);
        var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var shadow=lights.Select(l=>l.shadows).ToArray();
        var actor=Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);var actorStates=actor.Select(a=>a.enabled).ToArray();
        int originalCap=Application.targetFrameRate;float far=cam.farClipPlane;bool cameraEnabled=cam.enabled;Application.targetFrameRate=-1;
        string[] labels={"video-and-world","video-only-camera-off","lobby-no-video","lobby-no-shadows","lobby-far80","lobby-no-animators","lobby-restored"};
        var all=new List<object>();var rows=new List<object>();var times=new List<double>();int phase=-1,n=0,last=-1;
        Action restore=()=>{cam.enabled=cameraEnabled;cam.farClipPlane=far;for(int i=0;i<lights.Length;i++)lights[i].shadows=shadow[i];for(int i=0;i<actor.Length;i++)if(actor[i]!=null)actor[i].enabled=actorStates[i];};
        EditorApplication.CallbackFunction tick=null;
        tick=()=>{
            if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}
            if(Time.frameCount==last)return;last=Time.frameCount;
            try{
                if(phase<0||n>=270){
                    if(phase>=0){var sorted=times.OrderBy(x=>x).ToArray();all.Add(new{label=labels[phase],frames=times.Count,meanMs=times.Average(),medianMs=sorted[sorted.Length/2],p95Ms=sorted[(int)(sorted.Length*.95)],rows=rows.ToArray()});}
                    restore();phase++;n=0;rows.Clear();times.Clear();
                    if(phase==labels.Length){EditorApplication.update-=tick;Application.targetFrameRate=originalCap;Write("compare",new{device="Windows Editor, 1080x2340, uncapped, stationary lobby",cases=all});return;}
                    if(phase==0){opening.Open();}if(phase==1)cam.enabled=false;if(phase==2)opening.Skip();if(phase==3)foreach(var l in lights)l.shadows=LightShadows.None;if(phase==4)cam.farClipPlane=80;if(phase==5)foreach(var a in actor)a.enabled=false;
                }
                n++;if(n<=90)return;
                double ms=Time.unscaledDeltaTime*1000d;times.Add(ms);rows.Add(new{ms,batches=UnityStats.batches,triangles=UnityStats.triangles,draws=UnityStats.drawCalls,setPass=UnityStats.setPassCalls,video=opening.IsMoviePlaying,camera=cam.enabled,Application.isFocused});
            }catch(Exception ex){restore();Application.targetFrameRate=originalCap;EditorApplication.update-=tick;File.WriteAllText(Root+"/compare-error.txt",ex.ToString());}
        };EditorApplication.update+=tick;return new{cases=labels};
    }
    public static object Sample(string label="baseline",int warmup=90,int frames=240)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        int last=-1,n=0;var values=new List<object>();var times=new List<double>();int oldCap=Application.targetFrameRate;Application.targetFrameRate=-1;
        EditorApplication.CallbackFunction tick=null;tick=()=>{
            if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}
            if(Time.frameCount==last)return;last=Time.frameCount;
            if(n++<warmup)return;
            times.Add(Time.unscaledDeltaTime*1000d);values.Add(new{ms=Time.unscaledDeltaTime*1000d,batches=UnityStats.batches,draws=UnityStats.drawCalls,triangles=UnityStats.triangles,setPass=UnityStats.setPassCalls});
            if(times.Count<frames)return;EditorApplication.update-=tick;Application.targetFrameRate=oldCap;
            var sorted=times.OrderBy(x=>x).ToArray();Write(label+"-samples",values);Write(label+"-timing",new{scene=SceneManager.GetActiveScene().name,frames,warmup,meanMs=times.Average(),medianMs=sorted[sorted.Length/2],p95Ms=sorted[(int)(sorted.Length*.95)],Time.timeScale,Application.isFocused,width=Screen.width,height=Screen.height,device="Windows Editor; not S22"});
        };EditorApplication.update+=tick;return new{label,frames,warmup};
    }
    public static object OcclusionCost(string label="occlusion-cost")
    {
        var oc=Object.FindFirstObjectByType<NoryangjinCameraOcclusion>();if(oc==null)return "No component";
        var rows=new List<double>();for(int i=0;i<53;i++){var sw=System.Diagnostics.Stopwatch.StartNew();oc.RefreshVisibility(.0166667f);sw.Stop();if(i>=3)rows.Add(sw.Elapsed.TotalMilliseconds);}
        var result=new{scene=SceneManager.GetActiveScene().name,invocations=rows.Count,meanMs=rows.Average(),medianMs=rows.OrderBy(x=>x).ElementAt(25),maxMs=rows.Max(),note="Synchronous component microbenchmark, not player frame time",rows};Write(label,result);return result;
    }
    public static object CompareOcclusion(string label="occlusion-ab")
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        var component=Object.FindFirstObjectByType<NoryangjinCameraOcclusion>();var p=Object.FindFirstObjectByType<PlayerScript>();
        float oldScale=Time.timeScale;int oldCap=Application.targetFrameRate;bool oldEnabled=component.enabled;Time.timeScale=0;Application.targetFrameRate=-1;
        var samples=new List<object>();var all=new List<object>();int phase=0,n=0,last=-1;
        var field=component.GetType().GetField("candidateGroups",BindingFlags.Instance|BindingFlags.NonPublic);int groups=((System.Collections.ICollection)field.GetValue(component)).Count;
        EditorApplication.CallbackFunction tick=null;tick=()=>{
            if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}if(Time.frameCount==last)return;last=Time.frameCount;
            try{
                n++;if(n>90)samples.Add(new{ms=Time.unscaledDeltaTime*1000d,batches=UnityStats.batches,triangles=UnityStats.triangles,Application.isFocused});
                if(n<330)return;all.Add(new{phase,enabled=component.enabled,frames=samples.ToArray()});samples.Clear();n=0;phase++;
                if(phase==6){component.enabled=oldEnabled;Time.timeScale=oldScale;Application.targetFrameRate=oldCap;EditorApplication.update-=tick;Write(label,new{scene=SceneManager.GetActiveScene().name,groups,position=p.transform.position.ToString(),note="Stationary paused simulation, rendering active; repeated on/off, no screenshots within sample windows; Windows Editor",cases=all});return;}
                component.enabled=phase%2==0;
            }catch(Exception ex){component.enabled=oldEnabled;Time.timeScale=oldScale;Application.targetFrameRate=oldCap;EditorApplication.update-=tick;File.WriteAllText(Root+"/"+label+"-error.txt",ex.ToString());}
        };EditorApplication.update+=tick;return new{label,groups};
    }
    public static object Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        var saved=File.ReadAllLines(Root+"/session-before.txt");
        typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage").GetMethod("SelectStage",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{0});
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(saved[1]);EditorSceneManager.OpenScene(saved[0]);
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",bool.Parse(saved[3]));SessionState.SetBool("NoryangjinMapTool.TestFastLateral",bool.Parse(saved[4]));SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",float.Parse(saved[5],System.Globalization.CultureInfo.InvariantCulture));OpeningStoryUI.EditorAutoPlayEnabled=bool.Parse(saved[6]);
        typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage").GetMethod("SelectStage",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{int.Parse(saved[2])});
        var snapshot=File.ReadAllText(Root+"/registry-before.txt");var rows=snapshot.Split('\n').Where(s=>s.Length>0).Select(s=>s.Split('\t')).ToArray();
        using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryPath,true)){
            var names=new HashSet<string>(rows.Select(r=>System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(r[0]))));
            foreach(var name in key.GetValueNames())if(!names.Contains(name)){var m=System.Text.RegularExpressions.Regex.Match(name,@"^(.*)_h\d+$");if(m.Success)PlayerPrefs.DeleteKey(m.Groups[1].Value);}
            PlayerPrefs.Save();foreach(var name in key.GetValueNames())if(!names.Contains(name))key.DeleteValue(name,false);
            foreach(var row in rows){var name=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(row[0]));var kind=(Microsoft.Win32.RegistryValueKind)int.Parse(row[1]);var b=Convert.FromBase64String(row[2]);object v;
                if(kind==Microsoft.Win32.RegistryValueKind.DWord)v=BitConverter.ToInt32(b,0);else if(kind==Microsoft.Win32.RegistryValueKind.QWord)v=BitConverter.ToInt64(b,0);else if(kind==Microsoft.Win32.RegistryValueKind.MultiString)v=System.Text.Encoding.Unicode.GetString(b).TrimEnd('\0').Split('\0');else if(kind==Microsoft.Win32.RegistryValueKind.String||kind==Microsoft.Win32.RegistryValueKind.ExpandString)v=System.Text.Encoding.Unicode.GetString(b);else v=b;key.SetValue(name,v,kind);}
            key.Flush();
        }
        var result=new{registryByteIdentical=snapshot==RegistrySnapshot(),scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty};Write("restored",result);return result;
    }
    public static object VerifyRestored()
    {
        var snapshot=File.ReadAllText(Root+"/registry-before.txt");var rows=snapshot.Split('\n').Select(x=>x.Split('\t')).ToArray();var results=new List<object>();
        foreach(var key in new[]{"coin","jewel"}){
            var row=rows.First(r=>System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(r[0])).StartsWith(key+"_h"));int expected=BitConverter.ToInt32(Convert.FromBase64String(row[2]),0);int before=PlayerPrefs.GetInt(key);if(before!=expected)PlayerPrefs.SetInt(key,expected);results.Add(new{key,expected,before,after=PlayerPrefs.GetInt(key)});
        }
        PlayerPrefs.Save();CosmeticService.ReloadCatalog();var result=new{registryByteIdentical=snapshot==RegistrySnapshot(),wallet=results,scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,play=EditorApplication.isPlaying};Write("restore-verified",result);return result;
    }
}
