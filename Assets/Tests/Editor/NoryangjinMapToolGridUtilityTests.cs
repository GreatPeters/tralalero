using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Utils;

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
    public void ContinuationDirectionPopup_UsesCardinalKoreanOptionsAndExpectedActionLabel()
    {
        Assert.That(NoryangjinMapToolWindow.ContinuationDirectionLabels, Is.EqualTo(new[] { "북", "동", "남", "서" }));
        Assert.That(NoryangjinMapToolWindow.ContinuationButtonLabel, Is.EqualTo("이어 복붙"));
    }

    [Test]
    public void CopyPasteMode_UsesExplicitKoreanActionLabels()
    {
        Assert.That(NoryangjinMapToolWindow.CopyPlacedObjectButtonLabel, Is.EqualTo("복사하기"));
        Assert.That(NoryangjinMapToolWindow.PasteCopiedObjectModeLabel, Is.EqualTo("붙여넣기 중"));
    }

    [Test]
    public void CopiedObjectPastePosition_PreservesTileOffsetAndHeight()
    {
        Vector3 sourcePosition = new Vector3(2.93f, -2f, -1.24f);

        Vector3 pastedPosition = NoryangjinMapToolWindow.BuildCopiedObjectPastePosition(
            sourcePosition,
            new Vector2Int(12, -5),
            new Vector2Int(20, 3),
            0.225f);

        Assert.That(pastedPosition.x, Is.EqualTo(4.73f).Within(0.001f));
        Assert.That(pastedPosition.y, Is.EqualTo(-2f).Within(0.001f));
        Assert.That(pastedPosition.z, Is.EqualTo(0.56f).Within(0.001f));
    }

    [Test]
    public void CopiedObjectFootprintCells_TranslateToDestinationAnchor()
    {
        var sourceCells = new List<Vector2Int>
        {
            new(4, 7),
            new(5, 7),
            new(4, 8)
        };

        IReadOnlyList<Vector2Int> pastedCells = NoryangjinMapToolWindow.TranslateCopiedObjectFootprintCells(
            sourceCells,
            new Vector2Int(4, 7),
            new Vector2Int(-2, 3));

        Assert.That(
            pastedCells,
            Is.EqualTo(new[]
            {
                new Vector2Int(-2, 3),
                new Vector2Int(-1, 3),
                new Vector2Int(-2, 4)
            }));
    }

    [Test]
    public void RoadContinuationGridOffset_UsesManualFootprintRelativeToPrefabYaw()
    {
        var nativeExpected = new Dictionary<NoryangjinMapToolDirection, Vector2Int>
        {
            [NoryangjinMapToolDirection.North] = new Vector2Int(0, 30),
            [NoryangjinMapToolDirection.East] = new Vector2Int(50, 0),
            [NoryangjinMapToolDirection.South] = new Vector2Int(0, -30),
            [NoryangjinMapToolDirection.West] = new Vector2Int(-50, 0)
        };
        var quarterTurnExpected = new Dictionary<NoryangjinMapToolDirection, Vector2Int>
        {
            [NoryangjinMapToolDirection.North] = new Vector2Int(0, 50),
            [NoryangjinMapToolDirection.East] = new Vector2Int(30, 0),
            [NoryangjinMapToolDirection.South] = new Vector2Int(0, -50),
            [NoryangjinMapToolDirection.West] = new Vector2Int(-30, 0)
        };

        foreach (NoryangjinMapToolDirection direction in System.Enum.GetValues(typeof(NoryangjinMapToolDirection)))
        {
            Assert.That(
                NoryangjinMapToolWindow.BuildRoadContinuationGridOffset(
                    new Vector2Int(10, 6),
                    Quaternion.Euler(0f, 90f, 0f),
                    Quaternion.Euler(0f, 90f, 0f),
                    direction),
                Is.EqualTo(nativeExpected[direction]),
                $"native {direction}");
            Assert.That(
                NoryangjinMapToolWindow.BuildRoadContinuationGridOffset(
                    new Vector2Int(10, 6),
                    Quaternion.Euler(0f, 90f, 0f),
                    Quaternion.Euler(0f, 180f, 0f),
                    direction),
                Is.EqualTo(quarterTurnExpected[direction]),
                $"quarter turn {direction}");
        }
    }

    [Test]
    public void BoundsContinuationOffset_UsesEdgeContactBackgroundOverlapAndFineCellFallback()
    {
        Bounds sourceBounds = new Bounds(Vector3.zero, new Vector3(10f, 2f, 6f));
        Bounds duplicateBounds = sourceBounds;
        var propExpected = new Dictionary<NoryangjinMapToolDirection, Vector3>
        {
            [NoryangjinMapToolDirection.North] = new Vector3(0f, 0f, 6f),
            [NoryangjinMapToolDirection.East] = new Vector3(10f, 0f, 0f),
            [NoryangjinMapToolDirection.South] = new Vector3(0f, 0f, -6f),
            [NoryangjinMapToolDirection.West] = new Vector3(-10f, 0f, 0f)
        };
        var backgroundExpected = new Dictionary<NoryangjinMapToolDirection, Vector3>
        {
            [NoryangjinMapToolDirection.North] = new Vector3(0f, 0f, 5.775f),
            [NoryangjinMapToolDirection.East] = new Vector3(9.775f, 0f, 0f),
            [NoryangjinMapToolDirection.South] = new Vector3(0f, 0f, -5.775f),
            [NoryangjinMapToolDirection.West] = new Vector3(-9.775f, 0f, 0f)
        };

        foreach (NoryangjinMapToolDirection direction in System.Enum.GetValues(typeof(NoryangjinMapToolDirection)))
        {
            Assert.That(
                NoryangjinMapToolWindow.BuildBoundsContinuationOffset(
                    sourceBounds,
                    duplicateBounds,
                    direction,
                    0.225f,
                    0f),
                Is.EqualTo(propExpected[direction]).Using(Vector3ComparerWithEqualsOperator.Instance),
                $"prop {direction}");
            Assert.That(
                NoryangjinMapToolWindow.BuildBoundsContinuationOffset(
                    sourceBounds,
                    duplicateBounds,
                    direction,
                    0.225f,
                    0.225f),
                Is.EqualTo(backgroundExpected[direction]).Using(Vector3ComparerWithEqualsOperator.Instance),
                $"background {direction}");
        }

        Vector3 fallbackOffset = NoryangjinMapToolWindow.BuildBoundsContinuationOffset(
            new Bounds(Vector3.zero, Vector3.zero),
            new Bounds(Vector3.zero, Vector3.zero),
            NoryangjinMapToolDirection.South,
            0.225f,
            0f);

        Assert.That(fallbackOffset.x, Is.Zero.Within(0.001f));
        Assert.That(fallbackOffset.z, Is.EqualTo(-0.225f).Within(0.001f));
    }

    [Test]
    public void ContinuationObjectName_ReplacesCoordinateSuffixOrAddsOneToRenamedRoots()
    {
        Assert.That(
            NoryangjinMapToolWindow.BuildContinuationObjectName("Road_RightTurn_X+12_Z-03", new Vector2Int(62, -3)),
            Is.EqualTo("Road_RightTurn_X+62_Z-03"));
        Assert.That(
            NoryangjinMapToolWindow.BuildContinuationObjectName("수동 배치 부두", new Vector2Int(-4, 8)),
            Is.EqualTo("수동 배치 부두_X-04_Z+08"));
    }

    [Test]
    public void ContinuationAvailability_RequiresEnabledToolAndRejectsSingletonWaterBackdrop()
    {
        GameObject source = new GameObject("Prop_Crate_X+00_Z+00");
        GameObject water = new GameObject("Background_Water");
        try
        {
            Assert.That(NoryangjinMapToolWindow.CanContinuePlacedObject(true, source), Is.True);
            Assert.That(NoryangjinMapToolWindow.CanContinuePlacedObject(false, source), Is.False);
            Assert.That(NoryangjinMapToolWindow.CanContinuePlacedObject(true, water), Is.False);
            Assert.That(NoryangjinMapToolWindow.CanContinuePlacedObject(true, null), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(water);
        }
    }

    [Test]
    public void ResolveContinuationSource_PrefersSelectedPlacementRootThenFallsBackToLastPlaced()
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        Object previousSelection = Selection.activeObject;
        NoryangjinMapToolWindow window = null;
        try
        {
            GameObject selectedRoot = new GameObject("Prop_Selected_X+00_Z+00");
            SceneManager.MoveGameObjectToScene(selectedRoot, previewScene);
            GameObject selectedChild = new GameObject("SelectedChild");
            SceneManager.MoveGameObjectToScene(selectedChild, previewScene);
            selectedChild.transform.SetParent(selectedRoot.transform, false);

            GameObject lastPlaced = new GameObject("Prop_Last_X+01_Z+00");
            SceneManager.MoveGameObjectToScene(lastPlaced, previewScene);
            GameObject unrelated = new GameObject("Unrelated");
            SceneManager.MoveGameObjectToScene(unrelated, previewScene);

            window = ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();
            FieldInfo lastPlacedField = typeof(NoryangjinMapToolWindow).GetField(
                "lastPlacedObjectInstanceId",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lastPlacedField, Is.Not.Null);
            lastPlacedField.SetValue(window, lastPlaced.GetInstanceID());

            MethodInfo resolveMethod = typeof(NoryangjinMapToolWindow).GetMethod(
                "ResolveContinuationSource",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(resolveMethod, Is.Not.Null);

            Selection.activeGameObject = selectedChild;
            Assert.That(resolveMethod.Invoke(window, null), Is.SameAs(selectedRoot));

            Selection.activeGameObject = unrelated;
            Assert.That(resolveMethod.Invoke(window, null), Is.SameAs(lastPlaced));

            Selection.activeObject = null;
            Assert.That(resolveMethod.Invoke(window, null), Is.SameAs(lastPlaced));
        }
        finally
        {
            Selection.activeObject = previousSelection;
            if (window != null)
                Object.DestroyImmediate(window);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void SelectedFootprintHighlight_UsesOnlySelectedPlacementRoot()
    {
        GameObject selectedRoot = new GameObject("Prop_Selected_X+03_Z-02");
        GameObject selectedChild = new GameObject("SelectedChild");
        GameObject unrelated = new GameObject("Unrelated");
        try
        {
            selectedChild.transform.SetParent(selectedRoot.transform, false);

            Assert.That(
                NoryangjinMapToolWindow.ResolveSelectedFootprintTarget(selectedChild),
                Is.SameAs(selectedRoot));
            Assert.That(
                NoryangjinMapToolWindow.ResolveSelectedFootprintTarget(unrelated),
                Is.Null);
            Assert.That(
                NoryangjinMapToolWindow.ResolveSelectedFootprintTarget(null),
                Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(selectedRoot);
            Object.DestroyImmediate(unrelated);
        }
    }

    [Test]
    public void KnownDetachedRoadName_RecoversExactPalettePrefabOnly()
    {
        Assert.That(
            NoryangjinMapToolWindow.TryGetKnownRoadPrefabPathFromPlacedObjectName(
                "Road_RightTurn_X+12_Z-03",
                out string prefabPath),
            Is.True);
        Assert.That(prefabPath, Does.EndWith("Pier_Long_Fantasy_RightTurn.prefab"));
        Assert.That(
            NoryangjinMapToolWindow.TryGetKnownRoadPrefabPathFromPlacedObjectName(
                "Road_RightTurnCustom_X+12_Z-03",
                out _),
            Is.False);
    }

    [Test]
    public void DuplicatePlacedObjectForContinuation_PreservesPrefabConnectionOverridesAndAddedChildren()
    {
        const string prefabPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy.prefab";
        Undo.IncrementCurrentGroup();
        int testUndoGroup = Undo.GetCurrentGroup();
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        Object previousSelection = Selection.activeObject;
        try
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);

            GameObject source = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            Assert.That(source, Is.Not.Null);
            source.name = "Road_Basic_X+12_Z+00";
            source.transform.position = new Vector3(2.7f, 0.4f, -1.2f);
            source.transform.localScale = new Vector3(2.1f, 2.2f, 2.3f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(source.transform);

            GameObject addedChild = new GameObject("ContinuationAddedChild");
            SceneManager.MoveGameObjectToScene(addedChild, previewScene);
            addedChild.transform.SetParent(source.transform, false);

            GameObject duplicate = NoryangjinMapToolWindow.DuplicatePlacedObjectForContinuation(source);

            Assert.That(duplicate, Is.Not.Null);
            Assert.That(duplicate, Is.Not.SameAs(source));
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(duplicate), Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(duplicate), Is.EqualTo(prefabPath));
            Assert.That(duplicate.transform.Find("ContinuationAddedChild"), Is.Not.Null);
            Assert.That(duplicate.transform.position, Is.EqualTo(source.transform.position));
            Assert.That(duplicate.transform.localScale, Is.EqualTo(source.transform.localScale));
        }
        finally
        {
            Undo.RevertAllDownToGroup(testUndoGroup);
            Selection.activeObject = previousSelection;
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void PasteCopiedPlacedObject_PreservesInstancePropertiesAtTargetTile()
    {
        const string prefabPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy.prefab";
        Undo.IncrementCurrentGroup();
        int testUndoGroup = Undo.GetCurrentGroup();
        Scene activeSceneBefore = SceneManager.GetActiveScene();
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        Object previousSelection = Selection.activeObject;
        NoryangjinMapToolWindow window = null;
        try
        {
            GameObject root = new GameObject("Noryangjin_MapTool");
            SceneManager.MoveGameObjectToScene(root, previewScene);
            GameObject roads = new GameObject("Roads");
            SceneManager.MoveGameObjectToScene(roads, previewScene);
            roads.transform.SetParent(root.transform, false);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject source = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            Assert.That(source, Is.Not.Null);
            source.transform.SetParent(roads.transform, true);
            source.name = "Road_Basic_X+12_Z-05";
            source.transform.position = new Vector3(2.93f, -2f, -1.24f);
            source.transform.rotation = Quaternion.Euler(7f, 123f, 11f);
            source.transform.localScale = new Vector3(1.2f, 0.8f, 1.6f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(source.transform);

            GameObject addedChild = new GameObject("CopiedAddedChild");
            SceneManager.MoveGameObjectToScene(addedChild, previewScene);
            addedChild.transform.SetParent(source.transform, false);

            window = ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();
            GameObject pasted = window.PasteCopiedPlacedObject(source, new Vector2Int(20, 3));

            Assert.That(pasted, Is.Not.Null);
            Assert.That(pasted.name, Is.EqualTo("Road_Basic_X+20_Z+03"));
            Assert.That(pasted.transform.position, Is.EqualTo(new Vector3(4.73f, -2f, 0.56f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(pasted.transform.rotation, Is.EqualTo(source.transform.rotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(pasted.transform.localScale, Is.EqualTo(source.transform.localScale).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(pasted.transform.Find("CopiedAddedChild"), Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(pasted), Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pasted), Is.EqualTo(prefabPath));
            Assert.That(pasted.transform.parent, Is.SameAs(roads.transform));
            Assert.That(pasted.scene, Is.EqualTo(previewScene));
            Assert.That(Selection.activeGameObject, Is.SameAs(pasted));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeSceneBefore));
        }
        finally
        {
            Undo.RevertAllDownToGroup(testUndoGroup);
            Selection.activeObject = previousSelection;
            if (window != null)
                Object.DestroyImmediate(window);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void ContinuePlacedObject_ChainsConnectedRightTurnCopiesUsingManualFootprint()
    {
        const string prefabPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy_RightTurn.prefab";
        Undo.IncrementCurrentGroup();
        int testUndoGroup = Undo.GetCurrentGroup();
        Scene activeSceneBefore = SceneManager.GetActiveScene();
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        Object previousSelection = Selection.activeObject;
        NoryangjinMapToolWindow window = null;
        try
        {
            GameObject root = new GameObject("Noryangjin_MapTool");
            SceneManager.MoveGameObjectToScene(root, previewScene);
            GameObject roads = new GameObject("Roads");
            SceneManager.MoveGameObjectToScene(roads, previewScene);
            roads.transform.SetParent(root.transform, false);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject source = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            Assert.That(source, Is.Not.Null);
            source.transform.SetParent(roads.transform, true);
            source.name = "Road_RightTurn_X+12_Z+00";
            source.transform.position = new Vector3(12f * 0.225f, 0f, 0f);

            window = ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();
            GameObject first = window.ContinuePlacedObject(source, NoryangjinMapToolDirection.East);
            GameObject second = window.ContinuePlacedObject(first, NoryangjinMapToolDirection.East);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first.name, Is.EqualTo("Road_RightTurn_X+62_Z+00"));
            Assert.That(second.name, Is.EqualTo("Road_RightTurn_X+112_Z+00"));
            Assert.That(first.transform.position.x - source.transform.position.x, Is.EqualTo(11.25f).Within(0.001f));
            Assert.That(second.transform.position.x - first.transform.position.x, Is.EqualTo(11.25f).Within(0.001f));
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(first), Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(second), Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(second), Is.EqualTo(prefabPath));
            Assert.That(Selection.activeGameObject, Is.SameAs(second));
            Assert.That(first.scene, Is.EqualTo(previewScene));
            Assert.That(second.scene, Is.EqualTo(previewScene));
            Assert.That(first.transform.parent, Is.SameAs(roads.transform));
            Assert.That(second.transform.parent, Is.SameAs(roads.transform));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeSceneBefore));

            var serializedWindow = new SerializedObject(window);
            serializedWindow.Update();
            Assert.That(serializedWindow.FindProperty("gridX").intValue, Is.EqualTo(112));
            Assert.That(serializedWindow.FindProperty("gridZ").intValue, Is.Zero);

            string secondName = second.name;
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            serializedWindow.Update();
            Assert.That(roads.transform.Find(secondName), Is.Null);
            Assert.That(serializedWindow.FindProperty("gridX").intValue, Is.EqualTo(62));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeSceneBefore));

            Undo.PerformRedo();
            serializedWindow.Update();
            GameObject redoneSecond = roads.transform.Find(secondName)?.gameObject;
            Assert.That(redoneSecond, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(redoneSecond), Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeSceneBefore));
        }
        finally
        {
            Undo.RevertAllDownToGroup(testUndoGroup);
            Selection.activeObject = previousSelection;
            if (window != null)
                Object.DestroyImmediate(window);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
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
    public void KnownRoadPrefabs_UseFantasyBasicBridgeUphillDownhillAndTurnRoadSet()
    {
        Dictionary<string, string> roadPaths = GetKnownRoadPiecePathsByLabel();

        Assert.That(roadPaths, Has.Count.EqualTo(6));
        Assert.That(
            roadPaths["Basic"],
            Is.EqualTo("Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy.prefab"));
        Assert.That(
            roadPaths["LeftTurn"],
            Is.EqualTo("Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy_LeftTurn.prefab"));
        Assert.That(
            roadPaths["RightTurn"],
            Is.EqualTo("Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy_RightTurn.prefab"));
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
    public void SlopeHeightLabels_MarkUphillStartAsHigh()
    {
        var footprintCells = new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, 5)
        };

        bool hasLabels = NoryangjinMapToolWindow.TryBuildSlopeHeightLabelCells(
            "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Rope_Stairs_Fantasy.prefab",
            footprintCells,
            NoryangjinMapToolDirection.North,
            out Vector2Int highCell,
            out Vector2Int lowCell);

        Assert.That(hasLabels, Is.True);
        Assert.That(highCell, Is.EqualTo(new Vector2Int(0, 0)));
        Assert.That(lowCell, Is.EqualTo(new Vector2Int(0, 5)));
    }

    [Test]
    public void SlopeHeightLabels_MarkDownhillEndAsHigh()
    {
        var footprintCells = new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, 5)
        };

        bool hasLabels = NoryangjinMapToolWindow.TryBuildSlopeHeightLabelCells(
            "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Rope_Stairs_Fantasy_Downhill.prefab",
            footprintCells,
            NoryangjinMapToolDirection.North,
            out Vector2Int highCell,
            out Vector2Int lowCell);

        Assert.That(hasLabels, Is.True);
        Assert.That(highCell, Is.EqualTo(new Vector2Int(0, 5)));
        Assert.That(lowCell, Is.EqualTo(new Vector2Int(0, 0)));
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
    public void ConfiguredObstaclePrefabs_AppearOnceInGimmickPalette()
    {
        ObstaclePrefabs obstacleConfig =
            AssetDatabase.LoadAssetAtPath<ObstaclePrefabs>(
                NoryangjinMapToolWindow.ObstaclePaletteConfigPath);
        Assert.That(obstacleConfig, Is.Not.Null);
        Assert.That(obstacleConfig.obstaclePrefabs, Is.Not.Null);

        var expectedPaths = new List<string>();
        foreach (ObstacleTypePrefab entry in obstacleConfig.obstaclePrefabs)
        {
            if (entry == null ||
                entry.pattern == ObstaclePattern.None ||
                entry.prefab == null)
            {
                continue;
            }

            string path = AssetDatabase.GetAssetPath(entry.prefab);
            if (!string.IsNullOrEmpty(path) && !expectedPaths.Contains(path))
                expectedPaths.Add(path);
        }
        Assert.That(expectedPaths, Is.Not.Empty);

        MethodInfo getPaletteItems = typeof(NoryangjinMapToolWindow).GetMethod(
            "GetPaletteItems",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(getPaletteItems, Is.Not.Null);
        var counts = new Dictionary<string, int>();
        var sections = new Dictionary<string, NoryangjinMapToolPaletteSection>();
        var labels = new Dictionary<string, string>();
        NoryangjinMapToolWindow window =
            ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();
        try
        {
            foreach (object paletteItem in (IEnumerable)getPaletteItems.Invoke(window, null))
            {
                System.Type itemType = paletteItem.GetType();
                string prefabPath =
                    (string)itemType.GetProperty("PrefabPath").GetValue(paletteItem);
                if (!expectedPaths.Contains(prefabPath))
                    continue;

                counts[prefabPath] = counts.TryGetValue(prefabPath, out int count)
                    ? count + 1
                    : 1;
                sections[prefabPath] =
                    (NoryangjinMapToolPaletteSection)itemType
                        .GetProperty("Section")
                        .GetValue(paletteItem);
                labels[prefabPath] =
                    (string)itemType.GetProperty("Label").GetValue(paletteItem);
            }
        }
        finally
        {
            Object.DestroyImmediate(window);
        }

        foreach (string expectedPath in expectedPaths)
        {
            Assert.That(counts, Contains.Key(expectedPath), expectedPath);
            Assert.That(counts[expectedPath], Is.EqualTo(1), expectedPath);
            Assert.That(labels[expectedPath], Is.Not.Empty, expectedPath);
            Assert.That(labels[expectedPath].Length, Is.LessThanOrEqualTo(8), expectedPath);
            Assert.That(
                NoryangjinMapToolWindow.IsSelectablePalettePrefabPath(expectedPath),
                Is.True,
                expectedPath);
            Assert.That(
                sections[expectedPath],
                Is.EqualTo(NoryangjinMapToolPaletteSection.Gimmick),
                expectedPath);
        }

        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (string expectedPath in expectedPaths)
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(expectedPath);
                Assert.That(prefab, Is.Not.Null, expectedPath);
                GameObject instance = PrefabUtility.InstantiatePrefab(
                    prefab,
                    previewScene) as GameObject;
                Assert.That(instance, Is.Not.Null, expectedPath);
                Assert.That(
                    NoryangjinMapToolWindow.IsPlacedObjectOwnedByContentTab(
                        instance,
                        NoryangjinMapToolContentTab.Gimmick),
                    Is.True,
                    expectedPath);
                Assert.That(
                    NoryangjinMapToolWindow.IsPlacedObjectOwnedByContentTab(
                        instance,
                        NoryangjinMapToolContentTab.Object),
                    Is.False,
                    expectedPath);
                Object.DestroyImmediate(instance);
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void KnownRoadPrefabs_HaveUsableNonZeroPrefabRootScale()
    {
        foreach (string prefabPath in GetKnownRoadPiecePathsByLabel().Values)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            Assert.That(Mathf.Abs(prefab.transform.localScale.x), Is.GreaterThan(0f), prefabPath);
            Assert.That(Mathf.Abs(prefab.transform.localScale.y), Is.GreaterThan(0f), prefabPath);
            Assert.That(Mathf.Abs(prefab.transform.localScale.z), Is.GreaterThan(0f), prefabPath);
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
        return GetPaletteItemCategoriesByPath(out _);
    }

    private static Dictionary<string, NoryangjinMapToolPaletteCategory> GetPaletteItemCategoriesByPath(out Dictionary<string, int> countsByPath)
    {
        MethodInfo method = typeof(NoryangjinMapToolWindow).GetMethod("GetPaletteItems", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);

        var categories = new Dictionary<string, NoryangjinMapToolPaletteCategory>();
        countsByPath = new Dictionary<string, int>();
        NoryangjinMapToolWindow window = ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();
        try
        {
            foreach (object paletteItem in (IEnumerable)method.Invoke(window, null))
            {
                System.Type paletteItemType = paletteItem.GetType();
                string prefabPath = (string)paletteItemType.GetProperty("PrefabPath").GetValue(paletteItem);
                var category = (NoryangjinMapToolPaletteCategory)paletteItemType.GetProperty("Category").GetValue(paletteItem);
                categories[prefabPath] = category;
                countsByPath[prefabPath] = countsByPath.TryGetValue(prefabPath, out int count) ? count + 1 : 1;
            }
        }
        finally
        {
            Object.DestroyImmediate(window);
        }

        return categories;
    }

    private static Dictionary<string, string> GetPaletteItemLabelsByPath()
    {
        MethodInfo method = typeof(NoryangjinMapToolWindow).GetMethod("GetPaletteItems", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);

        var labels = new Dictionary<string, string>();
        NoryangjinMapToolWindow window = ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();
        try
        {
            foreach (object paletteItem in (IEnumerable)method.Invoke(window, null))
            {
                System.Type paletteItemType = paletteItem.GetType();
                string prefabPath = (string)paletteItemType.GetProperty("PrefabPath").GetValue(paletteItem);
                string label = (string)paletteItemType.GetProperty("Label").GetValue(paletteItem);
                labels[prefabPath] = label;
            }
        }
        finally
        {
            Object.DestroyImmediate(window);
        }

        return labels;
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
    public void MapToolScene_FeastWallsInheritFixedTypographyFromTheirPrefabs()
    {
        string sceneYaml = File.ReadAllText(NoryangjinMapToolWindow.MapToolScenePath);
        string altarGuid = AssetDatabase.AssetPathToGUID(
            NoryangjinMapToolWindow.BonusWallPrefabRoot +
            "/Box_left.prefab");

        Assert.That(altarGuid, Is.Not.Empty);
        Assert.That(
            HasTypographyOverride(sceneYaml, altarGuid),
            Is.False,
            "Placed bonus altars must inherit the prefab's fixed TMP sizing.");
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
        Assert.That(
            NoryangjinMapToolWindow.CategorizePrefabPath(NoryangjinMapToolWindow.JhWaterPrefabPath),
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
        Assert.That(
            NoryangjinMapToolWindow.BuildPaletteLabel(NoryangjinMapToolWindow.JhWaterPrefabPath),
            Is.EqualTo("물"));
    }

    [Test]
    public void JhWaterPrefab_AppearsInBackgroundPaletteCategory()
    {
        Dictionary<string, NoryangjinMapToolPaletteCategory> paletteCategories = GetPaletteItemCategoriesByPath(out Dictionary<string, int> paletteItemCounts);

        Assert.That(paletteCategories, Contains.Key(NoryangjinMapToolWindow.JhWaterPrefabPath));
        Assert.That(paletteCategories[NoryangjinMapToolWindow.JhWaterPrefabPath], Is.EqualTo(NoryangjinMapToolPaletteCategory.Background));
        Assert.That(paletteItemCounts[NoryangjinMapToolWindow.JhWaterPrefabPath], Is.EqualTo(1));
    }

    [Test]
    public void DockMetalCleatPrefab_IsHiddenFromMapToolPalette()
    {
        Dictionary<string, NoryangjinMapToolPaletteCategory> paletteCategories = GetPaletteItemCategoriesByPath();

        Assert.That(paletteCategories, Does.Not.ContainKey(NoryangjinMapToolWindow.DockMetalCleatPrefabPath));
    }

    [Test]
    public void JhWaterPrefab_UsesCustomDisplayNameInPalette()
    {
        NoryangjinMapToolPaletteDefaults defaults = AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(
            "Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset");
        string previousLabel = defaults.GetCustomLabel(NoryangjinMapToolWindow.JhWaterPrefabPath);

        try
        {
            defaults.SetCustomLabel(NoryangjinMapToolWindow.JhWaterPrefabPath, "테스트물");
            Dictionary<string, string> labels = GetPaletteItemLabelsByPath();

            Assert.That(labels[NoryangjinMapToolWindow.JhWaterPrefabPath], Is.EqualTo("테스트물"));
        }
        finally
        {
            defaults.SetCustomLabel(NoryangjinMapToolWindow.JhWaterPrefabPath, previousLabel);
            EditorUtility.SetDirty(defaults);
            AssetDatabase.SaveAssets();
        }
    }

    [Test]
    public void JhWaterPrefab_UsesLowCostBackgroundPlacementPath()
    {
        Assert.That(NoryangjinMapToolWindow.IsLowCostWaterBackgroundPath(NoryangjinMapToolWindow.JhWaterPrefabPath), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsGridManagedPaletteItemPath(NoryangjinMapToolWindow.JhWaterPrefabPath), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldShowPlacementPreview(NoryangjinMapToolWindow.JhWaterPrefabPath), Is.False);
        Assert.That(NoryangjinMapToolWindow.ShouldDrawPlacementValidityFill(NoryangjinMapToolWindow.JhWaterPrefabPath), Is.False);
    }

    [Test]
    public void WaterBackdrop_IsSelectableWithoutGridCoordinateSuffix()
    {
        var waterParent = new GameObject("Water");
        var water = new GameObject("Background_Water");
        var unrelated = new GameObject("Background_Other");

        try
        {
            water.transform.SetParent(waterParent.transform);
            unrelated.transform.SetParent(waterParent.transform);

            Assert.That(NoryangjinMapToolWindow.IsWaterBackdropSelectionTarget(water), Is.True);
            Assert.That(NoryangjinMapToolWindow.IsWaterBackdropSelectionTarget(unrelated), Is.False);
            Assert.That(NoryangjinMapToolWindow.IsWaterBackdropSelectionTarget(null), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(waterParent);
        }
    }

    [Test]
    public void BonusAltar_UsesCustomDisplayNameInBonusPalette()
    {
        string prefabPath =
            NoryangjinMapToolWindow.FeastOfFortuneBonusWallPrefabPaths[0];
        NoryangjinMapToolPaletteDefaults defaults =
            AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(
                "Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset");
        string previousLabel = defaults.GetCustomLabel(prefabPath);

        try
        {
            defaults.SetCustomLabel(prefabPath, "공격 상자");
            Dictionary<string, string> labels = GetPaletteItemLabelsByPath();

            Assert.That(labels[prefabPath], Is.EqualTo("공격 상자"));
        }
        finally
        {
            defaults.SetCustomLabel(prefabPath, previousLabel);
            EditorUtility.SetDirty(defaults);
            AssetDatabase.SaveAssets();
        }
    }

    [Test]
    public void OceanWaterBackdropSelection_UsesRendererBoundsBeyondOneByOneFootprint()
    {
        const float placementCellSize = 0.225f;
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        NoryangjinMapToolWindow window = null;

        try
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                NoryangjinMapToolWindow.NoryangjinOceanWaterBackdropPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            GameObject water = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            Assert.That(water, Is.Not.Null);
            water.transform.position = Vector3.zero;
            NoryangjinMapToolPaletteDefaults defaults = AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(
                "Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset");
            NoryangjinMapToolPalettePlacementEntry placement =
                defaults.GetOrCreateEntry(NoryangjinMapToolWindow.NoryangjinOceanWaterBackdropPrefabPath);
            Assert.That(placement.useManualFootprint, Is.True);
            Assert.That(placement.manualFootprint, Is.EqualTo(Vector2Int.one));
            water.transform.localScale = NoryangjinMapToolWindow.BuildPalettePlacementScale(
                prefab.transform.localScale,
                placement.scale);
            water.transform.rotation = NoryangjinMapToolWindow.BuildPalettePlacementRotation(
                prefab.transform.rotation,
                placement.yawOffset);

            Renderer[] renderers = water.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            Bounds rendererBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                rendererBounds.Encapsulate(renderers[i].bounds);

            List<Vector2Int> expected = NoryangjinMapToolWindow.BuildBoundsFootprintCells(
                rendererBounds,
                Vector3.zero,
                placementCellSize);
            window = ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();

            List<Vector2Int> actual = window.GetPlacedObjectSelectionFootprintCells(
                water,
                Vector2Int.zero,
                placementCellSize);

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Count, Is.GreaterThan(25));
        }
        finally
        {
            if (window != null)
                Object.DestroyImmediate(window);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void NormalBackgroundPrefabs_StillUseGridManagedPlacementPath()
    {
        const string backdropPath = "Assets/017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop.prefab";

        Assert.That(NoryangjinMapToolWindow.IsLowCostWaterBackgroundPath(backdropPath), Is.False);
        Assert.That(NoryangjinMapToolWindow.IsGridManagedPaletteItemPath(backdropPath), Is.True);
        Assert.That(NoryangjinMapToolWindow.ShouldShowPlacementPreview(backdropPath), Is.True);
    }

    [Test]
    public void BackgroundPaletteItems_UseSeparatePlacementLayer()
    {
        const string backdropPath = "Assets/017_STAGE01_NRY_BG_001_Ocean_water_plane_backdrop.prefab";

        Assert.That(
            NoryangjinMapToolWindow.GetPaletteItemLayer(backdropPath, NoryangjinMapToolPaletteCategory.Background),
            Is.EqualTo(NoryangjinMapToolPlacementLayer.Background));
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteItemLayer("Assets/001_STAGE01_NRY_PROPS_001_Blue_fish_crate.prefab", NoryangjinMapToolPaletteCategory.Prop),
            Is.EqualTo(NoryangjinMapToolPlacementLayer.Object));
    }

    [Test]
    public void BackgroundOverlayObjects_UseObjectPlacementLayer()
    {
        const string seaBuoyPath = "Assets/037_STAGE01_NRY_BG_026_Floating_sea_buoy.prefab";
        const string woodenPlankPath = "Assets/038_STAGE01_NRY_BG_027_Floating_wooden_plank.prefab";

        Assert.That(
            NoryangjinMapToolWindow.CategorizePrefabPath(seaBuoyPath),
            Is.EqualTo(NoryangjinMapToolPaletteCategory.Background));
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteItemLayer(seaBuoyPath, NoryangjinMapToolPaletteCategory.Background),
            Is.EqualTo(NoryangjinMapToolPlacementLayer.Object));
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteItemLayer(woodenPlankPath, NoryangjinMapToolPaletteCategory.Background),
            Is.EqualTo(NoryangjinMapToolPlacementLayer.Object));
    }

    [Test]
    public void BackgroundFootprints_BlockOnlyOtherBackgrounds()
    {
        var footprintCells = new[] { Vector2Int.zero };
        var occupiedCells = new HashSet<NoryangjinMapToolOccupiedCell>
        {
            new(Vector2Int.zero, NoryangjinMapToolPlacementLayer.Background)
        };

        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                footprintCells,
                NoryangjinMapToolPlacementLayer.Background,
                occupiedCells),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                footprintCells,
                NoryangjinMapToolPlacementLayer.Object,
                occupiedCells),
            Is.True);
    }

    [Test]
    public void ObjectFootprints_DoNotBlockBackgroundPlacement()
    {
        var footprintCells = new[] { Vector2Int.zero };
        var occupiedCells = new HashSet<NoryangjinMapToolOccupiedCell>
        {
            new(Vector2Int.zero, NoryangjinMapToolPlacementLayer.Object)
        };

        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                footprintCells,
                NoryangjinMapToolPlacementLayer.Background,
                occupiedCells),
            Is.True);
    }

    [Test]
    public void JhWaterPrefab_UsesReducedMapToolDefaultFootprint()
    {
        NoryangjinMapToolPaletteDefaults defaults = AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(
            "Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset");

        NoryangjinMapToolPalettePlacementEntry entry = defaults.GetOrCreateEntry(NoryangjinMapToolWindow.JhWaterPrefabPath);

        Assert.That(entry.scale, Is.EqualTo(new Vector3(0.35f, 0.35f, 0.35f)));
        Assert.That(entry.positionOffset, Is.EqualTo(new Vector2(14.0625f, 14.0625f)));
        Assert.That(entry.heightOffset, Is.EqualTo(-0.2f).Within(0.001f));
        Assert.That(entry.useManualFootprint, Is.True);
        Assert.That(entry.manualFootprint, Is.EqualTo(new Vector2Int(25, 25)));
    }

    [Test]
    public void DistantHillsideVillage_UsesSmallManualMapToolFootprint()
    {
        const string hillsideVillagePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/019_STAGE01_NRY_BG_003_Distant_hillside_village_module/019_STAGE01_NRY_BG_003_Distant_hillside_village_module.prefab";
        NoryangjinMapToolPaletteDefaults defaults = AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(
            "Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset");

        NoryangjinMapToolPalettePlacementEntry entry = defaults.GetOrCreateEntry(hillsideVillagePath);

        Assert.That(entry.scale, Is.EqualTo(new Vector3(0.7f, 0.7f, 0.7f)));
        Assert.That(entry.positionOffset, Is.EqualTo(new Vector2(2.25f, 1.125f)));
        Assert.That(entry.heightOffset, Is.EqualTo(-0.08f).Within(0.001f));
        Assert.That(entry.useManualFootprint, Is.True);
        Assert.That(entry.manualFootprint, Is.EqualTo(new Vector2Int(5, 3)));
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
    public void MapToolEnableToggle_UsesOnOffLabels()
    {
        Assert.That(NoryangjinMapToolWindow.MapToolEnabledLabel, Is.EqualTo("ON"));
        Assert.That(NoryangjinMapToolWindow.MapToolDisabledLabel, Is.EqualTo("OFF"));
        Assert.That(NoryangjinMapToolWindow.RefreshMapToolButtonLabel, Is.EqualTo("리프레시"));
        Assert.That(NoryangjinMapToolWindow.MapToolDisabledHelp, Does.Contain("비적용"));
    }

    [Test]
    public void MapToolEnableToggle_TargetsWorkObjectsForActiveStateSwitch()
    {
        Assert.That(NoryangjinMapToolWindow.MapToolWorkObjectNames, Does.Contain("MapTool_Work_Floor"));
        Assert.That(NoryangjinMapToolWindow.MapToolWorkObjectNames, Does.Contain("MapTool_Origin_Post"));
        Assert.That(NoryangjinMapToolWindow.MapToolWorkObjectNames, Does.Contain("MapTool_Work_Grid"));
    }

    [Test]
    public void MapToolEnableToggle_ControlsWhetherSceneViewAppliesTooling()
    {
        Assert.That(NoryangjinMapToolWindow.ShouldApplyMapTool(true), Is.True);
        Assert.That(NoryangjinMapToolWindow.ShouldApplyMapTool(false), Is.False);
    }

    [Test]
    public void PrimaryTabs_SeparateMapToolAndConvenienceControls()
    {
        Assert.That(
            NoryangjinMapToolWindow.PrimaryTabLabels,
            Is.EqualTo(new[] { "맵툴", "편의" }));
        Assert.That((int)NoryangjinMapToolTab.MapTool, Is.EqualTo(0));
        Assert.That((int)NoryangjinMapToolTab.Convenience, Is.EqualTo(1));
    }

    [Test]
    public void SceneUiVisibilityControls_UseExplicitKoreanTabs()
    {
        Assert.That(
            NoryangjinMapToolWindow.SceneUiVisibilityTabLabels,
            Is.EqualTo(new[] { "UI 활성화", "UI 비활성화" }));
        Assert.That(NoryangjinMapToolWindow.SceneUiSectionTitle, Is.EqualTo("씬 UI 표시"));
        Assert.That(NoryangjinMapToolWindow.SceneUiVisibilityHelp, Does.Contain("월드 UI"));
        Assert.That(
            typeof(NoryangjinMapToolWindow).GetMethod(
                "DrawSceneUiVisibilityControls",
                BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
    }

    [Test]
    public void SceneUiVisibilityToggle_TargetsOnlyRootCanvases()
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            var rootCanvasObject = new GameObject("Canvas", typeof(Canvas));
            var secondaryRootCanvasObject = new GameObject("OverlayCanvas", typeof(Canvas));
            var rootWorldCanvasObject = new GameObject("RootWorldCanvas", typeof(Canvas));
            var player = new GameObject("Player");
            var worldCanvasObject = new GameObject("WorldCanvas", typeof(Canvas));
            SceneManager.MoveGameObjectToScene(rootCanvasObject, previewScene);
            SceneManager.MoveGameObjectToScene(secondaryRootCanvasObject, previewScene);
            SceneManager.MoveGameObjectToScene(rootWorldCanvasObject, previewScene);
            SceneManager.MoveGameObjectToScene(player, previewScene);
            SceneManager.MoveGameObjectToScene(worldCanvasObject, previewScene);
            rootWorldCanvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            worldCanvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            worldCanvasObject.transform.SetParent(player.transform);

            Assert.That(
                NoryangjinMapToolWindow.GetSceneRootCanvasVisibilityTabIndex(
                    previewScene,
                    out int canvasCount),
                Is.EqualTo(0));
            Assert.That(canvasCount, Is.EqualTo(2));

            secondaryRootCanvasObject.SetActive(false);
            Assert.That(
                NoryangjinMapToolWindow.GetSceneRootCanvasVisibilityTabIndex(
                    previewScene,
                    out _),
                Is.EqualTo(-1),
                "Mixed root-canvas visibility must leave both tabs actionable.");

            bool disabled = NoryangjinMapToolWindow.SetSceneRootCanvasesActive(
                previewScene,
                active: false,
                recordUndo: false);

            Assert.That(disabled, Is.True);
            Assert.That(rootCanvasObject.activeSelf, Is.False);
            Assert.That(secondaryRootCanvasObject.activeSelf, Is.False);
            Assert.That(rootWorldCanvasObject.activeSelf, Is.True);
            Assert.That(worldCanvasObject.activeSelf, Is.True);
            Assert.That(
                NoryangjinMapToolWindow.GetSceneRootCanvasVisibilityTabIndex(
                    previewScene,
                    out _),
                Is.EqualTo(1));

            bool enabled = NoryangjinMapToolWindow.SetSceneRootCanvasesActive(
                previewScene,
                active: true,
                recordUndo: false);

            Assert.That(enabled, Is.True);
            Assert.That(rootCanvasObject.activeSelf, Is.True);
            Assert.That(secondaryRootCanvasObject.activeSelf, Is.True);
            Assert.That(rootWorldCanvasObject.activeSelf, Is.True);
            Assert.That(worldCanvasObject.activeSelf, Is.True);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void SceneUiVisibilityToggle_SupportsUndoAndRedoFromMixedState()
    {
        Undo.IncrementCurrentGroup();
        int testUndoGroup = Undo.GetCurrentGroup();
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        var primaryCanvasObject = new GameObject("Canvas", typeof(Canvas));
        var secondaryCanvasObject = new GameObject("OverlayCanvas", typeof(Canvas));
        SceneManager.MoveGameObjectToScene(primaryCanvasObject, previewScene);
        SceneManager.MoveGameObjectToScene(secondaryCanvasObject, previewScene);
        secondaryCanvasObject.SetActive(false);

        try
        {
            Assert.That(
                NoryangjinMapToolWindow.SetSceneRootCanvasesActive(
                    previewScene,
                    active: false,
                    recordUndo: true),
                Is.True);
            Assert.That(primaryCanvasObject.activeSelf, Is.False);
            Assert.That(secondaryCanvasObject.activeSelf, Is.False);

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(primaryCanvasObject.activeSelf, Is.True);
            Assert.That(secondaryCanvasObject.activeSelf, Is.False);

            Undo.PerformRedo();
            Assert.That(primaryCanvasObject.activeSelf, Is.False);
            Assert.That(secondaryCanvasObject.activeSelf, Is.False);
        }
        finally
        {
            Undo.RevertAllDownToGroup(testUndoGroup);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void RefreshMapTool_ReactivatesPlacementAndWorkObjectsOnly()
    {
        var root = new GameObject("Noryangjin_MapTool");
        var roads = new GameObject("Roads");
        var prop = new GameObject("Prop_Box_X+00_Z+00");
        var props = new GameObject("Props");
        var water = new GameObject("Water");
        var workGrid = new GameObject("MapTool_Work_Grid");
        var unrelated = new GameObject("Unrelated_Hidden");
        MeshRenderer propRenderer = prop.AddComponent<MeshRenderer>();
        MeshRenderer workGridRenderer = workGrid.AddComponent<MeshRenderer>();
        MeshRenderer unrelatedRenderer = unrelated.AddComponent<MeshRenderer>();

        try
        {
            roads.transform.SetParent(root.transform);
            prop.transform.SetParent(roads.transform);
            props.transform.SetParent(root.transform);
            water.transform.SetParent(root.transform);
            workGrid.transform.SetParent(root.transform);
            unrelated.transform.SetParent(root.transform);

            prop.SetActive(false);
            roads.SetActive(false);
            props.SetActive(false);
            water.SetActive(false);
            workGrid.SetActive(false);
            unrelated.SetActive(false);
            propRenderer.enabled = false;
            workGridRenderer.enabled = false;
            unrelatedRenderer.enabled = false;
            root.SetActive(false);

            bool changed = NoryangjinMapToolWindow.RestoreMapToolVisibleObjects(root.transform, recordUndo: false);

            Assert.That(changed, Is.True);
            Assert.That(root.activeSelf, Is.True);
            Assert.That(roads.activeSelf, Is.True);
            Assert.That(prop.activeSelf, Is.True);
            Assert.That(props.activeSelf, Is.True);
            Assert.That(water.activeSelf, Is.True);
            Assert.That(workGrid.activeSelf, Is.True);
            Assert.That(unrelated.activeSelf, Is.False);
            Assert.That(propRenderer.enabled, Is.True);
            Assert.That(workGridRenderer.enabled, Is.False);
            Assert.That(unrelatedRenderer.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RefreshMapTool_PreservesExistingCameraTransform()
    {
        var cameraObject = new GameObject("Custom_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.localPosition = new Vector3(-11f, 7f, 13f);
        camera.transform.localRotation = Quaternion.Euler(12f, 34f, 56f);
        camera.transform.localScale = new Vector3(2f, 3f, 4f);

        try
        {
            bool changed = NoryangjinMapToolWindow.ApplyMapToolCameraDefaults(camera, preserveTransform: true);

            Assert.That(changed, Is.True);
            Assert.That(cameraObject.name, Is.EqualTo("MapTool_Camera"));
            Assert.That(camera.transform.localPosition, Is.EqualTo(new Vector3(-11f, 7f, 13f)));
            Assert.That(camera.transform.localRotation.eulerAngles.x, Is.EqualTo(12f).Within(0.001f));
            Assert.That(camera.transform.localRotation.eulerAngles.y, Is.EqualTo(34f).Within(0.001f));
            Assert.That(camera.transform.localRotation.eulerAngles.z, Is.EqualTo(56f).Within(0.001f));
            Assert.That(camera.transform.localScale, Is.EqualTo(new Vector3(2f, 3f, 4f)));
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.orthographicSize, Is.EqualTo(24f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void RefreshMapTool_CanPreserveExistingCameraProjection()
    {
        var cameraObject = new GameObject("Custom_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.orthographicSize = 7f;

        try
        {
            bool changed = NoryangjinMapToolWindow.ApplyMapToolCameraDefaults(
                camera,
                preserveTransform: true,
                preserveProjection: true);

            Assert.That(changed, Is.True);
            Assert.That(cameraObject.name, Is.EqualTo("MapTool_Camera"));
            Assert.That(camera.orthographic, Is.False);
            Assert.That(camera.orthographicSize, Is.EqualTo(7f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void UndoRedoRefresh_ClearsTransientSceneViewFootprintState()
    {
        bool coarseSnapActive = true;
        int lastPlacedInstanceId = 12345;

        NoryangjinMapToolWindow.ClearTransientMapToolVisualStateAfterUndo(
            ref coarseSnapActive,
            ref lastPlacedInstanceId);

        Assert.That(coarseSnapActive, Is.False);
        Assert.That(lastPlacedInstanceId, Is.Zero);
    }

    [Test]
    public void PlacedObjectHeightLabel_FormatsCurrentYValue()
    {
        Assert.That(NoryangjinMapToolWindow.FormatPlacedObjectHeightLabel(1.075f), Is.EqualTo("Y 1.08"));
        Assert.That(NoryangjinMapToolWindow.FormatPlacedObjectHeightLabel(-0.5f), Is.EqualTo("Y -0.50"));
    }

    [Test]
    public void PlacedObjectHeightLabel_DrawsOnlyForMapToolPlacedObjectsWhenToolIsOn()
    {
        Assert.That(
            NoryangjinMapToolWindow.ShouldDrawPlacedObjectHeightLabel("Road_Basic_X+00_Z+00", true),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ShouldDrawPlacedObjectHeightLabel("MapTool_Work_Grid", true),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.ShouldDrawPlacedObjectHeightLabel("Road_Basic_X+00_Z+00", false),
            Is.False);
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
    public void EmptyCellDelete_FallsBackToBroadOverlapWhenNoObjectStartsAtCursor()
    {
        var propParent = new GameObject("Props");
        var broadProp = new GameObject("Prop_Broad_X+99_Z+99");

        try
        {
            broadProp.transform.SetParent(propParent.transform);

            GameObject target = NoryangjinMapToolWindow.SelectSingleCursorDeleteTarget(
                new List<GameObject>
                {
                    broadProp
                },
                Vector2Int.zero);

            Assert.That(target, Is.SameAs(broadProp));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(broadProp);
            UnityEngine.Object.DestroyImmediate(propParent);
        }
    }

    [Test]
    public void CursorCellObjectLabel_UsesKoreanPaletteNameOrBlankName()
    {
        const string crabAquariumPrefab = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/030_STAGE01_NRY_PROPS_030_Crab_aquarium_tank/030_STAGE01_NRY_PROPS_030_Crab_aquarium_tank.prefab";

        Assert.That(NoryangjinMapToolWindow.BuildCursorCellObjectLabel(crabAquariumPrefab), Is.EqualTo("게 수족관"));
        Assert.That(
            NoryangjinMapToolWindow.BuildCursorCellObjectLabel(NoryangjinMapToolWindow.TurnSpotPrefabPath),
            Is.EqualTo(NoryangjinMapToolWindow.TurnSpotPaletteItemLabel));
        Assert.That(NoryangjinMapToolWindow.BuildCursorCellObjectLabel(null), Is.EqualTo("빈 칸"));
    }

    [Test]
    public void PlacementSummary_PrefersSelectedPlacedObjectOverCursorObject()
    {
        var selectedRoot = new GameObject("Prop_Selected_X+00_Z+00");
        var cursorTarget = new GameObject("Prop_Cursor_X+01_Z+00");

        try
        {
            Assert.That(
                NoryangjinMapToolWindow.ResolvePlacementSummaryTarget(selectedRoot, cursorTarget),
                Is.SameAs(selectedRoot));
            Assert.That(
                NoryangjinMapToolWindow.ResolvePlacementSummaryTarget(null, cursorTarget),
                Is.SameAs(cursorTarget));
            Assert.That(
                NoryangjinMapToolWindow.ResolvePlacementSummaryTarget(null, null),
                Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(selectedRoot);
            Object.DestroyImmediate(cursorTarget);
        }
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
    public void WorkSubGridOverlay_IsHiddenByDefault()
    {
        Assert.That(NoryangjinMapToolWindow.DefaultShowWorkSubGrid, Is.False);
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
    public void SceneViewProjection_UsesOrthographicOnlyWhileMapToolIsEnabled()
    {
        Assert.That(
            NoryangjinMapToolWindow.ShouldUseOrthographicSceneView(true),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ShouldUseOrthographicSceneView(false),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.ShouldLockSceneViewRotation(true),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ShouldLockSceneViewRotation(false),
            Is.False);
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
        Assert.That(NoryangjinMapToolWindow.WorkGridExtent, Is.EqualTo(300));
        Assert.That(
            NoryangjinMapToolWindow.BuildWorkGridSpan(NoryangjinMapToolWindow.DefaultCellSize),
            Is.EqualTo(NoryangjinMapToolWindow.DefaultCellSize * 601f).Within(0.001f));
        Assert.That(NoryangjinMapToolWindow.WorkGridLineY, Is.GreaterThanOrEqualTo(0.035f));
        Assert.That(NoryangjinMapToolWindow.WorkGridLineWidth, Is.GreaterThanOrEqualTo(0.045f));
    }

    [Test]
    public void WorkGridExtentControls_ClampAndOfferLargerPresets()
    {
        Assert.That(
            NoryangjinMapToolWindow.WorkGridExtentPresets,
            Is.EqualTo(new[] { 300, 600, 900, 1200 }));
        Assert.That(
            NoryangjinMapToolWindow.NormalizeWorkGridExtent(0),
            Is.EqualTo(NoryangjinMapToolWindow.WorkGridExtent));
        Assert.That(
            NoryangjinMapToolWindow.NormalizeWorkGridExtent(1),
            Is.EqualTo(NoryangjinMapToolWindow.MinWorkGridExtent));
        Assert.That(
            NoryangjinMapToolWindow.NormalizeWorkGridExtent(5000),
            Is.EqualTo(NoryangjinMapToolWindow.MaxWorkGridExtent));
        Assert.That(
            NoryangjinMapToolWindow.BuildWorkGridSpan(
                NoryangjinMapToolWindow.DefaultCellSize,
                600),
            Is.EqualTo(NoryangjinMapToolWindow.DefaultCellSize * 1201f).Within(0.001f));
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
    public void PlacementValidityFill_UsesBlueForSelectedArea()
    {
        Color fill = NoryangjinMapToolWindow.GetPlacementValidityFillColor(NoryangjinMapToolSceneGridCellState.Selected);

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
    public void EnemyRouteAlignmentButtons_DescribePlayerTravelDirectionInKorean()
    {
        Assert.That(
            NoryangjinMapToolWindow.AlignSelectedEnemyToRouteButtonLabel,
            Does.Contain("플레이어 진행 방향"));
        Assert.That(
            NoryangjinMapToolWindow.AlignAllEnemiesToRouteButtonLabel,
            Does.Contain("플레이어 진행 방향"));
        Assert.That(
            NoryangjinMapToolWindow.EnemyAutomaticRouteAlignmentHint,
            Does.Contain("플레이어 경로가 없을 때만 아래 Y값"));
        Assert.That(
            NoryangjinMapToolWindow.EnemyAutomaticRouteAlignmentHint,
            Does.Contain("배치 후 수동 회전"));
    }

    [Test]
    public void BuildEnemyRouteAlignedRotation_PointsRootForwardAlongRouteWithoutHalfTurn()
    {
        Vector3 routeDirection = new Vector3(4f, 0f, -2f).normalized;

        Quaternion rotation =
            NoryangjinMapToolWindow.BuildEnemyRouteAlignedRotation(routeDirection);

        Vector3 expectedForward = new Vector3(4f, 0f, -2f).normalized;
        Assert.That(
            Vector3.Angle(rotation * Vector3.forward, expectedForward),
            Is.EqualTo(0f).Within(0.001f));
        Assert.That(
            Vector3.Dot(rotation * Vector3.forward, expectedForward),
            Is.GreaterThan(0.999f));
    }

    [Test]
    public void AutomaticEnemyRouteAlignment_RequiresOneOfFiveEnemyPrefabsAndRouteStart()
    {
        var routeStart = new GameObject("Route Start");
        try
        {
            Assert.That(NoryangjinMapToolWindow.EnemyPalettePrefabPaths, Has.Length.EqualTo(5));
            foreach (string prefabPath in NoryangjinMapToolWindow.EnemyPalettePrefabPaths)
            {
                Assert.That(
                    NoryangjinMapToolWindow.ShouldAutomaticallyAlignEnemyPlacement(
                        prefabPath,
                        routeStart.transform),
                    Is.True,
                    prefabPath);
            }

            Assert.That(
                NoryangjinMapToolWindow.ShouldAutomaticallyAlignEnemyPlacement(
                    NoryangjinMapToolWindow.TurnSpotPrefabPath,
                    routeStart.transform),
                Is.False);
            Assert.That(
                NoryangjinMapToolWindow.ShouldAutomaticallyAlignEnemyPlacement(
                    NoryangjinMapToolWindow.EnemyPalettePrefabPaths[0],
                    null),
                Is.False);
        }
        finally
        {
            Object.DestroyImmediate(routeStart);
        }
    }

    [Test]
    public void ResolveEnemyPlacementRotation_WithoutPlayerPreservesAuthoredYaw()
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        Quaternion authoredRotation = Quaternion.Euler(0f, 37f, 0f);

        try
        {
            Quaternion resolved = NoryangjinMapToolWindow.ResolveEnemyPlacementRotation(
                NoryangjinMapToolWindow.EnemyPalettePrefabPaths[0],
                new Vector3(4f, 0f, 8f),
                authoredRotation,
                previewScene);

            Assert.That(
                Quaternion.Angle(resolved, authoredRotation),
                Is.EqualTo(0f).Within(0.001f));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void CollectEnemyRouteAlignmentTargets_ReturnsPlacedEnemyRootsOnly()
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            var mapToolRoot = new GameObject("Noryangjin_MapTool");
            SceneManager.MoveGameObjectToScene(mapToolRoot, previewScene);
            var enemies = new GameObject("Enemies");
            SceneManager.MoveGameObjectToScene(enemies, previewScene);
            enemies.transform.SetParent(mapToolRoot.transform, false);

            var directEnemy = new GameObject("Enemy_Direct_X+00_Z+00");
            SceneManager.MoveGameObjectToScene(directEnemy, previewScene);
            directEnemy.transform.SetParent(enemies.transform, false);
            directEnemy.AddComponent<EnemyMovementController>();

            var nestedEnemyRoot = new GameObject("Enemy_Nested_X+01_Z+00");
            SceneManager.MoveGameObjectToScene(nestedEnemyRoot, previewScene);
            nestedEnemyRoot.transform.SetParent(enemies.transform, false);
            var nestedController = new GameObject("Movement Controller");
            SceneManager.MoveGameObjectToScene(nestedController, previewScene);
            nestedController.transform.SetParent(nestedEnemyRoot.transform, false);
            nestedController.AddComponent<EnemyMovementController>();

            var outsideEnemy = new GameObject("Enemy_Outside_X+02_Z+00");
            SceneManager.MoveGameObjectToScene(outsideEnemy, previewScene);
            outsideEnemy.transform.SetParent(mapToolRoot.transform, false);
            outsideEnemy.AddComponent<EnemyMovementController>();

            List<GameObject> targets =
                NoryangjinMapToolWindow.CollectEnemyRouteAlignmentTargets(mapToolRoot);

            Assert.That(targets, Is.EqualTo(new[] { directEnemy, nestedEnemyRoot }));
            Assert.That(
                NoryangjinMapToolWindow.CanAlignEnemyRootToPlayerRoute(
                    nestedEnemyRoot,
                    mapToolRoot.transform),
                Is.True);
            Assert.That(
                NoryangjinMapToolWindow.CanAlignEnemyRootToPlayerRoute(
                    outsideEnemy,
                    null),
                Is.False);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
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
    public void SelectedObjectMoveJoystick_MovesXZByOneSnapCellAndKeepsHeight()
    {
        Vector3 currentPosition = new Vector3(10f, 2.5f, -4f);
        float snapCellSize = NoryangjinMapToolWindow.BuildPlacementSnapCellSize(NoryangjinMapToolWindow.DefaultCellSize, false);

        Vector3 movedRight = NoryangjinMapToolWindow.MoveObjectPositionByGridStep(currentPosition, 1, 0, snapCellSize);
        Vector3 movedUp = NoryangjinMapToolWindow.MoveObjectPositionByGridStep(currentPosition, 0, 1, snapCellSize);

        Assert.That(movedRight.x, Is.EqualTo(currentPosition.x + snapCellSize).Within(0.001f));
        Assert.That(movedRight.y, Is.EqualTo(currentPosition.y).Within(0.001f));
        Assert.That(movedRight.z, Is.EqualTo(currentPosition.z).Within(0.001f));
        Assert.That(movedUp.x, Is.EqualTo(currentPosition.x).Within(0.001f));
        Assert.That(movedUp.y, Is.EqualTo(currentPosition.y).Within(0.001f));
        Assert.That(movedUp.z, Is.EqualTo(currentPosition.z + snapCellSize).Within(0.001f));
    }

    [Test]
    public void SelectedObjectMoveJoystick_MovesMapToolGridAnchorNameWithPosition()
    {
        Assert.That(
            NoryangjinMapToolWindow.MoveMapToolPlacedObjectNameByGridStep("Prop_Test_X+02_Z-01", 1, -1),
            Is.EqualTo("Prop_Test_X+03_Z-02"));
        Assert.That(
            NoryangjinMapToolWindow.MoveMapToolPlacedObjectNameByGridStep("Road_Bridge_X-02_Z+07", -1, 1),
            Is.EqualTo("Road_Bridge_X-03_Z+08"));
        Assert.That(
            NoryangjinMapToolWindow.MoveMapToolPlacedObjectNameByGridStep("Renamed Prop", 1, 0),
            Is.EqualTo("Renamed Prop"));
    }

    [Test]
    public void SelectedObjectRotationButtons_AddHalfTurnAndKeepOtherAxes()
    {
        Vector3 currentEuler = new Vector3(12f, 45f, -8f);

        Assert.That(
            NoryangjinMapToolWindow.MoveObjectRotationYByStep(currentEuler, -180f),
            Is.EqualTo(new Vector3(12f, -135f, -8f)));
        Assert.That(
            NoryangjinMapToolWindow.MoveObjectRotationYByStep(currentEuler, 180f),
            Is.EqualTo(new Vector3(12f, 225f, -8f)));
        Assert.That(
            NoryangjinMapToolWindow.MoveObjectRotationYByStep(currentEuler, -90f),
            Is.EqualTo(new Vector3(12f, -45f, -8f)));
        Assert.That(
            NoryangjinMapToolWindow.MoveObjectRotationYByStep(currentEuler, 90f),
            Is.EqualTo(new Vector3(12f, 135f, -8f)));
        Assert.That(
            NoryangjinMapToolWindow.MoveObjectRotationYByStep(new Vector3(12f, -135f, -8f), -180f),
            Is.EqualTo(new Vector3(12f, -315f, -8f)));
    }

    [Test]
    public void ApplySelectedObjectRotationToTarget_RecordsPrefabInstanceOverride()
    {
        const string folderPath = "Assets/Tests/Generated";
        const string prefabPath = folderPath + "/MapToolLiveRotationEditTest.prefab";
        bool createdFolder = false;
        GameObject source = new GameObject("MapToolLiveRotationEditTest");
        GameObject instance = null;

        try
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Tests", "Generated");
                createdFolder = true;
            }

            PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            Assert.That(
                NoryangjinMapToolWindow.ApplySelectedObjectRotationToTarget(
                    instance,
                    new Vector3(0f, 45f, 0f)),
                Is.True);

            Assert.That(instance.transform.eulerAngles.y, Is.EqualTo(45f).Within(0.001f));
            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instance);
            Assert.That(HasTransformRotationModification(modifications), Is.True);
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
    public void ApplySelectedObjectRotationToTarget_SupportsUndoAndRedo()
    {
        Undo.IncrementCurrentGroup();
        int testUndoGroup = Undo.GetCurrentGroup();
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        var target = new GameObject("Enemy_Guard_X+00_Z+00");
        SceneManager.MoveGameObjectToScene(target, previewScene);
        target.transform.rotation = Quaternion.Euler(0f, 37f, 0f);

        try
        {
            Assert.That(
                NoryangjinMapToolWindow.ApplySelectedObjectRotationToTarget(
                    target,
                    new Vector3(0f, 90f, 0f)),
                Is.True);
            Assert.That(target.transform.eulerAngles.y, Is.EqualTo(90f).Within(0.001f));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(target.transform.eulerAngles.y, Is.EqualTo(37f).Within(0.001f));

            Undo.PerformRedo();
            Assert.That(target.transform.eulerAngles.y, Is.EqualTo(90f).Within(0.001f));
        }
        finally
        {
            Undo.RevertAllDownToGroup(testUndoGroup);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void ResolveVisualPickSelectionTarget_PrefersHoveredPlacedObjectWithoutLayerPriority()
    {
        GameObject mapToolRoot = new GameObject("Noryangjin_MapTool");
        GameObject roads = new GameObject("Roads");
        GameObject props = new GameObject("Props");
        GameObject road = new GameObject("Road_Basic_X+00_Z+00");
        GameObject hoveredProp = new GameObject("Prop_Seagull_X+00_Z+00");
        GameObject hoveredMesh = new GameObject("Mesh");
        roads.transform.SetParent(mapToolRoot.transform);
        props.transform.SetParent(mapToolRoot.transform);
        road.transform.SetParent(roads.transform);
        hoveredProp.transform.SetParent(props.transform);
        hoveredMesh.transform.SetParent(hoveredProp.transform);

        try
        {
            Assert.That(
                NoryangjinMapToolWindow.ResolveVisualPickSelectionTarget(hoveredMesh, mapToolRoot),
                Is.SameAs(hoveredProp));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mapToolRoot);
        }
    }

    [Test]
    public void ResolveVisualPickSelectionTarget_RejectsHoveredObjectOutsideMapTool()
    {
        GameObject mapToolRoot = new GameObject("Noryangjin_MapTool");
        GameObject outsideRoot = new GameObject("Prop_Outside_X+00_Z+00");
        GameObject outsideMesh = new GameObject("Mesh");
        outsideMesh.transform.SetParent(outsideRoot.transform);

        try
        {
            Assert.That(
                NoryangjinMapToolWindow.ResolveVisualPickSelectionTarget(outsideMesh, mapToolRoot),
                Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mapToolRoot);
            UnityEngine.Object.DestroyImmediate(outsideRoot);
        }
    }

    [Test]
    public void ResolveHoveredHeightLabelTarget_UsesLastDrawnLabelUnderMouse()
    {
        GameObject water = new GameObject("Water");
        GameObject fishScrap = new GameObject("Fish Scrap");
        var labels = new List<KeyValuePair<GameObject, Rect>>
        {
            new(water, new Rect(10f, 10f, 40f, 20f)),
            new(fishScrap, new Rect(20f, 10f, 40f, 20f))
        };

        try
        {
            Assert.That(
                NoryangjinMapToolWindow.ResolveHoveredHeightLabelTarget(
                    labels,
                    new Vector2(30f, 20f)),
                Is.SameAs(fishScrap));
            Assert.That(
                NoryangjinMapToolWindow.ResolveHoveredHeightLabelTarget(
                    labels,
                    new Vector2(100f, 100f)),
                Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(water);
            UnityEngine.Object.DestroyImmediate(fishScrap);
        }
    }

    [Test]
    public void ResolveSceneSelectionTarget_PrefersHoveredHeightLabelOverWaterVisualPick()
    {
        GameObject fishScrap = new GameObject("Fish Scrap");
        GameObject water = new GameObject("Water");
        GameObject gridFallback = new GameObject("Grid Fallback");

        try
        {
            Assert.That(
                NoryangjinMapToolWindow.ResolveSceneSelectionTarget(
                    fishScrap,
                    water,
                    gridFallback),
                Is.SameAs(fishScrap));
            Assert.That(
                NoryangjinMapToolWindow.ResolveSceneSelectionTarget(
                    null,
                    water,
                    gridFallback),
                Is.SameAs(water));
            Assert.That(
                NoryangjinMapToolWindow.ResolveSceneSelectionTarget(
                    null,
                    null,
                    gridFallback),
                Is.SameAs(gridFallback));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fishScrap);
            UnityEngine.Object.DestroyImmediate(water);
            UnityEngine.Object.DestroyImmediate(gridFallback);
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
    public void ResolvePaletteDisplayLabel_UsesCustomLabelForRoadItems()
    {
        const string roadPath = "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy.prefab";

        Assert.That(NoryangjinMapToolWindow.ResolvePaletteDisplayLabel(roadPath, "  나무길  "), Is.EqualTo("나무길"));
        Assert.That(NoryangjinMapToolWindow.ResolvePaletteDisplayLabel(roadPath, ""), Is.EqualTo("기본길"));
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
    public void PaletteYawOffset_RoundTripsPlacementRotationWithPrefabAxisCorrection()
    {
        Quaternion prefabRotation = Quaternion.Euler(270f, 0f, 0f);
        Quaternion placedRotation = NoryangjinMapToolWindow.BuildPalettePlacementRotation(prefabRotation, 45f);

        float yawOffset = NoryangjinMapToolWindow.CalculatePaletteYawOffsetFromPlacedRotation(
            placedRotation,
            prefabRotation);

        Assert.That(yawOffset, Is.EqualTo(45f).Within(0.001f));
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
    public void SeagullPerchLayer_BlocksOnlyOtherSeagullPerches()
    {
        var occupiedCells = new HashSet<NoryangjinMapToolOccupiedCell>
        {
            new(new Vector2Int(0, 0), NoryangjinMapToolPlacementLayer.Object),
            new(new Vector2Int(1, 0), NoryangjinMapToolPlacementLayer.SeagullPerch)
        };

        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                new[] { new Vector2Int(0, 0) },
                NoryangjinMapToolPlacementLayer.SeagullPerch,
                occupiedCells),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                new[] { new Vector2Int(1, 0) },
                NoryangjinMapToolPlacementLayer.SeagullPerch,
                occupiedCells),
            Is.False);
        Assert.That(
            NoryangjinMapToolWindow.CanPlaceFootprintCells(
                new[] { new Vector2Int(1, 0) },
                NoryangjinMapToolPlacementLayer.Object,
                occupiedCells),
            Is.True);
    }

    [Test]
    public void SeagullPerchPrefab_UsesSeparatePlacementLayer()
    {
        Assert.That(NoryangjinMapToolWindow.IsSeagullPerchPrefabPath(NoryangjinMapToolWindow.SeagullPerchPrefabPath), Is.True);
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteItemLayer(
                NoryangjinMapToolWindow.SeagullPerchPrefabPath,
                NoryangjinMapToolPaletteCategory.Prop),
            Is.EqualTo(NoryangjinMapToolPlacementLayer.SeagullPerch));
        Assert.That(
            NoryangjinMapToolWindow.GetPaletteItemLayer(
                "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/001_STAGE01_NRY_PROPS_001_Blue_fish_crate/001_STAGE01_NRY_PROPS_001_Blue_fish_crate.prefab",
                NoryangjinMapToolPaletteCategory.Prop),
            Is.EqualTo(NoryangjinMapToolPlacementLayer.Object));
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

    private static bool HasTypographyOverride(string sceneYaml, string prefabGuid)
    {
        string[] lines = sceneYaml.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            if (!lines[index].Contains("guid: " + prefabGuid))
                continue;

            int end = Mathf.Min(index + 3, lines.Length);
            for (int propertyIndex = index + 1; propertyIndex < end; propertyIndex++)
            {
                string property = lines[propertyIndex].Trim();
                if (property is
                    "propertyPath: m_fontSize" or
                    "propertyPath: m_enableAutoSizing")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasTransformRotationModification(PropertyModification[] modifications)
    {
        if (modifications == null)
            return false;

        foreach (PropertyModification modification in modifications)
        {
            if (modification != null &&
                !string.IsNullOrEmpty(modification.propertyPath) &&
                modification.propertyPath.StartsWith("m_LocalRotation.", System.StringComparison.Ordinal))
            {
                return true;
            }
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
