#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public sealed class NoryangjinMapStaticOptimizerTests
{
    private const string Map1Path =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";
    private const string Map2Path =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity";
    private const string LowCostWaterMaterialPath =
        "Assets/ShooterSurvival/Materials/Env/HarborWater_Unlit_Tiled.mat";

    [Test]
    public void OptimizeScene_MarksOnlySafeEnvironmentRenderers()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject mapRoot = CreateRoot(scene, "Noryangjin_MapTool");
            Transform roads = CreateChild(mapRoot.transform, "Roads").transform;
            Transform props = CreateChild(mapRoot.transform, "Props").transform;
            CreateChild(mapRoot.transform, "Water");
            GameObject floor = CreateChild(mapRoot.transform, "MapTool_Work_Floor");
            GameObject grid = CreateChild(mapRoot.transform, "MapTool_Work_Grid");
            GameObject origin = CreateChild(mapRoot.transform, "MapTool_Origin_Post");

            GameObject safeRoad = CreateRenderableChild(roads, "Road_Safe");
            GameObject safeProp = CreateRenderableChild(props, "Prop_Safe");
            GameObject animatedProp = CreateRenderableChild(props, "Prop_Animated");
            animatedProp.AddComponent<Animator>();
            GameObject semanticEnemy =
                CreateRenderableChild(props, "010_STAGE01_NRY_ENEMY_010_Puffer_enemy");

            GameObjectUtility.SetStaticEditorFlags(
                safeProp,
                StaticEditorFlags.ReflectionProbeStatic);
            GameObjectUtility.SetStaticEditorFlags(
                animatedProp,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.ReflectionProbeStatic);
            animatedProp.GetComponent<MeshRenderer>()
                .motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;

            NoryangjinMapOptimizationReport report =
                NoryangjinMapStaticOptimizer.OptimizeScene(
                    scene,
                    recordUndo: false);

            Assert.That(report.EligibleStaticRenderers, Is.EqualTo(2));
            Assert.That(report.StaticRenderersChanged, Is.EqualTo(2));
            Assert.That(report.DynamicRootsSkipped, Is.EqualTo(2));
            Assert.That(report.DynamicBatchingFlagsCleared, Is.EqualTo(1));
            Assert.That(report.DynamicRendererPoliciesRestored, Is.EqualTo(1));
            AssertBatchingStatic(safeRoad, true);
            AssertBatchingStatic(safeProp, true);
            AssertBatchingStatic(animatedProp, false);
            AssertBatchingStatic(semanticEnemy, false);
            Assert.That(
                GameObjectUtility.GetStaticEditorFlags(safeProp)
                    .HasFlag(StaticEditorFlags.ReflectionProbeStatic),
                Is.True);
            Assert.That(
                GameObjectUtility.GetStaticEditorFlags(animatedProp)
                    .HasFlag(StaticEditorFlags.ReflectionProbeStatic),
                Is.True);
            Assert.That(
                animatedProp.GetComponent<MeshRenderer>()
                    .motionVectorGenerationMode,
                Is.EqualTo(MotionVectorGenerationMode.Object));
            Assert.That(safeRoad.GetComponent<MeshRenderer>()
                    .motionVectorGenerationMode,
                Is.EqualTo(MotionVectorGenerationMode.ForceNoMotion));
            Assert.That(floor.CompareTag("EditorOnly"), Is.True);
            Assert.That(grid.CompareTag("EditorOnly"), Is.True);
            Assert.That(origin.CompareTag("EditorOnly"), Is.True);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void OptimizeScene_IsIdempotentAndDisablesCameraCopies()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject mapRoot = CreateRoot(scene, "Noryangjin_MapTool");
            Transform roads = CreateChild(mapRoot.transform, "Roads").transform;
            CreateChild(mapRoot.transform, "Props");
            CreateChild(mapRoot.transform, "Water");
            CreateChild(mapRoot.transform, "MapTool_Work_Floor");
            CreateChild(mapRoot.transform, "MapTool_Work_Grid");
            CreateChild(mapRoot.transform, "MapTool_Origin_Post");
            CreateRenderableChild(roads, "Road_Safe");

            GameObject cameraObject = CreateRoot(scene, "MapTool_Camera");
            cameraObject.AddComponent<Camera>();
            UniversalAdditionalCameraData cameraData =
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            cameraData.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
            cameraData.renderPostProcessing = true;

            GameObject effectCameraObject = CreateRoot(scene, "EffectCamera");
            effectCameraObject.AddComponent<Camera>();
            UniversalAdditionalCameraData effectCameraData =
                effectCameraObject.AddComponent<UniversalAdditionalCameraData>();
            effectCameraData.requiresDepthOption =
                CameraOverrideOption.UsePipelineSettings;
            effectCameraData.requiresColorOption =
                CameraOverrideOption.UsePipelineSettings;
            effectCameraData.renderPostProcessing = true;

            NoryangjinMapOptimizationReport first =
                NoryangjinMapStaticOptimizer.OptimizeScene(
                    scene,
                    recordUndo: false);
            NoryangjinMapOptimizationReport second =
                NoryangjinMapStaticOptimizer.OptimizeScene(
                    scene,
                    recordUndo: false);

            Assert.That(first.CameraOverridesChanged, Is.EqualTo(1));
            Assert.That(first.HasSceneChanges, Is.True);
            Assert.That(second.HasSceneChanges, Is.False);
            Assert.That(cameraData.requiresDepthOption, Is.EqualTo(CameraOverrideOption.Off));
            Assert.That(cameraData.requiresColorOption, Is.EqualTo(CameraOverrideOption.Off));
            Assert.That(cameraData.renderPostProcessing, Is.False);
            Assert.That(
                effectCameraData.requiresDepthOption,
                Is.EqualTo(CameraOverrideOption.UsePipelineSettings));
            Assert.That(
                effectCameraData.requiresColorOption,
                Is.EqualTo(CameraOverrideOption.UsePipelineSettings));
            Assert.That(effectCameraData.renderPostProcessing, Is.True);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void OptimizeWaterInstance_UsesTwoTriangleMeshAndCheapRendererPolicy()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            SceneManager.MoveGameObjectToScene(water, scene);
            MeshRenderer renderer = water.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            renderer.allowOcclusionWhenDynamic = true;
            BoxCollider trigger = water.AddComponent<BoxCollider>();
            trigger.isTrigger = true;

            bool changed = NoryangjinMapStaticOptimizer.OptimizeWaterInstance(
                water,
                recordUndo: false);

            Mesh mesh = water.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(changed, Is.True);
            Assert.That(
                AssetDatabase.GetAssetPath(mesh),
                Is.EqualTo(NoryangjinMapStaticOptimizer.LowPolyWaterTileMeshPath));
            Assert.That(mesh.GetIndexCount(0) / 3, Is.EqualTo(2));
            Assert.That(water.GetComponent<Collider>().enabled, Is.False);
            Assert.That(trigger.enabled, Is.True);
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(renderer.receiveShadows, Is.False);
            Assert.That(
                renderer.motionVectorGenerationMode,
                Is.EqualTo(MotionVectorGenerationMode.ForceNoMotion));
            Assert.That(renderer.lightProbeUsage, Is.EqualTo(LightProbeUsage.Off));
            Assert.That(
                renderer.reflectionProbeUsage,
                Is.EqualTo(ReflectionProbeUsage.Off));
            Assert.That(renderer.allowOcclusionWhenDynamic, Is.False);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void OptimizeWaterInstance_OverridesOceanInstanceWithoutChangingSourcePrefab()
    {
        const string oceanPrefabPath =
            "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/" +
            "017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop/" +
            "017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop.prefab";

        GameObject sourcePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(oceanPrefabPath);
        Assert.That(sourcePrefab, Is.Not.Null);
        Mesh sourceMesh =
            sourcePrefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
        Material sourceMaterial =
            sourcePrefab.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;

        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(
                sourcePrefab,
                scene) as GameObject;
            Assert.That(instance, Is.Not.Null);

            bool changed = NoryangjinMapStaticOptimizer.OptimizeWaterInstance(
                instance,
                recordUndo: false);

            MeshFilter filter = instance.GetComponentInChildren<MeshFilter>(true);
            MeshRenderer renderer =
                instance.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(changed, Is.True);
            Assert.That(
                AssetDatabase.GetAssetPath(filter.sharedMesh),
                Is.EqualTo(NoryangjinMapStaticOptimizer.LowPolyOceanMeshPath));
            Assert.That(
                AssetDatabase.GetAssetPath(renderer.sharedMaterial),
                Is.EqualTo(LowCostWaterMaterialPath));
            Assert.That(
                sourcePrefab.GetComponentInChildren<MeshFilter>(true).sharedMesh,
                Is.SameAs(sourceMesh));
            Assert.That(
                sourcePrefab.GetComponentInChildren<MeshRenderer>(true).sharedMaterial,
                Is.SameAs(sourceMaterial));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [TestCase("Thing_BaseColor.png", 1024)]
    [TestCase("Thing_Normal.png", 1024)]
    [TestCase("Thing_Emission.png", 512)]
    [TestCase("Thing_Metallic.png", 512)]
    [TestCase("Thing_Roughness.png", 512)]
    [TestCase("Thing_Mask.png", 512)]
    public void ResolveAndroidTextureMaxSize_UsesMobileDetailBudget(
        string fileName,
        int expected)
    {
        Assert.That(
            NoryangjinMapStaticOptimizer.ResolveAndroidTextureMaxSize(
                $"Assets/Textures/{fileName}"),
            Is.EqualTo(expected));
    }

    [Test]
    public void AuthoredScenes_KeepOnlyEnvironmentStaticAndUseLowCostWater()
    {
        Scene previousActive = SceneManager.GetActiveScene();
        Scene map1 = SceneManager.GetSceneByPath(Map1Path);
        bool openedMap1 = !map1.IsValid() || !map1.isLoaded;
        Scene map2 = SceneManager.GetSceneByPath(Map2Path);
        bool openedMap2 = !map2.IsValid() || !map2.isLoaded;

        if (openedMap1)
            map1 = EditorSceneManager.OpenScene(Map1Path, OpenSceneMode.Additive);
        if (openedMap2)
            map2 = EditorSceneManager.OpenScene(Map2Path, OpenSceneMode.Additive);

        try
        {
            AssertSceneContract(map1, expectedStaticRenderers: 184);
            AssertSceneContract(map2, expectedStaticRenderers: 653);

            MeshRenderer[] oceanRenderers = FindRoot(map1, "Noryangjin_MapTool")
                .transform.Find("Props")
                .Cast<Transform>()
                .Where(child => child.name.Contains(
                    "_NRY_BG_001_Ocean_water_plane_backdrop",
                    StringComparison.Ordinal))
                .SelectMany(child => child.GetComponentsInChildren<MeshRenderer>(true))
                .ToArray();
            Assert.That(oceanRenderers.Length, Is.EqualTo(118));
            Assert.That(
                oceanRenderers.All(renderer =>
                    renderer.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0) / 3 == 2),
                Is.True);
            Assert.That(
                oceanRenderers.All(renderer =>
                    AssetDatabase.GetAssetPath(renderer.sharedMaterial) ==
                    LowCostWaterMaterialPath),
                Is.True);

            MeshFilter[] map2Water = FindRoot(map2, "Noryangjin_MapTool")
                .transform.Find("Props")
                .Cast<Transform>()
                .Where(child => child.name.StartsWith(
                    "Mode2_Water_",
                    StringComparison.Ordinal))
                .SelectMany(child => child.GetComponentsInChildren<MeshFilter>(true))
                .ToArray();
            Assert.That(map2Water.Length, Is.EqualTo(196));
            Assert.That(
                map2Water.All(filter => filter.sharedMesh.GetIndexCount(0) / 3 == 2),
                Is.True);
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded)
                EditorSceneManager.SetActiveScene(previousActive);
            if (openedMap2 && map2.IsValid() && map2.isLoaded)
                EditorSceneManager.CloseScene(map2, true);
            if (openedMap1 && map1.IsValid() && map1.isLoaded)
                EditorSceneManager.CloseScene(map1, true);
        }
    }

    private static void AssertSceneContract(
        Scene scene,
        int expectedStaticRenderers)
    {
        GameObject mapRoot = FindRoot(scene, "Noryangjin_MapTool");
        Transform roads = mapRoot.transform.Find("Roads");
        Transform props = mapRoot.transform.Find("Props");
        MeshRenderer[] environmentRenderers = roads
            .GetComponentsInChildren<MeshRenderer>(true)
            .Concat(props.GetComponentsInChildren<MeshRenderer>(true))
            .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
            .ToArray();

        Assert.That(environmentRenderers.Length, Is.EqualTo(expectedStaticRenderers));
        Assert.That(
            environmentRenderers.Count(renderer =>
                GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)
                    .HasFlag(StaticEditorFlags.BatchingStatic)),
            Is.EqualTo(expectedStaticRenderers));

        foreach (Transform guide in new[]
                 {
                     mapRoot.transform.Find("MapTool_Work_Floor"),
                     mapRoot.transform.Find("MapTool_Work_Grid"),
                     mapRoot.transform.Find("MapTool_Origin_Post")
                 })
        {
            Assert.That(guide, Is.Not.Null);
            Assert.That(guide.CompareTag("EditorOnly"), Is.True);
        }

        foreach (GameObject root in scene.GetRootGameObjects()
                     .Where(root => root.name != "Noryangjin_MapTool"))
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                AssertBatchingStatic(transform.gameObject, false);
        }

        foreach (MonoBehaviour dynamicBehaviour in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                     .Where(behaviour => behaviour != null &&
                                         behaviour.transform.IsChildOf(props)))
        {
            foreach (MeshRenderer renderer in
                     dynamicBehaviour.GetComponentsInChildren<MeshRenderer>(true))
            {
                AssertBatchingStatic(renderer.gameObject, false);
            }
        }

        UniversalAdditionalCameraData[] cameras = scene.GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<UniversalAdditionalCameraData>(true))
            .ToArray();
        Assert.That(cameras, Is.Not.Empty);
        Assert.That(
            cameras.All(camera =>
                camera.requiresDepthOption == CameraOverrideOption.Off &&
                camera.requiresColorOption == CameraOverrideOption.Off &&
                !camera.renderPostProcessing),
            Is.True);
    }

    private static GameObject CreateRoot(Scene scene, string name)
    {
        var root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static GameObject CreateRenderableChild(Transform parent, string name)
    {
        GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
        child.name = name;
        child.transform.SetParent(parent, false);
        UnityEngine.Object.DestroyImmediate(child.GetComponent<Collider>());
        return child;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .Single(root => string.Equals(root.name, name, StringComparison.Ordinal));
    }

    private static void AssertBatchingStatic(GameObject gameObject, bool expected)
    {
        bool actual = GameObjectUtility.GetStaticEditorFlags(gameObject)
            .HasFlag(StaticEditorFlags.BatchingStatic);
        Assert.That(actual, Is.EqualTo(expected), gameObject.name);
    }
}
#endif
