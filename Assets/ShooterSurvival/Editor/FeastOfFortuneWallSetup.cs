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
    private const string UniversalAlbedoPath =
        "Assets/polyperfect/Poly Universal Pack/Textures/Universal/Universal_A_Alb.png";
    private const string VerticalGlowTexturePath =
        "Assets/SrRubfish_VFX_02/Textures/Shared/FX_TX_VerticalGlow_01.png";
    private const string VerticalImpactTexturePath =
        "Assets/SrRubfish_VFX_02/Textures/Shared/FX_TX_VerticalImpact_01.png";
    private const string GlowAddTexturePath =
        "Assets/SrRubfish_VFX_02/Textures/Shared/FX_TX_GlowADD_01.png";
    internal const float StatCanvasScale = 0.06f;
    internal const float StatCanvasHorizontalScale = 0.06f;
    internal const float StatCanvasWidth = 1.2f;
    internal const float StatNameFontSize = 0.13f;
    internal const float StatNameMinFontSize = 0.045f;
    internal const float StatValueFontSize = 0.3f;
    internal const float StatValueMinFontSize = 0.16f;
    internal const float StatBadgeLeftAnchor = 0.27f;
    internal const float StatBadgeRightAnchor = 0.73f;
    internal const float StatBadgeBottomAnchor = 0.56f;
    internal const float StatBadgeTopAnchor = 0.74f;
    internal const float StatValueBottomAnchor = 0.78f;
    internal const float StatValueTopAnchor = 1f;
    private const float StatRowSpacing = 0.048f;
    private const float StatNameBottomAnchor = 0.87f;
    private const float StatNameTopAnchor = 0.96f;
    private const float ChoiceTitleFontSize = 0.105f;
    private const float ChoiceTitleBottomAnchor = 0.97f;
    private const float ChoiceTitleTopAnchor = 1.11f;
    internal const float AltarVisualScale = 1f;
    internal const float AltarHorizontalScale = 0.75f;
    private const float PedestalWidth = 0.95f;
    private const float PedestalHeight = 0.61f;
    private const float PedestalDepth = 0.68f;
    private const float WaterSurfaceHeight = PedestalHeight + 0.015f;
    private const float WaterVortexDiameter = 1.08f;
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
        public Material WaterVortex;
        public Material WaterFoam;
        public Material WarpCompass;
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
        WaterVortex,
        WaterFoam,
        WarpCompass,
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
        Texture2D verticalGlowTexture = LoadTexture(VerticalGlowTexturePath);
        Texture2D verticalImpactTexture = LoadTexture(VerticalImpactTexturePath);
        Texture2D glowAddTexture = LoadTexture(GlowAddTexturePath);
        Texture2D softAuraTexture = CreateOrUpdateEffectTexture(
            "BonusBox_SoftAura",
            GeneratedTextureFolder + "/BonusBox_SoftAura.png",
            EffectTextureShape.SoftAura);
        Texture2D waterVortexTexture = CreateOrUpdateEffectTexture(
            "BonusBox_WaterVortex",
            GeneratedTextureFolder + "/BonusBox_WaterVortex.png",
            EffectTextureShape.WaterVortex);
        Texture2D waterFoamTexture = CreateOrUpdateEffectTexture(
            "BonusBox_WaterFoam",
            GeneratedTextureFolder + "/BonusBox_WaterFoam.png",
            EffectTextureShape.WaterFoam);
        Texture2D warpCompassTexture = CreateOrUpdateEffectTexture(
            "BonusBox_WarpCompass",
            GeneratedTextureFolder + "/BonusBox_WarpCompass.png",
            EffectTextureShape.WarpCompass);
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
            waterVortexTexture,
            waterFoamTexture,
            warpCompassTexture,
            softAuraTexture,
            verticalGlowTexture,
            verticalImpactTexture,
            glowAddTexture,
            LoadBonusIcon(BuffType.att_normmal).texture,
            wearCracksTexture);
        EnsureOutlineRendererFeatures(
            attackMaterials.Body,
            attackMaterials.Edge,
            attackMaterials.Panel,
            attackMaterials.Accent);

        string altarPrefab = BuildWallPrefab(
            attackMaterials,
            beveledBoxMesh);
        RemoveLegacyRuneAssets();
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

    [MenuItem("Tools/Shooter Survival/Bonus Choice Boxes/Refresh Open Scene Altar Instances", false, 2322)]
    public static void RefreshOpenSceneAltarInstances()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit play mode before refreshing bonus altars.");

        UnityEngine.SceneManagement.Scene scene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject canonicalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LeftPrefabPath);
        if (!scene.IsValid() || !scene.isLoaded || canonicalPrefab == null)
            throw new InvalidOperationException(
                "A loaded scene and the canonical bonus altar prefab are required.");

        var targets = new List<AuthoredBonusWall>();
        foreach (AuthoredBonusWall altar in
                 UnityEngine.Object.FindObjectsByType<AuthoredBonusWall>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (altar.gameObject.scene != scene)
                continue;

            targets.Add(altar);
        }

        foreach (AuthoredBonusWall altar in targets)
        {
            Transform oldTransform = altar.transform;
            Transform parent = oldTransform.parent;
            int siblingIndex = oldTransform.GetSiblingIndex();
            Vector3 localPosition = oldTransform.localPosition;
            Quaternion localRotation = oldTransform.localRotation;
            Vector3 localScale = oldTransform.localScale;
            string instanceName = altar.gameObject.name;
            Rarity rarity = altar.Rarity;

            GameObject replacement = PrefabUtility.InstantiatePrefab(
                canonicalPrefab,
                parent) as GameObject;
            if (replacement == null)
                throw new InvalidOperationException("Could not refresh the canonical bonus altar.");

            replacement.transform.SetSiblingIndex(siblingIndex);
            replacement.transform.localPosition = localPosition;
            replacement.transform.localRotation = localRotation;
            replacement.transform.localScale = localScale;
            replacement.name = instanceName;
            replacement.GetComponent<AuthoredBonusWall>().Configure(rarity);
            NoryangjinMapToolWindow.ConfigureBonusWallInstance(replacement);
            Undo.RegisterCreatedObjectUndo(replacement, "Refresh Bonus Altar");
            Undo.DestroyObjectImmediate(altar.gameObject);
        }

        if (targets.Count > 0)
            EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[BonusChoiceBoxes] Refreshed {targets.Count} canonical altar instance(s).");
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

    [MenuItem("Tools/Shooter Survival/Bonus Choice Boxes/Capture Gameplay Preview", false, 2323)]
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
        List<WallStatCanvasBillboard> billboardComponents = new();
        List<bool> billboardEnabledStates = new();
        ParticleSystem[] particles = new ParticleSystem[choiceRoots.Length];
        bool[] autoRandomSeed = new bool[choiceRoots.Length];
        uint[] randomSeeds = new uint[choiceRoots.Length];
        int[] particleCounts = new int[choiceRoots.Length];
        string[] particleScreenBounds = new string[choiceRoots.Length];
        WallScript[] previewWalls = new WallScript[choiceRoots.Length];
        TMPro.TextMeshProUGUI[] previewLabels =
            new TMPro.TextMeshProUGUI[choiceRoots.Length];
        string[] originalLabels = new string[choiceRoots.Length];
        string[] originalValues = new string[choiceRoots.Length];
        Sprite[] originalIcons = new Sprite[choiceRoots.Length];
        Color[] originalValueColors = new Color[choiceRoots.Length];
        Color[] originalOutlineColors = new Color[choiceRoots.Length];
        bool[] originalLocalizationEnabled = new bool[choiceRoots.Length];
        BuffType[] originalBonusTypes = new BuffType[choiceRoots.Length];
        BonusChoiceAltarVfx[] previewVfx = new BonusChoiceAltarVfx[choiceRoots.Length];
        System.Reflection.MethodInfo refreshPresentation =
            typeof(BonusChoiceAltarVfx).GetMethod(
                "RefreshPresentation",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
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

                previewWalls[index] = choiceRoots[index]
                    .GetComponentInChildren<WallScript>(true);
                WallScript wall = previewWalls[index];
                if (wall == null || wall.statNameLoc == null || wall.statValueTmp == null)
                    throw new InvalidOperationException(
                        $"Choice UI references were not found: {choiceRoots[index].name}");

                previewLabels[index] =
                    wall.statNameLoc.GetComponent<TMPro.TextMeshProUGUI>();
                originalLabels[index] = previewLabels[index].text;
                originalValues[index] = wall.statValueTmp.text;
                originalValueColors[index] = wall.statValueTmp.color;
                originalLocalizationEnabled[index] = wall.statNameLoc.enabled;
                originalBonusTypes[index] = wall.buffType;
                if (wall.statIconImage != null)
                    originalIcons[index] = wall.statIconImage.sprite;
                if (wall.statBadgeOutline != null)
                    originalOutlineColors[index] = wall.statBadgeOutline.effectColor;

                BuffType previewType = index == 0
                    ? BuffType.hp_normal
                    : BuffType.attackSpeed_normal;
                Color previewAccent = BonusChoiceAltarVfx.ResolveUiAccent(previewType);
                wall.statNameLoc.enabled = false;
                previewLabels[index].enabled = true;
                previewLabels[index].text = index == 0 ? "HEALTH" : "ATK SPEED";
                wall.statValueTmp.enabled = true;
                wall.statValueTmp.text = index == 0 ? "+999" : "+11%";
                wall.statValueTmp.color = previewAccent;
                if (wall.statIconImage != null)
                    wall.statIconImage.sprite = LoadBonusIcon(previewType);
                if (wall.statBadgeImage != null)
                    wall.statBadgeImage.enabled = true;
                if (wall.statBadgeOutline != null)
                {
                    previewAccent.a = 0.9f;
                    wall.statBadgeOutline.enabled = true;
                    wall.statBadgeOutline.effectColor = previewAccent;
                }

                previewVfx[index] = choiceRoots[index]
                    .GetComponent<BonusChoiceAltarVfx>();
                if (previewVfx[index] != null && refreshPresentation != null)
                {
                    previewVfx[index].SetBonusType(previewType);
                    refreshPresentation.Invoke(previewVfx[index], null);
                }

                WallStatCanvasBillboard[] billboards = choiceRoots[index]
                    .GetComponentsInChildren<WallStatCanvasBillboard>(true);
                if (billboards.Length == 0)
                    throw new InvalidOperationException(
                        $"Choice billboard was not found: {choiceRoots[index].name}");

                foreach (WallStatCanvasBillboard billboard in billboards)
                {
                    billboardTransforms.Add(billboard.transform);
                    billboardRotations.Add(billboard.transform.rotation);
                    billboardComponents.Add(billboard);
                    billboardEnabledStates.Add(billboard.enabled);
                    billboard.FaceCamera(previewCamera);
                    billboard.enabled = false;
                }
                RectTransform statBadge = canvas.Find("Stat_Badge") as RectTransform;
                if (statBadge != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(statBadge);
                previewLabels[index].ForceMeshUpdate(true, true);
                wall.statValueTmp.ForceMeshUpdate(true, true);

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
                if (billboardComponents[index] != null)
                    billboardComponents[index].enabled = billboardEnabledStates[index];
                if (billboardTransforms[index] != null)
                    billboardTransforms[index].rotation = billboardRotations[index];
            }

            for (int index = 0; index < choiceRoots.Length; index++)
            {
                WallScript wall = previewWalls[index];
                if (wall != null)
                {
                    wall.statNameLoc.enabled = false;
                    previewLabels[index].text = originalLabels[index];
                    wall.statValueTmp.text = originalValues[index];
                    wall.statValueTmp.color = originalValueColors[index];
                    if (wall.statIconImage != null)
                        wall.statIconImage.sprite = originalIcons[index];
                    if (wall.statBadgeOutline != null)
                        wall.statBadgeOutline.effectColor = originalOutlineColors[index];
                    wall.statNameLoc.enabled = originalLocalizationEnabled[index];
                    if (wall.statNameLoc.enabled)
                        wall.statNameLoc.RefreshString();
                }
                if (previewVfx[index] != null && refreshPresentation != null)
                {
                    previewVfx[index].SetBonusType(originalBonusTypes[index]);
                    refreshPresentation.Invoke(previewVfx[index], null);
                }

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
        RequireAsset(VerticalGlowTexturePath);
        RequireAsset(VerticalImpactTexturePath);
        RequireAsset(GlowAddTexturePath);

        foreach (string iconFileName in BonusIconFileNames)
            RequireAsset(BonusIconResourceFolder + "/" + iconFileName);

        foreach (string rendererDataPath in RendererDataPaths)
            RequireAsset(rendererDataPath);

        if (Shader.Find(ShaderName) == null)
            throw new InvalidOperationException($"Could not find required shader: {ShaderName}");

        if (Shader.Find(UnlitShaderName) == null)
            throw new InvalidOperationException($"Could not find required shader: {UnlitShaderName}");
    }

    private static void RemoveLegacyRuneAssets()
    {
        string[] obsoleteAssetPaths =
        {
            MaterialFolder + "/BonusBox_AttackGlow.mat",
            MaterialFolder + "/BonusBox_AttackArcUnderglow.mat",
            MaterialFolder + "/BonusBox_AttackRuneCircle.mat",
            MaterialFolder + "/BonusBox_AttackParticles.mat",
            GeneratedTextureFolder + "/BonusBox_MagicCircle.png",
            GeneratedTextureFolder + "/BonusBox_EnergyMote.png"
        };
        foreach (string assetPath in obsoleteAssetPaths)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
        }
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
        Texture2D waterVortexTexture,
        Texture2D waterFoamTexture,
        Texture2D warpCompassTexture,
        Texture2D softAuraTexture,
        Texture2D verticalGlowTexture,
        Texture2D verticalImpactTexture,
        Texture2D glowAddTexture,
        Texture2D iconTexture,
        Texture2D wearCracksTexture)
    {
        string prefix = "BonusBox_" + variant;
        Color waterColor = new(
            glowColor.r * 0.82f,
            glowColor.g * 0.82f,
            glowColor.b * 0.82f,
            0.58f);
        Color foamColor = new(
            glowColor.r * 1.08f,
            glowColor.g * 1.08f,
            glowColor.b * 1.08f,
            0.78f);
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
            WaterVortex = CreateOrUpdateGlowMaterial(
                prefix + "WaterVortex",
                MaterialFolder + "/" + prefix + "WaterVortex.mat",
                waterColor,
                additive: true,
                texture: waterVortexTexture),
            WaterFoam = CreateOrUpdateGlowMaterial(
                prefix + "WaterFoam",
                MaterialFolder + "/" + prefix + "WaterFoam.mat",
                foamColor,
                additive: true,
                texture: waterFoamTexture),
            WarpCompass = CreateOrUpdateGlowMaterial(
                prefix + "WarpCompass",
                MaterialFolder + "/" + prefix + "WarpCompass.mat",
                new Color(glowColor.r, glowColor.g, glowColor.b, 0.72f),
                additive: true,
                texture: warpCompassTexture),
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
                prefix + "WaterDroplets",
                MaterialFolder + "/" + prefix + "WaterDroplets.mat",
                particleColor,
                additive: true,
                texture: softAuraTexture)
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
            EffectTextureShape.WaterVortex or
                EffectTextureShape.WaterFoam or
                EffectTextureShape.WarpCompass => 256,
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
            case EffectTextureShape.WaterVortex:
                return WaterVortexAlpha(x, y);
            case EffectTextureShape.WaterFoam:
                return WaterFoamAlpha(x, y);
            case EffectTextureShape.WarpCompass:
                return WarpCompassAlpha(x, y);
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

    private static float WaterVortexAlpha(float x, float y)
    {
        float radius = Mathf.Sqrt(x * x + y * y);
        if (radius < 0.12f || radius > 1f)
            return 0f;

        float angle = Mathf.Atan2(y, x);
        float radialMask = RangeAlpha(radius, 0.14f, 0.97f, 0.035f);
        float spiralWave = 0.5f + 0.5f * Mathf.Cos(angle * 3f - radius * 17.5f);
        float spiral = SmoothThreshold(0.68f, 0.94f, spiralWave) *
                       radialMask * Mathf.Lerp(0.5f, 1f, radius);
        float ripples = Mathf.Max(
            RingAlpha(radius, 0.32f, 0.018f),
            Mathf.Max(
                RingAlpha(radius, 0.57f, 0.018f),
                RingAlpha(radius, 0.84f, 0.02f))) * 0.42f;
        float hollowRim = RingAlpha(radius, 0.18f, 0.016f) * 0.65f;
        return Mathf.Clamp01(Mathf.Max(spiral * 0.82f, Mathf.Max(ripples, hollowRim)));
    }

    private static float WaterFoamAlpha(float x, float y)
    {
        float radius = Mathf.Sqrt(x * x + y * y);
        if (radius < 0.28f || radius > 1f)
            return 0f;

        float angle = Mathf.Atan2(y, x);
        float noise = Mathf.Sin(angle * 11f + radius * 31f) * 0.62f +
                      Mathf.Sin(angle * 7f - radius * 19f) * 0.38f;
        float brokenFoam = SmoothThreshold(0.18f, 0.72f, noise);
        float outerBand = RangeAlpha(radius, 0.62f, 0.97f, 0.035f);
        float innerCurlWave = 0.5f + 0.5f * Mathf.Cos(angle * 2f - radius * 14f);
        float innerCurl = SmoothThreshold(0.78f, 0.96f, innerCurlWave) *
                          RangeAlpha(radius, 0.3f, 0.72f, 0.04f) * 0.62f;
        float rim = RingAlpha(radius, 0.82f, 0.028f) *
                    Mathf.Lerp(0.25f, 0.75f, Mathf.Clamp01(noise * 0.5f + 0.5f));
        return Mathf.Clamp01(Mathf.Max(outerBand * brokenFoam, Mathf.Max(innerCurl, rim)));
    }

    private static float WarpCompassAlpha(float x, float y)
    {
        float radius = Mathf.Sqrt(x * x + y * y);
        if (radius < 0.18f || radius > 1f)
            return 0f;

        float angle = Mathf.Atan2(y, x);
        float rings = Mathf.Max(
            RingAlpha(radius, 0.34f, 0.012f),
            Mathf.Max(
                RingAlpha(radius, 0.62f, 0.012f),
                RingAlpha(radius, 0.89f, 0.014f)));
        float tickWave = Mathf.Abs(Mathf.Cos(angle * 12f));
        float ticks = SmoothThreshold(0.88f, 0.98f, tickWave) *
                      RangeAlpha(radius, 0.76f, 0.98f, 0.025f);
        float cardinalWave = Mathf.Max(
            Mathf.Abs(Mathf.Cos(angle * 2f)),
            Mathf.Abs(Mathf.Sin(angle * 2f)));
        float cardinalTicks = SmoothThreshold(0.985f, 0.999f, cardinalWave) *
                              RangeAlpha(radius, 0.42f, 0.86f, 0.02f) * 0.8f;
        float spokes = Mathf.Max(
            SegmentStroke(new Vector2(x, y), new Vector2(-0.78f, 0f), new Vector2(0.78f, 0f), 0.007f),
            SegmentStroke(new Vector2(x, y), new Vector2(0f, -0.78f), new Vector2(0f, 0.78f), 0.007f)) * 0.55f;
        return Mathf.Clamp01(Mathf.Max(rings * 0.82f, Mathf.Max(ticks, Mathf.Max(cardinalTicks, spokes))));
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

            wall.buffType = BuffType.attackSpeed_normal;
            bool statLocalizationWasEnabled =
                ConfigureStatLocalization(wall, BuffType.attackSpeed_normal);
            RectTransform[] iconAuras = ConfigureStatIcon(
                root,
                wall,
                BuffType.attackSpeed_normal,
                0f);
            ConfigureStatValuePreview(wall);
            ConfigureStatTypography(wall);

            BonusChoiceAltarVfx altarVfx = root.GetComponent<BonusChoiceAltarVfx>();
            if (altarVfx == null)
                altarVfx = root.AddComponent<BonusChoiceAltarVfx>();
            altarVfx.Configure(
                false,
                glowRoot,
                null,
                iconAuras);

            AuthoredBonusWall authoredBonus = root.GetComponent<AuthoredBonusWall>();
            if (authoredBonus == null)
                authoredBonus = root.AddComponent<AuthoredBonusWall>();
            authoredBonus.Configure(Rarity.Normal);

            ConfigureStatLabelPreview(wall, BuffType.attackSpeed_normal);
            RebuildDataFirstLayout(wall);
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
            new Vector3(0f, WaterSurfaceHeight - 0.012f, 0f),
            new Vector2(WaterVortexDiameter * 1.45f, WaterVortexDiameter * 1.45f),
            Quaternion.Euler(90f, 0f, 0f),
            materials.GroundAura);

        GameObject energyBillboard = new GameObject("IconEnergyBillboard");
        energyBillboard.transform.SetParent(visual.transform, false);
        energyBillboard.transform.localPosition = new Vector3(
            0f,
            WaterSurfaceHeight + WaterVortexDiameter * 0.47f,
            -0.035f);
        energyBillboard.AddComponent<WallStatCanvasBillboard>();

        GameObject iconHalo = CreateQuadPiece(
            energyBillboard.transform,
            "IconEnergyHalo",
            new Vector3(0f, -0.02f, -0.001f),
            new Vector2(
                WaterVortexDiameter * 1.28f,
                WaterVortexDiameter * 0.84f),
            Quaternion.identity,
            materials.IconHalo);
        iconHalo.GetComponent<MeshRenderer>().sortingOrder = 0;

        GameObject centralBeam = CreateQuadPiece(
            energyBillboard.transform,
            "VerticalBeam",
            Vector3.zero,
            new Vector2(
                WaterVortexDiameter * 1.1f,
                WaterVortexDiameter * 0.95f),
            Quaternion.identity,
            materials.Beam);
        centralBeam.GetComponent<MeshRenderer>().sortingOrder = 0;

        GameObject energyCore = CreateQuadPiece(
            energyBillboard.transform,
            "IconEnergyCore",
            new Vector3(0f, -0.035f, 0.001f),
            new Vector2(
                WaterVortexDiameter * 0.5f,
                WaterVortexDiameter * 0.85f),
            Quaternion.identity,
            materials.IconCore);
        energyCore.GetComponent<MeshRenderer>().sortingOrder = 1;

        float veilSideOffset = WaterVortexDiameter * 0.22f;
        float veilTilt = aggressiveVfx ? 9f : 7f;
        GameObject leftVeil = CreateQuadPiece(
            energyBillboard.transform,
            "IconEnergyVeilLeft",
            new Vector3(-veilSideOffset, -0.015f, 0.001f),
            new Vector2(
                WaterVortexDiameter * (aggressiveVfx ? 0.42f : 0.38f),
                WaterVortexDiameter * 0.93f),
            Quaternion.Euler(0f, 0f, -veilTilt),
            materials.IconVeil);
        GameObject rightVeil = CreateQuadPiece(
            energyBillboard.transform,
            "IconEnergyVeilRight",
            new Vector3(veilSideOffset, 0.015f, 0.002f),
            new Vector2(
                WaterVortexDiameter * (aggressiveVfx ? 0.42f : 0.38f),
                WaterVortexDiameter * 0.93f),
            Quaternion.Euler(0f, 0f, veilTilt),
            materials.IconVeil);
        leftVeil.GetComponent<MeshRenderer>().sortingOrder = 2;
        rightVeil.GetComponent<MeshRenderer>().sortingOrder = 2;

        GameObject glowRoot = new GameObject("GlowOrbit");
        glowRoot.transform.SetParent(visual.transform, false);
        glowRoot.transform.localPosition = new Vector3(0f, WaterSurfaceHeight, 0f);

        CreateWaterVortexLayer(
            glowRoot.transform,
            "WaterVortexOuter",
            0.001f,
            WaterVortexDiameter * 1.08f,
            0f,
            materials.WaterVortex);
        CreateWaterVortexLayer(
            glowRoot.transform,
            "WaterVortexInner",
            0.005f,
            WaterVortexDiameter * 0.76f,
            37f,
            materials.WaterVortex);
        CreateWaterVortexLayer(
            glowRoot.transform,
            "WarpCompass",
            0.008f,
            WaterVortexDiameter * 1.18f,
            11f,
            materials.WarpCompass);
        CreateWaterVortexLayer(
            glowRoot.transform,
            "WaterFoam",
            0.011f,
            WaterVortexDiameter * 1.13f,
            19f,
            materials.WaterFoam);

        CreateParticleEffect(
            visual.transform,
            materials.Particles,
            aggressiveVfx);
        return glowRoot.transform;
    }

    private static Transform CreateWaterVortexLayer(
        Transform parent,
        string name,
        float height,
        float diameter,
        float yaw,
        Material material)
    {
        GameObject layer = new GameObject(name);
        layer.transform.SetParent(parent, false);
        layer.transform.localPosition = new Vector3(0f, height, 0f);
        layer.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        CreateQuadPiece(
            layer.transform,
            "Surface",
            Vector3.zero,
            new Vector2(diameter, diameter),
            Quaternion.Euler(90f, 0f, 0f),
            material);
        return layer.transform;
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
        effectObject.transform.localPosition = new Vector3(0f, WaterSurfaceHeight + 0.015f, 0f);
        effectObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem particles = effectObject.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = aggressiveVfx
            ? new ParticleSystem.MinMaxCurve(0.55f, 0.95f)
            : new ParticleSystem.MinMaxCurve(0.75f, 1.2f);
        main.startSpeed = aggressiveVfx
            ? new ParticleSystem.MinMaxCurve(0.42f, 0.72f)
            : new ParticleSystem.MinMaxCurve(0.24f, 0.48f);
        main.startSize3D = true;
        main.startSizeX = aggressiveVfx
            ? new ParticleSystem.MinMaxCurve(0.055f, 0.095f)
            : new ParticleSystem.MinMaxCurve(0.04f, 0.075f);
        main.startSizeY = aggressiveVfx
            ? new ParticleSystem.MinMaxCurve(0.11f, 0.19f)
            : new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
        main.startSizeZ = main.startSizeX;
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.28f, 0.28f);
        main.startColor = Color.white;
        main.maxParticles = aggressiveVfx ? 28 : 20;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = aggressiveVfx ? 19f : 13f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = WaterVortexDiameter * (aggressiveVfx ? 0.4f : 0.43f);
        shape.radiusThickness = aggressiveVfx ? 0.58f : 0.48f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.orbitalZ = aggressiveVfx ? 1.75f : 1.15f;
        velocity.radial = aggressiveVfx ? -0.3f : -0.2f;

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
                new Keyframe(0f, 0.45f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, aggressiveVfx ? 0.18f : 0.28f)));

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

        RectTransform statText = wall.statNameLoc != null
            ? wall.statNameLoc.GetComponent<RectTransform>()
            : null;
        RectTransform valueText = wall.statValueTmp != null
            ? wall.statValueTmp.rectTransform
            : null;
        Transform existingIcon = wall.statIconImage != null
            ? wall.statIconImage.transform
            : canvas.Find("Stat_Icon");
        if (statText == null || valueText == null)
            throw new InvalidOperationException("Wall template stat text was not found.");

        statText.SetParent(canvas, false);
        valueText.SetParent(canvas, false);
        if (existingIcon != null)
            existingIcon.SetParent(canvas, false);

        RemoveChoiceBackgroundChrome(canvas);
        RemoveGeneratedCanvasChild(canvas, "Stat_Row");
        RemoveGeneratedCanvasChild(canvas, "Choice_Title");
        RemoveGeneratedCanvasChild(canvas, "Stat_Icon_AuraOuter");
        RemoveGeneratedCanvasChild(canvas, "Stat_Icon_AuraInner");

        bool isAttack = buffType is
            BuffType.att_normmal or
            BuffType.att_unique or
            BuffType.attPer_normal or
            BuffType.attPer_unique or
            BuffType.attackSpeed_normal or
            BuffType.attackSpeed_unique;
        Color accent = isAttack
            ? new Color(1f, 0.48f, 0.03f, 0.9f)
            : new Color(0.02f, 0.9f, 1f, 0.85f);

        Transform existingBadge = canvas.Find("Stat_Badge");
        GameObject badgeObject = existingBadge != null
            ? existingBadge.gameObject
            : new GameObject(
                "Stat_Badge",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
        badgeObject.layer = canvas.gameObject.layer;
        RectTransform badge = badgeObject.GetComponent<RectTransform>();
        badge.SetParent(canvas, false);
        badge.anchorMin = new Vector2(StatBadgeLeftAnchor, StatBadgeBottomAnchor);
        badge.anchorMax = new Vector2(StatBadgeRightAnchor, StatBadgeTopAnchor);
        badge.anchoredPosition = Vector2.zero;
        badge.sizeDelta = Vector2.zero;
        badge.localScale = Vector3.one;

        Image badgeImage = badgeObject.GetComponent<Image>();
        badgeImage.sprite = null;
        badgeImage.type = Image.Type.Simple;
        badgeImage.color = new Color(0.008f, 0.014f, 0.024f, 0.97f);
        badgeImage.raycastTarget = false;
        foreach (Shadow effect in badgeObject.GetComponents<Shadow>())
            UnityEngine.Object.DestroyImmediate(effect);
        var badgeOutline = badgeObject.AddComponent<UnityEngine.UI.Outline>();
        badgeOutline.effectColor = new Color(accent.r, accent.g, accent.b, 0.9f);
        badgeOutline.effectDistance = new Vector2(0.01f, 0.01f);
        badgeOutline.useGraphicAlpha = false;
        wall.statBadgeImage = badgeImage;
        wall.statBadgeOutline = badgeOutline;

        GameObject iconObject = existingIcon != null
            ? existingIcon.gameObject
            : new GameObject(
                "Stat_Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
        iconObject.layer = canvas.gameObject.layer;
        RectTransform iconTransform = iconObject.GetComponent<RectTransform>();
        iconTransform.SetParent(badge, false);
        iconTransform.anchorMin = new Vector2(0.055f, 0.18f);
        iconTransform.anchorMax = new Vector2(0.28f, 0.82f);
        iconTransform.anchoredPosition = Vector2.zero;
        iconTransform.sizeDelta = Vector2.zero;
        iconTransform.localScale = Vector3.one;

        Image image = iconObject.GetComponent<Image>();
        image.sprite = LoadBonusIcon(buffType);
        image.color = Color.white;
        image.material = null;
        image.preserveAspect = true;
        image.raycastTarget = false;
        wall.statIconImage = image;
        foreach (Shadow effect in iconObject.GetComponents<Shadow>())
            UnityEngine.Object.DestroyImmediate(effect);

        statText.SetParent(badge, false);
        statText.anchorMin = new Vector2(0.3f, 0.08f);
        statText.anchorMax = new Vector2(0.95f, 0.92f);
        statText.anchoredPosition3D = new Vector3(0f, 0f, -0.01f);
        statText.sizeDelta = Vector2.zero;
        statText.localScale = Vector3.one;
        statText.localRotation = Quaternion.identity;
        Canvas statTextCanvas = statText.GetComponent<Canvas>();
        if (statTextCanvas != null)
            UnityEngine.Object.DestroyImmediate(statTextCanvas);

        valueText.SetParent(canvas, false);
        valueText.anchorMin = new Vector2(0.05f, StatValueBottomAnchor);
        valueText.anchorMax = new Vector2(0.95f, StatValueTopAnchor);
        valueText.pivot = new Vector2(0.5f, 0.5f);
        valueText.anchoredPosition3D = new Vector3(0f, 0f, -0.02f);
        valueText.sizeDelta = Vector2.zero;
        valueText.localScale = Vector3.one;
        valueText.localRotation = Quaternion.identity;
        Canvas valueTextCanvas = valueText.GetComponent<Canvas>();
        if (valueTextCanvas != null)
            UnityEngine.Object.DestroyImmediate(valueTextCanvas);
        badge.SetAsLastSibling();
        valueText.SetAsLastSibling();

        return Array.Empty<RectTransform>();
    }

    private static void RemoveGeneratedCanvasChild(Transform canvas, string childName)
    {
        Transform child = canvas.Find(childName);
        if (child != null)
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static void RemoveChoiceBackgroundChrome(Transform canvas)
    {
        string[] obsoleteObjectNames =
        {
            "Choice_TextBackplate",
            "Choice_AccentBar",
            "Choice_TitleBackplate",
            "Choice_InfoBackplate"
        };
        foreach (string objectName in obsoleteObjectNames)
        {
            Transform existing = canvas.Find(objectName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }

    private static void RebuildDataFirstLayout(WallScript wall)
    {
        RectTransform badge = wall.statNameLoc != null
            ? wall.statNameLoc.transform.parent as RectTransform
            : null;
        if (badge == null || badge.name != "Stat_Badge")
            throw new InvalidOperationException("The data-first stat badge was not configured.");

        LayoutRebuilder.ForceRebuildLayoutImmediate(badge);
        Canvas.ForceUpdateCanvases();
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
        title.text = "공격력 강화";
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
                BuffType.att_normmal or BuffType.att_unique => "ATTACK",
                BuffType.attackSpeed_normal or BuffType.attackSpeed_unique => "ATK SPEED",
                BuffType.hp_normal or BuffType.hp_unique => "HEALTH",
                _ => statText.text
            };
        }
    }

    private static void ConfigureStatValuePreview(WallScript wall)
    {
        if (wall.statValueTmp == null)
            return;

        wall.statValueTmp.text = "+11%";
    }

    private static void ConfigureStatTypography(WallScript wall)
    {
        TMPro.TextMeshProUGUI statName =
            wall.statNameLoc != null
                ? wall.statNameLoc.GetComponent<TMPro.TextMeshProUGUI>()
                : null;
        if (statName == null || wall.statValueTmp == null)
            throw new InvalidOperationException("Wall template stat text was not found.");

        statName.enableAutoSizing = true;
        statName.fontSizeMin = StatNameMinFontSize;
        statName.fontSizeMax = StatNameFontSize;
        statName.fontSize = StatNameFontSize;
        statName.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
        statName.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        statName.fontStyle = TMPro.FontStyles.Bold;
        statName.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        statName.overflowMode = TMPro.TextOverflowModes.Truncate;
        statName.color = new Color32(232, 237, 241, 255);
        statName.outlineWidth = 0.28f;
        statName.outlineColor = Color.black;
        wall.statValueTmp.enableAutoSizing = true;
        wall.statValueTmp.fontSizeMin = StatValueMinFontSize;
        wall.statValueTmp.fontSizeMax = StatValueFontSize;
        wall.statValueTmp.fontSize = StatValueFontSize;
        wall.statValueTmp.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
        wall.statValueTmp.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        wall.statValueTmp.fontStyle = TMPro.FontStyles.Bold;
        wall.statValueTmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        wall.statValueTmp.overflowMode = TMPro.TextOverflowModes.Truncate;
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
            $"WaterVortexTexture: {GeneratedTextureFolder}/BonusBox_WaterVortex.png\n" +
            $"WaterFoamTexture: {GeneratedTextureFolder}/BonusBox_WaterFoam.png\n" +
            $"WarpCompassTexture: {GeneratedTextureFolder}/BonusBox_WarpCompass.png\n" +
            $"EnergyMoteTexture: {GeneratedTextureFolder}/BonusBox_EnergyMote.png\n" +
            $"WearCracksTexture: {GeneratedTextureFolder}/BonusBox_WearCracks.png\n" +
            "CompactBeveledTierAltar: True\n" +
            "RecessedFramedFrontPanel: True\n" +
            $"AltarHorizontalScale: {AltarHorizontalScale}\n" +
            "PresentationOffset: Centered\n" +
            "DataFirstValueAndBadge: True\n" +
            "MaximumPreviewLabel: ATK SPEED\n" +
            "ChoiceTextBackgroundChrome: False\n" +
            "ChoiceTextUnderlay: True\n" +
            "StaticSemanticMotes: False\n" +
            "CompactBadgeIcon: True\n" +
            "SingleReusableAltar: True\n" +
            "RuneMeshesAThroughI: False\n" +
            "LayeredWaterVortex: True\n" +
            "RotatingWarpCompass: True\n" +
            "LayeredIconAura: False\n" +
            "AnimatedGlowOrbit: True\n" +
            "RisingWaterDroplets: True\n" +
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
