using System.Collections.Generic;
using System.IO;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MonsterGrowthAndMapToolEnemyTests
{
    [Test]
    public void Progress_UsesFirstAndLastEnemiesAsEndpoints()
    {
        Assert.That(MonsterStatInterpolator.CalculateProgress(0, 20), Is.Zero);
        Assert.That(
            MonsterStatInterpolator.CalculateProgress(9, 20),
            Is.EqualTo(9f / 19f).Within(0.0001f));
        Assert.That(MonsterStatInterpolator.CalculateProgress(19, 20), Is.EqualTo(1f));
        Assert.That(MonsterStatInterpolator.CalculateProgress(-5, 20), Is.Zero);
        Assert.That(MonsterStatInterpolator.CalculateProgress(99, 20), Is.EqualTo(1f));
        Assert.That(MonsterStatInterpolator.CalculateProgress(0, 1), Is.Zero);
    }

    [Test]
    public void Evaluate_InterpolatesDamageAndHealthIndependently()
    {
        MonsterGrowthRow growth = CreateValidGrowthRow(1, EnemyTier.Elite);
        growth.initialDamage = 20f;
        growth.finalDamage = 220f;
        growth.initialHealth = 100f;
        growth.finalHealth = 1100f;

        MonsterStatInterpolator.Evaluate(
            growth,
            0.5f,
            out float damage,
            out float health);

        Assert.That(damage, Is.EqualTo(120f).Within(0.001f));
        Assert.That(health, Is.EqualTo(600f).Within(0.001f));
    }

    [Test]
    public void RouteDistance_FollowsTurnsRegardlessOfTurnListOrder()
    {
        ChapterRouteTurn firstTurn = new(
            new Vector3(0f, 0f, 10f),
            Vector3.right,
            "Turn01");
        ChapterRouteTurn secondTurn = new(
            new Vector3(10f, 0f, 10f),
            Vector3.back,
            "Turn02");
        ChapterRouteTurn[] turns = { secondTurn, firstTurn };

        float beforeFirstTurn = ChapterEnemyProgression.CalculateRouteDistance(
            new Vector3(0f, 0f, 4f),
            Vector3.zero,
            Vector3.forward,
            turns);
        float betweenTurns = ChapterEnemyProgression.CalculateRouteDistance(
            new Vector3(6f, 0f, 10f),
            Vector3.zero,
            Vector3.forward,
            turns);
        float afterSecondTurn = ChapterEnemyProgression.CalculateRouteDistance(
            new Vector3(10f, 0f, 4f),
            Vector3.zero,
            Vector3.forward,
            turns);

        Assert.That(beforeFirstTurn, Is.EqualTo(4f).Within(0.001f));
        Assert.That(betweenTurns, Is.EqualTo(16f).Within(0.001f));
        Assert.That(afterSecondTurn, Is.EqualTo(26f).Within(0.001f));
    }

    [Test]
    public void RouteDirection_ReturnsTravelAxesAcrossLaterallyOffsetRouteSections()
    {
        ChapterRouteTurn firstTurn = new(
            new Vector3(2f, 0f, 10f),
            new Vector3(3f, 4f, 0f),
            "Turn01");
        ChapterRouteTurn secondTurn = new(
            new Vector3(10f, 0f, 12f),
            new Vector3(0f, -2f, -4f),
            "Turn02");
        ChapterRouteTurn[] turns = { secondTurn, firstTurn };

        Vector3 beforeFirstTurn = ChapterEnemyProgression.CalculateRouteDirection(
            new Vector3(0f, 3f, 4f),
            Vector3.zero,
            new Vector3(0f, 3f, 2f),
            turns);
        Vector3 betweenTurns = ChapterEnemyProgression.CalculateRouteDirection(
            new Vector3(6f, -5f, 10f),
            Vector3.zero,
            new Vector3(0f, 3f, 2f),
            turns);
        Vector3 afterSecondTurn = ChapterEnemyProgression.CalculateRouteDirection(
            new Vector3(10f, 9f, 4f),
            Vector3.zero,
            new Vector3(0f, 3f, 2f),
            turns);

        Assert.That(beforeFirstTurn, Is.EqualTo(Vector3.forward));
        Assert.That(betweenTurns, Is.EqualTo(Vector3.right));
        Assert.That(afterSecondTurn, Is.EqualTo(Vector3.back));
    }

    [Test]
    public void RouteDirection_AtTurnKeepsEarlierRouteCandidateOnTie()
    {
        ChapterRouteTurn[] turns =
        {
            new(
                new Vector3(2f, 0f, 10f),
                Vector3.right,
                "Turn01")
        };

        Vector3 direction = ChapterEnemyProgression.CalculateRouteDirection(
            new Vector3(2f, 5f, 10f),
            Vector3.zero,
            Vector3.forward,
            turns);

        Assert.That(direction, Is.EqualTo(Vector3.forward));
    }

    [Test]
    public void RouteDirection_InvalidInitialDirectionFallsBackToForward()
    {
        Vector3 direction = ChapterEnemyProgression.CalculateRouteDirection(
            new Vector3(4f, 7f, 12f),
            Vector3.zero,
            Vector3.up,
            turns: null);

        Assert.That(direction, Is.EqualTo(Vector3.forward));
    }

    [Test]
    public void GrowthRows_RequireEveryTierForEveryContiguousChapter()
    {
        MonsterGrowthRow[] completeRows =
        {
            CreateValidGrowthRow(1, EnemyTier.Normal),
            CreateValidGrowthRow(1, EnemyTier.Elite),
            CreateValidGrowthRow(1, EnemyTier.Boss),
            CreateValidGrowthRow(2, EnemyTier.Normal),
            CreateValidGrowthRow(2, EnemyTier.Elite),
            CreateValidGrowthRow(2, EnemyTier.Boss)
        };

        Assert.DoesNotThrow(() => MonsterGrowthTables.ValidateRows(completeRows));
        Assert.That(
            GameManager.TryCollectGrowthRowsByChapter(
                completeRows,
                out Dictionary<int, Dictionary<EnemyTier, MonsterGrowthRow>> byChapter),
            Is.True);
        Assert.That(byChapter.Count, Is.EqualTo(2));
        Assert.That(byChapter[2].Count, Is.EqualTo(3));

        MonsterGrowthRow[] missingChapterRows =
        {
            CreateValidGrowthRow(1, EnemyTier.Normal),
            CreateValidGrowthRow(1, EnemyTier.Elite),
            CreateValidGrowthRow(1, EnemyTier.Boss),
            CreateValidGrowthRow(3, EnemyTier.Normal),
            CreateValidGrowthRow(3, EnemyTier.Elite),
            CreateValidGrowthRow(3, EnemyTier.Boss)
        };
        Assert.Throws<InvalidDataException>(
            () => MonsterGrowthTables.ValidateRows(missingChapterRows));
    }

    [Test]
    public void GrowthRows_RejectDuplicateTierAndInvalidCombatValues()
    {
        MonsterGrowthRow[] duplicateRows =
        {
            CreateValidGrowthRow(1, EnemyTier.Normal),
            CreateValidGrowthRow(1, EnemyTier.Normal),
            CreateValidGrowthRow(1, EnemyTier.Boss)
        };
        Assert.Throws<InvalidDataException>(
            () => MonsterGrowthTables.ValidateRows(duplicateRows));
        Assert.That(GameManager.TryCollectGrowthRowsByChapter(duplicateRows, out _), Is.False);

        MonsterGrowthRow invalidNormal = CreateValidGrowthRow(1, EnemyTier.Normal);
        invalidNormal.initialDamage = -1f;
        MonsterGrowthRow invalidElite = CreateValidGrowthRow(1, EnemyTier.Elite);
        invalidElite.coefficient = 0f;
        MonsterGrowthRow[] invalidRows =
        {
            invalidNormal,
            invalidElite,
            CreateValidGrowthRow(1, EnemyTier.Boss)
        };
        Assert.Throws<InvalidDataException>(
            () => MonsterGrowthTables.ValidateRows(invalidRows));
    }

    [Test]
    public void GameManager_BuildsEveryActualWorkbookChapterEndpoint()
    {
        GameObject managerObject = new("MonsterGrowthTestManager");
        try
        {
            List<MonsterGrowthRow> rows = MonsterGrowthTables.GetAll();
            MonsterGrowthTables.ValidateRows(rows);
            Assert.That(rows.Count, Is.EqualTo(15));
            Assert.That(
                GameManager.TryCollectGrowthRowsByChapter(rows, out var byChapter),
                Is.True);
            Assert.That(byChapter.Count, Is.EqualTo(5));

            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.maxChapter = 10;
            manager.maxStage = 10;
            Assert.DoesNotThrow(manager.SettingMonsterStats);

            foreach (MonsterGrowthRow row in rows)
            {
                List<List<EnemyStat>> stats = row.tier switch
                {
                    EnemyTier.Normal => manager.normalMonster,
                    EnemyTier.Elite => manager.eliteMonster,
                    EnemyTier.Boss => manager.bossMonster,
                    _ => null
                };
                Assert.That(
                    stats[row.chapter][1].damage,
                    Is.EqualTo(row.initialDamage).Within(0.001f),
                    row.ToString());
                Assert.That(
                    stats[row.chapter][1].health,
                    Is.EqualTo(row.initialHealth).Within(0.001f),
                    row.ToString());
                Assert.That(
                    stats[row.chapter][10].damage,
                    Is.EqualTo(row.finalDamage).Within(0.001f),
                    row.ToString());
                Assert.That(
                    stats[row.chapter][10].health,
                    Is.EqualTo(row.finalHealth).Within(0.001f),
                    row.ToString());
            }
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            GameManager.S = null;
            MonsterGrowthTables.Reload();
        }
    }

    [Test]
    public void ChapterController_AppliesGrowthWithoutGameManager()
    {
        GameObject controllerObject = new("ChapterGrowthRouteController");
        GameObject firstObject = new("Enemy_Guard_First");
        GameObject middleObject = new("Enemy_FatMan_Middle");
        GameObject lastObject = new("Enemy_Woman_Last");
        try
        {
            firstObject.transform.position = Vector3.zero;
            middleObject.transform.position = new Vector3(0f, 0f, 5f);
            lastObject.transform.position = new Vector3(0f, 0f, 10f);
            EnemyScript_space first = firstObject.AddComponent<EnemyScript_space>();
            EnemyScript_space middle = middleObject.AddComponent<EnemyScript_space>();
            EnemyScript_space last = lastObject.AddComponent<EnemyScript_space>();

            ChapterEnemyStatController controller =
                controllerObject.AddComponent<ChapterEnemyStatController>();
            controller.Configure(1);
            Assert.That(controller.ApplyStats(), Is.EqualTo(3));

            AssertRuntimeStats(first, 33f, 55f);
            AssertRuntimeStats(middle, 302.5f, 605f);
            AssertRuntimeStats(last, 1650f, 1870f);
        }
        finally
        {
            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(middleObject);
            Object.DestroyImmediate(lastObject);
            Object.DestroyImmediate(controllerObject);
            MonsterGrowthTables.Reload();
        }
    }

    [Test]
    public void MapTool_ExposesObjectEnemyGimmickAndBonusTabs()
    {
        Assert.That(
            NoryangjinMapToolWindow.ContentTabLabels,
            Is.EqualTo(new[] { "오브젝트", "적군", "기믹", "보너스" }));
        Assert.That(
            NoryangjinMapToolWindow.IsPaletteSectionVisible(
                NoryangjinMapToolContentTab.Enemy,
                NoryangjinMapToolPaletteSection.Enemy),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.IsPaletteSectionVisible(
                NoryangjinMapToolContentTab.Enemy,
                NoryangjinMapToolPaletteSection.Gimmick),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.IsPaletteSectionVisible(
                NoryangjinMapToolContentTab.Bonus,
                NoryangjinMapToolPaletteSection.Bonus),
            Is.True);
    }

    [Test]
    public void BonusPalette_ExposesPlayableWallsOnItsOwnPlacementLayer()
    {
        string[] prefabPaths =
            NoryangjinMapToolWindow.FindBonusWallPalettePrefabPaths();

        Assert.That(
            prefabPaths,
            Is.EqualTo(new[]
            {
                NoryangjinMapToolWindow.BonusWallPrefabRoot +
                "/Box_left.prefab"
            }));
        Assert.That(
            NoryangjinMapToolWindow.BuildBonusWallPaletteLabel(
                NoryangjinMapToolWindow.BonusWallPrefabRoot +
                "/Box_left.prefab"),
            Is.EqualTo("운명의 제단"));

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            Assert.That(
                prefab.GetComponentInChildren<WallScript>(true),
                Is.Not.Null,
                prefabPath);
            Assert.That(
                NoryangjinMapToolWindow.GetPaletteItemLayer(
                    prefabPath,
                    NoryangjinMapToolPaletteCategory.Prop),
                Is.EqualTo(NoryangjinMapToolPlacementLayer.Bonus),
                prefabPath);
        }

        Vector2Int[] footprint = { Vector2Int.zero };
        HashSet<NoryangjinMapToolOccupiedCell> occupiedCells = new()
        {
            new NoryangjinMapToolOccupiedCell(
                Vector2Int.zero,
                NoryangjinMapToolPlacementLayer.Object)
        };
        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                footprint,
                NoryangjinMapToolPlacementLayer.Bonus,
                occupiedCells),
            Is.True);
    }

    [Test]
    public void BonusAltar_UsesOneReusableRandomPrefab()
    {
        FeastOfFortuneWallSetup.BuildWallPrefabs();

        string prefabPath = NoryangjinMapToolWindow.FeastOfFortuneBonusWallPrefabPaths[0];
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(
            prefab.name,
            Is.EqualTo(Path.GetFileNameWithoutExtension(prefabPath)),
            "Unity normalizes a prefab root name to the prefab asset filename.");

        AuthoredBonusWall altar = prefab.GetComponent<AuthoredBonusWall>();
        WallScript wall = prefab.GetComponentInChildren<WallScript>(true);
        Assert.That(altar, Is.Not.Null);
        Assert.That(altar.Rarity, Is.EqualTo(Rarity.Normal));
        Assert.That(wall, Is.Not.Null);
        Assert.That(wall.isRandom, Is.True);
        Assert.That(wall.wallType, Is.EqualTo(WallType.BuffWall));
        Assert.That(wall.statValueTmp.text, Is.EqualTo("+11%"));
        Assert.That(
            prefab.transform.Find("GFX/Canvas/Choice_Title"),
            Is.Null,
            "The data-first layout does not render a separate choice title.");

        Assert.That(
            prefab.transform.Find("ChoiceAltarVisual/ChoiceGateFrame"),
            Is.Null,
            "The altar must stay visually simple instead of becoming a stall-like gate.");

        Assert.That(
            prefab.transform.Find("GFX/Canvas/Choice_InfoBackplate"),
            Is.Null,
            "The compact choice copy should stay cardless and attach through proximity.");

        RectTransform badge = prefab.transform
            .Find("GFX/Canvas/Stat_Badge") as RectTransform;
        RectTransform icon = prefab.transform
            .Find("GFX/Canvas/Stat_Badge/Stat_Icon") as RectTransform;
        Assert.That(badge, Is.Not.Null);
        Assert.That(icon, Is.Not.Null);
        Assert.That(icon.anchorMin.x, Is.EqualTo(0.055f).Within(0.001f));
        Assert.That(icon.anchorMax.x, Is.EqualTo(0.28f).Within(0.001f));
        Assert.That(wall.statBadgeImage, Is.Not.Null);
        Assert.That(wall.statBadgeOutline, Is.Not.Null);
        Assert.That(wall.statNameLoc.transform.parent, Is.EqualTo(badge));
        Assert.That(wall.statValueTmp.rectTransform.parent, Is.EqualTo(badge.parent));
        Assert.That(wall.statNameLoc.GetComponent<TMPro.TextMeshProUGUI>().enableAutoSizing, Is.True);
        Assert.That(wall.statValueTmp.enableAutoSizing, Is.True);
    }

    [Test]
    public void BonusAltar_ChoiceCopyUsesNoCrossLaneBackgroundChrome()
    {
        FeastOfFortuneWallSetup.BuildWallPrefabs();

        string prefabPath = NoryangjinMapToolWindow.FeastOfFortuneBonusWallPrefabPaths[0];
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(
            prefab.transform.Find("GFX/Canvas/Choice_TextBackplate"),
            Is.Null,
            "A wide backplate visually merges nearby left/right choices.");
        Assert.That(
            prefab.transform.Find("GFX/Canvas/Choice_AccentBar"),
            Is.Null,
            "A horizontal accent bar visually connects nearby choices.");
    }

    [Test]
    public void BonusChoiceWaterVortex_ReplacesRuneGeometryWithLayeredWaterEffects()
    {
        FeastOfFortuneWallSetup.BuildWallPrefabs();

        const string texturePath =
            "Assets/ShooterSurvival/Textures/Generated/BonusChoiceBoxes/BonusBox_WaterVortex.png";
        string absolutePath = Path.GetFullPath(texturePath);
        Assert.That(File.Exists(absolutePath), Is.True, texturePath);

        string prefabPath = NoryangjinMapToolWindow.FeastOfFortuneBonusWallPrefabPaths[0];
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Transform glow = prefab.transform.Find("ChoiceAltarVisual/GlowOrbit");
        Assert.That(glow, Is.Not.Null);
        Assert.That(glow.Find("WaterVortexOuter/Surface"), Is.Not.Null);
        Assert.That(glow.Find("WaterVortexInner/Surface"), Is.Not.Null);
        Assert.That(glow.Find("WarpCompass/Surface"), Is.Not.Null);
        Assert.That(glow.Find("WaterFoam/Surface"), Is.Not.Null);
        foreach (Transform child in glow)
            Assert.That(child.name, Does.Not.StartWith("Rune"));

        ParticleSystem droplets = prefab.transform
            .Find("ChoiceAltarVisual/ChoiceParticles")
            .GetComponent<ParticleSystem>();
        Assert.That(droplets.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Circle));
        Assert.That(droplets.velocityOverLifetime.enabled, Is.True);
        Assert.That(droplets.trails.enabled, Is.False);
        Assert.That(droplets.collision.enabled, Is.False);
        Assert.That(droplets.lights.enabled, Is.False);
        Assert.That(
            AssetDatabase.LoadMainAssetAtPath(
                "Assets/ShooterSurvival/Materials/Generated/BonusChoiceBoxes/BonusBox_AttackRuneCircle.mat"),
            Is.Null);
        Assert.That(
            AssetDatabase.LoadMainAssetAtPath(
                "Assets/ShooterSurvival/Textures/Generated/BonusChoiceBoxes/BonusBox_MagicCircle.png"),
            Is.Null);
        Assert.That(
            AssetDatabase.LoadMainAssetAtPath(
                "Assets/ShooterSurvival/Materials/Generated/BonusChoiceBoxes/BonusBox_AttackWarpCompass.mat"),
            Is.Not.Null);
        Assert.That(
            File.Exists(Path.GetFullPath(
                "Assets/ShooterSurvival/Textures/Generated/BonusChoiceBoxes/BonusBox_WarpCompass.png")),
            Is.True);

        Material[] portalMaterials =
        {
            AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/ShooterSurvival/Materials/Generated/BonusChoiceBoxes/BonusBox_AttackWaterVortex.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/ShooterSurvival/Materials/Generated/BonusChoiceBoxes/BonusBox_AttackWaterFoam.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/ShooterSurvival/Materials/Generated/BonusChoiceBoxes/BonusBox_AttackWarpCompass.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/ShooterSurvival/Materials/Generated/BonusChoiceBoxes/BonusBox_AttackWaterDroplets.mat")
        };
        Assert.That(portalMaterials, Has.None.Null);
        Color authoredPortalColor = portalMaterials[0].GetColor("_BaseColor");
        Vector3 authoredPortalHue = new Vector3(
            authoredPortalColor.r,
            authoredPortalColor.g,
            authoredPortalColor.b).normalized;
        foreach (Material portalMaterial in portalMaterials)
        {
            Color portalColor = portalMaterial.GetColor("_BaseColor");
            Vector3 portalHue = new Vector3(
                portalColor.r,
                portalColor.g,
                portalColor.b).normalized;
            Assert.That(
                Vector3.Angle(authoredPortalHue, portalHue),
                Is.LessThan(0.1f),
                $"{portalMaterial.name} must stay in the authored portal's single hue family.");
        }

        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                previewScene) as GameObject;
            BonusChoiceAltarVfx vfx = instance.GetComponent<BonusChoiceAltarVfx>();
            Transform instanceGlow = instance.transform.Find("ChoiceAltarVisual/GlowOrbit");
            Transform inner = instanceGlow.Find("WaterVortexInner");
            Transform compass = instanceGlow.Find("WarpCompass");
            Transform foam = instanceGlow.Find("WaterFoam");
            Quaternion glowBaseRotation = instanceGlow.localRotation;
            Quaternion innerBaseRotation = inner.localRotation;
            Quaternion compassBaseRotation = compass.localRotation;
            Quaternion foamBaseRotation = foam.localRotation;
            typeof(BonusChoiceAltarVfx)
                .GetMethod("CacheBaselines", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(vfx, null);
            inner.localRotation *= Quaternion.Euler(0f, 48f, 0f);
            compass.localRotation *= Quaternion.Euler(0f, 23f, 0f);
            foam.localRotation *= Quaternion.Euler(0f, -31f, 0f);
            typeof(BonusChoiceAltarVfx)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(vfx, null);
            Assert.That(
                Quaternion.Angle(inner.localRotation, innerBaseRotation),
                Is.LessThan(0.001f));
            Assert.That(
                Quaternion.Angle(compass.localRotation, compassBaseRotation),
                Is.LessThan(0.001f));
            Assert.That(
                Quaternion.Angle(foam.localRotation, foamBaseRotation),
                Is.LessThan(0.001f));

            typeof(BonusChoiceAltarVfx)
                .GetMethod("RotateWarpLayers", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(vfx, new object[] { 1f, 1f });
            Assert.That(
                Mathf.DeltaAngle(glowBaseRotation.eulerAngles.y, instanceGlow.localEulerAngles.y),
                Is.GreaterThan(10f));
            Assert.That(
                Mathf.DeltaAngle(innerBaseRotation.eulerAngles.y, inner.localEulerAngles.y),
                Is.LessThan(-10f));
            Assert.That(
                Mathf.DeltaAngle(compassBaseRotation.eulerAngles.y, compass.localEulerAngles.y),
                Is.LessThan(-10f));
            Assert.That(
                Mathf.DeltaAngle(foamBaseRotation.eulerAngles.y, foam.localEulerAngles.y),
                Is.GreaterThan(10f));
            Object.DestroyImmediate(instance);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }

        Texture2D readableTexture = new Texture2D(
            2,
            2,
            TextureFormat.RGBA32,
            false,
            true);
        try
        {
            bool loaded = ImageConversion.LoadImage(
                readableTexture,
                File.ReadAllBytes(absolutePath),
                false);
            Assert.That(loaded, Is.True, texturePath);
            Assert.That(readableTexture.width, Is.GreaterThanOrEqualTo(128));
            Assert.That(readableTexture.height, Is.EqualTo(readableTexture.width));

            Assert.That(
                readableTexture.GetPixel(0, 0).a,
                Is.LessThan(0.1f),
                "The transparent vortex texture must not fill its corners.");
            Assert.That(
                readableTexture.GetPixel(
                    readableTexture.width / 2,
                    readableTexture.height / 2).a,
                Is.LessThan(0.2f),
                "The middle of the water vortex must remain visibly hollow.");
            float outerVortexMaxAlpha = 0f;
            for (int index = 0; index < 72; index++)
            {
                float angle = index * Mathf.PI * 2f / 72f;
                outerVortexMaxAlpha = Mathf.Max(
                    outerVortexMaxAlpha,
                    readableTexture.GetPixelBilinear(
                        0.5f + Mathf.Cos(angle) * 0.44f,
                        0.5f + Mathf.Sin(angle) * 0.44f).a);
            }
            Assert.That(
                outerVortexMaxAlpha,
                Is.GreaterThan(0.5f),
                "At least one bright spiral crest must reach the outer water ring.");

            Color32[] pixels = readableTexture.GetPixels32();
            int opaquePixelCount = 0;
            foreach (Color32 pixel in pixels)
            {
                if (pixel.a >= 26)
                    opaquePixelCount++;
            }

            float opaqueRatio = (float)opaquePixelCount / pixels.Length;
            Assert.That(
                opaqueRatio,
                Is.LessThan(0.48f),
                $"Water-vortex alpha must stay broken and translucent, but {opaqueRatio:P1} of pixels were opaque.");
        }
        finally
        {
            Object.DestroyImmediate(readableTexture);
        }
    }

    [Test]
    public void BonusChoiceAltarVfx_ReenableRestoresAuthoredScaleAndIconPosition()
    {
        GameObject root = new("BonusChoiceAltarVfxTest");
        GameObject glowObject = new("GlowOrbit");
        GameObject iconObject = new("StatIcon", typeof(RectTransform));
        GameObject innerAuraObject = new("InnerAura", typeof(RectTransform));
        GameObject outerAuraObject = new("OuterAura", typeof(RectTransform));

        try
        {
            glowObject.transform.SetParent(root.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(root.transform, false);
            RectTransform innerAuraRect = innerAuraObject.GetComponent<RectTransform>();
            RectTransform outerAuraRect = outerAuraObject.GetComponent<RectTransform>();
            innerAuraRect.SetParent(root.transform, false);
            outerAuraRect.SetParent(root.transform, false);

            Vector3 authoredGlowScale = new(1.2f, 0.8f, 1.4f);
            Vector2 authoredIconPosition = new(12f, 34f);
            Quaternion authoredIconRotation = Quaternion.Euler(0f, 0f, -4f);
            Vector2 authoredInnerAuraPosition = new(-3f, 7f);
            Vector2 authoredOuterAuraPosition = new(5f, 9f);
            Vector3 authoredInnerAuraScale = new(1.1f, 1.2f, 1f);
            Vector3 authoredOuterAuraScale = new(1.4f, 1.5f, 1f);
            Quaternion authoredInnerAuraRotation = Quaternion.Euler(0f, 0f, -2f);
            Quaternion authoredOuterAuraRotation = Quaternion.Euler(0f, 0f, 3f);
            glowObject.transform.localScale = authoredGlowScale;
            iconRect.anchoredPosition = authoredIconPosition;
            iconRect.localRotation = authoredIconRotation;
            innerAuraRect.anchoredPosition = authoredInnerAuraPosition;
            outerAuraRect.anchoredPosition = authoredOuterAuraPosition;
            innerAuraRect.localScale = authoredInnerAuraScale;
            outerAuraRect.localScale = authoredOuterAuraScale;
            innerAuraRect.localRotation = authoredInnerAuraRotation;
            outerAuraRect.localRotation = authoredOuterAuraRotation;

            BonusChoiceAltarVfx vfx = root.AddComponent<BonusChoiceAltarVfx>();
            vfx.Configure(glowObject.transform, iconRect, innerAuraRect, outerAuraRect);

            glowObject.transform.localScale = authoredGlowScale * 1.08f;
            iconRect.anchoredPosition = authoredIconPosition + Vector2.up * 4f;
            iconRect.localRotation = Quaternion.Euler(0f, 0f, 8f);
            innerAuraRect.anchoredPosition += Vector2.up * 2f;
            outerAuraRect.anchoredPosition += Vector2.down * 3f;
            innerAuraRect.localScale *= 1.2f;
            outerAuraRect.localScale *= 0.8f;
            innerAuraRect.localRotation = Quaternion.Euler(0f, 0f, 12f);
            outerAuraRect.localRotation = Quaternion.Euler(0f, 0f, -9f);
            typeof(BonusChoiceAltarVfx)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(vfx, null);

            Assert.That(glowObject.transform.localScale, Is.EqualTo(authoredGlowScale));
            Assert.That(iconRect.anchoredPosition, Is.EqualTo(authoredIconPosition));
            Assert.That(iconRect.localRotation, Is.EqualTo(authoredIconRotation));
            Assert.That(innerAuraRect.anchoredPosition, Is.EqualTo(authoredInnerAuraPosition));
            Assert.That(outerAuraRect.anchoredPosition, Is.EqualTo(authoredOuterAuraPosition));
            Assert.That(innerAuraRect.localScale, Is.EqualTo(authoredInnerAuraScale));
            Assert.That(outerAuraRect.localScale, Is.EqualTo(authoredOuterAuraScale));
            Assert.That(innerAuraRect.localRotation, Is.EqualTo(authoredInnerAuraRotation));
            Assert.That(outerAuraRect.localRotation, Is.EqualTo(authoredOuterAuraRotation));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BonusChoiceAltarVfx_UsesRolledStatFamilyForNormalTheme()
    {
        string prefabPath = NoryangjinMapToolWindow.BonusWallPrefabRoot +
            "/Box_left.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                previewScene) as GameObject;
            Assert.That(instance, Is.Not.Null);

            BonusChoiceAltarVfx vfx = instance.GetComponent<BonusChoiceAltarVfx>();
            Renderer glowRenderer = instance.transform
                .Find("ChoiceAltarVisual/GlowOrbit")
                .GetComponentInChildren<Renderer>(true);
            WallScript wall = instance.GetComponentInChildren<WallScript>(true);
            UnityEngine.UI.Outline badgeOutline = wall?.statBadgeOutline;
            MethodInfo refreshPresentation = typeof(BonusChoiceAltarVfx).GetMethod(
                "RefreshPresentation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo updateStatUi = typeof(WallScript).GetMethod(
                "UpdateStatUI",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo effectRenderersField = typeof(BonusChoiceAltarVfx).GetField(
                "effectRenderers",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(vfx, Is.Not.Null);
            Assert.That(wall, Is.Not.Null);
            Assert.That(glowRenderer, Is.Not.Null);
            Assert.That(badgeOutline, Is.Not.Null);
            Assert.That(refreshPresentation, Is.Not.Null);
            Assert.That(updateStatUi, Is.Not.Null);
            Assert.That(effectRenderersField, Is.Not.Null);

            vfx.SetRarity(Rarity.Normal);
            vfx.SetBonusType(BuffType.hp_normal);
            refreshPresentation.Invoke(vfx, null);
            updateStatUi.Invoke(wall, new object[] { BuffType.hp_normal, 999f });

            Renderer[] themedRenderers = effectRenderersField.GetValue(vfx) as Renderer[];
            Assert.That(themedRenderers, Is.Not.Null.And.Not.Empty);

            var block = new MaterialPropertyBlock();
            glowRenderer.GetPropertyBlock(block);
            Color vitalityWorld = block.GetColor("_BaseColor");
            Assert.That(vitalityWorld.g, Is.GreaterThan(vitalityWorld.r));
            Assert.That(vitalityWorld.g, Is.GreaterThan(vitalityWorld.b));
            Assert.That(badgeOutline.effectColor.g, Is.GreaterThan(badgeOutline.effectColor.r));
            Assert.That(badgeOutline.effectColor.g, Is.GreaterThan(badgeOutline.effectColor.b));
            foreach (Renderer themedRenderer in themedRenderers)
            {
                block.Clear();
                themedRenderer.GetPropertyBlock(block);
                Color rendererColor = block.GetColor("_BaseColor");
                Assert.That(rendererColor.r, Is.EqualTo(vitalityWorld.r).Within(0.0001f));
                Assert.That(rendererColor.g, Is.EqualTo(vitalityWorld.g).Within(0.0001f));
                Assert.That(rendererColor.b, Is.EqualTo(vitalityWorld.b).Within(0.0001f));
            }

            vfx.SetBonusType(BuffType.att_normmal);
            refreshPresentation.Invoke(vfx, null);
            updateStatUi.Invoke(wall, new object[] { BuffType.att_normmal, 11f });
            block.Clear();
            glowRenderer.GetPropertyBlock(block);
            Color attackWorld = block.GetColor("_BaseColor");
            Assert.That(block.isEmpty, Is.False);
            Assert.That(attackWorld.r, Is.GreaterThan(attackWorld.g));
            Assert.That(attackWorld.g, Is.GreaterThan(attackWorld.b));
            Assert.That(badgeOutline.effectColor.r, Is.GreaterThan(badgeOutline.effectColor.g));
            Assert.That(badgeOutline.effectColor.r, Is.GreaterThan(badgeOutline.effectColor.b));

            foreach (Renderer themedRenderer in themedRenderers)
            {
                block.Clear();
                themedRenderer.GetPropertyBlock(block);
                Color rendererColor = block.GetColor("_BaseColor");
                Assert.That(rendererColor.r, Is.EqualTo(attackWorld.r).Within(0.0001f));
                Assert.That(rendererColor.g, Is.EqualTo(attackWorld.g).Within(0.0001f));
                Assert.That(rendererColor.b, Is.EqualTo(attackWorld.b).Within(0.0001f));
            }

            vfx.SetBonusType(BuffType.missileDistance_normal);
            refreshPresentation.Invoke(vfx, null);
            block.Clear();
            glowRenderer.GetPropertyBlock(block);
            Color utilityWorld = block.GetColor("_BaseColor");
            Assert.That(utilityWorld.b, Is.GreaterThan(utilityWorld.g));
            Assert.That(utilityWorld.g, Is.GreaterThan(utilityWorld.r));
            foreach (Renderer themedRenderer in themedRenderers)
            {
                block.Clear();
                themedRenderer.GetPropertyBlock(block);
                Color rendererColor = block.GetColor("_BaseColor");
                Assert.That(rendererColor.r, Is.EqualTo(utilityWorld.r).Within(0.0001f));
                Assert.That(rendererColor.g, Is.EqualTo(utilityWorld.g).Within(0.0001f));
                Assert.That(rendererColor.b, Is.EqualTo(utilityWorld.b).Within(0.0001f));
            }

            Object.DestroyImmediate(instance);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void BonusChoiceAltarVfx_UniqueGradeEnlargesAndEnrichesExistingEffects()
    {
        GameObject root = new("UniqueBonusChoiceAltarVfxTest");
        GameObject visual = new("ChoiceAltarVisual");
        GameObject glow = new("GlowOrbit");
        GameObject energy = new("IconEnergyBillboard");
        GameObject groundAura = new("GroundAura");
        GameObject frontSigil = new("FrontSigil");
        GameObject particleObject = new("ChoiceParticles", typeof(ParticleSystem));
        GameObject iconObject = new("StatIcon", typeof(RectTransform));
        GameObject auraObject = new("IconAura", typeof(RectTransform));
        Material effectMaterial = null;

        try
        {
            visual.transform.SetParent(root.transform, false);
            glow.transform.SetParent(visual.transform, false);
            energy.transform.SetParent(visual.transform, false);
            groundAura.transform.SetParent(visual.transform, false);
            frontSigil.transform.SetParent(visual.transform, false);
            particleObject.transform.SetParent(visual.transform, false);
            RectTransform icon = iconObject.GetComponent<RectTransform>();
            RectTransform aura = auraObject.GetComponent<RectTransform>();
            RawImage auraGraphic = auraObject.AddComponent<RawImage>();
            icon.SetParent(root.transform, false);
            aura.SetParent(root.transform, false);

            Shader effectShader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(effectShader, Is.Not.Null);
            effectMaterial = new Material(effectShader);
            Color baseEffectColor = new(2.3f, 0.75f, 0.04f, 0.44f);
            if (effectMaterial.HasProperty("_BaseColor"))
                effectMaterial.SetColor("_BaseColor", baseEffectColor);
            if (effectMaterial.HasProperty("_Color"))
                effectMaterial.SetColor("_Color", baseEffectColor);
            Color sharedBaseColor = effectMaterial.GetColor("_BaseColor");
            Color sharedLegacyColor = effectMaterial.GetColor("_Color");

            MeshRenderer glowRenderer = glow.AddComponent<MeshRenderer>();
            MeshRenderer energyRenderer = energy.AddComponent<MeshRenderer>();
            MeshRenderer groundRenderer = groundAura.AddComponent<MeshRenderer>();
            MeshRenderer sigilRenderer = frontSigil.AddComponent<MeshRenderer>();
            ParticleSystemRenderer particleRenderer =
                particleObject.GetComponent<ParticleSystemRenderer>();
            glowRenderer.sharedMaterial = effectMaterial;
            energyRenderer.sharedMaterial = effectMaterial;
            groundRenderer.sharedMaterial = effectMaterial;
            sigilRenderer.sharedMaterial = effectMaterial;
            particleRenderer.sharedMaterial = effectMaterial;
            Renderer[] effectRenderers =
            {
                glowRenderer,
                energyRenderer,
                groundRenderer,
                sigilRenderer,
                particleRenderer
            };
            Color glowOverrideColor = new(1.8f, 0.55f, 0.04f, 0.17f);
            var glowBaseBlock = new MaterialPropertyBlock();
            glowBaseBlock.SetColor("_BaseColor", glowOverrideColor);
            glowRenderer.SetPropertyBlock(glowBaseBlock);
            bool[] baseBlockEmpty = new bool[effectRenderers.Length];
            Color[] baseBlockColors = new Color[effectRenderers.Length];
            Color[] baseLegacyColors = new Color[effectRenderers.Length];
            var baseBlockProbe = new MaterialPropertyBlock();
            for (int index = 0; index < effectRenderers.Length; index++)
            {
                baseBlockProbe.Clear();
                effectRenderers[index].GetPropertyBlock(baseBlockProbe);
                baseBlockEmpty[index] = baseBlockProbe.isEmpty;
                baseBlockColors[index] = baseBlockProbe.GetColor("_BaseColor");
                baseLegacyColors[index] = baseBlockProbe.GetColor("_Color");
            }

            Vector3 glowScale = new(1.2f, 0.9f, 1.1f);
            Vector3 energyScale = new(0.8f, 1.3f, 1f);
            Vector3 groundScale = new(1.4f, 1f, 1.2f);
            Vector3 auraScale = new(1.1f, 1.25f, 1f);
            Color baseAuraColor = new(1f, 0.35f, 0.08f, 0.25f);
            glow.transform.localScale = glowScale;
            energy.transform.localScale = energyScale;
            groundAura.transform.localScale = groundScale;
            aura.localScale = auraScale;
            auraGraphic.color = baseAuraColor;

            ParticleSystem particles = particleObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.startSize3D = true;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.09f, 0.13f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.15f, 0.22f);
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.09f, 0.13f);
            main.maxParticles = 12;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 7f;

            BonusChoiceAltarVfx vfx = root.AddComponent<BonusChoiceAltarVfx>();
            vfx.Configure(false, glow.transform, icon, aura);
            vfx.SetRarity(Rarity.Unique);

            Assert.That(vfx.Rarity, Is.EqualTo(Rarity.Unique));
            Assert.That(glow.transform.localScale, Is.EqualTo(glowScale));
            Assert.That(energy.transform.localScale, Is.EqualTo(energyScale));
            Assert.That(groundAura.transform.localScale, Is.EqualTo(groundScale));
            Assert.That(aura.localScale, Is.EqualTo(auraScale));
            Assert.That(auraGraphic.color, Is.EqualTo(baseAuraColor));
            Assert.That(particles.emission.rateOverTime.constant, Is.EqualTo(7f));
            Assert.That(particles.main.startSizeY.constantMax, Is.EqualTo(0.22f));
            Assert.That(particles.main.maxParticles, Is.EqualTo(12));
            var editModeBlock = new MaterialPropertyBlock();
            for (int index = 0; index < effectRenderers.Length; index++)
            {
                editModeBlock.Clear();
                effectRenderers[index].GetPropertyBlock(editModeBlock);
                Assert.That(editModeBlock.isEmpty, Is.EqualTo(baseBlockEmpty[index]));
                AssertColorApproximately(
                    editModeBlock.GetColor("_BaseColor"),
                    baseBlockColors[index]);
                AssertColorApproximately(
                    editModeBlock.GetColor("_Color"),
                    baseLegacyColors[index]);
            }
            AssertColorApproximately(
                effectMaterial.GetColor("_BaseColor"),
                sharedBaseColor);
            AssertColorApproximately(
                effectMaterial.GetColor("_Color"),
                sharedLegacyColor);

            MethodInfo refreshPresentation = typeof(BonusChoiceAltarVfx).GetMethod(
                "RefreshPresentation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(refreshPresentation, Is.Not.Null);
            refreshPresentation.Invoke(vfx, null);

            Assert.That(glow.transform.localScale.x, Is.GreaterThan(glowScale.x));
            Assert.That(energy.transform.localScale.y, Is.GreaterThan(energyScale.y));
            Assert.That(groundAura.transform.localScale.x, Is.GreaterThan(groundScale.x));
            Assert.That(aura.localScale.x, Is.GreaterThan(auraScale.x));
            Assert.That(particles.emission.rateOverTime.constant, Is.GreaterThan(7f));
            Assert.That(particles.main.startSizeY.constantMax, Is.GreaterThan(0.22f));
            Assert.That(particles.main.maxParticles, Is.GreaterThan(12));
            var propertyBlock = new MaterialPropertyBlock();
            Color uniqueEffectColor = default;
            foreach (Renderer effectRenderer in effectRenderers)
            {
                propertyBlock.Clear();
                effectRenderer.GetPropertyBlock(propertyBlock);
                Color rendererColor = propertyBlock.GetColor("_BaseColor");
                Assert.That(rendererColor.b, Is.GreaterThan(rendererColor.r));
                Assert.That(rendererColor.r, Is.GreaterThan(rendererColor.g));
                Assert.That(
                    rendererColor.a,
                    Is.EqualTo(effectRenderer == glowRenderer
                        ? glowOverrideColor.a
                        : baseEffectColor.a));
                if (effectRenderer == glowRenderer)
                    uniqueEffectColor = rendererColor;
            }
            Assert.That(auraGraphic.color.b, Is.GreaterThan(auraGraphic.color.r));
            Assert.That(auraGraphic.color.r, Is.GreaterThan(auraGraphic.color.g));
            Assert.That(auraGraphic.color.a, Is.EqualTo(baseAuraColor.a));
            AssertColorApproximately(
                effectMaterial.GetColor("_BaseColor"),
                sharedBaseColor);
            AssertColorApproximately(
                effectMaterial.GetColor("_Color"),
                sharedLegacyColor);

            Vector3 uniqueGlowScale = glow.transform.localScale;
            Vector3 uniqueEnergyScale = energy.transform.localScale;
            Vector3 uniqueGroundScale = groundAura.transform.localScale;
            Vector3 uniqueAuraScale = aura.localScale;
            float uniqueParticleRate = particles.emission.rateOverTime.constant;
            float uniqueParticleSize = particles.main.startSizeY.constantMax;
            int uniqueParticleCapacity = particles.main.maxParticles;
            Color uniqueAuraColor = auraGraphic.color;
            refreshPresentation.Invoke(vfx, null);

            Assert.That(glow.transform.localScale, Is.EqualTo(uniqueGlowScale));
            Assert.That(energy.transform.localScale, Is.EqualTo(uniqueEnergyScale));
            Assert.That(groundAura.transform.localScale, Is.EqualTo(uniqueGroundScale));
            Assert.That(aura.localScale, Is.EqualTo(uniqueAuraScale));
            Assert.That(
                particles.emission.rateOverTime.constant,
                Is.EqualTo(uniqueParticleRate));
            Assert.That(
                particles.main.startSizeY.constantMax,
                Is.EqualTo(uniqueParticleSize));
            Assert.That(particles.main.maxParticles, Is.EqualTo(uniqueParticleCapacity));
            propertyBlock.Clear();
            glowRenderer.GetPropertyBlock(propertyBlock);
            Assert.That(
                propertyBlock.GetColor("_BaseColor"),
                Is.EqualTo(uniqueEffectColor));
            Assert.That(auraGraphic.color, Is.EqualTo(uniqueAuraColor));

            vfx.SetRarity(Rarity.Rare);
            refreshPresentation.Invoke(vfx, null);

            Assert.That(vfx.Rarity, Is.EqualTo(Rarity.Rare));
            Assert.That(glow.transform.localScale, Is.EqualTo(glowScale));
            Assert.That(energy.transform.localScale, Is.EqualTo(energyScale));
            Assert.That(groundAura.transform.localScale, Is.EqualTo(groundScale));
            Assert.That(aura.localScale, Is.EqualTo(auraScale));
            Assert.That(particles.emission.rateOverTime.constant, Is.EqualTo(7f));
            Assert.That(particles.main.startSizeY.constantMax, Is.EqualTo(0.22f));
            Assert.That(particles.main.maxParticles, Is.EqualTo(12));
            Color normalEffectColor = default;
            for (int index = 0; index < effectRenderers.Length; index++)
            {
                Renderer effectRenderer = effectRenderers[index];
                propertyBlock.Clear();
                effectRenderer.GetPropertyBlock(propertyBlock);
                Assert.That(propertyBlock.isEmpty, Is.False);
                Color rendererColor = propertyBlock.GetColor("_BaseColor");
                Assert.That(rendererColor.r, Is.GreaterThan(rendererColor.g));
                Assert.That(rendererColor.g, Is.GreaterThan(rendererColor.b));
                Assert.That(
                    rendererColor.a,
                    Is.EqualTo(index == 0 ? glowOverrideColor.a : baseEffectColor.a));
                if (index == 0)
                {
                    normalEffectColor = rendererColor;
                }
                else
                {
                    Assert.That(rendererColor.r, Is.EqualTo(normalEffectColor.r));
                    Assert.That(rendererColor.g, Is.EqualTo(normalEffectColor.g));
                    Assert.That(rendererColor.b, Is.EqualTo(normalEffectColor.b));
                }
            }
            Color normalAuraColor = BonusChoiceAltarVfx.ResolveUiAccent(BuffType.att_normmal);
            normalAuraColor.a = baseAuraColor.a;
            Assert.That(auraGraphic.color, Is.EqualTo(normalAuraColor));
            AssertColorApproximately(
                effectMaterial.GetColor("_BaseColor"),
                sharedBaseColor);
            AssertColorApproximately(
                effectMaterial.GetColor("_Color"),
                sharedLegacyColor);
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (effectMaterial != null)
                Object.DestroyImmediate(effectMaterial);
        }

        static void AssertColorApproximately(Color actual, Color expected)
        {
            float difference = Mathf.Max(
                Mathf.Abs(actual.r - expected.r),
                Mathf.Abs(actual.g - expected.g),
                Mathf.Abs(actual.b - expected.b),
                Mathf.Abs(actual.a - expected.a));
            Assert.That(difference, Is.LessThanOrEqualTo(0.0001f));
        }
    }

    [Test]
    public void FeastOfFortuneStatCanvas_FacesTheGameplayCamera()
    {
        Assert.That(
            typeof(WallStatCanvasBillboard).IsDefined(typeof(ExecuteAlways), false),
            Is.True,
            "The Noryangjin map-tool preview must face its camera outside Play mode.");

        string[] prefabPaths =
        {
            NoryangjinMapToolWindow.BonusWallPrefabRoot +
            "/Box_left.prefab"
        };
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject cameraObject = new GameObject("Gameplay Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.rotation = Quaternion.Euler(24f, 37f, 3f);
            Camera previouslyEnabledMainCamera = Camera.main;
            if (previouslyEnabledMainCamera != null && previouslyEnabledMainCamera != camera)
                previouslyEnabledMainCamera.enabled = false;

            try
            {
                foreach (string prefabPath in prefabPaths)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    GameObject instance =
                        PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
                    Assert.That(instance, Is.Not.Null, prefabPath);

                    WallStatCanvasBillboard[] billboards =
                        instance.GetComponentsInChildren<WallStatCanvasBillboard>(true);
                    Assert.That(
                        billboards.Length,
                        Is.EqualTo(2),
                        "The stat Canvas and the grouped energy plume must face the camera.");

                    Transform energyBillboard = instance.transform.Find(
                        "ChoiceAltarVisual/IconEnergyBillboard");
                    Assert.That(energyBillboard, Is.Not.Null, prefabPath);
                    Transform leftVeil = energyBillboard.Find("IconEnergyVeilLeft");
                    Transform rightVeil = energyBillboard.Find("IconEnergyVeilRight");
                    Quaternion leftTiltBeforeFacing = leftVeil.localRotation;
                    Quaternion rightTiltBeforeFacing = rightVeil.localRotation;

                    MethodInfo onValidate = typeof(WallStatCanvasBillboard).GetMethod(
                        "OnValidate",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo delayedFacing = typeof(WallStatCanvasBillboard).GetMethod(
                        "FaceMainCameraIfAvailable",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.That(onValidate, Is.Not.Null, prefabPath);
                    Assert.That(delayedFacing, Is.Not.Null, prefabPath);
                    foreach (WallStatCanvasBillboard billboard in billboards)
                    {
                        onValidate.Invoke(billboard, null);
                        delayedFacing.Invoke(billboard, null);
                        billboard.FaceCamera(camera);

                        Assert.That(
                            Vector3.Dot(billboard.transform.right, camera.transform.right),
                            Is.GreaterThan(0.999f),
                            $"{prefabPath}: {billboard.name}");
                        Assert.That(
                            Vector3.Dot(billboard.transform.up, camera.transform.up),
                            Is.GreaterThan(0.999f),
                            $"{prefabPath}: {billboard.name}");
                        Assert.That(
                            Vector3.Dot(billboard.transform.forward, camera.transform.forward),
                            Is.GreaterThan(0.999f),
                            $"{prefabPath}: {billboard.name}");
                    }

                    Assert.That(
                        Quaternion.Angle(leftVeil.localRotation, leftTiltBeforeFacing),
                        Is.LessThan(0.001f),
                        "Camera-facing must not erase the authored left plume tilt.");
                    Assert.That(
                        Quaternion.Angle(rightVeil.localRotation, rightTiltBeforeFacing),
                        Is.LessThan(0.001f),
                        "Camera-facing must not erase the authored right plume tilt.");
                    Assert.That(
                        Mathf.DeltaAngle(0f, leftVeil.localEulerAngles.z),
                        Is.LessThan(-5f));
                    Assert.That(
                        Mathf.DeltaAngle(0f, rightVeil.localEulerAngles.z),
                        Is.GreaterThan(5f));

                    Object.DestroyImmediate(instance);
                }
            }
            finally
            {
                if (previouslyEnabledMainCamera != null)
                    previouslyEnabledMainCamera.enabled = true;
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void FeastOfFortuneDataFirstLayout_FitsMaximumEnglishLabelAndValue()
    {
        string[] prefabPaths =
        {
            NoryangjinMapToolWindow.BonusWallPrefabRoot +
            "/Box_left.prefab"
        };
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            for (int index = 0; index < prefabPaths.Length; index++)
            {
                string prefabPath = prefabPaths[index];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                GameObject instance =
                    PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
                Assert.That(instance, Is.Not.Null, prefabPath);

                RectTransform canvas =
                    instance.transform.Find("GFX/Canvas") as RectTransform;
                RectTransform badge =
                    instance.transform.Find("GFX/Canvas/Stat_Badge") as RectTransform;
                RectTransform icon =
                    instance.transform.Find("GFX/Canvas/Stat_Badge/Stat_Icon") as RectTransform;
                WallScript wall = instance.GetComponentInChildren<WallScript>(true);
                TMPro.TextMeshProUGUI statName =
                    wall?.statNameLoc?.GetComponent<TMPro.TextMeshProUGUI>();
                TMPro.TextMeshProUGUI statValue = wall?.statValueTmp;
                Assert.That(canvas, Is.Not.Null, prefabPath);
                Assert.That(badge, Is.Not.Null, prefabPath);
                Assert.That(icon, Is.Not.Null, prefabPath);
                Assert.That(statName, Is.Not.Null, prefabPath);
                Assert.That(statValue, Is.Not.Null, prefabPath);
                Assert.That(
                    instance.transform.Find("GFX/Canvas/Stat_Row"),
                    Is.Null,
                    prefabPath);

                const string expectedLabel = "ATK SPEED";
                statName.GetComponent<UnityEngine.Localization.Components.LocalizeStringEvent>()
                    .enabled = false;
                statName.text = expectedLabel;
                statValue.text = "+999";
                LayoutRebuilder.ForceRebuildLayoutImmediate(badge);
                Canvas.ForceUpdateCanvases();
                statName.ForceMeshUpdate(true, true);
                statValue.ForceMeshUpdate(true, true);

                Rect iconBounds = GetRectBoundsInSpace(icon, canvas);
                Rect badgeBounds = GetRectBoundsInSpace(badge, canvas);
                Vector2 nameHorizontalBounds = GetVisibleTextHorizontalBoundsInSpace(
                    statName,
                    canvas);
                Vector2 nameVerticalBounds = GetVisibleTextVerticalBoundsInSpace(
                    statName,
                    canvas);
                Vector2 valueHorizontalBounds = GetVisibleTextHorizontalBoundsInSpace(
                    statValue,
                    canvas);
                Vector2 valueVerticalBounds = GetVisibleTextVerticalBoundsInSpace(
                    statValue,
                    canvas);
                Assert.That(
                    statName.textInfo.characterCount,
                    Is.EqualTo(expectedLabel.Length),
                    prefabPath);
                Assert.That(
                    nameHorizontalBounds.x,
                    Is.GreaterThanOrEqualTo(badgeBounds.xMin),
                    $"English label must remain inside its badge: {prefabPath}");
                Assert.That(
                    nameHorizontalBounds.y,
                    Is.LessThanOrEqualTo(badgeBounds.xMax),
                    $"English label must remain inside its badge: {prefabPath}");
                Assert.That(
                    nameVerticalBounds.x,
                    Is.GreaterThanOrEqualTo(badgeBounds.yMin),
                    prefabPath);
                Assert.That(
                    nameVerticalBounds.y,
                    Is.LessThanOrEqualTo(badgeBounds.yMax),
                    prefabPath);
                Assert.That(
                    iconBounds.xMax,
                    Is.LessThan(nameHorizontalBounds.x),
                    $"Badge icon and ATK SPEED label must not overlap: {prefabPath}");
                Assert.That(
                    valueVerticalBounds.x,
                    Is.GreaterThan(badgeBounds.yMax),
                    $"Large value must sit above the compact badge: {prefabPath}");
                Assert.That(
                    valueHorizontalBounds.x,
                    Is.GreaterThanOrEqualTo(canvas.rect.xMin),
                    prefabPath);
                Assert.That(
                    valueHorizontalBounds.y,
                    Is.LessThanOrEqualTo(canvas.rect.xMax),
                    prefabPath);
                Assert.That(statName.enableAutoSizing, Is.True, prefabPath);
                Assert.That(statValue.enableAutoSizing, Is.True, prefabPath);

                Object.DestroyImmediate(instance);
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void WallBonusIcons_CoverEveryBuffTypeWithAnImportedSprite()
    {
        Dictionary<BuffType, string> expectedResourceNames = new()
        {
            { BuffType.HealthBoost, "WallBonus_Health" },
            { BuffType.FireRateIncrease, "WallBonus_AttackSpeed" },
            { BuffType.ExtraHelp, "WallBonus_Tungtung" },
            { BuffType.att_normmal, "WallBonus_Attack" },
            { BuffType.attPer_normal, "WallBonus_Attack" },
            { BuffType.attackSpeed_normal, "WallBonus_AttackSpeed" },
            { BuffType.missileDistance_normal, "WallBonus_MissileDuration" },
            { BuffType.hp_normal, "WallBonus_Health" },
            { BuffType.hpPer_normal, "WallBonus_Health" },
            { BuffType.tungtung_rare, "WallBonus_Tungtung" },
            { BuffType.boombar_rare, "WallBonus_Boombar" },
            { BuffType.att_unique, "WallBonus_Attack" },
            { BuffType.attPer_unique, "WallBonus_Attack" },
            { BuffType.missileAdd_unique, "WallBonus_MissileAdd" },
            { BuffType.attackSpeed_unique, "WallBonus_AttackSpeed" },
            { BuffType.missileDistance_unique, "WallBonus_MissileDuration" },
            { BuffType.hp_unique, "WallBonus_Health" },
            { BuffType.hpPer_unique, "WallBonus_Health" }
        };
        MethodInfo loadEditorIcon = typeof(FeastOfFortuneWallSetup).GetMethod(
            "LoadBonusIcon",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(loadEditorIcon, Is.Not.Null);
        Assert.That(
            expectedResourceNames.Count,
            Is.EqualTo(System.Enum.GetValues(typeof(BuffType)).Length));

        foreach (KeyValuePair<BuffType, string> expected in expectedResourceNames)
        {
            string actual = BonusAltarRules.ResolveIconResourceName(expected.Key);
            Assert.That(actual, Is.EqualTo(expected.Value), expected.Key.ToString());
            Assert.That(actual, Does.Not.Contain("Percent"), expected.Key.ToString());
            Assert.That(
                Resources.Load<Sprite>("WallBonusIcons/" + actual),
                Is.Not.Null,
                expected.Key.ToString());
            Sprite editorIcon = (Sprite)loadEditorIcon.Invoke(
                null,
                new object[] { expected.Key });
            Assert.That(editorIcon.name, Is.EqualTo(expected.Value), expected.Key.ToString());
        }
    }

    [Test]
    public void BonusAltarGrade_CanChangePerInstanceAndSyncsRandomWall()
    {
        string prefabPath =
            NoryangjinMapToolWindow.FeastOfFortuneBonusWallPrefabPaths[0];
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            Assert.That(instance, Is.Not.Null);

            AuthoredBonusWall authoredBonus = instance.GetComponent<AuthoredBonusWall>();
            BonusChoiceAltarVfx altarVfx = instance.GetComponent<BonusChoiceAltarVfx>();
            Transform visual = instance.transform.Find("ChoiceAltarVisual");
            Transform glow = visual.Find("GlowOrbit");
            Transform energy = visual.Find("IconEnergyBillboard");
            Transform groundAura = visual.Find("GroundAura");
            ParticleSystem particles = visual.Find("ChoiceParticles")
                .GetComponent<ParticleSystem>();
            Vector3 glowScale = glow.localScale;
            Vector3 energyScale = energy.localScale;
            Vector3 groundAuraScale = groundAura.localScale;
            ParticleSystem.MinMaxCurve particleRate = particles.emission.rateOverTime;
            ParticleSystem.MinMaxCurve particleSize = particles.main.startSizeY;
            int particleCapacity = particles.main.maxParticles;

            Assert.That(
                NoryangjinMapToolWindow.ResolveFeastOfFortuneWall(instance),
                Is.Not.Null);
            Assert.That(
                NoryangjinMapToolWindow.ApplyFeastOfFortuneRarity(
                    instance,
                    Rarity.Unique,
                    recordUndo: false),
                Is.True);
            Assert.That(
                instance.GetComponentInChildren<WallScript>(true).rarity,
                Is.EqualTo(Rarity.Unique));
            Assert.That(authoredBonus.Rarity, Is.EqualTo(Rarity.Unique));
            Assert.That(altarVfx.Rarity, Is.EqualTo(Rarity.Unique));
            Assert.That(instance.GetComponentInChildren<WallScript>(true).isRandom, Is.True);
            Assert.That(glow.localScale, Is.EqualTo(glowScale));
            Assert.That(energy.localScale, Is.EqualTo(energyScale));
            Assert.That(groundAura.localScale, Is.EqualTo(groundAuraScale));
            Assert.That(
                particles.emission.rateOverTime.constant,
                Is.EqualTo(particleRate.constant));
            Assert.That(
                particles.main.startSizeY.constantMin,
                Is.EqualTo(particleSize.constantMin));
            Assert.That(
                particles.main.startSizeY.constantMax,
                Is.EqualTo(particleSize.constantMax));
            Assert.That(particles.main.maxParticles, Is.EqualTo(particleCapacity));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void FeastOfFortuneRarity_RejectsOtherBonusWalls()
    {
        string prefabPath =
            NoryangjinMapToolWindow.BonusWallPrefabRoot +
            "/wall_atk_normal.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            Assert.That(instance, Is.Not.Null);

            Assert.That(
                NoryangjinMapToolWindow.ResolveFeastOfFortuneWall(instance),
                Is.Null);
            Assert.That(
                NoryangjinMapToolWindow.ApplyFeastOfFortuneRarity(
                    instance,
                    Rarity.Unique,
                    recordUndo: false),
                Is.False);
            Assert.That(
                instance.GetComponentInChildren<WallScript>(true).rarity,
                Is.EqualTo(Rarity.Normal));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void WallLifetimeRoot_ResolvesCompleteCompositeRoot()
    {
        GameObject root = new GameObject("CompositeWallRoot");
        root.SetActive(false);
        try
        {
            root.AddComponent<BonusWallLifetimeRoot>();
            GameObject wallObject = new GameObject("WallLogic");
            wallObject.transform.SetParent(root.transform, false);
            WallScript wall = wallObject.AddComponent<WallScript>();
            wall.enabled = false;
            BoxCollider trigger = wallObject.AddComponent<BoxCollider>();
            trigger.enabled = false;
            trigger.isTrigger = false;

            MethodInfo getLifetimeObject = typeof(WallScript).GetMethod(
                "GetLifetimeObject",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(getLifetimeObject, Is.Not.Null);
            Assert.That(getLifetimeObject.Invoke(wall, null), Is.SameAs(root));

            wall.ReactivateLifetimeObject();
            Assert.That(root.activeSelf, Is.True);
            Assert.That(trigger.enabled, Is.True);
            Assert.That(trigger.isTrigger, Is.True, "A reused bonus choice must accept the player again.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ConfigureBonusWallInstance_MarksEveryPlayableWallAsNoryangjinRuntime()
    {
        string prefabPath =
            NoryangjinMapToolWindow.BonusWallPrefabRoot +
            "/random_wall_normal.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject instance =
                PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.scene, Is.EqualTo(previewScene));

            NoryangjinMapToolWindow.ConfigureBonusWallInstance(instance, recordUndo: false);

            WallScript[] walls = instance.GetComponentsInChildren<WallScript>(true);
            Assert.That(walls, Is.Not.Empty);
            foreach (WallScript wall in walls)
            {
                RuntimeBonusWall marker = wall.GetComponent<RuntimeBonusWall>();
                Assert.That(marker, Is.Not.Null);
                Assert.That(marker.RemoveWhenPreparingStage, Is.False);
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void EnemyPalette_UsesDedicatedOccupancyLayerAndFixedPrefabTiers()
    {
        ForwardEnemyArchetypeDefinition[] definitions =
            ForwardEnemyArchetypeCatalog.Definitions;
        Assert.That(NoryangjinMapToolWindow.EnemyPalettePrefabPaths.Length, Is.EqualTo(definitions.Length));
        for (int i = 0; i < definitions.Length; i++)
        {
            ForwardEnemyArchetypeDefinition definition = definitions[i];
            string prefabPath = NoryangjinMapToolWindow.EnemyPalettePrefabPaths[i];
            Assert.That(prefabPath, Is.EqualTo(definition.PrefabPath));
            Assert.That(
                NoryangjinMapToolWindow.TryGetFixedEnemyTier(prefabPath, out EnemyTier mappedTier),
                Is.True,
                prefabPath);
            Assert.That(mappedTier, Is.EqualTo(definition.Tier), prefabPath);
            Assert.That(
                NoryangjinMapToolWindow.GetPaletteItemLayer(
                    prefabPath,
                    NoryangjinMapToolPaletteCategory.Prop),
                Is.EqualTo(NoryangjinMapToolPlacementLayer.Enemy),
                prefabPath);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            EnemyScript_space enemy = prefab.GetComponent<EnemyScript_space>();
            Assert.That(enemy, Is.Not.Null, prefabPath);
        }

        Assert.That(
            NoryangjinMapToolWindow.GetPaletteItemLayer(
                NoryangjinMapToolWindow.EnemyMovementTriggerPrefabPath,
                NoryangjinMapToolPaletteCategory.Prop),
            Is.EqualTo(NoryangjinMapToolPlacementLayer.Enemy));
    }

    [Test]
    public void EnemyScriptSpace_UsesOnlyTheNoryangjinAuthoringContract()
    {
        GameObject fortuneAltar = AssetDatabase.LoadAssetAtPath<GameObject>(
            NoryangjinMapToolWindow.FeastOfFortuneBonusWallPrefabPaths[0]);
        Assert.That(fortuneAltar, Is.Not.Null);

        foreach (ForwardEnemyArchetypeDefinition definition in
                 ForwardEnemyArchetypeCatalog.Definitions)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                definition.PrefabPath);
            EnemyScript_space enemy = prefab.GetComponent<EnemyScript_space>();
            Assert.That(enemy, Is.Not.Null, definition.PrefabPath);

            var serializedEnemy = new SerializedObject(enemy);
            Assert.That(
                serializedEnemy.FindProperty("enemyData").objectReferenceValue,
                Is.Not.Null,
                definition.PrefabPath);
            Assert.That(
                serializedEnemy.FindProperty("bonusWall").objectReferenceValue,
                Is.SameAs(fortuneAltar),
                definition.PrefabPath);

            float expectedDelay = definition.Identity switch
            {
                "Enemy_FatMan" => 1.6f,
                "Enemy_Guard" => 0.8f,
                _ => 2f
            };
            Assert.That(
                serializedEnemy.FindProperty("throwReleaseDelay").floatValue,
                Is.EqualTo(expectedDelay).Within(0.001f),
                definition.PrefabPath);
        }

        string[] legacySpacePrefabPaths =
        {
            "Assets/ShooterSurvival/Prefabs/Entities/Space/EnemyTypeWalker.prefab",
            "Assets/ShooterSurvival/Prefabs/Entities/Space/EnemyTypeRusher.prefab",
            "Assets/ShooterSurvival/Prefabs/Entities/Space/EnemyTypeTank.prefab"
        };
        foreach (string prefabPath in legacySpacePrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            Assert.That(prefab.GetComponent<EnemyScript_space>(), Is.Null, prefabPath);
        }
    }

    [Test]
    public void EnemyDeathDrop_SpawnsTransientFortuneAltarAtEnemyPosition()
    {
        GameObject fortuneAltar = AssetDatabase.LoadAssetAtPath<GameObject>(
            NoryangjinMapToolWindow.FeastOfFortuneBonusWallPrefabPaths[0]);
        GameObject enemyObject = new("Enemy Drop Test");
        GameObject spawnedAltar = null;
        try
        {
            enemyObject.transform.position = new Vector3(3f, 0.4f, -7f);
            EnemyScript_space enemy = enemyObject.AddComponent<EnemyScript_space>();
            FieldInfo bonusWallField = typeof(EnemyScript_space).GetField(
                "bonusWall",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo spawnBonusAltar = typeof(EnemyScript_space).GetMethod(
                "SpawnBonusAltar",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bonusWallField, Is.Not.Null);
            Assert.That(spawnBonusAltar, Is.Not.Null);

            bonusWallField.SetValue(enemy, fortuneAltar);
            spawnedAltar = (GameObject)spawnBonusAltar.Invoke(enemy, null);

            Assert.That(spawnedAltar, Is.Not.Null);
            Assert.That(spawnedAltar.transform.position, Is.EqualTo(enemyObject.transform.position));
            Assert.That(spawnedAltar.transform.localScale, Is.EqualTo(Vector3.one * 3f));
            Assert.That(
                Mathf.DeltaAngle(spawnedAltar.transform.eulerAngles.y, 180f),
                Is.EqualTo(0f).Within(0.01f));
            Assert.That(spawnedAltar.GetComponent<AuthoredBonusWall>(), Is.Not.Null);
            Assert.That(spawnedAltar.GetComponent<BonusWallLifetimeRoot>(), Is.Not.Null);
            RuntimeBonusWall runtimeMarker = spawnedAltar.GetComponent<RuntimeBonusWall>();
            Assert.That(runtimeMarker, Is.Not.Null);
            Assert.That(runtimeMarker.RemoveWhenPreparingStage, Is.True);
            Assert.That(
                spawnedAltar.GetComponents<RuntimeBonusWall>(),
                Has.Length.EqualTo(1));
            WallScript wall = spawnedAltar.GetComponentInChildren<WallScript>(true);
            MethodInfo resolveEffectOverlay = typeof(WallScript).GetMethod(
                "ResolveEffectOverlay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(wall, Is.Not.Null);
            Assert.That(resolveEffectOverlay, Is.Not.Null);
            Assert.That(resolveEffectOverlay.Invoke(wall, null), Is.Null);
        }
        finally
        {
            if (spawnedAltar != null)
                Object.DestroyImmediate(spawnedAltar);
            Object.DestroyImmediate(enemyObject);
        }
    }

    [Test]
    public void FixedTierResolver_IgnoresSceneInstanceTierOverrides()
    {
        Assert.That(
            ForwardEnemyTierResolver.ResolveOrFallback(
                "Enemy_Enemy_Woman_X+00_Z+00",
                EnemyTier.Normal),
            Is.EqualTo(EnemyTier.Boss));
        Assert.That(
            ForwardEnemyTierResolver.ResolveOrFallback(
                "Enemy_FatMan(Clone)",
                EnemyTier.Normal),
            Is.EqualTo(EnemyTier.Elite));
        Assert.That(
            ForwardEnemyTierResolver.ResolveOrFallback(
                "Enemy_Guard(Clone)",
                EnemyTier.Boss),
            Is.EqualTo(EnemyTier.Normal));
    }

    [Test]
    public void Reset_ReactivatesDeadEncounterEnemyButNotQueuedPoolEnemy()
    {
        GameObject poolRoot = new("EnemyPoolerTest");
        poolRoot.SetActive(false);
        poolRoot.AddComponent<EnemyPooler>();
        GameObject queuedEnemy = new("QueuedEnemy");
        GameObject deadEncounterEnemy = new("DeadEncounterEnemy");

        try
        {
            queuedEnemy.transform.SetParent(poolRoot.transform);
            queuedEnemy.SetActive(false);
            deadEncounterEnemy.SetActive(false);

            Assert.That(GameManager.IsPooledEnemy(queuedEnemy), Is.True);
            Assert.That(GameManager.IsPooledEnemy(deadEncounterEnemy), Is.False);
            Assert.That(GameManager.ShouldResetEnemyByReEnable(queuedEnemy), Is.False);
            Assert.That(GameManager.ShouldResetEnemyByReEnable(deadEncounterEnemy), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(queuedEnemy);
            Object.DestroyImmediate(deadEncounterEnemy);
            Object.DestroyImmediate(poolRoot);
        }
    }

    [Test]
    public void EnemyOccupancy_BlocksOnlyOtherEnemiesOnTheSameCell()
    {
        Vector2Int[] footprint = { new(3, 7) };
        HashSet<NoryangjinMapToolOccupiedCell> occupiedCells = new()
        {
            new NoryangjinMapToolOccupiedCell(
                new Vector2Int(3, 7),
                NoryangjinMapToolPlacementLayer.Object)
        };

        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                footprint,
                NoryangjinMapToolPlacementLayer.Enemy,
                occupiedCells),
            Is.True);

        occupiedCells.Add(new NoryangjinMapToolOccupiedCell(
            new Vector2Int(3, 7),
            NoryangjinMapToolPlacementLayer.Enemy));

        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                footprint,
                NoryangjinMapToolPlacementLayer.Enemy,
                occupiedCells),
            Is.False);
    }

    [Test]
    public void EnemyTabDelete_PrefersEnemyAndNeverDeletesUnderlyingRoad()
    {
        GameObject roadParent = new("Roads");
        GameObject enemyParent = new("Enemies");
        GameObject road = new("Road_Wide_X+00_Z+00");
        GameObject enemy = new("Enemy_Guard_X+00_Z+00");

        try
        {
            road.transform.SetParent(roadParent.transform);
            enemy.transform.SetParent(enemyParent.transform);
            List<GameObject> candidates = new() { road, enemy };

            Assert.That(
                NoryangjinMapToolWindow.SelectSingleCursorDeleteTarget(
                    candidates,
                    Vector2Int.zero,
                    NoryangjinMapToolPlacementLayer.Enemy),
                Is.SameAs(enemy));
            Assert.That(
                NoryangjinMapToolWindow.SelectSingleCursorDeleteTarget(
                    new List<GameObject> { road },
                    Vector2Int.zero,
                    NoryangjinMapToolPlacementLayer.Enemy),
                Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(road);
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(roadParent);
            Object.DestroyImmediate(enemyParent);
        }
    }

    [Test]
    public void ContentTabs_OwnOnlyTheirMatchingPlacedObjects()
    {
        GameObject enemyParent = new("Enemies");
        GameObject bonusParent = new("Bonuses");
        GameObject enemy = new("Enemy_Guard_X+00_Z+00");
        GameObject bonus = new("Bonus_random_wall_normal_X+00_Z+00");
        GameObject prop = new("Prop_Box_X+00_Z+00");
        GameObject turnSpot = new("TurnSpot_X+00_Z+00");

        try
        {
            enemy.transform.SetParent(enemyParent.transform);
            bonus.transform.SetParent(bonusParent.transform);
            turnSpot.AddComponent<NoryangjinTurnSpot>();

            Assert.That(
                NoryangjinMapToolWindow.IsPlacedObjectOwnedByContentTab(
                    enemy,
                    NoryangjinMapToolContentTab.Enemy),
                Is.True);
            Assert.That(
                NoryangjinMapToolWindow.IsPlacedObjectOwnedByContentTab(
                    prop,
                    NoryangjinMapToolContentTab.Object),
                Is.True);
            Assert.That(
                NoryangjinMapToolWindow.IsPlacedObjectOwnedByContentTab(
                    turnSpot,
                    NoryangjinMapToolContentTab.Gimmick),
                Is.True);
            Assert.That(
                NoryangjinMapToolWindow.IsPlacedObjectOwnedByContentTab(
                    turnSpot,
                    NoryangjinMapToolContentTab.Object),
                Is.False);
            Assert.That(
                NoryangjinMapToolWindow.IsPlacedObjectOwnedByContentTab(
                    bonus,
                    NoryangjinMapToolContentTab.Bonus),
                Is.True);
            Assert.That(
                NoryangjinMapToolWindow.IsPlacedObjectOwnedByContentTab(
                    bonus,
                    NoryangjinMapToolContentTab.Object),
                Is.False);
        }
        finally
        {
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(prop);
            Object.DestroyImmediate(turnSpot);
            Object.DestroyImmediate(enemyParent);
            Object.DestroyImmediate(bonusParent);
        }
    }

    [Test]
    public void EnemyAssignment_ResolvesClickedEnemyRootAndRejectsOtherObjects()
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();

        try
        {
            GameObject mapToolRoot = CreatePreviewObject(
                previewScene,
                "Noryangjin_MapTool");
            GameObject enemies = CreatePreviewObject(previewScene, "Enemies");
            GameObject props = CreatePreviewObject(previewScene, "Props");
            GameObject enemy = CreatePreviewObject(
                previewScene,
                "Enemy_Guard_X+00_Z+00");
            GameObject enemyMesh = CreatePreviewObject(previewScene, "EnemyMesh");
            GameObject prop = CreatePreviewObject(
                previewScene,
                "Prop_Box_X+00_Z+00");
            GameObject triggerObject = CreatePreviewObject(
                previewScene,
                "EnemyTrigger_X+01_Z+00");
            GameObject triggerVisual = CreatePreviewObject(
                previewScene,
                "TriggerVisual");
            GameObject outsideEnemy = CreatePreviewObject(
                previewScene,
                "Enemy_OldMan_X+02_Z+00");

            enemies.transform.SetParent(mapToolRoot.transform);
            props.transform.SetParent(mapToolRoot.transform);
            enemy.transform.SetParent(enemies.transform);
            enemyMesh.transform.SetParent(enemy.transform);
            prop.transform.SetParent(props.transform);
            triggerObject.transform.SetParent(enemies.transform);
            triggerVisual.transform.SetParent(triggerObject.transform);

            EnemyMovementController enemyMovement =
                enemy.AddComponent<EnemyMovementController>();
            prop.AddComponent<EnemyMovementController>();
            EnemyMovementActivationTrigger trigger =
                triggerObject.AddComponent<EnemyMovementActivationTrigger>();
            outsideEnemy.AddComponent<EnemyMovementController>();

            Assert.That(
                NoryangjinMapToolWindow.ResolveEnemyMovementAssignmentTarget(
                    enemyMesh,
                    mapToolRoot),
                Is.SameAs(enemyMovement));
            Assert.That(
                NoryangjinMapToolWindow.ResolveEnemyMovementAssignmentTarget(
                    prop,
                    mapToolRoot),
                Is.Null);
            Assert.That(
                NoryangjinMapToolWindow.ResolveEnemyMovementAssignmentTarget(
                    triggerObject,
                    mapToolRoot),
                Is.Null);
            Assert.That(
                NoryangjinMapToolWindow.ResolveEnemyMovementAssignmentTarget(
                    outsideEnemy,
                    mapToolRoot),
                Is.Null);
            Assert.That(
                NoryangjinMapToolWindow.ResolveEnemyMovementAssignmentTriggerFromSelection(
                    triggerVisual),
                Is.SameAs(trigger));
            Assert.That(
                NoryangjinMapToolWindow.ResolveEnemyMovementAssignmentTriggerFromSelection(
                    enemyMesh),
                Is.Null);
            Assert.That(
                NoryangjinMapToolWindow.ResolveEnemyMovementAssignmentTriggerFromSelection(
                    outsideEnemy),
                Is.Null);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void EnemyHeightLabel_UsesRedForEnemiesAndTriggerSpots()
    {
        GameObject mapToolRoot = new("Noryangjin_MapTool");
        GameObject enemyParent = new("Enemies");
        GameObject propParent = new("Props");
        GameObject enemy = new("Enemy_Guard_X+00_Z+00");
        GameObject triggerSpot = new("EnemyTrigger_X+01_Z+00");
        GameObject prop = new("Prop_Box_X+02_Z+00");

        try
        {
            enemyParent.transform.SetParent(mapToolRoot.transform);
            propParent.transform.SetParent(mapToolRoot.transform);
            enemy.transform.SetParent(enemyParent.transform);
            triggerSpot.transform.SetParent(propParent.transform);
            prop.transform.SetParent(propParent.transform);
            triggerSpot.AddComponent<EnemyMovementActivationTrigger>();

            Assert.That(
                NoryangjinMapToolWindow.EnemyPlacedObjectHeightLabelColor,
                Is.EqualTo(Color.red));
            Assert.That(
                NoryangjinMapToolWindow.ShouldUseEnemyHeightLabelColor(enemy),
                Is.True);
            Assert.That(
                NoryangjinMapToolWindow.ShouldUseEnemyHeightLabelColor(triggerSpot),
                Is.True);
            Assert.That(
                NoryangjinMapToolWindow.ShouldUseEnemyHeightLabelColor(prop),
                Is.False);
            Assert.That(
                NoryangjinMapToolWindow.ShouldAppendActiveTriggerHeightLabel(
                    triggerSpot,
                    enemyParent.transform,
                    true),
                Is.True);

            triggerSpot.transform.SetParent(enemyParent.transform);
            Assert.That(
                NoryangjinMapToolWindow.ShouldAppendActiveTriggerHeightLabel(
                    triggerSpot,
                    enemyParent.transform,
                    true),
                Is.False);
        }
        finally
        {
            Object.DestroyImmediate(mapToolRoot);
        }
    }

    [Test]
    public void EnemyAssignment_ToggleAddsRemovesAndNormalizesTargets()
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();

        try
        {
            GameObject firstObject = CreatePreviewObject(previewScene, "FirstEnemy");
            GameObject secondObject = CreatePreviewObject(previewScene, "SecondEnemy");
            GameObject thirdObject = CreatePreviewObject(previewScene, "ThirdEnemy");
            EnemyMovementController first =
                firstObject.AddComponent<EnemyMovementController>();
            EnemyMovementController second =
                secondObject.AddComponent<EnemyMovementController>();
            EnemyMovementController third =
                thirdObject.AddComponent<EnemyMovementController>();

            EnemyMovementController[] addedTargets =
                NoryangjinMapToolWindow.BuildToggledEnemyMovementTargets(
                    new[] { first, null, first, second },
                    third,
                    out bool added);

            Assert.That(added, Is.True);
            Assert.That(addedTargets, Is.EqualTo(new[] { first, second, third }));

            EnemyMovementController[] removedTargets =
                NoryangjinMapToolWindow.BuildToggledEnemyMovementTargets(
                    addedTargets,
                    first,
                    out added);

            Assert.That(added, Is.False);
            Assert.That(removedTargets, Is.EqualTo(new[] { second, third }));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void EnemyAssignment_ToggleSupportsUndoAndRedo()
    {
        Undo.IncrementCurrentGroup();
        int testUndoGroup = Undo.GetCurrentGroup();
        Scene previewScene = EditorSceneManager.NewPreviewScene();

        try
        {
            GameObject triggerObject = CreatePreviewObject(
                previewScene,
                "EnemyTrigger_X+00_Z+00");
            GameObject enemyObject = CreatePreviewObject(
                previewScene,
                "Enemy_Guard_X+00_Z+01");
            EnemyMovementActivationTrigger trigger =
                triggerObject.AddComponent<EnemyMovementActivationTrigger>();
            EnemyMovementController enemy =
                enemyObject.AddComponent<EnemyMovementController>();

            Assert.That(
                NoryangjinMapToolWindow.ToggleEnemyMovementTargetAssignment(
                    trigger,
                    enemy),
                Is.True);
            Assert.That(trigger.Targets, Is.EqualTo(new[] { enemy }));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(trigger.Targets, Is.Empty);

            Undo.PerformRedo();
            Assert.That(trigger.Targets, Is.EqualTo(new[] { enemy }));
        }
        finally
        {
            Undo.RevertAllDownToGroup(testUndoGroup);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void EnemyAssignment_ConsumesOnlyPlainLeftClick()
    {
        Assert.That(
            NoryangjinMapToolWindow.ShouldHandleEnemyAssignmentInput(
                EventType.MouseDown,
                0,
                EventModifiers.None),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ShouldHandleEnemyAssignmentInput(
                EventType.MouseDown,
                0,
                EventModifiers.Alt),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.ShouldHandleEnemyAssignmentInput(
                EventType.MouseDown,
                0,
                EventModifiers.Shift),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.ShouldHandleEnemyAssignmentInput(
                EventType.MouseDown,
                0,
                EventModifiers.Control),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.ShouldHandleEnemyAssignmentInput(
                EventType.MouseDown,
                0,
                EventModifiers.Command),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.ShouldHandleEnemyAssignmentInput(
                EventType.MouseDown,
                1,
                EventModifiers.None),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.ShouldHandleEnemyAssignmentInput(
                EventType.MouseMove,
                0,
                EventModifiers.None),
            Is.False);
    }

    [Test]
    public void EnemyAssignment_EscapeCancelsOnlyOnKeyDown()
    {
        var escapeKeyDown = new Event
        {
            type = EventType.KeyDown,
            keyCode = KeyCode.Escape
        };
        var escapeKeyUp = new Event
        {
            type = EventType.KeyUp,
            keyCode = KeyCode.Escape
        };
        var enterKeyDown = new Event
        {
            type = EventType.KeyDown,
            keyCode = KeyCode.Return
        };

        Assert.That(
            NoryangjinMapToolWindow.IsEnemyAssignmentCancelEvent(escapeKeyDown),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.IsEnemyAssignmentCancelEvent(escapeKeyUp),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.IsEnemyAssignmentCancelEvent(enterKeyDown),
            Is.False);
    }

    [Test]
    public void ActivationTrigger_TargetsStaySerializedButHiddenFromInspector()
    {
        FieldInfo targetsField = typeof(EnemyMovementActivationTrigger).GetField(
            "targets",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(targetsField, Is.Not.Null);
        Assert.That(
            targetsField.GetCustomAttribute<SerializeField>(),
            Is.Not.Null);
        Assert.That(
            targetsField.GetCustomAttribute<HideInInspector>(),
            Is.Not.Null);
    }

    [Test]
    public void EnemyAssignment_SelectedTriggerOwnsModeAndCancelClearsSelection()
    {
        const BindingFlags InstancePrivate =
            BindingFlags.Instance | BindingFlags.NonPublic;
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        Object previousSelection = Selection.activeObject;
        NoryangjinMapToolWindow window =
            ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();

        try
        {
            GameObject mapToolRoot = CreatePreviewObject(
                previewScene,
                "Noryangjin_MapTool");
            GameObject enemies = CreatePreviewObject(previewScene, "Enemies");
            GameObject firstObject = CreatePreviewObject(
                previewScene,
                "EnemyTrigger_X+00_Z+00");
            GameObject secondObject = CreatePreviewObject(
                previewScene,
                "EnemyTrigger_X+01_Z+00");
            GameObject enemyObject = CreatePreviewObject(
                previewScene,
                "Enemy_Guard_X+02_Z+00");
            enemies.transform.SetParent(mapToolRoot.transform);
            firstObject.transform.SetParent(enemies.transform);
            secondObject.transform.SetParent(enemies.transform);
            enemyObject.transform.SetParent(enemies.transform);

            EnemyMovementActivationTrigger first =
                firstObject.AddComponent<EnemyMovementActivationTrigger>();
            EnemyMovementActivationTrigger second =
                secondObject.AddComponent<EnemyMovementActivationTrigger>();
            EnemyMovementController enemy =
                enemyObject.AddComponent<EnemyMovementController>();

            FieldInfo contentTabField = typeof(NoryangjinMapToolWindow).GetField(
                "selectedContentTab",
                InstancePrivate);
            FieldInfo palettePathField = typeof(NoryangjinMapToolWindow).GetField(
                "selectedPalettePrefabPath",
                InstancePrivate);
            FieldInfo activeTriggerField = typeof(NoryangjinMapToolWindow).GetField(
                "enemyAssignmentTrigger",
                InstancePrivate);
            FieldInfo copiedObjectField = typeof(NoryangjinMapToolWindow).GetField(
                "copiedPlacedObjectInstanceId",
                InstancePrivate);
            FieldInfo hoveredTargetField = typeof(NoryangjinMapToolWindow).GetField(
                "hoveredEnemyAssignmentTarget",
                InstancePrivate);
            MethodInfo resolveActive = typeof(NoryangjinMapToolWindow).GetMethod(
                "ResolveActiveEnemyAssignmentTrigger",
                InstancePrivate);
            MethodInfo cancelSelection = typeof(NoryangjinMapToolWindow).GetMethod(
                "CancelEnemyAssignmentSelection",
                InstancePrivate);

            Assert.That(contentTabField, Is.Not.Null);
            Assert.That(palettePathField, Is.Not.Null);
            Assert.That(activeTriggerField, Is.Not.Null);
            Assert.That(copiedObjectField, Is.Not.Null);
            Assert.That(hoveredTargetField, Is.Not.Null);
            Assert.That(resolveActive, Is.Not.Null);
            Assert.That(cancelSelection, Is.Not.Null);

            contentTabField.SetValue(
                window,
                NoryangjinMapToolContentTab.Object);
            palettePathField.SetValue(
                window,
                "Assets/Test/Placement.prefab");
            copiedObjectField.SetValue(window, int.MaxValue);

            Selection.activeGameObject = firstObject;
            Assert.That(resolveActive.Invoke(window, null), Is.SameAs(first));
            Assert.That(activeTriggerField.GetValue(window), Is.SameAs(first));
            Assert.That(
                contentTabField.GetValue(window),
                Is.EqualTo(NoryangjinMapToolContentTab.Enemy));
            Assert.That(palettePathField.GetValue(window), Is.Null);
            Assert.That(copiedObjectField.GetValue(window), Is.Zero);

            Selection.activeGameObject = secondObject;
            Assert.That(resolveActive.Invoke(window, null), Is.SameAs(second));
            Assert.That(activeTriggerField.GetValue(window), Is.SameAs(second));

            hoveredTargetField.SetValue(window, enemy);
            cancelSelection.Invoke(window, null);

            Assert.That(activeTriggerField.GetValue(window), Is.Null);
            Assert.That(hoveredTargetField.GetValue(window), Is.Null);
            Assert.That(Selection.activeObject, Is.Null);
            Assert.That(
                palettePathField.GetValue(window),
                Is.EqualTo(NoryangjinMapToolWindow.SelectPaletteItemPrefabPath));
        }
        finally
        {
            Selection.activeObject = previousSelection;
            Object.DestroyImmediate(window);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private static void AssertMaterialRegisteredForAllQualityRenderers(Material material)
    {
        string[] rendererDataPaths =
        {
            "Assets/FlatKit/Demos/Common/URP Configs/[FlatKit] Example Renderer.asset",
            "Assets/Settings/Mobile RP.asset",
            "Assets/Settings/PC RP.asset"
        };

        foreach (string rendererDataPath in rendererDataPaths)
        {
            Object[] rendererAssets = AssetDatabase.LoadAllAssetsAtPath(rendererDataPath);
            Object outlineFeature = System.Array.Find(
                rendererAssets,
                asset => asset != null && asset.GetType().Name == "ObjectOutlineRendererFeature");
            Assert.That(
                outlineFeature,
                Is.Not.Null,
                $"Renderer must support FlatKit object outlines: {rendererDataPath}");

            SerializedObject serializedFeature = new SerializedObject(outlineFeature);
            SerializedProperty activeProperty = serializedFeature.FindProperty("m_Active");
            Assert.That(activeProperty, Is.Not.Null, rendererDataPath);
            Assert.That(activeProperty.boolValue, Is.True, rendererDataPath);

            SerializedProperty materialsProperty = serializedFeature.FindProperty("materials");
            Assert.That(materialsProperty, Is.Not.Null, rendererDataPath);
            bool materialIsRegistered = false;
            for (int index = 0; index < materialsProperty.arraySize; index++)
            {
                if (materialsProperty.GetArrayElementAtIndex(index).objectReferenceValue == material)
                {
                    materialIsRegistered = true;
                    break;
                }
            }

            Assert.That(
                materialIsRegistered,
                Is.True,
                $"Outline feature must register {material.name}: {rendererDataPath}");
        }
    }

    private static Rect GetRectBoundsInSpace(RectTransform source, RectTransform space)
    {
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        float left = float.PositiveInfinity;
        float right = float.NegativeInfinity;
        float bottom = float.PositiveInfinity;
        float top = float.NegativeInfinity;
        foreach (Vector3 corner in corners)
        {
            Vector3 point = space.InverseTransformPoint(corner);
            left = Mathf.Min(left, point.x);
            right = Mathf.Max(right, point.x);
            bottom = Mathf.Min(bottom, point.y);
            top = Mathf.Max(top, point.y);
        }

        return Rect.MinMaxRect(left, bottom, right, top);
    }

    private static Vector2 GetVisibleTextHorizontalBoundsInSpace(
        TMPro.TextMeshProUGUI text,
        RectTransform space)
    {
        float left = float.PositiveInfinity;
        float right = float.NegativeInfinity;
        int visibleCharacters = 0;
        for (int index = 0; index < text.textInfo.characterCount; index++)
        {
            TMPro.TMP_CharacterInfo character = text.textInfo.characterInfo[index];
            if (!character.isVisible)
                continue;

            visibleCharacters++;
            Vector3[] corners =
            {
                character.vertex_BL.position,
                character.vertex_TL.position,
                character.vertex_TR.position,
                character.vertex_BR.position
            };
            foreach (Vector3 corner in corners)
            {
                Vector3 world = text.transform.TransformPoint(corner);
                float x = space.InverseTransformPoint(world).x;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
            }
        }

        Assert.That(visibleCharacters, Is.GreaterThan(0), text.text);
        return new Vector2(left, right);
    }

    private static Vector2 GetVisibleTextVerticalBoundsInSpace(
        TMPro.TextMeshProUGUI text,
        RectTransform space)
    {
        float bottom = float.PositiveInfinity;
        float top = float.NegativeInfinity;
        int visibleCharacters = 0;
        for (int index = 0; index < text.textInfo.characterCount; index++)
        {
            TMPro.TMP_CharacterInfo character = text.textInfo.characterInfo[index];
            if (!character.isVisible)
                continue;

            visibleCharacters++;
            Vector3[] corners =
            {
                character.vertex_BL.position,
                character.vertex_TL.position,
                character.vertex_TR.position,
                character.vertex_BR.position
            };
            foreach (Vector3 corner in corners)
            {
                Vector3 world = text.transform.TransformPoint(corner);
                float y = space.InverseTransformPoint(world).y;
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }
        }

        Assert.That(visibleCharacters, Is.GreaterThan(0), text.text);
        return new Vector2(bottom, top);
    }

    private static GameObject CreatePreviewObject(Scene scene, string name)
    {
        var gameObject = new GameObject(name);
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    private static MonsterGrowthRow CreateValidGrowthRow(int chapter, EnemyTier tier)
    {
        return new MonsterGrowthRow
        {
            chapter = chapter,
            tier = tier,
            initialDamage = 10f,
            finalDamage = 100f,
            initialHealth = 20f,
            finalHealth = 200f,
            coefficient = chapter
        };
    }

    private static void AssertRuntimeStats(
        EnemyScript_space enemy,
        float expectedDamage,
        float expectedHealth)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo damageField = typeof(EnemyScript_space).GetField("_damage", Flags);
        FieldInfo healthField = typeof(EnemyScript_space).GetField("_health", Flags);
        Assert.That(damageField, Is.Not.Null);
        Assert.That(healthField, Is.Not.Null);
        Assert.That(
            (float)damageField.GetValue(enemy),
            Is.EqualTo(expectedDamage).Within(0.001f));
        Assert.That(
            (float)healthField.GetValue(enemy),
            Is.EqualTo(expectedHealth).Within(0.001f));
    }
}
