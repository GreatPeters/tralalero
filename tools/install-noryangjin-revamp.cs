using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Builds the Noryangjin revamp (노량진 맵 개편 기획) into a SAFE COPY of the SR18 scene.
// The original Noryangjin_MapTool_Mode_SR18.unity is never opened for writing.
//   unity command run_script --file tools/install-noryangjin-revamp.cs --entry InstallNoryangjinRevamp.Main
// Re-running rebuilds the NoryangjinRevamp root and re-applies the scene edits idempotently.
public static class InstallNoryangjinRevamp
{
    const string Source = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
    const string Target = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18_Revamp.unity";
    const string MatDir = "Assets/ShooterSurvival/Materials/Generated/NoryangjinRevamp";
    const string MeshDir = "Assets/ShooterSurvival/Models/Generated/NoryangjinRevamp";
    const string Stage01 = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/";
    const string Meshy = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    const string Enemies = "Assets/JH/Model/Prefab/";
    const string Cfxr = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/";

    static Transform root;
    static TMP_FontAsset font;
    static readonly Dictionary<string, Material> mats = new();
    static float pathHalf = 3f;
    static readonly List<string> report = new();
    static GameObject fishTemplate;
    static readonly List<Transform> ceilingGroups = new();
    static Transform newRoads;
    static AudioClip Voice(string name)
    {
        string directory = "Assets/ShooterSurvival/Audio/NoryangjinRevamp";
        Directory.CreateDirectory(directory);
        string target = directory + "/" + name + ".wav";
        if (!File.Exists(target)) File.Copy("outputs/noryangjin-revamp-fix-2026-09-28/audio-v2/" + name + ".wav", target);
        AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(target);
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .65f;
        importer.defaultSampleSettings = settings; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(target);
    }

    // PA lines added for the 2026-09-29 feedback (tools/create-noryangjin-claude-voice.ps1).
    static AudioClip ClaudeVoice(string name)
    {
        string directory = "Assets/ShooterSurvival/Audio/NoryangjinRevamp";
        Directory.CreateDirectory(directory);
        string target = directory + "/" + name + ".wav";
        if (!File.Exists(target)) File.Copy("outputs/noryangjin-claude-fix-2026-09-28/audio/" + name + ".wav", target);
        AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(target);
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .65f;
        importer.defaultSampleSettings = settings; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(target);
    }

    public static object Main()
    {
        report.Clear(); mats.Clear(); ceilingGroups.Clear();
        if (!File.Exists(Target))
        {
            if (!AssetDatabase.CopyAsset(Source, Target)) throw new Exception("copy failed");
            report.Add("copied SR18 -> " + Target);
        }
        var scene = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects().Where(g => g.name == "NoryangjinRevamp").ToArray()) Object.DestroyImmediate(go);
        var roadRoot = GameObject.Find("Noryangjin_MapTool/Roads").transform;
        var previousSurface = roadRoot.Find("NoryangjinRevampSurfaces");
        if (previousSurface != null) Object.DestroyImmediate(previousSurface.gameObject);
        newRoads = new GameObject("NoryangjinRevampSurfaces").transform; newRoads.SetParent(roadRoot, false);
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(MeshDir);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");

        var player = Object.FindFirstObjectByType<PlayerScript>();
        var range = new SerializedObject(player).FindProperty("xRange").vector2Value;
        pathHalf = Mathf.Max(Mathf.Abs(range.x), Mathf.Abs(range.y));
        report.Add($"player xRange {range} -> pathHalf {pathHalf}");

