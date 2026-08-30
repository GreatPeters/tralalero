#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class NoryangjinMapStaticOptimizer
{
    internal const string Map2ScenePath =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity";
    internal const string LowPolyOceanMeshPath =
        "Assets/ShooterSurvival/Meshes/Generated/Noryangjin_OceanWater_LowPoly.asset";
    internal const string LowPolyWaterTileMeshPath =
        "Assets/ShooterSurvival/Meshes/Generated/Noryangjin_WaterTile_LowPoly.asset";

    private const string MapRootName = "Noryangjin_MapTool";
    private const string RoadsName = "Roads";
    private const string PropsName = "Props";
    private const string WaterName = "Water";
    private const string WorkFloorName = "MapTool_Work_Floor";
    private const string WorkGridName = "MapTool_Work_Grid";
    private const string OriginPostName = "MapTool_Origin_Post";
    private const string MapToolCameraName = "MapTool_Camera";
    private const string Mode2WaterPrefix = "Mode2_Water_";
    private const string OceanWaterToken = "_NRY_BG_001_Ocean_water_plane_backdrop";
    private const string AndroidPlatformName = "Android";
    private const string NoryangjinTextureRoot =
        "Assets/ShooterSurvival/Textures/MeshyAI/Stage01_Noryangjin/";
    private const string OceanSourceModelPath =
        "Assets/ShooterSurvival/Models/MeshyAI/Stage01_Noryangjin/" +
        "017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop/" +
        "017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop.fbx";
    private const string LowCostWaterMaterialPath =
        "Assets/ShooterSurvival/Materials/Env/HarborWater_Unlit_Tiled.mat";
    private const string Map2WaterMaterialPath =
        "Assets/ShooterSurvival/Materials/Generated/Noryangjin_Map2_Water.mat";

    private static readonly string[] EnvironmentParentNames =
    {
        RoadsName,
        PropsName,
        WaterName
    };

    private static readonly string[] EditorOnlyGuideNames =
    {
        WorkFloorName,
        WorkGridName,
        OriginPostName
    };

    public static void OptimizeCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Noryangjin Optimization] 플레이 모드에서는 씬을 최적화할 수 없습니다.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!IsNoryangjinMapScene(scene))
        {
            Debug.LogError("[Noryangjin Optimization] 먼저 노량진 맵툴 씬을 여세요.");
            return;
        }

        EnsureSharedWaterAssets();
        NoryangjinMapOptimizationReport report = OptimizeScene(scene, recordUndo: true);
        int textureImportersChanged = OptimizeAndroidTexturesUsedByScenes(new[] { scene });
        bool? streamingChanged = EnableMobileTextureStreaming();

        if (report.HasSceneChanges)
            EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(BuildSummary(
            new[] { report },
            textureImportersChanged,
            streamingChanged,
            savedScenes: false));
    }

    public static void OptimizeAllNoryangjinScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Noryangjin Optimization] 플레이 모드에서는 씬을 최적화할 수 없습니다.");
            return;
        }

        string[] targetPaths =
        {
            NoryangjinMapToolWindow.MapToolScenePath,
            Map2ScenePath
        };

        foreach (string path in targetPaths)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded && loaded.isDirty)
            {
                Debug.LogError(
                    $"[Noryangjin Optimization] 저장되지 않은 씬은 자동 최적화하지 않습니다: {path}");
                return;
            }
        }

        EnsureSharedWaterAssets();

        Scene previousActive = SceneManager.GetActiveScene();
        var reports = new List<NoryangjinMapOptimizationReport>();
        var optimizedScenes = new List<Scene>();
        var openedScenes = new List<Scene>();
        try
        {
            foreach (string path in targetPaths)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    openedScenes.Add(scene);
                }

                NoryangjinMapOptimizationReport report =
                    OptimizeScene(scene, recordUndo: false);
                reports.Add(report);
                optimizedScenes.Add(scene);

                if (report.HasSceneChanges && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException($"씬 저장 실패: {path}");
            }

            int textureImportersChanged =
                OptimizeAndroidTexturesUsedByScenes(optimizedScenes);
            bool? streamingChanged = EnableMobileTextureStreaming();
            Debug.Log(BuildSummary(
                reports,
                textureImportersChanged,
                streamingChanged,
                savedScenes: true));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded)
                EditorSceneManager.SetActiveScene(previousActive);

            foreach (Scene openedScene in openedScenes)
            {
                if (openedScene.IsValid() && openedScene.isLoaded && !openedScene.isDirty)
                    EditorSceneManager.CloseScene(openedScene, true);
            }
        }
    }

    internal static NoryangjinMapOptimizationReport OptimizeScene(
        Scene scene,
        bool recordUndo)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            throw new ArgumentException("A valid loaded scene is required.", nameof(scene));

        var report = new NoryangjinMapOptimizationReport(scene.path);
        GameObject mapRoot = FindRoot(scene, MapRootName);
        if (mapRoot == null)
            return report;

        if (string.Equals(
                scene.path,
                Map2ScenePath,
                StringComparison.OrdinalIgnoreCase))
        {
            DisableMap2WaterOutline();
        }

        report.GuideRootsTaggedEditorOnly +=
            EnsureEditorOnlyGuideTags(mapRoot.transform, recordUndo);
        report.CameraOverridesChanged +=
            OptimizeCameras(scene, recordUndo);

        foreach (string parentName in EnvironmentParentNames)
        {
            Transform parent = mapRoot.transform.Find(parentName);
            if (parent == null)
                continue;

            foreach (Transform placedRoot in parent)
                OptimizePlacedRoot(placedRoot.gameObject, recordUndo, report);
        }

        if (report.HasSceneChanges)
            EditorSceneManager.MarkSceneDirty(scene);

        return report;
    }

    internal static bool OptimizePlacedRoot(GameObject placedRoot, bool recordUndo)
    {
        if (placedRoot == null)
            return false;

        var report = new NoryangjinMapOptimizationReport(
            placedRoot.scene.IsValid() ? placedRoot.scene.path : string.Empty);
        OptimizePlacedRoot(placedRoot, recordUndo, report);
        return report.HasSceneChanges;
    }

    internal static bool OptimizeWaterInstance(GameObject instance, bool recordUndo)
    {
        return OptimizeWaterInstance(instance, recordUndo, out _);
    }

    private static bool OptimizeWaterInstance(
        GameObject instance,
        bool recordUndo,
        out int meshesReplaced)
    {
        meshesReplaced = 0;
        if (instance == null)
            return false;

        bool changed = false;
        bool isOcean = IsOceanWater(instance);
        Mesh expectedOceanMesh = isOcean ? EnsureLowPolyOceanMesh() : null;
        Mesh lowPolyTile = isOcean ? null : EnsureLowPolyWaterTileMesh();
        foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;

            Mesh expectedMesh = isOcean
                ? expectedOceanMesh
                : string.Equals(filter.sharedMesh.name, "Plane", StringComparison.Ordinal)
                    ? lowPolyTile
                    : null;
            if (expectedMesh == null || filter.sharedMesh == expectedMesh)
            {
                continue;
            }

            RecordObject(filter, recordUndo, "Use low-poly water mesh");
            filter.sharedMesh = expectedMesh;
            RecordPrefabOverride(filter);
            EditorUtility.SetDirty(filter);
            meshesReplaced++;
            changed = true;
        }

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.enabled || collider.isTrigger)
                continue;

            RecordObject(collider, recordUndo, "Disable water collision");
            collider.enabled = false;
            RecordPrefabOverride(collider);
            EditorUtility.SetDirty(collider);
            changed = true;
        }

        Material oceanMaterial = isOcean ? LoadLowCostWaterMaterial() : null;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (oceanMaterial != null && renderer.sharedMaterial != oceanMaterial)
            {
                RecordObject(renderer, recordUndo, "Use low-cost water material");
                renderer.sharedMaterial = oceanMaterial;
                RecordPrefabOverride(renderer);
                EditorUtility.SetDirty(renderer);
                changed = true;
            }

            if (ApplyWaterRendererPolicy(renderer, recordUndo))
                changed = true;
        }

        return changed;
    }

    internal static bool IsDynamicPlacementRoot(GameObject placedRoot)
    {
        if (placedRoot == null)
            return true;

        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(placedRoot);
        if (LooksLikeEnemy(placedRoot.name) || LooksLikeEnemy(prefabPath))
            return true;

        foreach (Component component in placedRoot.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
                return true;

            if (component is MonoBehaviour ||
                component is Animator ||
                component is Animation ||
                component is Rigidbody ||
                component is Rigidbody2D ||
                component is CharacterController ||
                component is Joint ||
                component is Joint2D ||
                component is SkinnedMeshRenderer ||
                component is ParticleSystem ||
                component is TrailRenderer ||
                component is LineRenderer ||
                component is Cloth ||
                component is Light)
            {
                return true;
            }

            string typeName = component.GetType().FullName ?? string.Empty;
            if (typeName.Contains("NavMeshAgent", StringComparison.Ordinal) ||
                typeName.Contains("PlayableDirector", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static int EnsureEditorOnlyGuideTags(Transform mapRoot, bool recordUndo)
    {
        if (mapRoot == null)
            return 0;

        int changed = 0;
        foreach (string guideName in EditorOnlyGuideNames)
        {
            Transform guide = mapRoot.Find(guideName);
            if (guide == null || guide.CompareTag("EditorOnly"))
                continue;

            RecordObject(guide.gameObject, recordUndo, "Exclude map guide from builds");
            guide.gameObject.tag = "EditorOnly";
            RecordPrefabOverride(guide.gameObject);
            EditorUtility.SetDirty(guide.gameObject);
            changed++;
        }

        return changed;
    }

    internal static int ResolveAndroidTextureMaxSize(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return 1024;

        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        return fileName.EndsWith("_Emission", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith("_Metallic", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith("_Roughness", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith("_Occlusion", StringComparison.OrdinalIgnoreCase) ||
               fileName.Contains("_Mask", StringComparison.OrdinalIgnoreCase)
            ? 512
            : 1024;
    }

    private static void OptimizePlacedRoot(
        GameObject placedRoot,
        bool recordUndo,
        NoryangjinMapOptimizationReport report)
    {
        if (IsDynamicPlacementRoot(placedRoot))
        {
            report.DynamicRootsSkipped++;
            bool batchingFlagsCleared = false;
            foreach (Transform transform in placedRoot.GetComponentsInChildren<Transform>(true))
            {
                StaticEditorFlags current =
                    GameObjectUtility.GetStaticEditorFlags(transform.gameObject);
                if ((current & StaticEditorFlags.BatchingStatic) == 0)
                    continue;

                SetStaticFlags(
                    transform.gameObject,
                    current & ~StaticEditorFlags.BatchingStatic,
                    recordUndo);
                report.DynamicBatchingFlagsCleared++;
                batchingFlagsCleared = true;
            }

            if (batchingFlagsCleared)
            {
                foreach (MeshRenderer renderer in
                         placedRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.motionVectorGenerationMode !=
                        MotionVectorGenerationMode.ForceNoMotion)
                    {
                        continue;
                    }

                    RecordObject(
                        renderer,
                        recordUndo,
                        "Restore dynamic object motion vectors");
                    renderer.motionVectorGenerationMode =
                        MotionVectorGenerationMode.Object;
                    RecordPrefabOverride(renderer);
                    EditorUtility.SetDirty(renderer);
                    report.DynamicRendererPoliciesRestored++;
                }
            }

            return;
        }

        bool waterRoot = IsWaterRoot(placedRoot);
        if (waterRoot &&
            OptimizeWaterInstance(
                placedRoot,
                recordUndo,
                out int waterMeshesReplaced))
        {
            report.WaterRenderersOptimized++;
            report.WaterMeshesReplaced += waterMeshesReplaced;
        }

        foreach (MeshRenderer renderer in
                 placedRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            report.EligibleStaticRenderers++;
            StaticEditorFlags current =
                GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
            StaticEditorFlags expected = current | StaticEditorFlags.BatchingStatic;
            if (current != expected)
            {
                SetStaticFlags(renderer.gameObject, expected, recordUndo);
                report.StaticRenderersChanged++;
            }

            if (renderer.motionVectorGenerationMode !=
                MotionVectorGenerationMode.ForceNoMotion)
            {
                RecordObject(renderer, recordUndo, "Disable static object motion vectors");
                renderer.motionVectorGenerationMode =
                    MotionVectorGenerationMode.ForceNoMotion;
                RecordPrefabOverride(renderer);
                EditorUtility.SetDirty(renderer);
                report.StaticRendererPoliciesChanged++;
            }
        }
    }

    private static int OptimizeCameras(Scene scene, bool recordUndo)
    {
        int changed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (!string.Equals(
                        camera.name,
                        MapToolCameraName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                UniversalAdditionalCameraData data =
                    camera.GetComponent<UniversalAdditionalCameraData>();
                if (data == null)
                    continue;

                bool needsChange =
                    data.requiresDepthOption != CameraOverrideOption.Off ||
                    data.requiresColorOption != CameraOverrideOption.Off ||
                    data.renderPostProcessing;
                if (!needsChange)
                    continue;

                RecordObject(data, recordUndo, "Optimize Noryangjin camera");
                data.requiresDepthOption = CameraOverrideOption.Off;
                data.requiresColorOption = CameraOverrideOption.Off;
                data.renderPostProcessing = false;
                RecordPrefabOverride(data);
                EditorUtility.SetDirty(data);
                changed++;
            }
        }

        return changed;
    }

    private static bool ApplyWaterRendererPolicy(Renderer renderer, bool recordUndo)
    {
        bool needsChange =
            renderer.shadowCastingMode != ShadowCastingMode.Off ||
            renderer.receiveShadows ||
            renderer.motionVectorGenerationMode !=
                MotionVectorGenerationMode.ForceNoMotion ||
            renderer.lightProbeUsage != LightProbeUsage.Off ||
            renderer.reflectionProbeUsage != ReflectionProbeUsage.Off ||
            renderer.allowOcclusionWhenDynamic;
        if (!needsChange)
            return false;

        RecordObject(renderer, recordUndo, "Optimize water renderer");
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode =
            MotionVectorGenerationMode.ForceNoMotion;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.allowOcclusionWhenDynamic = false;
        RecordPrefabOverride(renderer);
        EditorUtility.SetDirty(renderer);
        return true;
    }

    private static bool IsWaterRoot(GameObject placedRoot)
    {
        return IsMode2Water(placedRoot) || IsOceanWater(placedRoot);
    }

    private static bool IsMode2Water(GameObject placedRoot)
    {
        return placedRoot != null &&
               placedRoot.name.StartsWith(Mode2WaterPrefix, StringComparison.Ordinal);
    }

    private static bool IsOceanWater(GameObject placedRoot)
    {
        if (placedRoot == null)
            return false;

        string prefabPath =
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(placedRoot);
        return placedRoot.name.Contains(OceanWaterToken, StringComparison.Ordinal) ||
               prefabPath.Contains(OceanWaterToken, StringComparison.Ordinal);
    }

    private static bool LooksLikeEnemy(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.Contains("_ENEMY_", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("EnemyMovementTrigger", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("/Enemy/", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("Enemy_", StringComparison.OrdinalIgnoreCase);
    }

    private static void SetStaticFlags(
        GameObject gameObject,
        StaticEditorFlags flags,
        bool recordUndo)
    {
        RecordObject(gameObject, recordUndo, "Configure static batching");
        GameObjectUtility.SetStaticEditorFlags(gameObject, flags);
        RecordPrefabOverride(gameObject);
        EditorUtility.SetDirty(gameObject);
    }

    private static void RecordObject(
        UnityEngine.Object target,
        bool recordUndo,
        string undoName)
    {
        if (recordUndo && target != null)
            Undo.RecordObject(target, undoName);
    }

    private static void RecordPrefabOverride(UnityEngine.Object target)
    {
        if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .FirstOrDefault(root => string.Equals(
                root.name,
                name,
                StringComparison.Ordinal));
    }

    private static bool IsNoryangjinMapScene(Scene scene)
    {
        return scene.IsValid() &&
               scene.isLoaded &&
               (string.Equals(
                    scene.path,
                    NoryangjinMapToolWindow.MapToolScenePath,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    scene.path,
                    Map2ScenePath,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureSharedWaterAssets()
    {
        EnsureLowPolyOceanMesh();
        LoadLowCostWaterMaterial();
        EnsureLowPolyWaterTileMesh();
    }

    private static void DisableMap2WaterOutline()
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(Map2WaterMaterialPath);
        if (material == null || !material.GetShaderPassEnabled("Outline"))
            return;

        material.SetShaderPassEnabled("Outline", false);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
    }

    private static Material LoadLowCostWaterMaterial()
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(LowCostWaterMaterialPath);
        if (material == null)
        {
            throw new InvalidOperationException(
                $"Low-cost water material was not found: {LowCostWaterMaterialPath}");
        }

        return material;
    }

    private static Mesh EnsureLowPolyOceanMesh()
    {
        Mesh existing =
            AssetDatabase.LoadAssetAtPath<Mesh>(LowPolyOceanMeshPath);
        if (existing != null)
            return existing;

        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(OceanSourceModelPath);
        Mesh sourceMesh =
            source != null
                ? source.GetComponentInChildren<MeshFilter>(true)?.sharedMesh
                : null;
        if (sourceMesh == null)
            throw new InvalidOperationException(
                $"Ocean source mesh was not found: {OceanSourceModelPath}");

        Bounds bounds = sourceMesh.bounds;
        float z = bounds.center.z;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        var mesh = new Mesh
        {
            name = "Noryangjin_OceanWater_LowPoly",
            vertices = new[]
            {
                new Vector3(min.x, min.y, z),
                new Vector3(max.x, min.y, z),
                new Vector3(max.x, max.y, z),
                new Vector3(min.x, max.y, z)
            },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            },
            normals = Enumerable.Repeat(Vector3.forward, 4).ToArray(),
            triangles = new[] { 0, 1, 2, 0, 2, 3 }
        };
        mesh.RecalculateBounds();
        return SaveGeneratedMesh(mesh, LowPolyOceanMeshPath);
    }

    private static Mesh EnsureLowPolyWaterTileMesh()
    {
        Mesh existing =
            AssetDatabase.LoadAssetAtPath<Mesh>(LowPolyWaterTileMeshPath);
        if (existing != null)
            return existing;

        var mesh = new Mesh
        {
            name = "Noryangjin_WaterTile_LowPoly",
            vertices = new[]
            {
                new Vector3(-5f, 0f, -5f),
                new Vector3(-5f, 0f, 5f),
                new Vector3(5f, 0f, 5f),
                new Vector3(5f, 0f, -5f)
            },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            },
            normals = Enumerable.Repeat(Vector3.up, 4).ToArray(),
            tangents = Enumerable.Repeat(new Vector4(1f, 0f, 0f, 1f), 4).ToArray(),
            triangles = new[] { 0, 1, 2, 0, 2, 3 }
        };
        mesh.RecalculateBounds();
        return SaveGeneratedMesh(mesh, LowPolyWaterTileMeshPath);
    }

    private static Mesh SaveGeneratedMesh(Mesh mesh, string assetPath)
    {
        EnsureAssetFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssetIfDirty(mesh);
        return mesh;
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) ||
            AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string name = Path.GetFileName(folderPath);
        EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static int OptimizeAndroidTexturesUsedByScenes(
        IEnumerable<Scene> scenes)
    {
        string[] texturePaths = scenes
            .Where(scene => scene.IsValid() && scene.isLoaded)
            .SelectMany(CollectUsedEnvironmentTexturePaths)
            .Where(path => path.StartsWith(
                NoryangjinTextureRoot,
                StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        int changed = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in texturePaths)
            {
                TextureImporter importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                int expectedMaxSize = ResolveAndroidTextureMaxSize(path);
                TextureImporterPlatformSettings android =
                    importer.GetPlatformTextureSettings(AndroidPlatformName);
                bool needsChange =
                    !importer.mipmapEnabled ||
                    !importer.streamingMipmaps ||
                    !android.overridden ||
                    android.maxTextureSize != expectedMaxSize ||
                    android.format != TextureImporterFormat.Automatic ||
                    android.textureCompression !=
                        TextureImporterCompression.Compressed;
                if (!needsChange)
                    continue;

                importer.mipmapEnabled = true;
                importer.streamingMipmaps = true;
                android.name = AndroidPlatformName;
                android.overridden = true;
                android.maxTextureSize = expectedMaxSize;
                android.format = TextureImporterFormat.Automatic;
                android.textureCompression =
                    TextureImporterCompression.Compressed;
                android.compressionQuality = 50;
                importer.SetPlatformTextureSettings(android);
                EditorUtility.SetDirty(importer);
                AssetDatabase.WriteImportSettingsIfDirty(path);
                changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        return changed;
    }

    private static IEnumerable<string> CollectUsedEnvironmentTexturePaths(
        Scene scene)
    {
        GameObject mapRoot = FindRoot(scene, MapRootName);
        if (mapRoot == null)
            yield break;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Renderer renderer in
                 mapRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                foreach (int propertyId in material.GetTexturePropertyNameIDs())
                {
                    Texture texture = material.GetTexture(propertyId);
                    if (texture == null)
                        continue;

                    string path = AssetDatabase.GetAssetPath(texture);
                    if (!string.IsNullOrEmpty(path) && seen.Add(path))
                        yield return path;
                }
            }
        }
    }

    private static bool? EnableMobileTextureStreaming()
    {
        UnityEngine.Object[] qualityAssets =
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
        if (qualityAssets == null || qualityAssets.Length == 0)
            return null;

        using var serialized = new SerializedObject(qualityAssets[0]);
        SerializedProperty levels = serialized.FindProperty("m_QualitySettings");
        if (levels == null || !levels.isArray)
            return null;

        for (int index = 0; index < levels.arraySize; index++)
        {
            SerializedProperty level = levels.GetArrayElementAtIndex(index);
            SerializedProperty name = level.FindPropertyRelative("name");
            SerializedProperty streaming =
                level.FindPropertyRelative("streamingMipmapsActive");
            if (name == null ||
                streaming == null ||
                !string.Equals(name.stringValue, "Mobile", StringComparison.Ordinal))
            {
                continue;
            }

            if (streaming.boolValue)
                return false;

            streaming.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(qualityAssets[0]);
            AssetDatabase.SaveAssetIfDirty(qualityAssets[0]);
            return true;
        }

        return null;
    }

    private static string BuildSummary(
        IEnumerable<NoryangjinMapOptimizationReport> reports,
        int textureImportersChanged,
        bool? streamingChanged,
        bool savedScenes)
    {
        NoryangjinMapOptimizationReport[] values = reports.ToArray();
        return
            "[Noryangjin Optimization] 완료\n" +
            $"Scenes: {string.Join(", ", values.Select(value => value.ScenePath))}\n" +
            $"Static renderers: {values.Sum(value => value.EligibleStaticRenderers)} " +
            $"eligible / {values.Sum(value => value.StaticRenderersChanged)} changed\n" +
            $"Dynamic roots skipped: {values.Sum(value => value.DynamicRootsSkipped)}\n" +
            $"Editor-only guides: {values.Sum(value => value.GuideRootsTaggedEditorOnly)} changed\n" +
            $"Water meshes: {values.Sum(value => value.WaterMeshesReplaced)} changed\n" +
            $"Camera overrides: {values.Sum(value => value.CameraOverridesChanged)} changed\n" +
            $"Android texture importers: {textureImportersChanged} changed\n" +
            "Mobile texture streaming: " +
            (streamingChanged == true
                ? "enabled"
                : streamingChanged == false
                    ? "already enabled"
                    : "Mobile quality level not found") +
            "\n" +
            $"Scenes saved: {savedScenes}";
    }
}

public sealed class NoryangjinMapOptimizationReport
{
    public NoryangjinMapOptimizationReport(string scenePath)
    {
        ScenePath = scenePath ?? string.Empty;
    }

    public string ScenePath { get; }
    public int EligibleStaticRenderers { get; internal set; }
    public int StaticRenderersChanged { get; internal set; }
    public int StaticRendererPoliciesChanged { get; internal set; }
    public int DynamicRootsSkipped { get; internal set; }
    public int DynamicBatchingFlagsCleared { get; internal set; }
    public int DynamicRendererPoliciesRestored { get; internal set; }
    public int GuideRootsTaggedEditorOnly { get; internal set; }
    public int WaterRenderersOptimized { get; internal set; }
    public int WaterMeshesReplaced { get; internal set; }
    public int CameraOverridesChanged { get; internal set; }

    public bool HasSceneChanges =>
        StaticRenderersChanged > 0 ||
        StaticRendererPoliciesChanged > 0 ||
        DynamicBatchingFlagsCleared > 0 ||
        DynamicRendererPoliciesRestored > 0 ||
        GuideRootsTaggedEditorOnly > 0 ||
        WaterRenderersOptimized > 0 ||
        WaterMeshesReplaced > 0 ||
        CameraOverridesChanged > 0;
}
#endif
