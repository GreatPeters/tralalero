#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NoryangjinMapToolMode2LayoutBuilderTests
{
    [Test]
    public void Mode2Scene_IsRecognizedAsMapToolSceneAndReopensInPlace()
    {
        Assert.That(NoryangjinMapToolWindow.IsMapToolScenePath(NoryangjinMapToolWindow.MapToolScenePath), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsMapToolScenePath(NoryangjinMapToolWindow.MapToolScene2Path), Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ResolveMapToolScenePathToOpen(NoryangjinMapToolWindow.MapToolScene2Path),
            Is.EqualTo(NoryangjinMapToolWindow.MapToolScene2Path));
        Assert.That(
            NoryangjinMapToolWindow.ResolveMapToolScenePathToOpen("Assets/ShooterSurvival/Scenes/Main.unity"),
            Is.EqualTo(NoryangjinMapToolWindow.MapToolScenePath));
    }

    [Test]
    public void Mode2Builder_IsFailClosedToItsDedicatedScene()
    {
        Assert.That(
            NoryangjinMapToolMode2LayoutBuilder.CanBuildScenePath(
                NoryangjinMapToolMode2LayoutBuilder.TargetScenePath),
            Is.True);
        Assert.That(
            NoryangjinMapToolMode2LayoutBuilder.CanBuildScenePath(
            NoryangjinMapToolWindow.MapToolScenePath),
            Is.False);

        string[] protectedPaths =
        {
            NoryangjinMapToolMode2LayoutBuilder.TargetScenePath,
            "Temp/NoryangjinMapToolMode2LayoutReport.txt",
            "Temp/NoryangjinMapToolMode2TopPreview.png",
            "Temp/NoryangjinMapToolMode2ThreeQuarterPreview.png"
        };
        byte[][] before = protectedPaths
            .Select(path => File.Exists(path) ? File.ReadAllBytes(path) : null)
            .ToArray();
        Scene previewScene = EditorSceneManager.NewPreviewScene();

        try
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => NoryangjinMapToolMode2LayoutBuilder.BuildLayout(previewScene));
            Assert.That(
                exception.Message,
                Is.EqualTo(
                    $"Mode 2 layout can only be built in " +
                    $"'{NoryangjinMapToolMode2LayoutBuilder.TargetScenePath}'. " +
                    $"Active scene: '{previewScene.path}'."));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }

        for (int i = 0; i < protectedPaths.Length; i++)
        {
            if (before[i] == null)
            {
                Assert.That(File.Exists(protectedPaths[i]), Is.False);
                continue;
            }

            CollectionAssert.AreEqual(before[i], File.ReadAllBytes(protectedPaths[i]));
        }
    }

    [Test]
    public void Mode2PlacementSpecs_UseExistingSelectableStagePrefabs()
    {
        var specs = NoryangjinMapToolMode2LayoutBuilder.BuildPlacementSpecs();

        Assert.That(specs.Count, Is.EqualTo(126));
        Assert.That(specs.Select(spec => spec.Label).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(specs.Count));
        Assert.That(specs.Any(spec => spec.PrefabPath.Contains("Dock_metal_cleat", StringComparison.Ordinal)), Is.False);
        Assert.That(specs.Any(spec => spec.PrefabPath.Contains("_ROAD_", StringComparison.Ordinal)), Is.False);
        Assert.That(specs.Single(spec => spec.Label == "Background_Water_00").Yaw, Is.EqualTo(270f));
        Assert.That(specs.Single(spec => spec.Label == "Background_Water_01").Yaw, Is.EqualTo(90f));

        IReadOnlyList<string> requiredPrefabPaths =
            NoryangjinMapToolMode2LayoutBuilder.BuildRequiredPrefabPaths();
        Assert.That(requiredPrefabPaths, Is.SupersetOf(specs.Select(spec => spec.PrefabPath).Distinct()));
        Assert.That(
            requiredPrefabPaths,
            Does.Contain(
                "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/" +
                "035_STAGE01_NRY_PROPS_024_Anchor_prop/035_STAGE01_NRY_PROPS_024_Anchor_prop.prefab"));
        Assert.That(
            requiredPrefabPaths,
            Does.Contain(
                "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/" +
                "008_STAGE01_NRY_PROPS_008_Seagull_perch_post/008_STAGE01_NRY_PROPS_008_Seagull_perch_post.prefab"));

        foreach (string prefabPath in requiredPrefabPaths)
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),
                Is.Not.Null,
                $"Missing Mode 2 layout prefab: {prefabPath}");
        }
    }

    [Test]
    public void GeneratedMode2Scene_PreservesRoadSkeletonAndContainsDeterministicDressing()
    {
        Assert.That(File.Exists(NoryangjinMapToolMode2LayoutBuilder.TargetScenePath), Is.True);
        string sceneYaml = File.ReadAllText(NoryangjinMapToolMode2LayoutBuilder.TargetScenePath);
        int generatedPropCount = Regex.Matches(sceneYaml, @"(?:m_Name:|value:) Prop_Layout2_").Count;
        int roadCount = Regex.Matches(sceneYaml, @"(?:m_Name:|value:) Road_(?:Basic|LeftTurn|RightTurn)_").Count;

        Assert.That(
            generatedPropCount,
            Is.EqualTo(NoryangjinMapToolMode2LayoutBuilder.BuildPlacementSpecs().Count));
        Assert.That(roadCount, Is.EqualTo(19));
    }

    [Test]
    public void GeneratedMode2Scene_UsesExpectedTransformsConnectedPrefabsAndClearLane()
    {
        WithMode2Scene(scene =>
        {
            GameObject root = scene
                .GetRootGameObjects()
                .Single(candidate => candidate.name == "Noryangjin_MapTool");
            Transform roads = root.transform.Find("Roads");
            Transform props = root.transform.Find("Props");

            NoryangjinMapToolMode2LayoutBuilder.ValidateRoadSkeleton(roads);
            Assert.That(
                NoryangjinMapToolMode2LayoutBuilder.BuildRoadSkeletonSpecs()
                    .Select(spec => spec.Name)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(19));
            Assert.That(
                NoryangjinMapToolMode2LayoutBuilder.BuildRoadSkeletonSpecs()
                    .Select(spec => spec.PrefabPath)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(3));

            var generated = props
                .Cast<Transform>()
                .Where(child => child.name.StartsWith("Prop_Layout2_", StringComparison.Ordinal))
                .ToArray();
            Assert.That(generated.Length, Is.EqualTo(126));
            Assert.That(props.childCount, Is.EqualTo(173));
            Assert.That(generated.Select(child => child.name).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(126));

            NoryangjinMapToolPaletteDefaults paletteDefaults =
                AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(
                    NoryangjinMapToolMode2LayoutBuilder.PaletteDefaultsPath);
            Assert.That(paletteDefaults, Is.Not.Null);

            foreach (NoryangjinMapToolMode2LayoutBuilder.PlacementSpec spec in
                     NoryangjinMapToolMode2LayoutBuilder.BuildPlacementSpecs())
            {
                string namePrefix = $"Prop_Layout2_{spec.Label}_";
                Transform instance = generated.Single(
                    child => child.name.StartsWith(namePrefix, StringComparison.Ordinal));
                Assert.That(PrefabUtility.GetPrefabInstanceStatus(instance.gameObject), Is.EqualTo(PrefabInstanceStatus.Connected));
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance.gameObject),
                    Is.EqualTo(spec.PrefabPath));
                Assert.That(
                    NoryangjinMapToolMode2LayoutBuilder.PlacementMatchesSpec(instance, spec, paletteDefaults),
                    Is.True,
                    $"Transform drifted for {instance.name}");

                if (spec.AllowLaneOverlap)
                    continue;

                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.That(
                        NoryangjinMapToolMode2LayoutBuilder.IntersectsLane(renderer.bounds),
                        Is.False,
                        $"Clear-lane overlap: {instance.name}");
                }
            }
        });
    }

    [Test]
    public void FindMapToolRoot_UsesOnlyTheProvidedScene()
    {
        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene first = EditorSceneManager.NewPreviewScene();
        Scene second = EditorSceneManager.NewPreviewScene();

        try
        {
            var firstRoot = new GameObject("Noryangjin_MapTool");
            SceneManager.MoveGameObjectToScene(firstRoot, first);
            var secondRoot = new GameObject("Noryangjin_MapTool");
            SceneManager.MoveGameObjectToScene(secondRoot, second);

            Assert.That(
                NoryangjinMapToolMode2LayoutBuilder.FindMapToolRoot(second),
                Is.SameAs(secondRoot));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(second);
            EditorSceneManager.ClosePreviewScene(first);
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
        }
    }

    [Test]
    public void Mode2Builder_RejectsZeroAndNonFiniteScaleBeforePlacement()
    {
        Assert.Throws<InvalidOperationException>(
            () => NoryangjinMapToolMode2LayoutBuilder.ValidateScale("test", new Vector3(1f, 0f, 1f)));
        Assert.Throws<InvalidOperationException>(
            () => NoryangjinMapToolMode2LayoutBuilder.ValidateScale("test", new Vector3(float.NaN, 1f, 1f)));
    }

    [Test]
    public void Mode2LaneExceptionsAndRouteEnvelopes_ArePinnedByTheTest()
    {
        var specs = NoryangjinMapToolMode2LayoutBuilder.BuildPlacementSpecs();
        foreach (NoryangjinMapToolMode2LayoutBuilder.PlacementSpec spec in specs)
        {
            bool expectedException =
                spec.Label.StartsWith("Background_", StringComparison.Ordinal) ||
                spec.Label.StartsWith("Upper_Lamp_", StringComparison.Ordinal) ||
                spec.Label.StartsWith("Vertical_Lamp_", StringComparison.Ordinal) ||
                spec.Label is "Start_FishScrap_11" or "Start_IceScatter_12" or
                    "Upper_Water_SeaBuoy_03" or "Upper_Water_Plank_04" ||
                spec.Label.StartsWith("Atmosphere_", StringComparison.Ordinal) ||
                spec.Label.StartsWith("Direction_", StringComparison.Ordinal);

            Assert.That(
                spec.AllowLaneOverlap,
                Is.EqualTo(expectedException),
                $"Unexpected clear-lane exemption for {spec.Label}");
        }

        Assert.That(specs.Count(spec => spec.AllowLaneOverlap), Is.EqualTo(56));
        Assert.That(
            NoryangjinMapToolMode2LayoutBuilder.IntersectsLane(
                new Bounds(new Vector3(-10f, 0f, -115f), Vector3.one * 0.1f)),
            Is.True);
        Assert.That(
            NoryangjinMapToolMode2LayoutBuilder.IntersectsLane(
                new Bounds(new Vector3(-11f, 0f, -50f), Vector3.one * 0.1f)),
            Is.True);
        Assert.That(
            NoryangjinMapToolMode2LayoutBuilder.IntersectsLane(
                new Bounds(new Vector3(20f, 0f, -7f), Vector3.one * 0.1f)),
            Is.True);
        Assert.That(
            NoryangjinMapToolMode2LayoutBuilder.IntersectsLane(
                new Bounds(new Vector3(20f, 0f, -20f), Vector3.one * 0.1f)),
            Is.False);
    }

    private static void WithMode2Scene(Action<Scene> assertion)
    {
        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(NoryangjinMapToolMode2LayoutBuilder.TargetScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
        {
            scene = EditorSceneManager.OpenScene(
                NoryangjinMapToolMode2LayoutBuilder.TargetScenePath,
                OpenSceneMode.Additive);
        }

        try
        {
            assertion(scene);
        }
        finally
        {
            if (openedForTest)
                EditorSceneManager.CloseScene(scene, true);
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
        }
    }
}
#endif
