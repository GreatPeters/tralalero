#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage01NoryangjinAutoDraftBuilder
{
    private const string StagePrefabRoot = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin";
    private const string ScenePath = "Assets/ShooterSurvival/Scenes/Generated/Stage01_Noryangjin_AutoDraft.unity";
    private const string RequestPath = "Temp/Stage01NoryangjinAutoDraftRequest.txt";
    private const string ReportPath = "Temp/Stage01NoryangjinAutoDraftReport.txt";
    private const string GeneratedMaterialRoot = "Assets/ShooterSurvival/Materials/Generated";

    private const float RoadSpan = 6.4f;
    private const float DeckWidth = 10.4f;
    private const float RoadSideOffset = 5.05f;
    private const float BuildingSideOffset = 6.15f;
    private const float WaterSideOffset = 12.5f;

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

    [MenuItem("Tools/MeshyAI/Build Stage01 Noryangjin Auto Draft Scene", false, 2310)]
    public static void BuildScene()
    {
        EnsureFolder("Assets/ShooterSurvival/Scenes");
        EnsureFolder("Assets/ShooterSurvival/Scenes/Generated");

        var context = new BuildContext();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("Stage01_1_Noryangjin_ConceptDraft");
        Transform roads = CreateChild(root.transform, "01_Continuous_Wet_Pier_Road");
        Transform boundaries = CreateChild(root.transform, "02_Rope_Post_Railings");
        Transform buildings = CreateChild(root.transform, "03_Market_Buildings");
        Transform props = CreateChild(root.transform, "04_Edge_Props_Only");
        Transform gameplay = CreateChild(root.transform, "05_Clear_Center_Gameplay");
        Transform background = CreateChild(root.transform, "06_Harbor_Background");

        List<RoadNode> nodes = BuildRoadPath(context, roads, null);

        PlaceBoundaries(context, nodes, boundaries);
        PlaceMarketBuildings(context, nodes, buildings);
        PlacePropVariation(context, nodes, props);
        PlaceGameplay(context, nodes, gameplay);
        PlaceBackground(context, nodes, background);
        PlacePlayerPreview(context, nodes, gameplay);
        CreateLightingAndCamera(nodes);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WriteReport(context, nodes.Count);
        Debug.Log($"[MeshyAI] Stage01 Noryangjin auto draft scene built: {ScenePath}. {context.Placed} objects placed, {context.Missing} missing.");
    }

    private static List<RoadNode> BuildRoadPath(BuildContext context, Transform roads, Transform branches)
    {
        var nodes = new List<RoadNode>();
        const int nodeCount = 11;
        const float yaw = 0f;
        float pathLength = (nodeCount - 1) * RoadSpan;
        float deckLength = pathLength + RoadSpan * 1.8f;
        float centerZ = pathLength * 0.5f;

        Material deckMaterial = CreateOrLoadMaterial("Stage01_Wet_Pier_Wood", new Color(0.42f, 0.24f, 0.11f, 1f), 0.28f);
        Material deckLightMaterial = CreateOrLoadMaterial("Stage01_Wet_Pier_Wood_Light", new Color(0.50f, 0.30f, 0.15f, 1f), 0.32f);
        Material deckDarkMaterial = CreateOrLoadMaterial("Stage01_Wet_Pier_Wood_Dark", new Color(0.32f, 0.18f, 0.09f, 1f), 0.26f);
        Material darkWoodMaterial = CreateOrLoadMaterial("Stage01_Dark_Pier_Seams", new Color(0.16f, 0.10f, 0.06f, 1f), 0.18f);
        Material metalMaterial = CreateOrLoadMaterial("Stage01_Dark_Harbor_Metal", new Color(0.12f, 0.12f, 0.13f, 1f), 0.35f);

        Transform deckRoot = CreateChild(roads, "Stage01_1_Continuous_Pier_Deck");

        CreatePrimitiveObject(
            context,
            deckRoot,
            "Stage01_1_Pier_Underframe",
            PrimitiveType.Cube,
            new Vector3(0f, -0.28f, centerZ),
            new Vector3(DeckWidth, 0.22f, deckLength),
            deckMaterial);

        Material[] plankMaterials = { deckMaterial, deckLightMaterial, deckDarkMaterial };
        const int plankLaneCount = 7;
        const float plankGap = 0.08f;
        float plankWidth = ((DeckWidth - 0.9f) - (plankLaneCount - 1) * plankGap) / plankLaneCount;
        float plankSpan = 3.08f;
        float plankLength = plankSpan - 0.12f;
        int plankRowCount = Mathf.CeilToInt(deckLength / plankSpan);
        float firstPlankZ = centerZ - deckLength * 0.5f + plankSpan * 0.5f;
        float firstPlankX = -((plankLaneCount - 1) * (plankWidth + plankGap)) * 0.5f;

        for (int row = 0; row < plankRowCount; row++)
        {
            float z = firstPlankZ + row * plankSpan;
            for (int lane = 0; lane < plankLaneCount; lane++)
            {
                float x = firstPlankX + lane * (plankWidth + plankGap);
                Material plankMaterial = plankMaterials[(row + lane) % plankMaterials.Length];
                float heightOffset = ((row + lane) % 2) * 0.01f;
                CreatePrimitiveObject(
                    context,
                    deckRoot,
                    $"Deck_Plank_Row_{row:00}_Lane_{lane:00}",
                    PrimitiveType.Cube,
                    new Vector3(x, -0.015f + heightOffset, z),
                    new Vector3(plankWidth, 0.11f, plankLength),
                    plankMaterial);
            }
        }

        CreatePrimitiveObject(
            context,
            deckRoot,
            "Deck_Left_Edge_Beam",
            PrimitiveType.Cube,
            new Vector3(-DeckWidth * 0.5f, 0.08f, centerZ),
            new Vector3(0.34f, 0.42f, deckLength),
            darkWoodMaterial);

        CreatePrimitiveObject(
            context,
            deckRoot,
            "Deck_Right_Edge_Beam",
            PrimitiveType.Cube,
            new Vector3(DeckWidth * 0.5f, 0.08f, centerZ),
            new Vector3(0.34f, 0.42f, deckLength),
            darkWoodMaterial);

        CreatePrimitiveObject(
            context,
            deckRoot,
            "Deck_Left_Long_Seam",
            PrimitiveType.Cube,
            new Vector3(-1.7f, 0.025f, centerZ),
            new Vector3(0.025f, 0.025f, deckLength - 1.2f),
            darkWoodMaterial);

        CreatePrimitiveObject(
            context,
            deckRoot,
            "Deck_Right_Long_Seam",
            PrimitiveType.Cube,
            new Vector3(1.7f, 0.025f, centerZ),
            new Vector3(0.025f, 0.025f, deckLength - 1.2f),
            darkWoodMaterial);

        int seamCount = Mathf.RoundToInt(deckLength / 3.2f);
        float firstSeamZ = centerZ - deckLength * 0.5f + 1.6f;
        for (int i = 0; i < seamCount; i++)
        {
            float z = firstSeamZ + i * 3.2f;
            CreatePrimitiveObject(
                context,
                deckRoot,
                $"Deck_Cross_Seam_{i:00}",
                PrimitiveType.Cube,
                new Vector3(0f, 0.035f, z),
                new Vector3(DeckWidth - 0.85f, 0.03f, 0.035f),
                darkWoodMaterial);

            if (i % 2 == 0)
            {
                CreatePrimitiveObject(
                    context,
                    deckRoot,
                    $"Deck_Left_Bolt_Row_{i:00}",
                    PrimitiveType.Cube,
                    new Vector3(-4.25f, 0.075f, z),
                    new Vector3(0.22f, 0.06f, 0.22f),
                    metalMaterial);

                CreatePrimitiveObject(
                    context,
                    deckRoot,
                    $"Deck_Right_Bolt_Row_{i:00}",
                    PrimitiveType.Cube,
                    new Vector3(4.25f, 0.075f, z),
                    new Vector3(0.22f, 0.06f, 0.22f),
                    metalMaterial);
            }
        }

        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 position = new Vector3(0f, 0f, i * RoadSpan);
            nodes.Add(new RoadNode(position, yaw, "Stage01_1 Straight Pier"));
        }

        return nodes;
    }

    private static void PlaceBoundaries(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        Material postMaterial = CreateOrLoadMaterial("Stage01_Rope_Post_Wood", new Color(0.33f, 0.19f, 0.09f, 1f), 0.2f);
        Material ropeMaterial = CreateOrLoadMaterial("Stage01_Harbor_Rope", new Color(0.72f, 0.58f, 0.36f, 1f), 0.12f);

        for (int i = 0; i < nodes.Count; i += 2)
        {
            RoadNode node = nodes[i];

            CreatePrimitiveObject(
                context,
                parent,
                $"Left_Rope_Post_{i:00}",
                PrimitiveType.Cylinder,
                Side(node, -RoadSideOffset) + Vector3.up * 0.55f,
                new Vector3(0.16f, 0.55f, 0.16f),
                postMaterial);

            CreatePrimitiveObject(
                context,
                parent,
                $"Right_Rope_Post_{i:00}",
                PrimitiveType.Cylinder,
                Side(node, RoadSideOffset) + Vector3.up * 0.55f,
                new Vector3(0.16f, 0.55f, 0.16f),
                postMaterial);

            if (i < nodes.Count - 2)
            {
                float strandZ = node.Position.z + RoadSpan;
                CreatePrimitiveObject(
                    context,
                    parent,
                    $"Left_Rope_Strand_{i:00}",
                    PrimitiveType.Cube,
                    new Vector3(-RoadSideOffset, 0.78f, strandZ),
                    new Vector3(0.07f, 0.07f, RoadSpan * 1.7f),
                    ropeMaterial);

                CreatePrimitiveObject(
                    context,
                    parent,
                    $"Right_Rope_Strand_{i:00}",
                    PrimitiveType.Cube,
                    new Vector3(RoadSideOffset, 0.78f, strandZ),
                    new Vector3(0.07f, 0.07f, RoadSpan * 1.7f),
                    ropeMaterial);
            }

            if (i == 4 || i == 8)
                InstantiateStagePrefab(context, "024", parent, Side(node, RoadSideOffset - 0.25f, 0.25f), node.Yaw + 18f, $"life_ring_on_right_rail_{i:00}", 0f, 1.15f, 0f);
        }
    }

    private static void PlaceMarketBuildings(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        PlaceMarketBuilding(context, parent, nodes[2], -1, "014", "left_market_facade_near", -0.7f, 4.2f, -3f);
        PlaceMarketBuilding(context, parent, nodes[2], 1, "015", "right_sashimi_restaurant_near", 1.0f, 4.0f, 4f);
        PlaceMarketBuilding(context, parent, nodes[4], -1, "016", "left_seafood_display_mid", 0.4f, 3.8f, 3f);
        PlaceMarketBuilding(context, parent, nodes[5], 1, "043", "right_market_awning_mid", -0.3f, 3.6f, -5f);
        PlaceMarketBuilding(context, parent, nodes[7], -1, "021", "left_aquarium_row_far", 0.8f, 2.8f, 2f);
        PlaceMarketBuilding(context, parent, nodes[8], 1, "014", "right_market_facade_far", 1.0f, 4.0f, -4f);
        PlaceMarketBuilding(context, parent, nodes[9], -1, "015", "left_sashimi_endcap_far", 1.4f, 4.0f, 5f);
        PlaceMarketBuilding(context, parent, nodes[9], 1, "016", "right_seafood_endcap_far", 0.6f, 3.8f, -3f);

        InstantiateStagePrefab(context, "028", parent, Side(nodes[4], -BuildingSideOffset + 0.75f, -1.1f), nodes[4].Yaw + 4f, "left_crab_mascot_market_sign", 0f, 2.4f, 0f);
        InstantiateStagePrefab(context, "029", parent, Side(nodes[6], BuildingSideOffset - 0.75f, 0.8f), nodes[6].Yaw + 176f, "right_fish_mascot_market_sign", 0f, 2.4f, 0f);
    }

    private static void PlaceMarketBuilding(
        BuildContext context,
        Transform parent,
        RoadNode node,
        int side,
        string prefix,
        string name,
        float forwardOffset,
        float targetHeight,
        float yawOffset)
    {
        float yaw = node.Yaw + (side > 0 ? 180f : 0f) + yawOffset;
        Vector3 position = Side(node, side * BuildingSideOffset, forwardOffset);
        InstantiateStagePrefab(context, prefix, parent, position, yaw, name, 0f, targetHeight, 0f);
    }

    private static void PlacePropVariation(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        PlaceEdgeProp(context, parent, nodes[1], -1, "005", "left_foreground_cone", 4.75f, -1.6f, 0.95f, -8f);
        PlaceEdgeProp(context, parent, nodes[1], 1, "035", "right_foreground_anchor", 4.95f, -1.1f, 1.55f, 25f);
        PlaceEdgeProp(context, parent, nodes[2], -1, "001", "left_blue_fish_crate", 4.65f, 0.3f, 1.25f, -10f);
        PlaceEdgeProp(context, parent, nodes[2], 1, "002", "right_styrofoam_box", 4.65f, 0.9f, 1.1f, 8f);
        PlaceEdgeProp(context, parent, nodes[3], -1, "020", "left_fish_box_stack", 4.75f, 0.1f, 1.35f, 5f);
        PlaceEdgeProp(context, parent, nodes[3], 1, "032", "right_blue_crate_stack", 4.85f, 1.4f, 1.3f, -12f);
        PlaceEdgeProp(context, parent, nodes[5], -1, "003", "left_ice_fish_tank", 4.7f, -0.6f, 1.45f, -5f);
        PlaceEdgeProp(context, parent, nodes[5], 1, "033", "right_white_fish_crate", 4.85f, 1.0f, 1.2f, 12f);
        PlaceEdgeProp(context, parent, nodes[7], -1, "022", "left_ice_box_stack", 4.75f, 0.4f, 1.35f, 6f);
        PlaceEdgeProp(context, parent, nodes[7], 1, "025", "right_tire_fender", 4.95f, -0.5f, 1.15f, -18f);
        PlaceEdgeProp(context, parent, nodes[8], -1, "034", "left_fishing_net_pile", 4.95f, 1.1f, 0.85f, 15f);
        PlaceEdgeProp(context, parent, nodes[8], 1, "026", "right_dock_cleat", 4.85f, 0.2f, 0.45f, -12f);

        InstantiateStagePrefab(context, "039", parent, Side(nodes[2], -4.25f, -0.35f), nodes[2].Yaw, "left_edge_ice_scatter", 0f, 0.28f, 0.03f);
        InstantiateStagePrefab(context, "040", parent, Side(nodes[6], 4.25f, 0.5f), nodes[6].Yaw, "right_edge_fish_scrap_scatter", 0f, 0.28f, 0.03f);
    }

    private static void PlaceEdgeProp(
        BuildContext context,
        Transform parent,
        RoadNode node,
        int side,
        string prefix,
        string name,
        float sideOffset,
        float forwardOffset,
        float targetHeight,
        float yawOffset)
    {
        InstantiateStagePrefab(context, prefix, parent, Side(node, side * sideOffset, forwardOffset), node.Yaw + yawOffset, name, 0f, targetHeight, 0f);
    }

    private static void PlaceGameplay(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        InstantiateStagePrefab(context, "045", parent, nodes[7].Position + Vector3.up * 0.05f, nodes[7].Yaw, "overhead_lane_signal_gate", 0f, 5.0f, 0f);
        InstantiateStagePrefab(context, "044", parent, Side(nodes[8], 3.0f, 1.15f), nodes[8].Yaw, "right_shoulder_barricade_reference", 0f, 1.45f, 0f);
        InstantiateStagePrefab(context, "004", parent, Side(nodes[4], -3.05f, 1.1f), nodes[4].Yaw - 7f, "left_shoulder_seafood_cart_offset", 0f, 1.2f, 0f);

        for (int i = 0; i < 9; i++)
        {
            float z = nodes[1].Position.z + 3.2f + i * 3.15f;
            Vector3 position = new Vector3(0f, 0f, z);
            InstantiateStagePrefab(context, "009", parent, position + Vector3.up * 0.04f, 0f, $"center_pickup_line_{i:00}", 0f, 0.55f, 0.05f);
        }
    }

    private static void PlaceBackground(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        RoadNode mid = nodes[5];
        RoadNode far = nodes[nodes.Count - 1];
        float centerZ = (nodes[0].Position.z + far.Position.z) * 0.5f;
        float waterLength = far.Position.z + RoadSpan * 2f;
        Material waterMaterial = CreateOrLoadMaterial("Stage01_Clear_Harbor_Water", new Color(0.03f, 0.48f, 0.74f, 0.84f), 0.68f);

        CreatePrimitiveObject(
            context,
            parent,
            "Harbor_Water_Left_Flat_Plane",
            PrimitiveType.Plane,
            new Vector3(-WaterSideOffset, -0.34f, centerZ),
            new Vector3(0.95f, 1f, waterLength * 0.1f),
            waterMaterial);

        CreatePrimitiveObject(
            context,
            parent,
            "Harbor_Water_Right_Flat_Plane",
            PrimitiveType.Plane,
            new Vector3(WaterSideOffset, -0.34f, centerZ),
            new Vector3(0.95f, 1f, waterLength * 0.1f),
            waterMaterial);

        CreatePrimitiveObject(
            context,
            parent,
            "Harbor_Water_Back_Flat_Plane",
            PrimitiveType.Plane,
            new Vector3(0f, -0.36f, far.Position.z + RoadSpan * 1.25f),
            new Vector3(3.3f, 1f, 1.2f),
            waterMaterial);

        InstantiateStagePrefab(context, "018", parent, Side(nodes[4], -WaterSideOffset - 1.8f, 5.6f), mid.Yaw + 18f, "large_fishing_boat_left", 0f, 4.8f, -0.12f);
        InstantiateStagePrefab(context, "018", parent, Side(nodes[6], WaterSideOffset + 1.7f, 7.2f), mid.Yaw - 22f, "large_fishing_boat_right", 0f, 4.4f, -0.12f);
        InstantiateStagePrefab(context, "019", parent, far.Position + Forward(far.Yaw) * 18f + Right(far.Yaw) * -13.5f, far.Yaw, "distant_hillside_village", 22f, 0f, -0.08f);
        InstantiateStagePrefab(context, "037", parent, Side(nodes[5], -WaterSideOffset + 2.1f, -1.2f), mid.Yaw, "left_red_buoy", 0f, 1.05f, -0.1f);
        InstantiateStagePrefab(context, "038", parent, Side(nodes[3], WaterSideOffset - 2.0f, 1.9f), mid.Yaw + 18f, "right_floating_plank", 0f, 0.55f, -0.1f);
        InstantiateStagePrefab(context, "041", parent, Side(nodes[8], -5.7f, 1.2f) + Vector3.up * 6.2f, nodes[8].Yaw, "left_flying_gull", 0f, 0.95f, 0f);
        InstantiateStagePrefab(context, "041", parent, Side(nodes[9], 5.5f, 0.7f) + Vector3.up * 6.8f, nodes[9].Yaw + 14f, "right_flying_gull", 0f, 0.85f, 0f);
    }

    private static void PlacePlayerPreview(BuildContext context, List<RoadNode> nodes, Transform parent)
    {
        const string playerModel = "Assets/ShooterSurvival/Models/Player/Shark5/Shark2.fbx";
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(playerModel);
        if (model == null)
            return;

        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(model);

        RoadNode start = nodes[0];
        instance.name = "Player_Blue_Shark_Preview";
        instance.transform.SetParent(parent, false);
        instance.transform.position = start.Position - Forward(start.Yaw) * 4.3f + Vector3.up * 0.05f;
        instance.transform.rotation = Quaternion.Euler(0f, start.Yaw, 0f);
        FitHeight(instance, 1.8f);
        AlignBottom(instance, 0f);
        context.Placed++;
    }

    private static void CreateLightingAndCamera(List<RoadNode> nodes)
    {
        GameObject light = new GameObject("Directional Light - Harbor Sun");
        Light directional = light.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 1.4f;
        directional.color = new Color(1f, 0.92f, 0.78f, 1f);
        light.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

        Bounds bounds = new Bounds(nodes[0].Position, Vector3.one);
        foreach (RoadNode node in nodes)
            bounds.Encapsulate(node.Position);

        Vector3 center = bounds.center;
        GameObject cameraObject = new GameObject("Camera - Stage01_1 Reference Overview");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(18f, bounds.size.z * 0.42f);
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.transform.position = center + new Vector3(0f, 34f, -28f);
        cameraObject.transform.LookAt(center);

        GameObject runnerCameraObject = new GameObject("Camera - Runner 9x16 Preview");
        Camera runnerCamera = runnerCameraObject.AddComponent<Camera>();
        runnerCamera.fieldOfView = 64f;
        runnerCameraObject.transform.position = nodes[0].Position + new Vector3(0f, 4.9f, -8.6f);
        runnerCameraObject.transform.LookAt(nodes[7].Position + Vector3.up * 1.45f);
    }

    private static GameObject CreatePrimitiveObject(
        BuildContext context,
        Transform parent,
        string name,
        PrimitiveType primitiveType,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject instance = GameObject.CreatePrimitive(primitiveType);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.identity;
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

    private static Material CreateOrLoadMaterial(string materialName, Color color, float smoothness)
    {
        EnsureFolder(GeneratedMaterialRoot);

        string path = $"{GeneratedMaterialRoot}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

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

    private static GameObject InstantiateStagePrefab(
        BuildContext context,
        string prefix,
        Transform parent,
        Vector3 position,
        float yaw,
        string name,
        float targetMaxXZ,
        float targetHeight,
        float groundY)
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

        if (targetMaxXZ > 0f)
            FitMaxXZ(instance, targetMaxXZ);

        if (targetHeight > 0f)
            FitHeight(instance, targetHeight);

        AlignBottom(instance, groundY);

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

    private static void FitMaxXZ(GameObject instance, float target)
    {
        if (!TryGetBounds(instance, out Bounds bounds))
            return;

        float size = Mathf.Max(bounds.size.x, bounds.size.z);
        if (size <= 0.001f)
            return;

        float scale = target / size;
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
            $"MissingPrefabs: {context.Missing}\n" +
            context.Report);
    }

    private readonly struct RoadNode
    {
        public RoadNode(Vector3 position, float yaw, string section)
        {
            Position = position;
            Yaw = yaw;
            Section = section;
        }

        public readonly Vector3 Position;
        public readonly float Yaw;
        public readonly string Section;
    }

    private sealed class BuildContext
    {
        public int Placed;
        public int Missing;
        public readonly StringBuilder Report = new StringBuilder();
        public readonly Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>();
    }
}
#endif
