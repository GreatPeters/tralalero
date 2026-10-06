using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Reproducible chapter authoring. Only new chapter scenes/assets and the explicit
// RestStop destination/build-chain integration are writable targets.
public static class InstallChapters45
{
    const string Root="Assets/ShooterSurvival/Models/Chapters/Chapters45";
    const string Scenes="Assets/ShooterSurvival/Scenes/Tools/";
    const string Generated="Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    const string Record="outputs/chapters45-2026-10-02";
    static readonly Dictionary<string,Material> materials=new();
    static Transform map,roads,scenery,actors;
    static Chapter45Route route;
    static Chapter45Director director;
    static TMP_FontAsset font;
    static EnemySO enemyData;
    static int chapter;
    static int meshIndex;
    static Material asphalt,stone,blue,glass,white,gold,navy,green,red;

    static void Guard()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle Edit Mode required.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve dirty scene before authoring.");
    }
    public static object Main()
    {
        Guard();Directory.CreateDirectory(Record);Directory.CreateDirectory(Root);
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try { Build(4); Build(5); }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return new{scenes=new[]{"Jamsil","ShoeTower"},integrationPending=true};
    }
    public static object City(){Guard();Build(4);return "Jamsil authored";}
    public static object Tower(){Guard();Build(5);return "ShoeTower authored";}
    static void Build(int number)
    {
        chapter=number;meshIndex=0;materials.Clear();
        string name=number==4?"Jamsil":"ShoeTower",path=Scenes+name+".unity";
        EditorSceneManager.OpenScene(Scenes+"Noryangjin_MapTool_Mode_SR18_Revamp.unity");
        var scene=SceneManager.GetActiveScene();
        if(!EditorSceneManager.SaveScene(scene,path,true))throw new IOException("Cannot create scoped copy.");
        EditorSceneManager.OpenScene(path);scene=SceneManager.GetActiveScene();
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");
        enemyData=AssetDatabase.LoadAssetAtPath<EnemySO>(AssetDatabase.GUIDToAssetPath("540a5d24ed1af08488d38a3679c240cb"));
        var player=Object.FindFirstObjectByType<PlayerScript>();
        foreach(var root in scene.GetRootGameObjects())
            if(root.name=="Noryangjin_MapTool"||root.name=="NoryangjinRevamp"||root.name=="Original"||root.name=="MobileOcclusionViewVolume")Object.DestroyImmediate(root);
        var follower=player.GetComponent<NoryangjinRoadHeightFollower>();if(follower!=null)Object.DestroyImmediate(follower);
        foreach(var t in Object.FindObjectsByType<ChapterEnemyStatController>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(t);
        foreach(var t in Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include,FindObjectsSortMode.None)){t.gameObject.SetActive(false);t.enabled=true;t.autoPlayMovie=true;}
        foreach(var t in Object.FindObjectsByType<CoastalTutorialUI>(FindObjectsInactive.Include,FindObjectsSortMode.None)){t.gameObject.SetActive(true);t.enabled=false;if(t.panel!=null)t.panel.SetActive(false);}
        foreach(var t in Object.FindObjectsByType<NoryangjinCameraOcclusion>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(t);
        map=Group(null,"Chapter45_World");roads=Group(map,"Roads");scenery=Group(map,"Scenery");actors=Group(map,"Encounters");
        route=map.gameObject.AddComponent<Chapter45Route>();director=map.gameObject.AddComponent<Chapter45Director>();director.chapter=chapter;director.route=route;
        SetMaterials();
        if(chapter==4)CityGeometry();else TowerGeometry();
        player.transform.SetPositionAndRotation(route.segments[0].start+Vector3.up*.12f,Quaternion.identity);
        var ps=new SerializedObject(player);ps.FindProperty("xRange").vector2Value=new Vector2(-3.5f,3.5f);ps.ApplyModifiedPropertiesWithoutUndo();
        var cam=Camera.main;cam.transform.SetParent(null,true);cam.transform.SetPositionAndRotation(player.transform.position+new Vector3(0,13,-23),Quaternion.Euler(22,0,0));cam.fieldOfView=52;cam.farClipPlane=2500;cam.nearClipPlane=.3f;
        var follow=cam.GetComponent<StableGameplayCamera>()??cam.gameObject.AddComponent<StableGameplayCamera>();follow.Configure(player.transform);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.65f,.8f,.89f);RenderSettings.fogStartDistance=chapter==4?350:170;RenderSettings.fogEndDistance=chapter==4?2100:800;
        RenderSettings.ambientLight=new Color(.62f,.68f,.75f);
        var sun=Object.FindFirstObjectByType<Light>();if(sun!=null){sun.transform.rotation=Quaternion.Euler(45,-28,0);sun.color=new Color(1,.93f,.83f);sun.intensity=1.05f;}
        ConfigureUI();AuthorEncounters();
        var progress=Object.FindFirstObjectByType<ChapterProgression>();progress.chapter=chapter;progress.nextScene=chapter==4?"ShoeTower":"";progress.nextChapterMovie=null;progress.nextChapterTitle=chapter==4?"하늘의 신발":"공물을 찾았다";progress.nextChapterCaption=chapter==4?"유리탑 안으로. 가장 높은 곳의 신발을 향해.":"더 좋은 신발을 찾았다. 가브릴렐로에게 바칠 공물이다.";
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
        File.WriteAllText(Record+"/"+name+"-authoring.json",JsonUtility.ToJson(new Receipt{scene=path,renderers=map.GetComponentsInChildren<Renderer>(true).Length,colliders=map.GetComponentsInChildren<Collider>(true).Length,enemyCount=map.GetComponentsInChildren<EnemyScript_space>(true).Length,segments=route.segments.Length},true));
    }
    [Serializable] class Receipt{public string scene;public int renderers,colliders,enemyCount,segments;}
    static Transform Group(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
    static Material Mat(string name,Color color)
    {
        if(materials.TryGetValue(name,out var cached))return cached;
        string path=Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",name.Contains("Glass")?.6f:.15f);mat.enableInstancing=true;EditorUtility.SetDirty(mat);materials[name]=mat;return mat;
    }
    static void SetMaterials()
    {
        asphalt=Mat("Asphalt",new Color(.16f,.22f,.26f));stone=Mat("WarmLimestone",new Color(.78f,.76f,.67f));blue=Mat("TowerBlue",new Color(.22f,.47f,.64f));glass=Mat("DeepGlass",new Color(.12f,.29f,.4f));white=Mat("Ivory",new Color(.93f,.94f,.9f));gold=Mat("Brass",new Color(.89f,.63f,.23f));navy=Mat("Navy",new Color(.045f,.095f,.15f));green=Mat("Jade",new Color(.13f,.58f,.46f));red=Mat("SignalCoral",new Color(.94f,.27f,.16f));
    }
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool solid=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static GameObject Cylinder(Transform parent,string name,Vector3 p,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);go.transform.position=p;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return go;
    }
    static GameObject Model(Transform parent,string prefab,Vector3 p,float scale=1,float yaw=0)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(Generated+prefab+".prefab");if(src==null)throw new FileNotFoundException(prefab);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));go.transform.localScale*=scale;return go;
    }
    static TMP_Text Sign(Transform parent,string name,string text,Vector3 p,float width,float height,float size,Color color,Quaternion? rotation=null)
    {
        var go=new GameObject(name,typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.position=p;go.transform.rotation=rotation??Quaternion.identity;
        var t=go.GetComponent<TextMeshPro>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width,height);t.enableWordWrapping=false;t.outlineWidth=0;return t;
    }
    static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material mat)
    {var go=Box(parent,name,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),mat);go.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Merge(Transform group)
    {
        var filters=group.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<MeshRenderer>()!=null&&f.GetComponent<Collider>()==null&&f.gameObject.GetComponent<TMP_Text>()==null).ToArray();
        foreach(var batch in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
        {
            if(batch.Key==null)continue;var combine=batch.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=group.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
            if(combine.Length<2)continue;
            var mesh=new Mesh{name=group.name+" "+batch.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine);mesh.RecalculateBounds();
            string path=Root+"/"+chapter+"_"+(meshIndex++).ToString("D4")+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(batch.Key.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(group,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var rr=go.GetComponent<MeshRenderer>();rr.sharedMaterial=batch.Key;rr.shadowCastingMode=ShadowCastingMode.Off;
            foreach(var f in batch){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
        }
    }
    static void Segment(Vector3 a,Vector3 b,float length,int floor,string label,List<Chapter45Route.Segment> list)
    {list.Add(new Chapter45Route.Segment{start=a,end=b,length=length,floor=floor,label=label});}
    static void Road(Vector3 a,Vector3 b,float width,Material mat)
    {
        var go=Box(roads,"Walkable",(a+b)*.5f-Vector3.up*.18f,new Vector3(width,.36f,Vector3.Distance(a,b)+.5f),mat,true);go.transform.rotation=Quaternion.LookRotation(b-a);
    }
    static void CityGeometry()
    {
        var list=new List<Chapter45Route.Segment>();
        var points=new[]{new Vector3(0,0,0),new Vector3(0,0,300),new Vector3(30,0,600),new Vector3(30,0,1000),new Vector3(0,0,1350),new Vector3(0,0,1850)};
        for(int i=0;i<points.Length-1;i++)Segment(points[i],points[i+1],Vector3.Distance(points[i],points[i+1]),0,i==0?"잠실 진입":i<3?"잠실대로":i==3?"석촌호수 산책길":"슈 타워 광장",list);
        route.segments=list.ToArray();
        foreach(var s in route.segments)Road(s.start,s.end,18,asphalt);
        var ground=Box(scenery,"CityGround",new Vector3(0,-.8f,900),new Vector3(1300,1,2300),stone);
        // Highway memory lasts only the first ~12s; downtown begins at94m.
        for(int i=0;i<12;i++)
        {
            float z=i*8;Box(scenery,"ExitGuardrailL",new Vector3(-9,.8f,z),new Vector3(.4f,.4f,7),white);Box(scenery,"ExitGuardrailR",new Vector3(9,.8f,z),new Vector3(.4f,.4f,7),white);
        }
        Box(scenery,"ExitGantry",new Vector3(0,8,55),new Vector3(21,.55f,.65f),navy);Box(scenery,"JamsilDirection",new Vector3(0,7.6f,55),new Vector3(9,2.4f,.25f),green);
        Sign(scenery,"EntrySign","잠실  ·  JAMSIL  ↑",new Vector3(0,7.6f,54.8f),9,2,5,Color.white);
        for(int i=1;i<34;i++)
        {
            float d=i*53;Sample(d,out var c,out var f);var right=Vector3.Cross(Vector3.up,f);var g=Group(scenery,"CityBlock_"+i);g.position=c;
            var visibility=g.gameObject.AddComponent<Chapter45SceneryGroup>();visibility.startDistance=d-35;visibility.endDistance=d+35;
            for(int side=-1;side<=1;side+=2)
            {
                if((d>1020&&d<1410&&side<0)||(d>510&&d<860))continue;
                var pos=c+right*(side*(25+(i%3)*3));float height=18+(i%5)*8;float width=24+(i%3)*5;
                Box(g,"CommercialBlock",pos+Vector3.up*(height*.5f),new Vector3(width,height,38),i%3==0?stone:blue);
                Box(g,"MallPodium",pos+Vector3.up*3.5f,new Vector3(width+3,7,40),stone);
                for(int k=0;k<4;k++)Box(g,"StorefrontGlazing",pos-right*side*(width*.5f+.1f)+new Vector3(0,3,-13+k*8),new Vector3(.15f,4,6.5f),glass);
                for(int row=0;row<Mathf.FloorToInt(height/4)-1;row++)Box(g,"FacadeWindowBand",pos+Vector3.up*(8+row*4),new Vector3(width+.15f,1.7f,38.15f),glass);
                Box(g,"LimestoneCornice",pos+Vector3.up*height,new Vector3(width+1,.8f,39),white);
                if(i%3==0)Model(g,"P13_apartment_block",pos+right*side*55,1.5f);
                Box(g,"Pavement",c+right*side*12-Vector3.up*.05f,new Vector3(5,.2f,54),white);
                for(int tree=0;tree<3;tree++){var p=c+right*side*13+f*(-18+tree*18);Cylinder(g,"TreeTrunk",p+Vector3.up*2,new Vector3(.45f,2,.45f),stone);Cylinder(g,"TreeCrown",p+Vector3.up*5,new Vector3(3.5f,1.5f,3.5f),green);}
                if(i%4==0)Model(g,"P06_recycle_bins",c+right*side*11+f*8,.8f,side>0?-90:90);
            }
            for(int lane=-1;lane<=1;lane+=2)Box(g,"WhiteLaneDashes",c+right*lane*3+Vector3.up*.015f,new Vector3(.12f,.03f,10),white);
            if(i<20){Box(g,"MedianGarden",c-right*8+Vector3.up*.12f,new Vector3(1.1f,.4f,46),green);}
            if(i%5==2){Box(g,"BusBay",c+right*9.8f+Vector3.up*.02f,new Vector3(4,.035f,35),blue);Model(g,"V05_tour_bus",c+right*11+f*2,1,180);}
            Merge(g);
        }
        var lake=Group(scenery,"SeokchonLake");Box(lake,"LakeSurface",new Vector3(-180,-.22f,1200),new Vector3(290,.15f,460),Mat("Lake",new Color(.2f,.62f,.71f)));
        for(int i=0;i<24;i++)Box(lake,"PromenadeFence",new Vector3(-13,1,1010+i*16),new Vector3(.18f,1,12),white);
        Merge(lake);
        foreach(float d in new[]{370f,570f,970f})Crossing(d);
        var plaza=Group(scenery,"TowerPlaza");Box(plaza,"PlazaStone",new Vector3(0,-.12f,1700),new Vector3(125,.18f,340),white);
        for(int i=0;i<12;i++)Box(plaza,"PlazaInlay",new Vector3(0,.01f,1550+i*24),new Vector3(100,.02f,.3f),stone);
        foreach(int side in new[]{-1,1}){for(int i=0;i<5;i++)Cylinder(plaza,"PlazaPlanter",new Vector3(side*24,.7f,1570+i*50),new Vector3(5,.7f,5),green);}
        Landmark(new Vector3(0,0,1880),plaza);Merge(plaza);
    }
    static void Crossing(float d)
    {
        Sample(d,out var c,out var f);var g=Group(scenery,"Intersection_"+d);Box(g,"CrossStreet",c-Vector3.up*.08f,new Vector3(160,.18f,20),asphalt);
        for(int i=-7;i<=7;i++)Box(g,"ZebraStripe",c+new Vector3(i*1.15f,.025f,-8),new Vector3(.6f,.025f,5),white);
        foreach(int side in new[]{-1,1}){Cylinder(g,"SignalPole",c+new Vector3(side*11,3,-12),new Vector3(.2f,3,.2f),navy);Box(g,"TrafficSignal",c+new Vector3(side*11,5.5f,-12),new Vector3(.55f,1.8f,.4f),navy);Box(g,"SignalAmber",c+new Vector3(side*11,5.7f,-12.22f),new Vector3(.3f,.3f,.04f),gold);}
        Merge(g);
    }
    static void Landmark(Vector3 center,Transform parent)
    {
        var g=Group(parent,"CanonicalShoeTower");g.position=center;
        // A faceted tapered glass shaft, vertical fins and splayed support arms retain the intro silhouette.
        var vertices=new List<Vector3>();var indices=new List<int>();int sides=12;
        for(int ring=0;ring<7;ring++){float y=ring*42;float radius=Mathf.Lerp(35,8,ring/6f);for(int s=0;s<sides;s++){float a=s*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius));}}
        for(int r=0;r<6;r++)for(int s=0;s<sides;s++){int a=r*sides+s,b=r*sides+(s+1)%sides,c=a+sides,e=b+sides;indices.AddRange(new[]{a,c,b,b,c,e});}
        var mesh=new Mesh{name="Tapered glazed tower"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();string path=Root+"/ShoeTowerShaft.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}
        var shaft=new GameObject("Tapered blue glass shaft",typeof(MeshFilter),typeof(MeshRenderer));shaft.transform.SetParent(g,false);shaft.GetComponent<MeshFilter>().sharedMesh=mesh;shaft.GetComponent<MeshRenderer>().sharedMaterial=blue;
        for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;Beam(g,"VerticalFin",center+new Vector3(Mathf.Cos(a)*35,0,Mathf.Sin(a)*35),center+new Vector3(Mathf.Cos(a)*8,252,Mathf.Sin(a)*8),.5f,white);}
        for(int h=10;h<250;h+=10){float radius=Mathf.Lerp(35,8,h/252f);Cylinder(g,"FloorBand",center+Vector3.up*h,new Vector3(radius*2,.11f,radius*2),glass);}
        foreach(int side in new[]{-1,1})Beam(g,"SplayedCrownSupport",center+new Vector3(0,235,0),center+new Vector3(side*27,270,0),2.8f,white);
        // Real generated sneaker crown is installed by InstallCrown after its independent asset validation.
        var anchor=Group(g,"GeneratedCrownAnchor");anchor.position=center+Vector3.up*272;
        Box(g,"EntryGlass",center+new Vector3(0,6,-36),new Vector3(30,12,.5f),glass);
        Sign(g,"TowerEntryName","슈 타워  /  SHOE TOWER",center+new Vector3(0,13,-36.4f),30,3,8,Color.white);
    }
    static void TowerGeometry()
    {
        var list=new List<Chapter45Route.Segment>();
        Segment(new Vector3(0,0,0),new Vector3(0,0,530),530,0,"1F · 로비 / 리테일 갤러리",list);
        Segment(new Vector3(0,95,530),new Vector3(0,95,1120),590,1,"58F · 아카이브 / 전망 갤러리",list);
        Segment(new Vector3(0,235,1120),new Vector3(0,235,1650),530,2,"118F · 크라운 / 신발 전시실",list);
        route.segments=list.ToArray();
        foreach(var s in route.segments)
        {
            Road(s.start,s.end,12,white);int count=Mathf.CeilToInt(s.length/45);
            for(int i=0;i<count;i++)
            {
                var c=Vector3.Lerp(s.start,s.end,Mathf.Clamp01((i*45+22)/s.length));var g=Group(scenery,"Deck_"+s.floor+"_Bay_"+i);g.position=c;
                var visibility=g.gameObject.AddComponent<Chapter45SceneryGroup>();visibility.startDistance=s.start.z+i*45-5;visibility.endDistance=s.start.z+(i+1)*45+5;visibility.floor=s.floor;
                bool forkBay=s.floor==1&&c.z>670&&c.z<985;float edge=forkBay?32:15;
                Box(g,"InteriorFloor",c-Vector3.up*.23f,new Vector3(edge*2+2,.45f,45.5f),stone);
                foreach(int side in new[]{-1,1})
                {
                    Box(g,"WindowFrame",c+new Vector3(side*edge,5,0),new Vector3(.5f,10,44),blue);
                    Box(g,"WindowSill",c+new Vector3(side*(edge-3),1.05f,0),new Vector3(.35f,2.1f,44),glass);
                    Box(g,"BrassHandrail",c+new Vector3(side*(edge-3),2.2f,0),new Vector3(.15f,.15f,44),gold);
                    for(int k=-1;k<=1;k++){Box(g,"SlenderColumn",c+new Vector3(side*(edge-4.5f),5,k*19),new Vector3(.55f,10,.55f),white);}
                    if(i%3==0)Model(g,"P06_recycle_bins",c+new Vector3(side*(edge-7),0,-8),.7f);
                    if(i%3==1&&s.floor==0)Model(g,"I04_dining_set",c+new Vector3(side*8,0,3),.9f);
                }
                Box(g,"OverheadCrossbeam",c+new Vector3(0,14,16),new Vector3(edge*2+1,.6f,.8f),white);
                // Open central camera slot; a real high canopy reads interior without hiding play.
                foreach(int side in new[]{-1,1})Box(g,"CeilingWing",c+new Vector3(side*(edge-3),14.3f,0),new Vector3(8,.4f,45),navy);
                for(int k=0;k<3;k++)Box(g,"FloorInlay",c+new Vector3(0,.02f,-15+k*15),new Vector3(11,.025f,.2f),gold);
                if(i%3==0)Sign(g,"FloorDirectory",s.label,c+new Vector3(0,7.5f,18),19,2,5,Color.white);
                Merge(g);
            }
            // Exterior city visible through the open glazed edges records elevation.
            var vista=Group(scenery,"SkylineFromDeck_"+s.floor);for(int i=0;i<18;i++){float x=(i%2==0?-1:1)*(60+i*9);float h=15+(i%6)*11;Box(vista,"DistantSeoulBlock",new Vector3(x,h*.5f,s.start.z+80+i*24),new Vector3(25,h,30),i%2==0?blue:stone);}Merge(vista);
        }
        for(int i=0;i<2;i++)
        {
            var s=route.segments[i];var g=Group(actors,"GlassLift_"+i);var lift=g.gameObject.AddComponent<Chapter45Lift>();lift.afterSegment=i;lift.duration=6;lift.destinationLabel=i==0?"58F · 전망 갤러리":"118F · 신발 크라운";
            lift.platform=Box(g,"LiftPlatform",s.end-Vector3.up*.22f,new Vector3(12,.45f,11),gold,true).transform;
            for(int side=-1;side<=1;side+=2)Beam(g,"LiftGuideRail",s.end+new Vector3(side*7,0,0),route.segments[i+1].start+new Vector3(side*7,12,0),.45f,white);
            Sign(g,"LiftSign",i==0?"급행 리프트  →  58F":"크라운 리프트  →  118F",s.end+new Vector3(0,6,5),20,2,6,Color.white);
        }
        var showroom=Group(scenery,"InsideTheSneaker");var end=route.segments[2].end;
        Box(showroom,"SoleGalleryFloor",end+new Vector3(0,-.1f,-36),new Vector3(34,.2f,85),white,true);
        foreach(int side in new[]{-1,1})
        {
            for(int i=0;i<9;i++){float z=end.z-78+i*9;float radius=14-Mathf.Abs(i-4)*.7f;Beam(showroom,"SneakerRib",new Vector3(-radius,235,z),new Vector3(-radius*.65f,245,z),.4f,white);Beam(showroom,"SneakerRib",new Vector3(radius,235,z),new Vector3(radius*.65f,245,z),.4f,white);}
            for(int i=0;i<4;i++)Cylinder(showroom,"LaceEyelet",end+new Vector3(side*9,5,-67+i*15),new Vector3(1,.2f,1),gold);
        }
        for(int i=0;i<4;i++)Beam(showroom,"ArchitecturalLace",end+new Vector3(-9,9,-67+i*15),end+new Vector3(9,9,-58+i*15),.6f,white);
        Sign(showroom,"OfferingExhibit","더 좋은 신발\n가브릴렐로에게 바칠 공물",end+new Vector3(0,6,-8),24,5,6,Color.white);
        Cylinder(showroom,"OfferingPlinth",end+new Vector3(0,.5f,-8),new Vector3(4,.5f,4),gold);Merge(showroom);
    }
    static void Sample(float distance,out Vector3 center,out Vector3 forward)
    {
        float d=distance;foreach(var s in route.segments){if(d<=s.length){center=Vector3.Lerp(s.start,s.end,d/s.length);forward=(s.end-s.start).normalized;return;}d-=s.length;}var last=route.segments.Last();center=last.end;forward=(last.end-last.start).normalized;
    }
    static void ConfigureUI()
    {
        var canvas=Object.FindFirstObjectByType<CanvasScript>();var title=canvas.transform.Find("UI/Main/Center/ChapterTitle");
        if(title!=null){foreach(var c in title.GetComponents<Component>())if(c is Image||c is AspectRatioFitter)Object.DestroyImmediate(c);foreach(var child in title.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);var label=title.GetComponent<TextMeshProUGUI>()??title.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=chapter==4?"04  잠실\n하늘의 신발을 향해":"05  슈 타워\n가장 높은 곳으로";label.fontSize=56;label.color=new Color(.95f,.91f,.78f);label.alignment=TextAlignmentOptions.Center;}
        var panel=new GameObject("Chapter45 Objective",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvas.transform,false);var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-240);rect.sizeDelta=new Vector2(820,118);panel.GetComponent<Image>().color=new Color(.025f,.065f,.105f,.9f);panel.GetComponent<Image>().raycastTarget=false;
        TMP_Text Label(string name,float y,float size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(panel.transform,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(0,y);r.sizeDelta=new Vector2(785,52);var t=go.GetComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=Color.white;t.raycastTarget=false;return t;}
        var hud=panel.AddComponent<ChapterPatternHUD>();hud.panel=panel;hud.title=Label("Title",25,30);hud.description=Label("Detail",-25,23);director.hud=hud;panel.SetActive(false);
    }
    static EnemyScript_space Enemy(Transform parent,string prefab,float distance,float lane,int floor,int index)
    {
        Sample(distance,out var c,out var f);var go=new GameObject("Security_"+index);go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(c+Vector3.Cross(Vector3.up,f)*lane+Vector3.up*.05f,Quaternion.LookRotation(-f));go.tag="EnemyTag";
        var model=Model(go.transform,prefab,go.transform.position,1,180);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
        var hit=Group(go.transform,"Walker-HitPos");hit.localPosition=Vector3.up*1.4f;
        var capsule=go.AddComponent<CapsuleCollider>();capsule.center=Vector3.up*1.2f;capsule.radius=.6f;capsule.height=2.4f;capsule.isTrigger=true;
        go.AddComponent<AudioSource>().playOnAwake=false;
        var stat=go.AddComponent<EnemyScript_space>();var so=new SerializedObject(stat);so.FindProperty("enemyData").objectReferenceValue=enemyData;so.ApplyModifiedPropertiesWithoutUndo();stat.ConfigureRewards(false,6);
        var evt=go.AddComponent<EnemyEventController>();evt.HideWhileWaiting=false;evt.EventMode=EnemyEventMode.AttackLoop;
        var healthGo=new GameObject("Health",typeof(RectTransform),typeof(Canvas));healthGo.transform.SetParent(go.transform,false);healthGo.transform.localPosition=new Vector3(0,3.2f,0);healthGo.transform.localScale=Vector3.one*.012f;healthGo.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        var textGo=new GameObject("HP",typeof(RectTransform),typeof(TextMeshProUGUI));textGo.transform.SetParent(healthGo.transform,false);var t=textGo.GetComponent<TextMeshProUGUI>();t.font=font;t.text="";t.fontSize=50;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(250,90);t.raycastTarget=false;
        return stat;
    }
    static void AuthorEncounters()
    {
        int index=0;
        var stations=chapter==4?new[]{170f,215,265,315,420,475,900,1050,1130,1430,1490,1570,1650,1720,1780}:new[]{90f,145,215,285,360,425,610,675,980,1060,1190,1260,1340,1410,1480};
        foreach(float d in stations)
        {
            int floor=chapter==4?0:d<530?0:d<1120?1:2;
            var group=Group(actors,"Encounter_"+d);var encounter=group.gameObject.AddComponent<Chapter45Encounter>();encounter.activationDistance=d-55;encounter.stopDistance=d-9;encounter.floor=floor;encounter.health=chapter==4?145:180;encounter.damage=25;encounter.coinReward=6;encounter.statPrefix=chapter==4?"c45_city":"c45_tower";
            encounter.actors=new[]{Enemy(group,chapter==4?"C01_patrol_police":"C07_riot_police",d,-2.6f,floor,index++),Enemy(group,chapter==4?"C07_riot_police":"C01_patrol_police",d,2.6f,floor,index++)};
            encounter.requiredToProceed=(d==1780||d==1480);encounter.announcement=encounter.requiredToProceed?"앞길의 경비를 정리하자":"";
        }
        Choice(chapter==4?560:710,chapter==4?820:950);
        Bonus(chapter==4?1280:1040);
        foreach(float d in (chapter==4?new[]{370f,510f,970f,1460f}:new[]{250f,390f,650f,995f,1280f,1440f}))Hazard(d);
        foreach(float d in (chapter==4?new[]{1580f,1680f}:new[]{320f,1380f}))Target(d,false);
        if(chapter==5)Target(1575,true);
        var goal=Group(actors,chapter==4?"EnterTower":"CollectOffering");Sample(chapter==4?1847:1647,out var c,out var f);goal.position=c+Vector3.up*1;var col=goal.gameObject.AddComponent<BoxCollider>();col.size=new Vector3(12,4,4);col.isTrigger=true;var finish=goal.gameObject.AddComponent<Chapter45Goal>();finish.floor=chapter==4?0:2;finish.offering=chapter==5;
    }
    static void Choice(float start,float end)
    {
        var group=Group(actors,"RouteChoice");var choice=group.gameObject.AddComponent<Chapter45Choice>();choice.distance=start;choice.endDistance=end;choice.rewardDistance=end-45;choice.branchOffset=18;choice.transitionLength=24;choice.branchHalfWidth=3.5f;choice.healFraction=.2f;choice.coinReward=80;choice.coinKey=chapter==4?"c45_shopCoins":"towerArchiveCoinReward";choice.grantShieldOnRisk=chapter==5;choice.leftName=chapter==4?"호숫가 · 회복":"정비 통로 · 회복";choice.rightName=chapter==4?"상점가 · 코인":"보안 기록실 · 방패 + 코인";
        Sample(start-25,out var c,out var f);choice.previewLabel=Sign(group,"ChoicePreview",choice.leftName+"     |     "+choice.rightName,c+Vector3.up*5,30,3,5,Color.white);
        // Two actual parallel corridors, broad smooth approach/merge decks.
        for(float d=start;d<end;d+=12){Sample(d,out var a,out var ff);Sample(Mathf.Min(end,d+12),out var b,out var bf);float ta=Mathf.SmoothStep(0,1,Mathf.Min(d-start,end-d)/24);float tb=Mathf.SmoothStep(0,1,Mathf.Min(d+12-start,end-d-12)/24);foreach(int side in new[]{-1,1}){Road(a+Vector3.Cross(Vector3.up,ff)*side*18*ta,b+Vector3.Cross(Vector3.up,bf)*side*18*tb,8,side<0?green:stone);}}
        for(int k=0;k<2;k++)
        {
            float d=start+65+k*55;var row=Group(actors,"RiskRouteGuards_"+k);var encounter=row.gameObject.AddComponent<Chapter45Encounter>();encounter.activationDistance=d-45;encounter.stopDistance=d-9;encounter.floor=chapter==4?0:1;encounter.choiceIndex=0;encounter.requiredChoice=1;encounter.requiredToProceed=true;encounter.health=chapter==4?175:210;encounter.damage=30;encounter.statPrefix="c45_archive";encounter.coinReward=8;
            encounter.actors=new[]{Enemy(row,"C07_riot_police",d,15.4f,encounter.floor,100+k*2),Enemy(row,"C01_patrol_police",d,20.6f,encounter.floor,101+k*2)};
        }
    }
    static void Bonus(float distance)
    {var group=Group(actors,"ShieldOrAttack");var choice=group.gameObject.AddComponent<Chapter45Choice>();choice.kind=Chapter45Choice.ChoiceKind.ShieldOrAttack;choice.distance=distance;choice.endDistance=distance+15;choice.rewardDistance=distance+2;choice.leftName="방패 · 다음 피해 1회 방어";choice.rightName="공격력 +20%";choice.attackPercent=20;Sample(distance,out var c,out var f);Model(group,"B01_bonus_pad",c+new Vector3(-2.5f,0,0),.75f);Model(group,"B01_bonus_pad",c+new Vector3(2.5f,0,0),.75f);choice.previewLabel=Sign(group,"BonusChoiceLabel","방패   |   공격력 +20%",c+new Vector3(0,5,0),20,2,5,Color.white);}
    static void Hazard(float distance)
    {Sample(distance,out var c,out var f);var g=Group(actors,"Sweep_"+distance);var hazard=g.gameObject.AddComponent<Chapter45Hazard>();hazard.distance=distance;hazard.warningDistance=24;hazard.warningSeconds=1.6f;hazard.operationSeconds=1.3f;hazard.safeLane=indexSafe(distance);hazard.floor=chapter==4?0:distance<530?0:distance<1120?1:2;hazard.damageFraction=.08f;hazard.footprint=Box(g,"WarningFootprint",c+Vector3.up*.035f,new Vector3(7,.035f,5),red);hazard.body=chapter==4?Model(g,"V07_taxi",c,1,90).transform:Box(g,"SecuritySweep",c+Vector3.up,new Vector3(5,2,.8f),gold).transform;hazard.sweepFrom=new Vector3(12,0,0);hazard.sweepTo=new Vector3(-12,0,0);var hit=hazard.body.gameObject.AddComponent<BoxCollider>();hit.isTrigger=true;hit.size=chapter==4?new Vector3(4,2,7):Vector3.one;hit.center=chapter==4?Vector3.up:Vector3.zero;}
    static float indexSafe(float d)=>((int)d/10)%2==0?-3:3;
    static void Target(float distance,bool captain)
    {Sample(distance,out var c,out var f);var g=Group(actors,captain?"CrownCaptain":"SecurityPartition_"+distance);g.position=c;var target=g.gameObject.AddComponent<Chapter45Target>();target.distance=distance;target.lane=0;target.halfWidth=1.4f;target.health=captain?1250:180;target.healthKey=captain?"c45_captainHealth":"c45_targetHealth";target.floor=chapter==4?0:distance<530?0:distance<1120?1:2;target.blocking=true;target.blocksAllLanes=true;target.captain=captain;var panel=Box(g,"ShieldedCore",c+new Vector3(0,1.5f,0),new Vector3(3,3,1),captain?gold:blue,true);target.panel=panel.transform;target.panelCollider=panel.GetComponent<Collider>();var hit=g.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,1.5f,0);hit.size=new Vector3(3,3,1.3f);hit.isTrigger=true;target.hitCollider=hit;target.healthLabel=Sign(g,"TargetHealth","",c+new Vector3(0,4.3f,0),10,2,6,Color.white);if(captain){Model(g,"C07_riot_police",c+new Vector3(0,0,2),1.5f,180);var shield=Box(g,"AmberShield",c+new Vector3(0,1.5f,-.7f),new Vector3(3.4f,3.4f,.12f),gold);target.shieldVisual=shield;}}
}
