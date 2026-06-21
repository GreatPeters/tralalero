#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NoryangjinMapToolConceptLayoutBuilder
{
    private const string ScenePath = NoryangjinMapToolWindow.MapToolScenePath;
    private const string PaletteDefaultsPath = "Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset";
    private const string RequestPath = "Temp/NoryangjinMapToolConceptLayoutRequest.txt";
    private const string ReportPath = "Temp/NoryangjinMapToolConceptLayoutReport.txt";

    private const string RootName = "Noryangjin_MapTool";
    private const string RoadParentName = "Roads";
    private const string PropParentName = "Props";
    private const string WaterParentName = "Water";
    private const string WorkFloorName = "MapTool_Work_Floor";
    private const string WorkGridName = "MapTool_Work_Grid";
    private const string OriginPostName = "MapTool_Origin_Post";

    private const float PlacementHeight = 0f;
    private static readonly Vector3 Origin = Vector3.zero;

    private const string DockPierLongPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy.prefab";
    private const string DockPierLeftTurnPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy_LeftTurn.prefab";
    private const string DockPierRightTurnPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy_RightTurn.prefab";
    private const string RopeBridgePath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Bridges_Fantasy/Bridge_Rope_Small_Fantasy.prefab";
    private const string UphillPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Rope_Stairs_Fantasy.prefab";
    private const string DownhillPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Rope_Stairs_Fantasy_Downhill.prefab";
    private const string PierPillarsPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Pillars_Fantasy.prefab";

    private const string WaterPath = NoryangjinMapToolWindow.JhWaterPrefabPath;
    private const string BlueCratePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/001_STAGE01_NRY_PROPS_001_Blue_fish_crate/001_STAGE01_NRY_PROPS_001_Blue_fish_crate.prefab";
    private const string StyrofoamBoxPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/002_STAGE01_NRY_PROPS_002_Styrofoam_fish_box/002_STAGE01_NRY_PROPS_002_Styrofoam_fish_box.prefab";
    private const string IceFishTankPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/003_STAGE01_NRY_PROPS_003_Ice_fish_tank/003_STAGE01_NRY_PROPS_003_Ice_fish_tank.prefab";
    private const string PushCartPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/004_STAGE01_NRY_OBSTACLE_004_Seafood_push_cart/004_STAGE01_NRY_OBSTACLE_004_Seafood_push_cart.prefab";
    private const string SafetyConePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/005_STAGE01_NRY_OBSTACLE_005_Wet_floor_safety_cone/005_STAGE01_NRY_OBSTACLE_005_Wet_floor_safety_cone.prefab";
    private const string BuoyRopePostPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/006_STAGE01_NRY_PROPS_006_Buoy_with_rope_post/006_STAGE01_NRY_PROPS_006_Buoy_with_rope_post.prefab";
    private const string LampSignPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/007_STAGE01_NRY_PROPS_007_Fish_market_lamp_sign/007_STAGE01_NRY_PROPS_007_Fish_market_lamp_sign.prefab";
    private const string SeagullPerchPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/008_STAGE01_NRY_PROPS_008_Seagull_perch_post/008_STAGE01_NRY_PROPS_008_Seagull_perch_post.prefab";
    private const string DroppedFishPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/009_STAGE01_NRY_PICKUP_009_Dropped_fish_pickup/009_STAGE01_NRY_PICKUP_009_Dropped_fish_pickup.prefab";
    private const string PufferEnemyPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/010_STAGE01_NRY_ENEMY_010_Puffer_enemy/010_STAGE01_NRY_ENEMY_010_Puffer_enemy.prefab";
    private const string DockRailingPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/011_STAGE01_NRY_BND_001_Dock_railing_module/011_STAGE01_NRY_BND_001_Dock_railing_module.prefab";
    private const string RopeBarrierPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/012_STAGE01_NRY_BND_002_Rope_post_barrier/012_STAGE01_NRY_BND_002_Rope_post_barrier.prefab";
    private const string StorefrontPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/014_STAGE01_NRY_BLD_001_Fish_market_storefront_facade/014_STAGE01_NRY_BLD_001_Fish_market_storefront_facade.prefab";
    private const string SashimiStallPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/015_STAGE01_NRY_BLD_002_Sashimi_restaurant_stall_front/015_STAGE01_NRY_BLD_002_Sashimi_restaurant_stall_front.prefab";
    private const string SeafoodStallPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/016_STAGE01_NRY_BLD_003_Seafood_display_stall_module/016_STAGE01_NRY_BLD_003_Seafood_display_stall_module.prefab";
    private const string HarborBoatPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/018_STAGE01_NRY_BG_002_Harbor_fishing_boat/018_STAGE01_NRY_BG_002_Harbor_fishing_boat.prefab";
    private const string HillsideVillagePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/019_STAGE01_NRY_BG_003_Distant_hillside_village_module/019_STAGE01_NRY_BG_003_Distant_hillside_village_module.prefab";
    private const string FishBoxStackPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/020_STAGE01_NRY_DCR_001_Fish_box_stack/020_STAGE01_NRY_DCR_001_Fish_box_stack.prefab";
    private const string AquariumRowPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/021_STAGE01_NRY_DCR_002_Aquarium_tank_row/021_STAGE01_NRY_DCR_002_Aquarium_tank_row.prefab";
    private const string IceBoxStackPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/022_STAGE01_NRY_DCR_003_Ice_box_stack/022_STAGE01_NRY_DCR_003_Ice_box_stack.prefab";
    private const string DockPilingPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/023_STAGE01_NRY_PROPS_011_Standalone_dock_piling/023_STAGE01_NRY_PROPS_011_Standalone_dock_piling.prefab";
    private const string LifeRingPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/024_STAGE01_NRY_PROPS_012_Life_ring_buoy/024_STAGE01_NRY_PROPS_012_Life_ring_buoy.prefab";
    private const string TireFenderPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/025_STAGE01_NRY_PROPS_013_Boat_tire_fender/025_STAGE01_NRY_PROPS_013_Boat_tire_fender.prefab";
    private const string DockCleatPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/026_STAGE01_NRY_PROPS_014_Dock_metal_cleat/026_STAGE01_NRY_PROPS_014_Dock_metal_cleat.prefab";
    private const string RedLampPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/027_STAGE01_NRY_DCR_016_Red_market_hanging_lamp/027_STAGE01_NRY_DCR_016_Red_market_hanging_lamp.prefab";
    private const string CrabSignPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/028_STAGE01_NRY_PROPS_017_Crab_mascot_sign/028_STAGE01_NRY_PROPS_017_Crab_mascot_sign.prefab";
    private const string FishSignPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/029_STAGE01_NRY_PROPS_018_Fish_mascot_sign/029_STAGE01_NRY_PROPS_018_Fish_mascot_sign.prefab";
    private const string CrabTankPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/030_STAGE01_NRY_PROPS_019_Crab_aquarium_tank/030_STAGE01_NRY_PROPS_019_Crab_aquarium_tank.prefab";
    private const string OctopusTankPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/031_STAGE01_NRY_PROPS_020_Octopus_aquarium_tank/031_STAGE01_NRY_PROPS_020_Octopus_aquarium_tank.prefab";
    private const string OrangeCratePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/032_STAGE01_NRY_PROPS_021_Orange_fish_crate_variant/032_STAGE01_NRY_PROPS_021_Orange_fish_crate_variant.prefab";
    private const string WhiteCratePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/033_STAGE01_NRY_PROPS_022_White_fish_crate_variant/033_STAGE01_NRY_PROPS_022_White_fish_crate_variant.prefab";
    private const string NetPilePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/034_STAGE01_NRY_DCR_023_Fishing_net_pile/034_STAGE01_NRY_DCR_023_Fishing_net_pile.prefab";
    private const string AnchorPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/035_STAGE01_NRY_PROPS_024_Anchor_prop/035_STAGE01_NRY_PROPS_024_Anchor_prop.prefab";
    private const string UtilityPolePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/036_STAGE01_NRY_DCR_025_Harbor_utility_pole/036_STAGE01_NRY_DCR_025_Harbor_utility_pole.prefab";
    private const string SeaBuoyPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/037_STAGE01_NRY_BG_026_Floating_sea_buoy/037_STAGE01_NRY_BG_026_Floating_sea_buoy.prefab";
    private const string IceScatterPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/039_STAGE01_NRY_DCR_029_Ice_chunk_floor_scatter/039_STAGE01_NRY_DCR_029_Ice_chunk_floor_scatter.prefab";
    private const string FishScrapPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/040_STAGE01_NRY_DCR_030_Fish_scrap_floor_scatter/040_STAGE01_NRY_DCR_030_Fish_scrap_floor_scatter.prefab";
    private const string FlyingSeagullPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/041_STAGE01_NRY_DCR_031_Flying_seagull_silhouette/041_STAGE01_NRY_DCR_031_Flying_seagull_silhouette.prefab";
    private const string BoatDetailPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/042_STAGE01_NRY_BG_032_Fishing_boat_detail_kit/042_STAGE01_NRY_BG_032_Fishing_boat_detail_kit.prefab";
    private const string AwningVariantPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/043_STAGE01_NRY_BLD_034_Market_awning_color_variant_set/043_STAGE01_NRY_BLD_034_Market_awning_color_variant_set.prefab";
    private const string BarricadePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/044_STAGE01_NRY_OBSTACLE_036_Fish-market_wooden_X_barricade/044_STAGE01_NRY_OBSTACLE_036_Fish-market_wooden_X_barricade.prefab";
    private const string SignalGantryPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/045_STAGE01_NRY_GAMEPLAY_037_Harbor_lane_signal_gantry/045_STAGE01_NRY_GAMEPLAY_037_Harbor_lane_signal_gantry.prefab";

    [InitializeOnLoadMethod]
    private static void RunRequestedBuild()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RequestPath))
                return;

            File.Delete(RequestPath);

            try
            {
                BuildLayout();
            }
            catch (Exception ex)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, "Failed: " + ex);
                Debug.LogException(ex);
            }
        };
    }

    [MenuItem("Tools/MeshyAI/Build Noryangjin MapTool Concept Layout", false, 2312)]
    public static void BuildLayout()
    {
        EnsureFolder("Assets/ShooterSurvival/Scenes");
        EnsureFolder("Assets/ShooterSurvival/Scenes/Tools");

        Scene scene = File.Exists(ScenePath)
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var context = new BuildContext(LoadPaletteDefaults());
        GameObject root = EnsureRoot();
        Transform roads = EnsureChild(root.transform, RoadParentName);
        Transform props = EnsureChild(root.transform, PropParentName);
        Transform water = EnsureChild(root.transform, WaterParentName);

        ClearGeneratedChildren(roads, "Road_Concept");
        ClearGeneratedChildren(props, "Prop_Concept");
        ClearGeneratedChildren(water, "Concept_");
        ClearGeneratedChildren(water, "Prop_Concept");

        SetupWorkScene(root.transform);

        List<RouteNode> route = BuildRoute(context, roads);
        PlaceWaterAndBackground(context, water);
        PlaceBoundaries(context, props, route);
        PlaceMarketSide(context, props, route);
        PlaceHarborSide(context, props, route);
        PlaceGameplay(context, props, route);
        PlaceAtmosphere(context, props);
        SetupCameraAndLight(route);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WriteReport(context, route.Count);
        Debug.Log($"[MeshyAI] Noryangjin map-tool concept layout built in {ScenePath}. {context.Placed} objects placed, {context.Missing.Count} missing.");
    }

    private static List<RouteNode> BuildRoute(BuildContext context, Transform parent)
    {
        var route = new List<RouteNode>
        {
            new(0, -18, 0f, DockPierLongPath, "Basic"),
            new(0, -13, 0f, DockPierLongPath, "Basic"),
            new(0, -8, 0f, DockPierLongPath, "Basic"),
            new(0, -3, 0f, DockPierLeftTurnPath, "LeftTurn"),
            new(-5, -3, 270f, DockPierLongPath, "Basic"),
            new(-10, -3, 270f, DockPierRightTurnPath, "RightTurn"),
            new(-10, 2, 0f, RopeBridgePath, "Bridge"),
            new(-10, 7, 0f, UphillPath, "Uphill"),
            new(-10, 12, 0f, DockPierRightTurnPath, "RightTurn"),
            new(-5, 12, 90f, DockPierLongPath, "Basic"),
            new(0, 12, 90f, DockPierLongPath, "Basic"),
            new(5, 12, 90f, DockPierLeftTurnPath, "LeftTurn"),
            new(5, 17, 0f, DownhillPath, "Downhill"),
            new(5, 20, 0f, DockPierLongPath, "Basic")
        };

        foreach (RouteNode node in route)
        {
            GameObject road = PlacePrefab(
                context,
                node.PrefabPath,
                parent,
                NoryangjinMapToolGridUtility.BuildInstanceName("Road", "Concept" + node.Label, node.X, node.Z),
                node.X,
                node.Z,
                node.Yaw,
                Vector3.one,
                Vector3.zero);

            if (road == null)
                continue;

            if (node.PrefabPath == UphillPath)
                PlaceRoadCompanion(context, road.transform, node.Label, Vector3.zero);
            else if (node.PrefabPath == DownhillPath)
                PlaceRoadCompanion(context, road.transform, node.Label, new Vector3(0f, 1.075f, -2.233f));
        }

        return route;
    }

    private static void PlaceWaterAndBackground(BuildContext context, Transform parent)
    {
        PlacePrefab(context, WaterPath, parent, "Concept_Background_Water_Left", -19, 0, 0f, new Vector3(5.5f, 1f, 10f), Vector3.down * 0.18f);
        PlacePrefab(context, WaterPath, parent, "Concept_Background_Water_Right", 19, 0, 0f, new Vector3(5.5f, 1f, 10f), Vector3.down * 0.18f);

        PlaceProp(context, parent, SeaBuoyPath, -19, 0, 0f, new Vector3(1.3f, 1.3f, 1.3f));
        PlaceProp(context, parent, SeaBuoyPath, 19, 13, 0f, new Vector3(1.2f, 1.2f, 1.2f));
    }

    private static void PlaceBoundaries(BuildContext context, Transform parent, List<RouteNode> route)
    {
        for (int i = 0; i < route.Count; i += 2)
        {
            RouteNode node = route[i];
            Vector2Int right = SideOffset(node.Yaw, 6, true);
            Vector2Int left = SideOffset(node.Yaw, 6, false);
            string rightPath = i % 4 == 0 ? DockRailingPath : RopeBarrierPath;
            string leftPath = i % 4 == 0 ? RopeBarrierPath : DockPilingPath;

            PlaceProp(context, parent, rightPath, node.X + right.x, node.Z + right.y, node.Yaw, Vector3.one);
            PlaceProp(context, parent, leftPath, node.X + left.x, node.Z + left.y, node.Yaw + 180f, Vector3.one);

            if (i % 6 == 0)
                PlaceProp(context, parent, DockCleatPath, node.X + left.x + 2, node.Z + left.y, node.Yaw, Vector3.one);
        }
    }

    private static void PlaceMarketSide(BuildContext context, Transform parent, List<RouteNode> route)
    {
        string[] facades = { StorefrontPath, SashimiStallPath, SeafoodStallPath, AwningVariantPath };
        for (int i = 1; i < route.Count; i += 3)
        {
            RouteNode node = route[i];
            Vector2Int right = SideOffset(node.Yaw, 10, true);
            string facadePath = facades[(i / 3) % facades.Length];
            PlaceProp(context, parent, facadePath, node.X + right.x, node.Z + right.y, node.Yaw - 90f, Vector3.one);

            Vector2Int display = SideOffset(node.Yaw, 7, true);
            PlaceProp(context, parent, PickDisplayProp(i), node.X + display.x, node.Z + display.y, node.Yaw - 90f, Vector3.one);
            if (i % 6 == 1)
                PlaceProp(context, parent, RedLampPath, node.X + right.x - 2, node.Z + right.y + 2, node.Yaw - 90f, Vector3.one);
        }

        PlaceProp(context, parent, CrabSignPath, 12, -16, -90f, Vector3.one);
        PlaceProp(context, parent, FishSignPath, 12, -1, -90f, Vector3.one);
        PlaceProp(context, parent, LampSignPath, 14, 14, -90f, Vector3.one);
    }

    private static void PlaceHarborSide(BuildContext context, Transform parent, List<RouteNode> route)
    {
        string[] edgeProps =
        {
            BlueCratePath, StyrofoamBoxPath, OrangeCratePath, WhiteCratePath,
            FishBoxStackPath, IceBoxStackPath, NetPilePath, AnchorPath,
            LifeRingPath, TireFenderPath, BuoyRopePostPath
        };

        for (int i = 0; i < route.Count; i += 2)
        {
            RouteNode node = route[i];
            Vector2Int left = SideOffset(node.Yaw, 8, false);
            Vector2Int nearLeft = SideOffset(node.Yaw, 5, false);
            PlaceProp(context, parent, edgeProps[i % edgeProps.Length], node.X + left.x, node.Z + left.y, node.Yaw + 90f, Vector3.one);

            if (i % 4 == 0)
                PlaceProp(context, parent, i % 8 == 0 ? IceScatterPath : FishScrapPath, node.X + nearLeft.x, node.Z + nearLeft.y, node.Yaw, Vector3.one);
        }

        PlaceProp(context, parent, SeagullPerchPath, -9, -18, 0f, Vector3.one);
        PlaceProp(context, parent, UtilityPolePath, -15, 4, 0f, new Vector3(1.1f, 1.1f, 1.1f));
    }

    private static void PlaceGameplay(BuildContext context, Transform parent, List<RouteNode> route)
    {
        PlaceProp(context, parent, SignalGantryPath, route[1].X, route[1].Z - 3, 180f, Vector3.one);
        PlaceProp(context, parent, SignalGantryPath, route[^2].X, route[^2].Z + 3, 180f, Vector3.one);

        foreach (int routeIndex in new[] { 1, 3, 5, 7, 9, 11, 12 })
        {
            RouteNode node = route[routeIndex];
            PlaceProp(context, parent, DroppedFishPath, node.X, node.Z, node.Yaw + 180f, Vector3.one);
        }

        PlaceProp(context, parent, BarricadePath, route[3].X + 3, route[3].Z, 0f, Vector3.one);
        PlaceProp(context, parent, BarricadePath, route[11].X - 3, route[11].Z, 0f, Vector3.one);
        PlaceProp(context, parent, PushCartPath, route[7].X + 4, route[7].Z, 90f, Vector3.one);
        PlaceProp(context, parent, PufferEnemyPath, route[10].X + 4, route[10].Z, 180f, Vector3.one);
        PlaceProp(context, parent, SafetyConePath, route[0].X - 4, route[0].Z + 2, 0f, Vector3.one);
        PlaceProp(context, parent, SafetyConePath, route[^1].X + 4, route[^1].Z - 2, 0f, Vector3.one);
    }

    private static void PlaceAtmosphere(BuildContext context, Transform parent)
    {
        PlaceProp(context, parent, FlyingSeagullPath, -8, -4, 25f, new Vector3(1.4f, 1.4f, 1.4f), Vector3.up * 6f);
        PlaceProp(context, parent, FlyingSeagullPath, 12, 8, -20f, new Vector3(1.25f, 1.25f, 1.25f), Vector3.up * 7f);
        PlaceProp(context, parent, FlyingSeagullPath, -14, 18, 12f, new Vector3(1.2f, 1.2f, 1.2f), Vector3.up * 6.4f);
    }

    private static string PickDisplayProp(int index)
    {
        string[] displayProps =
        {
            IceFishTankPath,
            AquariumRowPath,
            CrabTankPath,
            OctopusTankPath,
            BlueCratePath,
            StyrofoamBoxPath,
            OrangeCratePath,
            WhiteCratePath
        };

        return displayProps[index % displayProps.Length];
    }

    private static void PlaceRoadCompanion(BuildContext context, Transform roadRoot, string label, Vector3 localPosition)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PierPillarsPath);
        if (prefab == null)
        {
            context.MarkMissing(PierPillarsPath);
            return;
        }

        GameObject companion = PrefabUtility.InstantiatePrefab(prefab, roadRoot) as GameObject;
        if (companion == null)
            companion = UnityEngine.Object.Instantiate(prefab, roadRoot);

        companion.name = "Concept" + label + "_Pillars";
        companion.transform.localPosition = localPosition;
        companion.transform.localRotation = Quaternion.identity;
        companion.transform.localScale = Vector3.one;
        context.Placed++;
        EditorUtility.SetDirty(companion);
    }

    private static void PlaceProp(BuildContext context, Transform parent, string prefabPath, int x, int z, float yaw, Vector3 scaleMultiplier)
    {
        PlaceProp(context, parent, prefabPath, x, z, yaw, scaleMultiplier, Vector3.zero);
    }

    private static void PlaceProp(
        BuildContext context,
        Transform parent,
        string prefabPath,
        int x,
        int z,
        float yaw,
        Vector3 scaleMultiplier,
        Vector3 worldOffset)
    {
        string variant = "Concept_" + Path.GetFileNameWithoutExtension(prefabPath);
        PlacePrefab(
            context,
            prefabPath,
            parent,
            NoryangjinMapToolGridUtility.BuildInstanceName("Prop", variant, x, z),
            x,
            z,
            yaw,
            scaleMultiplier,
            worldOffset);
    }

    private static GameObject PlacePrefab(
        BuildContext context,
        string prefabPath,
        Transform parent,
        string instanceName,
        int x,
        int z,
        float yaw,
        Vector3 scaleMultiplier,
        Vector3 worldOffset)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            context.MarkMissing(prefabPath);
            return null;
        }

        NoryangjinMapToolPalettePlacementEntry placement = context.GetPlacement(prefabPath);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(prefab, parent);

        Vector2 positionOffset = placement.positionOffset;
        instance.name = instanceName;
        instance.transform.position = NoryangjinMapToolWindow.BuildPalettePlacementPosition(
            Origin,
            x,
            z,
            NoryangjinMapToolWindow.BuildPlacementSnapCellSize(NoryangjinMapToolWindow.DefaultCellSize, false),
            PlacementHeight,
            placement.heightOffset,
            positionOffset) + worldOffset;
        instance.transform.rotation = NoryangjinMapToolWindow.BuildPalettePlacementRotation(
            prefab.transform.rotation,
            placement.yawOffset + yaw);
        instance.transform.localScale = NoryangjinMapToolWindow.BuildPalettePlacementScale(
            prefab.transform.localScale,
            Vector3.Scale(placement.scale, scaleMultiplier));

        context.Placed++;
        EditorUtility.SetDirty(instance);
        return instance;
    }

    private static Vector2Int SideOffset(float yaw, int distance, bool right)
    {
        int normalized = Mathf.RoundToInt(Mathf.Repeat(yaw, 360f));
        return normalized switch
        {
            90 => right ? new Vector2Int(0, -distance) : new Vector2Int(0, distance),
            180 => right ? new Vector2Int(-distance, 0) : new Vector2Int(distance, 0),
            270 => right ? new Vector2Int(0, distance) : new Vector2Int(0, -distance),
            _ => right ? new Vector2Int(distance, 0) : new Vector2Int(-distance, 0)
        };
    }

    private static void SetupWorkScene(Transform root)
    {
        EnsureChild(root, RoadParentName);
        EnsureChild(root, PropParentName);
        EnsureChild(root, WaterParentName);
        EnsureWorkFloor(root);
        EnsureWorkGrid(root);
        EnsureOriginPost(root);
    }

    private static void EnsureWorkFloor(Transform root)
    {
        Transform floor = root.Find(WorkFloorName);
        if (floor == null)
        {
            GameObject floorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorObject.name = WorkFloorName;
            floorObject.transform.SetParent(root, false);
            floor = floorObject.transform;
        }

        float size = NoryangjinMapToolWindow.DefaultCellSize * NoryangjinMapToolWindow.WorkGridExtent * 2f;
        floor.localPosition = new Vector3(0f, -0.08f, 0f);
        floor.localScale = new Vector3(size, 0.02f, size);
    }

    private static void EnsureWorkGrid(Transform root)
    {
        Transform grid = root.Find(WorkGridName);
        if (grid != null)
            return;

        grid = new GameObject(WorkGridName).transform;
        grid.SetParent(root, false);

        Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
        material.color = new Color(0.05f, 0.06f, 0.07f, 0.72f);

        float cell = NoryangjinMapToolWindow.DefaultCellSize;
        int extent = NoryangjinMapToolWindow.WorkGridExtent;
        float span = cell * extent * 2f;
        for (int i = -extent; i <= extent; i++)
        {
            float offset = i * cell;
            CreateGridLine(grid, $"ConceptGrid_X_{i:+00;-00;+00}", new Vector3(offset, NoryangjinMapToolWindow.WorkGridLineY, 0f), new Vector3(0.018f, 0.004f, span), material);
            CreateGridLine(grid, $"ConceptGrid_Z_{i:+00;-00;+00}", new Vector3(0f, NoryangjinMapToolWindow.WorkGridLineY, offset), new Vector3(span, 0.004f, 0.018f), material);
        }
    }

    private static void CreateGridLine(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.SetParent(parent, false);
        line.transform.localPosition = position;
        line.transform.localScale = scale;

        Renderer renderer = line.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private static void EnsureOriginPost(Transform root)
    {
        Transform post = root.Find(OriginPostName);
        if (post == null)
        {
            GameObject postObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postObject.name = OriginPostName;
            postObject.transform.SetParent(root, false);
            post = postObject.transform;
        }

        post.localPosition = new Vector3(0f, 0.35f, 0f);
        post.localScale = new Vector3(0.16f, 0.35f, 0.16f);
    }

    private static void SetupCameraAndLight(List<RouteNode> route)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("MapTool_Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
        }

        camera.gameObject.name = "MapTool_Camera";
        Bounds bounds = BuildRouteBounds(route);
        Vector3 center = bounds.center;

        camera.transform.position = center + new Vector3(0f, 36f, -28f);
        camera.transform.LookAt(center);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(22f, bounds.size.z * 0.52f);

        Light light = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (light == null)
        {
            GameObject lightObject = new GameObject("MapTool_DirectionalLight");
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        light.gameObject.name = "MapTool_DirectionalLight";
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.intensity = 1.25f;
    }

    private static Bounds BuildRouteBounds(List<RouteNode> route)
    {
        float cellSize = NoryangjinMapToolWindow.BuildPlacementSnapCellSize(NoryangjinMapToolWindow.DefaultCellSize, false);
        Bounds bounds = new Bounds(NoryangjinMapToolGridUtility.GridToWorld(Origin, route[0].X, route[0].Z, cellSize, 0f), Vector3.one);
        foreach (RouteNode node in route)
            bounds.Encapsulate(NoryangjinMapToolGridUtility.GridToWorld(Origin, node.X, node.Z, cellSize, 0f));

        bounds.Expand(new Vector3(22f, 0f, 12f));
        return bounds;
    }

    private static GameObject EnsureRoot()
    {
        GameObject root = GameObject.Find(RootName);
        if (root != null)
            return root;

        root = new GameObject(RootName);
        EditorUtility.SetDirty(root);
        return root;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        EditorUtility.SetDirty(childObject);
        return childObject.transform;
    }

    private static void ClearGeneratedChildren(Transform parent, string namePrefix)
    {
        var targets = new List<GameObject>();
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(namePrefix, StringComparison.Ordinal))
                targets.Add(child.gameObject);
        }

        foreach (GameObject target in targets)
            UnityEngine.Object.DestroyImmediate(target);
    }

    private static NoryangjinMapToolPaletteDefaults LoadPaletteDefaults()
    {
        return AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(PaletteDefaultsPath);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folder);
    }

    private static void WriteReport(BuildContext context, int routeCount)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        using var writer = new StreamWriter(ReportPath);
        writer.WriteLine("Noryangjin map-tool concept layout");
        writer.WriteLine($"Scene: {ScenePath}");
        writer.WriteLine($"Route nodes: {routeCount}");
        writer.WriteLine($"Placed objects: {context.Placed}");
        writer.WriteLine($"Missing prefabs: {context.Missing.Count}");
        foreach (string missing in context.Missing)
            writer.WriteLine("- " + missing);
    }

    private readonly struct RouteNode
    {
        public RouteNode(int x, int z, float yaw, string prefabPath, string label)
        {
            X = x;
            Z = z;
            Yaw = yaw;
            PrefabPath = prefabPath;
            Label = label;
        }

        public int X { get; }
        public int Z { get; }
        public float Yaw { get; }
        public string PrefabPath { get; }
        public string Label { get; }
    }

    private sealed class BuildContext
    {
        private readonly NoryangjinMapToolPaletteDefaults defaults;
        private readonly HashSet<string> missing = new(StringComparer.Ordinal);

        public BuildContext(NoryangjinMapToolPaletteDefaults defaults)
        {
            this.defaults = defaults;
        }

        public int Placed { get; set; }
        public IReadOnlyCollection<string> Missing => missing;

        public NoryangjinMapToolPalettePlacementEntry GetPlacement(string prefabPath)
        {
            return defaults != null
                ? defaults.GetOrCreateEntry(prefabPath)
                : NoryangjinMapToolPalettePlacementEntry.CreateDefault(prefabPath);
        }

        public void MarkMissing(string prefabPath)
        {
            missing.Add(prefabPath);
        }
    }
}
#endif
