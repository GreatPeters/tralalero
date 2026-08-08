using System.Collections.Generic;
using System.IO;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        Assert.That(prefabPaths, Has.Length.EqualTo(14));
        Assert.That(
            prefabPaths,
            Does.Not.Contain(
                NoryangjinMapToolWindow.BonusWallPrefabRoot +
                "/random_wall_normal_fix.prefab"));

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
    public void ConfigureBonusWallInstance_MarksEveryPlayableWallAsNoryangjinRuntime()
    {
        string prefabPath =
            NoryangjinMapToolWindow.BonusWallPrefabRoot +
            "/random_wall_normal.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            NoryangjinMapToolWindow.ConfigureBonusWallInstance(instance);

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
            Object.DestroyImmediate(instance);
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
                Is.Not.Null,
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
