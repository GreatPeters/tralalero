using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using IndianOceanAssets.ShooterSurvival.Analytics;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HighwaySceneBuilder
{
    public const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/HighWay.unity";
    private const string AssetRoot = "Assets/ShooterSurvival/Models/Highway/Route";
    public static readonly Vector3[] Points = {
        new(-420,0,-740),new(-420,0,-320),new(-160,0,-320),new(-160,5,220),
        new(200,5,220),new(200,0,720),new(460,0,720) };
    public const float Length = 2340;
    private static Transform roads, props, enemies, bonuses, targets;
    private static Material asphalt, white, yellow, steel, concrete, green, red, grass;
    private static readonly List<object> enemyRows=new(),bonusRows=new(),hazardRows=new();

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        if (File.Exists(ScenePath)) throw new InvalidOperationException("HighWay already exists. Refine the authored scene; do not replace it.");
        var source=EditorSceneManager.OpenScene(NoryangjinMapToolWindow.Sr18MapToolScenePath);
        ConfigureTravel(source.GetRootGameObjects().Single(g=>g.name=="Canvas"),1,"HighWay");
        EditorSceneManager.MarkSceneDirty(source);EditorSceneManager.SaveScene(source);
        if(!AssetDatabase.CopyAsset(source.path,ScenePath))throw new InvalidOperationException("Scene copy failed.");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        foreach(var child in map.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach(var extra in scene.GetRootGameObjects().Where(g=>g.name=="Original" && g.GetComponentsInChildren<Renderer>().Length==0).ToArray())UnityEngine.Object.DestroyImmediate(extra);
        roads=Group(map,"Roads");props=Group(map,"Props");enemies=Group(map,"Enemies");bonuses=Group(map,"Bonuses");targets=Group(map,"Highway_EnemyTargets");
        Group(map,"Water");Group(map,"MapTool_Work_Grid");Group(map,"MapTool_Work_Floor");Group(map,"MapTool_Origin_Post");
        Directory.CreateDirectory(AssetRoot);Directory.CreateDirectory("Assets/ShooterSurvival/Prefabs/Highway/Roads");Directory.CreateDirectory("Assets/ShooterSurvival/Prefabs/Highway/Gimmicks");
        Materials();BuildRoad();BuildScenery();
        return FinishScene(scene,map);
    }
    public static object ResumeDraft()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath || File.Exists("map-concepts/highway-chapter-2026-09-11/placements.json"))throw new InvalidOperationException("Only the unfinished HighWay draft may be resumed.");
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        roads=map.Find("Roads");props=map.Find("Props");enemies=map.Find("Enemies");bonuses=map.Find("Bonuses");targets=map.Find("Highway_EnemyTargets");
        if(roads.childCount<100 || enemies.childCount!=0 || bonuses.childCount!=0)throw new InvalidOperationException("Unexpected partial draft; inspect before continuing.");
        foreach(var child in props.Cast<Transform>().Where(t=>t.name=="Highway_SignGantry" || t.name=="Highway_Tunnel_Rib").ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        Materials();BuildLandmarks();return FinishScene(scene,map);
    }
    private static object FinishScene(UnityEngine.SceneManagement.Scene scene,Transform map)
    {
        BuildEncounters();BuildHazards();BuildBonuses();
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var camera=Camera.main.transform;
        Vector3 offset=player.transform.InverseTransformPoint(camera.position);Quaternion rotation=Quaternion.Inverse(player.transform.rotation)*camera.rotation;
        camera.SetParent(player.transform,true);camera.localPosition=offset;camera.localRotation=rotation;camera.localScale=Vector3.one;
        player.transform.SetPositionAndRotation(Points[0]+Vector3.up*.12f,Quaternion.identity);
        player.GetComponent<NoryangjinRoadHeightFollower>().Configure(roads,.12f);
        camera.GetComponent<NoryangjinCameraOcclusion>().Configure(player.transform,roads);
        camera.GetComponent<NoryangjinCameraOcclusion>().ConfigureAdditionalOccluders(props.Cast<Transform>().Where(t=>t.name=="Highway_SignGantry"||t.name=="Highway_Tunnel_Rib").ToArray());
        var playerData=new SerializedObject(player);playerData.FindProperty("xRange").vector2Value=new Vector2(-4.4f,4.4f);playerData.ApplyModifiedPropertiesWithoutUndo();
        var exit=Group(map,"Highway_StageExit");exit.position=Sample(Length-10,out var direction);exit.rotation=Quaternion.LookRotation(direction);exit.gameObject.tag="GameEndTriggerTag";
        var exitCollider=exit.gameObject.AddComponent<BoxCollider>();exitCollider.isTrigger=true;exitCollider.center=Vector3.up*1.5f;exitCollider.size=new Vector3(14,3,1);
        var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas");ConfigureTravel(canvas,2,"");RefinedGameUI.BuildShop(canvas);RefinedGameUI.BuildOpening(canvas);
        var analytics=canvas.GetComponent<GameplayAnalyticsSceneContext>();if(analytics==null)analytics=canvas.AddComponent<GameplayAnalyticsSceneContext>();analytics.Configure(2,1,6,"forward_march",true);
        if(map.GetComponent<EncounterPlacementController>()==null)map.gameObject.AddComponent<EncounterPlacementController>();
        var light=scene.GetRootGameObjects().Single(g=>g.name=="MapTool_DirectionalLight").GetComponent<Light>();light.color=new Color(1f,.94f,.83f);light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(48,-32,0);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.60f,.77f,.88f);RenderSettings.fogStartDistance=110;RenderSettings.fogEndDistance=310;
        Camera.main.farClipPlane=400;
        var build=EditorBuildSettings.scenes.ToList();
        foreach(string path in new[]{NoryangjinMapToolWindow.Sr18MapToolScenePath,ScenePath})
        {var entry=build.FirstOrDefault(s=>s.path==path);if(entry==null)build.Add(new EditorBuildSettingsScene(path,true));else entry.enabled=true;}
        EditorBuildSettings.scenes=build.ToArray();
        foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true))
            if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("map-concepts/highway-chapter-2026-09-11");
        File.WriteAllText("map-concepts/highway-chapter-2026-09-11/placements.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{length=Length,points=Points.Select(p=>new[]{p.x,p.y,p.z}),enemies=enemyRows,bonuses=bonusRows,gimmicks=hazardRows},Newtonsoft.Json.Formatting.Indented));
        NoryangjinMapToolWindow.Open();return new{scene=ScenePath,length=Length,enemies=enemyRows.Count,bonuses=bonusRows.Count,gimmicks=hazardRows.Count,props=props.childCount};
    }
    public static Vector3 Sample(float distance,out Vector3 direction)
    {
        float left=Mathf.Clamp(distance,0,Length);
        for(int i=0;i<Points.Length-1;i++)
        {
            Vector3 delta=Points[i+1]-Points[i];float length=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;
            if(left<=length || i==Points.Length-2){direction=Vector3.ProjectOnPlane(delta,Vector3.up).normalized;return Vector3.Lerp(Points[i],Points[i+1],left/length);}
            left-=length;
        }
        direction=Vector3.right;return Points[^1];
    }
    private static void Materials()
    {
        asphalt=Material("Asphalt",new Color(.48f,.49f,.51f));asphalt.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/polyperfect/Poly Universal Pack/Textures/City/Asphalt_A_City_Alb.png"));
        white=Material("RoadPaint",new Color(.91f,.93f,.87f));yellow=Material("WarningYellow",new Color(1,.66f,.08f));steel=Material("GalvanizedSteel",new Color(.38f,.48f,.54f));
        concrete=Material("Concrete",new Color(.47f,.50f,.49f));green=Material("SignGreen",new Color(.025f,.24f,.15f));red=Material("Signal",Color.red);red.EnableKeyword("_EMISSION");grass=Material("Ground",new Color(.20f,.30f,.19f));
    }
    private static Material Material(string name,Color color)
    {string path=AssetRoot+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.2f);m.enableInstancing=true;GeneratedStylizedSurface.Apply(m);return m;}
    private static void BuildRoad()
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var surface=new List<int>();var paint=new List<int>();var edge=new List<int>();
        void Quad(float x0,float x1,float z0,float z1,float y,List<int> tris)
        {int n=vertices.Count;vertices.AddRange(new[]{new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x0,y,z1),new Vector3(x1,y,z1)});uv.AddRange(new[]{new Vector2(x0/5,z0/5),new Vector2(x1/5,z0/5),new Vector2(x0/5,z1/5),new Vector2(x1/5,z1/5)});tris.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
        Quad(-7,7,-10,10,0,surface);
        foreach(float x in new[]{-1.8f,1.8f})foreach(float z in new[]{-9f,1f})Quad(x-.075f,x+.075f,z,z+5,.018f,paint);
        foreach(float x in new[]{-6.15f,6.15f})Quad(x-.07f,x+.07f,-10,10,.018f,paint);
        foreach(float x in new[]{-6.6f,6.6f})Quad(x-.065f,x+.065f,-10,10,.02f,edge);
        var mesh=new Mesh{name="Highway20m"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=3;mesh.SetTriangles(surface,0);mesh.SetTriangles(paint,1);mesh.SetTriangles(edge,2);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,AssetRoot+"/Highway20m.asset");
        var root=new GameObject("고속도로 직선 20m");root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=new[]{asphalt,white,yellow};
        var floor=new Mesh{name="HighwayCollision"};floor.vertices=vertices.Take(4).ToArray();floor.triangles=new[]{0,2,1,1,2,3};floor.RecalculateNormals();AssetDatabase.CreateAsset(floor,AssetRoot+"/HighwayCollision.asset");root.AddComponent<MeshCollider>().sharedMesh=floor;
        string path="Assets/ShooterSurvival/Prefabs/Highway/Roads/HighwayStraight.prefab";PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
        int serial=0;
        for(int leg=0;leg<Points.Length-1;leg++)
        {
            var delta=Points[leg+1]-Points[leg];int count=Mathf.RoundToInt(Vector3.ProjectOnPlane(delta,Vector3.up).magnitude/20);
            for(int j=0;j<count;j++)
            {
                var road=Instance(path,roads,"HWY_R"+(++serial).ToString("D3"));road.transform.position=Vector3.Lerp(Points[leg],Points[leg+1],(j+.5f)/count);road.transform.rotation=Quaternion.LookRotation(delta.normalized);
                for(int side=-1;side<=1;side+=2)
                {
                    if(j>0 && j<count-1)
                    {
                        Cube(road.transform,"Guardrail",new Vector3(side*6.85f,.9f,0),new Vector3(.13f,.26f,20),steel);
                        for(int k=0;k<4;k++)Cube(road.transform,"Post",new Vector3(side*6.85f,.5f,-7.5f+k*5),new Vector3(.16f,1,.16f),steel);
                    }
                    Cube(road.transform,"DeckEdge",new Vector3(side*6.95f,-.3f,0),new Vector3(.2f,.6f,20),concrete);
                }
            }
            if(leg<Points.Length-2)
            {
                var corner=Instance(path,roads,"HWY_Corner_"+leg);corner.transform.position=Points[leg+1];corner.transform.localScale=new Vector3(1,1,.7f);
                var spot=Group(props,"Highway_Turn_"+leg);spot.position=Points[leg+1];spot.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(delta,Vector3.up));
                var turn=spot.gameObject.AddComponent<NoryangjinTurnSpot>();turn.TargetYawDegrees=Quaternion.LookRotation(Vector3.ProjectOnPlane(Points[leg+2]-Points[leg+1],Vector3.up)).eulerAngles.y;turn.TurnDurationSeconds=.45f;
                var collider=spot.GetComponent<BoxCollider>();collider.isTrigger=true;collider.center=Vector3.up;collider.size=new Vector3(14,2,.8f);
            }
        }
    }
    private static void BuildScenery()
    {
        Cube(props,"CityGround",new Vector3(0,-7,0),new Vector3(1600,1,1900),grass);
        string[] buildings=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ithappy/Megacity/Prefabs/Buildings/Skyscrapers","Assets/ithappy/Megacity/Prefabs/Buildings/Business center"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("eiffel")&&!p.Contains("casino")).ToArray();
        for(float d=25;d<Length-20;d+=30)
        {
            var center=Sample(d,out var forward);var right=Vector3.Cross(Vector3.up,forward);int index=Mathf.RoundToInt(d/30);
            foreach(int side in new[]{-1,1})
            {
                if(index%3==0)PlaceProp(65,center+right*(side*9),forward,side<0?0:180);
                var tree=Instance("Assets/ithappy/Megacity/Prefabs/Props/tree_"+(index%2==0?"012":"013")+".prefab",props,"Highway_Tree_"+index+"_"+side);
                Fit(tree,6+(index%4)*.8f);tree.transform.position=new Vector3(center.x, -6.5f,center.z)+right*(side*(17+index%5));
                if(index%2==0)
                {
                    var building=Instance(buildings[(index+(side+1)*3)%buildings.Length],props,"Highway_City_"+index+"_"+side);Fit(building,25+(index%5)*8);
                    building.transform.position=new Vector3(center.x,-6.5f,center.z)+right*(side*(45+index%3*12));building.transform.rotation=Quaternion.LookRotation(-right*side);
                }
            }
            if(index%4==0)PlaceProp(72,center+right*6.3f,forward);
            if(index%7==0)PlaceProp(73,center-right*8.2f,forward);
            if(index%8==0)PlaceProp(52,center+right*10,forward,180);
            if(d>500 && d<1100 && index%2==0){PlaceProp(51,center+right*8,forward);PlaceProp(55,center-right*8,forward);}
            if(d>900 && index%6==0)PlaceProp(new[]{67,68,69,80,81,82,83}[index%7],center+right*(index%2==0?11:-11),forward);
        }
        BuildLandmarks();
    }
    private static void BuildLandmarks()
    {
        foreach(float d in new[]{70f,480f,870f,1420f,2100f})Gantry(d);
        foreach(float d in new[]{1490f,1770f,1990f}){var p=Sample(d,out var f);PlaceProp(60,p+Vector3.Cross(Vector3.up,f)*8.6f,f);PlaceProp(79,p-Vector3.Cross(Vector3.up,f)*8,f);}
        // Open-sided tunnel ribs preserve the gameplay camera while framing the final district.
        for(float d=2180;d<Length-15;d+=18)
        {var p=Sample(d,out var f);var gate=Group(props,"Highway_Tunnel_Rib");gate.position=p;gate.rotation=Quaternion.LookRotation(f);foreach(int side in new[]{-1,1})Cube(gate,"TunnelWall",new Vector3(side*8,3.8f,0),new Vector3(.5f,7.6f,.7f),concrete);Cube(gate,"TunnelArch",new Vector3(0,7.4f,0),new Vector3(16,.55f,.7f),concrete);}
    }
    private static void Gantry(float distance)
    {
        var p=Sample(distance,out var f);var g=Group(props,"Highway_SignGantry");g.position=p;g.rotation=Quaternion.LookRotation(f);
        foreach(int side in new[]{-1,1})Cube(g,"GantryPost",new Vector3(side*7.5f,3.7f,0),new Vector3(.28f,7.4f,.28f),steel);
        Cube(g,"GantryBeam",new Vector3(0,7.1f,0),new Vector3(15,.3f,.3f),steel);
        for(int i=-1;i<=1;i++)
        {var sign=Cube(g,"GreenSign",new Vector3(i*3.7f,6.0f,0),new Vector3(3.25f,1.7f,.12f),green);var arrow=Group(sign.transform,"Arrow");arrow.localPosition=new Vector3(0,0,-.51f);var text=arrow.gameObject.AddComponent<TextMeshPro>();text.text="↑";text.fontSize=7;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.transform.localScale=new Vector3(1/3.25f,1/1.7f,1);text.transform.localRotation=Quaternion.Euler(0,180,0);}
    }
    private static void BuildEncounters()
    {
        enemyRows.Clear();float[] distances={90,170,260,340,510,585,770,850,935,1020,1100,1150,1300,1370,1460,1510,1670,1750,1830,1910,1980,2170,2270};
        for(int i=0;i<distances.Length;i++)
        {
            int type=i==distances.Length-1?2:i%6;float d=distances[i];var p=Sample(d,out var f);float lane=new[]{-3.5f,3.5f,0f}[i%3];
            string id=$"HWY_E{i+1:D2}_{HighwayEnemyBuilder.Names[type]}";
            var enemy=Instance(HighwayEnemyBuilder.Folder+"/"+HighwayEnemyBuilder.Names[type]+".prefab",enemies,id);enemy.transform.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*lane+Vector3.up*.08f,Quaternion.LookRotation(f));
            var events=enemy.GetComponent<EnemyEventController>();bool ranged=enemy.GetComponent<EnemyScript_space>().HasConfiguredProjectile;
            events.EventMode=ranged?EnemyEventMode.Shoot:EnemyEventMode.PatrolBetweenStartAndTarget;events.PatrolAcrossRoad=!ranged;events.MoveSpeed=2.4f;events.HideWhileWaiting=false;
            float move=ranged?0:3.4f;
            if(!ranged){enemy.transform.position-=Vector3.Cross(Vector3.up,f)*move*.5f;var target=Group(targets,id+"_Target");target.position=enemy.transform.position+Vector3.Cross(Vector3.up,f)*move;events.TargetPoint=target;}
            var spot=Group(props,id+"_Activation");spot.position=Sample(d-42,out _)+Vector3.up*.08f;spot.rotation=Quaternion.LookRotation(f);var trigger=spot.gameObject.AddComponent<EnemyEventActivationSpot>();trigger.Targets=new[]{events};var col=spot.GetComponent<BoxCollider>();col.size=new Vector3(14,2,.8f);col.center=Vector3.up;
            enemyRows.Add(new{id,distance=d,model=HighwayEnemyBuilder.Names[type],mode=ranged?"사격":"왕복",speed=2.4f,move,lead=42,delay=.55f,projectileSpeed=12,tier=i==distances.Length-1?"Boss":type==2||type==3?"Elite":"Normal"});
        }
    }
    private static void BuildBonuses()
    {
        bonusRows.Clear();int index=0;
        foreach(float d in new[]{55f,205f,300f,540f,800f,975f,1080f,1330f,1430f,1700f,1880f,2200f})
        {
            var p=Sample(d,out var f);string id=$"HWY_B{++index:D2}";var choices=new AuthoredBonusWall[2];
            for(int side=0;side<2;side++){var g=Instance("Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab",bonuses,id+(side==0?"":"_Right"));g.transform.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*(side==0?-3:3),Quaternion.LookRotation(-f));g.transform.localScale=new Vector3(2.25f,2.9f,2.9f);choices[side]=g.GetComponent<AuthoredBonusWall>();choices[side].Configure(index%4==0?Rarity.Rare:Rarity.Normal);}
            var pair=choices[0].gameObject.AddComponent<BonusWallChoicePair>();pair.Configure(choices[0],choices[1]);bonusRows.Add(new{id,distance=d,rarity=index%4==0?"Rare":"Normal"});
        }
    }
    private static void BuildHazards()
    {
        hazardRows.Clear();int serial=0;
        foreach(float d in new[]{220f,310f,525f,610f,810f,970f,1120f,1320f,1400f,1730f,1830f,1950f,2160f,2220f})
        {var g=Hazard($"HWY_G{++serial:D2}_Roadblock",d,new[]{-3.5f,3.5f,0}[serial%3],ObstaclePattern.HighwayRoadblock);var h=g.GetComponent<HighwayHazard>();h.visual=PlaceProp(serial%2==0?50:51,g.transform.position,g.transform.forward,0,g.transform).transform;h.breakHealth=85;g.GetComponent<BoxCollider>().size=new Vector3(2.2f,2,1.2f);RecordHazard(g,d,"HighwayRoadblock",25);}
        foreach(float d in new[]{250f,760f,1040f,1360f,1800f,2130f})
        {var g=Hazard($"HWY_G{++serial:D2}_Traffic",d,-4.5f,ObstaclePattern.HighwayTraffic);var h=g.GetComponent<HighwayHazard>();h.visual=PlaceProp(serial%2==0?81:82,g.transform.position,g.transform.forward,90,g.transform).transform;h.crossingDistance=9;h.cycleSeconds=4;g.GetComponent<BoxCollider>().size=new Vector3(4.4f,1.6f,2);h.warning=Cube(g.transform,"TrafficWarning",new Vector3(4.5f,.04f,3),new Vector3(9,.025f,.35f),yellow);RecordHazard(g,d,"HighwayTraffic",35);}
        foreach(float d in new[]{1490f,1770f,1990f})for(int lane=0;lane<3;lane++)
        {
            var g=Hazard($"HWY_G{++serial:D2}_Toll_L{lane}",d,(lane-1)*3.6f,ObstaclePattern.HighwayToll);var h=g.GetComponent<HighwayHazard>();h.laneIndex=lane;h.cycleSeconds=6;
            var visual=Group(g.transform,"TollLane");h.visual=visual;Cube(visual,"Island",new Vector3(-1.5f,.15f,0),new Vector3(.35f,.3f,3.5f),concrete);Cube(visual,"Motor",new Vector3(-1.5f,.7f,0),new Vector3(.4f,1.1f,.45f),yellow);
            h.barrierArm=Group(visual,"ArmPivot");h.barrierArm.localPosition=new Vector3(-1.5f,1.3f,0);Cube(h.barrierArm,"Arm",new Vector3(1.5f,0,0),new Vector3(3,.17f,.17f),white);
            for(int stripe=0;stripe<5;stripe++)Cube(h.barrierArm,"RedStripe",new Vector3(.25f+stripe*.6f,0,-.092f),new Vector3(.25f,.18f,.018f),red);
            h.signal=Cube(visual,"Signal",new Vector3(-1.5f,2,0),new Vector3(.32f,.34f,.12f),red).GetComponent<Renderer>();g.GetComponent<BoxCollider>().size=new Vector3(3.25f,2,.5f);RecordHazard(g,d,"HighwayToll",40);
        }
    }
    private static GameObject Hazard(string id,float d,float lane,ObstaclePattern pattern)
    {var g=Group(props,id).gameObject;var p=Sample(d,out var f);g.transform.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*lane,Quaternion.LookRotation(f));g.tag="Obstacle";var col=g.AddComponent<BoxCollider>();col.isTrigger=true;col.center=Vector3.up;var rb=g.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;var stats=g.AddComponent<ObstacleStats>();stats.obstaclePattern=pattern;g.AddComponent<HighwayHazard>();return g;}
    private static void RecordHazard(GameObject g,float d,string pattern,float damage)
    {g.GetComponent<ObstacleStats>().value=damage;hazardRows.Add(new{id=g.name,distance=d,pattern,effect=damage});if(!File.Exists("Assets/ShooterSurvival/Prefabs/Highway/Gimmicks/"+pattern+".prefab")){var p=g.transform.position;var r=g.transform.rotation;g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);PrefabUtility.SaveAsPrefabAsset(g,"Assets/ShooterSurvival/Prefabs/Highway/Gimmicks/"+pattern+".prefab");g.transform.SetPositionAndRotation(p,r);}}
    private static GameObject PlaceProp(int id,Vector3 position,Vector3 direction,float yaw=0,Transform parent=null)
    {var g=Instance(HighwayAssetImporter.Prefabs+"/HWY_"+id.ToString("D3")+".prefab",parent==null?props:parent,"HighwayProp_"+id);g.transform.SetPositionAndRotation(position,Quaternion.LookRotation(direction)*Quaternion.Euler(0,yaw,0));return g;}
    private static GameObject Instance(string path,Transform parent,string name)
    {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Missing prefab: "+path);var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.name=name;g.SetActive(true);return g;}
    private static Transform Group(Transform parent,string name)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    private static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;return g;}
    private static void Fit(GameObject g,float height)
    {var bounds=HighwayAssetImporter.BoundsOf(g);g.transform.localScale*=height/bounds.size.y;bounds=HighwayAssetImporter.BoundsOf(g);g.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);foreach(var c in g.GetComponentsInChildren<Collider>(true))c.enabled=false;}
    private static void ConfigureTravel(GameObject canvas,int chapter,string next)
    {
        var travel=canvas.GetComponent<ChapterProgression>();if(travel==null)travel=canvas.AddComponent<ChapterProgression>();travel.chapter=chapter;travel.nextScene=next;
        var controller=canvas.GetComponent<CanvasScript>();var panel=controller.youWinUI.transform;
        foreach(var button in panel.GetComponentsInChildren<Button>(true))UnityEngine.Object.DestroyImmediate(button.gameObject);
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/JH/Font/쩡야공유/GmarketSansTTFBold SDF2.asset");
        Button Button(string name,string title,float x0,float x1)
        {var root=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));root.transform.SetParent(panel,false);var r=(RectTransform)root.transform;r.anchorMin=new Vector2(x0,.09f);r.anchorMax=new Vector2(x1,.22f);r.offsetMin=r.offsetMax=Vector2.zero;root.GetComponent<Image>().color=new Color(.86f,.69f,.38f);var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(root.transform,false);var tr=(RectTransform)label.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;var t=label.GetComponent<TextMeshProUGUI>();t.font=font;t.text=title;t.fontSize=36;t.enableAutoSizing=true;t.fontSizeMin=24;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.03f,.07f,.1f);return root.GetComponent<Button>();}
        var replay=Button("ReplayChapter","다시 도전",.08f,string.IsNullOrEmpty(next)?.92f:.46f);UnityEventTools.AddPersistentListener(replay.onClick,travel.Replay);
        if(!string.IsNullOrEmpty(next)){var onward=Button("NextChapter","고속도로로",.52f,.92f);UnityEventTools.AddPersistentListener(onward.onClick,travel.LoadNextChapter);}
        EditorUtility.SetDirty(travel);
    }
}
