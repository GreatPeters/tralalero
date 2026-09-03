#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using IndianOceanAssets.ShooterSurvival;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Utils;

public sealed class NoryangjinSr18SceneTests
{
    private const string Map1Path =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";
    private const string Map2Path =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity";
    private const string Sr18Path =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
    private const string PlacementReportPath =
        "map-concepts/noryangjin-expansion-2026-09-02/sr18-unity-placement.json";
    private const string RouteManifestPath =
        "map-concepts/noryangjin-expansion-2026-09-02/route-manifest.json";
    private const string Map1ReviewedSha256 =
        "D3CFFE380E022ED0D45F9A251581861B2066960D48D54F5897181038F81C1315";
    private const string Map2ReviewedSha256 =
        "F7C8F2E0B39F2E515C5A237ED8E18E14CA60ECA5170914F10C01EC128A8C61E1";
    private const string Sr18ReviewedSha256 =
        "296ABEF194DBF4D3E7E857E962267B8F09CC2884146EE7D62B10C620BE148CDB";

    [Test]
    public void MapTool_RecognizesTheSr18SiblingScene()
    {
        Assert.That(NoryangjinMapToolWindow.Sr18MapToolScenePath, Is.EqualTo(Sr18Path));
        Assert.That(NoryangjinMapToolWindow.IsMapToolScenePath(Sr18Path), Is.True);
        Assert.That(NoryangjinMapToolWindow.ResolveMapToolScenePathToOpen(Sr18Path), Is.EqualTo(Sr18Path));
        Assert.That(NoryangjinMapToolWindow.IsMapToolScenePath(NoryangjinMapToolWindow.MapToolScene2Path), Is.True);
        Assert.That(
            NoryangjinMapToolWindow.ResolveMapToolScenePathToOpen(NoryangjinMapToolWindow.MapToolScene2Path),
            Is.EqualTo(NoryangjinMapToolWindow.MapToolScene2Path));
    }

    [Test]
    public void PlacementReport_MatchesTheSelectedSr18ManifestContract()
    {
        PlacementReport report = LoadReport();
        JObject manifest = LoadSr18Manifest();

        AssertPlacementReportContract(report, manifest);
    }