        root = new GameObject("NoryangjinRevamp").transform;
        var director = root.gameObject.AddComponent<NoryangjinRevampDirector>();
        // Stop far enough that the shark's projectiles spawn in front of the wall, not inside it.
        director.stopGap = 2.6f;
        director.breakEffect = Load<GameObject>(Cfxr + "Explosions/CFXR Explosion 1.prefab");
        director.bigBreakEffect = Load<GameObject>(Cfxr + "Explosions/CFXR Explosion 2 Bigger.prefab");
        director.splashEffect = Load<GameObject>(Cfxr + "Liquids/CFXR Water Splash.prefab");
        director.sun = GameObject.Find("MapTool_DirectionalLight")?.GetComponent<Light>();
        if (director.sun != null)
        {
            var baseColor = director.sun.color; float baseIntensity = director.sun.intensity;
            director.sunColor = new Gradient();
            director.sunColor.SetKeys(new[] {
                    new GradientColorKey(new Color(1f, .78f, .62f), 0), new GradientColorKey(baseColor, .3f),
                    new GradientColorKey(baseColor, .72f), new GradientColorKey(new Color(1f, .66f, .42f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            director.sunIntensity = new AnimationCurve(new Keyframe(0, baseIntensity * .82f), new Keyframe(.3f, baseIntensity), new Keyframe(.72f, baseIntensity), new Keyframe(1, baseIntensity * .88f));
        }
        director.shieldAura = ShieldAura();

        var templates = Templates();
        fishTemplate = Load<GameObject>(Meshy + "N08_frozen_tuna.prefab");
        var suppress = new List<GameObject>();

        BuildBarricade();
        BuildMarketHall(templates);
        BuildBranch();
        BuildWaves();
        BuildMysteryGate("RandomGate_1", new Vector3(225.5f, 0, 30f), 0);
        BuildMysteryGate("RandomGate_2", new Vector3(326.7f, 0, 150f), 0);
        BuildAuction(templates);
        BuildFinalBoss();
        BuildMarketIncidents(templates);
        BuildLandmarksAndAtmosphere(director);
        ceilingGroups.AddRange(NoryangjinInteriorV2Builder.Apply(root,pathHalf,font));
        var feedbackOccluders=NoryangjinFeedbackV3Builder.Apply(root,newRoads,pathHalf,font);
        ceilingGroups.AddRange(feedbackOccluders);
        // 2026-09-29 feedback: seagull dives, connector corners, live-fish auction, outlines, end charge, new boss.
        report.AddRange(NoryangjinClaudeFeedbackBuilder.Apply(root, roadRoot, newRoads, pathHalf, font, ClaudeVoice));
        // Last: lift the finished market over the S3 east pier (the outside deck already follows the branch profile).
        var lift = NoryangjinCrossingLiftBuilder.Apply(root, newRoads, roadRoot, root.GetComponentInChildren<NoryangjinMarketBranch>(), Mat("CrossingConcrete", new Color(.6f, .62f, .62f)));
        report.Add($"crossing lift {LiftHeight}m: moved {lift.moved}, deformed {lift.deformed} (subdivided {lift.subdivided}), hid {lift.hiddenRoads} old S6 tiles, {lift.columns} columns, {lift.details.Count} raised props hideable from S3");

        // Encounters replaced by the new set pieces (placement data would re-enable them, so the
        // director keeps them hidden at run time instead of deleting scene objects).
        foreach (var e in Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (e.name.StartsWith("SR18_") && !e.name.Contains("E25")
                && (InZone(e.transform.position) || InHallEvents(e.transform.position) || InIncidentZone(e.transform.position))) suppress.Add(e.gameObject);
        // Fixed bonus pairs standing inside the box-wall / turret / auction stretches.
        var bonuses = GameObject.Find("Noryangjin_MapTool/Bonuses");
        if (bonuses != null)
            foreach (Transform b in bonuses.transform)
                if (InZone(b.position) || InHallEvents(b.position) || InIncidentZone(b.position)) suppress.Add(b.gameObject);
        // Existing gimmicks (holes, lamps, buckets) inside the new set pieces would stack two hazards.
        var gimmickRoot = GameObject.Find("Noryangjin_MapTool/Props");
        if (gimmickRoot != null)
            foreach (Transform g in gimmickRoot.transform)
                if (g.GetComponent<NoryangjinTurnSpot>() == null &&
                    (InHallScenery(g.position) || OverlapsBypass(g) || (g.name.StartsWith("SR18_L_G") && (InZone(g.position) || InIncidentZone(g.position) || InPier(g.position))))) suppress.Add(g.gameObject);
        var hide = root.gameObject.AddComponent<NoryangjinRevampSuppressor>();
        hide.targets = suppress.Distinct().ToArray();
        foreach (var target in hide.targets) target.SetActive(false);
        // Encounters no longer suppressed (the former outdoor auction) are re-enabled at run time by
        // EncounterPlacementController from Data.xlsx, so their saved inactive state does not matter.
        var crossing = root.gameObject.AddComponent<NoryangjinCrossingVisibility>();
        var branchForVisibility = root.GetComponentInChildren<NoryangjinMarketBranch>();
        crossing.targets = roadRoot.GetComponentsInChildren<Renderer>(true).Where(r => !r.transform.IsChildOf(newRoads) && r.bounds.max.y < 10
            && Enumerable.Range(0,64).Any(i => new Bounds(branchForVisibility.Point(Mathf.Min(branchForVisibility.length,i*6),true)+Vector3.up*3,new Vector3(12,8,10)).Intersects(r.bounds))).ToArray();
        player.GetComponent<NoryangjinRoadHeightFollower>()?.Configure(roadRoot, .12f);
        var occlusion = GameObject.Find("MapTool_Camera").GetComponent<NoryangjinCameraOcclusion>();
        if (occlusion != null)
        {
            occlusion.ConfigureAdditionalOccluders(ceilingGroups.ToArray());
            // Shops and the opening lane-signal gantry (whose sign board sits at camera height) fade when in the way.
            var shops=gimmickRoot!=null?gimmickRoot.transform.Cast<Transform>().Where(t=>t.name.StartsWith("SR18_Route_Shop_")||t.name.Contains("signal_gantry")).ToArray():Array.Empty<Transform>();
            var coldScreens=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ColdCurtain"||t.name=="ColdStoreSign");
            // With the normal (high) S3 camera, the raised pier bridge and the raised market floor over S3
            // sit between camera and shark; let them fade like other occluders (user 2026-09-29: no camera drop).
            var branchAnchor = root.GetComponentInChildren<NoryangjinMarketBranch>().transform;
            var hallRoot = root.Find("MarketHall");
            var raised = branchAnchor.Cast<Transform>().Concat(newRoads.Cast<Transform>().Where(t => t.name.StartsWith("OutsidePierDeck")))
                .Concat(hallRoot != null ? hallRoot.GetComponentsInChildren<Transform>(true).Where(t => t.name == "WetFloor") : Enumerable.Empty<Transform>())
                .Where(t => { var r = t.GetComponent<Renderer>(); return r == null || r.bounds.min.y > 2.5f; })
                .Concat(hallRoot != null ? hallRoot.GetComponentsInChildren<Transform>(true).Where(t => t.name == "CrossingUndersideSlab") : Enumerable.Empty<Transform>()).ToArray();
            occlusion.ConfigureFeedbackTransparency(shops.Concat(coldScreens).Concat(feedbackOccluders).Concat(raised).ToArray());
            report.Add("fading raised bridge/market parts: " + raised.Length);
        }
        report.Add("suppressed: " + string.Join(",", suppress.Select(s => s.name)));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return report;
    }

    // ---------------------------------------------------------------- zones and anchors
    static readonly Vector3 HallOrigin = new(124.3f, 0, 45f);
    static readonly Vector3 HallForward = Vector3.back;
    // Market and outside pier climb this high over the S3 east pier (branch distance 40..64 is level).
    const float LiftHeight = 9f;
    static bool OverS3(Vector3 p, float margin)
        => Mathf.Abs(p.z - NoryangjinCrossingLiftBuilder.S3Z) < NoryangjinCrossingLiftBuilder.S3HalfWidth + margin && p.x > 40 && p.x < 180;
    static bool InZone(Vector3 p)
    {
        bool hall = Mathf.Abs(p.x - 124.3f) < 8 && p.y < 3 && p.z < -95 && p.z > -348;   // hose .. shutter
        // The outdoor dawn auction (x 355..432) was removed by the V3 builder; its SR18 encounters stay live
        // so the late run is not empty (user 2026-09-29).
        bool barricade = Mathf.Abs(p.z + 7.25f) < 6 && p.x > 146 && p.x < 172;
        return hall || barricade;
    }

    static bool InHallEvents(Vector3 p) => Mathf.Abs(p.x - 124.3f) < 10 && p.y < 3 && p.z < 58 && p.z > -342;
    static bool InHallScenery(Vector3 p) => p.y < 11 &&
        (Mathf.Abs(p.x - 124.3f) < 22 && p.z < 48 && p.z > -344
        || Mathf.Abs(p.x + 67) < 22 && p.z > -357 && p.z < -198);
    static bool InIncidentZone(Vector3 p)
    {
        if (p.y > 3) return false;
        bool cold = Mathf.Abs(p.x + 67) < 10 && p.z > -340 && p.z < -182;
        bool port = Mathf.Abs(p.x - 225.5f) < 10 && p.z > -65 && p.z < 5;
        bool gull = Mathf.Abs(p.x - 326.7f) < 10 && p.z > 80 && p.z < 115;
        bool gates = Mathf.Abs(p.x - 225.5f) < 10 && p.z > 8 && p.z < 48
            || Mathf.Abs(p.x - 326.7f) < 10 && p.z > 130 && p.z < 172;
        return cold || port || gull || gates;
    }
    static bool InPier(Vector3 p) => Mathf.Abs(p.z + 348.1f) < 8 && p.x > 20 && p.x < 124;
    static bool OverlapsBypass(Transform oldProp)
    {
        if (oldProp.position.y > 10) return false;
        var branch = root.GetComponentInChildren<NoryangjinMarketBranch>();
        var renderers = oldProp.GetComponentsInChildren<Renderer>(true);
        for (float d = 0; d <= branch.length; d += 6)
        {
            // Reach from the sea up to above the (possibly raised) deck so nothing pokes through the bridge.
            var point = branch.Point(d, true); float deck = point.y - branch.transform.position.y;
            var corridor = new Bounds(new Vector3(point.x, branch.transform.position.y + (deck + 6) * .5f, point.z), new Vector3(14, deck + 8, 10));
            foreach (var renderer in renderers)
                if (corridor.Intersects(renderer.bounds)) return true;
        }
        return false;
    }

    static Transform Anchor(string name, Vector3 position, Vector3 forward, Transform parent = null)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent != null ? parent : root, false);
        t.position = position; t.rotation = Quaternion.LookRotation(forward);
        return t;
    }

    // ---------------------------------------------------------------- A. barricade (S3, heading east)
    static void BuildBarricade()
    {
        var a = Anchor("A_MarketBarricade", new Vector3(160f, 0, -7.25f), Vector3.right);
        var ev = a.gameObject.AddComponent<NoryangjinWallEvent>();
        ev.triggerAhead = 42; ev.banner = "길이 막혔다! 부숴서 뚫어라"; ev.bannerSeconds = 2.6f;

        var truck = Breakable(a, "OverturnedSeafoodTruck", new Vector3(-pathHalf * .42f, 0, 0), pathHalf * .6f, 1.3f, 9);
        var truckModel = Place(Meshy + "V04_1ton_truck.prefab", truck.transform, new Vector3(0, 1.05f, 0), Quaternion.Euler(0, 90, 90), 1f);
        if (truckModel != null) FitLength(truckModel, 3.8f);
        var boxes = Breakable(a, "StyrofoamBoxWall", new Vector3(pathHalf * .62f, 0, -.2f), pathHalf * .4f, .7f, 5);
        boxes.fishDropPrefab = fishTemplate;
        BoxStack(boxes, 2, 3, pathHalf * .8f);
        var lpg = Breakable(a, "LpgCylinders", new Vector3(pathHalf * .18f, 0, -1.3f), .7f, .4f, 2);
        lpg.blocksPath = false; lpg.explodeRadius = pathHalf * 2.2f; lpg.coins = 6; lpg.healthLabelHeight = 1.9f;
        for (int i = 0; i < 3; i++) Place(Meshy + "N02_lpg_cylinder.prefab", lpg.transform, new Vector3((i - 1) * .55f, 0, i == 1 ? -.25f : 0), Quaternion.identity, 1f);
        Place(Stage01 + "004_STAGE01_NRY_OBSTACLE_004_Seafood_push_cart/004_STAGE01_NRY_OBSTACLE_004_Seafood_push_cart.prefab", a, new Vector3(-pathHalf - .8f, 0, 1.5f), Quaternion.Euler(0, 30, 0), 1f);
        ev.pieces = new[] { truck, boxes, lpg };
        foreach (var p in ev.pieces) p.armOnRunStart = false;
    }

    // ---------------------------------------------------------------- E/F/G/H. market hall (S6, heading south)
    static void BuildMarketHall(GameObject[] templates)
    {
        var hall = Anchor("MarketHall", HallOrigin, HallForward);
        Vector3 At(float along, float lateral = 0, float up = 0) => HallOrigin + HallForward * along + Vector3.Cross(Vector3.up, HallForward) * lateral + Vector3.up * up;
        var right = Vector3.Cross(Vector3.up, HallForward);
        var rot = Quaternion.LookRotation(HallForward);
        var beam = Mat("HallBeam", new Color(.62f, .66f, .72f));
        var signBlue = Mat("SignBlue", new Color(.06f, .22f, .78f));
        var signFrame = Mat("SignFrame", new Color(.12f, .12f, .16f));
        var tv = Mat("TvBlack", new Color(.05f, .05f, .07f));
        var tvScreen = Mat("TvScreen", new Color(.2f, .34f, .62f), emission: new Color(.2f, .34f, .62f));
        var facade = Mat("HallFacade", new Color(.86f, .88f, .9f));
        const float length = 378;

        // Entrance and exit facades.
        Facade(hall, At(-2), rot, "노량진수산시장  시장동", facade, signBlue, false);
        var exit = Facade(hall, At(length), rot, "출구", facade, signBlue, true);

        // The roof stays below the SR18 elevated crossing (y=12); the indoor camera sits below it.
        BuildHallShell(hall, length);
        string[] shops = { "오성수산", "대흥수산", "신광수산", "동해수산", "광주수산", "한강수산", "남도수산", "바다상회", "대원수산", "서해수산", "제주수산", "부산상회", "목포수산", "여수상회", "통영수산", "완도수산" };
        int shop = 0;
        for (float along = 8; along < length - 6; along += 12, shop++)
        {
            float side = shop % 2 == 0 ? -1 : 1;
            var sign = Box(hall, "StallSign", At(along, side * (pathHalf + 1.4f), 5.8f), rot, new Vector3(3.6f, 1.2f, .14f), signFrame);
            var plate = Box(sign, "Plate", sign.position - HallForward * .02f, rot, new Vector3(3.4f, .9f, .16f), signBlue, world: true);
            Label(plate, $"{(shop % 3 == 0 ? "활어" : shop % 3 == 1 ? "선어" : "패류")}-{101 + shop}\n{shops[shop % shops.Length]}", 2.6f, Color.white, new Vector2(3.3f, .9f), -HallForward * .1f);
            Box(hall, "Wire", At(along, side * (pathHalf + 1.4f), 7f), rot, new Vector3(.04f, 1.8f, .04f), signFrame);
            Place(Stage01 + "027_STAGE01_NRY_DCR_016_Red_market_hanging_lamp/027_STAGE01_NRY_DCR_016_Red_market_hanging_lamp.prefab", hall, At(along + 6, -side * (pathHalf + 1.2f), 6.3f), rot, .8f);
        }
        foreach (float along in new[] { 24f, 96f, 200f, 272f })
        {
            float side = along < 150 ? 1 : -1;
            var set = Box(hall, "MarketTV", At(along, side * (pathHalf + 1.2f), 5.5f), rot, new Vector3(3.2f, 1.9f, .25f), tv);
            var screen = Box(set, "Screen", set.position - HallForward * .02f, rot, new Vector3(2.9f, 1.6f, .27f), tvScreen, world: true);
            Label(screen, "속보: 상어, 노량진\n시장동 진입", 2.1f, Color.white, new Vector2(2.8f, 1.5f), -HallForward * .15f);
        }
        var clock = Box(hall, "DigitalClock", At(20, 0, 6.7f), rot, new Vector3(2.4f, .9f, .2f), tv);
        Label(clock, "04:38", 5f, new Color(1f, .25f, .2f), new Vector2(2.3f, .9f), -HallForward * .12f);

        // Entrance banner.
        var enter = Anchor("E0_EnterHall", At(0), HallForward, hall);
        var enterEv = enter.gameObject.AddComponent<NoryangjinBannerEvent>();
        enterEv.triggerAhead = 22; enterEv.banner = "노량진 시장동 입장! 호객 주의";

        // E. merchant rush from the far end of the aisle.
        var rush = Anchor("E1_MerchantRush", At(58), HallForward, hall);
        var rushEv = rush.gameObject.AddComponent<NoryangjinRushEvent>();
        rushEv.templates = templates.Take(3).ToArray();
        rushEv.triggerAhead = 36; rushEv.banner = "호객 러시! 앞줄부터 뚫어라";
        rushEv.rows = 5; rushEv.perRow = 3; rushEv.rowGap = 4.5f; rushEv.spread = pathHalf * 1.6f; rushEv.runSpeed = 3.2f; rushEv.homing = .8f;
        rushEv.healthHits = .95f; rushEv.damageShare = .05f; rushEv.coinsEach = 3;
        rushEv.routeScope = NoryangjinRevampEvent.RouteScope.Indoor; rushEv.hazardSeconds = 12;
        rushEv.shouts = new[] {Voice("merchant-catch"), Voice("merchant-look")};

        // H. floor-cleaning hoses.
        var hose = Anchor("H_HoseCleaning", At(150), HallForward, hall);
        var hoseEv = hose.gameObject.AddComponent<NoryangjinHoseEvent>();
        hoseEv.triggerAhead = 30; hoseEv.banner = "물청소 시간! 물줄을 피하세요"; hoseEv.halfWidth = pathHalf; hoseEv.bandHalfDepth = .9f;
        var jets = new List<NoryangjinHoseEvent.Jet>();
        var water = Mat("HoseWater", new Color(.72f, .88f, 1f, .72f), transparent: true, emission: new Color(.25f, .4f, .55f));
        var wet = Mat("WetFloor", new Color(.25f, .55f, 1f, .38f), transparent: true);
        for (int i = 0; i < 4; i++)
        {
            float along = i * 9f; int side = i % 2 == 0 ? -1 : 1;
            float reach = pathHalf * 1.35f;
            var worker = Visual(templates[i % 3], hose, hose.position + hose.forward * along + hose.right * side * (pathHalf + 1.1f), Quaternion.LookRotation(-side * hose.right), "HoseWorker");
            worker.gameObject.AddComponent<NoryangjinCrowdPose>().state = ForwardEnemyAnimationContract.AttackLoop;
            var jetRoot = new GameObject("Jet" + i).transform; jetRoot.SetParent(hose, false);
            jetRoot.localPosition = new Vector3(side * pathHalf, 0, along); jetRoot.localRotation = Quaternion.identity;
            var stream = Cylinder(jetRoot, "Stream", new Vector3(-side * reach * .5f, 1.0f, 0), Quaternion.Euler(0, 0, 90), new Vector3(.42f, reach * .5f, .42f), water);
            Box(jetRoot, "WetBand", jetRoot.position + hose.right * (-side * reach * .5f) + Vector3.up * .06f, hose.rotation, new Vector3(reach, .02f, hoseEv.bandHalfDepth * 2), wet, world: true);
            var hoseLine = Cylinder(jetRoot, "Hose", new Vector3(side * .7f, .55f, 0), Quaternion.Euler(0, 0, 90), new Vector3(.12f, .7f, .12f), Mat("HoseRubber", new Color(.1f, .35f, .85f)));
            jets.Add(new NoryangjinHoseEvent.Jet { along = along, side = side, reach = reach, period = 2.8f, onSeconds = 1.5f, phase = i * .7f, visual = jetRoot });
        }
        hoseEv.jets = jets.ToArray();
        hoseEv.routeScope = NoryangjinRevampEvent.RouteScope.Indoor; hoseEv.hazardSeconds = 10;

        // F. turret truck boss.
        var turret = Anchor("F_TurretCharge", At(236), HallForward, hall);
        var turretEv = turret.gameObject.AddComponent<NoryangjinTurretEvent>();
        turretEv.triggerAhead = 45; turretEv.banner = "터렛트 돌진! 보스를 부숴라"; turretEv.chargeSpeed = 5; turretEv.stopDistance = 9; turretEv.bossHits = 16;
        turretEv.routeScope = NoryangjinRevampEvent.RouteScope.Indoor; turretEv.hazardSeconds = 10;
        var truck = Breakable(turret, "TurretTruck", Vector3.zero, pathHalf * .8f, 2.2f, 16);
        truck.armOnRunStart = false; truck.healthLabelHeight = 5.6f; truck.coins = 25;
        var body = Place(Meshy + "N01_turret_truck.prefab", truck.transform, new Vector3(0, 0, 0), Quaternion.Euler(0, 90, 0), 1f);
        var cargo = new List<Transform>();
        for (int y = 0; y < 3; y++) for (int x = 0; x < 2; x++) for (int z = 0; z < 2; z++)
        {
            var box = Place(Stage01 + "002_STAGE01_NRY_PROPS_002_Styrofoam_fish_box/002_STAGE01_NRY_PROPS_002_Styrofoam_fish_box.prefab", truck.transform, new Vector3((x - .5f) * .85f, 1.2f + y * .58f, .25f + z * .8f), Quaternion.Euler(0, (x + y + z) % 2 * 8, 0), 1f);
            if (box != null) { FitWidth(box, .9f); cargo.Add(box); }
        }
        truck.shedPieces = cargo.ToArray();
        var driver = Visual(templates[0], truck.transform, truck.transform.position - turret.forward * 1.0f + Vector3.up * .48f, Quaternion.LookRotation(-turret.forward), "TurretDriver");
        driver.localScale = Vector3.one * .9f;
        driver.gameObject.AddComponent<NoryangjinCrowdPose>().state = ForwardEnemyAnimationContract.AttackLoop;
        turretEv.turret = truck;

        // G. box walls + shutter countdown.
        var shutterAnchor = Anchor("G_ShutterEscape", At(length - 1.5f), HallForward, hall);
        var shutterEv = shutterAnchor.gameObject.AddComponent<NoryangjinShutterEvent>();
        shutterEv.triggerAhead = 98; shutterEv.banner = "문이 곧 닫힙니다. 주의해주세요!"; shutterEv.announceVoice = ClaudeVoice("door-closing"); shutterEv.bannerSeconds = 3f; shutterEv.slackSeconds = 7;
        shutterEv.routeScope = NoryangjinRevampEvent.RouteScope.Indoor; shutterEv.hazardSeconds = 28;
        var walls = new List<NoryangjinBreakable>();
        for (int i = 0; i < 5; i++)
        {
            float along = 300 + i * 11 - (length - 1.5f);
            var wall = Breakable(shutterAnchor, "BoxWall" + (i + 1), new Vector3(0, 0, along), pathHalf + .3f, .75f, 5);
            wall.armOnRunStart = false; wall.coins = 2; wall.healthLabelHeight = 2.9f; wall.fishDropPrefab = fishTemplate;
            BoxStack(wall, 3, Mathf.Max(4, Mathf.RoundToInt(pathHalf * 2 / 1.05f)), pathHalf * 2);
            walls.Add(wall);
        }
        shutterEv.walls = walls.ToArray();
        var shutter = Breakable(shutterAnchor, "Shutter", new Vector3(0, 0, .2f), pathHalf + 1, .4f, 6);
        shutter.armOnRunStart = false; shutter.drainWhileBlocked = .05f; shutter.healthLabelHeight = 5f; shutter.coins = 0;
        var panel = Box(shutter.transform, "ShutterPanel", shutter.transform.position + Vector3.up * 2.4f, shutter.transform.rotation, new Vector3(pathHalf * 2 + 2, 4.6f, .22f), Mat("ShutterSteel", new Color(.58f, .6f, .63f)), world: true);
        for (int s = 0; s < 10; s++)
            Box(panel, "Slat", panel.position + Vector3.up * (-2.1f + s * .46f) - shutter.transform.forward * .12f, shutter.transform.rotation, new Vector3(pathHalf * 2 + 2, .05f, .05f), Mat("ShutterGroove", new Color(.35f, .36f, .38f)), world: true);
        for (int s = 0; s < 8; s++)
            Box(panel, "Stripe", panel.position + Vector3.up * -2.15f + shutter.transform.right * ((s - 3.5f) * (pathHalf * 2 + 2) / 8f) - shutter.transform.forward * .13f, shutter.transform.rotation * Quaternion.Euler(0, 0, 35), new Vector3(.5f, .5f, .04f), Mat(s % 2 == 0 ? "StripeYellow" : "StripeBlack", s % 2 == 0 ? new Color(1f, .8f, .05f) : new Color(.08f, .08f, .08f)), world: true);
        shutterEv.shutter = shutter; shutterEv.shutterPanel = panel; shutterEv.openHeight = 4.4f;
        var lamp = Mat("WarnLamp", new Color(1f, .45f, .1f), emission: new Color(1f, .4f, .05f));
        foreach (float side in new[] { -1f, 1f }) Box(exit, "WarnLamp", exit.position + right * side * (pathHalf + 1.6f) + Vector3.up * 5.4f, rot, Vector3.one * .45f, lamp, world: true);
        report.Add($"hall built: walls {walls.Count}, jets {jets.Count}");
    }

    static Transform Facade(Transform parent, Vector3 at, Quaternion rot, string text, Material wall, Material sign, bool exit)
    {
        var f = new GameObject(exit ? "ExitFacade" : "EntranceFacade").transform; f.SetParent(parent, false);
        f.position = at; f.rotation = rot;
        foreach (float side in new[] { -1f, 1f })
            Box(f, "Pillar", at + f.right * side * (pathHalf + 2.6f) + Vector3.up * 4f, rot, new Vector3(2.2f, 8, 1.4f), wall, world: true);
        var lintel = Box(f, "Lintel", at + Vector3.up * 8f, rot, new Vector3(pathHalf * 2 + 7.4f, 1.5f, 1.4f), wall, world: true);
        var plate = Box(lintel, "SignPlate", lintel.position - f.forward * .75f, rot, new Vector3(pathHalf * 2 + 5, 1.7f, .1f), sign, world: true);
        Label(plate, text, 7f, Color.white, new Vector2(pathHalf * 2 + 5, 1.7f), -f.forward * .08f);
        ceilingGroups.Add(f);
        return f;
    }

    // ---------------------------------------------------------------- C. waves (S7, heading west)
    static void BuildWaves()
    {
        // Starts after the pier bridge has come back down over S3 (branch distance 100).
        var a = Anchor("C_PierWaves", HallOrigin + HallForward * 100 + Vector3.left * 25, HallForward);
        var ev = a.gameObject.AddComponent<NoryangjinWaveEvent>();
        ev.triggerAhead = 22; ev.banner = "바깥 부두: 큰 파도 구간! 한 번 닿으면 끝"; ev.bannerSeconds = 2.2f; ev.halfWidth = pathHalf;
        ev.warnSeconds = 1.2f; ev.crashSeconds = .9f; ev.leadMetres = 0;
        ev.routeScope = NoryangjinRevampEvent.RouteScope.Outdoor;
        const string Water = "Assets/SrRubfish_VFX_02/Prefabs/";
        ev.crestPrefab = Load<GameObject>(Water + "FX_PF_WaveAttack_Projectile.prefab");
        ev.foamPrefab = Load<GameObject>(Water + "FX_PF_Waterfall_MainFoam.prefab");
        ev.splashPrefab = Load<GameObject>(Water + "FX_PF_Water_SimpleSplash_01.prefab");
        ev.puddlePrefab = Load<GameObject>(Water + "FX_PF_Waterfall_UnderwaterFoam.prefab");
        // Big enough to read as a wave, small enough that the shark and the red zone stay visible.
        ev.crestScale = 1.75f; ev.foamScale = 1.15f;
        // The pier lane is only +-pathHalf (2 m) wide: keep a dodge possible but tight.
        ev.gapHalfWidth = .75f; ev.hunterHalfWidth = 1.1f; ev.hunterLockSeconds = .45f;
        var danger = Mat("DangerZone", new Color(1f, .12f, .1f, .42f), transparent: true, emission: new Color(.6f, .05f, .05f));
        var border = Mat("DangerBorder", new Color(1f, .15f, .1f), emission: new Color(1f, .1f, .05f));
        // Chains get shorter warnings as the pier goes on; only a precise dodge survives.
        var plan = new (NoryangjinWaveEvent.Kind kind, float along, int side, float gap, float warn)[] {
            (NoryangjinWaveEvent.Kind.Side, 0, -1, 0, 1.4f), (NoryangjinWaveEvent.Kind.Side, 16, 1, 0, 1.25f),
            (NoryangjinWaveEvent.Kind.Gap, 32, 0, .55f, 1.35f), (NoryangjinWaveEvent.Kind.Hunter, 48, -1, 0, 1.3f),
            (NoryangjinWaveEvent.Kind.Side, 64, -1, 0, 1.1f), (NoryangjinWaveEvent.Kind.Side, 72, 1, 0, 1f),
            (NoryangjinWaveEvent.Kind.Gap, 90, 0, -.6f, 1.2f), (NoryangjinWaveEvent.Kind.Hunter, 106, 1, 0, 1.2f),
            (NoryangjinWaveEvent.Kind.Hunter, 120, -1, 0, 1.15f), (NoryangjinWaveEvent.Kind.Gap, 136, 0, 0, 1.15f),
            (NoryangjinWaveEvent.Kind.Side, 150, 1, 0, 1f), (NoryangjinWaveEvent.Kind.Side, 158, -1, 0, .95f),
            (NoryangjinWaveEvent.Kind.Side, 166, 1, 0, .95f), (NoryangjinWaveEvent.Kind.Gap, 184, 0, .75f, 1.1f),
            (NoryangjinWaveEvent.Kind.Hunter, 200, 1, 0, 1.1f), (NoryangjinWaveEvent.Kind.Side, 214, -1, 0, 1f) };
        var waves = new List<NoryangjinWaveEvent.Wave>();
        for (int i = 0; i < plan.Length; i++)
        {
            var warning = new GameObject("Warning" + i).transform; warning.SetParent(a, false);
            warning.localPosition = new Vector3(0, .05f, plan[i].along); warning.localRotation = Quaternion.identity;
            foreach (string zoneName in new[] { "ZoneA", "ZoneB" })
            {
                // Unit footprint; the event scales it to each pattern's flooded interval at run time.
                var zone = new GameObject(zoneName).transform; zone.SetParent(warning, false);
                Box(zone, "Fill", Vector3.zero, Quaternion.identity, new Vector3(1, .03f, 1), danger, local: true);
                foreach (float edge in new[] { -.5f, .5f }) Box(zone, "Rim", new Vector3(edge, .03f, 0), Quaternion.identity, new Vector3(.05f, .05f, 1), border, local: true);
            }
            var skull = new GameObject("Skull").transform; skull.SetParent(warning, false); skull.localPosition = new Vector3(0, 2.8f, 0);
            skull.gameObject.AddComponent<NoryangjinFaceCamera>();
            var t = skull.gameObject.AddComponent<TextMeshPro>();
            t.font = font; t.text = "<size=150%>위험!</size>\n휩쓸리면 즉사"; t.fontSize = 5; t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(1f, .9f, .85f); LabelOutline(t, "WaveWarningText", new Color32(170, 0, 0, 255)); t.rectTransform.sizeDelta = new Vector2(6, 3);
            var safe = new GameObject("SafeTag").transform; safe.SetParent(warning, false); safe.localPosition = new Vector3(0, 2.2f, 0);
            safe.gameObject.AddComponent<NoryangjinFaceCamera>();
            var st = safe.gameObject.AddComponent<TextMeshPro>();
            st.font = font; st.text = "안전"; st.fontSize = 5; st.fontStyle = FontStyles.Bold; st.alignment = TextAlignmentOptions.Center;
            st.color = new Color(.55f, 1f, .55f); LabelOutline(st, "WaveSafeText", new Color32(0, 70, 20, 255)); st.rectTransform.sizeDelta = new Vector2(4, 2);
            warning.gameObject.SetActive(false);
            waves.Add(new NoryangjinWaveEvent.Wave { kind = plan[i].kind, along = plan[i].along, side = plan[i].side, gapCenter = plan[i].gap,
                length = 11, warnSeconds = plan[i].warn, warning = warning });
        }
        ev.waves = waves.ToArray();
        report.Add($"waves: {plan.Length} crests (side/gap/hunter) over {plan[plan.Length - 1].along:0} m, SrRubfish crest/foam FX");
    }

    // Curling wave: a thick water face whose lip curls toward the road centre (+X), extruded along Z
    // (unit length). Submesh 1 is the white foam band along the crest and lip.
    static Mesh WaveMesh()
    {
        string path = MeshDir + "/WaveCurl2.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        var profile = new[] {
            new Vector2(-5.5f, 0), new Vector2(-4.4f, 1.8f), new Vector2(-3.2f, 3.8f), new Vector2(-1.8f, 5.4f), new Vector2(-.4f, 6.1f),
            new Vector2(.9f, 5.9f), new Vector2(1.9f, 5.0f), new Vector2(2.3f, 3.9f), new Vector2(1.8f, 3.2f), new Vector2(.9f, 3.3f),
            new Vector2(.2f, 3.9f), new Vector2(-.3f, 3.0f), new Vector2(-.1f, 1.7f), new Vector2(.8f, .7f), new Vector2(2.4f, 0) };
        int foamFrom = 3, foamTo = 9;
        int segs = 12, n = profile.Length;
        var verts = new List<Vector3>(); var body = new List<int>(); var foam = new List<int>();
        for (int s = 0; s <= segs; s++)
        {
            float z = s / (float)segs - .5f;
            float wob = Mathf.Sin(s * 1.7f) * .35f, lift = Mathf.Cos(s * 1.1f) * .25f;
            foreach (var p in profile) verts.Add(new Vector3(p.x + wob * Mathf.Clamp01(p.y / 3f), p.y + lift * Mathf.Clamp01(p.y / 3f), z));
        }
        for (int s = 0; s < segs; s++)
            for (int i = 0; i < n - 1; i++)
            {
                int a = s * n + i, b = a + 1, c = a + n, d = c + 1;
                var list = i >= foamFrom && i < foamTo ? foam : body;
                list.AddRange(new[] { a, c, b, b, c, d, a, b, c, b, d, c });
            }
        var mesh = new Mesh { name = "WaveCurl2", subMeshCount = 2 };
        mesh.SetVertices(verts); mesh.SetTriangles(body, 0); mesh.SetTriangles(foam, 1);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // ---------------------------------------------------------------- random gates
    static void BuildMysteryGate(string name, Vector3 at, float yaw)
    {
        var a = Anchor(name, at, Quaternion.Euler(0, yaw, 0) * Vector3.forward);
        var ev = a.gameObject.AddComponent<NoryangjinMysteryGateEvent>();
        ev.triggerAhead = 30; ev.banner = "";
        var display = new GameObject("RandomPads").transform; display.SetParent(a, false);
        var random = display.gameObject.AddComponent<BonusPadRandomDisplay>();
        random.padOffsets = new[] { -pathHalf * .55f, pathHalf * .55f }; random.padWidth = Mathf.Min(2.4f, pathHalf * .9f);
        ev.display = display.gameObject;
    }

    // ---------------------------------------------------------------- D. auction (S13, heading east)
    static void BuildAuction(GameObject[] templates)
    {
        var a = Anchor("D_DawnAuction", new Vector3(392f, 0, 203.5f), Vector3.right);
        var right = a.right;
        // The stalls here used to be hidden for this outdoor auction. The V3 builder removes the whole
        // auction, so they are restored (reinstalls reactivate any copy an older install hid).
        var props = GameObject.Find("Noryangjin_MapTool/Props");
        int restored = 0;
        if (props != null)
            foreach (Transform t in props.transform)
                if (t.name.StartsWith("SR18_Route_Shop_") && t.position.x > 350 && t.position.x < 432 && Mathf.Abs(t.position.z - 203.5f) < 16 && !t.gameObject.activeSelf) { t.gameObject.SetActive(true); restored++; }
        report.Add("auction stalls restored " + restored);
        var floor = Mat("AuctionFloor", new Color(.52f, .55f, .58f));
        foreach (float side in new[] { -1f, 1f })
        {
            Box(a, "AuctionFloor", a.position + right * side * (pathHalf + 7.5f) + Vector3.up * -.05f + a.forward * -4, a.rotation, new Vector3(14, .1f, 64), floor, world: true);
            for (int row = 0; row < 3; row++)
                for (int k = 0; k < 12; k++)
                {
                    var box = Place(Stage01 + "002_STAGE01_NRY_PROPS_002_Styrofoam_fish_box/002_STAGE01_NRY_PROPS_002_Styrofoam_fish_box.prefab", a,
                        a.position + right * side * (pathHalf + 2.4f + row * 3.4f) + a.forward * (-30 + k * 1.9f), a.rotation, 1f);
                    if (box != null) FitWidth(box, 1.1f);
                }
        }
        // Crowd of bidders around the box rows (decorative).
        int n = 0;
        foreach (float side in new[] { -1f, 1f })
            for (int row = 0; row < 3; row++)
                for (int k = 0; k < 7; k++, n++)
                {
                    var pos = a.position + right * side * (pathHalf + 4f + row * 3.4f) + a.forward * (-26 + k * 4.2f + row * 1.3f);
                    var who = Visual(templates[n % 3], a, pos, Quaternion.LookRotation(-side * right + a.forward * .3f), "Bidder");
                    var pose = who.gameObject.AddComponent<NoryangjinCrowdPose>();
                    pose.state = (n % 3 == 0) ? ForwardEnemyAnimationContract.AttackLoop : ForwardEnemyAnimationContract.Idle; pose.speed = .9f + (n % 5) * .06f;
                }
        // Auctioneer on a box + banner + clock.
        var stand = Box(a, "AuctioneerBox", a.position + right * (pathHalf + 3f) + a.forward * 6, a.rotation, new Vector3(1.6f, .8f, 1.6f), Mat("Crate", new Color(.55f, .4f, .25f)), world: true);
        var auctioneer = Visual(templates[0], a, stand.position + Vector3.up * .4f, Quaternion.LookRotation(-right - a.forward), "Auctioneer");
        auctioneer.gameObject.AddComponent<NoryangjinCrowdPose>().state = ForwardEnemyAnimationContract.AttackLoop;
        var bannerFrame = Box(a, "AuctionBanner", a.position + a.forward * 22 + Vector3.up * 16f, Quaternion.LookRotation(a.forward), new Vector3(pathHalf * 2 + 12, 2.6f, .12f), Mat("BannerWhite", new Color(.95f, .95f, .92f)), world: true);
        Label(bannerFrame, "새벽 경매", 12f, new Color(.08f, .2f, .6f), new Vector2(pathHalf * 2 + 12, 2.6f), -a.forward * .1f);
        var clock = Box(a, "AuctionClock", a.position + a.forward * 22 + Vector3.up * 13.8f, Quaternion.LookRotation(a.forward), new Vector3(3, 1.1f, .2f), Mat("TvBlack", new Color(.05f, .05f, .07f)), world: true);
        Label(clock, "04:30", 6f, new Color(1f, .25f, .2f), new Vector2(3, 1.1f), -a.forward * .12f);
        foreach (float side in new[] { -1f, 1f })
            Box(a, "BannerPost", a.position + a.forward * 22 + right * side * (pathHalf + 6) + Vector3.up * 8.5f, a.rotation, new Vector3(.3f, 17, .3f), Mat("HallBeam", new Color(.62f, .66f, .72f)), world: true);

        // Human wall of startled bidders (real enemies that stand and block).
        var wall = Anchor("D1_BidderWall", a.position, a.forward, a);
        var wallEv = wall.gameObject.AddComponent<NoryangjinRushEvent>();
        wallEv.templates = templates.Take(3).ToArray(); wallEv.triggerAhead = 46; wallEv.banner = "\"상어다! 막아!\" 경매 도중 난입!";
        wallEv.rows = 1; wallEv.perRow = 3; wallEv.rowGap = 1.6f; wallEv.spread = pathHalf * 1.8f; wallEv.runSpeed = 0; wallEv.startAlong = 0;
        wallEv.homing = 0; wallEv.healthHits = 1.6f; wallEv.damageShare = .05f; wallEv.coinsEach = 3; wallEv.animation = ForwardEnemyAnimationContract.AttackLoop;


        // Reward choice pads after the wall.
        var reward = Anchor("D2_AuctionReward", a.position + a.forward * 14, a.forward, a);
        var rewardEv = reward.gameObject.AddComponent<NoryangjinRewardChoiceEvent>();
        rewardEv.triggerAhead = 30; rewardEv.banner = "낙찰 보상! 한쪽을 밟아 고르세요"; rewardEv.padOffset = pathHalf * .55f; rewardEv.padWidth = Mathf.Min(2.4f, pathHalf * .9f);
        rewardEv.shieldIcon = Load<Sprite>("Assets/ShooterSurvival/UI/CoastalEnamel/SahurShield.png");
        rewardEv.healIcon = Load<Sprite>("Assets/ShooterSurvival/Resources/WallBonusIcons/WallBonus_HealthPercent.png");
    }

    // ---------------------------------------------------------------- I. final boss
    static void BuildFinalBoss()
    {
        var woman = Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e.name == "SR18_L_E25_T293_Enemy_Woman");
        if (woman == null) { report.Add("final boss not found"); return; }
        // Human height, very wide: blocks the whole deck so she cannot be dodged.
        woman.transform.localScale = new Vector3(2.75f, 1.72f, 2.3f);
        var col = woman.GetComponent<Collider>();
        if (col is CapsuleCollider cap)
        {
            float needed = (pathHalf + .6f) / woman.transform.lossyScale.x;
            if (cap.radius < needed) cap.radius = needed;
            report.Add($"boss capsule radius {cap.radius:F2} height {cap.height:F2}");
        }
        else if (col is BoxCollider box)
        {
            var size = box.size; size.x = Mathf.Max(size.x, (pathHalf * 2 + 1.2f) / woman.transform.lossyScale.x); box.size = size;
            report.Add($"boss box size {box.size}");
        }
        var b = Anchor("I_FinalBoss", new Vector3(248f, 0, 246f), Vector3.forward);
        var ev = b.gameObject.AddComponent<NoryangjinBannerEvent>();
        ev.triggerAhead = 8; ev.banner = "노량진 아줌마 등장! 피할 곳이 없다"; ev.bannerSeconds = 3f;
    }

    // ---------------------------------------------------------------- completed market shell / branch / incidents
    static void BuildHallShell(Transform hall, float length)
    {
        var white = Mat("LowWhiteCeiling", new Color(.91f, .92f, .9f));
        var wall = Mat("IndoorWall", new Color(.75f, .81f, .82f));
        var trim = Mat("IndoorBlueTrim", new Color(.08f, .22f, .48f));
        var tile = Mat("WetGrayTile", Color.white); tile.SetFloat("_Smoothness", .72f);
        string texturePath = MeshDir + "/WetTileAlbedo.png";
        if (!File.Exists(texturePath)) File.Copy("outputs/noryangjin-revamp-fix-2026-09-28/wet-tile-albedo.png", texturePath);
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var tileImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        tileImporter.wrapMode = TextureWrapMode.Repeat; tileImporter.maxTextureSize = 1024; tileImporter.SaveAndReimport();
        tile.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        tile.SetTextureScale("_BaseMap", new Vector2(1, length / 14f)); EditorUtility.SetDirty(tile);
        var grout = Mat("TileGrout", new Color(.18f, .25f, .28f));
        var light = Mat("WhiteFluorescent", new Color(.88f, .99f, 1), emission: new Color(1.1f, 1.25f, 1.3f));
        Vector3 At(float d, float x = 0, float y = 0) => hall.position + hall.forward * d + hall.right * x + Vector3.up * y;
        float width = pathHalf * 2 + 11;
        // All new walkable surfaces are below the existing road-root so its support cache sees them.
        var floor = Box(newRoads, "IndoorTileSurface", At(length * .5f, 0, .04f), hall.rotation,
            new Vector3(width, .12f, length), tile);
        floor.gameObject.AddComponent<MeshCollider>().sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh;
        foreach (float side in new[] {-1f, 1f})
        {
            Box(hall, "EnclosedWall", At(length * .5f, side * width * .5f, 4.25f), hall.rotation, new Vector3(.3f, 8.5f, length), wall);
            Box(hall, "WallBase", At(length * .5f, side * (width * .5f - .2f), .6f), hall.rotation, new Vector3(.1f, 1.2f, length), trim);
            Box(hall, "Drain", At(length * .5f, side * (pathHalf + .3f), .12f), hall.rotation, new Vector3(.28f, .02f, length), grout);
        }
        for (float d = 5; d < length; d += 10)
        {
            var roof = Box(hall, "LowCeilingPanel", At(d, 0, 8.5f), hall.rotation, new Vector3(width, .18f, 9.9f), white);
            ceilingGroups.Add(roof);
            Box(hall, "CeilingRib", At(d - 5, 0, 8.25f), hall.rotation, new Vector3(width, .24f, .16f), wall);
            foreach (float side in new[] {-1f, 1f})
            {
                Box(hall, "Fluorescent", At(d, side * 2.5f, 8.23f), hall.rotation, new Vector3(.48f, .1f, 3.5f), light);
                Box(hall, "StallDivider", At(d, side * (pathHalf + 4.3f), 1.35f), hall.rotation, new Vector3(4, 2.7f, .16f), white);
                string tank = ((int)d / 10 % 3) switch {
                    0 => "030_STAGE01_NRY_PROPS_019_Crab_aquarium_tank",
                    1 => "031_STAGE01_NRY_PROPS_020_Octopus_aquarium_tank", _ => "003_STAGE01_NRY_PROPS_003_Ice_fish_tank" };
                var display = Place(Stage01 + tank + "/" + tank + ".prefab", hall, At(d + 2, side * (pathHalf + 2.2f)), hall.rotation, 1);
                if (display != null) { FitWidth(display, 3.1f); display.position += Vector3.up * (.13f - Bounds(display).min.y); }
            }
            if ((int)d % 30 == 5)
            {
                var lamp = new GameObject("IndoorFill").AddComponent<Light>(); lamp.transform.SetParent(hall, false);
                lamp.transform.position = At(d, 0, 6.3f); lamp.type = LightType.Point; lamp.range = 12;
                lamp.intensity = 1.8f; lamp.color = new Color(.77f, .92f, 1); lamp.shadows = LightShadows.None;
            }
        }
        report.Add("indoor shell: tiled floor, opaque walls, y=8.5 white ceiling, modular tank stalls; upper y=12 roads retained");
    }

    static Sprite CardSprite(string source, string name)
    {
        string path = MeshDir + "/" + name + ".png";
        if (!File.Exists(path)) File.Copy(source, path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.maxTextureSize = 512; importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void BuildBranch()
    {
        var anchor = Anchor("B_MarketOrPier", HallOrigin, HallForward);
        var branch = anchor.gameObject.AddComponent<NoryangjinMarketBranch>();
        branch.triggerAhead = 12; branch.earliestChoice = 60; branch.length = 378; branch.outsideOffset = 25;
        branch.travelSpeed = 4.8f; branch.transition = 32;
        // Both routes climb over the S3 east pier (z = -7.25, branch distance ~52) instead of crossing it at grade.
        branch.liftHeight = LiftHeight;
        var ui = new GameObject("MarketRouteChoice", typeof(RectTransform)); ui.transform.SetParent(root, false);
        branch.choiceUI = ui.AddComponent<HighwayChapter2UI>(); branch.choiceUI.font = font;
        branch.indoorPicture = CardSprite("outputs/noryangjin-revamp-2026-09-27/concepts/n6-indoor-v3.png", "IndoorChoice");
        branch.outdoorPicture = CardSprite("outputs/noryangjin-revamp-2026-09-27/concepts/n4-wave-v3.png", "PierChoice");
        var timber = Mat("OutsideDeck", new Color(.44f, .30f, .17f));
        var rim = Mat("OutsideRailing", new Color(.28f, .2f, .12f));
        for (float d = 0; d < branch.length; d += 3)
        {
            branch.Sample(d + 1.5f, true, out var center, out var forward);
            var deck = Box(newRoads, "OutsidePierDeck", center + Vector3.up * -.02f, Quaternion.LookRotation(forward), new Vector3(pathHalf * 2 + 1.6f, .18f, 3.15f), timber);
            deck.gameObject.AddComponent<MeshCollider>().sharedMesh = deck.GetComponent<MeshFilter>().sharedMesh;
            Box(anchor, "DeckPlankJoint", center + Vector3.up * .076f, Quaternion.LookRotation(forward), new Vector3(pathHalf * 2 + 1.4f, .016f, .05f), rim);
            float lift = branch.Height(d + 1.5f);
            // Rails, beams and piles only once the pier has left the market walls.
            bool clearOfHall = Mathf.Abs(Vector3.Dot(center - branch.transform.position, branch.transform.right)) > pathHalf + 7.5f;
            if ((d < 35 && (lift < .4f || !clearOfHall)) || d > branch.length - 35) continue;
            foreach (float side in new[] {-1f, 1f})
            {
                Vector3 p = center + Vector3.Cross(Vector3.up, forward) * side * (pathHalf + .7f);
                Box(anchor, "PierRail", p + Vector3.up * 1.2f, Quaternion.LookRotation(forward), new Vector3(.15f, .16f, 3.15f), rim);
                if ((int)d % 9 == 0) Box(anchor, "PierPost", p + Vector3.up * .7f, Quaternion.identity, new Vector3(.3f, 1.4f, .3f), rim);
                // Bridge piles down to the sea, kept off the S3 road that passes underneath.
                if (lift > 1.2f && (int)d % 6 == 0 && !OverS3(p, 1.2f))
                    Box(anchor, "BridgePile", new Vector3(p.x, (lift - .2f - 2.6f) * .5f, p.z), Quaternion.identity, new Vector3(.42f, lift - .2f + 2.6f, .42f), Mat("BridgePileTimber", new Color(.24f, .17f, .1f)));
            }
            if (lift > 1.2f) Box(anchor, "BridgeBeam", center + Vector3.up * -.32f, Quaternion.LookRotation(forward), new Vector3(pathHalf * 2 + 1.2f, .38f, 3.1f), Mat("BridgeBeamTimber", new Color(.3f, .21f, .12f)));
        }
        var sign = Box(anchor, "ForkSign", HallOrigin + Vector3.left * 12 + Vector3.up * 5.4f, Quaternion.LookRotation(HallForward), new Vector3(7, 1.8f, .2f), Mat("SignBlue", Color.blue));
        Label(sign, "시장 안쪽  /  바깥 부두", 5, Color.white, new Vector2(6.8f, 1.7f), -HallForward * .13f);
        report.Add("branch: physical outside deck, 60s popup / 5s default indoor / shared cold-store merge");
    }

    static NoryangjinMarketIncident Incident(string name, Vector3 at, Vector3 forward, NoryangjinMarketIncident.Kind kind, string message, string prefab, int count = 1)
    {
        var a = Anchor(name, at, forward);
        var e = a.gameObject.AddComponent<NoryangjinMarketIncident>(); e.kind = kind; e.banner = message;
        e.triggerAhead = 24; e.hazardSeconds = e.duration = 10; e.halfWidth = pathHalf; e.warningSeconds = 2;
        var actors = new List<Transform>(); var breakables = new List<NoryangjinBreakable>();
        for (int i = 0; i < count; i++)
        {
            Vector3 start = new Vector3(-pathHalf - 4, 0, i * 5);
            if (kind == NoryangjinMarketIncident.Kind.ContainerDrop) start = new Vector3(e.lane, 11, 0);
            if (kind == NoryangjinMarketIncident.Kind.EscapingCart) start = new Vector3(0, 0, 0);
            var b = Breakable(a, name + "_Prop" + i, start, 1.2f, 1.0f, kind == NoryangjinMarketIncident.Kind.ReversingTruck ? 7 : 3);
            b.blocksPath = false; b.armOnRunStart = false; b.healthLabelHeight = 3;
            b.coins = kind == NoryangjinMarketIncident.Kind.EscapingCart ? 20 : 2;
            b.fishDropPrefab = kind == NoryangjinMarketIncident.Kind.Seagulls || kind == NoryangjinMarketIncident.Kind.ContainerDrop ? null : fishTemplate;
            Transform visual;
            if (string.IsNullOrEmpty(prefab))
            {
                visual = Box(b.transform, "ContainerBody", Vector3.up * 1.7f, Quaternion.identity, new Vector3(2.5f, 3.4f, 4.2f), Mat("CargoContainerBlue", new Color(.14f, .35f, .61f)), local: true);
                for (int j = 0; j < 8; j++) Box(visual, "Rib", visual.position + a.forward * (-1.9f + j * .53f), a.rotation, new Vector3(2.58f, 3.4f, .07f), Mat("ContainerRib", new Color(.1f, .26f, .44f)));
            }
            else
            {
                visual = Place(prefab, b.transform, Vector3.zero, Quaternion.identity, 1);
                if (visual != null) FitWidth(visual, kind == NoryangjinMarketIncident.Kind.ReversingTruck ? 5.5f : kind == NoryangjinMarketIncident.Kind.Seagulls ? 1.7f : 2.1f);
            }
            if (kind == NoryangjinMarketIncident.Kind.Seagulls) b.showHealth = false;
            actors.Add(b.transform); breakables.Add(b);
        }
        e.actors = actors.ToArray(); e.destructibles = breakables.ToArray();
        e.warning = Box(a, "WarningFootprint", at + a.right * e.lane + Vector3.up * .15f, a.rotation, new Vector3(2.8f, .018f, 6),
            Mat("IncidentWarning", new Color(1, .52f, .08f, .4f), transparent:true));
        if (kind == NoryangjinMarketIncident.Kind.ReversingTruck || kind == NoryangjinMarketIncident.Kind.EscapingCart)
            e.puddle = Box(a, "SpilledWater", at + a.right * e.lane + Vector3.up * .16f, a.rotation, new Vector3(3.5f, .025f, 12),
                Mat("SpilledWater", new Color(.15f, .64f, .88f, .45f), transparent:true));
        return e;
    }

    static void BuildMarketIncidents(GameObject[] templates)
    {
        var craneDrop = Incident("L_CraneContainer", new Vector3(225.5f, 0, -25), Vector3.forward, NoryangjinMarketIncident.Kind.ContainerDrop, "그림자 주의! 컨테이너가 내려옵니다", null);
        craneDrop.warningVoice = Voice("container-warning");
        string gull = Stage01 + "041_STAGE01_NRY_DCR_031_Flying_seagull_silhouette/041_STAGE01_NRY_DCR_031_Flying_seagull_silhouette.prefab";
        var gulls = Incident("M_SeagullFlock", new Vector3(326.7f, 0, 100), Vector3.forward, NoryangjinMarketIncident.Kind.Seagulls, "갈매기 떼! 한쪽으로 피하세요", gull, 4);
        gulls.duration = gulls.hazardSeconds = 7;
        var ca = Anchor("E3_ShopGuardianCat", HallOrigin + HallForward * 85, HallForward);
        var cat = ca.gameObject.AddComponent<NoryangjinCatEvent>(); cat.triggerAhead = 16; cat.routeScope = NoryangjinRevampEvent.RouteScope.Indoor;
        cat.cat = Place(Meshy + "N07_market_cat.prefab", ca, new Vector3(pathHalf + 2, 0, 0), Quaternion.Euler(0, -90, 0), 1);
        cat.protectedStall = Breakable(ca, "ProtectedShopCrate", new Vector3(pathHalf + .8f, 0, 1.2f), 1, .7f, 3);
        cat.protectedStall.blocksPath = false; cat.protectedStall.showHealth = false;
        BoxStack(cat.protectedStall, 2, 2, 1.4f);
        report.Add("incidents: shadow container, seagull flock, guardian cat; unmanned carts, box toss and warehouse-crossing truck removed");
    }

    static void BuildLandmarksAndAtmosphere(NoryangjinRevampDirector director)
    {
        var landmarks = Anchor("DistrictLandmarks", Vector3.zero, Vector3.forward);
        var cold = Anchor("ColdStorage", new Vector3(-67, 0, -323), Vector3.forward, landmarks);
        var frost = Mat("ColdStorageIce", new Color(.73f, .85f, .93f));
        var freezerFloor = Box(newRoads, "ColdStorageFloor", cold.position + cold.forward * 43 + Vector3.up * .02f, cold.rotation, new Vector3(pathHalf * 2 + 11, .15f, 132), frost);
        freezerFloor.gameObject.AddComponent<MeshCollider>().sharedMesh = freezerFloor.GetComponent<MeshFilter>().sharedMesh;
        foreach (float side in new[] {-1f, 1f})
        {
            Box(cold, "WarehouseWall", cold.position + cold.forward * 34 + cold.right * side * (pathHalf + 5), cold.rotation, new Vector3(.4f, 10, 84), frost);
            for (int i = 0; i < 9; i++)
            {
                var tuna = Place(Meshy + "N08_frozen_tuna.prefab", cold, new Vector3(side * (pathHalf + 2.4f), 2.4f, i * 7), Quaternion.Euler(70, 0, 0), .7f);
                if (tuna != null) Cylinder(cold, "TunaHangingRail", new Vector3(side * (pathHalf + 2.4f), 5.8f, i * 7), Quaternion.identity, new Vector3(.04f, 2.5f, .04f), Mat("ColdRail", new Color(.28f, .4f, .52f)));
            }
        }
        Place(Meshy + "N03_forklift.prefab", cold, new Vector3(-pathHalf - 4, 0, 22), Quaternion.Euler(0, 90, 0), 1);
        for (int i = 0; i < 7; i++)
        {
            var roof = Box(cold, "ColdRoof", cold.position + cold.forward * (6 + i * 12) + Vector3.up * 10, cold.rotation,
                new Vector3(pathHalf * 2 + 11, .25f, 12), frost); ceilingGroups.Add(roof);
            var mist = Place(Cfxr + "Misc/CFXR Smoke Source 3D.prefab", cold, new Vector3(i % 2 == 0 ? -pathHalf - 2 : pathHalf + 2, .3f, 6 + i * 12), Quaternion.identity, .6f);
            if (mist != null) foreach (var ps in mist.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main; main.startColor = new Color(.78f,.9f,1,.2f); main.startLifetime = 2; main.startSize = 2.2f; main.maxParticles = 16;
                var emission = ps.emission; emission.rateOverTime = 3;
            }
        }
        foreach (float along in new[] {0f, 70f})
        {
            var curtain=Anchor("ColdCurtain",cold.position+cold.forward*along,cold.forward,cold);
            for (int i = 0; i < 12; i++) Box(curtain, "VinylCurtain", cold.position + cold.forward * along + cold.right * (i - 5.5f) * .7f + Vector3.up * 6,
                cold.rotation, new Vector3(.66f, 3.8f, .06f), Mat("ColdVinyl", new Color(.6f, .85f, .95f, .24f), transparent:true));
            var sign = Box(cold, "ColdStoreSign", cold.position + cold.forward * along + Vector3.up * 9.2f, cold.rotation, new Vector3(10, 1.4f, .2f), Mat("ColdStoreBlue", new Color(.03f, .16f, .38f)));
            Label(sign, "노량진 냉동 창고", 5, Color.white, new Vector2(9.8f, 1.2f), -cold.forward * .14f);
            ceilingGroups.Add(sign);
        }
        // Far enough from the x=225.5 pier that the passing camera never fills with crane.
        Place(Meshy + "N05_harbor_crane.prefab", landmarks, new Vector3(245, 0, -22), Quaternion.Euler(0, 90, 0), 1);
        Place(Meshy + "N05_harbor_crane.prefab", landmarks, new Vector3(247, 0, 44), Quaternion.Euler(0, 10, 0), 1.2f);
        string boat = Stage01 + "018_STAGE01_NRY_BG_002_Harbor_fishing_boat/018_STAGE01_NRY_BG_002_Harbor_fishing_boat.prefab";
        var ship = Place(boat, landmarks, new Vector3(259, -1.2f, 9), Quaternion.Euler(0, 20, 0), 1);
        if (ship != null) FitWidth(ship, 24);
        Place(Meshy + "N06_red_lighthouse.prefab", landmarks, new Vector3(256, 0, 318), Quaternion.identity, 1.1f);
        var skyline = Mat("DistantSkyline", new Color(.27f, .40f, .53f));
        for (int i = 0; i < 18; i++)
            Box(landmarks, "DistantCity", new Vector3(-140 + i * 35, 10 + i % 4 * 6, 375), Quaternion.identity, new Vector3(18, 20 + i % 4 * 12, 15), skyline);
        var bridge = Box(landmarks, "DistantRailBridge", new Vector3(160, 21, 330), Quaternion.identity, new Vector3(390, 2, 10), Mat("BridgeSteel", new Color(.4f, .44f, .51f)));
        for (int i = 0; i < 9; i++) Box(landmarks, "BridgePier", new Vector3(-10 + i * 44, 10, 330), Quaternion.identity, new Vector3(4, 20, 7), skyline);
        var train = Anchor("Line1Train", new Vector3(120, 23, 330), Vector3.right, landmarks);
        for (int i = 0; i < 5; i++)
        {
            Box(train, "RailCar", train.position + Vector3.right * i * 10, Quaternion.identity, new Vector3(9.5f, 3.5f, 3), Mat("TrainCream", new Color(.78f, .83f, .84f)));
            Box(train, "Line1Blue", train.position + Vector3.right * i * 10 + Vector3.back * 1.52f, Quaternion.identity, new Vector3(9.5f, 1.1f, .03f), Mat("TrainBlue", new Color(.05f, .18f, .65f)));
            for (int j = 0; j < 5; j++) Box(train, "TrainWindow", train.position + Vector3.right * (i * 10 + (j - 2) * 1.5f) + Vector3.back * 1.55f + Vector3.up * .8f,
                Quaternion.identity, new Vector3(1, .75f, .02f), Mat("TrainWindow", new Color(.05f, .11f, .2f)));
        }
        var atmosphere = root.gameObject.AddComponent<NoryangjinMarketAtmosphere>();
        atmosphere.gameplayCamera = Object.FindFirstObjectByType<StableGameplayCamera>(); atmosphere.train = train;
        atmosphere.auction = root.GetComponentsInChildren<NoryangjinRushEvent>().FirstOrDefault(e => e.name == "D1_BidderWall");
        atmosphere.clocks = root.GetComponentsInChildren<TMP_Text>().Where(t => t.transform.parent.name.Contains("Clock")).ToArray();
        atmosphere.televisions = root.GetComponentsInChildren<TMP_Text>().Where(t => t.transform.parent.parent != null && t.transform.parent.parent.name == "MarketTV").ToArray();
        string skyPath = MatDir + "/TimeSky.mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null) { sky = new Material(Shader.Find("FlatKit/GradientSkybox")); AssetDatabase.CreateAsset(sky, skyPath); }
        sky.shader = Shader.Find("FlatKit/GradientSkybox"); sky.SetFloat("_Intensity", 1); sky.SetFloat("_Exponent", 1.4f);
        sky.SetVector("_Direction",Vector3.up); sky.SetColor("_Color2",new Color(.24f,.37f,.62f)); sky.SetColor("_Color1",new Color(.32f,.4f,.52f));
        atmosphere.skyTemplate = sky; EditorUtility.SetDirty(sky);
        atmosphere.broadcasts = Enumerable.Range(1,3).Select(i => Voice("broadcast-" + i)).ToArray();
        var camera = GameObject.Find("MapTool_Camera").GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.Skybox;
        var props = GameObject.Find("Noryangjin_MapTool/Props").transform;
        foreach (Transform shop in props)
        {
            if (!shop.name.StartsWith("SR18_Route_Shop_") || InHallScenery(shop.position)) continue;
            int zone = shop.position.x < 0 ? 0 : shop.position.x < 200 ? 1 : shop.position.z < 80 ? 3 : 4;
            Color tint = new[] {new Color(1,.86f,.67f), new Color(.75f,1,.95f),Color.white,new Color(1,.94f,.82f),new Color(1,.78f,.68f)}[zone];
            var tintOwner = shop.GetComponent<NoryangjinShopTint>() ?? shop.gameObject.AddComponent<NoryangjinShopTint>(); tintOwner.color = tint;
            foreach (var renderer in shop.GetComponentsInChildren<Renderer>(true))
            {
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block); block.SetColor("_BaseColor", tint); renderer.SetPropertyBlock(block);
            }
        }
        report.Add("landmarks: freezer/forklift/tuna, crane/boat, lighthouse, skyline and animated Line1 train");
    }

    // ---------------------------------------------------------------- templates
    static GameObject[] Templates()
    {
        var holder = new GameObject("Templates").transform; holder.SetParent(root, false);
        holder.gameObject.SetActive(false);
        var list = new List<GameObject>();
        foreach (string id in new[] { "Enemy_OldMan", "Enemy_YllowMan_Sword", "Enemy_YllowMan_Net", "Enemy_Woman" })
        {
            var prefab = Load<GameObject>(Enemies + id + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
            go.name = "Template_" + id;
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var c in go.GetComponents<Component>())
                if (!(c is Transform || c is Animator || c is EnemyScript_space || c is Collider || c is AudioSource || c is Rigidbody)) Object.DestroyImmediate(c);
            go.transform.localScale = Vector3.one * 1.69f;
            list.Add(go);
        }
        return list.ToArray();
    }

    static Transform Visual(GameObject template, Transform parent, Vector3 pos, Quaternion rot, string name)
    {
        var go = Object.Instantiate(template, parent);
        go.name = name; go.SetActive(true);
        go.transform.SetPositionAndRotation(pos, rot * Quaternion.Euler(0,180,0));
        go.transform.localScale = Vector3.one * 1.69f;
        if (name == "TurretDriver")
        {
            var enemy = go.GetComponent<EnemyScript_space>();
            var held = enemy != null ? new SerializedObject(enemy).FindProperty("heldProjectile")?.objectReferenceValue as Transform : null;
            if (held != null) held.gameObject.SetActive(false);
        }
        foreach (var c in go.GetComponents<Component>()) if (c is EnemyScript_space || c is AudioSource || c is Rigidbody) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var t in go.GetComponentsInChildren<TMP_Text>(true)) t.gameObject.SetActive(false);
        foreach (var c in go.GetComponentsInChildren<Canvas>(true)) c.gameObject.SetActive(false);
        go.tag = "Untagged";
        return go.transform;
    }

    // ---------------------------------------------------------------- helpers
    static NoryangjinBreakable Breakable(Transform parent, string name, Vector3 local, float halfWidth, float halfDepth, float hits)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = local; go.transform.localRotation = Quaternion.identity;
        var b = go.AddComponent<NoryangjinBreakable>();
        b.halfWidth = halfWidth; b.halfDepth = halfDepth; b.hitsToBreak = hits;
        var col = go.AddComponent<BoxCollider>(); col.isTrigger = true;
        col.center = new Vector3(0, 1.3f, 0); col.size = new Vector3(halfWidth * 2, 2.6f, halfDepth * 2);
        go.tag = "Obstacle";
        go.layer = LayerMask.NameToLayer("Enemy");
        return b;
    }

