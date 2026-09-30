using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Screenshot-feedback pass. Rebuilt only in the SR18 safe-copy installer, after the market presentation.
public static class NoryangjinFeedbackV3Builder
{
    const string Dir="Assets/ShooterSurvival/Models/Generated/NoryangjinFeedbackV3";
    const string Prefabs="Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    static readonly Dictionary<string,Material> mats=new();
    static readonly List<Transform> fade=new();
    static readonly Dictionary<string,Mesh> splitMeshes=new();
    static TMP_FontAsset font;
    static Material steel,ivory,blue,dark,floor;
    public static Transform[] Apply(Transform root,Transform roads,float half,TMP_FontAsset typeface)
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
        foreach(var id in new[]{"N15_refrigeration_unit","N16_auction_counter","N17_tuna_ice_pallet","N18_coldstore_gateway"})
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+id+".prefab")==null)throw new Exception("Missing feedback asset "+id);
        Directory.CreateDirectory(Dir);font=typeface;mats.Clear();fade.Clear();splitMeshes.Clear();
        steel=Mat("Steel",new Color(.49f,.57f,.6f),.7f,.5f);ivory=Mat("InsulatedIvory",new Color(.82f,.85f,.84f),.35f);
        blue=Mat("BlueTrim",new Color(.035f,.18f,.35f),.5f);dark=Mat("Graphite",new Color(.075f,.095f,.12f),.35f);
        floor=Mat("ConcreteFloor",new Color(.7f,.76f,.78f),.58f);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ShooterSurvival/Models/Generated/NoryangjinInteriorV2/WetFloor.png");
        floor.SetTexture("_BaseMap",texture);floor.SetTextureScale("_BaseMap",new Vector2(3,24));EditorUtility.SetDirty(floor);
        var visibility=root.GetComponentInChildren<NoryangjinInteriorDetailVisibility>();
        if(visibility!=null)
        {
            if(visibility.fogCurtain!=null)Object.DestroyImmediate(visibility.fogCurtain.gameObject);
            visibility.fogCurtain=null;visibility.viewDistance=115;
        }
        foreach(var b in root.GetComponentsInChildren<NoryangjinRushEvent>())b.coinsEach=3;
        SplitRoadScenery(roads.parent);BuildShutter(root,half);ConnectDeck(root,roads,half);BuildWater(root);
        BuildColdAuction(root,roads);ConfigureContainer(root);GroupCeilings(root);
        var atmosphere=root.GetComponent<NoryangjinMarketAtmosphere>();
        atmosphere.broadcasts=Enumerable.Range(1,3).Select(i=>Voice("broadcast-"+i)).ToArray();
        // Every ceiling panel and wall is registered; the former installer registered one panel per bay.
        fade.AddRange(root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="V2Roof"||t.name=="CeilingJointBacking"||t.name=="TiledWall"||t.name=="TiledColumn"||t.name=="MarketPortico"||t.name=="ColdRoof"));
        var cam=GameObject.Find("MapTool_Camera").GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinCameraOcclusion>();
        var props=GameObject.Find("Noryangjin_MapTool/Props");
        var shops=props!=null?props.transform.Cast<Transform>().Where(t=>t.name.StartsWith("SR18_Route_Shop_")).ToArray():Array.Empty<Transform>();
        cam.ConfigureFeedbackTransparency(fade.Concat(shops).ToArray());
        AssetDatabase.SaveAssets();return fade.ToArray();
    }
    static void BuildShutter(Transform root,float half)
    {
        foreach(var old in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="MarketPortico"&&t.position.z< -320).ToArray())Object.DestroyImmediate(old.gameObject);
        var e=root.GetComponentInChildren<NoryangjinShutterEvent>();
        e.shutter.healthLabelFrontOffset=.35f;e.shutter.healthLabelHeight=3.8f;e.shutter.healthFontSize=4.5f;
        if(e.shutterPanel!=null)Object.DestroyImmediate(e.shutterPanel.gameObject);
        var moving=Node(e.shutter.transform,"RollerCurtain",Vector3.up*.1f);e.shutterPanel=moving;
        var roller=moving.gameObject.AddComponent<NoryangjinRollerShutterVisual>();roller.height=5.8f;
        var slats=new List<Transform>();var slat=SlatMesh();
        for(int i=0;i<38;i++)
        {
            var t=Node(moving,"InterlockingSlat",Vector3.up*((i+.5f)*5.8f/38));t.localScale=new Vector3(half*2+1.7f,5.8f/38,1);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=slat;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=steel;slats.Add(t);
        }
        roller.slats=slats.ToArray();roller.bottomRail=Box(moving,"StraightRubberBottom",new Vector3(0,.07f,-.025f),new Vector3(half*2+1.72f,.14f,.19f),dark);
        e.roller=roller;e.openHeight=4.8f;e.passClearance=3.8f;e.closeSeconds=.45f;
        roller.SetOpening(e.openHeight);
        var frame=Node(e.transform,"ShutterTracksAndMotor",Vector3.zero);fade.Add(frame);
        foreach(float side in new[]{-1f,1f})
        {
            Box(frame,"GuideChannel",new Vector3(side*(half+1),3.15f,0),new Vector3(.34f,6.3f,.55f),dark);
            Box(frame,"GuideLip",new Vector3(side*(half+.85f),3.05f,-.20f),new Vector3(.09f,6.1f,.09f),steel);
            Box(frame,"FootGuard",new Vector3(side*(half+1),.58f,-.36f),new Vector3(.38f,1.15f,.14f),Mat("GuardYellow",new Color(.88f,.64f,.13f),.4f));
        }
        Box(frame,"RollerHousing",new Vector3(0,6.3f,.08f),new Vector3(half*2+2.5f,.8f,.95f),ivory);
        Box(frame,"MotorBox",new Vector3(half+1.38f,5.94f,.08f),new Vector3(.5f,.75f,.8f),steel);
        Label(frame,"Title","시장동 출구",new Vector3(0,6.35f,-.444f),new Vector2(4,.55f),3.2f,Color.white);
        Box(frame,"BlueHeader",new Vector3(0,6.35f,-.405f),new Vector3(4.8f,.64f,.05f),blue);
        // Ray-test the open frame's actual geometry instead of treating its aperture as a solid box.
        foreach(var mesh in frame.GetComponentsInChildren<MeshFilter>())mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
        // Seven barriers finish three metres before the door, rather than leaving an empty thirty-metre run-up.
        float[] distances={300,314,328,342,354,365,373.5f};var walls=new List<NoryangjinBreakable>();
        for(int i=0;i<distances.Length;i++)
        {
            NoryangjinBreakable b;
            if(i<e.walls.Length)b=e.walls[i];
            else{b=Object.Instantiate(e.walls[e.walls.Length-1],e.transform);b.name="BoxWall"+(i+1);}
            b.transform.localPosition=new Vector3(0,0,distances[i]-376.5f);walls.Add(b);
        }
        e.walls=walls.ToArray();
    }
    static void GroupCeilings(Transform root)
    {
        foreach(var bay in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("HallSegment_")||t.name.StartsWith("InsulatedBay")).ToArray())
        {
            var members=bay.Cast<Transform>().Where(t=>t.localPosition.y>5.4f&&t.GetComponent<Light>()==null).ToArray();
            var group=Node(bay,"OverheadScenery",Vector3.zero);
            foreach(var member in members)member.SetParent(group,true);
            fade.RemoveAll(t=>t!=null&&t.IsChildOf(group));fade.Add(group);
        }
    }
    static Mesh SlatMesh()
    {
        string path=Dir+"/ShutterSlat.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null)return saved;
        var profile=new[]{new Vector2(-.49f,0),new Vector2(-.34f,-.035f),new Vector2(.34f,-.035f),new Vector2(.49f,0)};
        var v=new List<Vector3>();var tri=new List<int>();
        foreach(var p in profile){v.Add(new Vector3(-.5f,p.x,p.y));v.Add(new Vector3(.5f,p.x,p.y));}
        for(int i=0;i<3;i++){int a=i*2;tri.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
        var mesh=new Mesh{name="CurvedSteelSlat"};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void ConnectDeck(Transform root,Transform roads,float half)
    {
        var detail=Node(root,"ContinuousLoadingDeck",Vector3.zero);
        var timber=Mat("DockTimber",new Color(.33f,.26f,.19f),.32f);
        var edge=Mat("DockSteelEdge",new Color(.22f,.27f,.29f),.58f,.35f);
        Vector3[] p={new(124.3f,0,-326),new(124.3f,0,-348),new(-67,0,-348),new(-67,0,-328)};
        for(int i=0;i<p.Length-1;i++)
        {
            var delta=p[i+1]-p[i];float length=delta.magnitude;var forward=delta.normalized;var rotation=Quaternion.LookRotation(forward);
            var slab=Box(roads,"ContinuousConnector"+i,(p[i]+p[i+1])*.5f+Vector3.up*.015f,new Vector3(half*2+3,.17f,length+1),timber);slab.rotation=rotation;AddFloorCollider(slab);
            for(float d=0;d<length;d+=1.2f)
            {
                var line=Box(detail,"PlankSeam",p[i]+forward*d+Vector3.up*.104f,new Vector3(half*2+2.9f,.012f,.035f),edge);line.rotation=rotation;
            }
            foreach(float side in new[]{-1f,1f})
            {
                var beam=Box(detail,"SupportGirder",(p[i]+p[i+1])*.5f+Vector3.Cross(Vector3.up,forward)*side*(half+1.3f)-Vector3.up*.25f,new Vector3(.24f,.45f,length+1),edge);beam.rotation=rotation;
            }
        }
    }
    static void BuildWater(Transform root)
    {
        var patchMat=PatchMaterial("WetPuddle",NoryangjinWetPatch.FilmColor,true);
        foreach(var hose in root.GetComponentsInChildren<NoryangjinHoseEvent>())
        {
            hose.warningSeconds=0;
            foreach(var jet in hose.jets)
            {
                if(jet.warning!=null)Object.DestroyImmediate(jet.warning.gameObject);jet.warning=null;
                var at=jet.visual.localPosition+new Vector3(-jet.side*jet.reach*.5f,.21f,0);
                var patch=Node(hose.transform,"PersistentWetFloor",at);var effect=patch.gameObject.AddComponent<NoryangjinWetPatch>();
                effect.halfSize=new Vector2(jet.reach*.5f+.35f,1.45f)*.8f; // user 2026-09-29: dark patches 20% smallereffect.sidewaysSpeed=-jet.side*.95f;effect.drySeconds=10;
                var surface=Ground(patch,"WaterFilm",Vector3.zero,effect.halfSize*2,patchMat);effect.surface=surface.GetComponent<Renderer>();
                var splash=Node(patch,"FloorSplash",new Vector3(-jet.side*jet.reach*.48f,.08f,0)).gameObject.AddComponent<ParticleSystem>();splash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=splash.main;main.playOnAwake=false;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(.3f,.6f);main.startSpeed=new ParticleSystem.MinMaxCurve(1.1f,2.4f);main.startSize=new ParticleSystem.MinMaxCurve(.08f,.2f);main.startColor=new Color(.73f,.94f,1,.9f);main.gravityModifier=.75f;main.maxParticles=96;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=splash.emission;emission.rateOverTime=65;var shape=splash.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=60;shape.radius=.38f;splash.transform.localRotation=Quaternion.Euler(-90,0,0);
                var particleRenderer=splash.GetComponent<ParticleSystemRenderer>();particleRenderer.sharedMaterial=ParticleMaterial();effect.splash=splash;jet.wetPatch=effect;
            }
        }
    }
    static void BuildColdAuction(Transform root,Transform roads)
    {
        var old=root.Find("DistrictLandmarks/ColdStorage");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var oldFloor=roads.Find("ColdStorageFloor");if(oldFloor!=null)Object.DestroyImmediate(oldFloor.gameObject);
        var former=root.GetComponentsInChildren<NoryangjinRushEvent>().FirstOrDefault(e=>e.name=="D1_BidderWall");
        if(former!=null)Object.DestroyImmediate(former.transform.parent.gameObject);
        var room=Node(root,"ColdAuctionHall",new Vector3(-67,0,-333));
        var slab=Box(roads,"ColdAuctionFloor",new Vector3(-67,.015f,-278),new Vector3(18,.17f,150),floor);AddFloorCollider(slab);
        // Preserve the established collision plane while separating the overlapping visual surfaces.
        slab.GetComponent<Renderer>().enabled=false;
        Box(roads,"ColdAuctionFloorSurface",new Vector3(-67,.026f,-269),new Vector3(18,.17f,132),floor);
        const float length=114,width=18,height=7.8f;
        for(int i=0;i<12;i++)
        {
            float z=4.75f+i*9.5f;
            var bay=Node(room,"InsulatedBay"+i,new Vector3(0,0,z));
            foreach(float side in new[]{-1f,1f})
            {
                fade.Add(Box(bay,"InsulatedWall",new Vector3(side*width*.5f,height*.5f,0),new Vector3(.22f,height,9.5f),ivory));
                Box(bay,"ProtectiveSkirting",new Vector3(side*8.82f,.47f,0),new Vector3(.12f,.94f,9.5f),blue);
                for(int j=0;j<5;j++)Box(bay,"PanelJoint",new Vector3(side*8.87f,3.9f,(j-2)*1.9f),new Vector3(.035f,7.6f,.025f),steel);
                fade.Add(Box(bay,"SteelColumn",new Vector3(side*8.45f,3.9f,-4.65f),new Vector3(.32f,7.8f,.32f),steel));
                Box(bay,"RefrigerantPipe",new Vector3(side*7.8f,7.24f,0),new Vector3(.14f,.14f,9.5f),steel);
            }
            fade.Add(Box(bay,"CeilingPanel",new Vector3(0,height,0),new Vector3(width,.16f,9.5f),ivory));
            fade.Add(Box(bay,"Truss",new Vector3(0,7.30f,-4.6f),new Vector3(17.8f,.24f,.24f),steel));
            foreach(float side in new[]{-1f,1f})
            {
                Box(bay,"WarehouseLuminaire",new Vector3(side*3.9f,6.75f,0),new Vector3(2.1f,.14f,.5f),dark);
                Box(bay,"LightDiffuser",new Vector3(side*3.9f,6.67f,0),new Vector3(1.94f,.025f,.42f),Mat("WarmDiffuser",new Color(.94f,.98f,1),0,0,true));
            }
            if(i%3==1)
            {
                foreach(float side in new[]{-1f,1f})Model("N15_refrigeration_unit",bay,new Vector3(side*7.95f,4.95f,0),Quaternion.Euler(0,side<0?90:-90,0),1);
                var light=Node(bay,"ColdBounce",new Vector3(0,5.9f,0)).gameObject.AddComponent<Light>();light.type=LightType.Point;light.intensity=2.3f;light.range=18;light.color=new Color(.86f,.94f,1);light.shadows=LightShadows.None;
            }
            if(i>0&&i<11)
            {
                float side=i%2==0?-1:1;
                Model("N17_tuna_ice_pallet",bay,new Vector3(side*6.1f,.12f,-1.7f),Quaternion.Euler(0,(i%3-1)*8,0),.84f);
                Box(bay,"PalletBayStripe",new Vector3(side*5.7f,.112f,1.1f),new Vector3(4.1f,.008f,.07f),blue);
            }
        }
        foreach(float z in new[]{0f,length})
        {
            var entrance=Node(room,"ColdAuctionPortal",new Vector3(0,0,z));
            Model("N18_coldstore_gateway",entrance,new Vector3(0,.10f,z==0?3.54f:-3.54f),Quaternion.Euler(0,z==0?180:0,0),.9f,true);
            Label(entrance,"WarehouseName",z==0?"노량진 냉동 · 활어 경매장":"상하차 출구",new Vector3(0,5.8f,z==0?-.08f:.08f),new Vector2(8.0f,.6f),3.5f,new Color(.025f,.085f,.17f));
            foreach(float side in new[]{-1f,1f})Box(entrance,"FacadeInfill",new Vector3(side*7.96f,3.9f,0),new Vector3(2.08f,7.8f,.22f),ivory);
            Box(entrance,"UpperInfill",new Vector3(0,7.27f,0),new Vector3(18,1.06f,.22f),ivory);
            fade.Add(entrance);
        }
        var auction=Node(room,"LiveAuction",new Vector3(0,0,52));var activity=auction.gameObject.AddComponent<NoryangjinAuctionActivity>();
        Model("N16_auction_counter",auction,new Vector3(4.1f,.12f,2),Quaternion.identity,.78f);
        Model("N16_auction_counter",auction,new Vector3(-4.1f,.12f,17),Quaternion.identity,.78f);
        var board=Box(auction,"AuctionPriceBoard",new Vector3(3.9f,4.1f,4),new Vector3(3.8f,1.35f,.15f),blue);
        foreach(float x in new[]{2.65f,5.15f})Box(auction,"BoardHanger",new Vector3(x,5.92f,4),new Vector3(.045f,2.3f,.045f),steel);
        activity.priceBoard=Label(board,"LivePrice","활어 경매 진행 중\n30,000원",new Vector3(0,0,-.08f),new Vector2(3.5f,1.15f),3.1f,Color.white);
        activity.auctioneer=Person(auction,"Auctioneer",new Vector3(4.15f,.12f,3.7f),Vector3.back,"N13_merchant_male");
        var bidders=new List<Animator>();
        for(int i=0;i<5;i++)
        {
            var pos=new Vector3(2.9f+(i%3)*1.7f,.12f,-1.6f-(i/3)*2.1f);
            bidders.Add(Person(auction,"Bidder"+i,pos,new Vector3(4.15f,0,3.7f)-pos,i%2==0?"N13_merchant_male":"N14_merchant_female"));
        }
        bidders.Add(Person(auction,"LeftCaller",new Vector3(-4.1f,.12f,18.7f),Vector3.back,"N13_merchant_male"));
        for(int i=0;i<3;i++)bidders.Add(Person(auction,"LeftBidder"+i,new Vector3(-3.0f-i*1.65f,.12f,13.5f),Vector3.forward,i%2==0?"N14_merchant_female":"N13_merchant_male"));
        activity.bidders=bidders.ToArray();activity.voice=auction.gameObject.AddComponent<AudioSource>();activity.voice.playOnAwake=false;activity.voice.spatialBlend=1;activity.voice.minDistance=5;activity.voice.maxDistance=34;activity.voice.volume=.45f;activity.callClip=Voice("auction-call");activity.soldClip=Voice("auction-sold");
        var banner=Node(room,"D_ColdAuction",new Vector3(0,0,30)).gameObject.AddComponent<NoryangjinBannerEvent>();banner.triggerAhead=30;banner.banner="경매 중! 가운데로 지나가세요";
        root.GetComponent<NoryangjinMarketAtmosphere>().auction=banner;
        var reward=Node(room,"D2_AuctionReward",new Vector3(0,0,94)).gameObject.AddComponent<NoryangjinRewardChoiceEvent>();reward.triggerAhead=24;reward.banner="경매장 통과 보상! 한쪽을 선택하세요";reward.padOffset=1.1f;reward.padWidth=1.8f;
        reward.shieldIcon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ShooterSurvival/UI/CoastalEnamel/SahurShield.png");reward.healIcon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ShooterSurvival/Resources/WallBonusIcons/WallBonus_HealthPercent.png");
        var visibility=root.GetComponentInChildren<NoryangjinInteriorDetailVisibility>();
        var bound=new Bounds(new Vector3(-67,4,-278),new Vector3(22,9,138));
        if(visibility!=null)visibility.coldRoadVisuals=GameObject.Find("Noryangjin_MapTool/Roads").GetComponentsInChildren<Renderer>(true).Where(r=>!r.transform.IsChildOf(roads)&&r.bounds.Intersects(bound)).ToArray();
        var props=GameObject.Find("Noryangjin_MapTool/Props");
        if(props!=null)foreach(Transform prop in props.transform)
        {
            if(prop.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinTurnSpot>()!=null)continue;
            if(prop.GetComponentsInChildren<Renderer>(true).Any(r=>r.bounds.Intersects(bound)))prop.gameObject.SetActive(false);
        }
    }
    static Animator Person(Transform parent,string name,Vector3 p,Vector3 facing,string id)
    {
        facing.y=0;var body=Model(id,parent,p,Quaternion.LookRotation(facing.normalized),1);body.name=name;
        return body.GetComponentInChildren<Animator>();
    }
    static void ConfigureContainer(Transform root)
    {
        var e=root.GetComponentsInChildren<NoryangjinMarketIncident>().First(x=>x.kind==NoryangjinMarketIncident.Kind.ContainerDrop);
        e.triggerAhead=38;e.warningSeconds=1.05f;e.dropSeconds=.22f;e.duration=e.hazardSeconds=7;e.lane=-1.4f;
        e.impactEffect=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Smoke Poof.prefab");
        if(e.warning!=null)Object.DestroyImmediate(e.warning.gameObject);
        e.warning=Ground(e.transform,"ContainerShadow",new Vector3(e.lane,.32f,0),new Vector2(3.3f,5.4f),PatchMaterial("ContainerShadow",new Color(.015f,.018f,.022f,.65f),false));
    }
    static void SplitRoadScenery(Transform roadRoot)
    {
        foreach(var r in roadRoot.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.transform.parent==roadRoot).ToArray())
        {
            var filter=r.GetComponent<MeshFilter>();if(filter==null)continue;
            var saved=r.GetComponent<NoryangjinRoadScenerySplit>();
            if(saved!=null){filter.sharedMesh=saved.source;if(saved.overhead!=null)Object.DestroyImmediate(saved.overhead.gameObject);}
            // These dock modules bundle low walkable planks and tall facades in one mesh.
            // Elevated roads and ramps retain their authored renderer and their existing height-aware occlusion.
            if(r.bounds.min.y>-10||r.bounds.max.y>6||r.bounds.max.y<1)continue;
            var source=filter.sharedMesh;var vertices=source.vertices;var upper=new List<int>[source.subMeshCount];var lower=new List<int>[source.subMeshCount];int count=0;
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                upper[sub]=new List<int>();lower[sub]=new List<int>();var indices=source.GetTriangles(sub);
                for(int i=0;i<indices.Length;i+=3)
                {
                    bool high=false;for(int j=0;j<3;j++)if(filter.transform.TransformPoint(vertices[indices[i+j]]).y>.65f)high=true;
                    var list=high?upper[sub]:lower[sub];for(int j=0;j<3;j++)list.Add(indices[i+j]);if(high)count+=3;
                }
            }
            if(count==0)continue;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string sourceGuid,out long fileId);
            string key=Hash128.Compute(sourceGuid+"/"+fileId+"/"+string.Join(",",upper.SelectMany(s=>s))).ToString();
            var ground=SaveSplit(source,lower,Dir+"/RoadGround_"+key+".asset");var roof=SaveSplit(source,upper,Dir+"/RoadScenery_"+key+".asset");
            filter.sharedMesh=ground;var child=Node(r.transform,"FadableRoadScenery",Vector3.zero);child.gameObject.AddComponent<MeshFilter>().sharedMesh=roof;
            var overhead=child.gameObject.AddComponent<MeshRenderer>();overhead.sharedMaterials=r.sharedMaterials;
            saved??=r.gameObject.AddComponent<NoryangjinRoadScenerySplit>();saved.source=source;saved.overhead=overhead;fade.Add(child);
            var visibility=Object.FindFirstObjectByType<NoryangjinInteriorDetailVisibility>();
            if(visibility!=null&&visibility.oldRoadVisuals.Contains(r))visibility.oldRoadVisuals=visibility.oldRoadVisuals.Append(overhead).ToArray();
        }
    }
    static Mesh SaveSplit(Mesh original,List<int>[] submeshes,string path)
    {
        if(splitMeshes.TryGetValue(path,out var cached))return cached;
        var mesh=Object.Instantiate(original);mesh.name=Path.GetFileNameWithoutExtension(path);
        for(int i=0;i<submeshes.Length;i++)mesh.SetTriangles(submeshes[i],i,false);
        var vertices=mesh.vertices;var used=submeshes.SelectMany(s=>s).Distinct().ToArray();
        var bounds=new Bounds(vertices[used[0]],Vector3.zero);foreach(int index in used)bounds.Encapsulate(vertices[index]);mesh.bounds=bounds;
        var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(prior==null){AssetDatabase.CreateAsset(mesh,path);splitMeshes[path]=mesh;return mesh;}
        EditorUtility.CopySerialized(mesh,prior);Object.DestroyImmediate(mesh);splitMeshes[path]=prior;return prior;
    }
    static AudioClip Voice(string name)
    {
        string path=Dir+"/"+name+".wav";if(!File.Exists(path))File.Copy("outputs/noryangjin-feedback-v3-2026-09-28/audio/"+name+".wav",path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    static Transform Node(Transform parent,string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
    static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(go.GetComponent<Collider>());go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;}
    static void AddFloorCollider(Transform t)=>t.gameObject.AddComponent<MeshCollider>().sharedMesh=t.GetComponent<MeshFilter>().sharedMesh;
    static Transform Ground(Transform parent,string name,Vector3 p,Vector2 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Quad);Object.DestroyImmediate(go.GetComponent<Collider>());go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(90,0,0);go.transform.localScale=new Vector3(size.x,size.y,1);go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return go.transform;
    }
    static Transform Model(string id,Transform parent,Vector3 p,Quaternion q,float scale,bool exactOcclusion=false)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+id+".prefab"),parent);var t=go.transform;t.localPosition=p;t.localRotation=q;t.localScale=Vector3.one*scale;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var output=new Material[r.sharedMaterials.Length];
            for(int i=0;i<output.Length;i++)
            {
                var source=r.sharedMaterials[i];var texture=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.mainTexture;
                var m=Mat(id+"_"+i,Color.white,id.StartsWith("N1")?.45f:.25f,id=="N15_refrigeration_unit"?.25f:.05f);m.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(m);output[i]=m;
            }
            r.sharedMaterials=output;PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            if(exactOcclusion&&r.TryGetComponent<MeshFilter>(out var filter))r.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);return t;
    }
    static TMP_Text Label(Transform parent,string name,string text,Vector3 p,Vector2 size,float pointSize,Color color)
    {
        var scale=parent.lossyScale;var t=Node(parent,name,new Vector3(p.x/scale.x,p.y/scale.y,p.z/scale.z)).gameObject.AddComponent<TextMeshPro>();t.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        t.font=font;t.text=text;t.color=color;t.fontSize=pointSize;t.enableAutoSizing=true;t.fontSizeMin=pointSize*.6f;t.fontSizeMax=pointSize;t.alignment=TextAlignmentOptions.Center;t.fontStyle=FontStyles.Bold;t.rectTransform.sizeDelta=size;return t;
    }
    static Material Mat(string name,Color color,float smooth=.3f,float metal=0,bool unlit=false)
    {
        if(mats.TryGetValue(name,out var cached))return cached;string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);m.enableInstancing=true;EditorUtility.SetDirty(m);mats[name]=m;return m;
    }
    static Material PatchMaterial(string name,Color color,bool wet)
    {var m=Mat(name,color);m.shader=Shader.Find("ShooterSurvival/NoryangjinGroundPatch");m.SetColor("_BaseColor",color);m.SetFloat("_Wet",wet?1:0);EditorUtility.SetDirty(m);return m;}
    static Material ParticleMaterial()
    {
        var m=Mat("SplashParticles",Color.white);m.shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
        string path=Dir+"/SoftDroplet.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(texture==null)
        {
            texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="SoftDroplet",wrapMode=TextureWrapMode.Clamp};var pixels=new Color[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),.6f));}
            texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,path);
        }
        m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",new Color(.7f,.91f,1,.6f));m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;EditorUtility.SetDirty(m);return m;
    }
}
