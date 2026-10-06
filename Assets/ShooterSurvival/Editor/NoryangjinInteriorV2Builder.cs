using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Reproducible reference-led visual replacement, called by the revamp installer after its mechanics.
public static class NoryangjinInteriorV2Builder
{
    const string AssetsRoot = "Assets/ShooterSurvival/Models/Generated/NoryangjinInteriorV2";
    const string Prefabs = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    static readonly string[] Required = { "N09_driven_turret", "N10_crab_aquarium", "N11_fish_counter", "N12_foam_box", "N13_merchant_male", "N14_merchant_female" };
    static readonly Dictionary<string,Material> materials = new();
    static readonly List<Transform> occluders = new(), bays = new();
    static readonly List<Light> lamps = new();
    static readonly List<TMP_Text> clocks = new(), news = new();
    static TMP_FontAsset font;
    static Mesh shadeMesh;

    public static Transform[] Apply(Transform root, float pathHalf, TMP_FontAsset labelFont)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Install in Edit Mode");
        foreach (var id in Required) if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+id+".prefab")==null)
            throw new InvalidOperationException("Replacement model missing: "+id);
        font=labelFont;materials.Clear();occluders.Clear();bays.Clear();lamps.Clear();clocks.Clear();news.Clear();
        Directory.CreateDirectory(AssetsRoot); Directory.CreateDirectory(AssetsRoot+"/Materials");
        var hall=root.Find("MarketHall");
        if(hall==null)throw new InvalidOperationException("Base market events must exist first");
        // Keep the event anchors and colliders; replace the entire authored scenery family.
        foreach(Transform child in hall.Cast<Transform>().ToArray())
            if(child.GetComponent<NoryangjinRevampEvent>()==null)Object.DestroyImmediate(child.gameObject);
        var quality=Node(hall,"ReferenceMarketInterior",Vector3.zero);
        var floorTexture=Texture("WetFloor","outputs/noryangjin-interior-v2-2026-09-28/wet-floor.png",1024);
        var tvTexture=Texture("NewsScreen","outputs/noryangjin-interior-v2-2026-09-28/news-screen.png",1024);
        var ceiling=Mat("WarmWhiteCeiling",new Color(.88f,.88f,.82f),unlit:true);
        var walls=Mat("IvoryTiles",new Color(.83f,.86f,.84f));
        var grout=Mat("FineTileJoint",new Color(.65f,.69f,.68f));
        var steel=Mat("Steel",new Color(.5f,.57f,.59f),.72f,.6f);
        var navy=Mat("BlueEnamel",new Color(.025f,.16f,.5f),.5f);
        var white=Mat("PaintedIvory",new Color(.91f,.92f,.87f),.45f);
        var pipe=Mat("PipeGraphite",new Color(.17f,.22f,.24f),.45f,.2f);
        var emit=Mat("LampWarm",new Color(1,.91f,.72f),unlit:true);
        var floorCollider=GameObject.Find("Noryangjin_MapTool/Roads/NoryangjinRevampSurfaces/IndoorTileSurface");
        if(floorCollider!=null)floorCollider.GetComponent<Renderer>().enabled=false;
        const float length=378, width=13.6f, height=7.8f;
        shadeMesh=Shade();
        for(float d=0;d<length;d+=12)
        {
            float size=Mathf.Min(12,length-d);
            var segment=Node(quality,"HallSegment_"+d,new Vector3(0,0,d+size*.5f));bays.Add(segment);
            Box(segment,"CeilingJointBacking",new Vector3(0,height+.075f,0),new Vector3(width,.025f,size),Mat("CeilingGrout",new Color(.55f,.59f,.59f),unlit:true));
            var floor=Mat("WetFloor_"+size,Color.white,.88f,.07f);floor.SetTexture("_BaseMap",floorTexture);
            floor.SetTextureScale("_BaseMap",new Vector2(width/2.4f,size/2.4f));EditorUtility.SetDirty(floor);
            Box(segment,"WetFloor",new Vector3(0,.101f,0),new Vector3(width,.018f,size+.02f),floor);
            foreach(float side in new[]{-1f,1f})
            {
                Box(segment,"TiledWall",new Vector3(side*width*.5f,3.8f,0),new Vector3(.16f,7.6f,size),walls);
                Box(segment,"WallBase",new Vector3(side*(width*.5f-.09f),.42f,0),new Vector3(.06f,.84f,size),navy);
                for(float y=1.2f;y<5.9f;y+=.65f)Box(segment,"WallTileJoint",new Vector3(side*(width*.5f-.09f),y,0),new Vector3(.02f,.014f,size),grout);
                Box(segment,"Drain",new Vector3(side*(pathHalf+.45f),.12f,0),new Vector3(.18f,.025f,size),pipe);
                Box(segment,"PipeRail",new Vector3(side*3.35f,7.13f,0),new Vector3(.10f,.12f,size),pipe);
                Box(segment,"TiledColumn",new Vector3(side*6.0f,3.85f,-size*.5f),new Vector3(.45f,7.7f,.48f),white);
            }
            // Small ceiling tiles, fine seams and shallow T rails instead of massive grey strips.
            for(int row=0;row<10;row++)
            {
                float z=-size*.5f+(row+.5f)*size/10;
                for(int col=0;col<4;col++)
                {
                    var panel=Box(segment,"V2Roof",new Vector3((col-1.5f)*3.35f,height,z),new Vector3(3.31f,.075f,size/10-.035f),ceiling);
                    panel.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                }
            }
            occluders.Add(segment.Find("V2Roof"));
            Box(segment,"CeilingSpine",new Vector3(0,height-.04f,0),new Vector3(.045f,.055f,size),steel);
            Box(segment,"SlimCrossbeam",new Vector3(0,height-.12f,-size*.5f),new Vector3(width,.13f,.11f),white);
            for(int i=0;i<2;i++)
            {
                float z=-size*.25f+i*size*.5f;
                foreach(float side in new[]{-1f,1f})Pendant(segment,new Vector3(side*3.0f,6.35f,z),white,emit,pipe);
                Box(segment,"FluorescentHousing",new Vector3(0,7.64f,z),new Vector3(.28f,.12f,2.6f),white);
                Box(segment,"FluorescentDiffuser",new Vector3(0,7.55f,z),new Vector3(.19f,.035f,2.45f),Mat("CoolTube",new Color(.87f,.97f,1),unlit:true));
            }
            if((int)d%24==0)
            {
                var light=Node(segment,"AisleBounce",new Vector3(0,5.5f,0)).gameObject.AddComponent<Light>();
                light.type=LightType.Point;light.color=new Color(1,.9f,.74f);light.intensity=3.4f;light.range=13;light.shadows=LightShadows.None;lamps.Add(light);
            }
        }
        string[] shops={"오성수산","대흥수산","동해수산","한강수산","광주수산","남도수산","제주수산","여수상회"};
        for(int i=0;i<75;i++)foreach(float side in new[]{-1f,1f})
        {
            float d=4+i*4.9f+(side>0?1.8f:0);if(d>length-6)continue;
            var bay=Node(quality,"StallBay_"+i+"_"+side,new Vector3(side*4.6f,0,d));bays.Add(bay);
            bool aquarium=(i+(side>0?1:0))%3!=1;
            var cabinet=Place(aquarium?"N10_crab_aquarium":"N11_fish_counter",bay,Vector3.up*.12f,Quaternion.Euler(0,side<0?90:-90,0),aquarium?.94f:.88f);
            Surface(cabinet,aquarium?"Aquarium":"FishCounter",.4f,aquarium?.03f:.16f);
            if(aquarium)
            {
                // A restrained glow beneath the water shelf, not an opaque neon block.
                Box(bay,"AquariumGlow",new Vector3(-side*.63f,1.45f,0),new Vector3(.025f,.045f,2.9f),Mat("AquaGlow",new Color(.3f,.78f,.88f),unlit:true));
            }
            var foam=Place("N12_foam_box",bay,new Vector3(-side*1.3f,.12f,(i%2==0?1.7f:-1.7f)),Quaternion.Euler(0,i%2*12,0),.7f);
            Surface(foam,"Foam",.22f,0);
            if(i%3==0)
            {
                var top=Place("N12_foam_box",bay,new Vector3(-side*1.3f,.50f,1.7f),Quaternion.Euler(0,-8,0),.7f);Surface(top,"Foam",.22f,0);
                Bucket(bay,new Vector3(-side*1.0f,.1f,-1.8f),i%2==0?new Color(.8f,.13f,.08f):new Color(.06f,.3f,.64f));
            }
            if(i%4==0)
            {
                var sign=Node(quality,"NumberedStoreSign",new Vector3(side*3.25f,5.5f,d));bays.Add(sign);
                Box(sign,"Frame",Vector3.zero,new Vector3(2.35f,1.25f,.12f),Mat("SignRim",new Color(.08f,.15f,.23f),.65f));
                Box(sign,"BlueFace",new Vector3(0,0,-.07f),new Vector3(2.20f,1.1f,.03f),Mat("SignFace",new Color(.035f,.17f,.64f),unlit:true));
                Box(sign,"Separator",new Vector3(0,.20f,-.095f),new Vector3(2.05f,.025f,.008f),white);
                Label(sign,"Category","활어 · 선어",new Vector3(0,.40f,-.10f),new Vector2(2.08f,.34f),1.9f,Color.white);
                Label(sign,"Shop",shops[(i+(side>0?2:0))%shops.Length],new Vector3(0,-.20f,-.10f),new Vector2(2.08f,.6f),2.7f,new Color(1,.89f,.4f));
                foreach(var label in sign.GetComponentsInChildren<TMP_Text>().ToArray())
                {
                    var back=Object.Instantiate(label.gameObject,sign).transform;back.name=label.name+"_Back";
                    back.localPosition=new Vector3(label.transform.localPosition.x,label.transform.localPosition.y,.135f);
                    back.localRotation=Quaternion.Euler(0,180,0);
                }
                Box(sign,"Hanger",new Vector3(0,1.14f,0),new Vector3(.035f,1.15f,.035f),pipe);
                occluders.Add(sign);
            }
        }
        foreach(float d in new[]{28f,96f,188f,282f})
        {
            var tv=Node(quality,"Television",new Vector3(d<150?-1.1f:1.1f,6.1f,d));bays.Add(tv);
            Box(tv,"TVBody",Vector3.zero,new Vector3(2.95f,1.75f,.22f),Mat("TVCharcoal",new Color(.035f,.05f,.06f),.35f));
            var screenMat=Mat("TVPicture",Color.white,unlit:true);screenMat.SetTexture("_BaseMap",tvTexture);EditorUtility.SetDirty(screenMat);
            Screen(tv,new Vector3(0,.10f,-.125f),new Vector2(2.75f,1.52f),screenMat);
            Box(tv,"TickerBand",new Vector3(0,-.91f,-.13f),new Vector3(2.95f,.32f,.04f),Mat("TickerRed",new Color(.65f,.035f,.045f),unlit:true));
            news.Add(Label(tv,"Ticker","속보: 상어, 노량진 시장동 진입",new Vector3(0,-.91f,-.155f),new Vector2(2.8f,.3f),1.2f,Color.white));
            Box(tv,"Suspension",new Vector3(0,1.25f,.05f),new Vector3(.085f,.8f,.085f),pipe);occluders.Add(tv);
        }
        foreach(float d in new[]{15f,142f,268f})
        {
            var clock=Node(quality,"DigitalClockV2",new Vector3(0,6.96f,d));bays.Add(clock);
            Box(clock,"ClockCase",Vector3.zero,new Vector3(1.65f,.64f,.15f),Mat("ClockBlack",new Color(.018f,.026f,.028f),unlit:true));
            clocks.Add(Label(clock,"ClockDigits","04:30",new Vector3(0,0,-.083f),new Vector2(1.5f,.5f),2.6f,new Color(1,.16f,.12f)));
        }
        Portico(quality,0,"노량진수산시장  시장동",white,navy);Portico(quality,length,"출구",white,navy);
        ReplaceTruck(root,pathHalf);ReplaceBoxWalls(root);ReplacePeople(root);ReplaceCarts(root);ReplaceHoses(root);CompactReadouts(root);
        var visibility=quality.gameObject.AddComponent<NoryangjinInteriorDetailVisibility>();visibility.bays=bays.ToArray();visibility.localLights=lamps.ToArray();
        visibility.hangingDisplays=quality.Cast<Transform>().Where(t=>t.name=="Television"||t.name=="NumberedStoreSign"||t.name=="DigitalClockV2").ToArray();
        var hallBounds=new Bounds(new Vector3(124.3f,4,-144),new Vector3(13.6f,8,378));
        visibility.oldRoadVisuals=GameObject.Find("Noryangjin_MapTool/Roads").GetComponentsInChildren<Renderer>(true).Where(r=>!r.transform.IsChildOf(floorCollider.transform.parent)&&r.bounds.Intersects(hallBounds)).ToArray();
        var coldBounds=new Bounds(new Vector3(-67,4,-283.5f),new Vector3(13.6f,8,99));
        visibility.coldRoadVisuals=GameObject.Find("Noryangjin_MapTool/Roads").GetComponentsInChildren<Renderer>(true).Where(r=>!r.transform.IsChildOf(floorCollider.transform.parent)&&r.bounds.Intersects(coldBounds)).ToArray();
        visibility.fogCurtain=Box(quality,"DistantAir",new Vector3(0,4,75),new Vector3(width,8,.06f),Mat("DistantAir",new Color(.65f,.71f,.72f),unlit:true));
        visibility.fogCurtain.gameObject.SetActive(false);
        var ambience=root.GetComponent<NoryangjinMarketAtmosphere>();
        ambience.clocks=clocks.Concat(root.GetComponentsInChildren<TMP_Text>().Where(t=>t.transform.parent.name=="AuctionClock")).ToArray();
        ambience.televisions=news.ToArray();
        AddReflections(quality);
        AssetDatabase.SaveAssets();
        File.WriteAllText("outputs/noryangjin-interior-v2-2026-09-28/visual-install.json",JsonUtility.ToJson(new Receipt{version="reference-interior-v2",bayGroups=bays.Count,localLights=lamps.Count,newModels=Required},true));
        return occluders.ToArray();
    }
    [Serializable] class Receipt{public string version;public int bayGroups,localLights;public string[] newModels;}

    static void Screen(Transform parent,Vector3 position,Vector2 size,Material material)
    {
        string path=AssetsRoot+"/UprightScreen.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            mesh=new Mesh{name="UprightScreen"};mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        var screen=Node(parent,"Picture",position);screen.localScale=new Vector3(size.x,size.y,1);
        screen.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;screen.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    static void CompactReadouts(Transform root)
    {
        foreach(var block in root.GetComponentsInChildren<NoryangjinBreakable>(true))
        {
            block.healthReadoutDistance=24;
            if(block.name=="TurretTruck")continue;
            block.healthFontSize=3.2f;
            var ui=Node(block.transform,"CompactDamage",Vector3.up*block.healthLabelHeight);block.healthBarRoot=ui;
            block.attachedDamageLabel=Label(ui,"Damage","",new Vector3(2.0f,-.2f,0),new Vector2(1.7f,.5f),2.2f,new Color(1,.37f,.2f));
        }
    }
    static void ReplaceHoses(Transform root)
    {
        var thread=Mat("SprayThread",new Color(.65f,.88f,1),unlit:true);
        var drop=Mat("SprayDrop",new Color(.38f,.73f,1),.8f);
        var warning=Mat("JetWarning",new Color(1,.65f,.08f,.35f),unlit:true);
        warning.SetFloat("_Surface",1);warning.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);warning.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);warning.SetFloat("_ZWrite",0);warning.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");warning.SetOverrideTag("RenderType","Transparent");warning.renderQueue=3000;EditorUtility.SetDirty(warning);
        foreach(var hose in root.GetComponentsInChildren<NoryangjinHoseEvent>())foreach(var jet in hose.jets)
        {
            foreach(Transform child in jet.visual.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            var spray=jet.visual.gameObject.AddComponent<NoryangjinHoseSprayVisual>();spray.end=new Vector3(-jet.side*jet.reach,.16f,0);
            var lines=new List<LineRenderer>();
            for(int i=0;i<3;i++)
            {
                var line=Node(jet.visual,"WaterThread"+i,Vector3.zero).gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=18;line.startWidth=.04f-i*.008f;line.endWidth=.017f;line.numCapVertices=3;line.sharedMaterial=thread;line.shadowCastingMode=ShadowCastingMode.Off;lines.Add(line);
            }
            spray.threads=lines.ToArray();var drops=new List<Transform>();
            for(int i=0;i<12;i++)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);Object.DestroyImmediate(go.GetComponent<Collider>());go.name="WaterDrop";go.transform.SetParent(jet.visual,false);go.transform.localScale=Vector3.one*(.055f+(i%3)*.02f);go.GetComponent<Renderer>().sharedMaterial=drop;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;drops.Add(go.transform);
            }
            spray.droplets=drops.ToArray();
            var hardware=Node(hose.transform,"HoseNozzle",jet.visual.localPosition);
            Cylinder(hardware,"Nozzle",new Vector3(jet.side*.3f,.95f,0),new Vector3(.10f,.3f,.10f),Quaternion.Euler(0,0,90),Mat("HoseNozzleBlue",new Color(.035f,.15f,.42f),.5f));
            Cylinder(hardware,"SteelTip",new Vector3(0,.95f,0),new Vector3(.13f,.06f,.13f),Quaternion.Euler(0,0,90),Mat("Steel",new Color(.5f,.57f,.59f),.72f,.6f));
            jet.warning=Node(hose.transform,"JetWarning",jet.visual.localPosition+Vector3.up*.145f);
            var border=jet.warning.gameObject.AddComponent<LineRenderer>();border.useWorldSpace=false;border.loop=true;border.positionCount=4;border.startWidth=border.endWidth=.045f;border.sharedMaterial=warning;border.shadowCastingMode=ShadowCastingMode.Off;
            border.SetPositions(new[]{new Vector3(0,0,-hose.bandHalfDepth),new Vector3(-jet.side*jet.reach,0,-hose.bandHalfDepth),new Vector3(-jet.side*jet.reach,0,hose.bandHalfDepth),new Vector3(0,0,hose.bandHalfDepth)});
        }
    }

    static void ReplacePeople(Transform root)
    {
        var source=new[]{Load("N13_merchant_male"),Load("N14_merchant_female")};
        var templates=root.Find("Templates");int i=0;
        foreach(Transform actor in templates)
            if(actor.GetComponent<EnemyScript_space>()!=null && !actor.name.Contains("Woman"))ReplacePerson(actor,source[i++%2]);
        foreach(var pose in root.GetComponentsInChildren<NoryangjinCrowdPose>(true))
        {
            if(pose.name=="TurretDriver")continue;
            ReplacePerson(pose.transform,source[i++%2]);
            if(pose.name=="HoseWorker")pose.state=ForwardEnemyAnimationContract.Idle;
        }
        foreach(var rush in root.GetComponentsInChildren<NoryangjinRushEvent>())
        {
            // Existing templates now have the new body; their combat and reward contracts stay intact.
            rush.rowGap=Mathf.Max(rush.rowGap,3.0f);
        }
    }
    static void ReplacePerson(Transform actor,GameObject body)
    {
        foreach(var animator in actor.GetComponentsInChildren<Animator>(true).ToArray())
            if(animator.transform!=actor)Object.DestroyImmediate(animator.gameObject);else Object.DestroyImmediate(animator);
        foreach(var renderer in actor.GetComponentsInChildren<Renderer>(true).ToArray())
        {
            if(renderer==null||renderer.GetComponent<TMP_Text>()!=null||renderer.GetComponentInParent<Canvas>()!=null)continue;
            if(renderer.transform==actor)Object.DestroyImmediate(renderer);else Object.DestroyImmediate(renderer.gameObject);
        }
        var replacement=(GameObject)PrefabUtility.InstantiatePrefab(body,actor);replacement.name="NewMarketBody";
        replacement.transform.localPosition=new Vector3(0,.12f/actor.lossyScale.y,0);replacement.transform.localRotation=Quaternion.Euler(0,180,0);
        var s=actor.lossyScale;replacement.transform.localScale=new Vector3(1/s.x,1/s.y,1/s.z);
        PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.transform);
        Surface(replacement.transform,body.name,.25f,0);
        foreach(var canvas in actor.GetComponentsInChildren<Canvas>(true))
        {
            var rect=(RectTransform)canvas.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition3D=Vector3.up*(2.75f/s.y);
            rect.localScale=Vector3.one*.42f;
            canvas.gameObject.AddComponent<NoryangjinFaceCamera>();
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);EditorUtility.SetDirty(rect);
        }
        if(actor.TryGetComponent<CapsuleCollider>(out var cap))
        {cap.height=2.25f/s.y;cap.radius=.44f/Mathf.Max(s.x,s.z);cap.center=new Vector3(0,1.13f/s.y,0);}
        var combat=actor.GetComponent<EnemyScript_space>();
        if(combat!=null)
        {
            var so=new SerializedObject(combat);
            foreach(string field in new[]{"heldProjectile","throwPoint"}){var prop=so.FindProperty(field);if(prop!=null)prop.objectReferenceValue=null;}
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    static void ReplaceTruck(Transform root,float pathHalf)
    {
        var e=root.GetComponentsInChildren<NoryangjinTurretEvent>().Single();var truck=e.turret;
        foreach(Transform child in truck.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        var body=Place("N09_driven_turret",truck.transform,Vector3.up*.12f,Quaternion.Euler(0,90,0),1);Surface(body,"DrivenTricycle",.4f,.03f);
        var boxes=new List<Transform>();
        for(int y=0;y<3;y++)for(int x=0;x<2;x++)for(int z=0;z<3;z++)
        {
            var box=Place("N12_foam_box",truck.transform,new Vector3((x-.5f)*.85f,.91f+y*.405f,.42f+z*.57f),Quaternion.Euler(0,y%2==0?0:5,0),.75f);
            Surface(box,"Foam",.22f,0);boxes.Add(box);
        }
        truck.shedPieces=boxes.ToArray();truck.healthLabelHeight=4.18f;truck.healthFontSize=4.2f;truck.halfDepth=2.35f;
        var ui=Node(truck.transform,"BossReadout",new Vector3(0,3.6f,0));truck.healthBarRoot=ui;
        Box(ui,"HealthBack",Vector3.zero,new Vector3(3.5f,.22f,.035f),Mat("BossBarBack",new Color(.03f,.05f,.08f),unlit:true));
        truck.healthBarFill=Box(ui,"HealthFill",new Vector3(0,0,-.028f),new Vector3(3.35f,.13f,.025f),Mat("BossBarRed",new Color(.94f,.13f,.06f),unlit:true));
        truck.attachedDamageLabel=Label(ui,"Damage","",new Vector3(2.35f,-.18f,-.03f),new Vector2(1.8f,.55f),2.7f,new Color(1,.37f,.2f));
    }
    static void ReplaceBoxWalls(Transform root)
    {
        foreach(var block in root.GetComponentsInChildren<NoryangjinBreakable>())
        {
            if(block.name=="TurretTruck"||block.shedPieces.Length==0)continue;
            var parts=new List<Transform>();
            foreach(var old in block.shedPieces)
            {
                if(old==null)continue;
                var b=Bounds(old);var replacement=Place("N12_foam_box",block.transform,old.localPosition,old.localRotation,1);
                replacement.localScale*=Mathf.Max(.2f,b.size.x)/1.15f;
                Surface(replacement,"Foam",.22f,0);parts.Add(replacement);Object.DestroyImmediate(old.gameObject);
            }
            block.shedPieces=parts.ToArray();
        }
    }
    static void ReplaceCarts(Transform root)
    {
        var e=root.GetComponentsInChildren<NoryangjinMarketIncident>().FirstOrDefault(x=>x.name=="E2_CartConvoy");
        if(e==null)return;
        var metal=Mat("CartSteel",new Color(.52f,.59f,.62f),.65f,.45f);var rubber=Mat("CartRubber",new Color(.06f,.08f,.09f));
        foreach(var actor in e.actors)
        {
            foreach(Transform t in actor.Cast<Transform>().ToArray())Object.DestroyImmediate(t.gameObject);
            Box(actor,"CartDeck",new Vector3(0,.42f,0),new Vector3(1.6f,.12f,1.1f),metal);
            foreach(float x in new[]{-.67f,.67f})foreach(float z in new[]{-.42f,.42f})
            {
                Cylinder(actor,"Wheel",new Vector3(x,.23f,z),new Vector3(.36f,.10f,.36f),Quaternion.Euler(0,0,90),rubber);
            }
            foreach(float z in new[]{-.47f,.47f})Box(actor,"HandlePost",new Vector3(-.7f,1.0f,z),new Vector3(.07f,1.1f,.07f),metal);
            Box(actor,"Handle",new Vector3(-.7f,1.55f,0),new Vector3(.09f,.09f,1.0f),metal);
            for(int y=0;y<2;y++){var box=Place("N12_foam_box",actor,new Vector3(.1f,.49f+y*.43f,0),Quaternion.identity,.8f);Surface(box,"Foam",.22f,0);}
        }
    }
    static void AddReflections(Transform root)
    {
        for(int i=0;i<3;i++)
        {
            var probe=Node(root,"MarketReflection"+i,new Vector3(0,3.2f,45+i*138)).gameObject.AddComponent<ReflectionProbe>();
            probe.size=new Vector3(14,8,150);probe.boxProjection=true;probe.blendDistance=8;probe.resolution=128;probe.hdr=true;
            probe.mode=ReflectionProbeMode.Baked;probe.cullingMask=~0;probe.intensity=.9f;
            string path=AssetsRoot+"/MarketReflection"+i+".exr";
            var texture=AssetDatabase.LoadAssetAtPath<Texture>(path);
            if(texture==null)
            {
                if(!File.Exists(path))Lightmapping.BakeReflectionProbe(probe,path);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                texture=AssetDatabase.LoadAssetAtPath<Texture>(path);
            }
            if(texture!=null){probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=texture;}
        }
    }
    static void Portico(Transform parent,float d,string title,Material white,Material blue)
    {
        var gate=Node(parent,"MarketPortico",new Vector3(0,0,d));
        foreach(float side in new[]{-1f,1f})Box(gate,"Column",new Vector3(side*5.3f,3.8f,0),new Vector3(.6f,7.6f,.6f),white);
        Box(gate,"Lintel",new Vector3(0,7.0f,0),new Vector3(11,.9f,.45f),white);
        Box(gate,"TitleFace",new Vector3(0,7.0f,-.24f),new Vector3(7.8f,.68f,.035f),blue);
        Label(gate,"Title",title,new Vector3(0,7.0f,-.268f),new Vector2(7.5f,.62f),3.8f,Color.white);occluders.Add(gate);
    }
    static void Pendant(Transform parent,Vector3 p,Material shade,Material emission,Material wire)
    {
        var lamp=Node(parent,"Pendant",p);var mesh=lamp.gameObject.AddComponent<MeshFilter>();mesh.sharedMesh=shadeMesh;
        lamp.gameObject.AddComponent<MeshRenderer>().sharedMaterial=shade;
        Cylinder(lamp,"Diffuser",new Vector3(0,-.02f,0),new Vector3(.78f,.017f,.78f),Quaternion.identity,emission);
        Cylinder(lamp,"Suspension",new Vector3(0,.78f,0),new Vector3(.035f,.5f,.035f),Quaternion.identity,wire);
    }
    static void Bucket(Transform parent,Vector3 p,Color color)
    {
        Cylinder(parent,"Bucket",p+Vector3.up*.26f,new Vector3(.5f,.26f,.5f),Quaternion.identity,Mat("Bucket"+ColorUtility.ToHtmlStringRGB(color),color,.45f));
        Cylinder(parent,"BucketWater",p+Vector3.up*.525f,new Vector3(.43f,.005f,.43f),Quaternion.identity,Mat("BucketWater",new Color(.1f,.33f,.45f),.85f));
    }
    static Mesh Shade()
    {
        string path=AssetsRoot+"/PendantShadeSmooth.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null)return saved;
        var profile=new[]{new Vector2(.08f,.32f),new Vector2(.15f,.29f),new Vector2(.3f,.19f),new Vector2(.47f,.03f),new Vector2(.5f,0),new Vector2(.47f,-.025f)};
        var vertices=new List<Vector3>();var tris=new List<int>();const int n=32;
        foreach(var ring in profile)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;vertices.Add(new Vector3(Mathf.Cos(a)*ring.x,ring.y,Mathf.Sin(a)*ring.x));}
        for(int r=0;r<profile.Length-1;r++)for(int i=0;i<n;i++){int a=r*n+i,b=r*n+(i+1)%n,c=a+n,d=b+n;tris.AddRange(new[]{a,b,c,b,d,c});}
        var mesh=new Mesh{name="PendantShade"};mesh.SetVertices(vertices);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Transform Node(Transform parent,string name,Vector3 local)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;return t;}
    static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;}
    static Transform Cylinder(Transform parent,string name,Vector3 p,Vector3 size,Quaternion rotation,Material material)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localRotation=rotation;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;}
    static Transform Place(string id,Transform parent,Vector3 p,Quaternion rotation,float scale)
    {var go=(GameObject)PrefabUtility.InstantiatePrefab(Load(id),parent);go.transform.localPosition=p;go.transform.localRotation=rotation;go.transform.localScale=Vector3.one*scale;PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);return go.transform;}
    static GameObject Load(string id)=>AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+id+".prefab");
    static Bounds Bounds(Transform root)
    {var rs=root.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static TMP_Text Label(Transform parent,string name,string text,Vector3 p,Vector2 size,float fontSize,Color color)
    {
        var t=Node(parent,name,p).gameObject.AddComponent<TextMeshPro>();t.font=font;t.text=text;t.color=color;t.fontSize=fontSize;
        t.alignment=TextAlignmentOptions.Center;t.fontStyle=FontStyles.Bold;t.enableAutoSizing=true;t.fontSizeMin=fontSize*.65f;t.fontSizeMax=fontSize;
        t.rectTransform.sizeDelta=size;return t;
    }
    static Material Mat(string name,Color color,float smooth=.25f,float metal=0,bool unlit=false)
    {
        if(materials.TryGetValue(name,out var cached))return cached;
        string path=AssetsRoot+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);
        m.enableInstancing=true;EditorUtility.SetDirty(m);materials[name]=m;return m;
    }
    static void Surface(Transform model,string family,float smooth,float metal)
    {
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var source=renderer.sharedMaterials;var output=new Material[source.Length];
            for(int i=0;i<source.Length;i++)
            {
                Texture baseMap=source[i].HasProperty("_BaseMap")?source[i].GetTexture("_BaseMap"):source[i].mainTexture;
                string key=baseMap!=null?AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(baseMap)):"plain";
                var material=Mat(family+"_"+key,Color.white,smooth,metal);
                material.SetTexture("_BaseMap",baseMap);EditorUtility.SetDirty(material);output[i]=material;
            }
            renderer.sharedMaterials=output;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(model);
    }
    static Texture2D Texture(string name,string source,int size)
    {
        string path=AssetsRoot+"/"+name+".png";if(!File.Exists(path))File.Copy(source,path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.maxTextureSize=size;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
