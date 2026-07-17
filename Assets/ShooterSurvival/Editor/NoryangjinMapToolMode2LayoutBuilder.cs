#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NoryangjinMapToolMode2LayoutBuilder
{
    internal const string TargetScenePath = NoryangjinMapToolWindow.MapToolScene2Path;

    internal const string PaletteDefaultsPath = "Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset";
    private const string ReportPath = "Temp/NoryangjinMapToolMode2LayoutReport.txt";
    private const string PreviewPath = "Temp/NoryangjinMapToolMode2TopPreview.png";
    private const string ThreeQuarterPreviewPath = "Temp/NoryangjinMapToolMode2ThreeQuarterPreview.png";
    private const string RootName = "Noryangjin_MapTool";
    private const string RoadParentName = "Roads";
    private const string PropParentName = "Props";
    private const string GeneratedNamePrefix = "Prop_Layout2_";
    private const int ExpectedRoadCount = 19;

    private const string StagePrefabRoot = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin";
    private const string BasicRoadPrefabPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy.prefab";
    private const string LeftTurnRoadPrefabPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy_LeftTurn.prefab";
    private const string RightTurnRoadPrefabPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy_RightTurn.prefab";

    private static readonly string BlueCratePath = StagePrefab("001_STAGE01_NRY_PROPS_001_Blue_fish_crate");
    private static readonly string StyrofoamBoxPath = StagePrefab("002_STAGE01_NRY_PROPS_002_Styrofoam_fish_box");
    private static readonly string IceFishTankPath = StagePrefab("003_STAGE01_NRY_PROPS_003_Ice_fish_tank");
    private static readonly string PushCartPath = StagePrefab("004_STAGE01_NRY_OBSTACLE_004_Seafood_push_cart");
    private static readonly string SafetyConePath = StagePrefab("005_STAGE01_NRY_OBSTACLE_005_Wet_floor_safety_cone");
    private static readonly string BuoyRopePostPath = StagePrefab("006_STAGE01_NRY_PROPS_006_Buoy_with_rope_post");
    private static readonly string LampSignPath = StagePrefab("007_STAGE01_NRY_PROPS_007_Fish_market_lamp_sign");
    private static readonly string SeagullPerchPath = StagePrefab("008_STAGE01_NRY_PROPS_008_Seagull_perch_post");
    private static readonly string DockRailingPath = StagePrefab("011_STAGE01_NRY_BND_001_Dock_railing_module");
    private static readonly string RopeBarrierPath = StagePrefab("012_STAGE01_NRY_BND_002_Rope_post_barrier");
    private static readonly string SeawallCurbPath = StagePrefab("013_STAGE01_NRY_BND_003_Concrete_seawall_curb");
    private static readonly string StorefrontPath = StagePrefab("014_STAGE01_NRY_BLD_001_Fish_market_storefront_facade");
    private static readonly string SashimiStallPath = StagePrefab("015_STAGE01_NRY_BLD_002_Sashimi_restaurant_stall_front");
    private static readonly string SeafoodStallPath = StagePrefab("016_STAGE01_NRY_BLD_003_Seafood_display_stall_module");
    private static readonly string OceanWaterPath = StagePrefab("017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop");
    private static readonly string FishBoxStackPath = StagePrefab("020_STAGE01_NRY_DCR_001_Fish_box_stack");
    private static readonly string AquariumRowPath = StagePrefab("021_STAGE01_NRY_DCR_002_Aquarium_tank_row");
    private static readonly string IceBoxStackPath = StagePrefab("022_STAGE01_NRY_DCR_003_Ice_box_stack");
    private static readonly string DockPilingPath = StagePrefab("023_STAGE01_NRY_PROPS_011_Standalone_dock_piling");
    private static readonly string LifeRingPath = StagePrefab("024_STAGE01_NRY_PROPS_012_Life_ring_buoy");
    private static readonly string TireFenderPath = StagePrefab("025_STAGE01_NRY_PROPS_013_Boat_tire_fender");
    private static readonly string RedLampPath = StagePrefab("027_STAGE01_NRY_DCR_016_Red_market_hanging_lamp");
    private static readonly string CrabSignPath = StagePrefab("028_STAGE01_NRY_PROPS_017_Crab_mascot_sign");
    private static readonly string FishSignPath = StagePrefab("029_STAGE01_NRY_PROPS_018_Fish_mascot_sign");
    private static readonly string CrabTankPath = StagePrefab("030_STAGE01_NRY_PROPS_019_Crab_aquarium_tank");
    private static readonly string OctopusTankPath = StagePrefab("031_STAGE01_NRY_PROPS_020_Octopus_aquarium_tank");
    private static readonly string OrangeCratePath = StagePrefab("032_STAGE01_NRY_PROPS_021_Orange_fish_crate_variant");
    private static readonly string WhiteCratePath = StagePrefab("033_STAGE01_NRY_PROPS_022_White_fish_crate_variant");
    private static readonly string NetPilePath = StagePrefab("034_STAGE01_NRY_DCR_023_Fishing_net_pile");
    private static readonly string AnchorPath = StagePrefab("035_STAGE01_NRY_PROPS_024_Anchor_prop");
    private static readonly string UtilityPolePath = StagePrefab("036_STAGE01_NRY_DCR_025_Harbor_utility_pole");
    private static readonly string SeaBuoyPath = StagePrefab("037_STAGE01_NRY_BG_026_Floating_sea_buoy");
    private static readonly string FloatingPlankPath = StagePrefab("038_STAGE01_NRY_BG_027_Floating_wooden_plank");
    private static readonly string IceScatterPath = StagePrefab("039_STAGE01_NRY_DCR_029_Ice_chunk_floor_scatter");
    private static readonly string FishScrapPath = StagePrefab("040_STAGE01_NRY_DCR_030_Fish_scrap_floor_scatter");
    private static readonly string FlyingSeagullPath = StagePrefab("041_STAGE01_NRY_DCR_031_Flying_seagull_silhouette");
    private static readonly string AwningPath = StagePrefab("043_STAGE01_NRY_BLD_034_Market_awning_color_variant_set");
    private static readonly string BarricadePath = StagePrefab("044_STAGE01_NRY_OBSTACLE_036_Fish-market_wooden_X_barricade");
    private static readonly string SignalGantryPath = StagePrefab("045_STAGE01_NRY_GAMEPLAY_037_Harbor_lane_signal_gantry");
    private static readonly string RainyGroundPath = StagePrefab("049_STAGE01_NRY_BG_033_Noryangjin_rainy_market_ground_backdrop");

    private static readonly Rect LowerLane = Rect.MinMaxRect(-12f, -117.3f, 7f, -112.8f);
    private static readonly Rect VerticalLane = Rect.MinMaxRect(-13.2f, -111f, -8.7f, -10f);
    private static readonly Rect UpperLane = Rect.MinMaxRect(-13f, -9.43f, 76f, -4.93f);

    private static readonly RoadSkeletonSpec[] ExpectedRoadSkeleton =
    {
        new("Road_Basic_X-62_Z-397", new Vector3(-7.45006466f, 0f, -78.0250854f), 0f, BasicRoadPrefabPath),
        new("Road_Basic_X-62_Z-447", new Vector3(-7.45006466f, 0f, -89.2750854f), 0f, BasicRoadPrefabPath),
        new("Road_Basic_X-62_Z-497", new Vector3(-7.45006466f, 0f, -100.525085f), 0f, BasicRoadPrefabPath),
        new("Road_Basic_X-63_Z-146", new Vector3(-7.675064f, 0f, -21.550087f), 0f, BasicRoadPrefabPath),
        new("Road_Basic_X-63_Z-196", new Vector3(-7.675064f, 0f, -32.800087f), 0f, BasicRoadPrefabPath),
        new("Road_Basic_X-63_Z-246", new Vector3(-7.675064f, 0f, -44.050087f), 0f, BasicRoadPrefabPath),
        new("Road_Basic_X-63_Z-297", new Vector3(-7.675064f, 0f, -55.5250854f), 0f, BasicRoadPrefabPath),
        new("Road_Basic_X-63_Z-347", new Vector3(-7.675064f, 0f, -66.7750854f), 0f, BasicRoadPrefabPath),
        new("Road_Basic_X-63_Z-96", new Vector3(-7.675064f, 0f, -10.300087f), 0f, BasicRoadPrefabPath),
        new("Road_LeftTurn_X-12_Z-527", new Vector3(-2.99999952f, 0f, -111.775055f), 270f, LeftTurnRoadPrefabPath),
        new("Road_LeftTurn_X-62_Z-527", new Vector3(-14.250001f, 0f, -111.775055f), 270f, LeftTurnRoadPrefabPath),
        new("Road_RightTurn_X-12_Z-46", new Vector3(8.399987f, 0f, -10.4500055f), 90f, RightTurnRoadPrefabPath),
        new("Road_RightTurn_X-62_Z-46", new Vector3(-2.85001278f, 0f, -10.4500055f), 90f, RightTurnRoadPrefabPath),
        new("Road_RightTurn_X+138_Z-46", new Vector3(42.1499863f, 0f, -10.4500055f), 90f, RightTurnRoadPrefabPath),
        new("Road_RightTurn_X+188_Z-46", new Vector3(53.3999863f, 0f, -10.4500055f), 90f, RightTurnRoadPrefabPath),
        new("Road_RightTurn_X+238_Z-46", new Vector3(64.64999f, 0f, -10.4500055f), 90f, RightTurnRoadPrefabPath),
        new("Road_RightTurn_X+288_Z-46", new Vector3(75.89998f, 0f, -10.4500055f), 90f, RightTurnRoadPrefabPath),
        new("Road_RightTurn_X+38_Z-46", new Vector3(19.6499863f, 0f, -10.4500055f), 90f, RightTurnRoadPrefabPath),
        new("Road_RightTurn_X+88_Z-46", new Vector3(30.8999863f, 0f, -10.4500055f), 90f, RightTurnRoadPrefabPath)
    };

    [MenuItem("Tools/MeshyAI/Build Noryangjin MapTool Mode 2 Layout", false, 2313)]
    public static void BuildLayout()
    {
        BuildLayout(SceneManager.GetActiveScene());
    }

    internal static void BuildLayout(Scene scene)
    {
        if (!CanBuildScenePath(scene.path))
        {
            throw new InvalidOperationException(
                $"Mode 2 layout can only be built in '{TargetScenePath}'. Active scene: '{scene.path}'.");
        }

        GameObject root = FindMapToolRoot(scene);
        Transform roads = root != null ? root.transform.Find(RoadParentName) : null;
        Transform props = root != null ? root.transform.Find(PropParentName) : null;
        if (root == null || roads == null || props == null)
            throw new InvalidOperationException("The active Mode 2 scene is missing its map-tool root, Roads, or Props container.");

        if (roads.gameObject.scene.handle != scene.handle || props.gameObject.scene.handle != scene.handle)
            throw new InvalidOperationException("The Mode 2 Roads and Props containers must belong to the active target scene.");

        ValidateRoadSkeleton(roads);

        NoryangjinMapToolPaletteDefaults paletteDefaults =
            AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(PaletteDefaultsPath);
        if (paletteDefaults == null)
            throw new InvalidOperationException($"Missing palette defaults at '{PaletteDefaultsPath}'.");

        IReadOnlyList<PlacementSpec> specs = BuildPlacementSpecs();
        IReadOnlyList<string> requiredPrefabPaths = BuildRequiredPrefabPaths(specs);
        PreflightPrefabs(requiredPrefabPaths);
        PreflightPlacementDefaults(paletteDefaults, specs, requiredPrefabPaths);
        ClearVerificationArtifacts();

        ClearGeneratedChildren(props);
        RepositionCopiedProps(props, paletteDefaults);

        var context = new BuildContext(paletteDefaults);
        foreach (PlacementSpec spec in specs)
            PlacePrefab(context, props, spec);

        ValidateGeneratedLaneClearance(context);
        if (context.Warnings.Count > 0)
        {
            throw new InvalidOperationException(
                $"Mode 2 layout has {context.Warnings.Count} clear-lane overlap(s); the scene was not saved. " +
                string.Join(" | ", context.Warnings));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, TargetScenePath))
            throw new IOException($"Unity failed to save '{TargetScenePath}'.");

        string previewStatus = CapturePreviewsSafely();
        WriteReport(context, roads.childCount, props.childCount, previewStatus);
        Debug.Log(
            $"[MeshyAI] Noryangjin MapTool Mode 2 layout built. " +
            $"Placed {context.PlacedObjects.Count} generated props with {context.Warnings.Count} lane warnings. " +
            $"Previews: {previewStatus}");
    }

    internal static bool CanBuildScenePath(string scenePath)
    {
        return string.Equals(scenePath, TargetScenePath, StringComparison.OrdinalIgnoreCase);
    }

    internal static IReadOnlyList<RoadSkeletonSpec> BuildRoadSkeletonSpecs()
    {
        return ExpectedRoadSkeleton;
    }

    internal static IReadOnlyList<string> BuildRequiredPrefabPaths()
    {
        return BuildRequiredPrefabPaths(BuildPlacementSpecs());
    }

    internal static IReadOnlyList<PlacementSpec> BuildPlacementSpecs()
    {
        var specs = new List<PlacementSpec>();

        void Add(
            string prefabPath,
            string label,
            float rootX,
            float rootZ,
            float yaw = 0f,
            float height = 0f,
            float scale = 1f,
            bool allowLaneOverlap = false)
        {
            specs.Add(new PlacementSpec(
                prefabPath,
                label,
                new Vector3(rootX, height, rootZ),
                yaw,
                Vector3.one * scale,
                allowLaneOverlap));
        }

        // Background under the currently sparse eastbound pier.
        Add(RainyGroundPath, "Background_Ground_00", 13.7f, -20.1f, allowLaneOverlap: true);
        Add(RainyGroundPath, "Background_Ground_01", 24.95f, -20.1f, allowLaneOverlap: true);
        Add(RainyGroundPath, "Background_Ground_02", 36.2f, -20.1f, allowLaneOverlap: true);
        Add(RainyGroundPath, "Background_Ground_03", 47.45f, -20.1f, allowLaneOverlap: true);
        Add(RainyGroundPath, "Background_Ground_04", 58.7f, -20.1f, allowLaneOverlap: true);
        Add(RainyGroundPath, "Background_Ground_05", 69.95f, -20.1f, allowLaneOverlap: true);
        Add(OceanWaterPath, "Background_Water_00", 82.725f, 3.075f, 270f, allowLaneOverlap: true);
        Add(OceanWaterPath, "Background_Water_01", 82.725f, 16.35f, 90f, allowLaneOverlap: true);

        float[] interiorWaterX = { 12.525f, 25.8f, 39.075f, 52.35f, 65.625f, 78.9f };
        float[] interiorWaterZ = { -121.575f, -101.325f, -81.075f, -60.825f, -40.575f, -20.325f };
        int interiorWaterIndex = 0;
        foreach (float waterX in interiorWaterX)
        {
            foreach (float waterZ in interiorWaterZ)
            {
                // The copied scene already has these three tiles at the lower-left edge of the basin.
                if (Mathf.Approximately(waterX, 12.525f) && waterZ <= -81.075f)
                    continue;

                Add(
                    OceanWaterPath,
                    $"Background_InteriorWater_{interiorWaterIndex++:00}",
                    waterX,
                    waterZ,
                    allowLaneOverlap: true);
            }
        }

        // North edge of the long upper pier.
        float[] upperRailX = { -8.8f, 2.45f, 13.7f, 24.95f, 36.2f, 47.45f, 58.7f, 69.95f };
        for (int i = 0; i < upperRailX.Length; i++)
            Add(DockRailingPath, $"Upper_Railing_{i:00}", upperRailX[i], -2.65f, 90f);

        // Harbor boundaries around the copied lower and vertical route.
        Add(DockRailingPath, "Vertical_West_Railing_00", -16.2f, -101f);
        Add(RopeBarrierPath, "Vertical_West_Rope_01", -16.2f, -90f);
        Add(DockRailingPath, "Vertical_West_Railing_02", -16.2f, -79f);
        Add(RopeBarrierPath, "Vertical_East_Rope_00", -5.6f, -101f, 180f);
        Add(DockRailingPath, "Vertical_East_Railing_01", -5.6f, -90f, 180f);
        Add(RopeBarrierPath, "Vertical_East_Rope_02", -5.6f, -79f, 180f);
        Add(RopeBarrierPath, "Start_South_Rope_00", -12f, -121f, 90f);
        Add(RopeBarrierPath, "Start_South_Rope_01", -6f, -121f, 90f);
        Add(RopeBarrierPath, "Start_South_Rope_02", 0f, -121f, 90f);
        Add(SeawallCurbPath, "Finish_East_Curb_00", 77.8f, -10.8f);
        Add(SeawallCurbPath, "Finish_East_Curb_01", 77.8f, -3.5f);

        // South-facing market frontage along the previously empty upper arm.
        Add(StorefrontPath, "Upper_Market_Storefront_00", 13.7f, -17.5f, 90f);
        Add(SashimiStallPath, "Upper_Market_Sashimi_01", 24.95f, -17.5f, 90f);
        Add(SeafoodStallPath, "Upper_Market_Seafood_02", 36.2f, -17.5f, 90f);
        Add(AwningPath, "Upper_Market_Awning_03", 47.45f, -17.5f, 90f);
        Add(StorefrontPath, "Upper_Market_Storefront_04", 58.7f, -17.5f, 90f);
        Add(AquariumRowPath, "Upper_Market_Aquarium_05", 69.95f, -14.5f, 90f);

        Add(BlueCratePath, "Upper_Display_BlueCrate_00", 17f, -12.8f, 15f);
        Add(WhiteCratePath, "Upper_Display_WhiteCrate_01", 20f, -12.8f, -12f);
        Add(IceBoxStackPath, "Upper_Display_IceBox_02", 29f, -13.2f, 90f);
        Add(CrabTankPath, "Upper_Display_CrabTank_03", 41f, -13.8f, 90f);
        Add(OctopusTankPath, "Upper_Display_OctopusTank_04", 52f, -13.8f, 90f);
        Add(FishBoxStackPath, "Upper_Display_FishBoxes_05", 61f, -13f, 90f);
        Add(PushCartPath, "Upper_Display_Cart_06", 72f, -13.5f, 90f);
        Add(LampSignPath, "Upper_Sign_Lamp_00", 16f, -12f, 90f);
        Add(CrabSignPath, "Upper_Sign_Crab_01", 34f, -12.5f, 90f);
        Add(LampSignPath, "Upper_Sign_Lamp_02", 55f, -12f, 90f);
        Add(FishSignPath, "Upper_Sign_Fish_03", 58f, -12.5f, 90f);
        Add(RedLampPath, "Upper_Lamp_00", 27f, -14f, 90f, 2.5f, allowLaneOverlap: true);
        Add(RedLampPath, "Upper_Lamp_01", 49f, -14f, 90f, 2.5f, allowLaneOverlap: true);
        Add(RedLampPath, "Upper_Lamp_02", 65f, -14f, 90f, 2.5f, allowLaneOverlap: true);

        // Extra shoulder dressing around the existing six-building vertical market.
        Add(FishBoxStackPath, "Vertical_Left_FishBoxes_00", -16f, -71f, 90f);
        Add(IceFishTankPath, "Vertical_Right_IceTank_00", -5.7f, -71f, 270f);
        Add(CrabSignPath, "Vertical_Left_CrabSign_01", -16f, -59f, 90f);
        Add(LampSignPath, "Vertical_Left_LampSign_01", -15.5f, -56.5f, 90f);
        Add(IceBoxStackPath, "Vertical_Right_IceBoxes_01", -5.7f, -59f, 270f);
        Add(PushCartPath, "Vertical_Left_Cart_02", -16f, -47f, 90f);
        Add(NetPilePath, "Vertical_Left_Net_02", -15.8f, -43.5f, 35f);
        Add(FishSignPath, "Vertical_Right_FishSign_02", -5.7f, -47f, 270f);
        Add(LampSignPath, "Vertical_Right_LampSign_02", -5.5f, -44f, 270f);
        Add(CrabTankPath, "Vertical_Left_CrabTank_03", -16f, -35f, 90f);
        Add(OctopusTankPath, "Vertical_Right_OctopusTank_03", -5.7f, -35f, 270f);
        Add(UtilityPolePath, "Vertical_UtilityPole_00", -16.5f, -73f);
        Add(UtilityPolePath, "Vertical_UtilityPole_01", -5f, -56f);
        Add(UtilityPolePath, "Vertical_UtilityPole_02", -16.5f, -31f);
        Add(SafetyConePath, "Vertical_Cone_00", -15.3f, -37f, 20f);
        Add(SafetyConePath, "Vertical_Cone_01", -6.6f, -28f, -15f);
        Add(SafetyConePath, "Vertical_Cone_02", -15.2f, -16f, 10f);
        Add(RedLampPath, "Vertical_Lamp_00", -15f, -63f, 90f, 2.5f, allowLaneOverlap: true);
        Add(RedLampPath, "Vertical_Lamp_01", -6.3f, -52f, 270f, 2.5f, allowLaneOverlap: true);
        Add(RedLampPath, "Vertical_Lamp_02", -15f, -41f, 90f, 2.5f, allowLaneOverlap: true);
        Add(RedLampPath, "Vertical_Lamp_03", -6.3f, -30f, 270f, 2.5f, allowLaneOverlap: true);

        // Lower start pocket and harbor logistics.
        Add(BlueCratePath, "Start_BlueCrate_00", -16f, -119f, 10f);
        Add(WhiteCratePath, "Start_WhiteCrate_01", -12f, -120f, -10f);
        Add(StyrofoamBoxPath, "Start_Styrofoam_02", -8f, -120f, 8f);
        Add(OrangeCratePath, "Start_OrangeCrate_03", -4f, -120f, -15f);
        Add(NetPilePath, "Start_NetPile_04", -16f, -113f, 25f);
        Add(FishBoxStackPath, "Start_FishBoxes_05", -1f, -122f, 90f);
        Add(SafetyConePath, "Start_Cone_06", 4f, -120f, -20f);
        Add(DockPilingPath, "Start_Piling_07", -16f, -108f);
        Add(TireFenderPath, "Start_Tire_08", -15f, -105f, 20f);
        Add(LifeRingPath, "Start_LifeRing_09", 0f, -109f, -15f);
        Add(BuoyRopePostPath, "Start_BuoyPost_10", 7f, -108f, 90f);
        Add(FishScrapPath, "Start_FishScrap_11", -3f, -116f, 30f, allowLaneOverlap: true);
        Add(IceScatterPath, "Start_IceScatter_12", 2f, -117f, -20f, allowLaneOverlap: true);

        // Water-side accents, direction markers, and finish dressing.
        Add(LifeRingPath, "Upper_Water_LifeRing_00", 36f, -2.2f, 15f);
        Add(TireFenderPath, "Upper_Water_Tire_01", 58f, -2.2f, -12f);
        Add(BuoyRopePostPath, "Upper_Water_BuoyPost_02", 74f, -2.4f, 90f);
        Add(SeaBuoyPath, "Upper_Water_SeaBuoy_03", 83f, 5f, 20f, allowLaneOverlap: true);
        Add(FloatingPlankPath, "Upper_Water_Plank_04", 80f, 14f, -18f, allowLaneOverlap: true);
        Add(FlyingSeagullPath, "Atmosphere_Seagull_00", 10f, 8f, 20f, 6f, 1.15f, true);
        Add(FlyingSeagullPath, "Atmosphere_Seagull_01", 48f, 10f, -15f, 7f, 1.2f, true);
        Add(SignalGantryPath, "Direction_Gantry_Vertical", -10.95f, -24f, 0f, allowLaneOverlap: true);
        Add(SignalGantryPath, "Direction_Gantry_Finish", 68f, -7.18f, 90f, allowLaneOverlap: true);
        Add(BarricadePath, "Finish_Barricade_Shoulder", 67.5f, -12.8f, 90f);
        Add(FishBoxStackPath, "Finish_FishBoxes", 77f, -13f, 90f);
        Add(SafetyConePath, "Finish_Cone", 75f, -11.8f, -10f);

        return specs;
    }

    private static string StagePrefab(string assetName)
    {
        return $"{StagePrefabRoot}/{assetName}/{assetName}.prefab";
    }

    internal static GameObject FindMapToolRoot(Scene scene)
    {
        GameObject[] matches = scene
            .GetRootGameObjects()
            .Where(candidate => string.Equals(candidate.name, RootName, StringComparison.Ordinal))
            .ToArray();

        if (matches.Length > 1)
            throw new InvalidOperationException($"The active Mode 2 scene contains {matches.Length} '{RootName}' roots.");

        return matches.SingleOrDefault();
    }

    internal static void ValidateRoadSkeleton(Transform roads)
    {
        if (roads == null)
            throw new ArgumentNullException(nameof(roads));

        if (roads.childCount != ExpectedRoadCount)
        {
            throw new InvalidOperationException(
                $"Expected {ExpectedRoadCount} copied road modules, found {roads.childCount}. No changes were made.");
        }

        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (Transform road in roads)
        {
            RoadSkeletonSpec expected = ExpectedRoadSkeleton.FirstOrDefault(
                spec => string.Equals(spec.Name, road.name, StringComparison.Ordinal));
            if (expected.Name == null)
                throw new InvalidOperationException($"Unexpected road '{road.name}'. No changes were made.");

            if (!seenNames.Add(road.name))
                throw new InvalidOperationException($"Duplicate road '{road.name}'. No changes were made.");

            bool prefabMatches =
                PrefabUtility.GetPrefabInstanceStatus(road.gameObject) == PrefabInstanceStatus.Connected &&
                string.Equals(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(road.gameObject),
                    expected.PrefabPath,
                    StringComparison.Ordinal);
            bool positionMatches = Vector3.SqrMagnitude(road.localPosition - expected.LocalPosition) <= 0.0001f;
            bool rotationMatches = Quaternion.Angle(road.localRotation, Quaternion.Euler(0f, expected.Yaw, 0f)) <= 0.1f;
            bool scaleMatches = Vector3.SqrMagnitude(road.localScale - expected.LocalScale) <= 0.0001f;
            if (!prefabMatches || !positionMatches || !rotationMatches || !scaleMatches)
            {
                throw new InvalidOperationException(
                    $"Road '{road.name}' no longer matches the copied Mode 2 route skeleton. No changes were made.");
            }
        }
    }

    private static IReadOnlyList<string> BuildRequiredPrefabPaths(IReadOnlyList<PlacementSpec> specs)
    {
        return specs
            .Select(spec => spec.PrefabPath)
            .Concat(new[] { AnchorPath, SeagullPerchPath })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static void PreflightPrefabs(IReadOnlyList<string> requiredPrefabPaths)
    {
        string[] missing = requiredPrefabPaths
            .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            .ToArray();

        if (missing.Length > 0)
            throw new FileNotFoundException("Missing Mode 2 layout prefabs:\n" + string.Join("\n", missing));
    }

    private static void PreflightPlacementDefaults(
        NoryangjinMapToolPaletteDefaults paletteDefaults,
        IReadOnlyList<PlacementSpec> specs,
        IReadOnlyList<string> requiredPrefabPaths)
    {
        foreach (string prefabPath in requiredPrefabPaths)
        {
            NoryangjinMapToolPalettePlacementEntry placement = paletteDefaults.GetOrCreateEntry(prefabPath);
            ValidateFinite(prefabPath, "position offset", placement.positionOffset);
            ValidateFinite(prefabPath, "yaw offset", placement.yawOffset);
            ValidateFinite(prefabPath, "height offset", placement.heightOffset);
            ValidateScale(prefabPath, placement.scale);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            ValidateScale(prefabPath, Vector3.Scale(prefab.transform.localScale, placement.scale));
        }

        foreach (PlacementSpec spec in specs)
        {
            ValidateFinite(spec.PrefabPath, "desired position", spec.DesiredRootPosition);
            ValidateFinite(spec.PrefabPath, "yaw", spec.Yaw);
            ValidateScale(spec.PrefabPath, spec.ScaleMultiplier);
        }
    }

    private static void ValidateFinite(string prefabPath, string valueName, float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new InvalidOperationException($"'{prefabPath}' has an invalid {valueName}: {value}.");
    }

    private static void ValidateFinite(string prefabPath, string valueName, Vector2 value)
    {
        ValidateFinite(prefabPath, valueName + ".x", value.x);
        ValidateFinite(prefabPath, valueName + ".y", value.y);
    }

    private static void ValidateFinite(string prefabPath, string valueName, Vector3 value)
    {
        ValidateFinite(prefabPath, valueName + ".x", value.x);
        ValidateFinite(prefabPath, valueName + ".y", value.y);
        ValidateFinite(prefabPath, valueName + ".z", value.z);
    }

    internal static void ValidateScale(string prefabPath, Vector3 scale)
    {
        ValidateFinite(prefabPath, "scale", scale);
        if (Mathf.Abs(scale.x) <= 0.0001f ||
            Mathf.Abs(scale.y) <= 0.0001f ||
            Mathf.Abs(scale.z) <= 0.0001f)
        {
            throw new InvalidOperationException($"'{prefabPath}' has a zero scale component: {scale}.");
        }
    }

    private static void ClearGeneratedChildren(Transform props)
    {
        var generated = new List<GameObject>();
        foreach (Transform child in props)
        {
            if (child.name.StartsWith(GeneratedNamePrefix, StringComparison.Ordinal))
                generated.Add(child.gameObject);
        }

        foreach (GameObject child in generated)
            UnityEngine.Object.DestroyImmediate(child);
    }

    private static void RepositionCopiedProps(
        Transform props,
        NoryangjinMapToolPaletteDefaults paletteDefaults)
    {
        Transform anchor = FindDirectChildStartingWith(props, "Prop_035_STAGE01_NRY_PROPS_024_Anchor_prop");
        if (anchor != null)
        {
            MoveExistingPrefab(
                anchor,
                paletteDefaults,
                AnchorPath,
                "035_STAGE01_NRY_PROPS_024_Anchor_prop_MovedForLayout2",
                new Vector3(-15.4f, 0f, -83.4f),
                338.9f);
        }

        Transform duplicateSeagull = FindDirectChildStartingWith(
            props,
            "Prop_008_STAGE01_NRY_PROPS_008_Seagull_perch_post_MovedForLayout2");
        if (duplicateSeagull == null)
            duplicateSeagull = props.Find("Prop_008_STAGE01_NRY_PROPS_008_Seagull_perch_post_X-64_Z-451 (1)");

        if (duplicateSeagull != null)
        {
            MoveExistingPrefab(
                duplicateSeagull,
                paletteDefaults,
                SeagullPerchPath,
                "008_STAGE01_NRY_PROPS_008_Seagull_perch_post_MovedForLayout2",
                new Vector3(29f, 2.705f, -2.7f),
                45f);
        }
    }

    private static Transform FindDirectChildStartingWith(Transform parent, string prefix)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                return child;
        }

        return null;
    }

    private static void MoveExistingPrefab(
        Transform target,
        NoryangjinMapToolPaletteDefaults paletteDefaults,
        string prefabPath,
        string variant,
        Vector3 desiredRootPosition,
        float yaw)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            throw new FileNotFoundException($"Missing copied-prop prefab '{prefabPath}'.");

        NoryangjinMapToolPalettePlacementEntry placement = paletteDefaults.GetOrCreateEntry(prefabPath);
        Vector2Int grid = RootPositionToGrid(desiredRootPosition, placement);
        target.name = NoryangjinMapToolGridUtility.BuildInstanceName("Prop", variant, grid.x, grid.y);
        target.position = BuildRootPosition(grid, placement, desiredRootPosition.y - placement.heightOffset);
        target.rotation = NoryangjinMapToolWindow.BuildPalettePlacementRotation(
            prefab.transform.rotation,
            placement.yawOffset + yaw);
        EditorUtility.SetDirty(target.gameObject);
    }

    private static void PlacePrefab(BuildContext context, Transform parent, PlacementSpec spec)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
        NoryangjinMapToolPalettePlacementEntry placement = context.PaletteDefaults.GetOrCreateEntry(spec.PrefabPath);
        Vector2Int grid = RootPositionToGrid(spec.DesiredRootPosition, placement);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(prefab, parent);

        instance.name = NoryangjinMapToolGridUtility.BuildInstanceName(
            "Prop",
            "Layout2_" + spec.Label,
            grid.x,
            grid.y);
        instance.transform.position = BuildRootPosition(grid, placement, spec.DesiredRootPosition.y);
        instance.transform.rotation = NoryangjinMapToolWindow.BuildPalettePlacementRotation(
            prefab.transform.rotation,
            placement.yawOffset + spec.Yaw);
        instance.transform.localScale = NoryangjinMapToolWindow.BuildPalettePlacementScale(
            prefab.transform.localScale,
            Vector3.Scale(placement.scale, spec.ScaleMultiplier));

        EditorUtility.SetDirty(instance);
        context.PlacedObjects.Add(new PlacedObject(instance, spec.AllowLaneOverlap));
    }

    internal static bool PlacementMatchesSpec(
        Transform instance,
        PlacementSpec spec,
        NoryangjinMapToolPaletteDefaults paletteDefaults)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
        if (instance == null || prefab == null || paletteDefaults == null)
            return false;

        NoryangjinMapToolPalettePlacementEntry placement = paletteDefaults.GetOrCreateEntry(spec.PrefabPath);
        Vector2Int grid = RootPositionToGrid(spec.DesiredRootPosition, placement);
        Vector3 expectedPosition = BuildRootPosition(grid, placement, spec.DesiredRootPosition.y);
        Quaternion expectedRotation = NoryangjinMapToolWindow.BuildPalettePlacementRotation(
            prefab.transform.rotation,
            placement.yawOffset + spec.Yaw);
        Vector3 expectedScale = NoryangjinMapToolWindow.BuildPalettePlacementScale(
            prefab.transform.localScale,
            Vector3.Scale(placement.scale, spec.ScaleMultiplier));

        return Vector3.SqrMagnitude(instance.position - expectedPosition) <= 0.0001f &&
               Quaternion.Angle(instance.rotation, expectedRotation) <= 0.1f &&
               Vector3.SqrMagnitude(instance.localScale - expectedScale) <= 0.0001f;
    }

    private static Vector2Int RootPositionToGrid(
        Vector3 desiredRootPosition,
        NoryangjinMapToolPalettePlacementEntry placement)
    {
        float cellSize = NoryangjinMapToolWindow.BuildPlacementSnapCellSize(
            NoryangjinMapToolWindow.DefaultCellSize,
            false);
        return new Vector2Int(
            Mathf.RoundToInt((desiredRootPosition.x - placement.positionOffset.x) / cellSize),
            Mathf.RoundToInt((desiredRootPosition.z - placement.positionOffset.y) / cellSize));
    }

    private static Vector3 BuildRootPosition(
        Vector2Int grid,
        NoryangjinMapToolPalettePlacementEntry placement,
        float additionalHeight)
    {
        float cellSize = NoryangjinMapToolWindow.BuildPlacementSnapCellSize(
            NoryangjinMapToolWindow.DefaultCellSize,
            false);
        return NoryangjinMapToolWindow.BuildPalettePlacementPosition(
            Vector3.zero,
            grid.x,
            grid.y,
            cellSize,
            additionalHeight,
            placement.heightOffset,
            placement.positionOffset);
    }

    private static void ValidateGeneratedLaneClearance(BuildContext context)
    {
        foreach (PlacedObject placed in context.PlacedObjects)
        {
            if (placed.AllowLaneOverlap)
                continue;

            foreach (Renderer renderer in placed.GameObject.GetComponentsInChildren<Renderer>(true))
            {
                Bounds bounds = renderer.bounds;
                if (!IntersectsLane(bounds))
                    continue;

                context.Warnings.Add($"{placed.GameObject.name} renderer overlaps a clear-lane envelope.");
                break;
            }
        }
    }

    internal static bool IntersectsLane(Bounds bounds)
    {
        return IntersectsXZ(bounds, LowerLane) ||
               IntersectsXZ(bounds, VerticalLane) ||
               IntersectsXZ(bounds, UpperLane);
    }

    private static bool IntersectsXZ(Bounds bounds, Rect lane)
    {
        return bounds.max.x > lane.xMin &&
               bounds.min.x < lane.xMax &&
               bounds.max.z > lane.yMin &&
               bounds.min.z < lane.yMax;
    }

    private static void ClearVerificationArtifacts()
    {
        DeleteArtifact(ReportPath);
        DeleteArtifact(PreviewPath);
        DeleteArtifact(ThreeQuarterPreviewPath);
    }

    private static void ClearPreviewArtifacts()
    {
        DeleteArtifact(PreviewPath);
        DeleteArtifact(ThreeQuarterPreviewPath);
    }

    private static void DeleteArtifact(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void WriteReport(
        BuildContext context,
        int roadCount,
        int propCount,
        string previewStatus)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        using var writer = new StreamWriter(ReportPath);
        writer.WriteLine("Noryangjin MapTool Mode 2 layout");
        writer.WriteLine($"Scene: {TargetScenePath}");
        writer.WriteLine($"Roads preserved: {roadCount}");
        writer.WriteLine($"Generated props: {context.PlacedObjects.Count}");
        writer.WriteLine($"Total direct Props children: {propCount}");
        writer.WriteLine($"Lane warnings: {context.Warnings.Count}");
        writer.WriteLine($"Preview capture: {previewStatus}");
        foreach (string warning in context.Warnings)
            writer.WriteLine("- " + warning);
    }

    private static string CapturePreviewsSafely()
    {
        try
        {
            CapturePreviews();
            return "Captured";
        }
        catch (Exception exception)
        {
            ClearPreviewArtifacts();
            Debug.LogWarning(
                $"[MeshyAI] Mode 2 scene was saved, but preview capture failed and old previews were removed: " +
                $"{exception.GetType().Name}: {exception.Message}");
            return $"Failed ({exception.GetType().Name})";
        }
    }

    private static void CapturePreviews()
    {
        Vector3 center = new Vector3(31f, 0f, -52f);
        CapturePreview(PreviewPath, new Vector3(31f, 180f, -52f), center, 1200, 1400, 90f);
        CapturePreview(ThreeQuarterPreviewPath, new Vector3(31f, 115f, -180f), center, 1400, 1000, 86f);
    }

    private static void CapturePreview(
        string outputPath,
        Vector3 cameraPosition,
        Vector3 lookAt,
        int width,
        int height,
        float orthographicSize)
    {
        GameObject cameraObject = new GameObject("Mode2_Temporary_Preview_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        RenderTexture renderTexture = null;
        Texture2D texture = null;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            camera.transform.position = cameraPosition;
            camera.transform.LookAt(lookAt);
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.aspect = width / (float)height;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.31f, 0.39f, 1f);

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousActive;
            camera.targetTexture = null;
            if (renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderTexture);
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    internal readonly struct PlacementSpec
    {
        public PlacementSpec(
            string prefabPath,
            string label,
            Vector3 desiredRootPosition,
            float yaw,
            Vector3 scaleMultiplier,
            bool allowLaneOverlap)
        {
            PrefabPath = prefabPath;
            Label = label;
            DesiredRootPosition = desiredRootPosition;
            Yaw = yaw;
            ScaleMultiplier = scaleMultiplier;
            AllowLaneOverlap = allowLaneOverlap;
        }

        public string PrefabPath { get; }
        public string Label { get; }
        public Vector3 DesiredRootPosition { get; }
        public float Yaw { get; }
        public Vector3 ScaleMultiplier { get; }
        public bool AllowLaneOverlap { get; }
    }

    internal readonly struct RoadSkeletonSpec
    {
        public RoadSkeletonSpec(string name, Vector3 localPosition, float yaw, string prefabPath)
        {
            Name = name;
            LocalPosition = localPosition;
            Yaw = yaw;
            PrefabPath = prefabPath;
            LocalScale = new Vector3(2.2f, 4f, 2.24f);
        }

        public string Name { get; }
        public Vector3 LocalPosition { get; }
        public float Yaw { get; }
        public string PrefabPath { get; }
        public Vector3 LocalScale { get; }
    }

    private readonly struct PlacedObject
    {
        public PlacedObject(GameObject gameObject, bool allowLaneOverlap)
        {
            GameObject = gameObject;
            AllowLaneOverlap = allowLaneOverlap;
        }

        public GameObject GameObject { get; }
        public bool AllowLaneOverlap { get; }
    }

    private sealed class BuildContext
    {
        public BuildContext(NoryangjinMapToolPaletteDefaults paletteDefaults)
        {
            PaletteDefaults = paletteDefaults;
        }

        public NoryangjinMapToolPaletteDefaults PaletteDefaults { get; }
        public List<PlacedObject> PlacedObjects { get; } = new();
        public List<string> Warnings { get; } = new();
    }
}
#endif
