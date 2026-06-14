#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage01NoryangjinSecondAutoDraftBuilder
{
    private const string StagePrefabRoot = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin";
    private const string ScenePath = "Assets/ShooterSurvival/Scenes/Generated/Stage01_2_Noryangjin_AutoDraft.unity";
    private const string RequestPath = "Temp/Stage01NoryangjinSecondAutoDraftRequest.txt";
    private const string ReportPath = "Temp/Stage01NoryangjinSecondAutoDraftReport.txt";
    private const string GeneratedMaterialRoot = "Assets/ShooterSurvival/Materials/Generated";
    private const string PreviewPath = "output/stage01_2_noryangjin_autodraft_preview.png";

    private const float RoadSpan = 4.35f;
    private const float DeckWidth = 6.4f;
    private const float RoadSideOffset = 3.05f;
    private const float PropSideOffset = 2.75f;
    private const float MarketSideOffset = 3.05f;
    private const float WaterSideOffset = 6.15f;

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
                BuildScene();
            }
            catch (System.Exception ex)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, "Failed: " + ex);
                Debug.LogException(ex);
            }
        };
    }

    [MenuItem("Tools/MeshyAI/Build Stage01_2 Noryangjin Auto Draft Scene", false, 2311)]
    public static void BuildScene()
    {
        EnsureFolder("Assets/ShooterSurvival/Scenes");
        EnsureFolder("Assets/ShooterSurvival/Scenes/Generated");

        var context = new BuildContext();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("Stage01_2_Noryangjin_WetMarketPier_ConceptDraft");
        Transform roads = CreateChild(root.transform, "01_Dense_Wet_Fish_Market_Pier");
        Transform boundaries = CreateChild(root.transform, "02_Tight_Rope_Post_Railings");
        Transform market = CreateChild(root.transform, "03_Fish_Market_Facades_Awnings");
        Transform props = CreateChild(root.transform, "04_Scale_Calibrated_Market_Props");
        Transform gameplay = CreateChild(root.transform, "05_Clear_Center_Gameplay");
        Transform background = CreateChild(root.transform, "06_Water_Boats_Background");
        Transform markers = CreateChild(root.transform, "00_Stage01_2_Build_Validation");

        List<RoadNode> nodes = BuildRoadPath(context, roads);

        PlaceBoundaries(context, nodes, boundaries);
        PlaceMarketFacades(context, nodes, market);
        PlaceDenseMarketProps(context, nodes, props);
        PlaceGameplay(context, nodes, gameplay);
        PlaceBackground(context, nodes, background);
        PlacePlayerPreview(context, nodes, gameplay);
        Camera runnerCamera = CreateLightingAndCamera(nodes);
        CaptureRunnerPreview(context, runnerCamera);
        CreateChild(markers, "Stage01_2_Stage01_Noryangjin_Source_Prefab_Set_Dressing");
        UpgradeStage01SecondGeneratedMaterials(context);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WriteReport(context, nodes.Count);
        Debug.Log($"[MeshyAI] Stage01_2 Noryangjin auto draft scene built: {ScenePath}. {context.Placed} objects placed, {context.Missing} missing.");
    }

    private static List<RoadNode> BuildRoadPath(BuildContext context, Transform parent)
    {
        var nodes = new List<RoadNode>();
        float[] pathX =
        {
            0f,
            0f,
            0f,
            0.35f,
            0.85f,
            1.05f,
            0.4f,
            -0.75f,
            -1.08f,
            -0.72f,
            -0.22f,
            0.08f
        };

        int nodeCount = pathX.Length;
        float pathLength = (nodeCount - 1) * RoadSpan;
        float deckLength = pathLength + RoadSpan * 1.8f;
        float centerZ = pathLength * 0.5f;

        Transform deckRoot = CreateChild(parent, "Stage01_2_Continuous_Concept_Pier");
        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 position = new Vector3(pathX[i], 0f, i * RoadSpan);
            float yaw = CalculatePathYaw(pathX, i);
            RoadNode node = new RoadNode(position, yaw);
            nodes.Add(node);
        }

        CreateContinuousPierDeck(context, deckRoot, nodes);
        PlaceRoadSurfaceSkins(context, deckRoot, nodes);

        CreateBoxColliderObject(
            context,
            deckRoot,
            "Stage01_2_Road_Playable_Collider",
            new Vector3(0f, 0.08f, centerZ),
            Quaternion.identity,
            new Vector3(DeckWidth + 1.25f, 0.22f, deckLength + 0.5f));

        return nodes;
    }

    private static void PlaceRoadSurfaceSkins(BuildContext context, Transform parent, List<RoadNode> nodes)
    {
        RoadNode start = nodes[0];
        CreateRoadSurfaceSkin(context, parent, "048", start.Position - Forward(start.Yaw) * 2.65f + Vector3.up * 0.035f, start.Yaw, "road_surface_skin_wide_foreground", 7.15f, 0.03f, 1.05f, 2.9f, 0.16f);

        for (int i = 0; i < nodes.Count; i += 2)
        {
            RoadNode node = nodes[i];
            string prefix = i >= 4 && i <= 8 ? "047" : "046";
            string kind = prefix == "047" ? "curve" : "near";
            CreateRoadSurfaceSkin(context, parent, prefix, node.Position + Vector3.up * 0.04f, node.Yaw, $"road_surface_skin_{kind}_{i:00}", 6.35f, 0.035f, 0.92f, 2.55f, 0.16f);
        }
    }

    private static void CreateRoadSurfaceSkin(BuildContext context, Transform parent, string prefix, Vector3 position, float yaw, string name, float targetMaxXZ, float groundY, float scaleMultiplier, float lengthMultiplier, float heightMultiplier)
    {
        GameObject instance = InstantiateStagePrefab(context, prefix, parent, position, yaw, name, targetMaxXZ, 0f, groundY, scaleMultiplier);
        if (instance == null)
            return;

        Vector3 scale = instance.transform.localScale;
        scale.y *= lengthMultiplier;
        scale.z *= heightMultiplier;
        instance.transform.localScale = scale;
        AlignBottom(instance, groundY);
    }

    private static void CreateContinuousPierDeck(BuildContext context, Transform parent, List<RoadNode> nodes)
    {
        Material deckMaterial = CreateOrLoadMaterial("Stage01_2_Concept_Wet_Pier_Wood", new Color(0.40f, 0.22f, 0.10f, 1f), 0.42f);
        Material deckLightMaterial = CreateOrLoadMaterial("Stage01_2_Concept_Wet_Pier_Wood_Light", new Color(0.52f, 0.31f, 0.15f, 1f), 0.48f);
        Material deckDarkMaterial = CreateOrLoadMaterial("Stage01_2_Concept_Wet_Pier_Wood_Dark", new Color(0.26f, 0.14f, 0.07f, 1f), 0.36f);
        Material seamMaterial = CreateOrLoadMaterial("Stage01_2_Concept_Dark_Pier_Seams", new Color(0.10f, 0.07f, 0.05f, 1f), 0.18f);
        Material metalMaterial = CreateOrLoadMaterial("Stage01_2_Concept_Wet_Rivet_Metal", new Color(0.12f, 0.11f, 0.10f, 1f), 0.48f);

        Material[] plankMaterials = { deckMaterial, deckLightMaterial, deckDarkMaterial };
        const int laneCount = 7;
        const float laneGap = 0.055f;
        float laneWidth = (DeckWidth - 0.65f - (laneCount - 1) * laneGap) / laneCount;
        float rowLength = RoadSpan + 0.45f;
        float firstLaneOffset = -((laneCount - 1) * (laneWidth + laneGap)) * 0.5f;

        for (int row = 0; row < nodes.Count; row++)
        {
            RoadNode node = nodes[row];
            Quaternion rotation = Quaternion.Euler(0f, node.Yaw, 0f);

            CreatePrimitiveObject(
                context,
                parent,
                $"Stage01_2_Pier_Underframe_{row:00}",
                PrimitiveType.Cube,
                node.Position + Vector3.down * 0.18f,
                rotation,
                new Vector3(DeckWidth + 0.4f, 0.2f, rowLength),
                deckDarkMaterial);

            for (int lane = 0; lane < laneCount; lane++)
            {
                float laneOffset = firstLaneOffset + lane * (laneWidth + laneGap);
                Vector3 plankPosition = node.Position + Right(node.Yaw) * laneOffset + Vector3.up * (((row + lane) % 2) * 0.008f);
                CreatePrimitiveObject(
                    context,
                    parent,
                    $"Stage01_2_Pier_Plank_Row_{row:00}_Lane_{lane:00}",
                    PrimitiveType.Cube,
                    plankPosition,
                    rotation,
                    new Vector3(laneWidth, 0.105f, rowLength - 0.12f),
                    plankMaterials[(row + lane) % plankMaterials.Length]);
            }

            CreatePrimitiveObject(
                context,
                parent,
                $"Stage01_2_Pier_Cross_Seam_{row:00}",
                PrimitiveType.Cube,
                node.Position - Forward(node.Yaw) * (rowLength * 0.48f) + Vector3.up * 0.07f,
                rotation,
                new Vector3(DeckWidth - 0.45f, 0.026f, 0.045f),
                seamMaterial);

            CreatePrimitiveObject(
                context,
                parent,
                $"Stage01_2_Pier_Left_Edge_Beam_{row:00}",
                PrimitiveType.Cube,
                node.Position + Right(node.Yaw) * (-DeckWidth * 0.5f) + Vector3.up * 0.08f,
                rotation,
                new Vector3(0.28f, 0.32f, rowLength),
                seamMaterial);

            CreatePrimitiveObject(
                context,
                parent,
                $"Stage01_2_Pier_Right_Edge_Beam_{row:00}",
                PrimitiveType.Cube,
                node.Position + Right(node.Yaw) * (DeckWidth * 0.5f) + Vector3.up * 0.08f,
                rotation,
                new Vector3(0.28f, 0.32f, rowLength),
                seamMaterial);

            if (row % 2 == 0)
            {
                CreatePrimitiveObject(
                    context,
                    parent,
                    $"Stage01_2_Pier_Left_Rivet_{row:00}",
                    PrimitiveType.Cube,
                    node.Position + Right(node.Yaw) * -2.35f + Vector3.up * 0.16f,
                    rotation,
                    new Vector3(0.18f, 0.045f, 0.18f),
                    metalMaterial);

                CreatePrimitiveObject(
                    context,
                    parent,
                    $"Stage01_2_Pier_Right_Rivet_{row:00}",
                    PrimitiveType.Cube,
                    node.Position + Right(node.Yaw) * 2.35f + Vector3.up * 0.16f,
                    rotation,
                    new Vector3(0.18f, 0.045f, 0.18f),
                    metalMaterial);
            }
        }

        RoadNode foreground = nodes[0];
        Quaternion foregroundRotation = Quaternion.Euler(0f, foreground.Yaw, 0f);
        float foregroundWidth = DeckWidth + 2.8f;
        float foregroundLength = RoadSpan * 1.58f;
        float foregroundLaneWidth = (foregroundWidth - 0.65f - (laneCount - 1) * laneGap) / laneCount;
        float foregroundFirstLaneOffset = -((laneCount - 1) * (foregroundLaneWidth + laneGap)) * 0.5f;
        Vector3 foregroundCenter = foreground.Position - Forward(foreground.Yaw) * 2.95f;

        CreatePrimitiveObject(
            context,
            parent,
            "Stage01_2_Pier_Foreground_Extension_Underframe",
            PrimitiveType.Cube,
            foregroundCenter + Vector3.down * 0.18f,
            foregroundRotation,
            new Vector3(foregroundWidth + 0.4f, 0.2f, foregroundLength),
            deckDarkMaterial);

        for (int lane = 0; lane < laneCount; lane++)
        {
            float laneOffset = foregroundFirstLaneOffset + lane * (foregroundLaneWidth + laneGap);
            CreatePrimitiveObject(
                context,
                parent,
                $"Stage01_2_Pier_Foreground_Extension_Lane_{lane:00}",
                PrimitiveType.Cube,
                foregroundCenter + Right(foreground.Yaw) * laneOffset + Vector3.up * ((lane % 2) * 0.008f),
                foregroundRotation,
                new Vector3(foregroundLaneWidth, 0.105f, foregroundLength - 0.12f),
                plankMaterials[lane % plankMaterials.Length]);
        }

        CreatePrimitiveObject(
            context,
            parent,
            "Stage01_2_Pier_Foreground_Extension_Cross_Seam",
            PrimitiveType.Cube,
            foregroundCenter + Forward(foreground.Yaw) * (foregroundLength * 0.48f) + Vector3.up * 0.07f,
            foregroundRotation,
            new Vector3(foregroundWidth - 0.45f, 0.026f, 0.045f),
            seamMaterial);

        CreatePrimitiveObject(
            context,
            parent,
            "Stage01_2_Pier_Foreground_Left_Edge_Beam",
            PrimitiveType.Cube,
            foregroundCenter + Right(foreground.Yaw) * (-foregroundWidth * 0.5f) + Vector3.up * 0.08f,
            foregroundRotation,
            new Vector3(0.28f, 0.32f, foregroundLength),
            seamMaterial);

        CreatePrimitiveObject(
            context,
            parent,
            "Stage01_2_Pier_Foreground_Right_Edge_Beam",
            PrimitiveType.Cube,
            foregroundCenter + Right(foreground.Yaw) * (foregroundWidth * 0.5f) + Vector3.up * 0.08f,
            foregroundRotation,
            new Vector3(0.28f, 0.32f, foregroundLength),
            seamMaterial);
    }

    private static float CalculatePathYaw(float[] pathX, int index)
    {
        int previous = Mathf.Max(0, index - 1);
        int next = Mathf.Min(pathX.Length - 1, index + 1);
        Vector3 delta = new Vector3(pathX[next] - pathX[previous], 0f, (next - previous) * RoadSpan);
        if (delta.sqrMagnitude <= 0.001f)
            return 0f;

        return Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
    }

    private static void PlaceBoundaries(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        for (int i = 1; i < nodes.Count; i += 4)
        {
            RoadNode node = nodes[i];
            InstantiateStagePrefab(context, "011", parent, Side(node, -RoadSideOffset - 0.18f, 0.05f), node.Yaw, $"left_dock_railing_module_{i:00}", 2.15f, 0.82f, 0f, 0.86f);
            InstantiateStagePrefab(context, "011", parent, Side(node, RoadSideOffset + 0.18f, 0.05f), node.Yaw + 180f, $"right_dock_railing_module_{i:00}", 2.15f, 0.82f, 0f, 0.86f);

            InstantiateStagePrefab(context, "012", parent, Side(node, -RoadSideOffset - 0.25f, 1.25f), node.Yaw, $"left_rope_post_barrier_{i:00}", 1.75f, 0.9f, 0f, 0.8f);
            InstantiateStagePrefab(context, "012", parent, Side(node, RoadSideOffset + 0.25f, 1.25f), node.Yaw + 180f, $"right_rope_post_barrier_{i:00}", 1.75f, 0.9f, 0f, 0.8f);
        }

        InstantiateStagePrefab(context, "024", parent, Side(nodes[3], RoadSideOffset - 0.05f, 0.5f), nodes[3].Yaw + 18f, "right_life_ring_near", 0f, 1.0f, 0f);
        InstantiateStagePrefab(context, "024", parent, Side(nodes[8], -RoadSideOffset + 0.05f, -0.35f), nodes[8].Yaw - 18f, "left_life_ring_far", 0f, 0.95f, 0f);
    }

    private static void PlaceMarketFacades(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        PlaceMarketBuilding(context, parent, nodes[0], -1, "043", "left_market_awning_foreground", 0.65f, 5.85f, 5.05f, -3f, 0.98f);
        PlaceMarketBuilding(context, parent, nodes[0], 1, "016", "right_market_display_foreground", 0.85f, 5.35f, 4.9f, 3f, 0.96f);
        PlaceMarketBuilding(context, parent, nodes[1], -1, "021", "left_market_aquarium_foreground", 0.0f, 4.75f, 4.15f, 2f, 0.92f);
        PlaceMarketBuilding(context, parent, nodes[1], 1, "043", "right_market_awning_foreground", 0.1f, 5.45f, 4.75f, -4f, 0.95f);
        PlaceMarketBuilding(context, parent, nodes[2], -1, "014", "left_market_facade_near", -0.45f, 5.15f, 4.75f, -5f, 0.96f);
        PlaceMarketBuilding(context, parent, nodes[2], 1, "015", "right_sashimi_restaurant_near", 0.65f, 4.95f, 4.55f, 5f, 0.88f);
        PlaceMarketBuilding(context, parent, nodes[4], -1, "016", "left_seafood_display_mid", 0.2f, 4.45f, 4.15f, 4f, 0.9f);
        PlaceMarketBuilding(context, parent, nodes[5], 1, "043", "right_market_awning_mid", -0.3f, 4.65f, 4.15f, -6f, 0.92f);
        PlaceMarketBuilding(context, parent, nodes[7], -1, "021", "left_aquarium_row_far", 0.25f, 3.35f, 3.25f, 2f, 0.8f);
        PlaceMarketBuilding(context, parent, nodes[8], 1, "014", "right_market_facade_far", 0.75f, 3.8f, 3.55f, -5f, 0.8f);
        PlaceMarketBuilding(context, parent, nodes[10], -1, "015", "left_sashimi_endcap_far", 0.75f, 3.55f, 3.35f, 5f, 0.78f);
        PlaceMarketBuilding(context, parent, nodes[10], 1, "016", "right_seafood_endcap_far", 0.25f, 3.5f, 3.3f, -4f, 0.78f);

        InstantiateStagePrefab(context, "027", parent, Side(nodes[2], -3.35f, -0.95f) + Vector3.up * 1.45f, nodes[2].Yaw, "left_warm_market_lamp_near", 0.85f, 0.85f, 1.45f, 0.85f);
        InstantiateStagePrefab(context, "027", parent, Side(nodes[3], 3.35f, -0.4f) + Vector3.up * 1.45f, nodes[3].Yaw + 180f, "right_warm_market_lamp_near", 0.85f, 0.85f, 1.45f, 0.85f);
        InstantiateStagePrefab(context, "007", parent, Side(nodes[5], -3.42f, -0.2f) + Vector3.up * 0.95f, nodes[5].Yaw - 4f, "left_fish_market_lamp_sign", 1.12f, 1.12f, 0.95f, 0.78f);
        InstantiateStagePrefab(context, "007", parent, Side(nodes[6], 3.42f, 0.35f) + Vector3.up * 0.95f, nodes[6].Yaw + 184f, "right_fish_market_lamp_sign", 1.12f, 1.12f, 0.95f, 0.78f);
        InstantiateStagePrefab(context, "028", parent, Side(nodes[4], -3.32f, -1.1f) + Vector3.up * 0.12f, nodes[4].Yaw + 4f, "left_crab_mascot_market_sign", 1.25f, 1.35f, 0.12f, 0.78f);
        InstantiateStagePrefab(context, "029", parent, Side(nodes[6], 3.32f, 0.8f) + Vector3.up * 0.12f, nodes[6].Yaw + 176f, "right_fish_mascot_market_sign", 1.25f, 1.35f, 0.12f, 0.78f);
        PlaceTallMarketFrame(context, nodes, parent);
        PlaceUpperMarketDepth(context, nodes, parent);
    }

    private static void PlaceTallMarketFrame(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        InstantiateStagePrefab(context, "036", parent, Side(nodes[0], -3.55f, -0.35f), nodes[0].Yaw - 6f, "left_foreground_utility_pole_frame", 1.35f, 6.8f, 0f, 1.2f);
        InstantiateStagePrefab(context, "036", parent, Side(nodes[0], 3.55f, -0.25f), nodes[0].Yaw + 186f, "right_foreground_utility_pole_frame", 1.35f, 6.8f, 0f, 1.2f);
        InstantiateStagePrefab(context, "027", parent, Side(nodes[1], -3.18f, 0.25f) + Vector3.up * 2.55f, nodes[1].Yaw - 3f, "left_upper_hanging_lamp_frame", 1.0f, 1.0f, 2.55f, 0.95f);
        InstantiateStagePrefab(context, "027", parent, Side(nodes[1], 3.18f, 0.3f) + Vector3.up * 2.55f, nodes[1].Yaw + 183f, "right_upper_hanging_lamp_frame", 1.0f, 1.0f, 2.55f, 0.95f);
        InstantiateStagePrefab(context, "029", parent, Side(nodes[2], 3.12f, -1.15f) + Vector3.up * 2.75f, nodes[2].Yaw + 178f, "right_upper_fish_sign_frame", 1.55f, 1.75f, 2.75f, 0.95f);
        InstantiateStagePrefab(context, "028", parent, Side(nodes[2], -3.12f, -1.05f) + Vector3.up * 2.55f, nodes[2].Yaw - 4f, "left_upper_crab_sign_frame", 1.45f, 1.7f, 2.55f, 0.92f);
    }

    private static void PlaceUpperMarketDepth(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        InstantiateStagePrefab(context, "036", parent, Side(nodes[3], -3.52f, 0.45f), nodes[3].Yaw - 4f, "left_mid_utility_pole_frame", 1.25f, 6.2f, 0f, 1.06f);
        InstantiateStagePrefab(context, "036", parent, Side(nodes[4], 3.52f, 0.25f), nodes[4].Yaw + 184f, "right_mid_utility_pole_frame", 1.25f, 6.05f, 0f, 1.04f);
        InstantiateStagePrefab(context, "027", parent, Side(nodes[4], -3.18f, -0.35f) + Vector3.up * 2.65f, nodes[4].Yaw - 5f, "left_upper_hanging_lamp_depth_04", 0.95f, 1.0f, 2.65f, 0.9f);
        InstantiateStagePrefab(context, "027", parent, Side(nodes[5], 3.18f, -0.15f) + Vector3.up * 2.55f, nodes[5].Yaw + 182f, "right_upper_hanging_lamp_depth_05", 0.95f, 1.0f, 2.55f, 0.9f);
        InstantiateStagePrefab(context, "027", parent, Side(nodes[6], -3.18f, 0.25f) + Vector3.up * 2.5f, nodes[6].Yaw - 4f, "left_upper_hanging_lamp_depth_06", 0.9f, 0.95f, 2.5f, 0.88f);
        InstantiateStagePrefab(context, "027", parent, Side(nodes[6], 3.18f, 0.35f) + Vector3.up * 2.6f, nodes[6].Yaw + 184f, "right_upper_hanging_lamp_depth_06", 0.9f, 0.95f, 2.6f, 0.88f);
    }

    private static void PlaceMarketBuilding(BuildContext context, Transform parent, RoadNode node, int side, string prefix, string name, float forwardOffset, float targetHeight, float targetMaxXZ, float yawOffset, float scaleMultiplier)
    {
        float yaw = node.Yaw + (side > 0 ? 180f : 0f) + yawOffset;
        Vector3 position = Side(node, side * MarketSideOffset, forwardOffset);
        InstantiateStagePrefab(context, prefix, parent, position, yaw, name, targetMaxXZ, targetHeight, 0f, scaleMultiplier);
    }

    private static void PlaceDenseMarketProps(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        PlaceEdgeProp(context, parent, nodes[0], -1, "001", "left_foreground_blue_crate_blocker", PropSideOffset, 2.35f, 1.55f, 2.15f, -8f, 1.18f);
        PlaceEdgeProp(context, parent, nodes[0], 1, "002", "right_foreground_styrofoam_box_blocker", PropSideOffset, 2.85f, 1.45f, 2.05f, 5f, 1.18f);
        PlaceEdgeProp(context, parent, nodes[1], -1, "030", "left_crab_aquarium_foreground", 3.1f, 0.95f, 1.85f, 2.45f, -6f, 1.08f);
        PlaceEdgeProp(context, parent, nodes[1], 1, "031", "right_octopus_aquarium_foreground", 3.1f, 1.35f, 1.85f, 2.45f, 6f, 1.08f);
        PlaceEdgeProp(context, parent, nodes[2], -1, "020", "left_fish_box_stack_mid", 2.9f, -0.15f, 1.5f, 2.0f, 4f, 1.12f);
        PlaceEdgeProp(context, parent, nodes[2], 1, "003", "right_ice_fish_tank_mid", 2.95f, 0.75f, 1.55f, 2.1f, -5f, 1.08f);
        PlaceEdgeProp(context, parent, nodes[4], -1, "032", "left_orange_crate_stack", 2.8f, 0.35f, 1.45f, 2.0f, 8f, 1.12f);
        PlaceEdgeProp(context, parent, nodes[4], 1, "033", "right_white_fish_crate", 2.85f, -0.15f, 1.35f, 1.9f, -10f, 1.1f);
        PlaceEdgeProp(context, parent, nodes[6], -1, "004", "left_seafood_cart_offset", 2.65f, 0.95f, 1.45f, 2.05f, -7f, 1.1f);
        PlaceEdgeProp(context, parent, nodes[6], 1, "022", "right_ice_box_stack", 2.85f, 0.35f, 1.45f, 2.0f, 6f, 1.1f);
        PlaceEdgeProp(context, parent, nodes[8], -1, "034", "left_fishing_net_pile", 3.05f, 0.45f, 0.95f, 1.75f, 12f, 1.08f);
        PlaceEdgeProp(context, parent, nodes[8], 1, "025", "right_tire_fender_stack", 3.05f, 0.4f, 1.1f, 1.65f, -16f, 1.08f);
        PlaceEdgeProp(context, parent, nodes[10], -1, "006", "left_buoy_with_rope_post", 3.0f, -0.3f, 1.3f, 1.8f, 7f, 1.05f);
        PlaceEdgeProp(context, parent, nodes[10], 1, "026", "right_dock_cleat_far", 2.95f, 0.15f, 0.5f, 1.1f, -8f, 1.0f);

        InstantiateStagePrefab(context, "039", parent, Side(nodes[3], -2.4f, -0.25f), nodes[3].Yaw, "left_ice_scatter_wet_market", 1.35f, 0.32f, 0.03f, 1.15f);
        InstantiateStagePrefab(context, "040", parent, Side(nodes[5], 2.45f, 0.4f), nodes[5].Yaw, "right_fish_scrap_scatter_wet_market", 1.35f, 0.32f, 0.03f, 1.15f);
        CreateForegroundDensity(context, parent, nodes);
    }

    private static void CreateForegroundDensity(BuildContext context, Transform parent, List<RoadNode> nodes)
    {
        RoadNode start = nodes[0];
        InstantiateStagePrefab(context, "035", parent, Side(start, -3.05f, -0.75f), start.Yaw - 22f, "left_foreground_anchor_prop", 1.85f, 1.35f, 0f, 1.05f);
        InstantiateStagePrefab(context, "034", parent, Side(start, 3.05f, -0.45f), start.Yaw + 18f, "right_foreground_fishing_net_pile", 1.9f, 1.05f, 0f, 1.0f);
        InstantiateStagePrefab(context, "039", parent, Side(start, -2.25f, 0.75f), start.Yaw + 10f, "left_foreground_ice_chunk_scatter", 1.35f, 0.32f, 0.03f, 1.1f);
        InstantiateStagePrefab(context, "040", parent, Side(start, 2.25f, 1.1f), start.Yaw - 8f, "right_foreground_fish_scrap_scatter", 1.35f, 0.32f, 0.03f, 1.1f);
    }

    private static void PlaceEdgeProp(BuildContext context, Transform parent, RoadNode node, int side, string prefix, string name, float sideOffset, float forwardOffset, float targetHeight, float targetMaxXZ, float yawOffset, float scaleMultiplier)
    {
        InstantiateStagePrefab(context, prefix, parent, Side(node, side * sideOffset, forwardOffset), node.Yaw + yawOffset, name, targetMaxXZ, targetHeight, 0f, scaleMultiplier);
    }

    private static void PlaceGameplay(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        InstantiateStagePrefab(context, "045", parent, nodes[7].Position + Vector3.up * 0.05f, nodes[7].Yaw, "overhead_harbor_lane_signal", 5.25f, 4.25f, 0f, 0.96f);
        InstantiateStagePrefab(context, "044", parent, Side(nodes[8], 2.0f, 1.05f), nodes[8].Yaw - 6f, "right_edge_barricade_warning", 1.9f, 1.25f, 0f, 0.95f);

        Material pickupMaterial = CreateOrLoadMaterial("Stage01_2_Gold_Coin", new Color(1f, 0.68f, 0.08f, 1f), 0.72f);

        for (int i = 0; i < 11; i++)
        {
            RoadNode node = nodes[Mathf.Min(i, nodes.Count - 1)];
            Vector3 position = node.Position + Vector3.up * 0.62f + Forward(node.Yaw) * 0.75f;
            CreateGoldCoin(context, parent, $"Stage01_2_Center_Gold_Coin_Line_{i:00}", position, node.Yaw, pickupMaterial);
        }
    }

    private static void CreateGoldCoin(BuildContext context, Transform parent, string name, Vector3 position, float yaw, Material material)
    {
        Quaternion rotation = Quaternion.Euler(90f, yaw, 0f);
        GameObject coin = CreatePrimitiveObject(
            context,
            parent,
            name,
            PrimitiveType.Cylinder,
            position,
            rotation,
            new Vector3(0.36f, 0.035f, 0.36f),
            material);

        Material faceMaterial = CreateOrLoadMaterial("Stage01_2_Gold_Coin_Face", new Color(1f, 0.82f, 0.18f, 1f), 0.78f);
        CreatePrimitiveObject(
            context,
            coin.transform,
            name + "_Emboss_Horizontal",
            PrimitiveType.Cube,
            Vector3.forward * 0.022f,
            Quaternion.identity,
            new Vector3(0.19f, 0.026f, 0.016f),
            faceMaterial);
        CreatePrimitiveObject(
            context,
            coin.transform,
            name + "_Emboss_Vertical",
            PrimitiveType.Cube,
            Vector3.forward * 0.024f,
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.19f, 0.026f, 0.016f),
            faceMaterial);
    }

    private static void PlaceBackground(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        RoadNode mid = nodes[5];
        RoadNode far = nodes[nodes.Count - 1];
        float centerZ = (nodes[0].Position.z + far.Position.z) * 0.5f;
        float waterLength = far.Position.z + RoadSpan * 2.1f;
        Material waterMaterial = CreateOrLoadMaterial("Stage01_2_Visible_Harbor_Water", new Color(0.02f, 0.46f, 0.78f, 0.9f), 0.72f);

        CreatePrimitiveObject(
            context,
            parent,
            "Harbor_Water_Left_Flat_Plane",
            PrimitiveType.Plane,
            new Vector3(-WaterSideOffset, -0.24f, centerZ),
            Quaternion.identity,
            new Vector3(1.65f, 1f, waterLength * 0.1f),
            waterMaterial);

        CreatePrimitiveObject(
            context,
            parent,
            "Harbor_Water_Right_Flat_Plane",
            PrimitiveType.Plane,
            new Vector3(WaterSideOffset, -0.24f, centerZ),
            Quaternion.identity,
            new Vector3(1.65f, 1f, waterLength * 0.1f),
            waterMaterial);

        CreatePrimitiveObject(
            context,
            parent,
            "Harbor_Water_Back_Flat_Plane",
            PrimitiveType.Plane,
            new Vector3(0f, -0.34f, far.Position.z + RoadSpan * 1.15f),
            Quaternion.identity,
            new Vector3(3.0f, 1f, 1.25f),
            waterMaterial);

        InstantiateStagePrefab(context, "017", parent, new Vector3(0f, -0.38f, centerZ + RoadSpan * 1.5f), far.Yaw, "stage01_ocean_water_backdrop_center", 20f, 0f, -0.38f, 1.0f);
        InstantiateStagePrefab(context, "018", parent, Side(nodes[4], -WaterSideOffset - 0.45f, 2.6f), mid.Yaw + 12f, "offset_fishing_boat_left_background", 6.0f, 4.25f, -0.12f, 0.96f);
        InstantiateStagePrefab(context, "018", parent, Side(nodes[7], WaterSideOffset - 0.25f, 3.35f), mid.Yaw - 20f, "offset_fishing_boat_right_background", 5.6f, 4.0f, -0.12f, 0.94f);
        InstantiateStagePrefab(context, "018", parent, Side(nodes[9], -3.55f, 2.15f), mid.Yaw + 8f, "center_left_mast_boat_background", 5.9f, 4.9f, -0.12f, 0.94f);
        InstantiateStagePrefab(context, "018", parent, far.Position + Forward(far.Yaw) * 5.6f + Right(far.Yaw) * 0.45f, mid.Yaw - 4f, "center_mast_boat_background", 5.2f, 6.2f, -0.12f, 0.88f);
        InstantiateStagePrefab(context, "042", parent, Side(nodes[8], -WaterSideOffset + 0.55f, 2.65f), mid.Yaw + 8f, "left_boat_detail_cluster", 4.2f, 2.9f, -0.12f, 0.95f);
        InstantiateStagePrefab(context, "019", parent, far.Position + Forward(far.Yaw) * 16.5f + Right(far.Yaw) * -12.5f, far.Yaw, "distant_hillside_village_backdrop", 9.5f, 0f, -0.08f, 0.78f);
        CreateDistantSkyline(context, parent, far);
        InstantiateStagePrefab(context, "037", parent, Side(nodes[5], -WaterSideOffset + 0.85f, -1.0f), mid.Yaw, "left_red_buoy_open_water", 0.9f, 0.85f, -0.1f, 1.0f);
        InstantiateStagePrefab(context, "037", parent, Side(nodes[8], WaterSideOffset - 0.9f, 0.7f), mid.Yaw, "right_red_buoy_open_water", 0.9f, 0.85f, -0.1f, 1.0f);
        InstantiateStagePrefab(context, "038", parent, Side(nodes[4], WaterSideOffset - 0.85f, -1.25f), mid.Yaw + 20f, "right_floating_plank_open_water", 1.15f, 0.5f, -0.1f, 1.0f);
        InstantiateStagePrefab(context, "041", parent, Side(nodes[8], -3.4f, 1.2f) + Vector3.up * 6.4f, nodes[8].Yaw, "left_flying_gull_open_harbor", 1.0f, 0.95f, 6.4f, 1.0f);
        InstantiateStagePrefab(context, "041", parent, Side(nodes[9], 3.3f, 0.7f) + Vector3.up * 6.9f, nodes[9].Yaw + 14f, "right_flying_gull_open_harbor", 0.95f, 0.85f, 6.9f, 1.0f);
    }

    private static void CreateDistantSkyline(BuildContext context, Transform parent, RoadNode far)
    {
        Material skylineMaterial = CreateOrLoadMaterial("Stage01_2_Distant_Noryangjin_Skyline", new Color(0.20f, 0.32f, 0.42f, 1f), 0.18f);
        float baseZ = far.Position.z + RoadSpan * 3.2f;
        float[] xOffsets = { -2.45f, -1.48f, -0.62f, 0.25f, 1.18f, 2.05f };
        float[] heights = { 3.2f, 4.5f, 4.0f, 6.8f, 5.1f, 3.9f };
        float[] widths = { 0.85f, 1.05f, 0.8f, 1.2f, 1.0f, 0.85f };

        for (int i = 0; i < xOffsets.Length; i++)
        {
            CreatePrimitiveObject(
                context,
                parent,
                $"Stage01_2_Distant_Noryangjin_Skyline_Tower_{i:00}",
                PrimitiveType.Cube,
                new Vector3(xOffsets[i], heights[i] * 0.5f - 0.16f, baseZ + i * 0.12f),
                Quaternion.identity,
                new Vector3(widths[i], heights[i], 0.24f),
                skylineMaterial);
        }
    }

    private static void PlacePlayerPreview(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        const string playerModel = "Assets/ShooterSurvival/Models/Player/Shark5/Shark2.fbx";
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(playerModel);
        if (model == null)
        {
            context.Missing++;
            context.Report.AppendLine("Missing player model: " + playerModel);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(model);

        RoadNode start = nodes[0];
        instance.name = "Player_Blue_Shark_Preview";
        instance.transform.SetParent(parent, false);
        instance.transform.position = start.Position - Forward(start.Yaw) * 2.75f + Vector3.up * 0.05f;
        instance.transform.rotation = Quaternion.Euler(0f, start.Yaw, 0f);
        FitHeight(instance, 1.15f);
        AlignBottom(instance, 0f);
        UpgradeRendererMaterialsToStageLit(instance);
        context.Placed++;
    }

    private static Camera CreateLightingAndCamera(List<RoadNode> nodes)
    {
        GameObject light = new GameObject("Directional Light - Open Harbor Sun");
        Light directional = light.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 1.65f;
        directional.color = new Color(1f, 0.91f, 0.76f, 1f);
        light.transform.rotation = Quaternion.Euler(46f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.35f, 0.42f, 0.48f, 1f);

        Bounds bounds = new Bounds(nodes[0].Position, Vector3.one);
        foreach (RoadNode node in nodes)
            bounds.Encapsulate(node.Position);

        Vector3 center = bounds.center;
        GameObject cameraObject = new GameObject("Camera - Stage01_2 Reference Overview");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(19f, bounds.size.z * 0.4f);
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.transform.position = center + new Vector3(0f, 35f, -29f);
        cameraObject.transform.LookAt(center);

        GameObject runnerCameraObject = new GameObject("Camera - Stage01_2 Runner 9x16 Preview");
        Camera runnerCamera = runnerCameraObject.AddComponent<Camera>();
        runnerCamera.fieldOfView = 50f;
        runnerCamera.nearClipPlane = 0.1f;
        runnerCamera.clearFlags = CameraClearFlags.Skybox;
        runnerCameraObject.transform.position = nodes[0].Position + new Vector3(0f, 3.05f, -9.6f);
        runnerCameraObject.transform.LookAt(nodes[2].Position + Vector3.down * 2.0f);

        return runnerCamera;
    }

    private static void CaptureRunnerPreview(BuildContext context, Camera camera)
    {
        if (camera == null)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(PreviewPath));

        RenderTexture renderTexture = new RenderTexture(720, 1280, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D texture = new Texture2D(720, 1280, TextureFormat.RGBA32, false);

        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0);
            texture.Apply();
            File.WriteAllBytes(PreviewPath, texture.EncodeToPNG());
            context.PreviewPath = PreviewPath;
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static GameObject CreatePrimitiveObject(BuildContext context, Transform parent, string name, PrimitiveType primitiveType, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject instance = GameObject.CreatePrimitive(primitiveType);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;
        instance.transform.localScale = scale;

        Renderer renderer = instance.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        Collider collider = instance.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);

        context.Placed++;
        return instance;
    }

    private static GameObject CreateBoxColliderObject(BuildContext context, Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 size)
    {
        GameObject instance = new GameObject(name);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;

        BoxCollider collider = instance.AddComponent<BoxCollider>();
        collider.size = size;

        context.Placed++;
        return instance;
    }

    private static Shader GetStageLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        return shader;
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color, float smoothness)
    {
        EnsureFolder(GeneratedMaterialRoot);

        string path = $"{GeneratedMaterialRoot}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = GetStageLitShader();
            if (shader == null)
                return null;

            material = new Material(shader)
            {
                name = materialName
            };
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyInstanceMaterial(GameObject instance, Material material)
    {
        if (instance == null || material == null)
            return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            renderer.sharedMaterial = material;
    }

    private static void UpgradeStage01SecondGeneratedMaterials(BuildContext context)
    {
        EnsureFolder(GeneratedMaterialRoot);

        Shader shader = GetStageLitShader();
        if (shader == null)
            return;

        string[] materialPaths = Directory.GetFiles(GeneratedMaterialRoot, "Stage01_2*.mat");
        foreach (string materialPath in materialPaths)
        {
            string assetPath = materialPath.Replace('\\', '/');
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null || material.shader == shader)
                continue;

            material.shader = shader;
            EditorUtility.SetDirty(material);
            context.MaterialShaderUpgrades++;
        }
    }

    private static void UpgradeRendererMaterialsToStageLit(GameObject instance)
    {
        if (instance == null)
            return;

        Shader shader = GetStageLitShader();
        if (shader == null)
            return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == shader)
                    continue;

                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
        }
    }

    private static GameObject InstantiateStagePrefab(BuildContext context, string prefix, Transform parent, Vector3 position, float yaw, string name, float targetMaxXZ, float targetHeight, float groundY, float scaleMultiplier = 1f)
    {
        GameObject prefab = LoadPrefab(context, prefix);
        if (prefab == null)
            return null;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(prefab);

        instance.name = $"{prefix}_{name}";
        instance.transform.SetParent(parent, false);
        Quaternion prefabAxisCorrection = instance.transform.localRotation;
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefabAxisCorrection;

        FitEnvelope(instance, targetMaxXZ, targetHeight);
        if (!Mathf.Approximately(scaleMultiplier, 1f))
            instance.transform.localScale *= scaleMultiplier;
        AlignBottom(instance, groundY);
        UpgradeRendererMaterialsToStageLit(instance);

        context.Placed++;
        return instance;
    }

    private static GameObject LoadPrefab(BuildContext context, string prefix)
    {
        if (context.PrefabCache.TryGetValue(prefix, out GameObject cached))
            return cached;

        string[] guids = AssetDatabase.FindAssets(prefix + " t:Prefab", new[] { StagePrefabRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!fileName.StartsWith(prefix + "_", System.StringComparison.Ordinal))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            context.PrefabCache[prefix] = prefab;
            return prefab;
        }

        context.Missing++;
        context.Report.AppendLine("Missing prefab prefix: " + prefix);
        context.PrefabCache[prefix] = null;
        return null;
    }

    private static void FitEnvelope(GameObject instance, float targetMaxXZ, float targetHeight)
    {
        if (!TryGetBounds(instance, out Bounds bounds))
            return;

        float scale = float.PositiveInfinity;

        if (targetHeight > 0f && bounds.size.y > 0.001f)
            scale = Mathf.Min(scale, targetHeight / bounds.size.y);

        if (targetMaxXZ > 0f)
        {
            float maxXZ = Mathf.Max(bounds.size.x, bounds.size.z);
            if (maxXZ > 0.001f)
                scale = Mathf.Min(scale, targetMaxXZ / maxXZ);
        }

        if (float.IsInfinity(scale) || scale <= 0f)
            return;

        instance.transform.localScale *= scale;
    }

    private static void FitHeight(GameObject instance, float target)
    {
        if (!TryGetBounds(instance, out Bounds bounds))
            return;

        if (bounds.size.y <= 0.001f)
            return;

        float scale = target / bounds.size.y;
        instance.transform.localScale *= scale;
    }

    private static void AlignBottom(GameObject instance, float y)
    {
        if (!TryGetBounds(instance, out Bounds bounds))
            return;

        Vector3 position = instance.transform.position;
        position.y += y - bounds.min.y;
        instance.transform.position = position;
    }

    private static bool TryGetBounds(GameObject instance, out Bounds bounds)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    private static Vector3 Side(RoadNode node, float sideOffset, float forwardOffset = 0f)
    {
        return node.Position + Right(node.Yaw) * sideOffset + Forward(node.Yaw) * forwardOffset;
    }

    private static Vector3 Forward(float yaw)
    {
        return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
    }

    private static Vector3 Right(float yaw)
    {
        return Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        string folder = Path.GetFileName(assetPath);

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void WriteReport(BuildContext context, int roadNodeCount)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(
            ReportPath,
            $"ScenePath: {ScenePath}\n" +
            $"RoadNodes: {roadNodeCount}\n" +
            $"ObjectsPlaced: {context.Placed}\n" +
            $"MaterialShaderUpgrades: {context.MaterialShaderUpgrades}\n" +
            $"PreviewPath: {context.PreviewPath}\n" +
            $"MissingPrefabs: {context.Missing}\n" +
            context.Report);
    }

    private readonly struct RoadNode
    {
        public RoadNode(Vector3 position, float yaw)
        {
            Position = position;
            Yaw = yaw;
        }

        public readonly Vector3 Position;
        public readonly float Yaw;
    }

    private sealed class BuildContext
    {
        public int Placed;
        public int Missing;
        public int MaterialShaderUpgrades;
        public string PreviewPath = string.Empty;
        public readonly StringBuilder Report = new StringBuilder();
        public readonly Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>();
    }
}
#endif