    static void BoxStack(NoryangjinBreakable b, int layers, int across, float width)
    {
        var pieces = new List<Transform>();
        string path = Stage01 + "002_STAGE01_NRY_PROPS_002_Styrofoam_fish_box/002_STAGE01_NRY_PROPS_002_Styrofoam_fish_box.prefab";
        float cell = width / across;
        for (int y = 0; y < layers; y++)
            for (int x = 0; x < across; x++)
            {
                var box = Place(path, b.transform, new Vector3(-width * .5f + cell * (x + .5f) + (y % 2) * cell * .12f, 0, (y % 2) * .08f), Quaternion.Euler(0, (x * 7 + y * 13) % 11 - 5, 0), 1f);
                if (box == null) continue;
                FitWidth(box, cell * .98f);
                var bounds = Bounds(box);
                box.position += Vector3.up * (y * bounds.size.y - (bounds.min.y - b.transform.position.y));
                pieces.Add(box);
            }
        b.shedPieces = pieces.ToArray();
        if (b.fishDropPrefab != null) { b.rewardsPerPiece = true; b.coins = 0; }
        var col = b.GetComponent<BoxCollider>();
        if (col != null) { col.size = new Vector3(width, Mathf.Max(1.8f, layers * .75f), b.halfDepth * 2); col.center = new Vector3(0, col.size.y * .5f, 0); }
    }

