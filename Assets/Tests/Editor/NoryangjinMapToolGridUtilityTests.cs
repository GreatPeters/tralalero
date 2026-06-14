using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class NoryangjinMapToolGridUtilityTests
{
    [Test]
    public void GridToWorld_UsesXZGridAndExplicitHeight()
    {
        Vector3 origin = new Vector3(10f, 0.25f, -4f);

        Vector3 world = NoryangjinMapToolGridUtility.GridToWorld(origin, 2, -3, 4.5f, 1.25f);

        Assert.That(world.x, Is.EqualTo(19f).Within(0.001f));
        Assert.That(world.y, Is.EqualTo(1.25f).Within(0.001f));
        Assert.That(world.z, Is.EqualTo(-17.5f).Within(0.001f));
    }

    [Test]
    public void SnapToGrid_RoundsXZAndKeepsExplicitHeight()
    {
        Vector3 origin = new Vector3(1f, 0f, 2f);
        Vector3 position = new Vector3(6.8f, 9f, -4.7f);

        Vector3 snapped = NoryangjinMapToolGridUtility.SnapToGrid(position, origin, 3f, 0.5f);

        Assert.That(snapped.x, Is.EqualTo(7f).Within(0.001f));
        Assert.That(snapped.y, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(snapped.z, Is.EqualTo(-4f).Within(0.001f));
    }

    [Test]
    public void GridToWorld_UsesFallbackCellSizeWhenInvalid()
    {
        Vector3 origin = new Vector3(1f, 0f, 2f);

        Vector3 world = NoryangjinMapToolGridUtility.GridToWorld(origin, 2, -3, 0f, 0.75f);

        Assert.That(world.x, Is.EqualTo(3f).Within(0.001f));
        Assert.That(world.y, Is.EqualTo(0.75f).Within(0.001f));
        Assert.That(world.z, Is.EqualTo(-1f).Within(0.001f));
    }

    [Test]
    public void DefaultCellSize_IsHalfOfPreviousMapToolStep()
    {
        Assert.That(NoryangjinMapToolWindow.DefaultCellSize, Is.EqualTo(1.125f).Within(0.001f));
    }

    [Test]
    public void MigrateCellSize_ReplacesLegacyDefaultWithSmallerStep()
    {
        Assert.That(NoryangjinMapToolWindow.MigrateCellSizeDefault(4.5f), Is.EqualTo(1.125f).Within(0.001f));
        Assert.That(NoryangjinMapToolWindow.MigrateCellSizeDefault(2.25f), Is.EqualTo(1.125f).Within(0.001f));
        Assert.That(NoryangjinMapToolWindow.MigrateCellSizeDefault(3f), Is.EqualTo(3f).Within(0.001f));
    }

    [Test]
    public void DirectionToYaw_ReturnsQuarterTurnYaw()
    {
        Assert.That(NoryangjinMapToolGridUtility.DirectionToYaw(NoryangjinMapToolDirection.North), Is.EqualTo(0f));
        Assert.That(NoryangjinMapToolGridUtility.DirectionToYaw(NoryangjinMapToolDirection.East), Is.EqualTo(90f));
        Assert.That(NoryangjinMapToolGridUtility.DirectionToYaw(NoryangjinMapToolDirection.South), Is.EqualTo(180f));
        Assert.That(NoryangjinMapToolGridUtility.DirectionToYaw(NoryangjinMapToolDirection.West), Is.EqualTo(270f));
    }

    [Test]
    public void DirectionAfterRoadTurn_UpdatesHeadingBeforeCursorAdvance()
    {
        NoryangjinMapToolDirection afterLeftTurn = NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(
            NoryangjinMapToolDirection.North,
            NoryangjinMapToolRoadTurn.Left90);
        Vector2Int leftTurnStep = NoryangjinMapToolGridUtility.DirectionToStep(afterLeftTurn);

        Assert.That(afterLeftTurn, Is.EqualTo(NoryangjinMapToolDirection.West));
        Assert.That(leftTurnStep, Is.EqualTo(new Vector2Int(-1, 0)));

        NoryangjinMapToolDirection afterRightTurn = NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(
            NoryangjinMapToolDirection.East,
            NoryangjinMapToolRoadTurn.Right90);
        Vector2Int rightTurnStep = NoryangjinMapToolGridUtility.DirectionToStep(afterRightTurn);

        Assert.That(afterRightTurn, Is.EqualTo(NoryangjinMapToolDirection.South));
        Assert.That(rightTurnStep, Is.EqualTo(new Vector2Int(0, -1)));
    }

    [Test]
    public void DirectionAfterRoadTurn_CoversEveryQuarterTurn()
    {
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.North, NoryangjinMapToolRoadTurn.Straight), Is.EqualTo(NoryangjinMapToolDirection.North));
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.East, NoryangjinMapToolRoadTurn.Straight), Is.EqualTo(NoryangjinMapToolDirection.East));
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.South, NoryangjinMapToolRoadTurn.Straight), Is.EqualTo(NoryangjinMapToolDirection.South));
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.West, NoryangjinMapToolRoadTurn.Straight), Is.EqualTo(NoryangjinMapToolDirection.West));

        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.North, NoryangjinMapToolRoadTurn.Right90), Is.EqualTo(NoryangjinMapToolDirection.East));
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.East, NoryangjinMapToolRoadTurn.Right90), Is.EqualTo(NoryangjinMapToolDirection.South));
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.South, NoryangjinMapToolRoadTurn.Right90), Is.EqualTo(NoryangjinMapToolDirection.West));
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.West, NoryangjinMapToolRoadTurn.Right90), Is.EqualTo(NoryangjinMapToolDirection.North));

        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.North, NoryangjinMapToolRoadTurn.Left90), Is.EqualTo(NoryangjinMapToolDirection.West));
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.East, NoryangjinMapToolRoadTurn.Left90), Is.EqualTo(NoryangjinMapToolDirection.North));
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.South, NoryangjinMapToolRoadTurn.Left90), Is.EqualTo(NoryangjinMapToolDirection.East));
        Assert.That(NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(NoryangjinMapToolDirection.West, NoryangjinMapToolRoadTurn.Left90), Is.EqualTo(NoryangjinMapToolDirection.South));
    }

    [Test]
    public void BuildInstanceName_IncludesKindAndGridCoordinate()
    {
        string name = NoryangjinMapToolGridUtility.BuildInstanceName("Road", "Straight", -2, 7);

        Assert.That(name, Is.EqualTo("Road_Straight_X-02_Z+07"));
    }

    [Test]
    public void KnownRoadPrefabs_AreAvailableToMapTool()
    {
        string[] missingRoadPrefabs = NoryangjinMapToolWindow.FindMissingRoadPrefabPaths();

        Assert.That(missingRoadPrefabs, Is.Empty);
    }

    [Test]
    public void KnownRoadPrefabs_UseFantasyBasicBridgeUphillAndDownhillRoadSet()
    {
        Dictionary<string, string> roadPaths = GetKnownRoadPiecePathsByLabel();

        Assert.That(roadPaths, Has.Count.EqualTo(4));
        Assert.That(
            roadPaths["Basic"],
            Is.EqualTo("Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy.prefab"));
        Assert.That(
            roadPaths["Bridge"],
            Is.EqualTo("Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Bridges_Fantasy/Bridge_Rope_Small_Fantasy.prefab"));
        Assert.That(
            roadPaths["Uphill"],
            Is.EqualTo("Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Rope_Stairs_Fantasy.prefab"));
        Assert.That(
            roadPaths["Downhill"],
            Is.EqualTo("Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Rope_Stairs_Fantasy_Downhill.prefab"));
    }

    [Test]
    public void StairRoadPieces_IncludePillarsAsCompanionPrefab()
    {
        Dictionary<string, string[]> companionPaths = GetKnownRoadPieceCompanionPathsByLabel();

        Assert.That(companionPaths["Basic"], Is.Empty);
        Assert.That(companionPaths["Bridge"], Is.Empty);
        Assert.That(
            companionPaths["Uphill"],
            Is.EqualTo(new[]
            {
                "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Pillars_Fantasy.prefab"
            }));
        Assert.That(
            companionPaths["Downhill"],
            Is.EqualTo(new[]
            {
                "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Pillars_Fantasy.prefab"
            }));
    }

    [Test]
    public void DownhillRoadPiece_UsesSeparatePrefabAsset()
    {
        Dictionary<string, string> roadPaths = GetKnownRoadPiecePathsByLabel();

        Assert.That(roadPaths["Downhill"], Is.Not.EqualTo(roadPaths["Uphill"]));
    }

    [Test]
    public void DownhillRoadPiece_UsesCustomizedPillarsLocalPosition()
    {
        Dictionary<string, Vector3[]> companionPositions = GetKnownRoadPieceCompanionLocalPositionsByLabel();

        Assert.That(companionPositions["Uphill"][0], Is.EqualTo(Vector3.zero));
        Assert.That(companionPositions["Downhill"][0].x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(companionPositions["Downhill"][0].y, Is.EqualTo(1.075f).Within(0.001f));
        Assert.That(companionPositions["Downhill"][0].z, Is.EqualTo(-2.233f).Within(0.001f));
    }

    [Test]
    public void KnownRoadPrefabs_AppearInRoadPaletteCategory()
    {
        Dictionary<string, NoryangjinMapToolPaletteCategory> paletteCategories = GetPaletteItemCategoriesByPath();

        foreach (string prefabPath in GetKnownRoadPiecePathsByLabel().Values)
        {
            Assert.That(paletteCategories, Contains.Key(prefabPath));
            Assert.That(paletteCategories[prefabPath], Is.EqualTo(NoryangjinMapToolPaletteCategory.Road));
        }
    }

    [Test]
    public void KnownRoadPrefabs_HavePrefabRootScaleOne()
    {
        foreach (string prefabPath in GetKnownRoadPiecePathsByLabel().Values)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one), prefabPath);
        }
    }

    private static Dictionary<string, string> GetKnownRoadPiecePathsByLabel()
    {
        FieldInfo field = typeof(NoryangjinMapToolWindow).GetField("RoadPieces", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);

        var paths = new Dictionary<string, string>();
        foreach (object roadPiece in (System.Array)field.GetValue(null))
        {
            System.Type roadPieceType = roadPiece.GetType();
            string label = (string)roadPieceType.GetProperty("Label").GetValue(roadPiece);
            string path = (string)roadPieceType.GetProperty("PrefabPath").GetValue(roadPiece);
            paths[label] = path;
        }

        return paths;
    }

    private static Dictionary<string, NoryangjinMapToolPaletteCategory> GetPaletteItemCategoriesByPath()
    {
        MethodInfo method = typeof(NoryangjinMapToolWindow).GetMethod("GetPaletteItems", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);

        var categories = new Dictionary<string, NoryangjinMapToolPaletteCategory>();
        NoryangjinMapToolWindow window = ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();
        try
        {
            foreach (object paletteItem in (IEnumerable)method.Invoke(window, null))
            {
                System.Type paletteItemType = paletteItem.GetType();
                string prefabPath = (string)paletteItemType.GetProperty("PrefabPath").GetValue(paletteItem);
                var category = (NoryangjinMapToolPaletteCategory)paletteItemType.GetProperty("Category").GetValue(paletteItem);
                categories[prefabPath] = category;
            }
        }
        finally
        {
            Object.DestroyImmediate(window);
        }

        return categories;
    }

    private static Dictionary<string, string[]> GetKnownRoadPieceCompanionPathsByLabel()
    {
        FieldInfo field = typeof(NoryangjinMapToolWindow).GetField("RoadPieces", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);

        var paths = new Dictionary<string, string[]>();
        foreach (object roadPiece in (System.Array)field.GetValue(null))
        {
            System.Type roadPieceType = roadPiece.GetType();
            PropertyInfo labelProperty = roadPieceType.GetProperty("Label");
            PropertyInfo companionProperty = roadPieceType.GetProperty("CompanionPrefabPaths");
            Assert.That(companionProperty, Is.Not.Null);

            string label = (string)labelProperty.GetValue(roadPiece);
            string[] companionPaths = (string[])companionProperty.GetValue(roadPiece);
            paths[label] = companionPaths;
        }

        return paths;
    }

    private static Dictionary<string, Vector3[]> GetKnownRoadPieceCompanionLocalPositionsByLabel()
    {
        FieldInfo field = typeof(NoryangjinMapToolWindow).GetField("RoadPieces", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);

        var positions = new Dictionary<string, Vector3[]>();
        foreach (object roadPiece in (System.Array)field.GetValue(null))
        {
            System.Type roadPieceType = roadPiece.GetType();
            PropertyInfo labelProperty = roadPieceType.GetProperty("Label");
            PropertyInfo companionPositionsProperty = roadPieceType.GetProperty("CompanionLocalPositions");
            Assert.That(companionPositionsProperty, Is.Not.Null);

            string label = (string)labelProperty.GetValue(roadPiece);
            Vector3[] companionPositions = (Vector3[])companionPositionsProperty.GetValue(roadPiece);
            positions[label] = companionPositions;
        }

        return positions;
    }

    [Test]
    public void KoreanWindowTitle_IsUsedForReadableEditorUi()
    {
        Assert.That(NoryangjinMapToolWindow.KoreanWindowTitle, Is.EqualTo("노량진 맵툴"));
    }

    [Test]
    public void MapToolScenePath_IsSeparateEditorOnlyToolScene()
    {
        Assert.That(
            NoryangjinMapToolWindow.MapToolScenePath,
            Is.EqualTo("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity"));
        Assert.That(NoryangjinMapToolWindow.IsMapToolScenePath(NoryangjinMapToolWindow.MapToolScenePath), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsMapToolScenePath("Assets/ShooterSurvival/Scenes/Generated/Stage01_Noryangjin_AutoDraft.unity"), Is.False);
    }

    [Test]
    public void MapToolScene_HasWorkFloorForReadablePlacement()
    {
        string sceneYaml = File.ReadAllText(NoryangjinMapToolWindow.MapToolScenePath);

        Assert.That(sceneYaml, Does.Contain("m_Name: MapTool_Work_Floor"));
        Assert.That(sceneYaml, Does.Contain("m_Name: MapTool_Work_Grid"));
        Assert.That(sceneYaml, Does.Contain("m_Name: MapTool_Origin_Post"));
    }

    [Test]
    public void FormatCursorStatus_UsesKoreanLabelsAndGridCoordinates()
    {
        string status = NoryangjinMapToolWindow.FormatCursorStatus(
            false,
            3,
            -2,
            NoryangjinMapToolDirection.West,
            4.5f);

        Assert.That(status, Does.Contain("일반 씬"));
        Assert.That(status, Does.Contain("커서 X 3 / Z -2"));
        Assert.That(status, Does.Contain("방향 서쪽"));
        Assert.That(status, Does.Contain("셀 4.5"));
    }

    [Test]
    public void DirectionToKorean_ReturnsReadableDirectionName()
    {
        Assert.That(NoryangjinMapToolWindow.DirectionToKorean(NoryangjinMapToolDirection.North), Is.EqualTo("북쪽"));
        Assert.That(NoryangjinMapToolWindow.DirectionToKorean(NoryangjinMapToolDirection.East), Is.EqualTo("동쪽"));
        Assert.That(NoryangjinMapToolWindow.DirectionToKorean(NoryangjinMapToolDirection.South), Is.EqualTo("남쪽"));
        Assert.That(NoryangjinMapToolWindow.DirectionToKorean(NoryangjinMapToolDirection.West), Is.EqualTo("서쪽"));
    }

    [Test]
    public void PrimaryTabs_StartWithSimplePlacement()
    {
        Assert.That(NoryangjinMapToolWindow.PrimaryTabNames, Is.Empty);
    }

    [Test]
    public void MoveGridCoordinate_OffsetsCursorBySingleCell()
    {
        Vector2Int moved = NoryangjinMapToolWindow.MoveGridCoordinate(new Vector2Int(4, -2), -1, 1);

        Assert.That(moved, Is.EqualTo(new Vector2Int(3, -1)));
    }

    [Test]
    public void FormatSimplePlacementHint_ExplainsNextActionInKorean()
    {
        string hint = NoryangjinMapToolWindow.FormatSimplePlacementHint(
            new Vector3(9f, 0.5f, -4f),
            NoryangjinMapToolDirection.North);

        Assert.That(hint, Does.Contain("월드 좌표"));
        Assert.That(hint, Does.Contain("북쪽"));
        Assert.That(hint, Does.Contain("아이콘"));
    }

    [Test]
    public void CategorizePrefabPath_GroupsRtsPaletteItems()
    {
        Assert.That(
            NoryangjinMapToolWindow.CategorizePrefabPath("Assets/046_STAGE01_NRY_ROAD_038_Noryangjin_modular_straight_timber_road_module.prefab"),
            Is.EqualTo(NoryangjinMapToolPaletteCategory.Road));
        Assert.That(
            NoryangjinMapToolWindow.CategorizePrefabPath("Assets/015_STAGE01_NRY_BLD_002_Sashimi_restaurant_stall_front.prefab"),
            Is.EqualTo(NoryangjinMapToolPaletteCategory.Building));
        Assert.That(
            NoryangjinMapToolWindow.CategorizePrefabPath("Assets/004_STAGE01_NRY_OBSTACLE_004_Seafood_push_cart.prefab"),
            Is.EqualTo(NoryangjinMapToolPaletteCategory.Prop));
        Assert.That(
            NoryangjinMapToolWindow.CategorizePrefabPath("Assets/020_STAGE01_NRY_DCR_001_Fish_box_stack.prefab"),
            Is.EqualTo(NoryangjinMapToolPaletteCategory.Decoration));
        Assert.That(
            NoryangjinMapToolWindow.CategorizePrefabPath("Assets/018_STAGE01_NRY_BG_002_Harbor_fishing_boat.prefab"),
            Is.EqualTo(NoryangjinMapToolPaletteCategory.Background));
    }

    [Test]
    public void BuildPaletteLabel_RemovesStagePrefixAndUnderscores()
    {
        string label = NoryangjinMapToolWindow.BuildPaletteLabel("Assets/015_STAGE01_NRY_BLD_002_Sashimi_restaurant_stall_front.prefab");

        Assert.That(label, Is.EqualTo("횟집가판"));
    }

    [Test]
    public void BuildPaletteLabel_ReturnsKoreanNamesForPaletteItems()
    {
        Assert.That(
            NoryangjinMapToolWindow.BuildPaletteLabel("Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy.prefab"),
            Is.EqualTo("기본길"));
        Assert.That(
            NoryangjinMapToolWindow.BuildPaletteLabel("Assets/001_STAGE01_NRY_PROPS_001_Blue_fish_crate.prefab"),
            Is.EqualTo("파란상자"));
        Assert.That(
            NoryangjinMapToolWindow.BuildPaletteLabel("Assets/017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop.prefab"),
            Is.EqualTo("바다배경"));
    }

    [Test]
    public void BuildPaletteLabel_TranslatesEveryVisibleNoryangjinPrefab()
    {
        var englishLabels = new List<string>();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin" });

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid).Replace('\\', '/');
            if (prefabPath.Contains("/_old/"))
                continue;
            if (NoryangjinMapToolWindow.CategorizePrefabPath(prefabPath) == NoryangjinMapToolPaletteCategory.Road)
                continue;

            string label = NoryangjinMapToolWindow.BuildPaletteLabel(prefabPath);
            if (ContainsEnglishLetter(label))
                englishLabels.Add($"{label} <- {prefabPath}");
        }

        Assert.That(englishLabels, Is.Empty);
    }

    [Test]
    public void BuildPaletteLabel_KeepsVisibleLabelsWithinEightCharacters()
    {
        var longLabels = new List<string>();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin" });

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid).Replace('\\', '/');
            if (prefabPath.Contains("/_old/"))
                continue;
            if (NoryangjinMapToolWindow.CategorizePrefabPath(prefabPath) == NoryangjinMapToolPaletteCategory.Road)
                continue;

            string label = NoryangjinMapToolWindow.BuildPaletteLabel(prefabPath);
            if (label.Length > 8)
                longLabels.Add($"{label} ({label.Length}) <- {prefabPath}");
        }

        Assert.That(longLabels, Is.Empty);
    }

    [Test]
    public void PaletteCategoryToKorean_ReturnsReadableCategoryName()
    {
        Assert.That(NoryangjinMapToolWindow.PaletteCategoryToKorean(NoryangjinMapToolPaletteCategory.All), Is.EqualTo("전체"));
        Assert.That(NoryangjinMapToolWindow.PaletteCategoryToKorean(NoryangjinMapToolPaletteCategory.Road), Is.EqualTo("도로"));
        Assert.That(NoryangjinMapToolWindow.PaletteCategoryToKorean(NoryangjinMapToolPaletteCategory.Building), Is.EqualTo("건물"));
        Assert.That(NoryangjinMapToolWindow.PaletteCategoryToKorean(NoryangjinMapToolPaletteCategory.Prop), Is.EqualTo("소품"));
        Assert.That(NoryangjinMapToolWindow.PaletteCategoryToKorean(NoryangjinMapToolPaletteCategory.Decoration), Is.EqualTo("장식"));
        Assert.That(NoryangjinMapToolWindow.PaletteCategoryToKorean(NoryangjinMapToolPaletteCategory.Background), Is.EqualTo("배경"));
    }

    [Test]
    public void PaletteLayout_LeavesRoomBetweenCards()
    {
        Assert.That(NoryangjinMapToolWindow.PaletteSidePadding, Is.EqualTo(10));
        Assert.That(NoryangjinMapToolWindow.PaletteTopPadding, Is.GreaterThanOrEqualTo(6));
        Assert.That(NoryangjinMapToolWindow.PaletteTileGap, Is.GreaterThanOrEqualTo(8));
        Assert.That(NoryangjinMapToolWindow.PaletteRowGap, Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void PaletteSections_SeparateMovementAndObjects()
    {
        Assert.That(NoryangjinMapToolWindow.PositionMoveSectionTitle, Is.EqualTo("설치 조정"));
        Assert.That(NoryangjinMapToolWindow.PlacementAngleSectionTitle, Is.EqualTo("설치 각도"));
        Assert.That(NoryangjinMapToolWindow.ObjectSectionTitle, Is.EqualTo("오브젝트"));
    }

    [Test]
    public void JoystickPad_IsHiddenBecauseMousePlacementTracksTheCursor()
    {
        Assert.That(NoryangjinMapToolWindow.ShowJoystickPad, Is.False);
    }

    [Test]
    public void MousePlacementTracking_DoesNotUseRepaintBecauseItWouldDropCoarseSnap()
    {
        Assert.That(NoryangjinMapToolWindow.ShouldTrackSceneMouseForPlacementPreview(EventType.MouseMove), Is.True);
        Assert.That(NoryangjinMapToolWindow.ShouldTrackSceneMouseForPlacementPreview(EventType.MouseDrag), Is.True);
        Assert.That(NoryangjinMapToolWindow.ShouldTrackSceneMouseForPlacementPreview(EventType.Repaint), Is.False);
    }

    [Test]
    public void ShiftModifier_UsesCoarsePlacementSnap()
    {
        var shiftEvent = new Event { modifiers = EventModifiers.Shift };
        var controlEvent = new Event { modifiers = EventModifiers.Control };
        var normalEvent = new Event { modifiers = EventModifiers.None };

        Assert.That(NoryangjinMapToolWindow.ShouldUseCoarsePlacementSnap(shiftEvent), Is.True);
        Assert.That(NoryangjinMapToolWindow.ShouldUseCoarsePlacementSnap(controlEvent), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldUseCoarsePlacementSnap(normalEvent), Is.False);
    }

    [Test]
    public void BrokenCustomPaletteLabel_IsIgnored()
    {
        Assert.That(NoryangjinMapToolWindow.LooksLikeBrokenKoreanText("?꾩씠肄"), Is.True);
        Assert.That(NoryangjinMapToolWindow.LooksLikeBrokenKoreanText("게 수족관"), Is.False);
    }

    [Test]
    public void VisibleKoreanLabels_DoNotContainBrokenCharacters()
    {
        string[] labels =
        {
            NoryangjinMapToolWindow.KoreanWindowTitle,
            NoryangjinMapToolWindow.PositionMoveSectionTitle,
            NoryangjinMapToolWindow.ObjectSectionTitle,
            NoryangjinMapToolWindow.DeleteAllPlacedObjectsButtonLabel,
            NoryangjinMapToolWindow.PaletteCategoryToKorean(NoryangjinMapToolPaletteCategory.All),
            NoryangjinMapToolWindow.PaletteCategoryToKorean(NoryangjinMapToolPaletteCategory.Road),
            NoryangjinMapToolWindow.DirectionToKorean(NoryangjinMapToolDirection.West),
            NoryangjinMapToolWindow.BuildPaletteLabel("Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/030_STAGE01_NRY_PROPS_030_Crab_aquarium_tank/030_STAGE01_NRY_PROPS_030_Crab_aquarium_tank.prefab")
        };

        foreach (string label in labels)
            Assert.That(NoryangjinMapToolWindow.LooksLikeBrokenKoreanText(label), Is.False, label);
    }

    [Test]
    public void PaletteCenterPlacement_DoesNotUseRoadAutoAdvance()
    {
        Assert.That(
            NoryangjinMapToolWindow.ShouldAdvanceRoadCursorAfterPlacement(
                userEnabledAdvance: true,
                placementAllowsAdvance: false),
            Is.False);
    }

    [Test]
    public void EmptyPaletteItem_IsFirstBlankCell()
    {
        Assert.That(NoryangjinMapToolWindow.EmptyPaletteItemLabel, Is.EqualTo("빈 칸"));
        Assert.That(NoryangjinMapToolWindow.EmptyPaletteItemSortOrder, Is.LessThan(0));
        Assert.That(NoryangjinMapToolWindow.EmptyPaletteItemPrefabPath, Is.Not.Empty);
        Assert.That(NoryangjinMapToolWindow.IsEmptyPaletteItemPath(NoryangjinMapToolWindow.EmptyPaletteItemPrefabPath), Is.True);
    }

    [Test]
    public void SelectionPaletteItems_AreSpecialModesNextToBlankCell()
    {
        Assert.That(NoryangjinMapToolWindow.SelectPaletteItemLabel, Is.EqualTo("선택"));
        Assert.That(NoryangjinMapToolWindow.ClearSelectionPaletteItemLabel, Is.EqualTo("해제"));
        Assert.That(NoryangjinMapToolWindow.GetSpecialPaletteIconText(NoryangjinMapToolWindow.SelectPaletteItemPrefabPath), Is.EqualTo("✓"));
        Assert.That(NoryangjinMapToolWindow.GetSpecialPaletteIconText(NoryangjinMapToolWindow.ClearSelectionPaletteItemPrefabPath), Is.EqualTo("X"));
        Assert.That(NoryangjinMapToolWindow.SelectPaletteItemSortOrder, Is.GreaterThan(NoryangjinMapToolWindow.EmptyPaletteItemSortOrder));
        Assert.That(NoryangjinMapToolWindow.ClearSelectionPaletteItemSortOrder, Is.GreaterThan(NoryangjinMapToolWindow.SelectPaletteItemSortOrder));
        Assert.That(NoryangjinMapToolWindow.IsSelectPaletteItemPath(NoryangjinMapToolWindow.SelectPaletteItemPrefabPath), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsClearSelectionPaletteItemPath(NoryangjinMapToolWindow.ClearSelectionPaletteItemPrefabPath), Is.True);
    }

    [Test]
    public void DirectionFromMoveOffset_UsesLastJoystickMove()
    {
        Assert.That(NoryangjinMapToolWindow.DirectionFromMoveOffset(0, 1), Is.EqualTo(NoryangjinMapToolDirection.North));
        Assert.That(NoryangjinMapToolWindow.DirectionFromMoveOffset(1, 0), Is.EqualTo(NoryangjinMapToolDirection.East));
        Assert.That(NoryangjinMapToolWindow.DirectionFromMoveOffset(0, -1), Is.EqualTo(NoryangjinMapToolDirection.South));
        Assert.That(NoryangjinMapToolWindow.DirectionFromMoveOffset(-1, 0), Is.EqualTo(NoryangjinMapToolDirection.West));
    }

    [Test]
    public void UndoCommand_IsHandledForControlZAndEditorUndoCommand()
    {
        var controlZ = new Event
        {
            type = EventType.KeyDown,
            keyCode = KeyCode.Z,
            modifiers = EventModifiers.Control
        };
        var commandZ = new Event
        {
            type = EventType.KeyDown,
            keyCode = KeyCode.Z,
            modifiers = EventModifiers.Command
        };
        var editorUndo = new Event
        {
            type = EventType.ExecuteCommand,
            commandName = "UndoRedoPerformed"
        };
        var mouseClick = new Event { type = EventType.MouseDown };

        Assert.That(NoryangjinMapToolWindow.IsUndoCommand(controlZ), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsUndoCommand(commandZ), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsUndoCommand(editorUndo), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsUndoCommand(mouseClick), Is.False);
    }

    [Test]
    public void IsMapToolPlacedObjectName_MatchesGridCoordinate()
    {
        Assert.That(NoryangjinMapToolWindow.IsMapToolPlacedObjectName("Prop_Test_X+02_Z-01", 2, -1), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsMapToolPlacedObjectName("Road_Straight_X-02_Z+07", -2, 7), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsMapToolPlacedObjectName("Road_Straight_X-02_Z+07", -2, 6), Is.False);
    }

    [Test]
    public void DeleteAllButton_TargetsOnlyPlacedMapToolObjects()
    {
        Assert.That(NoryangjinMapToolWindow.DeleteAllPlacedObjectsButtonLabel, Is.EqualTo("모두 삭제"));
        Assert.That(NoryangjinMapToolWindow.IsMapToolPlacedObjectName("Prop_Test_X+02_Z-01"), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsMapToolPlacedObjectName("Road_Straight_X-02_Z+07"), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsMapToolPlacedObjectName("Roads"), Is.False);
        Assert.That(NoryangjinMapToolWindow.IsMapToolPlacedObjectName("Props"), Is.False);
    }

    [Test]
    public void DeleteAllButton_TargetsDirectChildrenOfPlacementContainersEvenWhenRenamed()
    {
        Assert.That(
            NoryangjinMapToolWindow.ShouldDeleteAllPlacedObjectsTarget("Copied Road", "Roads"),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ShouldDeleteAllPlacedObjectsTarget("Renamed Prop", "Props"),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ShouldDeleteAllPlacedObjectsTarget("Road_Straight_X-02_Z+07", "Noryangjin_MapTool"),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ShouldDeleteAllPlacedObjectsTarget("MapTool_Work_SubGrid_X_P00_S1", "MapTool_Work_Grid"),
            Is.False);
    }

    [Test]
    public void EmptyCellDelete_ChoosesOneCursorTargetWithObjectPriority()
    {
        var roadParent = new GameObject("Roads");
        var propParent = new GameObject("Props");
        var road = new GameObject("Road_Wide_X+00_Z+00");
        var prop = new GameObject("Prop_Box_X+00_Z+00");

        try
        {
            road.transform.SetParent(roadParent.transform);
            prop.transform.SetParent(propParent.transform);

            GameObject target = NoryangjinMapToolWindow.SelectSingleCursorDeleteTarget(
                new List<GameObject>
                {
                    road,
                    prop
                },
                Vector2Int.zero);

            Assert.That(target, Is.SameAs(prop));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(road);
            UnityEngine.Object.DestroyImmediate(prop);
            UnityEngine.Object.DestroyImmediate(roadParent);
            UnityEngine.Object.DestroyImmediate(propParent);
        }
    }

    [Test]
    public void EmptyCellDelete_PrefersObjectAnchoredAtCursorOverBroadOverlap()
    {
        var roadParent = new GameObject("Roads");
        var propParent = new GameObject("Props");
        var exactRoad = new GameObject("Road_Wide_X+00_Z+00");
        var broadProp = new GameObject("Prop_Broad_X+99_Z+99");

        try
        {
            exactRoad.transform.SetParent(roadParent.transform);
            broadProp.transform.SetParent(propParent.transform);

            GameObject target = NoryangjinMapToolWindow.SelectSingleCursorDeleteTarget(
                new List<GameObject>
                {
                    broadProp,
                    exactRoad
                },
                Vector2Int.zero);

            Assert.That(target, Is.SameAs(exactRoad));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(exactRoad);
            UnityEngine.Object.DestroyImmediate(broadProp);
            UnityEngine.Object.DestroyImmediate(roadParent);
            UnityEngine.Object.DestroyImmediate(propParent);
        }
    }

    [Test]
    public void CursorCellObjectLabel_UsesKoreanPaletteNameOrBlankName()
    {
        const string crabAquariumPrefab = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/030_STAGE01_NRY_PROPS_030_Crab_aquarium_tank/030_STAGE01_NRY_PROPS_030_Crab_aquarium_tank.prefab";

        Assert.That(NoryangjinMapToolWindow.BuildCursorCellObjectLabel(crabAquariumPrefab), Is.EqualTo("게 수족관"));
        Assert.That(NoryangjinMapToolWindow.BuildCursorCellObjectLabel(null), Is.EqualTo("빈 칸"));
    }

    [Test]
    public void CursorCellIcon_IsLargeEnoughForCurrentSelection()
    {
        Assert.That(NoryangjinMapToolWindow.CursorCellIconSize, Is.GreaterThanOrEqualTo(128));
        Assert.That(NoryangjinMapToolWindow.CursorCellSummaryWidth, Is.GreaterThanOrEqualTo(NoryangjinMapToolWindow.CursorCellIconSize));
        Assert.That(NoryangjinMapToolWindow.CursorCellLabelTopGap, Is.EqualTo(0));
        Assert.That(NoryangjinMapToolWindow.CursorCellLabelOffsetX, Is.LessThan(0));
        Assert.That(NoryangjinMapToolWindow.CursorCellLabelOffsetY, Is.EqualTo(-6));
        Assert.That(NoryangjinMapToolWindow.PositionMoveControlsMinHeight, Is.GreaterThanOrEqualTo(170));
    }

    [Test]
    public void SceneGridCells_UseOccupiedOrEmptyState()
    {
        var occupiedCells = new HashSet<Vector2Int> { new(1, -1) };

        Assert.That(
            NoryangjinMapToolWindow.GetSceneGridCellState(new Vector2Int(1, -1), occupiedCells),
            Is.EqualTo(NoryangjinMapToolSceneGridCellState.Occupied));
        Assert.That(
            NoryangjinMapToolWindow.GetSceneGridCellState(new Vector2Int(2, -1), occupiedCells),
            Is.EqualTo(NoryangjinMapToolSceneGridCellState.Empty));
    }

    [Test]
    public void SceneGridCellFill_IsOnlyDrawnForCurrentCell()
    {
        Assert.That(
            NoryangjinMapToolWindow.ShouldDrawSceneGridCellFill(new Vector2Int(2, -1), new Vector2Int(2, -1)),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ShouldDrawSceneGridCellFill(new Vector2Int(1, -1), new Vector2Int(2, -1)),
            Is.False);
    }

    [Test]
    public void SceneGridDirectionArrow_IsDisabled()
    {
        Assert.That(NoryangjinMapToolWindow.ShowSceneDirectionArrow, Is.False);
    }

    [Test]
    public void SceneGridOverlay_IsHiddenByDefault()
    {
        Assert.That(NoryangjinMapToolWindow.DefaultShowSceneGrid, Is.False);
    }

    [Test]
    public void SceneViewTopMode_UsesExactOverheadOrthographicView()
    {
        NoryangjinMapToolSceneViewPreset preset = NoryangjinMapToolWindow.BuildSceneViewPreset(true);

        Assert.That(preset.Orthographic, Is.True);
        Assert.That(preset.Rotation.eulerAngles.x, Is.EqualTo(90f).Within(0.001f));
        Assert.That(preset.Rotation.eulerAngles.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(preset.Size, Is.EqualTo(NoryangjinMapToolWindow.TopSceneViewSize).Within(0.001f));
    }

    [Test]
    public void SceneViewOriginalMode_UsesReadableAngledMapToolView()
    {
        NoryangjinMapToolSceneViewPreset preset = NoryangjinMapToolWindow.BuildSceneViewPreset(false);

        Assert.That(preset.Orthographic, Is.True);
        Assert.That(preset.Rotation.eulerAngles.x, Is.EqualTo(55f).Within(0.001f));
        Assert.That(preset.Rotation.eulerAngles.y, Is.EqualTo(315f).Within(0.001f));
        Assert.That(preset.Size, Is.EqualTo(NoryangjinMapToolWindow.DefaultSceneViewSize).Within(0.001f));
    }

    [Test]
    public void WorkGridLines_UseCellBoundariesInsteadOfCellCenters()
    {
        float cellSize = NoryangjinMapToolWindow.DefaultCellSize;

        Assert.That(
            NoryangjinMapToolWindow.BuildWorkGridBoundaryOffset(0, cellSize),
            Is.EqualTo(-cellSize * 0.5f).Within(0.001f));
        Assert.That(
            NoryangjinMapToolWindow.BuildWorkGridBoundaryOffset(1, cellSize),
            Is.EqualTo(cellSize * 0.5f).Within(0.001f));
    }

    [Test]
    public void SceneGridCellFill_ExtendsExactlyToWorkGridBoundaries()
    {
        float cellSize = NoryangjinMapToolWindow.DefaultCellSize;

        Assert.That(
            NoryangjinMapToolWindow.BuildSceneGridCellFillHalfExtent(cellSize),
            Is.EqualTo(cellSize * 0.5f).Within(0.001f));
    }

    [Test]
    public void SceneGridCellFill_UsesSameVisualHeightAsWorkGridLines()
    {
        Assert.That(
            NoryangjinMapToolWindow.BuildSceneGridOverlayHeight(0f),
            Is.EqualTo(NoryangjinMapToolWindow.WorkGridLineY).Within(0.001f));
    }

    [Test]
    public void WorkGridLines_AreVisibleInExactTopView()
    {
        Assert.That(NoryangjinMapToolWindow.WorkGridExtent, Is.GreaterThanOrEqualTo(20));
        Assert.That(NoryangjinMapToolWindow.WorkGridLineY, Is.GreaterThanOrEqualTo(0.035f));
        Assert.That(NoryangjinMapToolWindow.WorkGridLineWidth, Is.GreaterThanOrEqualTo(0.045f));
    }

    [Test]
    public void TopViewWorkGridOverlay_UsesFixedPixelWidthForZoomedOutReadability()
    {
        Assert.That(NoryangjinMapToolWindow.DrawTopViewWorkGridOverlay, Is.True);
        Assert.That(NoryangjinMapToolWindow.WorkGridOverlayLineWidthPixels, Is.GreaterThanOrEqualTo(2f));
    }

    [Test]
    public void TopViewWorkGridOverlay_DrawsWhenSceneViewIsTopOrthographicAfterReload()
    {
        Assert.That(
            NoryangjinMapToolWindow.ShouldDrawStableTopViewWorkGridOverlay(
                drawEnabled: true,
                topViewToggle: false,
                sceneViewOrthographic: true,
                sceneViewRotation: Quaternion.Euler(90f, 0f, 0f)),
            Is.True);

        Assert.That(
            NoryangjinMapToolWindow.ShouldDrawStableTopViewWorkGridOverlay(
                drawEnabled: true,
                topViewToggle: false,
                sceneViewOrthographic: true,
                sceneViewRotation: Quaternion.Euler(55f, -45f, 0f)),
            Is.True);

        Assert.That(
            NoryangjinMapToolWindow.ShouldDrawStableTopViewWorkGridOverlay(
                drawEnabled: true,
                topViewToggle: false,
                sceneViewOrthographic: false,
                sceneViewRotation: Quaternion.Euler(90f, 0f, 0f)),
            Is.False);
    }

    [Test]
    public void WorkGridSubdividesEachPlacementCellIntoFiveByFive()
    {
        Assert.That(NoryangjinMapToolWindow.WorkGridSubdivisionsPerCell, Is.EqualTo(5));
        Assert.That(
            NoryangjinMapToolWindow.BuildWorkGridSubcellSize(NoryangjinMapToolWindow.DefaultCellSize),
            Is.EqualTo(NoryangjinMapToolWindow.DefaultCellSize / 5f).Within(0.001f));
    }

    [Test]
    public void PlacementSnapCellSize_UsesSubcellByDefaultAndFullCellWithCoarseSnap()
    {
        float cellSize = NoryangjinMapToolWindow.DefaultCellSize;

        Assert.That(
            NoryangjinMapToolWindow.BuildPlacementSnapCellSize(cellSize, false),
            Is.EqualTo(cellSize / NoryangjinMapToolWindow.WorkGridSubdivisionsPerCell).Within(0.001f));
        Assert.That(
            NoryangjinMapToolWindow.BuildPlacementSnapCellSize(cellSize, true),
            Is.EqualTo(cellSize).Within(0.001f));
    }

    [Test]
    public void BuildPlacementGridCell_SnapsToSubcellByDefaultAndFullCellWithCoarseSnap()
    {
        Vector3 origin = Vector3.zero;
        float cellSize = 10f;
        Vector3 world = new Vector3(5.9f, 0f, -5.9f);

        Assert.That(
            NoryangjinMapToolWindow.BuildPlacementGridCell(world, origin, cellSize, false),
            Is.EqualTo(new Vector2Int(3, -3)));
        Assert.That(
            NoryangjinMapToolWindow.BuildPlacementGridCell(world, origin, cellSize, true),
            Is.EqualTo(new Vector2Int(5, -5)));
    }

    [Test]
    public void SnapPlacementGridCellToCoarseStep_UsesCurrentGridAsAnchor()
    {
        Vector2Int anchor = new Vector2Int(3, -2);

        Assert.That(
            NoryangjinMapToolWindow.SnapPlacementGridCellToCoarseStep(new Vector2Int(9, -13), anchor),
            Is.EqualTo(new Vector2Int(8, -12)));
    }

    [Test]
    public void ScaleManualFootprintForPlacementGrid_ConvertsMainCellsToSubcells()
    {
        Assert.That(
            NoryangjinMapToolWindow.ScaleManualFootprintForPlacementGrid(new Vector2Int(3, 2)),
            Is.EqualTo(new Vector2Int(15, 10)));
    }

    [Test]
    public void PlacementPreview_IsShownOnlyForRealPrefabPaletteItems()
    {
        Assert.That(NoryangjinMapToolWindow.ShouldShowPlacementPreview(null), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldShowPlacementPreview(string.Empty), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldShowPlacementPreview(NoryangjinMapToolWindow.EmptyPaletteItemPrefabPath), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldShowPlacementPreview(NoryangjinMapToolWindow.SelectPaletteItemPrefabPath), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldShowPlacementPreview(NoryangjinMapToolWindow.ClearSelectionPaletteItemPrefabPath), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldShowPlacementPreview("Assets/ShooterSurvival/Prefabs/Test.prefab"), Is.True);
    }

    [Test]
    public void PlacementValidityFill_IsShownOnlyWhilePlacingRealPrefab()
    {
        Assert.That(NoryangjinMapToolWindow.ShouldDrawPlacementValidityFill(null), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldDrawPlacementValidityFill(string.Empty), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldDrawPlacementValidityFill(NoryangjinMapToolWindow.EmptyPaletteItemPrefabPath), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldDrawPlacementValidityFill(NoryangjinMapToolWindow.SelectPaletteItemPrefabPath), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldDrawPlacementValidityFill(NoryangjinMapToolWindow.ClearSelectionPaletteItemPrefabPath), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldDrawPlacementValidityFill("Assets/ShooterSurvival/Prefabs/Test.prefab"), Is.True);
    }

    [Test]
    public void PlacementValidityFill_UsesBrightLimeWhenPlacementIsAllowed()
    {
        Color fill = NoryangjinMapToolWindow.GetPlacementValidityFillColor(NoryangjinMapToolSceneGridCellState.Empty);

        Assert.That(fill.r, Is.InRange(0.3f, 0.4f));
        Assert.That(fill.g, Is.GreaterThanOrEqualTo(0.95f));
        Assert.That(fill.b, Is.InRange(0.14f, 0.22f));
        Assert.That(fill.a, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void PlacementValidityFill_UsesBlueForLastPlacedArea()
    {
        Color fill = NoryangjinMapToolWindow.GetPlacementValidityFillColor(NoryangjinMapToolSceneGridCellState.LastPlaced);

        Assert.That(fill.b, Is.GreaterThan(fill.r));
        Assert.That(fill.b, Is.GreaterThan(fill.g));
        Assert.That(fill.a, Is.GreaterThan(0.3f));
    }

    [Test]
    public void PlacementValidityFill_IsDrawnAsGuiOverlayAboveScenePreview()
    {
        Assert.That(NoryangjinMapToolWindow.DrawPlacementValidityFillAsGuiOverlay, Is.True);
    }

    [Test]
    public void PlacementPreview_PreservesOriginalColorWithHalfAlpha()
    {
        Color original = new Color(0.2f, 0.4f, 0.8f, 1f);

        Color preview = NoryangjinMapToolWindow.BuildPlacementPreviewTransparentColor(original);

        Assert.That(preview.r, Is.EqualTo(original.r).Within(0.001f));
        Assert.That(preview.g, Is.EqualTo(original.g).Within(0.001f));
        Assert.That(preview.b, Is.EqualTo(original.b).Within(0.001f));
        Assert.That(preview.a, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void RotationQuickButtons_UseFortyFiveDegreeStepsWithoutReset()
    {
        Assert.That(NoryangjinMapToolWindow.RotationQuickStepDegrees, Is.EqualTo(45f));
        Assert.That(NoryangjinMapToolWindow.RotationQuickButtonLabels, Is.EqualTo(new[] { "-45도", "+45도" }));
        Assert.That(NoryangjinMapToolWindow.RotationQuickAxis, Is.EqualTo(Vector3.up));
        Assert.That(
            NoryangjinMapToolWindow.MovePlacementYawOffsetByStep(10f, NoryangjinMapToolWindow.RotationQuickStepDegrees),
            Is.EqualTo(55f).Within(0.001f));
        Assert.That(
            NoryangjinMapToolWindow.MovePlacementYawOffsetByStep(10f, -NoryangjinMapToolWindow.RotationQuickStepDegrees),
            Is.EqualTo(-35f).Within(0.001f));
    }

    [Test]
    public void HeightQuickButtons_UseTenthUnitStepsFromCurrentHeight()
    {
        Assert.That(NoryangjinMapToolWindow.HeightQuickStep, Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(NoryangjinMapToolWindow.HeightQuickButtonLabels, Is.EqualTo(new[] { "-0.1", "+0.1" }));
        Assert.That(NoryangjinMapToolWindow.MoveHeightByStep(1.25f, 0.1f), Is.EqualTo(1.35f).Within(0.001f));
        Assert.That(NoryangjinMapToolWindow.MoveHeightByStep(1.25f, -0.1f), Is.EqualTo(1.15f).Within(0.001f));
    }

    [Test]
    public void PositionOffsetQuickButtons_OfferFineAndTenthUnitSteps()
    {
        Assert.That(NoryangjinMapToolWindow.PositionOffsetQuickSteps, Is.EqualTo(new[] { -0.01f, -0.1f, 0.1f, 0.01f }));
        Assert.That(NoryangjinMapToolWindow.PositionOffsetQuickButtonLabels, Is.EqualTo(new[] { "-0.01", "-0.1", "+0.1", "+0.01" }));
        Assert.That(NoryangjinMapToolWindow.MovePositionOffsetByStep(0.25f, 0.01f), Is.EqualTo(0.26f).Within(0.001f));
        Assert.That(NoryangjinMapToolWindow.MovePositionOffsetByStep(0.25f, -0.01f), Is.EqualTo(0.24f).Within(0.001f));
        Assert.That(NoryangjinMapToolWindow.MovePositionOffsetByStep(0.25f, 0.1f), Is.EqualTo(0.35f).Within(0.001f));
        Assert.That(NoryangjinMapToolWindow.MovePositionOffsetByStep(0.25f, -0.1f), Is.EqualTo(0.15f).Within(0.001f));
    }

    [Test]
    public void SelectedObjectSaveButtons_DistinguishIndividualAndPrefabWideActions()
    {
        Assert.That(NoryangjinMapToolWindow.SelectedObjectIndividualSaveButtonLabel, Is.EqualTo("개별 저장"));
        Assert.That(NoryangjinMapToolWindow.SelectedObjectPrefabWideSaveButtonLabel, Is.EqualTo("프리팹 전체 적용"));
        Assert.That(NoryangjinMapToolWindow.SelectedObjectIndividualSaveHint, Does.Contain("현재 선택 오브젝트"));
    }

    [Test]
    public void SelectedObjectScaleFields_ShowAndApplyLocalScaleValues()
    {
        Assert.That(NoryangjinMapToolWindow.SelectedObjectScaleSectionLabel, Is.EqualTo("스케일"));
        Assert.That(
            NoryangjinMapToolWindow.BuildSelectedObjectScaleFromFields(new Vector3(1.2f, 0.8f, 2.5f)),
            Is.EqualTo(new Vector3(1.2f, 0.8f, 2.5f)));
    }

    [Test]
    public void SelectedObjectScaleFields_ShowGridAxesBeforeHeight()
    {
        Assert.That(NoryangjinMapToolWindow.SelectedObjectScaleAxisLabels, Is.EqualTo(new[] { "X", "Z", "Y" }));
        Assert.That(
            NoryangjinMapToolWindow.BuildSelectedObjectScaleFromDisplayedFields(572f, 410f, 562f),
            Is.EqualTo(new Vector3(572f, 562f, 410f)));
    }

    [Test]
    public void ApplyPrefabAssetRootScale_WritesRootScaleToPrefabAsset()
    {
        const string folderPath = "Assets/Tests/Generated";
        const string prefabPath = folderPath + "/MapToolScaleWriteTest.prefab";
        bool createdFolder = false;
        GameObject source = new GameObject("MapToolScaleWriteTest");

        try
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Tests", "Generated");
                createdFolder = true;
            }

            source.transform.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(source, prefabPath);

            Vector3 scale = new Vector3(1.25f, 0.75f, 2.5f);
            Assert.That(NoryangjinMapToolWindow.ApplyPrefabAssetRootScale(prefabPath, scale), Is.True);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab.transform.localScale, Is.EqualTo(scale));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            AssetDatabase.DeleteAsset(prefabPath);
            if (createdFolder)
                AssetDatabase.DeleteAsset(folderPath);
        }
    }

    [Test]
    public void ApplySelectedObjectScaleToTarget_WritesPrefabAssetDuringLiveEdit()
    {
        const string folderPath = "Assets/Tests/Generated";
        const string prefabPath = folderPath + "/MapToolLiveScaleEditTest.prefab";
        bool createdFolder = false;
        GameObject source = new GameObject("MapToolLiveScaleEditTest");
        GameObject instance = null;

        try
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Tests", "Generated");
                createdFolder = true;
            }

            source.transform.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            Vector3 liveScale = new Vector3(1.5f, 0.75f, 2.25f);
            Assert.That(
                NoryangjinMapToolWindow.ApplySelectedObjectScaleToTarget(
                    instance,
                    liveScale,
                    writePrefabAssetRoot: true),
                Is.True);

            Assert.That(instance.transform.localScale, Is.EqualTo(liveScale));
            Assert.That(prefab.transform.localScale, Is.EqualTo(liveScale));
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(source);
            AssetDatabase.DeleteAsset(prefabPath);
            if (createdFolder)
                AssetDatabase.DeleteAsset(folderPath);
        }
    }

    [Test]
    public void ApplyPrefabInstanceRootScaleOverride_WritesPrefabRootFromPlacedInstance()
    {
        const string folderPath = "Assets/Tests/Generated";
        const string prefabPath = folderPath + "/MapToolInstanceScaleOverrideTest.prefab";
        bool createdFolder = false;
        GameObject source = new GameObject("MapToolInstanceScaleOverrideTest");
        GameObject instance = null;

        try
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Tests", "Generated");
                createdFolder = true;
            }

            source.transform.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            Vector3 scale = new Vector3(2f, 0.5f, 3f);
            Assert.That(
                NoryangjinMapToolWindow.ApplyPrefabInstanceRootScaleOverride(instance, prefabPath, scale),
                Is.True);

            Assert.That(instance.transform.localScale, Is.EqualTo(scale));
            Assert.That(prefab.transform.localScale, Is.EqualTo(scale));
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(source);
            AssetDatabase.DeleteAsset(prefabPath);
            if (createdFolder)
                AssetDatabase.DeleteAsset(folderPath);
        }
    }

    [Test]
    public void ResolveSelectedPlacedObject_ReturnsPlacedRootWhenPrefabChildIsSelected()
    {
        GameObject root = new GameObject("Road_Test_X+03_Z-02");
        GameObject child = new GameObject("Mesh");
        child.transform.SetParent(root.transform);

        try
        {
            Assert.That(NoryangjinMapToolWindow.ResolveSelectedPlacedObject(child), Is.SameAs(root));
            Assert.That(NoryangjinMapToolWindow.ResolveSelectedPlacedObject(root), Is.SameAs(root));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ResolveSelectedPlacedObject_ReturnsPlacementContainerChildWhenNameWasChanged()
    {
        GameObject roads = new GameObject("Roads");
        GameObject root = new GameObject("046_STAGE01_NRY_ROAD_038_Noryangjin_modular_straight_timber_road_module");
        GameObject child = new GameObject("Mesh");
        root.transform.SetParent(roads.transform);
        child.transform.SetParent(root.transform);

        try
        {
            Assert.That(NoryangjinMapToolWindow.ResolveSelectedPlacedObject(child), Is.SameAs(root));
            Assert.That(NoryangjinMapToolWindow.ResolveSelectedPlacedObject(root), Is.SameAs(root));
            Assert.That(NoryangjinMapToolWindow.ResolveSelectedPlacedObject(roads), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(roads);
        }
    }

    [Test]
    public void CalculatePaletteColumnCount_UsesAvailableWidth()
    {
        Assert.That(NoryangjinMapToolWindow.CalculatePaletteColumnCount(180f), Is.EqualTo(2));
        Assert.That(NoryangjinMapToolWindow.CalculatePaletteColumnCount(390f), Is.EqualTo(4));
        Assert.That(NoryangjinMapToolWindow.CalculatePaletteColumnCount(500f), Is.EqualTo(5));
    }

    [Test]
    public void GetPaletteTileClickAction_DoubleClickSelectsPrefabAsset()
    {
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteTileClickAction(1, 0),
            Is.EqualTo(NoryangjinMapToolPaletteClickAction.SelectInMapTool));
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteTileClickAction(2, 0),
            Is.EqualTo(NoryangjinMapToolPaletteClickAction.SelectPrefabAsset));
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteTileClickAction(3, 0),
            Is.EqualTo(NoryangjinMapToolPaletteClickAction.SelectPrefabAsset));
    }

    [Test]
    public void GetPaletteTileClickAction_RightClickDoesNotRenameDisplayName()
    {
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteTileClickAction(1, 1),
            Is.EqualTo(NoryangjinMapToolPaletteClickAction.SelectInMapTool));
    }

    [Test]
    public void GetPaletteLabelClickAction_DoubleClickRenamesDisplayName()
    {
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteLabelClickAction(1, 0),
            Is.EqualTo(NoryangjinMapToolPaletteClickAction.SelectInMapTool));
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteLabelClickAction(2, 0),
            Is.EqualTo(NoryangjinMapToolPaletteClickAction.RenameDisplayName));
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteLabelClickAction(2, 1),
            Is.EqualTo(NoryangjinMapToolPaletteClickAction.SelectInMapTool));
    }

    [Test]
    public void SelectProjectAsset_SelectsOriginalPrefabAsset()
    {
        const string prefabPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/001_STAGE01_NRY_PROPS_001_Blue_fish_crate/001_STAGE01_NRY_PROPS_001_Blue_fish_crate.prefab";
        Object expectedAsset = AssetDatabase.LoadMainAssetAtPath(prefabPath);

        Assert.That(NoryangjinMapToolWindow.SelectProjectAsset(prefabPath), Is.True);
        Assert.That(Selection.activeObject, Is.SameAs(expectedAsset));
    }

    [Test]
    public void OpenProjectAsset_SelectsOriginalPrefabBeforeOpening()
    {
        const string prefabPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/001_STAGE01_NRY_PROPS_001_Blue_fish_crate/001_STAGE01_NRY_PROPS_001_Blue_fish_crate.prefab";

        Assert.That(NoryangjinMapToolWindow.OpenProjectAsset(prefabPath), Is.True);
        Assert.That(AssetDatabase.GetAssetPath(Selection.activeObject), Is.EqualTo(prefabPath));
    }

    [Test]
    public void NormalizePaletteDisplayName_TrimsAndLimitsToEightCharacters()
    {
        Assert.That(NoryangjinMapToolWindow.NormalizePaletteDisplayName("  긴 이름 테스트  "), Is.EqualTo("긴 이름 테스트"));
        Assert.That(NoryangjinMapToolWindow.NormalizePaletteDisplayName("  긴 이름 테스트 초과  "), Is.EqualTo("긴 이름 테스트"));
        Assert.That(NoryangjinMapToolWindow.NormalizePaletteDisplayName(""), Is.Empty);
    }

    [Test]
    public void BuildPalettePlacementPosition_AddsPerItemHeightOffset()
    {
        Vector3 position = NoryangjinMapToolWindow.BuildPalettePlacementPosition(
            new Vector3(1f, 0f, 2f),
            2,
            -1,
            4f,
            0.5f,
            1.25f);

        Assert.That(position, Is.EqualTo(new Vector3(9f, 1.75f, -2f)));
    }

    [Test]
    public void BuildPalettePlacementPosition_AddsPerItemXZOffset()
    {
        Vector3 position = NoryangjinMapToolWindow.BuildPalettePlacementPosition(
            new Vector3(1f, 0f, 2f),
            2,
            -1,
            4f,
            0.5f,
            1.25f,
            new Vector2(0.25f, -0.5f));

        Assert.That(position, Is.EqualTo(new Vector3(9.25f, 1.75f, -2.5f)));
    }

    [Test]
    public void BuildPalettePlacementPosition_AddsPerItemXYZOffset()
    {
        Vector3 position = NoryangjinMapToolWindow.BuildPalettePlacementPosition(
            new Vector3(1f, 0f, 2f),
            2,
            -1,
            4f,
            0.5f,
            1.25f,
            new Vector3(0.25f, 0.75f, -0.5f));

        Assert.That(position, Is.EqualTo(new Vector3(9.25f, 2.5f, -2.5f)));
    }

    [Test]
    public void CalculatePlacedObjectPositionOffset_UsesPlacedObjectGridAnchor()
    {
        Vector2 offset = NoryangjinMapToolWindow.CalculatePlacedObjectPositionOffset(
            new Vector3(9.25f, 1.75f, -2.5f),
            new Vector3(1f, 0f, 2f),
            new Vector2Int(2, -1),
            4f);

        Assert.That(offset.x, Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(offset.y, Is.EqualTo(-0.5f).Within(0.001f));
    }

    [Test]
    public void CalculatePlacedObjectPositionOffset_UsesPlacementHeightForYOffset()
    {
        Vector3 offset = NoryangjinMapToolWindow.CalculatePlacedObjectPositionOffset(
            new Vector3(9.25f, 1.75f, -2.5f),
            new Vector3(1f, 0f, 2f),
            new Vector2Int(2, -1),
            4f,
            0.5f);

        Assert.That(offset.x, Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(offset.y, Is.EqualTo(1.25f).Within(0.001f));
        Assert.That(offset.z, Is.EqualTo(-0.5f).Within(0.001f));
    }

    [Test]
    public void BuildPlacedObjectPositionWithOffset_KeepsCurrentHeight()
    {
        Vector3 position = NoryangjinMapToolWindow.BuildPlacedObjectPositionWithOffset(
            new Vector3(1f, 0f, 2f),
            new Vector2Int(2, -1),
            4f,
            1.75f,
            new Vector2(0.25f, -0.5f));

        Assert.That(position, Is.EqualTo(new Vector3(9.25f, 1.75f, -2.5f)));
    }

    [Test]
    public void BuildPlacedObjectPositionWithOffset_AddsYOffset()
    {
        Vector3 position = NoryangjinMapToolWindow.BuildPlacedObjectPositionWithOffset(
            new Vector3(1f, 0f, 2f),
            new Vector2Int(2, -1),
            4f,
            0.5f,
            new Vector3(0.25f, 1.25f, -0.5f));

        Assert.That(position, Is.EqualTo(new Vector3(9.25f, 1.75f, -2.5f)));
    }

    [Test]
    public void CopyPlacedObjectTransformToPaletteEntry_SavesReusablePrefabDefaults()
    {
        NoryangjinMapToolPalettePlacementEntry entry = NoryangjinMapToolPalettePlacementEntry.CreateDefault("Assets/Test.prefab");

        NoryangjinMapToolWindow.CopyPlacedObjectTransformToPaletteEntry(
            entry,
            new Vector3(9.25f, 1.75f, -2.5f),
            Quaternion.Euler(0f, 45f, 0f),
            new Vector3(3f, 2f, 1f),
            Quaternion.identity,
            new Vector3(2f, 1f, 0.5f),
            new Vector3(1f, 0f, 2f),
            new Vector2Int(2, -1),
            4f,
            0.5f);

        Assert.That(entry.positionOffset.x, Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(entry.positionOffset.y, Is.EqualTo(-0.5f).Within(0.001f));
        Assert.That(entry.heightOffset, Is.EqualTo(1.25f).Within(0.001f));
        Assert.That(entry.yawOffset, Is.EqualTo(45f).Within(0.001f));
        Assert.That(entry.scale, Is.EqualTo(new Vector3(1.5f, 2f, 2f)));
    }

    [Test]
    public void BuildPalettePlacementRotation_UsesPrefabBaseAndPerItemYawOnly()
    {
        Quaternion rotation = NoryangjinMapToolWindow.BuildPalettePlacementRotation(
            Quaternion.identity,
            45f);

        Assert.That(rotation.eulerAngles.y, Is.EqualTo(45f).Within(0.001f));
    }

    [Test]
    public void BuildPalettePlacementRotation_PreservesPrefabBaseRotation()
    {
        Quaternion prefabRotation = Quaternion.Euler(270f, 0f, 0f);

        Quaternion rotation = NoryangjinMapToolWindow.BuildPalettePlacementRotation(
            prefabRotation,
            0f);

        Assert.That(rotation.eulerAngles.x, Is.EqualTo(270f).Within(0.01f));
    }

    [Test]
    public void BuildPalettePlacementScale_MultipliesPrefabBaseScale()
    {
        Vector3 scale = NoryangjinMapToolWindow.BuildPalettePlacementScale(
            new Vector3(100f, 100f, 100f),
            new Vector3(0.5f, 1f, 2f));

        Assert.That(scale, Is.EqualTo(new Vector3(50f, 100f, 200f)));
    }

    [Test]
    public void CalculateFootprintSize_CeilsWorldBoundsToGridCells()
    {
        Vector2Int footprint = NoryangjinMapToolWindow.CalculateFootprintSize(
            new Vector3(4.6f, 2f, 3.1f),
            1.125f);

        Assert.That(footprint, Is.EqualTo(new Vector2Int(5, 3)));
    }

    [Test]
    public void BuildAnchoredFootprintCells_UsesAnchorCellAndFootprintSize()
    {
        List<Vector2Int> cells = NoryangjinMapToolWindow.BuildAnchoredFootprintCells(
            new Vector2Int(2, -1),
            new Vector2Int(3, 2));

        Assert.That(cells, Is.EqualTo(new[]
        {
            new Vector2Int(2, -1),
            new Vector2Int(3, -1),
            new Vector2Int(4, -1),
            new Vector2Int(2, 0),
            new Vector2Int(3, 0),
            new Vector2Int(4, 0)
        }));
    }

    [Test]
    public void BuildDisplayedFootprintCells_UsesTheFootprintShownInPaletteBadge()
    {
        List<Vector2Int> cells = NoryangjinMapToolWindow.BuildDisplayedFootprintCells(
            new Vector2Int(6, -2),
            new Vector2Int(1, 2));

        Assert.That(cells, Is.EqualTo(new[]
        {
            new Vector2Int(6, -2),
            new Vector2Int(6, -1)
        }));
    }

    [Test]
    public void BuildBoundsFootprintCells_UsesOnlyCellsOverlappedByActualBounds()
    {
        var bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(0.9f, 1f, 0.9f));

        List<Vector2Int> cells = NoryangjinMapToolWindow.BuildBoundsFootprintCells(
            bounds,
            Vector3.zero,
            1f);

        Assert.That(cells, Is.EqualTo(new[] { Vector2Int.zero }));
    }

    [Test]
    public void BuildBoundsFootprintCells_RespectsBoundsOffsetFromAnchor()
    {
        var bounds = new Bounds(new Vector3(1.2f, 0f, 0f), new Vector3(0.8f, 1f, 0.8f));

        List<Vector2Int> cells = NoryangjinMapToolWindow.BuildBoundsFootprintCells(
            bounds,
            Vector3.zero,
            1f);

        Assert.That(cells, Is.EqualTo(new[] { new Vector2Int(1, 0) }));
    }

    [Test]
    public void CanPlaceFootprint_ReturnsFalseWhenAnyCellIsOccupied()
    {
        var occupiedCells = new HashSet<Vector2Int>
        {
            new Vector2Int(4, 0)
        };

        bool canPlace = NoryangjinMapToolWindow.CanPlaceFootprint(
            new Vector2Int(2, -1),
            new Vector2Int(3, 2),
            occupiedCells);

        Assert.That(canPlace, Is.False);
    }

    [Test]
    public void CanPlaceFootprintLayer_AllowsObjectsOnRoadButBlocksRoadOnRoad()
    {
        var occupiedCells = new HashSet<NoryangjinMapToolOccupiedCell>
        {
            new(new Vector2Int(0, 0), NoryangjinMapToolPlacementLayer.Road)
        };
        var footprintCells = new[] { new Vector2Int(0, 0) };

        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                footprintCells,
                NoryangjinMapToolPlacementLayer.Object,
                occupiedCells),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                footprintCells,
                NoryangjinMapToolPlacementLayer.Road,
                occupiedCells),
            Is.False);
    }

    [Test]
    public void ResolveFootprintPreviewState_ColorsEntireFootprintRedWhenAnyCellBlocksPlacement()
    {
        var occupiedCells = new HashSet<NoryangjinMapToolOccupiedCell>
        {
            new(new Vector2Int(1, 0), NoryangjinMapToolPlacementLayer.Object)
        };
        var footprintCells = new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0)
        };

        Assert.That(
            NoryangjinMapToolWindow.ResolveFootprintPreviewState(
                footprintCells,
                NoryangjinMapToolPlacementLayer.Object,
                occupiedCells),
            Is.EqualTo(NoryangjinMapToolSceneGridCellState.Occupied));
    }

    [Test]
    public void ResolveFootprintPreviewState_ColorsEntireFootprintGreenWhenPlacementIsAllowed()
    {
        var occupiedCells = new HashSet<NoryangjinMapToolOccupiedCell>
        {
            new(new Vector2Int(1, 0), NoryangjinMapToolPlacementLayer.Road)
        };
        var footprintCells = new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0)
        };

        Assert.That(
            NoryangjinMapToolWindow.ResolveFootprintPreviewState(
                footprintCells,
                NoryangjinMapToolPlacementLayer.Object,
                occupiedCells),
            Is.EqualTo(NoryangjinMapToolSceneGridCellState.Empty));
    }

    [Test]
    public void GetSelectionPriority_PrefersObjectsOverRoads()
    {
        Assert.That(
            NoryangjinMapToolWindow.GetSelectionPriority(NoryangjinMapToolPlacementLayer.Object),
            Is.LessThan(NoryangjinMapToolWindow.GetSelectionPriority(NoryangjinMapToolPlacementLayer.Road)));
    }

    [Test]
    public void CanPlaceEmptyPaletteItem_IgnoresOccupiedCells()
    {
        var occupiedCells = new HashSet<Vector2Int>
        {
            new Vector2Int(2, -1)
        };

        Assert.That(NoryangjinMapToolWindow.CanPlaceEmptyPaletteItem(new Vector2Int(2, -1), occupiedCells), Is.True);
    }

    [Test]
    public void BuildFootprintLabel_FormatsAsGridSize()
    {
        Assert.That(NoryangjinMapToolWindow.BuildFootprintLabel(new Vector2Int(4, 4)), Is.EqualTo("4x4"));
    }

    [Test]
    public void NormalizeManualFootprint_ClampsEachAxisToAtLeastOne()
    {
        Assert.That(NoryangjinMapToolWindow.NormalizeManualFootprint(new Vector2Int(0, -2)), Is.EqualTo(Vector2Int.one));
        Assert.That(NoryangjinMapToolWindow.NormalizeManualFootprint(new Vector2Int(3, 2)), Is.EqualTo(new Vector2Int(3, 2)));
    }

    [Test]
    public void ResolvePaletteFootprint_UsesManualOverrideWhenEnabled()
    {
        NoryangjinMapToolPalettePlacementEntry entry = NoryangjinMapToolPalettePlacementEntry.CreateDefault("Assets/Test.prefab");
        entry.useManualFootprint = true;
        entry.manualFootprint = new Vector2Int(1, 2);

        Assert.That(
            NoryangjinMapToolWindow.ResolvePaletteFootprint(new Vector2Int(4, 3), entry),
            Is.EqualTo(new Vector2Int(1, 2)));
    }

    [Test]
    public void ShouldEditPaletteFootprintBadge_RequiresLeftDoubleClick()
    {
        Assert.That(NoryangjinMapToolWindow.ShouldEditPaletteFootprintBadge(2, 0), Is.True);
        Assert.That(NoryangjinMapToolWindow.ShouldEditPaletteFootprintBadge(1, 0), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldEditPaletteFootprintBadge(2, 1), Is.False);
    }

    [Test]
    public void PalettePlacementDefaultEntry_UsesSafeFallbackValues()
    {
        NoryangjinMapToolPalettePlacementEntry entry = NoryangjinMapToolPalettePlacementEntry.CreateDefault("Assets/Test.prefab");

        Assert.That(entry.prefabPath, Is.EqualTo("Assets/Test.prefab"));
        Assert.That(entry.scale, Is.EqualTo(Vector3.one));
        Assert.That(entry.positionOffset, Is.EqualTo(Vector2.zero));
        Assert.That(entry.yawOffset, Is.EqualTo(0f));
        Assert.That(entry.heightOffset, Is.EqualTo(0f));
        Assert.That(entry.useManualFootprint, Is.False);
        Assert.That(entry.manualFootprint, Is.EqualTo(Vector2Int.one));
    }

    private static bool ContainsEnglishLetter(string value)
    {
        foreach (char character in value)
        {
            if (character is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                return true;
        }

        return false;
    }

    private static Bounds CalculateRendererBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty, target.name);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}
