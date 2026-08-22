#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlatKit;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class FeastOfFortuneWallSetup
{
    private const string ShaderName = "FlatKit/Stylized Surface";
    private const string UnlitShaderName = "Universal Render Pipeline/Unlit";
    private const string TemplatePrefabPath =
        "Assets/ShooterSurvival/Prefabs/Walls/New/random_wall_normal.prefab";
    private const string MaterialFolder =
        "Assets/ShooterSurvival/Materials/Generated/BonusChoiceBoxes";
    private const string GeneratedTextureFolder =
        "Assets/ShooterSurvival/Textures/Generated/BonusChoiceBoxes";
    private const string PrefabFolder = "Assets/ShooterSurvival/Prefabs/Walls/New";
    private const string LeftPrefabPath = PrefabFolder + "/Box_left.prefab";
    private const string DeprecatedRightPrefabPath = PrefabFolder + "/Box_Right.prefab";
    private const string LegacyLeftPrefabPath =
        PrefabFolder + "/wall_feast_of_fortune_left.prefab";
    private const string BonusIconResourceFolder =
        "Assets/ShooterSurvival/Resources/WallBonusIcons";
    private const string BeveledBoxMeshPath =
        MaterialFolder + "/BonusBox_BeveledBox.asset";
    private const string ChoiceTextMaterialPath =
        MaterialFolder + "/BonusBox_ChoiceText.mat";
    private const string RunePrefabFolder =
        "Assets/polyperfect/Poly Universal Pack/Prefabs/Primeval/Runes";
    private const string UniversalAlbedoPath =
        "Assets/polyperfect/Poly Universal Pack/Textures/Universal/Universal_A_Alb.png";
    private const string CircleGroundTexturePath =
        "Assets/SrRubfish_VFX_02/Textures/Shared/FX_TX_CircleGround_Buff_01.png";
    private const string VerticalGlowTexturePath =
        "Assets/SrRubfish_VFX_02/Textures/Shared/FX_TX_VerticalGlow_01.png";
    private const string VerticalImpactTexturePath =
        "Assets/SrRubfish_VFX_02/Textures/Shared/FX_TX_VerticalImpact_01.png";
    private const string GlowAddTexturePath =
        "Assets/SrRubfish_VFX_02/Textures/Shared/FX_TX_GlowADD_01.png";
    internal const float StatCanvasScale = 0.06f;
    internal const float StatCanvasHorizontalScale = 0.06f;
    internal const float StatCanvasWidth = 1.2f;
    internal const float StatNameFontSize = 0.085f;
    internal const float StatValueFontSize = StatNameFontSize;
    internal const float StatRowSpacing = 0.048f;
    internal const float StatIconLeftAnchor = 0.27f;
    internal const float StatIconRightAnchor = 0.73f;
    internal const float StatIconBottomAnchor = 0.394f;
    internal const float StatIconTopAnchor = 0.946f;
    internal const float StatNameBottomAnchor = 0.94f;
    internal const float StatNameTopAnchor = 1.02f;
    internal const float StatValueBottomAnchor = 0.94f;
    internal const float StatValueTopAnchor = 1.02f;
    internal const float ChoiceTitleFontSize = 0.12f;
    internal const float ChoiceTitleBottomAnchor = 1.04f;
    internal const float ChoiceTitleTopAnchor = 1.2f;
    internal const float ChoiceTextBackplateLeftAnchor = 0.055f;
    internal const float ChoiceTextBackplateRightAnchor = 0.945f;
    internal const float ChoiceTextBackplateBottomAnchor = 0.915f;
    internal const float ChoiceTextBackplateTopAnchor = 1.22f;
    internal const float ChoiceTextBackplateAlpha = 0.58f;
    internal const float AltarVisualScale = 1f;
    internal const float AltarHorizontalScale = 0.75f;
    private const float PedestalWidth = 0.95f;
    private const float PedestalHeight = 0.61f;
    private const float PedestalDepth = 0.68f;
    private const float RuneCircleHeight = PedestalHeight + 0.015f;
    private const float RuneCircleDiameter = 1.04f;
    private static readonly string[] BonusIconFileNames =
    {
        "WallBonus_Attack.png",
        "WallBonus_AttackSpeed.png",
        "WallBonus_MissileDuration.png",
        "WallBonus_Health.png",
        "WallBonus_MissileAdd.png",
        "WallBonus_Tungtung.png",
        "WallBonus_Boombar.png"
    };
    private static readonly string[] RendererDataPaths =
    {
        "Assets/FlatKit/Demos/Common/URP Configs/[FlatKit] Example Renderer.asset",
        "Assets/Settings/Mobile RP.asset",
        "Assets/Settings/PC RP.asset"
    };
    private const string ReportPath = "Temp/BonusChoiceBoxSetupReport.txt";
    private const string GameplayPreviewPath = "Temp/BonusChoicePremiumVfx.png";
    private const int GameplayPreviewWidth = 369;
    private const int GameplayPreviewHeight = 657;
    internal const int StatCanvasSortingOrder = 3;
    internal const int StatIconSortingOrder = 4;

    private sealed class AltarMaterialSet
    {
        public Material Body;
        public Material Accent;
        public Material Edge;
        public Material Panel;
        public Material WearDecal;
        public Material GlyphGlow;
        public Material RingGlow;
        public Material ArcUnderglow;
        public Material GroundAura;
        public Material Beam;
        public Material IconHalo;
        public Material IconCore;
        public Material IconVeil;
        public Material FrontSigil;
        public Material Particles;
    }

    private enum EffectTextureShape
    {
        SoftAura,
        EnergyMote,
        MagicCircle,
        WearCracks
    }

    [MenuItem("Tools/Shooter Survival/Bonus Choice Boxes/Build Box Prefabs", false, 2320)]
    public static void BuildWallPrefabs()
    {
        ValidateSourceAssets();
        EnsureFolder(MaterialFolder);
        EnsureFolder(GeneratedTextureFolder);
        MigrateLegacyPrefab(LegacyLeftPrefabPath, LeftPrefabPath);

        foreach (string iconFileName in BonusIconFileNames)
            ConfigureBonusIcon(BonusIconResourceFolder + "/" + iconFileName);

        Texture2D universalAlbedo = LoadTexture(UniversalAlbedoPath);
        Texture2D circleTexture = LoadTexture(CircleGroundTexturePath);
        Texture2D verticalGlowTexture = LoadTexture(VerticalGlowTexturePath);
        Texture2D verticalImpactTexture = LoadTexture(VerticalImpactTexturePath);
        Texture2D glowAddTexture = LoadTexture(GlowAddTexturePath);
        Texture2D softAuraTexture = CreateOrUpdateEffectTexture(
            "BonusBox_SoftAura",
            GeneratedTextureFolder + "/BonusBox_SoftAura.png",
            EffectTextureShape.SoftAura);
        Texture2D magicCircleTexture = CreateOrUpdateEffectTexture(
            "BonusBox_MagicCircle",
            GeneratedTextureFolder + "/BonusBox_MagicCircle.png",
            EffectTextureShape.MagicCircle);
        Texture2D energyMoteTexture = CreateOrUpdateEffectTexture(
            "BonusBox_EnergyMote",
            GeneratedTextureFolder + "/BonusBox_EnergyMote.png",
            EffectTextureShape.EnergyMote);
        Texture2D wearCracksTexture = CreateOrUpdateEffectTexture(
            "BonusBox_WearCracks",
            GeneratedTextureFolder + "/BonusBox_WearCracks.png",
            EffectTextureShape.WearCracks);
        Mesh beveledBoxMesh = CreateOrUpdateBeveledBoxMesh();

        AltarMaterialSet attackMaterials = CreateAltarMaterials(
            "Attack",
            new Color(0.357f, 0.22f, 0.137f, 1f),
            new Color(0.92f, 0.31f, 0.055f, 1f),
            new Color(2.3f, 0.75f, 0.04f, 0.78f),
            universalAlbedo,
            circleTexture,
            magicCircleTexture,
            softAuraTexture,
            verticalGlowTexture,
            verticalImpactTexture,
            glowAddTexture,
            LoadBonusIcon(BuffType.att_normmal).texture,
            energyMoteTexture,
            wearCracksTexture);
        EnsureOutlineRendererFeatures(
            attackMaterials.Body,
            attackMaterials.Edge,
            attackMaterials.Panel,
            attackMaterials.Accent);

        string altarPrefab = BuildWallPrefab(
            attackMaterials,
            beveledBoxMesh);
        AssetDatabase.SaveAssets();
        WriteReport(altarPrefab);

        Debug.Log(
            $"[BonusChoiceBoxes] Built one data-driven Bonus_Altar prefab. " +
            $"Report={ReportPath}");
    }

    [MenuItem("Tools/Shooter Survival/Bonus Choice Boxes/Migrate Open Scene To Single Altar", false, 2321)]
    public static void MigrateOpenSceneToSingleAltar()
    {
        UnityEngine.SceneManagement.Scene scene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject canonicalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LeftPrefabPath);
        if (!scene.IsValid() || !scene.isLoaded || canonicalPrefab == null)
            throw new InvalidOperationException(
                "A loaded scene and the canonical bonus altar prefab are required.");

        AuthoredBonusWall[] altars = UnityEngine.Object.FindObjectsByType<AuthoredBonusWall>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        int migrated = 0;
        int normalized = 0;
        bool changed = false;
        foreach (AuthoredBonusWall altar in altars)
        {
            if (altar.gameObject.scene != scene)
                continue;

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                altar.gameObject);
            if (string.Equals(
                    prefabPath,
                    DeprecatedRightPrefabPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                Transform oldTransform = altar.transform;
                Transform parent = oldTransform.parent;
                GameObject replacement = PrefabUtility.InstantiatePrefab(
                    canonicalPrefab,
                    parent) as GameObject;
                if (replacement == null)
                    throw new InvalidOperationException("Could not instantiate the canonical bonus altar.");

                replacement.transform.SetSiblingIndex(oldTransform.GetSiblingIndex());
                replacement.transform.localPosition = oldTransform.localPosition;
                replacement.transform.localRotation = oldTransform.localRotation;
                replacement.transform.localScale = oldTransform.localScale;
                replacement.name = BuildMigratedAltarName(altar.gameObject.name);
                replacement.GetComponent<AuthoredBonusWall>().Configure(altar.Rarity);
                NoryangjinMapToolWindow.ConfigureBonusWallInstance(replacement);
                Undo.RegisterCreatedObjectUndo(replacement, "Migrate Bonus Altar");
                Undo.DestroyObjectImmediate(altar.gameObject);
                migrated++;
                changed = true;
                continue;
            }

            if (string.Equals(prefabPath, LeftPrefabPath, StringComparison.OrdinalIgnoreCase))
            {
                string normalizedName = BuildMigratedAltarName(altar.gameObject.name);
                if (!NeedsCanonicalAltarNormalization(altar, normalizedName))
                    continue;

                WallScript wall = altar.Wall;
                Undo.RecordObject(altar.gameObject, "Normalize Bonus Altar");
                Undo.RecordObject(altar, "Normalize Bonus Altar");
                if (wall != null)
                    Undo.RecordObject(wall, "Normalize Bonus Altar");

                altar.gameObject.name = normalizedName;
                altar.Configure(altar.Rarity);
                NoryangjinMapToolWindow.ConfigureBonusWallInstance(altar.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(altar);
                if (wall != null)
                    PrefabUtility.RecordPrefabInstancePropertyModifications(wall);
                EditorUtility.SetDirty(altar.gameObject);
                normalized++;
                changed = true;
            }
        }

        if (changed)
            EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            $"[BonusChoiceBoxes] Migrated {migrated} deprecated altar instance(s) " +
            $"and normalized {normalized} canonical instance(s).");
    }

    private static bool NeedsCanonicalAltarNormalization(
        AuthoredBonusWall altar,
        string normalizedName)
    {
        if (!string.Equals(altar.gameObject.name, normalizedName, StringComparison.Ordinal))
            return true;

        WallScript[] walls = altar.GetComponentsInChildren<WallScript>(true);
        foreach (WallScript wall in walls)
        {
            if (wall.wallType != WallType.BuffWall ||
                !wall.isRandom ||
                wall.rarity != altar.Rarity)
            {
                return true;
            }

            RuntimeBonusWall marker = wall.GetComponent<RuntimeBonusWall>();
            if (marker == null || marker.RemoveWhenPreparingStage)
                return true;
        }

        return walls.Length == 0;
    }

    private static string BuildMigratedAltarName(string currentName)
    {
        int coordinateIndex = currentName.IndexOf("_X", StringComparison.Ordinal);
        return coordinateIndex >= 0
            ? "Bonus_Altar" + currentName.Substring(coordinateIndex)
            : "Bonus_Altar";
    }

    [MenuItem("Tools/Shooter Survival/Bonus Choice Boxes/Capture Gameplay Preview", false, 2322)]
    public static void CaptureGameplayPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException(
                "Exit play mode before capturing the deterministic bonus-choice preview.");

        UnityEngine.SceneManagement.Scene scene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("A loaded scene is required for the gameplay preview.");

        Camera sourceCamera = null;
        foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (camera.gameObject.scene == scene && camera.name == "MapTool_Camera")
            {
                sourceCamera = camera;
                break;
            }
        }

        if (sourceCamera == null)
            throw new InvalidOperationException("MapTool_Camera was not found in the active scene.");

        GameObject[] choiceRoots = FindPlacedChoiceRoots(scene);
        GameObject previewCameraObject = new GameObject(
            "BonusChoiceGameplayPreviewCamera",
            typeof(Camera),
            typeof(UniversalAdditionalCameraData));
        previewCameraObject.hideFlags = HideFlags.HideAndDontSave;

        Camera previewCamera = previewCameraObject.GetComponent<Camera>();
        previewCamera.CopyFrom(sourceCamera);
        previewCamera.enabled = false;
        previewCamera.fieldOfView = 40f;
        previewCamera.aspect = (float)GameplayPreviewWidth / GameplayPreviewHeight;
        Vector3 choiceCenter =
            (choiceRoots[0].transform.position + choiceRoots[1].transform.position) * 0.5f;
        previewCamera.transform.SetPositionAndRotation(
            new Vector3(choiceCenter.x, 7.7f, choiceCenter.z - 16.475f),
            Quaternion.Euler(17.5f, 0f, 0f));

        UniversalAdditionalCameraData sourceCameraData =
            sourceCamera.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData previewCameraData =
            previewCamera.GetUniversalAdditionalCameraData();
        previewCameraData.renderPostProcessing = sourceCameraData.renderPostProcessing;
        previewCameraData.renderShadows = sourceCameraData.renderShadows;
        previewCameraData.antialiasing = sourceCameraData.antialiasing;
        previewCameraData.antialiasingQuality = sourceCameraData.antialiasingQuality;

        List<Transform> billboardTransforms = new();
        List<Quaternion> billboardRotations = new();
        ParticleSystem[] particles = new ParticleSystem[choiceRoots.Length];
        bool[] autoRandomSeed = new bool[choiceRoots.Length];
        uint[] randomSeeds = new uint[choiceRoots.Length];
        int[] particleCounts = new int[choiceRoots.Length];
        string[] particleScreenBounds = new string[choiceRoots.Length];
        RenderTexture renderTexture = null;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D screenshot = null;
        try
        {
            for (int index = 0; index < choiceRoots.Length; index++)
            {
                Transform canvas = choiceRoots[index].transform.Find("GFX/Canvas");
                if (canvas == null)
                    throw new InvalidOperationException(
                        $"Choice Canvas was not found: {choiceRoots[index].name}");

                WallStatCanvasBillboard[] billboards = choiceRoots[index]
                    .GetComponentsInChildren<WallStatCanvasBillboard>(true);
                if (billboards.Length == 0)
                    throw new InvalidOperationException(
                        $"Choice billboard was not found: {choiceRoots[index].name}");

                foreach (WallStatCanvasBillboard billboard in billboards)
                {
                    billboardTransforms.Add(billboard.transform);
                    billboardRotations.Add(billboard.transform.rotation);
                    billboard.FaceCamera(previewCamera);
                }
                RectTransform statRow = canvas.Find("Stat_Row") as RectTransform;
                if (statRow != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(statRow);

                particles[index] = choiceRoots[index].transform
                    .Find("ChoiceAltarVisual/ChoiceParticles")
                    ?.GetComponent<ParticleSystem>();
                if (particles[index] == null)
                    throw new InvalidOperationException(
                        $"Choice particle system was not found: {choiceRoots[index].name}");

                autoRandomSeed[index] = particles[index].useAutoRandomSeed;
                randomSeeds[index] = particles[index].randomSeed;
                particles[index].useAutoRandomSeed = false;
                particles[index].randomSeed = index == 0 ? 1701u : 2401u;
                particles[index].Simulate(1.2f, false, true, true);
                particleCounts[index] = particles[index].particleCount;
                if (particleCounts[index] == 0)
                    throw new InvalidOperationException(
                        $"Choice particles did not simulate: {choiceRoots[index].name}");
                particleScreenBounds[index] = DescribeParticleScreenBounds(
                    particles[index],
                    previewCamera);
            }

            Canvas.ForceUpdateCanvases();
            renderTexture = RenderTexture.GetTemporary(
                GameplayPreviewWidth,
                GameplayPreviewHeight,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            previewCamera.targetTexture = renderTexture;
            previewCamera.Render();
            RenderTexture.active = renderTexture;

            screenshot = new Texture2D(
                GameplayPreviewWidth,
                GameplayPreviewHeight,
                TextureFormat.RGB24,
                false,
                false);
            screenshot.ReadPixels(
                new Rect(0f, 0f, GameplayPreviewWidth, GameplayPreviewHeight),
                0,
                0,
                false);
            screenshot.Apply(false, false);
            Directory.CreateDirectory(Path.GetDirectoryName(GameplayPreviewPath) ?? "Temp");
            File.WriteAllBytes(
                Path.GetFullPath(GameplayPreviewPath),
                screenshot.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousActive;
            previewCamera.targetTexture = null;
            if (renderTexture != null)
                RenderTexture.ReleaseTemporary(renderTexture);
            if (screenshot != null)
                UnityEngine.Object.DestroyImmediate(screenshot);

            for (int index = 0; index < billboardTransforms.Count; index++)
            {
                if (billboardTransforms[index] != null)
                    billboardTransforms[index].rotation = billboardRotations[index];
            }

            for (int index = 0; index < choiceRoots.Length; index++)
            {
                if (particles[index] == null)
                    continue;

                particles[index].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles[index].randomSeed = randomSeeds[index];
                particles[index].useAutoRandomSeed = autoRandomSeed[index];
            }

            UnityEngine.Object.DestroyImmediate(previewCameraObject);
        }

        Debug.Log(
            $"[BonusChoiceBoxes] Gameplay preview captured: {GameplayPreviewPath}. " +
            $"Particles=Attack:{particleCounts[0]}[{particleScreenBounds[0]}]," +
            $"Health:{particleCounts[1]}[{particleScreenBounds[1]}]");
    }

    private static string DescribeParticleScreenBounds(
        ParticleSystem particles,
        Camera camera)
    {
        ParticleSystem.Particle[] snapshot =
            new ParticleSystem.Particle[particles.particleCount];
        int count = particles.GetParticles(snapshot);
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        for (int index = 0; index < count; index++)
        {
            Vector3 worldPosition = particles.transform.TransformPoint(
                snapshot[index].position);
            Vector3 viewportPosition = camera.WorldToViewportPoint(worldPosition);
            Vector3 screenPosition = new Vector3(
                viewportPosition.x * GameplayPreviewWidth,
                viewportPosition.y * GameplayPreviewHeight,
                viewportPosition.z);
            minX = Mathf.Min(minX, screenPosition.x);
            maxX = Mathf.Max(maxX, screenPosition.x);
            minY = Mathf.Min(minY, screenPosition.y);
            maxY = Mathf.Max(maxY, screenPosition.y);
        }

        return $"x={minX:F0}-{maxX:F0},y={minY:F0}-{maxY:F0}";
    }

    private static GameObject[] FindPlacedChoiceRoots(
        UnityEngine.SceneManagement.Scene scene)
    {
        var roots = new List<GameObject>();
        foreach (AuthoredBonusWall authoredBonus in
                 UnityEngine.Object.FindObjectsByType<AuthoredBonusWall>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
        {
            if (authoredBonus.gameObject.scene != scene ||
                authoredBonus.transform.parent == null ||
                authoredBonus.transform.parent.name != "Bonuses")
            {
                continue;
            }

            roots.Add(authoredBonus.gameObject);
        }

        roots.Sort((left, right) =>
            left.transform.position.x.CompareTo(right.transform.position.x));
        if (roots.Count < 2)
            throw new InvalidOperationException(
                "At least two placed bonus altars are required under Bonuses for the preview.");

        return new[] { roots[0], roots[1] };
    }

    private static void ValidateSourceAssets()
    {
        RequireAsset(TemplatePrefabPath);
        RequireAsset(UniversalAlbedoPath);
        RequireAsset(CircleGroundTexturePath);
        RequireAsset(VerticalGlowTexturePath);
        RequireAsset(VerticalImpactTexturePath);
        RequireAsset(GlowAddTexturePath);

        foreach (string iconFileName in BonusIconFileNames)
            RequireAsset(BonusIconResourceFolder + "/" + iconFileName);

        for (char rune = 'A'; rune <= 'I'; rune++)
            RequireAsset($"{RunePrefabFolder}/Rune_{rune}.prefab");

        foreach (string rendererDataPath in RendererDataPaths)
            RequireAsset(rendererDataPath);

        if (Shader.Find(ShaderName) == null)
            throw new InvalidOperationException($"Could not find required shader: {ShaderName}");

        if (Shader.Find(UnlitShaderName) == null)
            throw new InvalidOperationException($"Could not find required shader: {UnlitShaderName}");
    }

    private static void MigrateLegacyPrefab(string legacyPath, string currentPath)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(currentPath) != null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(legacyPath) == null)
        {
            return;
        }

        string error = AssetDatabase.MoveAsset(legacyPath, currentPath);
        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(
                $"Could not migrate bonus box prefab '{legacyPath}': {error}");
    }

    private static void RequireAsset(string assetPath)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            throw new FileNotFoundException($"Required asset is missing: {assetPath}");
    }

    private static void ConfigureBonusIcon(string iconPath)
    {
        if (AssetImporter.GetAtPath(iconPath) is not TextureImporter importer)
            throw new InvalidOperationException($"Wall bonus icon importer unavailable: {iconPath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 256;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static AltarMaterialSet CreateAltarMaterials(
        string variant,
        Color bodyColor,
        Color accentColor,
        Color glowColor,
        Texture2D universalAlbedo,
        Texture2D circleTexture,
        Texture2D magicCircleTexture,
        Texture2D softAuraTexture,
        Texture2D verticalGlowTexture,
        Texture2D verticalImpactTexture,
        Texture2D glowAddTexture,
        Texture2D iconTexture,
        Texture2D particleTexture,
        Texture2D wearCracksTexture)
    {
        string prefix = "BonusBox_" + variant;
        Color glyphColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.36f);
        Color ringColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.44f);
        Color arcColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.12f);
        Color auraColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.3f);
        Color beamColor = new Color(
            glowColor.r,
            glowColor.g,
            glowColor.b,
            variant == "Attack" ? 0.48f : 0.42f);
        Color iconVeilColor = new Color(
            glowColor.r,
            glowColor.g,
            glowColor.b,
            variant == "Attack" ? 0.62f : 0.54f);
        Color sigilColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.55f);
        Color particleColor = new Color(
            glowColor.r,
            glowColor.g,
            glowColor.b,
            variant == "Attack" ? 0.95f : 0.85f);
        Color edgeColor = Color.Lerp(bodyColor, accentColor, 0.28f);
        Color panelColor = new Color(
            bodyColor.r * 0.5f,
            bodyColor.g * 0.5f,
            bodyColor.b * 0.5f,
            1f);
        Color wearColor = variant == "Attack"
            ? new Color(0.055f, 0.018f, 0.008f, 0.62f)
            : new Color(0.008f, 0.075f, 0.062f, 0.58f);

        return new AltarMaterialSet
        {
            Body = CreateOrUpdateStylizedMaterial(
                prefix + "Pedestal",
                MaterialFolder + "/" + prefix + "Pedestal.mat",
                bodyColor,
                universalAlbedo,
                0.38f),
            Accent = CreateOrUpdateStylizedMaterial(
                prefix + "Accent",
                MaterialFolder + "/" + prefix + "Accent.mat",
                accentColor),
            Edge = CreateOrUpdateStylizedMaterial(
                prefix + "Edge",
                MaterialFolder + "/" + prefix + "Edge.mat",
                edgeColor,
                universalAlbedo,
                0.28f),
            Panel = CreateOrUpdateStylizedMaterial(
                prefix + "Panel",
                MaterialFolder + "/" + prefix + "Panel.mat",
                panelColor,
                universalAlbedo,
                0.2f),
            WearDecal = CreateOrUpdateGlowMaterial(
                prefix + "WearDecal",
                MaterialFolder + "/" + prefix + "WearDecal.mat",
                wearColor,
                additive: false,
                texture: wearCracksTexture),
            GlyphGlow = CreateOrUpdateGlowMaterial(
                prefix + "Glow",
                MaterialFolder + "/" + prefix + "Glow.mat",
                glyphColor,
                additive: true),
            RingGlow = CreateOrUpdateGlowMaterial(
                prefix + "RuneCircle",
                MaterialFolder + "/" + prefix + "RuneCircle.mat",
                ringColor,
                additive: true,
                texture: magicCircleTexture),
            ArcUnderglow = CreateOrUpdateGlowMaterial(
                prefix + "ArcUnderglow",
                MaterialFolder + "/" + prefix + "ArcUnderglow.mat",
                arcColor,
                additive: true,
                texture: circleTexture),
            GroundAura = CreateOrUpdateGlowMaterial(
                prefix + "GroundAura",
                MaterialFolder + "/" + prefix + "GroundAura.mat",
                auraColor,
                additive: true,
                texture: softAuraTexture),
            Beam = CreateOrUpdateGlowMaterial(
                prefix + "Beam",
                MaterialFolder + "/" + prefix + "Beam.mat",
                beamColor,
                additive: true,
                texture: verticalGlowTexture),
            IconHalo = CreateOrUpdateGlowMaterial(
                prefix + "IconHalo",
                MaterialFolder + "/" + prefix + "IconHalo.mat",
                new Color(
                    glowColor.r,
                    glowColor.g,
                    glowColor.b,
                    variant == "Attack" ? 0.35f : 0.32f),
                additive: true,
                texture: glowAddTexture),
            IconCore = CreateOrUpdateGlowMaterial(
                prefix + "IconCore",
                MaterialFolder + "/" + prefix + "IconCore.mat",
                variant == "Attack"
                    ? new Color(2.8f, 1.65f, 0.2f, 0.8f)
                    : new Color(0.8f, 2.4f, 2.8f, 0.75f),
                additive: true,
                texture: verticalGlowTexture),
            IconVeil = CreateOrUpdateGlowMaterial(
                prefix + "IconVeil",
                MaterialFolder + "/" + prefix + "IconVeil.mat",
                iconVeilColor,
                additive: true,
                texture: variant == "Attack"
                    ? verticalImpactTexture
                    : verticalGlowTexture),
            FrontSigil = CreateOrUpdateGlowMaterial(
                prefix + "FrontSigil",
                MaterialFolder + "/" + prefix + "FrontSigil.mat",
                sigilColor,
                additive: true,
                texture: iconTexture),
            Particles = CreateOrUpdateGlowMaterial(
                prefix + "Particles",
                MaterialFolder + "/" + prefix + "Particles.mat",
                particleColor,
                additive: true,
                texture: variant == "Attack"
                    ? verticalImpactTexture
                    : particleTexture)
        };
    }

    private static Texture2D LoadTexture(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath)
            ?? throw new FileNotFoundException($"Required texture is missing: {assetPath}");
    }

    private static Texture2D CreateOrUpdateEffectTexture(
        string textureName,
        string assetPath,
        EffectTextureShape shape)
    {
        int size = shape switch
        {
            EffectTextureShape.MagicCircle => 256,
            EffectTextureShape.WearCracks => 128,
            _ => 64
        };
        Texture2D generated = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = ((x + 0.5f) / size) * 2f - 1f;
                float py = ((y + 0.5f) / size) * 2f - 1f;
                float alpha = EffectAlpha(shape, px, py);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        generated.SetPixels(pixels);
        generated.Apply(false, false);
        byte[] png = generated.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(generated);

        string absolutePath = Path.GetFullPath(assetPath);
        if (!File.Exists(absolutePath) || !BytesEqual(File.ReadAllBytes(absolutePath), png))
        {
            File.WriteAllBytes(absolutePath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = size;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return LoadTexture(assetPath);
    }

    private static float EffectAlpha(EffectTextureShape shape, float x, float y)
    {
        switch (shape)
        {
            case EffectTextureShape.SoftAura:
            {
                float radius = Mathf.Sqrt(x * x + y * y);
                return Mathf.Pow(Mathf.Clamp01(1f - radius), 2.2f);
            }
            case EffectTextureShape.EnergyMote:
            {
                float radius = Mathf.Sqrt(x * x + y * y);
                float softCore = Mathf.Pow(
                    Mathf.Clamp01(1f - radius / 0.62f),
                    1.2f);
                float verticalRay =
                    Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(x) / 0.14f), 1.8f) *
                    Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y) / 0.95f), 0.7f) *
                    0.9f;
                float horizontalRay =
                    Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y) / 0.15f), 1.8f) *
                    Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(x) / 0.7f), 0.85f) *
                    0.68f;
                return Mathf.Clamp01(Mathf.Max(
                    softCore,
                    Mathf.Max(verticalRay, horizontalRay)));
            }
            case EffectTextureShape.MagicCircle:
                return MagicCircleAlpha(x, y);
            case EffectTextureShape.WearCracks:
                return WearCracksAlpha(x, y);
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }

    private static float SoftShapeAlpha(float signedDistance, float feather)
    {
        return Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(-feather, feather, signedDistance));
    }

    private static float MagicCircleAlpha(float x, float y)
    {
        float radius = Mathf.Sqrt(x * x + y * y);
        float alpha = Mathf.Max(
            RingAlpha(radius, 0.92f, 0.009f),
            Mathf.Max(
                RingAlpha(radius, 0.72f, 0.008f),
                RingAlpha(radius, 0.53f, 0.007f)));

        const int tickCount = 24;
        float angle = Mathf.Atan2(y, x);
        float sectorAngle = Mathf.PI * 2f / tickCount;
        float wrappedAngle = Mathf.Repeat(angle + sectorAngle * 0.5f, sectorAngle) -
                             sectorAngle * 0.5f;
        float tangentialDistance = Mathf.Abs(wrappedAngle) * radius;
        int sectorIndex = Mathf.FloorToInt((angle + Mathf.PI) / sectorAngle);
        float tickOuterRadius = sectorIndex % 2 == 0 ? 0.885f : 0.85f;
        float angularMask = 1f - SmoothThreshold(0.008f, 0.018f, tangentialDistance);
        float radialMask = RangeAlpha(radius, 0.775f, tickOuterRadius, 0.009f);
        alpha = Mathf.Max(alpha, angularMask * radialMask);

        for (int index = 0; index < 4; index++)
        {
            float cardinalAngle = index * Mathf.PI * 0.5f;
            float centerX = Mathf.Cos(cardinalAngle) * 0.625f;
            float centerY = Mathf.Sin(cardinalAngle) * 0.625f;
            float diamondDistance =
                0.057f - (Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY));
            alpha = Mathf.Max(alpha, SoftShapeAlpha(diamondDistance, 0.01f));
        }

        return alpha;
    }

    private static float RingAlpha(float radius, float ringRadius, float halfWidth)
    {
        return 1f - SmoothThreshold(
            halfWidth,
            halfWidth + 0.009f,
            Mathf.Abs(radius - ringRadius));
    }

    private static float RangeAlpha(float value, float minimum, float maximum, float feather)
    {
        float lower = SmoothThreshold(minimum - feather, minimum + feather, value);
        float upper = 1f - SmoothThreshold(maximum - feather, maximum + feather, value);
        return lower * upper;
    }

    private static float SmoothThreshold(float edge0, float edge1, float value)
    {
        float t = Mathf.InverseLerp(edge0, edge1, value);
        return t * t * (3f - 2f * t);
    }

    private static float WearCracksAlpha(float x, float y)
    {
        Vector2 point = new(x, y);
        float alpha = SegmentStroke(
            point,
            new Vector2(-0.82f, 0.62f),
            new Vector2(-0.38f, 0.25f),
            0.018f);
        alpha = Mathf.Max(alpha, SegmentStroke(
            point,
            new Vector2(-0.38f, 0.25f),
            new Vector2(-0.12f, -0.05f),
            0.015f));
        alpha = Mathf.Max(alpha, SegmentStroke(
            point,
            new Vector2(-0.12f, -0.05f),
            new Vector2(0.33f, -0.48f),
            0.014f));
        alpha = Mathf.Max(alpha, SegmentStroke(
            point,
            new Vector2(-0.38f, 0.25f),
            new Vector2(-0.52f, -0.18f),
            0.012f));
        alpha = Mathf.Max(alpha, SegmentStroke(
            point,
            new Vector2(-0.12f, -0.05f),
            new Vector2(0.28f, 0.12f),
            0.011f));
        alpha = Mathf.Max(alpha, SegmentStroke(
            point,
            new Vector2(0.54f, 0.72f),
            new Vector2(0.34f, 0.37f),
            0.015f));
        alpha = Mathf.Max(alpha, SegmentStroke(
            point,
            new Vector2(0.34f, 0.37f),
            new Vector2(0.64f, 0.04f),
            0.012f));

        float leftChip = 0.055f - Mathf.Max(
            Mathf.Abs(x + 0.68f),
            Mathf.Abs(y + 0.56f) * 1.35f);
        float rightChip = 0.045f - Mathf.Max(
            Mathf.Abs(x - 0.73f) * 1.2f,
            Mathf.Abs(y - 0.12f));
        alpha = Mathf.Max(alpha, SoftShapeAlpha(leftChip, 0.018f) * 0.72f);
        alpha = Mathf.Max(alpha, SoftShapeAlpha(rightChip, 0.016f) * 0.62f);
        return alpha;
    }

    private static float SegmentStroke(
        Vector2 point,
        Vector2 start,
        Vector2 end,
        float width)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        float t = lengthSquared > 0f
            ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared)
            : 0f;
        float distance = Vector2.Distance(point, start + segment * t);
        return 1f - SmoothThreshold(width, width + 0.018f, distance);
    }

    private static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;

        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
                return false;
        }

        return true;
    }

    private static Mesh CreateOrUpdateBeveledBoxMesh()
    {
        const float cornerBevel = 0.1f;
        const float edgeBevel = 0.085f;
        Vector2[] perimeter =
        {
            new(-0.5f + cornerBevel, -0.5f),
            new(0.5f - cornerBevel, -0.5f),
            new(0.5f, -0.5f + cornerBevel),
            new(0.5f, 0.5f - cornerBevel),
            new(0.5f - cornerBevel, 0.5f),
            new(-0.5f + cornerBevel, 0.5f),
            new(-0.5f, 0.5f - cornerBevel),
            new(-0.5f, -0.5f + cornerBevel)
        };
        float[] ringHeights = { -0.5f, -0.5f + edgeBevel, 0.5f - edgeBevel, 0.5f };
        float[] ringScales = { 1f - edgeBevel * 2f, 1f, 1f, 1f - edgeBevel * 2f };

        List<Vector3> vertices = new(34);
        List<Vector2> uvs = new(34);
        for (int ring = 0; ring < ringHeights.Length; ring++)
        {
            for (int index = 0; index < perimeter.Length; index++)
            {
                Vector2 point = perimeter[index] * ringScales[ring];
                vertices.Add(new Vector3(point.x, ringHeights[ring], point.y));
                uvs.Add(new Vector2(index / 8f, ring / 3f));
            }
        }

        List<int> triangles = new(8 * 6 * 3 + 8 * 2 * 3);
        for (int ring = 0; ring < ringHeights.Length - 1; ring++)
        {
            int lowerStart = ring * 8;
            int upperStart = (ring + 1) * 8;
            for (int index = 0; index < 8; index++)
            {
                int next = (index + 1) % 8;
                triangles.Add(lowerStart + index);
                triangles.Add(upperStart + index);
                triangles.Add(lowerStart + next);
                triangles.Add(lowerStart + next);
                triangles.Add(upperStart + index);
                triangles.Add(upperStart + next);
            }
        }

        int bottomCenter = vertices.Count;
        vertices.Add(new Vector3(0f, -0.5f, 0f));
        uvs.Add(new Vector2(0.5f, 0.5f));
        int topCenter = vertices.Count;
        vertices.Add(new Vector3(0f, 0.5f, 0f));
        uvs.Add(new Vector2(0.5f, 0.5f));
        int topStart = (ringHeights.Length - 1) * 8;
        for (int index = 0; index < 8; index++)
        {
            int next = (index + 1) % 8;
            triangles.Add(bottomCenter);
            triangles.Add(index);
            triangles.Add(next);
            triangles.Add(topCenter);
            triangles.Add(topStart + next);
            triangles.Add(topStart + index);
        }

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BeveledBoxMeshPath);
        bool createAsset = mesh == null;
        if (createAsset)
            mesh = new Mesh { name = "BonusBox_BeveledBox" };
        else
            mesh.Clear();

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        if (createAsset)
            AssetDatabase.CreateAsset(mesh, BeveledBoxMeshPath);
        else
            EditorUtility.SetDirty(mesh);

        return mesh;
    }

    private static Material CreateOrUpdateStylizedMaterial(
        string assetName,
        string materialPath,
        Color baseColor,
        Texture texture = null,
        float textureImpact = 1f)
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
            throw new InvalidOperationException($"Could not find required shader: {ShaderName}");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = assetName };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        SetColorIfPresent(material, "_BaseColor", baseColor);
        SetColorIfPresent(material, "_Color", baseColor);
        SetColorIfPresent(
            material,
            "_ColorDim",
            new Color(baseColor.r * 0.7f, baseColor.g * 0.7f, baseColor.b * 0.7f, 1f));
        SetColorIfPresent(
            material,
            "_ColorDimExtra",
            new Color(baseColor.r * 0.48f, baseColor.g * 0.48f, baseColor.b * 0.48f, 1f));
        SetColorIfPresent(material, "_OutlineColor", Color.black);
        SetTextureIfPresent(material, "_BaseMap", texture ?? Texture2D.whiteTexture);

        SetFloatIfPresent(material, "_CelPrimaryMode", 1f);
        SetFloatIfPresent(material, "_CelExtraEnabled", 1f);
        SetFloatIfPresent(material, "_SelfShadingSize", 0.208f);
        SetFloatIfPresent(material, "_ShadowEdgeSize", 0.062f);
        SetFloatIfPresent(material, "_Flatness", 1f);
        SetFloatIfPresent(material, "_TextureBlendingMode", 0f);
        SetFloatIfPresent(material, "_TextureImpact", textureImpact);

        SetFloatIfPresent(material, "_OutlineEnabled", 1f);
        SetFloatIfPresent(material, "_OutlineWidth", 1.2f);
        SetFloatIfPresent(material, "_OutlineScale", 1f);
        SetFloatIfPresent(material, "_OutlineDepthOffset", 0.005f);
        SetFloatIfPresent(material, "_OutlineSpace", 0f);
        SetFloatIfPresent(material, "_CameraDistanceImpact", 0.2f);

        material.DisableKeyword("_NORMALMAP");
        material.EnableKeyword("_CELPRIMARYMODE_SINGLE");
        material.DisableKeyword("_CELPRIMARYMODE_NONE");
        material.DisableKeyword("_CELPRIMARYMODE_STEPS");
        material.DisableKeyword("_CELPRIMARYMODE_CURVE");
        material.EnableKeyword("DR_CEL_EXTRA_ON");
        material.EnableKeyword("_TEXTUREBLENDINGMODE_MULTIPLY");
        material.DisableKeyword("_TEXTUREBLENDINGMODE_ADD");
        material.EnableKeyword("DR_OUTLINE_ON");
        material.EnableKeyword("_OUTLINESPACE_SCREEN");
        material.DisableKeyword("_OUTLINESPACE_OBJECT");
        material.DisableKeyword("DR_OUTLINE_SMOOTH_NORMALS");
        material.DisableKeyword("DR_SPECULAR_ON");
        material.DisableKeyword("DR_RIM_ON");
        material.DisableKeyword("DR_GRADIENT_ON");
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        material.DisableKeyword("_EMISSION");
        material.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
        if (material.HasProperty("_EmissionMap"))
            material.SetTexture("_EmissionMap", null);
        SetColorIfPresent(material, "_EmissionColor", Color.black);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateOrUpdateGlowMaterial(
        string assetName,
        string materialPath,
        Color color,
        bool additive,
        Texture texture = null)
    {
        Shader shader = Shader.Find(UnlitShaderName);
        if (shader == null)
            throw new InvalidOperationException(
                $"Could not find required shader: {UnlitShaderName}");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = assetName };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        SetColorIfPresent(material, "_BaseColor", color);
        SetColorIfPresent(material, "_Color", color);
        SetTextureIfPresent(material, "_BaseMap", texture ?? Texture2D.whiteTexture);
        SetTextureIfPresent(material, "_MainTex", texture ?? Texture2D.whiteTexture);
        SetFloatIfPresent(material, "_Surface", 1f);
        SetFloatIfPresent(material, "_Blend", additive ? 2f : 0f);
        SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloatIfPresent(
            material,
            "_DstBlend",
            additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(material, "_ZWrite", 0f);
        SetFloatIfPresent(material, "_Cull", (float)CullMode.Off);
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureOutlineRendererFeatures(params Material[] materials)
    {
        foreach (string rendererDataPath in RendererDataPaths)
            EnsureOutlineRendererFeature(rendererDataPath, materials);
    }

    private static void EnsureOutlineRendererFeature(
        string rendererDataPath,
        Material[] materials)
    {
        ScriptableRendererData rendererData =
            AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererDataPath);
        if (rendererData == null)
            throw new InvalidOperationException(
                $"Could not load renderer data: {rendererDataPath}");

        ObjectOutlineRendererFeature outlineFeature = null;
        foreach (var rendererFeature in rendererData.rendererFeatures)
        {
            if (rendererFeature is ObjectOutlineRendererFeature candidate)
            {
                outlineFeature = candidate;
                break;
            }
        }

        if (outlineFeature == null)
        {
            outlineFeature = ScriptableObject.CreateInstance<ObjectOutlineRendererFeature>();
            outlineFeature.name = "Flat Kit Per Object Outline";
            outlineFeature.Create();
            AssetDatabase.AddObjectToAsset(outlineFeature, rendererData);
            rendererData.rendererFeatures.Add(outlineFeature);
        }

        bool changed = !outlineFeature.isActive || !outlineFeature.autoReferenceMaterials;
        outlineFeature.SetActive(true);
        outlineFeature.autoReferenceMaterials = true;
        foreach (Material material in materials)
        {
            if (outlineFeature.materials.Contains(material))
                continue;

            outlineFeature.RegisterMaterial(material, true);
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(outlineFeature);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssetIfDirty(outlineFeature);
            AssetDatabase.SaveAssetIfDirty(rendererData);
        }
    }

    private static void SetColorIfPresent(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
            material.SetColor(property, value);
    }

    private static void SetFloatIfPresent(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    private static void SetTextureIfPresent(Material material, string property, Texture value)
    {
        if (material.HasProperty(property))
            material.SetTexture(property, value);
    }

    private static string BuildWallPrefab(
        AltarMaterialSet materials,
        Mesh beveledBoxMesh)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TemplatePrefabPath);
        try
        {
            DisableTemplateRenderer(root);

            if (root.GetComponent<BonusWallLifetimeRoot>() == null)
                root.AddComponent<BonusWallLifetimeRoot>();

            WallScript wall = root.GetComponentInChildren<WallScript>(true);
            if (wall == null)
                throw new InvalidOperationException("Wall template no longer contains WallScript.");

            Transform glowRoot = CreateChoiceAltarVisual(
                root.transform,
                materials,
                beveledBoxMesh,
                false,
                0f);

            wall.buffType = BuffType.att_normmal;
            bool statLocalizationWasEnabled =
                ConfigureStatLocalization(wall, BuffType.att_normmal);
            RectTransform[] iconAuras = ConfigureStatIcon(
                root,
                wall,
                BuffType.att_normmal,
                0f);
            ConfigureStatValuePreview(wall);
            ConfigureStatTypography(wall);

            BonusChoiceAltarVfx altarVfx = root.GetComponent<BonusChoiceAltarVfx>();
            if (altarVfx == null)
                altarVfx = root.AddComponent<BonusChoiceAltarVfx>();
            altarVfx.Configure(
                false,
                glowRoot,
                wall.statIconImage.rectTransform,
                iconAuras);

            AuthoredBonusWall authoredBonus = root.GetComponent<AuthoredBonusWall>();
            if (authoredBonus == null)
                authoredBonus = root.AddComponent<AuthoredBonusWall>();
            authoredBonus.Configure(Rarity.Normal);

            ConfigureStatLabelPreview(wall, BuffType.att_normmal);
            RebuildStatRowLayout(wall);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                LeftPrefabPath,
                out bool success);
            if (!success || saved == null)
                throw new InvalidOperationException(
                    $"Could not save wall prefab: {LeftPrefabPath}");

            RestoreSavedLocalizationEnabled(saved, statLocalizationWasEnabled);

            return LeftPrefabPath;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform CreateChoiceAltarVisual(
        Transform parent,
        AltarMaterialSet materials,
        Mesh beveledBoxMesh,
        bool aggressiveVfx,
        float visualOffsetX)
    {
        GameObject visual = new GameObject("ChoiceAltarVisual");
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = new Vector3(visualOffsetX, 0f, 0f);
        visual.transform.localScale = new Vector3(
            AltarHorizontalScale,
            AltarVisualScale,
            AltarVisualScale);

        CreateMeshPiece(
            visual.transform,
            "PedestalBase",
            beveledBoxMesh,
            new Vector3(0f, 0.05f, 0f),
            new Vector3(PedestalWidth, 0.1f, PedestalDepth),
            Quaternion.identity,
            materials.Body);
        CreateMeshPiece(
            visual.transform,
            "PedestalBaseEdge",
            beveledBoxMesh,
            new Vector3(0.006f, 0.118f, -0.004f),
            new Vector3(0.89f, 0.045f, 0.625f),
            Quaternion.Euler(0f, 0.65f, 0.2f),
            materials.Edge);
        CreateMeshPiece(
            visual.transform,
            "PedestalCore",
            beveledBoxMesh,
            new Vector3(0f, 0.295f, 0f),
            new Vector3(0.81f, 0.31f, 0.54f),
            Quaternion.identity,
            materials.Body);
        CreateMeshPiece(
            visual.transform,
            "PedestalLowerTrim",
            beveledBoxMesh,
            new Vector3(-0.004f, 0.155f, 0.002f),
            new Vector3(0.85f, 0.045f, 0.6f),
            Quaternion.Euler(0f, -0.55f, -0.18f),
            materials.Edge);
        CreateMeshPiece(
            visual.transform,
            "PedestalUpperTrim",
            beveledBoxMesh,
            new Vector3(0.005f, 0.445f, -0.003f),
            new Vector3(0.86f, 0.045f, 0.61f),
            Quaternion.Euler(0f, 0.55f, 0.15f),
            materials.Edge);
        CreateMeshPiece(
            visual.transform,
            "PedestalCapLower",
            beveledBoxMesh,
            new Vector3(-0.004f, 0.485f, 0.002f),
            new Vector3(0.89f, 0.055f, 0.63f),
            Quaternion.Euler(0f, -0.35f, 0f),
            materials.Edge);
        CreateMeshPiece(
            visual.transform,
            "PedestalCap",
            beveledBoxMesh,
            new Vector3(0f, 0.56f, 0f),
            new Vector3(0.92f, 0.1f, 0.66f),
            Quaternion.identity,
            materials.Body);

        CreateMeshPiece(
            visual.transform,
            "FrontPanelRecess",
            beveledBoxMesh,
            new Vector3(0f, 0.295f, -0.276f),
            new Vector3(0.59f, 0.225f, 0.014f),
            Quaternion.identity,
            materials.Panel);
        CreateMeshPiece(
            visual.transform,
            "FrontFrameLeft",
            beveledBoxMesh,
            new Vector3(-0.318f, 0.295f, -0.286f),
            new Vector3(0.045f, 0.3f, 0.03f),
            Quaternion.Euler(0f, 0f, -0.6f),
            materials.Edge);
        CreateMeshPiece(
            visual.transform,
            "FrontFrameRight",
            beveledBoxMesh,
            new Vector3(0.318f, 0.295f, -0.286f),
            new Vector3(0.045f, 0.3f, 0.03f),
            Quaternion.Euler(0f, 0f, 0.45f),
            materials.Edge);
        CreateMeshPiece(
            visual.transform,
            "FrontFrameTop",
            beveledBoxMesh,
            new Vector3(0f, 0.424f, -0.286f),
            new Vector3(0.67f, 0.04f, 0.03f),
            Quaternion.Euler(0f, 0f, 0.35f),
            materials.Edge);
        CreateMeshPiece(
            visual.transform,
            "FrontFrameBottom",
            beveledBoxMesh,
            new Vector3(0f, 0.166f, -0.286f),
            new Vector3(0.67f, 0.04f, 0.03f),
            Quaternion.Euler(0f, 0f, -0.25f),
            materials.Edge);

        CreateQuadPiece(
            visual.transform,
            "WearCrackLeft",
            new Vector3(-0.13f, 0.33f, -0.304f),
            new Vector2(0.27f, 0.18f),
            Quaternion.Euler(0f, 0f, 8f),
            materials.WearDecal);
        CreateQuadPiece(
            visual.transform,
            "WearCrackRight",
            new Vector3(0.16f, 0.245f, -0.304f),
            new Vector2(0.24f, 0.15f),
            Quaternion.Euler(0f, 0f, -15f),
            materials.WearDecal);

        CreateMeshPiece(
            visual.transform,
            "EdgeChip_Left",
            beveledBoxMesh,
            new Vector3(-0.36f, 0.445f, -0.318f),
            new Vector3(0.075f, 0.022f, 0.025f),
            Quaternion.Euler(0f, 0f, 8f),
            materials.Panel);
        CreateMeshPiece(
            visual.transform,
            "EdgeChip_Right",
            beveledBoxMesh,
            new Vector3(0.38f, 0.155f, -0.307f),
            new Vector3(0.055f, 0.025f, 0.028f),
            Quaternion.Euler(0f, 0f, -12f),
            materials.Panel);
        CreateMeshPiece(
            visual.transform,
            "EdgeChip_Cap",
            beveledBoxMesh,
            new Vector3(0.21f, 0.51f, -0.342f),
            new Vector3(0.08f, 0.02f, 0.028f),
            Quaternion.Euler(0f, 0f, 5f),
            materials.Panel);

        CreateQuadPiece(
            visual.transform,
            "FrontSigil",
            new Vector3(0f, 0.295f, -0.306f),
            new Vector2(0.247f, 0.247f),
            Quaternion.identity,
            materials.FrontSigil);

        CreateQuadPiece(
            visual.transform,
            "GroundAura",
            new Vector3(0f, RuneCircleHeight - 0.012f, 0f),
            new Vector2(RuneCircleDiameter * 1.45f, RuneCircleDiameter * 1.45f),
            Quaternion.Euler(90f, 0f, 0f),
            materials.GroundAura);

        GameObject energyBillboard = new GameObject("IconEnergyBillboard");
        energyBillboard.transform.SetParent(visual.transform, false);
        energyBillboard.transform.localPosition = new Vector3(
            0f,
            RuneCircleHeight + RuneCircleDiameter * 0.47f,
            -0.035f);
        energyBillboard.AddComponent<WallStatCanvasBillboard>();

        GameObject iconHalo = CreateQuadPiece(
            energyBillboard.transform,
            "IconEnergyHalo",
            new Vector3(0f, -0.02f, -0.001f),
            new Vector2(
                RuneCircleDiameter * 1.28f,
                RuneCircleDiameter * 0.84f),
            Quaternion.identity,
            materials.IconHalo);
        iconHalo.GetComponent<MeshRenderer>().sortingOrder = 0;

        GameObject centralBeam = CreateQuadPiece(
            energyBillboard.transform,
            "VerticalBeam",
            Vector3.zero,
            new Vector2(
                RuneCircleDiameter * 1.1f,
                RuneCircleDiameter * 0.95f),
            Quaternion.identity,
            materials.Beam);
        centralBeam.GetComponent<MeshRenderer>().sortingOrder = 0;

        GameObject energyCore = CreateQuadPiece(
            energyBillboard.transform,
            "IconEnergyCore",
            new Vector3(0f, -0.035f, 0.001f),
            new Vector2(
                RuneCircleDiameter * 0.5f,
                RuneCircleDiameter * 0.85f),
            Quaternion.identity,
            materials.IconCore);
        energyCore.GetComponent<MeshRenderer>().sortingOrder = 1;

        float veilSideOffset = RuneCircleDiameter * 0.22f;
        float veilTilt = aggressiveVfx ? 9f : 7f;
        GameObject leftVeil = CreateQuadPiece(
            energyBillboard.transform,
            "IconEnergyVeilLeft",
            new Vector3(-veilSideOffset, -0.015f, 0.001f),
            new Vector2(
                RuneCircleDiameter * (aggressiveVfx ? 0.42f : 0.38f),
                RuneCircleDiameter * 0.93f),
            Quaternion.Euler(0f, 0f, -veilTilt),
            materials.IconVeil);
        GameObject rightVeil = CreateQuadPiece(
            energyBillboard.transform,
            "IconEnergyVeilRight",
            new Vector3(veilSideOffset, 0.015f, 0.002f),
            new Vector2(
                RuneCircleDiameter * (aggressiveVfx ? 0.42f : 0.38f),
                RuneCircleDiameter * 0.93f),
            Quaternion.Euler(0f, 0f, veilTilt),
            materials.IconVeil);
        leftVeil.GetComponent<MeshRenderer>().sortingOrder = 2;
        rightVeil.GetComponent<MeshRenderer>().sortingOrder = 2;

        GameObject glowRoot = new GameObject("GlowOrbit");
        glowRoot.transform.SetParent(visual.transform, false);
        glowRoot.transform.localPosition = new Vector3(0f, RuneCircleHeight, 0f);

        CreateQuadPiece(
            glowRoot.transform,
            "RuneArcUnderglow",
            new Vector3(0f, 0.001f, 0f),
            new Vector2(RuneCircleDiameter * 1.05f, RuneCircleDiameter * 1.05f),
            Quaternion.Euler(90f, 0f, 18f),
            materials.ArcUnderglow);
        CreateQuadPiece(
            glowRoot.transform,
            "RuneCircleOuter",
            new Vector3(0f, 0.002f, 0f),
            new Vector2(RuneCircleDiameter, RuneCircleDiameter),
            Quaternion.Euler(90f, 0f, 0f),
            materials.RingGlow);
        CreateQuadPiece(
            glowRoot.transform,
            "RuneCircleInner",
            new Vector3(0f, 0.006f, 0f),
            new Vector2(RuneCircleDiameter * 0.78f, RuneCircleDiameter * 0.78f),
            Quaternion.Euler(90f, 0f, 105f),
            materials.RingGlow);

        float runeRadius = RuneCircleDiameter * 0.39f;
        for (int index = 0; index < 9; index++)
        {
            char runeLetter = (char)('A' + index);
            float angle = index * 40f;
            float radians = angle * Mathf.Deg2Rad;
            Mesh runeMesh = LoadPrefabMesh($"{RunePrefabFolder}/Rune_{runeLetter}.prefab");
            float sourceSize = Mathf.Max(
                runeMesh.bounds.size.x,
                Mathf.Max(runeMesh.bounds.size.y, runeMesh.bounds.size.z));
            float runeScale = 0.1f / sourceSize;
            CreateMeshPiece(
                glowRoot.transform,
                $"Rune_{runeLetter}",
                runeMesh,
                new Vector3(
                    Mathf.Cos(radians) * runeRadius,
                    0.012f,
                    Mathf.Sin(radians) * runeRadius),
                Vector3.one * runeScale,
                Quaternion.Euler(90f, -angle - 90f, 0f),
                materials.GlyphGlow);
        }

        CreateParticleEffect(
            visual.transform,
            materials.Particles,
            aggressiveVfx);
        return glowRoot.transform;
    }

    private static Mesh LoadPrefabMesh(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        MeshFilter meshFilter = prefab != null
            ? prefab.GetComponentInChildren<MeshFilter>(true)
            : null;
        if (meshFilter == null || meshFilter.sharedMesh == null)
            throw new InvalidOperationException($"Prefab has no reusable mesh: {prefabPath}");

        return meshFilter.sharedMesh;
    }

    private static GameObject CreateMeshPiece(
        Transform parent,
        string name,
        Mesh mesh,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject piece = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localScale = localScale;
        piece.transform.localRotation = localRotation;
        piece.GetComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return piece;
    }

    private static GameObject CreateQuadPiece(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector2 localSize,
        Quaternion localRotation,
        Material material)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
        piece.name = name;
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localScale = new Vector3(localSize.x, localSize.y, 1f);
        piece.transform.localRotation = localRotation;
        if (piece.TryGetComponent(out Collider collider))
            UnityEngine.Object.DestroyImmediate(collider);

        MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return piece;
    }

    private static void CreateParticleEffect(
        Transform parent,
        Material particleMaterial,
        bool aggressiveVfx)
    {
        GameObject effectObject = new GameObject("ChoiceParticles", typeof(ParticleSystem));
        effectObject.transform.SetParent(parent, false);
        effectObject.transform.localPosition = new Vector3(0f, RuneCircleHeight + 0.02f, 0f);
        effectObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem particles = effectObject.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = aggressiveVfx
            ? new ParticleSystem.MinMaxCurve(0.85f, 1.25f)
            : new ParticleSystem.MinMaxCurve(1.5f, 2.2f);
        main.startSpeed = aggressiveVfx
            ? new ParticleSystem.MinMaxCurve(0.65f, 0.95f)
            : new ParticleSystem.MinMaxCurve(0.32f, 0.5f);
        main.startSize3D = true;
        main.startSizeX = aggressiveVfx
            ? new ParticleSystem.MinMaxCurve(0.1f, 0.18f)
            : new ParticleSystem.MinMaxCurve(0.09f, 0.13f);
        main.startSizeY = aggressiveVfx
            ? new ParticleSystem.MinMaxCurve(0.22f, 0.38f)
            : new ParticleSystem.MinMaxCurve(0.15f, 0.22f);
        main.startSizeZ = main.startSizeX;
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.28f, 0.28f);
        main.startColor = Color.white;
        main.maxParticles = aggressiveVfx ? 16 : 12;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = aggressiveVfx ? 11f : 7f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = aggressiveVfx ? 14f : 17f;
        shape.radius = RuneCircleDiameter * (aggressiveVfx ? 0.35f : 0.38f);
        shape.radiusThickness = aggressiveVfx ? 0.58f : 0.72f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(aggressiveVfx ? 0.9f : 0.78f, 0.16f),
                new GradientAlphaKey(aggressiveVfx ? 0.68f : 0.58f, 0.68f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.RotationOverLifetimeModule rotationOverLifetime =
            particles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = aggressiveVfx
            ? new ParticleSystem.MinMaxCurve(-0.45f, 0.45f)
            : new ParticleSystem.MinMaxCurve(-0.24f, 0.24f);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.65f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, aggressiveVfx ? 0.3f : 0.5f)));

        ParticleSystem.TrailModule trails = particles.trails;
        trails.enabled = false;
        ParticleSystem.CollisionModule collision = particles.collision;
        collision.enabled = false;
        ParticleSystem.LightsModule lights = particles.lights;
        lights.enabled = false;
        ParticleSystem.SubEmittersModule subEmitters = particles.subEmitters;
        subEmitters.enabled = false;

        ParticleSystemRenderer particleRenderer =
            effectObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = particleMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortMode = ParticleSystemSortMode.OldestInFront;
        particleRenderer.sortingOrder = 1;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
    }

    private static void DisableTemplateRenderer(GameObject root)
    {
        Transform gfx = root.transform.Find("GFX");
        if (gfx == null)
            throw new InvalidOperationException("Wall template GFX was not found.");

        Renderer[] templateRenderers = gfx.GetComponentsInChildren<Renderer>(true);
        if (templateRenderers.Length == 0)
            throw new InvalidOperationException("Wall template renderers were not found.");

        foreach (Renderer renderer in templateRenderers)
            renderer.enabled = false;
    }

    private static RectTransform[] ConfigureStatIcon(
        GameObject root,
        WallScript wall,
        BuffType buffType,
        float visualOffsetX)
    {
        Transform canvas = root.transform.Find("GFX/Canvas");
        if (canvas == null)
            throw new InvalidOperationException("Wall template Canvas was not found.");

        RectTransform canvasTransform = canvas as RectTransform;
        if (canvasTransform == null)
            throw new InvalidOperationException("Wall template Canvas must use a RectTransform.");

        Quaternion editPreviewRotation = Quaternion.Euler(17.5f, 180f, 0f);
        canvasTransform.localRotation =
            Quaternion.Inverse(canvasTransform.parent.localRotation) * editPreviewRotation;
        canvasTransform.localPosition = new Vector3(
            visualOffsetX / 25f,
            0.003f,
            0.034f);
        canvasTransform.localScale = new Vector3(
            StatCanvasHorizontalScale,
            StatCanvasScale,
            StatCanvasScale);
        canvasTransform.sizeDelta = new Vector2(
            StatCanvasWidth,
            canvasTransform.sizeDelta.y);
        Canvas choiceCanvas = canvas.GetComponent<Canvas>();
        if (choiceCanvas == null)
            throw new InvalidOperationException("Wall template Canvas component was not found.");

        choiceCanvas.sortingLayerID = SortingLayer.NameToID("Default");
        choiceCanvas.sortingOrder = StatCanvasSortingOrder;
        if (canvas.GetComponent<WallStatCanvasBillboard>() == null)
            canvas.gameObject.AddComponent<WallStatCanvasBillboard>();

        Transform existingIcon = canvas.Find("Stat_Icon");
        GameObject iconObject = existingIcon != null
            ? existingIcon.gameObject
            : new GameObject("Stat_Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.layer = canvas.gameObject.layer;
        RectTransform iconTransform = iconObject.GetComponent<RectTransform>();
        iconTransform.SetParent(canvas, false);
        bool isAttack = buffType is BuffType.att_normmal or BuffType.att_unique;
        float iconLeftAnchor = isAttack ? StatIconLeftAnchor : 0.245f;
        float iconRightAnchor = isAttack ? StatIconRightAnchor : 0.755f;
        float iconBottomAnchor = isAttack ? StatIconBottomAnchor : 0.364f;
        float iconTopAnchor = isAttack ? StatIconTopAnchor : 0.976f;
        iconTransform.anchorMin = new Vector2(iconLeftAnchor, iconBottomAnchor);
        iconTransform.anchorMax = new Vector2(iconRightAnchor, iconTopAnchor);
        iconTransform.anchoredPosition = Vector2.zero;
        iconTransform.sizeDelta = Vector2.zero;
        iconTransform.localScale = Vector3.one;

        Image image = iconObject.GetComponent<Image>();
        Sprite iconSprite = LoadBonusIcon(buffType);
        Texture2D softAuraTexture = LoadTexture(
            GeneratedTextureFolder + "/BonusBox_SoftAura.png");
        image.sprite = iconSprite;
        image.color = Color.white;
        image.material = null;
        image.preserveAspect = true;
        image.raycastTarget = false;
        wall.statIconImage = image;

        Canvas iconCanvas = iconObject.GetComponent<Canvas>();
        if (iconCanvas == null)
            iconCanvas = iconObject.AddComponent<Canvas>();
        iconCanvas.overrideSorting = true;
        iconCanvas.sortingLayerID = choiceCanvas.sortingLayerID;
        iconCanvas.sortingOrder = StatIconSortingOrder;

        foreach (Shadow effect in iconObject.GetComponents<Shadow>())
            UnityEngine.Object.DestroyImmediate(effect);

        Color iconGlow = isAttack
            ? new Color(1f, 0.48f, 0.03f, 0.9f)
            : new Color(0.02f, 0.9f, 1f, 0.85f);

        float iconCenterY = (iconBottomAnchor + iconTopAnchor) * 0.5f;
        float iconAnchorWidth = iconRightAnchor - iconLeftAnchor;
        float iconAnchorHeight = iconTopAnchor - iconBottomAnchor;
        Vector2 outerHalfSize = new(
            iconAnchorWidth * 0.775f,
            iconAnchorHeight * 0.52f);
        Vector2 innerHalfSize = new(
            iconAnchorWidth * 0.65f,
            iconAnchorHeight * 0.5f);
        RectTransform outerAura = CreateOrConfigureIconAura(
            canvas,
            "Stat_Icon_AuraOuter",
            softAuraTexture,
            new Vector2(0.5f - outerHalfSize.x, iconCenterY - outerHalfSize.y),
            new Vector2(0.5f + outerHalfSize.x, iconCenterY + outerHalfSize.y),
            new Color(iconGlow.r, iconGlow.g, iconGlow.b, 0.12f));
        RectTransform innerAura = CreateOrConfigureIconAura(
            canvas,
            "Stat_Icon_AuraInner",
            softAuraTexture,
            new Vector2(0.5f - innerHalfSize.x, iconCenterY - innerHalfSize.y),
            new Vector2(0.5f + innerHalfSize.x, iconCenterY + innerHalfSize.y),
            new Color(iconGlow.r, iconGlow.g, iconGlow.b, 0.25f));
        RectTransform textBackplate = CreateOrConfigureChoiceTextBackplate(canvas);
        outerAura.SetSiblingIndex(iconTransform.GetSiblingIndex());
        innerAura.SetSiblingIndex(iconTransform.GetSiblingIndex());
        textBackplate.SetSiblingIndex(iconTransform.GetSiblingIndex());
        iconTransform.SetAsLastSibling();

        RectTransform statText = wall.statNameLoc != null
            ? wall.statNameLoc.GetComponent<RectTransform>()
            : null;
        RectTransform valueText = wall.statValueTmp != null
            ? wall.statValueTmp.rectTransform
            : null;
        ConfigureStatRow(canvasTransform, statText, valueText);

        TMPro.TextMeshProUGUI statTemplate =
            statText != null ? statText.GetComponent<TMPro.TextMeshProUGUI>() : null;
        CreateOrConfigureChoiceTitle(canvas, isAttack, statTemplate);

        return new[] { innerAura, outerAura };
    }

    private static RectTransform CreateOrConfigureChoiceTextBackplate(Transform canvas)
    {
        const string objectName = "Choice_TextBackplate";
        Transform existing = canvas.Find(objectName);
        GameObject plateObject = existing != null
            ? existing.gameObject
            : new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
        plateObject.layer = canvas.gameObject.layer;

        RectTransform plateTransform = plateObject.GetComponent<RectTransform>();
        plateTransform.SetParent(canvas, false);
        plateTransform.anchorMin = new Vector2(
            ChoiceTextBackplateLeftAnchor,
            ChoiceTextBackplateBottomAnchor);
        plateTransform.anchorMax = new Vector2(
            ChoiceTextBackplateRightAnchor,
            ChoiceTextBackplateTopAnchor);
        plateTransform.pivot = new Vector2(0.5f, 0.5f);
        plateTransform.anchoredPosition = Vector2.zero;
        plateTransform.sizeDelta = Vector2.zero;
        plateTransform.localScale = Vector3.one;
        plateTransform.localRotation = Quaternion.identity;

        RawImage legacyRawImage = plateObject.GetComponent<RawImage>();
        if (legacyRawImage != null)
            UnityEngine.Object.DestroyImmediate(legacyRawImage);
        foreach (Shadow effect in plateObject.GetComponents<Shadow>())
            UnityEngine.Object.DestroyImmediate(effect);

        Image plateImage = plateObject.GetComponent<Image>();
        if (plateImage == null)
            plateImage = plateObject.AddComponent<Image>();
        Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd");
        plateImage.sprite = roundedSprite;
        plateImage.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        plateImage.color = new Color(
            0.015f,
            0.025f,
            0.04f,
            ChoiceTextBackplateAlpha);
        plateImage.material = null;
        plateImage.raycastTarget = false;
        return plateTransform;
    }

    private static void ConfigureStatRow(
        RectTransform canvas,
        RectTransform statText,
        RectTransform valueText)
    {
        if (statText == null || valueText == null)
            throw new InvalidOperationException("Wall template stat text was not found.");

        Transform existingRow = canvas.Find("Stat_Row");
        GameObject rowObject = existingRow != null
            ? existingRow.gameObject
            : new GameObject("Stat_Row", typeof(RectTransform));
        rowObject.layer = canvas.gameObject.layer;

        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.SetParent(canvas, false);
        row.anchorMin = new Vector2(0f, StatNameBottomAnchor);
        row.anchorMax = new Vector2(1f, StatNameTopAnchor);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = Vector2.zero;
        row.sizeDelta = Vector2.zero;
        row.localScale = Vector3.one;
        row.localRotation = Quaternion.identity;

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = StatRowSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;
        layout.reverseArrangement = false;

        ConfigureStatRowChild(statText, row, 0);
        ConfigureStatRowChild(valueText, row, 1);
        row.SetAsLastSibling();
    }

    private static void ConfigureStatRowChild(
        RectTransform child,
        RectTransform row,
        int siblingIndex)
    {
        child.SetParent(row, false);
        child.anchorMin = new Vector2(0.5f, 0.5f);
        child.anchorMax = new Vector2(0.5f, 0.5f);
        child.pivot = new Vector2(0.5f, 0.5f);
        child.anchoredPosition = Vector2.zero;
        child.sizeDelta = Vector2.zero;
        child.localScale = Vector3.one;
        child.localRotation = Quaternion.identity;
        child.SetSiblingIndex(siblingIndex);
    }

    private static void RebuildStatRowLayout(WallScript wall)
    {
        RectTransform statRow = wall.statValueTmp != null
            ? wall.statValueTmp.rectTransform.parent as RectTransform
            : null;
        if (statRow == null || statRow.name != "Stat_Row")
            throw new InvalidOperationException("The centered stat row was not configured.");

        LayoutRebuilder.ForceRebuildLayoutImmediate(statRow);
    }

    private static void CreateOrConfigureChoiceTitle(
        Transform canvas,
        bool isAttack,
        TMPro.TextMeshProUGUI template)
    {
        if (template == null)
            throw new InvalidOperationException("Choice title requires the wall stat font template.");

        Transform existing = canvas.Find("Choice_Title");
        GameObject titleObject = existing != null
            ? existing.gameObject
            : new GameObject(
                "Choice_Title",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TMPro.TextMeshProUGUI));
        titleObject.layer = canvas.gameObject.layer;

        RectTransform titleTransform = titleObject.GetComponent<RectTransform>();
        titleTransform.SetParent(canvas, false);
        titleTransform.anchorMin = new Vector2(0.02f, ChoiceTitleBottomAnchor);
        titleTransform.anchorMax = new Vector2(0.98f, ChoiceTitleTopAnchor);
        titleTransform.anchoredPosition = Vector2.zero;
        titleTransform.sizeDelta = Vector2.zero;
        titleTransform.localScale = new Vector3(0.78f, 1f, 1f);

        TMPro.TextMeshProUGUI title = titleObject.GetComponent<TMPro.TextMeshProUGUI>();
        title.font = template.font;
        Material titleMaterial = CreateOrUpdateChoiceTextMaterial(template.font);
        SetFloatIfPresent(titleMaterial, "_OutlineWidth", 0.28f);
        title.text = "운명의 제단";
        title.color = new Color32(255, 253, 247, 255);
        title.enableAutoSizing = false;
        title.fontSize = ChoiceTitleFontSize;
        title.fontStyle = TMPro.FontStyles.Bold;
        title.alignment = TMPro.TextAlignmentOptions.Center;
        title.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        title.overflowMode = TMPro.TextOverflowModes.Overflow;
        title.outlineWidth = 0.28f;
        title.outlineColor = Color.black;
        title.raycastTarget = false;
        title.fontSharedMaterial = titleMaterial;
        titleTransform.SetAsLastSibling();
    }

    private static RectTransform CreateOrConfigureIconAura(
        Transform canvas,
        string name,
        Texture texture,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        Transform existing = canvas.Find(name);
        GameObject auraObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        auraObject.layer = canvas.gameObject.layer;

        RectTransform auraTransform = auraObject.GetComponent<RectTransform>();
        auraTransform.SetParent(canvas, false);
        auraTransform.anchorMin = anchorMin;
        auraTransform.anchorMax = anchorMax;
        auraTransform.anchoredPosition = Vector2.zero;
        auraTransform.sizeDelta = Vector2.zero;
        auraTransform.localScale = Vector3.one;

        foreach (Shadow effect in auraObject.GetComponents<Shadow>())
            UnityEngine.Object.DestroyImmediate(effect);

        Image legacyImage = auraObject.GetComponent<Image>();
        if (legacyImage != null)
            UnityEngine.Object.DestroyImmediate(legacyImage);

        RawImage auraImage = auraObject.GetComponent<RawImage>();
        if (auraImage == null)
            auraImage = auraObject.AddComponent<RawImage>();
        auraImage.texture = texture;
        auraImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        auraImage.color = color;
        auraImage.raycastTarget = false;
        return auraTransform;
    }

    private static bool ConfigureStatLocalization(WallScript wall, BuffType buffType)
    {
        if (wall.statNameLoc == null)
            throw new InvalidOperationException("Wall template stat localization was not found.");

        bool localizationWasEnabled = wall.statNameLoc.enabled;
        wall.statNameLoc.enabled = false;
        wall.statNameLoc.StringReference.SetReference(
            "AllTexts",
            BonusAltarRules.ResolveLocalizationKey(buffType));

        ConfigureStatLabelPreview(wall, buffType);
        return localizationWasEnabled;
    }

    private static void RestoreSavedLocalizationEnabled(
        GameObject savedPrefab,
        bool enabled)
    {
        WallScript savedWall = savedPrefab.GetComponentInChildren<WallScript>(true);
        if (savedWall == null || savedWall.statNameLoc == null)
            throw new InvalidOperationException(
                "Saved wall prefab no longer contains stat localization.");

        SerializedObject serializedLocalization =
            new SerializedObject(savedWall.statNameLoc);
        SerializedProperty enabledProperty =
            serializedLocalization.FindProperty("m_Enabled");
        if (enabledProperty == null)
            throw new InvalidOperationException(
                "Could not restore saved stat localization enabled state.");

        enabledProperty.boolValue = enabled;
        serializedLocalization.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(savedWall.statNameLoc);
    }

    private static void ConfigureStatLabelPreview(WallScript wall, BuffType buffType)
    {
        TMPro.TextMeshProUGUI statText =
            wall.statNameLoc != null
                ? wall.statNameLoc.GetComponent<TMPro.TextMeshProUGUI>()
                : null;

        if (statText != null)
        {
            statText.text = buffType switch
            {
                BuffType.att_normmal or BuffType.att_unique => "공격력",
                BuffType.hp_normal or BuffType.hp_unique => "체력",
                _ => statText.text
            };
        }
    }

    private static void ConfigureStatValuePreview(WallScript wall)
    {
        if (wall.statValueTmp == null)
            return;

        wall.statValueTmp.text = "+?";
    }

    private static void ConfigureStatTypography(WallScript wall)
    {
        TMPro.TextMeshProUGUI statName =
            wall.statNameLoc != null
                ? wall.statNameLoc.GetComponent<TMPro.TextMeshProUGUI>()
                : null;
        if (statName == null || wall.statValueTmp == null)
            throw new InvalidOperationException("Wall template stat text was not found.");

        statName.enableAutoSizing = false;
        statName.fontSize = StatNameFontSize;
        statName.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
        statName.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        statName.fontStyle = TMPro.FontStyles.Bold;
        statName.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        statName.overflowMode = TMPro.TextOverflowModes.Overflow;
        statName.color = new Color32(232, 237, 241, 255);
        statName.outlineWidth = 0.28f;
        statName.outlineColor = Color.black;
        wall.statValueTmp.enableAutoSizing = false;
        wall.statValueTmp.fontSize = StatValueFontSize;
        wall.statValueTmp.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
        wall.statValueTmp.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        wall.statValueTmp.fontStyle = TMPro.FontStyles.Bold;
        wall.statValueTmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        wall.statValueTmp.overflowMode = TMPro.TextOverflowModes.Overflow;
        wall.statValueTmp.color = wall.buffType is BuffType.att_normmal or BuffType.att_unique
            ? new Color32(255, 208, 112, 255)
            : new Color32(131, 242, 229, 255);
        wall.statValueTmp.outlineWidth = 0.28f;
        wall.statValueTmp.outlineColor = Color.black;

        Material choiceTextMaterial = CreateOrUpdateChoiceTextMaterial(statName.font);
        statName.fontSharedMaterial = choiceTextMaterial;
        wall.statValueTmp.fontSharedMaterial = choiceTextMaterial;
    }

    private static Material CreateOrUpdateChoiceTextMaterial(TMPro.TMP_FontAsset font)
    {
        if (font == null || font.material == null)
            throw new InvalidOperationException("Choice text requires a TMP font material.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(ChoiceTextMaterialPath);
        if (material == null)
        {
            material = new Material(font.material) { name = "BonusBox_ChoiceText" };
            AssetDatabase.CreateAsset(material, ChoiceTextMaterialPath);
        }
        else
        {
            material.shader = font.material.shader;
            material.CopyPropertiesFromMaterial(font.material);
        }

        SetFloatIfPresent(material, "_OutlineWidth", 0.28f);
        SetColorIfPresent(material, "_OutlineColor", Color.black);
        SetFloatIfPresent(material, "_FaceDilate", 0.08f);
        material.EnableKeyword("UNDERLAY_ON");
        SetColorIfPresent(material, "_UnderlayColor", new Color(0f, 0f, 0f, 0.65f));
        SetFloatIfPresent(material, "_UnderlayOffsetX", 0.06f);
        SetFloatIfPresent(material, "_UnderlayOffsetY", -0.08f);
        SetFloatIfPresent(material, "_UnderlayDilate", 0.04f);
        SetFloatIfPresent(material, "_UnderlaySoftness", 0.1f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Sprite LoadBonusIcon(BuffType buffType)
    {
        string fileName = BonusAltarRules.ResolveIconResourceName(buffType) + ".png";
        string iconPath = BonusIconResourceFolder + "/" + fileName;
        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath)
            ?? throw new FileNotFoundException($"Required wall bonus icon is missing: {iconPath}");
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        string folder = Path.GetFileName(assetPath);
        if (string.IsNullOrEmpty(parent))
            throw new InvalidOperationException($"Invalid asset folder: {assetPath}");

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void WriteReport(string altarPrefab)
    {
        string reportDirectory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(reportDirectory))
            Directory.CreateDirectory(reportDirectory);

        File.WriteAllText(
            ReportPath,
            $"AltarPrefab: {altarPrefab}\n" +
            $"Shader: {ShaderName}\n" +
            "TemplateLogicPreserved: True\n" +
            "BonusSource: Data.xlsx / 보너스\n" +
            "Grades: Normal | Elite(Rare data) | Unique\n" +
            "NearbyDuplicateStatsBlocked: True\n" +
            $"PedestalMesh: {BeveledBoxMeshPath}\n" +
            $"PedestalDimensions: {PedestalWidth} x {PedestalHeight} x {PedestalDepth}\n" +
            $"MagicCircleTexture: {GeneratedTextureFolder}/BonusBox_MagicCircle.png\n" +
            $"EnergyMoteTexture: {GeneratedTextureFolder}/BonusBox_EnergyMote.png\n" +
            $"WearCracksTexture: {GeneratedTextureFolder}/BonusBox_WearCracks.png\n" +
            $"ArcUnderglowTexture: {CircleGroundTexturePath}\n" +
            "CompactBeveledTierAltar: True\n" +
            "RecessedFramedFrontPanel: True\n" +
            $"AltarHorizontalScale: {AltarHorizontalScale}\n" +
            "PresentationOffset: Centered\n" +
            "ChoiceTitle: 운명의 제단 (runtime alias after roll)\n" +
            "ChoiceTitleHorizontalScale: 0.78\n" +
            "ChoiceTextSmokedBackplate: True\n" +
            "ChoiceTextUnderlay: True\n" +
            "StaticSemanticMotes: False\n" +
            "SingleSemanticHeroIcon: True\n" +
            "SingleReusableAltar: True\n" +
            "RuneMeshesAThroughI: True\n" +
            "LayeredIconAura: True\n" +
            "AnimatedGlowOrbit: True\n" +
            "SparseAbstractEnergyParticles: True\n" +
            "ReferenceScaleIconPlume: True\n" +
            "ForegroundStatCanvasSortingOrder: " + StatCanvasSortingOrder + "\n" +
            "ForegroundIconSortingOrder: " + StatIconSortingOrder + "\n" +
            "AttackPlumeTexture: " + VerticalImpactTexturePath + "\n" +
            "HealthPlumeTexture: " + VerticalGlowTexturePath + "\n" +
            "IconHaloTexture: " + GlowAddTexturePath + "\n" +
            "OutlineEnabled: True\n" +
            "GlowMaterials: AdditiveUnlit\n");
    }

}
#endif
