using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

// Disposable Play Mode visual staging only. No scene, asset, material or project setting is saved.
public static class PreviewHighwayNativeThreeLanes
{
    const string Record="outputs/highway-native-three-lanes-2026-09-26";
    const string Prefabs="Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    const string PreviewName="Three Lane Visual Preview (temporary)";
    const float Station=1000;
    static string Json(object o)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    static void Write(string name,object value)=>File.WriteAllText(Record+"/"+name+".json",Json(value));
    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();
        if(rs.Length==0)throw new Exception("No renderers on "+go.name);
        var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    public static object Inspect()
    {
        Directory.CreateDirectory(Record);
        var r=Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var p=Object.FindFirstObjectByType<PlayerScript>(FindObjectsInactive.Include);var c=Camera.main;
        var map=r.transform;var roads=map.Find("Roads");var props=map.Find("Props");
        var info=new{playing=EditorApplication.isPlaying,scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,
            camera=c==null?null:new{path=PathOf(c.transform),position=V(c.transform.position),localPosition=V(c.transform.localPosition),rotation=V(c.transform.eulerAngles),c.fieldOfView,c.orthographic,c.orthographicSize,c.aspect,components=c.GetComponents<Component>().Select(x=>x.GetType().Name).ToArray()},
            player=p==null?null:new{path=PathOf(p.transform),position=V(p.transform.position),p.MaxHealth,p.currentHealth},
            points=new[]{0f,350,450,600,750}.Select(d=>{r.Sample(d,false,out var q,out var f);return new{d,position=V(q),forward=V(f)};}).ToArray(),
            roads=roads.Cast<Transform>().Take(24).Select(t=>new{name=t.name,children=t.childCount,position=V(t.position),renderers=t.GetComponentsInChildren<Renderer>(true).Take(4).Select(m=>new{m.name,type=m.GetType().Name,materials=m.sharedMaterials.Select(a=>a==null?null:AssetDatabase.GetAssetPath(a)).ToArray()}).ToArray()}).ToArray(),
            roadside=props.Cast<Transform>().Where(t=>t.name.Contains("Guard")||t.name.Contains("Barrier")||t.name.Contains("Median")||t.name.Contains("Gantry")).Select(t=>new{t.name,path=PathOf(t)}).Take(30).ToArray()};
        Write("inspection",info);return info;
    }
    public static object Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().name!="HighWay"||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean HighWay in Edit Mode required");
        Directory.CreateDirectory(Record);Directory.CreateDirectory("tmp/image-previews/highway-native-three-lanes-2026-09-26");
        var snapshot=ChapterPlaytestPreferences.SnapshotAt(Record+"/before-prefs.tsv");
        var extra=new[]{"TutorialDone"}.Concat(Enumerable.Range(1,5).SelectMany(i=>new[]{ChapterUpgradeService.LevelKey(i),ChapterUpgradeService.OwnedKey(i)}));
        File.WriteAllLines(Record+"/extra-prefs.tsv",extra.Select(k=>k+"\t"+PlayerPrefs.HasKey(k)+"\t"+PlayerPrefs.GetInt(k)));
        Write("before",new{scene=SceneManager.GetActiveScene().path,timeScale=Time.timeScale,power=SessionState.GetBool("NoryangjinMapTool.TestPower9999",false),fastLateral=SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false),testSpeed=SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1)});
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",false);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);
        return snapshot;
    }
    public static object PositionForBaseline()
    {
        if(!EditorApplication.isPlaying||!File.Exists(Record+"/before-prefs.tsv"))throw new Exception("Prepared Play Mode required");
        var player=Object.FindFirstObjectByType<PlayerScript>();var r=Object.FindFirstObjectByType<HighwayRoute>();
        OpeningStoryUI.Instance?.Skip();Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        if(!TimeManager.isGameRunning)throw new Exception("Lobby has not started yet");
        TimeManager.timeFactor=0;Time.timeScale=0;
        foreach(var c in player.GetComponentsInChildren<Collider>())c.enabled=false;
        typeof(HighwayRoute).GetProperty("Distance").SetValue(r,Station);
        r.Sample(Station,false,out var centre,out var forward);player.ApplyContinuousRoutePose(centre+Vector3.up*.12f,forward,0);
        player.canShoot=false;
        var tutorial=Object.FindFirstObjectByType<CoastalTutorialUI>();if(tutorial!=null){tutorial.Close();tutorial.enabled=false;}
        foreach(var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))harness.enabled=false;
        r.hud?.Clear(r);
        return new{station=Station,player=V(player.transform.position),camera=V(Camera.main.transform.position),freeze=true};
    }
    static GameObject Strip(Transform parent,HighwayRoute route,string name,float from,float to,float lane,float width,Material material)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();
        line.useWorldSpace=true;line.alignment=LineAlignment.TransformZ;go.transform.rotation=Quaternion.Euler(90,0,0);line.widthMultiplier=width;line.sharedMaterial=material;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
        int n=Mathf.CeilToInt((to-from)/2)+1;line.positionCount=n;
        for(int i=0;i<n;i++){route.Sample(Mathf.Lerp(from,to,i/(float)(n-1)),false,out var p,out var f);line.SetPosition(i,p+Vector3.Cross(Vector3.up,f)*lane+Vector3.up*.045f);}return go;
    }
    static void SideRoad(Transform parent,HighwayRoute route,Material asphalt)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
        for(int i=0;i<=95;i++){float d=Station-30+i*2;routed(d,out var p,out var right);
            v.Add(p+right*-17+Vector3.up*.005f);v.Add(p+right*-7.9f+Vector3.up*.005f);uv.Add(new Vector2(0,d/5));uv.Add(new Vector2(1.8f,d/5));
            if(i>0){int n=i*2;tri.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});}}
        var mesh=new Mesh{name="Temporary adjacent roadway"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var g=new GameObject("Empty adjacent roadway",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=asphalt;
        void routed(float d,out Vector3 p,out Vector3 right){route.Sample(d,false,out p,out var f);right=Vector3.Cross(Vector3.up,f);}
    }
    static void Car(Transform parent,HighwayRoute route,string file,float d,float lane,float width)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+file+".prefab");if(source==null)throw new Exception(file);
        var go=Object.Instantiate(source,parent);go.name="Preview_"+file;go.SetActive(true);go.transform.rotation=Quaternion.identity;
        foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
        foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
        foreach(var renderer in go.GetComponentsInChildren<Renderer>(true)){renderer.enabled=true;renderer.forceRenderingOff=false;}
        foreach(var collider in go.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        var b=BoundsOf(go);go.transform.localScale*=width/Mathf.Max(.01f,b.size.x);
        route.Sample(d,false,out var p,out var f);go.transform.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*lane,Quaternion.LookRotation(-f));
        b=BoundsOf(go);go.transform.position+=Vector3.up*(p.y+.04f-b.min.y);
    }
    public static object Stage()
    {
        if(!EditorApplication.isPlaying||GameObject.Find(PreviewName)!=null)throw new Exception("Fresh prepared Play Mode preview required");
        Time.timeScale=0;TimeManager.timeFactor=0;
        var r=Object.FindFirstObjectByType<HighwayRoute>();var map=r.transform;var roads=map.Find("Roads");var props=map.Find("Props");
        var created=new GameObject(PreviewName);var root=created.transform;
        var asphalt=roads.GetComponentsInChildren<MeshRenderer>().First(x=>x.name.StartsWith("Main_")).sharedMaterial;
        var nativeWhite=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/ApprovedRoadConcepts/RoadWhite.mat")??throw new Exception("Native road paint missing");
        var white=new Material(nativeWhite){name="Temporary native white lane paint"};
        if(white.HasProperty("_BaseColor"))white.SetColor("_BaseColor",new Color(.95f,.95f,.88f));if(white.HasProperty("_Color"))white.SetColor("_Color",new Color(.95f,.95f,.88f));
        // Existing surface, shading and scenery stay; replace only the lane paint for this view.
        int hiddenLines=0;foreach(var line in roads.GetComponentsInChildren<LineRenderer>(true)){line.enabled=false;hiddenLines++;}
        foreach(var road in roads.Cast<Transform>().Where(t=>t.name.StartsWith("Bypass_")||t.name.StartsWith("Opposing_")).ToArray())road.gameObject.SetActive(false);
        foreach(var script in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if(script is OncomingLaneTraffic||script is AmbientTrafficPath||script is AmbientTrafficLane||script is HighwayOncomingTraffic||script is WaterDeerCrossing||script is LogTruckSpill||script is RestStopRouteGimmick)script.gameObject.SetActive(false);
        foreach(string child in new[]{"Enemies","Bonuses"}){var t=map.Find(child);if(t!=null)t.gameObject.SetActive(false);}
        // Hide nearby authored traffic/hazard/sign placements to make this a three-vehicle composition.
        int hiddenProps=0;foreach(Transform t in props)
        {
            float d=r.NearestDistance(t.position);if(d<Station-30||d>Station+120)continue;
            r.Sample(d,false,out var p,out var f);float lane=Vector3.Dot(t.position-p,Vector3.Cross(Vector3.up,f));
            if(Mathf.Abs(lane)>9)continue;
            if(t.GetComponentInChildren<HighwayHazard>(true)!=null||t.GetComponentInChildren<ObstacleStats>(true)!=null||t.name.Contains("Sign")||t.name.Contains("Gantry")||t.name.Contains("Guardrail")){t.gameObject.SetActive(false);hiddenProps++;}
        }
        for(float d=Station-30;d<Station+160;d+=9)foreach(float lane in new[]{-2.2f,2.2f})Strip(root,r,"Three-lane divider",d,d+4,lane,.16f,white);
        foreach(float lane in new[]{-6.65f,6.65f})Strip(root,r,"Main road edge",Station-30,Station+160,lane,.16f,white);
        SideRoad(root,r,asphalt);
        foreach(float lane in new[]{-11f,-14f})for(float d=Station-30;d<Station+160;d+=10)Strip(root,r,"Background lane",d,d+4,lane,.12f,white);
        // Small median made from a native gray concrete material; no new project asset.
        var concrete=props.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(x=>x.name.Contains("Support")||x.name.Contains("Pillar"))?.sharedMaterial??asphalt;
        for(float d=Station-30;d<Station+160;d+=4)
        {
            r.Sample(d,false,out var p,out var f);var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Temporary median block";g.transform.SetParent(root);g.transform.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*-7.35f+Vector3.up*.55f,Quaternion.LookRotation(f));g.transform.localScale=new Vector3(.65f,1.1f,4.08f);g.GetComponent<Renderer>().sharedMaterial=concrete;g.GetComponent<Collider>().enabled=false;
        }
        Car(root,r,"V02_white_sedan",Station+15,-4.4f,2.5f);
        Car(root,r,"V05_tour_bus",Station+26,0,2.9f);
        Car(root,r,"V04_1ton_truck",Station+37,4.4f,2.7f);
        var camera=Camera.main;Write("stage",new{station=Station,hiddenLines,hiddenProps,mainLaneCenters=new[]{-4.4f,0,4.4f},dividerOffsets=new[]{-2.2f,2.2f},nativeAssets=true,AIImage=false,gameplayValidation=false,camera=new{position=V(camera.transform.position),rotation=V(camera.transform.eulerAngles),camera.fieldOfView,camera.aspect},cars=root.Cast<Transform>().Where(t=>t.name.StartsWith("Preview_V")).Select(t=>new{t.name,position=V(t.position),rotation=V(t.eulerAngles)}).ToArray()});
        return new{temporary=true,hiddenLines,hiddenProps,station=Station,created=root.childCount};
    }
    public static object Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        var restored=ChapterPlaytestPreferences.RestoreAt(Record+"/before-prefs.tsv");
        foreach(var line in File.ReadAllLines(Record+"/extra-prefs.tsv")){var p=line.Split('\t');if(bool.Parse(p[1]))PlayerPrefs.SetInt(p[0],int.Parse(p[2]));else PlayerPrefs.DeleteKey(p[0]);}PlayerPrefs.Save();
        var mismatches=new List<string>();foreach(var line in File.ReadAllLines(Record+"/before-prefs.tsv"))
        {var p=line.Split('\t');string val=p[1]=="int"?PlayerPrefs.GetInt(p[0]).ToString(CultureInfo.InvariantCulture):p[1]=="float"?PlayerPrefs.GetFloat(p[0]).ToString("R",CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(p[0])));if(PlayerPrefs.HasKey(p[0])!=bool.Parse(p[2])||bool.Parse(p[2])&&val!=p[3])mismatches.Add(p[0]);}
        foreach(var line in File.ReadAllLines(Record+"/extra-prefs.tsv")){var p=line.Split('\t');if(PlayerPrefs.HasKey(p[0])!=bool.Parse(p[1])||bool.Parse(p[1])&&PlayerPrefs.GetInt(p[0])!=int.Parse(p[2]))mismatches.Add(p[0]);}
        var result=new{restored,mismatches,scene=SceneManager.GetActiveScene().name,dirty=SceneManager.GetActiveScene().isDirty,temporaryObjects=GameObject.Find(PreviewName)!=null,timeScale=Time.timeScale};Write("restored",result);return result;
    }
    public static object CaptureFirst()
    {
        ScreenCapture.CaptureScreenshot("tmp/image-previews/highway-native-three-lanes-2026-09-26/native-three-lanes-v2.png");
        return new{Screen.width,Screen.height,camera=V(Camera.main.transform.position),rotation=V(Camera.main.transform.eulerAngles),Camera.main.fieldOfView};
    }
    public static object CaptureBaseline()
    {
        ScreenCapture.CaptureScreenshot("tmp/image-previews/highway-native-three-lanes-2026-09-26/before-current-game-portrait.png");
        return new{Screen.width,Screen.height};
    }
    public static object CleanupCaptureAsset()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        const string generated="Assets/tmp/image-previews/highway-native-three-lanes-2026-09-26";
        string workspace=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        string resolved=Path.GetFullPath(generated);
        if(!resolved.StartsWith(workspace+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Capture cleanup escaped workspace");
        if(Directory.Exists(generated))
        {
            var allowed=new[]{"before-current-game.png","before-current-game.png.meta"};
            if(Directory.EnumerateFileSystemEntries(generated).Any(f=>!allowed.Contains(Path.GetFileName(f))))throw new Exception("Unexpected content; preserve capture directory");
            if(File.Exists(generated+"/before-current-game.png")&&!File.Exists(Record+"/rejected-stretched-baseline.png"))File.Copy(generated+"/before-current-game.png",Record+"/rejected-stretched-baseline.png");
            if(!AssetDatabase.DeleteAsset(generated))throw new Exception("Could not remove temporary capture asset");
        }
        return new{removedTemporaryCaptureAsset=!Directory.Exists(generated)};
    }
}