    static Transform Place(string path, Transform parent, Vector3 localOrWorld, Quaternion rot, float scale)
    {
        var prefab = Load<GameObject>(path);
        if (prefab == null) { report.Add("missing prefab " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        if (localOrWorld.sqrMagnitude > 2500) go.transform.SetPositionAndRotation(localOrWorld, rot);
        else { go.transform.localPosition = localOrWorld; go.transform.localRotation = rot; }
        go.transform.localScale *= scale;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        if (path.StartsWith(Stage01) && Mathf.Abs(localOrWorld.y) < .001f)
        {
            float ground = go.transform.position.y;
            go.transform.position += Vector3.up * (ground - Bounds(go).min.y);
        }
        return go.transform;
    }

    static void FitWidth(Transform t, float width)
    {
        var b = Bounds(t.gameObject);
        float w = Mathf.Max(b.size.x, b.size.z);
        if (w > .001f) t.localScale *= width / w;
    }
    static void FitLength(Transform t, float length) => FitWidth(t, length);

    static Bounds Bounds(Transform t) => Bounds(t.gameObject);
    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }

    static Transform Box(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 size, Material mat, bool world = true, bool local = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        if (local) { go.transform.localPosition = pos; go.transform.localRotation = rot; }
        else go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = Vector3.one;
        var lossy = parent.lossyScale;
        go.transform.localScale = new Vector3(size.x / Mathf.Max(.0001f, lossy.x), size.y / Mathf.Max(.0001f, lossy.y), size.z / Mathf.Max(.0001f, lossy.z));
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Transform Cylinder(Transform parent, string name, Vector3 local, Quaternion rot, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false); go.transform.localPosition = local; go.transform.localRotation = rot; go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    static void Label(Transform onto, string text, float size, Color color, Vector2 rect, Vector3 worldOffset)
    {
        var go = new GameObject("Label"); go.transform.SetParent(onto, false);
        go.transform.position = onto.position + worldOffset; go.transform.rotation = onto.rotation;
        var lossy = onto.lossyScale;
        go.transform.localScale = new Vector3(1 / Mathf.Max(.0001f, lossy.x), 1 / Mathf.Max(.0001f, lossy.y), 1 / Mathf.Max(.0001f, lossy.z));
        var t = go.AddComponent<TextMeshPro>();
        t.font = font; t.text = text; t.fontSize = size; t.color = color; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center; t.enableAutoSizing = true; t.fontSizeMin = 1; t.fontSizeMax = size;
        t.rectTransform.sizeDelta = rect;
    }

    static void LabelOutline(TMP_Text text, string name, Color color)
    {
        string path = MatDir + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(font.material); AssetDatabase.CreateAsset(material,path); }
        material.SetFloat(ShaderUtilities.ID_OutlineWidth,.3f); material.SetColor(ShaderUtilities.ID_OutlineColor,color);
        text.fontSharedMaterial = material; text.UpdateMeshPadding(); EditorUtility.SetDirty(material);
    }

    static Transform ShieldAura()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "ShieldAura";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(root, false); go.transform.localScale = new Vector3(2.6f, 2.9f, 2.6f);
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat("ShieldAura", new Color(.35f, .7f, 1f, .28f), transparent: true, emission: new Color(.2f, .45f, .9f));
        go.SetActive(false);
        return go.transform;
    }

    static Material Mat(string name, Color color, bool transparent = false, Color? emission = null)
    {
        if (mats.TryGetValue(name, out var cached)) return cached;
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", transparent ? .6f : .25f);
        if (transparent)
        {
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
            m.SetOverrideTag("RenderType", "Transparent");
        }
        if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
        EditorUtility.SetDirty(m);
        mats[name] = m;
        return m;
    }

    static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) report.Add("missing " + path);
        return a;
    }
}