    [Test]
    public void Sr18Scene_IsTheRoadsOnlyLighthouseInfinityLayout()
    {
        Assert.That(File.Exists(Sr18Path), Is.True, "SR18 scene has not been baked yet.");
        Assert.That(HashFile(Map1Path), Is.EqualTo(Map1ReviewedSha256), "Reviewed Map1 baseline changed.");
        Assert.That(HashFile(Map2Path), Is.EqualTo(Map2ReviewedSha256), "Reviewed Map2 baseline changed.");
        Assert.That(HashFile(Sr18Path), Is.EqualTo(Sr18ReviewedSha256), "Reviewed roads-only SR18 baseline changed.");

        PlacementReport report = LoadReport();
        AssertPlacementReportContract(report, LoadSr18Manifest());

        Scene previousActive = SceneManager.GetActiveScene();
        bool previousDirty = previousActive.IsValid() && previousActive.isDirty;
        Scene map1 = SceneManager.GetSceneByPath(Map1Path);
        Scene sr18 = SceneManager.GetSceneByPath(Sr18Path);
        AssertLoadedSceneIsClean(map1, "Map1");
        AssertLoadedSceneIsClean(sr18, "SR18");

        bool openedMap1 = !map1.IsValid() || !map1.isLoaded;
        bool openedSr18 = !sr18.IsValid() || !sr18.isLoaded;
        if (openedMap1)
            map1 = EditorSceneManager.OpenScene(Map1Path, OpenSceneMode.Additive);
        if (openedSr18)
            sr18 = EditorSceneManager.OpenScene(Sr18Path, OpenSceneMode.Additive);

        try
        {
            Assert.That(map1.isDirty, Is.False, "Map1 must be saved before SR18 validation.");
            Assert.That(sr18.isDirty, Is.False, "SR18 must be saved before validation.");

            Transform sourceRoot = FindRoot(map1, "Noryangjin_MapTool").transform;
            Transform targetRoot = FindRoot(sr18, "Noryangjin_MapTool").transform;
            Transform sourceRoads = sourceRoot.Find("Roads");
            Transform targetRoads = targetRoot.Find("Roads");
            Transform sourceProps = sourceRoot.Find("Props");
            Transform targetProps = targetRoot.Find("Props");

            Assert.That(sourceRoads.childCount, Is.EqualTo(51));
            Assert.That(targetRoads.childCount, Is.EqualTo(230));
            AssertCopiedRoadPrefix(sourceRoads, targetRoads);
            AssertNamedSourceEndpoint(report, sourceRoads, targetRoads);
            AssertGeneratedRoads(report, targetRoads);
            AssertGeneratedRoadSeams(report, targetRoads);
            AssertCrossingGeometry(report, sourceRoads, targetRoads);

            NoryangjinTurnSpot[] sourceTurnSpots = GetSceneTurnSpots(sourceProps, map1);
            NoryangjinTurnSpot[] targetTurnSpots = GetSceneTurnSpots(targetProps, sr18);
            Assert.That(targetTurnSpots.Length, Is.EqualTo(15));
            Assert.That(targetProps.childCount, Is.EqualTo(15));
            AssertCopiedTurnSpots(sourceTurnSpots, targetTurnSpots);
            AssertGeneratedTurnSpots(report, targetTurnSpots);

            AssertEmptyPlacementRoot(targetRoot, "Enemies");
            AssertEmptyPlacementRoot(targetRoot, "Bonuses");
            AssertEmptyPlacementRoot(targetRoot, "Water");
            Assert.That(sr18.GetRootGameObjects().Any(root => root.name == "Bonus_Altar"), Is.False);
            Assert.That(sr18.GetRootGameObjects().Any(root => root.name.StartsWith("Box_left", StringComparison.Ordinal)), Is.False);

            Assert.That(FindRoot(sr18, "Noryangjin_Player"), Is.Not.Null);
            Assert.That(FindRoot(sr18, "Managers"), Is.Not.Null);
            Assert.That(FindRoot(sr18, "Canvas"), Is.Not.Null);
            Assert.That(FindRoot(sr18, "EventSystem"), Is.Not.Null);
            Assert.That(FindRoot(sr18, "UpgradeServices"), Is.Not.Null);
            AssertRetainedInfrastructure(sr18);

            string[] buildScenes = EditorBuildSettings.scenes
                .Select(scene => scene.path)
                .ToArray();
            Assert.That(buildScenes, Does.Not.Contain(Sr18Path));
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded)
                EditorSceneManager.SetActiveScene(previousActive);
            if (openedSr18 && sr18.IsValid() && sr18.isLoaded)
                EditorSceneManager.CloseScene(sr18, true);
            if (openedMap1 && map1.IsValid() && map1.isLoaded)
                EditorSceneManager.CloseScene(map1, true);

            Assert.That(HashFile(Map1Path), Is.EqualTo(Map1ReviewedSha256));
            Assert.That(HashFile(Map2Path), Is.EqualTo(Map2ReviewedSha256));
            Assert.That(HashFile(Sr18Path), Is.EqualTo(Sr18ReviewedSha256));
            if (previousActive.IsValid() && previousActive.isLoaded)
                Assert.That(previousActive.isDirty, Is.EqualTo(previousDirty));
        }
    }

    private static void AssertPlacementReportContract(PlacementReport report, JObject manifest)
    {
        Assert.That((string)manifest["id"], Is.EqualTo("super-radical-18"));
        Assert.That(report.source.scenePath, Is.EqualTo(Map1Path));
        Assert.That(report.source.sha256, Is.EqualTo(Map1ReviewedSha256));
        Assert.That(HashFile(Map1Path), Is.EqualTo(report.source.sha256));
        Assert.That(report.source.roadCount, Is.EqualTo(51));
        Assert.That(report.source.turnSpotCount, Is.EqualTo(5));
        Assert.That(report.target.scenePath, Is.EqualTo(Sr18Path));
        Assert.That(report.target.routeSpec, Is.EqualTo((string)manifest["spec"]));
        Assert.That(report.target.extensionRoadCount, Is.EqualTo((int)manifest["addedModules"]));
        Assert.That(report.target.totalRoadCount, Is.EqualTo((int)manifest["totalModules"]));
        Assert.That(report.target.extensionTurnSpotCount, Is.EqualTo((int)manifest["addedTurns"]));
        Assert.That(report.target.totalTurnSpotCount, Is.EqualTo((int)manifest["totalTurns"]));
        Assert.That(report.roads.Length, Is.EqualTo(report.target.extensionRoadCount));
        Assert.That(report.turnSpots.Length, Is.EqualTo(report.target.extensionTurnSpotCount));
        Assert.That(report.target.buildSettingsIncluded, Is.False);

        AssertRouteTopology(report, manifest);
        AssertUpperIntervals(report, manifest);
        AssertManifestCrossings(report, manifest);
        AssertRouteBounds(report);

        Assert.That(report.roads.Count(road => road.kind == "RightTurn"), Is.EqualTo(report.target.newRoadMix.RightTurn));
        Assert.That(report.roads.Count(road => road.kind == "LeftTurn"), Is.EqualTo(report.target.newRoadMix.LeftTurn));
        Assert.That(report.roads.Count(road => road.kind == "Uphill"), Is.EqualTo(report.target.newRoadMix.Uphill));
        Assert.That(report.roads.Count(road => road.kind == "Downhill"), Is.EqualTo(report.target.newRoadMix.Downhill));
        Assert.That(report.roads.Count(road => road.kind == "Basic"), Is.EqualTo(report.target.newRoadMix.Basic));
    }

    private static void AssertRouteTopology(PlacementReport report, JObject manifest)
    {
        RouteLeg[] legs = ParseRoute((string)manifest["spec"]);
        Assert.That(legs.Sum(leg => leg.Count), Is.EqualTo(report.roads.Length));
        Assert.That(legs.Length - 1, Is.EqualTo(report.turnSpots.Length));

        int roadCursor = 0;
        int x = 0;
        int z = 0;
        for (int legIndex = 0; legIndex < legs.Length; legIndex++)
        {
            RouteLeg leg = legs[legIndex];
            Vector2Int step = DirectionStep(leg.Direction);
            for (int stepInLeg = 1; stepInLeg <= leg.Count; stepInLeg++)
            {
                x += step.x;
                z += step.y;
                RoadPlacement road = report.roads[roadCursor];
                int expectedIndex = roadCursor + 1;

                Assert.That(road.index, Is.EqualTo(expectedIndex), $"Road {expectedIndex} index");
                Assert.That(road.leg, Is.EqualTo(legIndex + 1), $"Road {expectedIndex} leg");
                Assert.That(road.stepInLeg, Is.EqualTo(stepInLeg), $"Road {expectedIndex} step");
                Assert.That(road.direction, Is.EqualTo(leg.Direction), $"Road {expectedIndex} direction");
                Assert.That(road.@abstract, Is.EqualTo(new[] { x, z }), $"Road {expectedIndex} abstract coordinate");
                Assert.That(road.world[0],
                    Is.EqualTo(report.source.endpointWorld[0] + x * report.source.pitch).Within(0.001f),
                    $"Road {expectedIndex} world X");
                Assert.That(road.world[2],
                    Is.EqualTo(report.source.endpointWorld[2] + z * report.source.pitch).Within(0.001f),
                    $"Road {expectedIndex} world Z");
                Assert.That(road.yaw, Is.EqualTo(DirectionYaw(leg.Direction)).Within(0.01f), $"Road {expectedIndex} yaw");

                string expectedKind = ExpectedRoadKind(report.target.upperIntervals, legs, legIndex, stepInLeg, expectedIndex);
                Assert.That(road.kind, Is.EqualTo(expectedKind), $"Road {expectedIndex} kind");
                roadCursor++;
            }

            if (legIndex >= legs.Length - 1)
                continue;

            RouteLeg nextLeg = legs[legIndex + 1];
            TurnPlacement turn = report.turnSpots[legIndex];
            Assert.That(turn.ordinal, Is.EqualTo(legIndex + 1));
            Assert.That(turn.roadIndex, Is.EqualTo(roadCursor + 1));
            Assert.That(turn.grid, Is.EqualTo(new[] { x, z }));
            Assert.That(turn.triggerYaw, Is.EqualTo(DirectionYaw(leg.Direction)).Within(0.01f));
            Assert.That(turn.targetYaw, Is.EqualTo(DirectionYaw(nextLeg.Direction)).Within(0.01f));
            Assert.That(turn.targetX, Is.EqualTo(0f).Within(0.01f));
            Assert.That(turn.duration, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(turn.world[0], Is.EqualTo(report.source.endpointWorld[0] + x * report.source.pitch).Within(0.001f));
            Assert.That(turn.world[1], Is.EqualTo(report.source.endpointWorld[1]).Within(0.001f));
            Assert.That(turn.world[2], Is.EqualTo(report.source.endpointWorld[2] + z * report.source.pitch).Within(0.001f));
        }

        Assert.That(roadCursor, Is.EqualTo(report.roads.Length));
        Assert.That(report.target.finalDirection, Is.EqualTo(legs[^1].Direction));
        Assert.That(report.target.finalDirection, Is.EqualTo((string)manifest["highwayDirection"]));
        Assert.That(report.roads[^1].direction, Is.EqualTo(report.target.finalDirection));
        Assert.That(report.roads[^1].@abstract, Is.EqualTo(new[] { x, z }));
    }

    private static string ExpectedRoadKind(
        ElevationInterval[] intervals,
        RouteLeg[] legs,
        int legIndex,
        int stepInLeg,
        int roadIndex)
    {
        if (legIndex > 0 && stepInLeg == 1)
            return TurnKind(legs[legIndex - 1].Direction, legs[legIndex].Direction);

        ElevationInterval interval = intervals.SingleOrDefault(candidate =>
            roadIndex >= candidate.start && roadIndex < candidate.endExclusive);
        if (interval != null && roadIndex == interval.start)
            return "Uphill";
        if (interval != null && roadIndex == interval.endExclusive - 1)
            return "Downhill";
        return "Basic";
    }

    private static void AssertUpperIntervals(PlacementReport report, JObject manifest)
    {
        JArray features = (JArray)manifest["features"];
        Assert.That(report.target.upperIntervals.Length, Is.EqualTo(features.Count));
        for (int index = 0; index < features.Count; index++)
        {
            JObject feature = (JObject)features[index];
            ElevationInterval interval = report.target.upperIntervals[index];
            Assert.That(interval.start, Is.EqualTo((int)feature["startModule"]));
            Assert.That(interval.endExclusive, Is.EqualTo((int)feature["endModule"]));
        }

        ElevationInterval[] recomputed = RecomputeElevatedIntervals(report.roads);
        Assert.That(recomputed.Length, Is.EqualTo(report.target.upperIntervals.Length));
        for (int index = 0; index < recomputed.Length; index++)
        {
            Assert.That(recomputed[index].start, Is.EqualTo(report.target.upperIntervals[index].start));
            Assert.That(recomputed[index].endExclusive, Is.EqualTo(report.target.upperIntervals[index].endExclusive));
        }

        JArray points = (JArray)manifest["crossings"]["points"];
        foreach (JObject feature in features.Children<JObject>())
        {
            int segmentIndex = (int)feature["segmentIndex"];
            int crossingCount = points.Children<JObject>().Count(point => (int)point["upperSegment"] == segmentIndex);
            Assert.That(crossingCount, Is.EqualTo((int)feature["crossingCount"]));
        }
    }

    private static void AssertManifestCrossings(PlacementReport report, JObject manifest)
    {
        JObject manifestCrossings = (JObject)manifest["crossings"];
        JArray points = (JArray)manifestCrossings["points"];
        Assert.That(report.target.crossings.Length, Is.EqualTo((int)manifestCrossings["total"]));
        Assert.That(points.Children<JObject>().Count(point => (string)point["kind"] == "self"),
            Is.EqualTo((int)manifestCrossings["self"]));
        Assert.That(points.Children<JObject>().Count(point => (string)point["kind"] == "preserved"),
            Is.EqualTo((int)manifestCrossings["overCurrent"]));

        foreach (JObject point in points.Children<JObject>())
        {
            string manifestKind = (string)point["kind"];
            int x = (int)point["x"];
            int z = (int)point["z"];
            string reportKind = manifestKind == "self" ? "new-new" : "source-new";
            Crossing crossing = report.target.crossings.Single(candidate =>
                candidate.kind == reportKind && CoordinatesEqual(candidate.@abstract, x, z));

            Assert.That(crossing.world, Is.EqualTo(new[]
            {
                report.source.endpointWorld[0] + x * report.source.pitch,
                report.source.endpointWorld[2] + z * report.source.pitch
            }).Within(0.001f));

            int upperSegment = (int)point["upperSegment"];
            RoadPlacement expectedUpper = report.roads.Single(road =>
                road.leg == upperSegment + 1 && CoordinatesEqual(road.@abstract, x, z));
            Assert.That(crossing.upperRoadIndex, Is.EqualTo(expectedUpper.index));

            if (manifestKind == "self")
            {
                int lowerSegment = (int)point["lowerSegment"];
                RoadPlacement expectedLower = report.roads.Single(road =>
                    road.leg == lowerSegment + 1 && CoordinatesEqual(road.@abstract, x, z));
                Assert.That(crossing.lowerRoadIndex, Is.EqualTo(expectedLower.index));
                Assert.That(string.IsNullOrEmpty(crossing.lowerSourceRoadName), Is.True);
            }
            else
            {
                Assert.That((string)point["lowerSegment"], Does.StartWith("current:"));
                Assert.That(crossing.lowerRoadIndex, Is.Zero);
                Assert.That(crossing.lowerSourceRoadName, Is.Not.Empty);
            }
        }

        IGrouping<string, RoadPlacement>[] duplicates = report.roads
            .GroupBy(road => $"{road.@abstract[0]},{road.@abstract[1]}")
            .Where(group => group.Count() > 1)
            .ToArray();
        Crossing[] selfCrossings = report.target.crossings.Where(crossing => crossing.kind == "new-new").ToArray();
        Assert.That(duplicates.Length, Is.EqualTo(selfCrossings.Length),
            "Only the two planned self-crossings may repeat a new-road lattice coordinate.");
        foreach (IGrouping<string, RoadPlacement> duplicate in duplicates)
        {
            RoadPlacement[] roads = duplicate.OrderBy(road => road.index).ToArray();
            Assert.That(roads.Length, Is.EqualTo(2), duplicate.Key + " must be a single crossing, not a retrace.");
            Crossing crossing = selfCrossings.Single(candidate =>
                CoordinatesEqual(candidate.@abstract, roads[0].@abstract[0], roads[0].@abstract[1]));
            Assert.That(new[] { roads[0].index, roads[1].index },
                Is.EqualTo(new[] { crossing.lowerRoadIndex, crossing.upperRoadIndex }));
            Assert.That(IsNorthSouth(roads[0].direction) ^ IsNorthSouth(roads[1].direction), Is.True,
                duplicate.Key + " must pair one north/south road with one east/west road.");
        }
    }

    private static void AssertRouteBounds(PlacementReport report)
    {
        Assert.That(report.target.bounds.minX, Is.EqualTo(report.roads.Min(road => road.world[0])).Within(0.001f));
        Assert.That(report.target.bounds.maxX, Is.EqualTo(report.roads.Max(road => road.world[0])).Within(0.001f));
        Assert.That(report.target.bounds.minZ, Is.EqualTo(report.roads.Min(road => road.world[2])).Within(0.001f));
        Assert.That(report.target.bounds.maxZ, Is.EqualTo(report.roads.Max(road => road.world[2])).Within(0.001f));
    }

    private static void AssertCopiedRoadPrefix(Transform sourceRoads, Transform targetRoads)
    {
        for (int index = 0; index < sourceRoads.childCount; index++)
        {
            Transform source = sourceRoads.GetChild(index);
            Transform target = targetRoads.GetChild(index);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target.gameObject),
                Is.EqualTo(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(source.gameObject)));
            Assert.That(target.position, Is.EqualTo(source.position).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(target.rotation.eulerAngles, Is.EqualTo(source.rotation.eulerAngles).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(target.localScale, Is.EqualTo(source.localScale).Using(Vector3ComparerWithEqualsOperator.Instance));
        }
    }

    private static void AssertGeneratedRoads(PlacementReport report, Transform targetRoads)
    {
        for (int index = 0; index < report.roads.Length; index++)
        {
            RoadPlacement expected = report.roads[index];
            Transform actual = RoadByExtensionIndex(targetRoads, expected.index);
            Assert.That(actual.name, Is.EqualTo($"SR18_Road_{expected.index:000}_{expected.kind}"));
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actual.gameObject), Is.EqualTo(expected.prefabPath));
            AssertVector(actual.position, expected.world, 0.02f, actual.name + " position");
            Assert.That(Mathf.DeltaAngle(actual.eulerAngles.y, expected.yaw), Is.EqualTo(0f).Within(0.1f));
            AssertVector(actual.localScale, expected.scale, 0.02f, actual.name + " scale");
        }
    }

    private static void AssertNamedSourceEndpoint(
        PlacementReport report,
        Transform sourceRoads,
        Transform targetRoads)
    {
        Transform[] sourceMatches = Enumerable.Range(0, sourceRoads.childCount)
            .Select(sourceRoads.GetChild)
            .Where(road => road.name == report.source.endpointRoadName)
            .ToArray();
        Transform[] targetMatches = Enumerable.Range(0, report.source.roadCount)
            .Select(targetRoads.GetChild)
            .Where(road => road.name == report.source.endpointRoadName)
            .ToArray();
        Assert.That(sourceMatches.Length, Is.EqualTo(1), "Source endpoint name must be unique in Map1 roads.");
        Assert.That(targetMatches.Length, Is.EqualTo(1), "Source endpoint name must be unique in SR18 copied roads.");
        Assert.That(targetMatches[0].position,
            Is.EqualTo(sourceMatches[0].position).Using(Vector3ComparerWithEqualsOperator.Instance));
        AssertVector(targetMatches[0].position, report.source.endpointWorld, 0.001f, "Source endpoint world position");
    }

    private static void AssertCrossingGeometry(PlacementReport report, Transform sourceRoads, Transform targetRoads)
    {
        foreach (Crossing crossing in report.target.crossings)
        {
            Transform upper = RoadByExtensionIndex(targetRoads, crossing.upperRoadIndex);
            Transform lower = crossing.kind == "new-new"
                ? RoadByExtensionIndex(targetRoads, crossing.lowerRoadIndex)
                : Enumerable.Range(0, sourceRoads.childCount)
                    .Select(sourceRoads.GetChild)
                    .Single(road => road.name == crossing.lowerSourceRoadName);
            string label = $"{crossing.kind} crossing {crossing.@abstract[0]},{crossing.@abstract[1]}";

            Bounds upperRendererBounds = CalculateRendererBounds(upper);
            Bounds lowerRendererBounds = CalculateRendererBounds(lower);
            AssertBoundsContainXZ(upperRendererBounds, crossing.world, label + " upper renderer");
            AssertBoundsContainXZ(lowerRendererBounds, crossing.world, label + " lower renderer");
            Assert.That(upperRendererBounds.min.y - lowerRendererBounds.max.y, Is.GreaterThan(0.1f),
                label + " renderer vertical clearance");

            bool hasUpperCollider = TryCalculateColliderBounds(upper, out Bounds upperColliderBounds);
            bool hasLowerCollider = TryCalculateColliderBounds(lower, out Bounds lowerColliderBounds);
            Assert.That(hasUpperCollider, Is.True, label + " upper road must have a collider");
            Assert.That(hasLowerCollider, Is.True, label + " lower road must have a collider");
            AssertBoundsContainXZ(upperColliderBounds, crossing.world, label + " upper collider");
            AssertBoundsContainXZ(lowerColliderBounds, crossing.world, label + " lower collider");
            Assert.That(upperColliderBounds.min.y - lowerColliderBounds.max.y, Is.GreaterThan(0.1f),
                label + " collider vertical clearance");
        }
    }

    private static void AssertGeneratedRoadSeams(PlacementReport report, Transform roads)
    {
        Transform sourceEndpoint = Enumerable.Range(0, report.source.roadCount)
            .Select(roads.GetChild)
            .Single(road => road.name == report.source.endpointRoadName);
        AssertRoadSeam(sourceEndpoint, RoadByExtensionIndex(roads, 1));

        for (int firstIndex = report.source.roadCount; firstIndex < roads.childCount - 1; firstIndex++)
        {
            Transform firstRoad = roads.GetChild(firstIndex);
            Transform secondRoad = roads.GetChild(firstIndex + 1);
            AssertRoadSeam(firstRoad, secondRoad);
        }
    }

    private static void AssertRoadSeam(Transform firstRoad, Transform secondRoad)
    {
        Bounds first = CalculateRendererBounds(firstRoad);
        Bounds second = CalculateRendererBounds(secondRoad);
        float gapX = Mathf.Max(0f, Mathf.Max(first.min.x - second.max.x, second.min.x - first.max.x));
        float gapY = Mathf.Max(0f, Mathf.Max(first.min.y - second.max.y, second.min.y - first.max.y));
        float gapZ = Mathf.Max(0f, Mathf.Max(first.min.z - second.max.z, second.min.z - first.max.z));
        float threeDimensionalGap = Mathf.Sqrt(gapX * gapX + gapY * gapY + gapZ * gapZ);
        Assert.That(threeDimensionalGap, Is.LessThanOrEqualTo(0.05f),
            $"Road seam {firstRoad.name}->{secondRoad.name}");
    }

    private static void AssertCopiedTurnSpots(
        NoryangjinTurnSpot[] sourceSpots,
        NoryangjinTurnSpot[] targetSpots)
    {
        NoryangjinTurnSpot[] copied = targetSpots
            .Where(spot => !spot.name.StartsWith("SR18_Turn_", StringComparison.Ordinal))
            .OrderBy(spot => spot.transform.GetSiblingIndex())
            .ToArray();
        sourceSpots = sourceSpots.OrderBy(spot => spot.transform.GetSiblingIndex()).ToArray();
        Assert.That(sourceSpots.Length, Is.EqualTo(5));
        Assert.That(copied.Length, Is.EqualTo(sourceSpots.Length));

        for (int index = 0; index < sourceSpots.Length; index++)
        {
            NoryangjinTurnSpot source = sourceSpots[index];
            NoryangjinTurnSpot target = copied[index];
            Assert.That(target.name, Is.EqualTo(source.name));
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target.gameObject),
                Is.EqualTo(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(source.gameObject)));
            Assert.That(target.transform.position, Is.EqualTo(source.transform.position).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(target.transform.rotation.eulerAngles, Is.EqualTo(source.transform.rotation.eulerAngles).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(target.transform.localScale, Is.EqualTo(source.transform.localScale).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(target.TargetYawDegrees, Is.EqualTo(source.TargetYawDegrees).Within(0.001f));
            Assert.That(target.TargetXDegrees, Is.EqualTo(source.TargetXDegrees).Within(0.001f));
            Assert.That(target.TurnDurationSeconds, Is.EqualTo(source.TurnDurationSeconds).Within(0.001f));
        }
    }

    private static void AssertGeneratedTurnSpots(PlacementReport report, NoryangjinTurnSpot[] allSpots)
    {
        NoryangjinTurnSpot[] generated = allSpots
            .Where(spot => spot.name.StartsWith("SR18_Turn_", StringComparison.Ordinal))
            .OrderBy(spot => spot.name)
            .ToArray();
        Assert.That(generated.Length, Is.EqualTo(10));
        for (int index = 0; index < generated.Length; index++)
        {
            TurnPlacement expected = report.turnSpots[index];
            AssertVector(generated[index].transform.position, expected.world, 0.02f, generated[index].name);
            Assert.That(Mathf.DeltaAngle(generated[index].transform.eulerAngles.y, expected.triggerYaw), Is.EqualTo(0f).Within(0.1f));
            Assert.That(Mathf.DeltaAngle(generated[index].TargetYawDegrees, expected.targetYaw), Is.EqualTo(0f).Within(0.1f));
            Assert.That(generated[index].TargetXDegrees, Is.EqualTo(expected.targetX).Within(0.01f));
            Assert.That(generated[index].TurnDurationSeconds, Is.EqualTo(expected.duration).Within(0.01f));
        }
    }

    private static void AssertRetainedInfrastructure(Scene scene)
    {
        GameObject camera = FindInScene(scene, "MapTool_Camera");
        GameObject light = FindInScene(scene, "MapTool_DirectionalLight");
        Assert.That(camera.GetComponent<Camera>(), Is.Not.Null);
        Assert.That(light.GetComponent<Light>(), Is.Not.Null);
        Assert.That(FindInScene(scene, "MapTool_Work_Floor"), Is.Not.Null);
        Assert.That(FindInScene(scene, "MapTool_Work_Grid"), Is.Not.Null);
        Assert.That(FindInScene(scene, "MapTool_Origin_Post"), Is.Not.Null);
    }

    private static void AssertEmptyPlacementRoot(Transform mapRoot, string name)
    {
        Transform root = mapRoot.Find(name);
        Assert.That(root, Is.Not.Null, name + " root is missing");
        Assert.That(root.childCount, Is.Zero, name + " must be empty in the roads-only scene");
    }

    private static void AssertLoadedSceneIsClean(Scene scene, string label)
    {
        if (scene.IsValid() && scene.isLoaded)
            Assert.That(scene.isDirty, Is.False, label + " has unsaved changes; validation refuses unsaved state.");
    }

    private static NoryangjinTurnSpot[] GetSceneTurnSpots(Transform props, Scene scene)
    {
        return props.GetComponentsInChildren<NoryangjinTurnSpot>(true)
            .Where(spot => spot.gameObject.scene == scene)
            .ToArray();
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().Single(root => root.name == name);
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(transform => transform.name == name)
            .gameObject;
    }

    private static Transform RoadByExtensionIndex(Transform roads, int extensionIndex)
    {
        return roads.GetChild(50 + extensionIndex);
    }

    private static Bounds CalculateRendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty, root.name + " has no renderer");
        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        return bounds;
    }

    private static bool TryCalculateColliderBounds(Transform root, out Bounds bounds)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = colliders[0].bounds;
        for (int index = 1; index < colliders.Length; index++)
            bounds.Encapsulate(colliders[index].bounds);
        return true;
    }

    private static void AssertBoundsContainXZ(Bounds bounds, float[] worldXZ, string label)
    {
        const float Tolerance = 0.05f;
        Assert.That(worldXZ[0], Is.InRange(bounds.min.x - Tolerance, bounds.max.x + Tolerance), label + " X");
        Assert.That(worldXZ[1], Is.InRange(bounds.min.z - Tolerance, bounds.max.z + Tolerance), label + " Z");
    }

    private static PlacementReport LoadReport()
    {
        Assert.That(File.Exists(PlacementReportPath), Is.True);
        PlacementReport report = JsonUtility.FromJson<PlacementReport>(File.ReadAllText(PlacementReportPath));
        Assert.That(report, Is.Not.Null);
        return report;
    }

    private static JObject LoadSr18Manifest()
    {
        Assert.That(File.Exists(RouteManifestPath), Is.True);
        JArray routes = JArray.Parse(File.ReadAllText(RouteManifestPath));
        JObject[] matches = routes.Children<JObject>()
            .Where(route => (string)route["id"] == "super-radical-18")
            .ToArray();
        Assert.That(matches.Length, Is.EqualTo(1), "Manifest must contain exactly one super-radical-18 row.");
        return matches[0];
    }

    private static RouteLeg[] ParseRoute(string routeSpec)
    {
        return routeSpec.Split(',')
            .Select(token => new RouteLeg(
                token.Substring(0, 1),
                int.Parse(token.Substring(1), CultureInfo.InvariantCulture)))
            .ToArray();
    }

    private static ElevationInterval[] RecomputeElevatedIntervals(RoadPlacement[] roads)
    {
        var intervals = new List<ElevationInterval>();
        int start = -1;
        for (int index = 0; index < roads.Length; index++)
        {
            if (roads[index].elevated && start < 0)
                start = roads[index].index;
            if (start >= 0 && (!roads[index].elevated || index == roads.Length - 1))
            {
                int endExclusive = roads[index].elevated ? roads[index].index + 1 : roads[index].index;
                intervals.Add(new ElevationInterval { start = start, endExclusive = endExclusive });
                start = -1;
            }
        }
        return intervals.ToArray();
    }

    private static Vector2Int DirectionStep(string direction)
    {
        return direction switch
        {
            "N" => new Vector2Int(0, 1),
            "E" => new Vector2Int(1, 0),
            "S" => new Vector2Int(0, -1),
            "W" => new Vector2Int(-1, 0),
            _ => throw new AssertionException("Unknown route direction: " + direction)
        };
    }

    private static float DirectionYaw(string direction)
    {
        return direction switch
        {
            "N" => 0f,
            "E" => 90f,
            "S" => 180f,
            "W" => 270f,
            _ => throw new AssertionException("Unknown route direction: " + direction)
        };
    }

    private static string TurnKind(string from, string to)
    {
        float delta = Mathf.Repeat(DirectionYaw(to) - DirectionYaw(from), 360f);
        return delta switch
        {
            90f => "RightTurn",
            270f => "LeftTurn",
            _ => throw new AssertionException($"Route turn must be 90 degrees: {from}->{to}")
        };
    }

    private static bool CoordinatesEqual(int[] coordinates, int x, int z)
    {
        return coordinates is { Length: 2 } && coordinates[0] == x && coordinates[1] == z;
    }

    private static bool IsNorthSouth(string direction)
    {
        return direction is "N" or "S";
    }

    private static void AssertVector(Vector3 actual, float[] expected, float tolerance, string label)
    {
        Assert.That(actual.x, Is.EqualTo(expected[0]).Within(tolerance), label + " X");
        Assert.That(actual.y, Is.EqualTo(expected[1]).Within(tolerance), label + " Y");
        Assert.That(actual.z, Is.EqualTo(expected[2]).Within(tolerance), label + " Z");
    }

    private static string HashFile(string path)
    {
        using SHA256 sha = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private sealed class RouteLeg
    {
        public RouteLeg(string direction, int count)
        {
            Direction = direction;
            Count = count;
        }

        public string Direction { get; }
        public int Count { get; }
    }

#pragma warning disable CS0649 // JsonUtility populates these serialized DTO fields.
    [Serializable] private sealed class PlacementReport { public SourceData source; public TargetData target; public RoadPlacement[] roads; public TurnPlacement[] turnSpots; }
    [Serializable] private sealed class SourceData { public string scenePath; public string sha256; public int roadCount; public int turnSpotCount; public string endpointRoadName; public float[] endpointWorld; public float pitch; }
    [Serializable] private sealed class TargetData
    {
        public string scenePath;
        public bool buildSettingsIncluded;
        public int totalRoadCount;
        public int totalTurnSpotCount;
        public int extensionRoadCount;
        public int extensionTurnSpotCount;
        public string routeSpec;
        public string finalDirection;
        public ElevationInterval[] upperIntervals;
        public RouteBounds bounds;
        public Crossing[] crossings;
        public RoadMix newRoadMix;
    }
    [Serializable] private sealed class ElevationInterval { public int start; public int endExclusive; }
    [Serializable] private sealed class RouteBounds { public float minX; public float maxX; public float minZ; public float maxZ; }
    [Serializable] private sealed class RoadMix { public int Basic; public int Uphill; public int Downhill; public int RightTurn; public int LeftTurn; }
    [Serializable] private sealed class RoadPlacement
    {
        public int index;
        public int leg;
        public int stepInLeg;
        public string direction;
        public int[] @abstract;
        public string kind;
        public string prefabPath;
        public float[] world;
        public float yaw;
        public float[] scale;
        public bool elevated;
    }
    [Serializable] private sealed class TurnPlacement
    {
        public int ordinal;
        public int roadIndex;
        public int[] grid;
        public float[] world;
        public float triggerYaw;
        public float targetYaw;
        public float targetX;
        public float duration;
    }
    [Serializable] private sealed class Crossing
    {
        public string kind;
        public int lowerRoadIndex;
        public string lowerSourceRoadName;
        public int upperRoadIndex;
        public int[] @abstract;
        public float[] world;
    }
#pragma warning restore CS0649
}
#endif
