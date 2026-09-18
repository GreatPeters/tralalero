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

    [Test]
    public void Sr18RoadDecks_SupportThreeLanesAcrossEveryJoinAndRamp()
    {
        Scene scene = SceneManager.GetSceneByPath(Sr18Path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(Sr18Path, OpenSceneMode.Additive);
        try
        {
            Transform roads = FindRoot(scene, "Noryangjin_MapTool").transform.Find("Roads");
            Physics.SyncTransforms();
            PlacementReport report = LoadReport();
            Transform endpoint = roads.Find(report.source.endpointRoadName);
            Vector3 origin = endpoint.TransformPoint(new Vector3(-1.5f, 0f, -4.9f));
            Collider[][] colliders = Enumerable.Range(51, 179)
                .Select(index => roads.GetChild(index).GetComponentsInChildren<Collider>(true)).ToArray();
            var failures = new List<string>();
            int samples = 0;
            for (int index = 0; index < report.roads.Length; index++)
            {
                RoadPlacement road = report.roads[index];
                Vector2Int gridStep = DirectionStep(road.direction);
                Vector3 forward = new Vector3(gridStep.x, 0f, gridStep.y);
                Vector3 side = Vector3.Cross(Vector3.up, forward);
                Vector3 end = origin + new Vector3(road.@abstract[0], 0f, road.@abstract[1]) * report.source.pitch;
                Collider[] nearby = colliders.Skip(Mathf.Max(0, index - 1)).Take(index == 0 ? 2 : 3).SelectMany(items => items)
                    .Concat(index == 0 ? endpoint.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>()).ToArray();
                for (int step = 0; step <= 45; step++)
                {
                    float fraction = step / 45f;
                    float height = Mathf.Lerp(DeckHeightAt(index), DeckHeightAt(index + 1), fraction);
                    foreach (float lane in new[] { -2f, 0f, 2f })
                    {
                        Vector3 point = end - forward * report.source.pitch * (1f - fraction) + side * lane;
                        point.y = height;
                        samples++;
                        bool supported = false;
                        foreach (float nudge in new[] { 0f, -0.06f, 0.06f })
                        {
                            Ray ray = new Ray(point + forward * nudge + Vector3.up * 0.45f, Vector3.down);
                            if (nearby.Any(collider => collider.Raycast(ray, out RaycastHit hit, 0.9f) && hit.normal.y > 0.7f))
                            {
                                supported = true;
                                break;
                            }
                        }
                        if (!supported && failures.Count < 20)
                            failures.Add($"road {index + 1}, t={fraction:F2}, lane={lane}: no deck at {point}");
                    }
                }
            }
            Assert.That(failures, Is.Empty, $"Actual deck support failed among {samples} samples:\n" + string.Join("\n", failures));
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static float DeckHeightAt(int completedRoads)
    {
        if (completedRoads >= 70 && completedRoads <= 72) return (completedRoads - 69) * 4f;
        if (completedRoads >= 73 && completedRoads <= 86) return 12f;
        if (completedRoads >= 87 && completedRoads <= 89) return (89 - completedRoads) * 4f;
        if (completedRoads >= 151 && completedRoads <= 153) return (completedRoads - 150) * 4f;
        if (completedRoads >= 154 && completedRoads <= 155) return 12f;
        if (completedRoads >= 156 && completedRoads <= 158) return (158 - completedRoads) * 4f;
        return 0f;
    }

    [Test]
    public void Sr18Market_LeavesTheExistingTimberRoadsVisible()
    {
        Scene scene = SceneManager.GetSceneByPath(Sr18Path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(Sr18Path, OpenSceneMode.Additive);
        try
        {
            Transform root = FindRoot(scene, "Noryangjin_MapTool").transform;
            Bounds[] roadBounds = root.Find("Roads").Cast<Transform>().Select(CalculateRendererBounds).ToArray();
            var covered = new List<string>();
            var detached = new List<string>();
            foreach (Transform prop in root.Find("Props"))
            {
                string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prop.gameObject);
                if (!prefab.Contains("_BLD_") && !prefab.Contains("049_STAGE01"))
                    continue;
                Bounds b = CalculateRendererBounds(prop);
                if (roadBounds.Any(r => b.min.x < r.max.x && b.max.x > r.min.x &&
                                        b.min.z < r.max.z && b.max.z > r.min.z))
                    covered.Add(prop.name);
                // Every shop/quay belongs beside the existing route, not an invented interior street.
                float nearest = roadBounds.Min(r => Vector2.Distance(
                    new Vector2(b.center.x, b.center.z),
                    new Vector2(Mathf.Clamp(b.center.x, r.min.x, r.max.x),
                        Mathf.Clamp(b.center.z, r.min.z, r.max.z))));
                if (nearest >= 6f)
                    detached.Add(prop.name);
            }
            Assert.That(covered, Is.Empty, "Market must not cover any existing timber road: " +
                string.Join(", ", covered.Take(12)));
            Assert.That(detached, Is.Empty, "Market must follow the existing route: " +
                string.Join(", ", detached.Take(12)));
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

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
    public void Sr18Ramps_UseSlopeSafeShadingWithoutChangingTheirWoodTexture()
    {
        const string sourcePath = "Assets/ShooterSurvival/Materials/Env/pirate/Noryangjin_RoadBasic_SubtleOutline.mat";
        const string slopePath = "Assets/ShooterSurvival/Materials/Generated/SR18_RoadSlope.mat";
        Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
        Scene scene = SceneManager.GetSceneByPath(Sr18Path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(Sr18Path, OpenSceneMode.Additive);
        try
        {
            Transform roads = FindRoot(scene, "Noryangjin_MapTool").transform.Find("Roads");
            Transform[] ramps = roads.Cast<Transform>().Where(t => t.name.EndsWith("_Uphill", StringComparison.Ordinal) ||
                t.name.EndsWith("_Downhill", StringComparison.Ordinal)).ToArray();
            Assert.That(ramps.Length, Is.EqualTo(12));
            foreach (Transform ramp in ramps)
            {
                foreach (Material material in ramp.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials))
                {
                    Assert.That(material.GetFloat("_SelfShadingSize"), Is.EqualTo(0.45f).Within(0.001f), ramp.name);
                    Assert.That(material.GetFloat("_ShadowEdgeSize"), Is.EqualTo(0.08f).Within(0.001f), ramp.name);
                    Assert.That(AssetDatabase.GetAssetPath(material), Is.EqualTo(slopePath));
                    Assert.That(material.shader, Is.EqualTo(source.shader));
                    Assert.That(material.GetTexture("_BaseMap"), Is.EqualTo(source.GetTexture("_BaseMap")));
                    Assert.That(material.GetTextureScale("_BaseMap"), Is.EqualTo(source.GetTextureScale("_BaseMap")));
                    Assert.That(material.GetTextureOffset("_BaseMap"), Is.EqualTo(source.GetTextureOffset("_BaseMap")));
                    Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(source.GetColor("_BaseColor")));
                    Assert.That(material.shaderKeywords, Is.EquivalentTo(source.shaderKeywords));
                }
            }
            // A slope-only correction must not change the material used by flat roads and other maps.
            Assert.That(source.GetFloat("_SelfShadingSize"), Is.EqualTo(0.838f).Within(0.001f));
            Assert.That(roads.Find("SR18_Road_073_Basic").GetComponent<Renderer>().sharedMaterial, Is.EqualTo(source));
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void PlacementReport_MatchesTheSelectedSr18ManifestContract()
    {
        PlacementReport report = LoadReport();
        JObject manifest = LoadSr18Manifest();

        AssertPlacementReportContract(report, manifest);
    }

    [Test]
    public void Sr18TurnSpots_CoverEveryLaneAndTriggerAtTheCornerCenter()
    {
        Scene scene = SceneManager.GetSceneByPath(Sr18Path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(Sr18Path, OpenSceneMode.Additive);
        try
        {
            NoryangjinTurnSpot[] spots = FindRoot(scene, "Noryangjin_MapTool")
                .GetComponentsInChildren<NoryangjinTurnSpot>(true).Where(s => !s.IsSlopeTransition).ToArray();
            PlayerScript player = FindInScene(scene, "Noryangjin_Player").GetComponent<PlayerScript>();
            CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
            Vector3 playerScale = player.transform.lossyScale;
            // This authored capsule is clamped by its diameter, so its world shape is a sphere.
            Assert.That(capsule.direction, Is.EqualTo(1));
            Assert.That(capsule.height, Is.LessThanOrEqualTo(2f * capsule.radius));
            Assert.That(playerScale, Is.EqualTo(Vector3.one * playerScale.x).Using(Vector3ComparerWithEqualsOperator.Instance));
            float radius = capsule.radius * playerScale.x;
            Vector3 centerOffset = Vector3.Scale(capsule.center, playerScale);
            Assert.That(spots.Length, Is.EqualTo(15));

            foreach (NoryangjinTurnSpot spot in spots)
            {
                BoxCollider box = spot.GetComponent<BoxCollider>();
                Assert.That(spot.isActiveAndEnabled && box.enabled && box.isTrigger, Is.True, spot.name);
                Vector3 size = Vector3.Scale(box.size, spot.transform.lossyScale);
                Assert.That(size.x, Is.EqualTo(8f).Within(0.01f));
                Assert.That(size.y, Is.EqualTo(2f).Within(0.01f));
                Assert.That(size.z, Is.EqualTo(0.8f).Within(0.01f));
                Quaternion incomingRotation = spot.transform.rotation;
                Vector3 forward = incomingRotation * Vector3.forward;
                Vector3 side = incomingRotation * Vector3.right;
                foreach (float lane in new[] { -3.3f, -1.65f, 0f, 1.65f, 3.3f })
                {
                    float? entry = null;
                    for (int step = 0; step <= 100; step++)
                    {
                        float distance = -1f + step * 0.02f;
                        Vector3 center = spot.transform.position + Vector3.up * 0.12f +
                            side * lane + forward * distance + incomingRotation * centerOffset;
                        if (SphereIntersectsTurnBox(center, radius, box))
                        {
                            entry = distance;
                            break;
                        }
                    }
                    Assert.That(entry.HasValue, Is.True, spot.name + " lane " + lane + " misses the trigger");
                    Assert.That(entry.Value, Is.InRange(-0.1f, 0.02f), spot.name + " turns before/after the corner");
                }
                Vector3 atOtherDeck = spot.transform.position + Vector3.up * 12.12f + incomingRotation * centerOffset;
                Assert.That(SphereIntersectsTurnBox(atOtherDeck, radius, box), Is.False, spot.name + " reaches the other deck");
            }
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static bool SphereIntersectsTurnBox(Vector3 center, float radius, BoxCollider box)
    {
        Vector3 local = box.transform.InverseTransformPoint(center) - box.center;
        Vector3 half = box.size * 0.5f;
        Vector3 closest = box.transform.TransformPoint(box.center + new Vector3(
            Mathf.Clamp(local.x, -half.x, half.x), Mathf.Clamp(local.y, -half.y, half.y),
            Mathf.Clamp(local.z, -half.z, half.z)));
        return (center - closest).sqrMagnitude <= radius * radius;
    }

    [Test]
    public void Sr18SlopeSpots_MatchEveryRampBoundaryWithoutAddingRouteCheckpoints()
    {
        Scene scene = SceneManager.GetSceneByPath(Sr18Path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(Sr18Path, OpenSceneMode.Additive);
        try
        {
            Transform map = FindRoot(scene, "Noryangjin_MapTool").transform;
            NoryangjinTurnSpot[] all = map.GetComponentsInChildren<NoryangjinTurnSpot>(true);
            NoryangjinTurnSpot[] slopes = all.Where(s => s.IsSlopeTransition).OrderBy(s => s.name).ToArray();
            Assert.That(all.Length, Is.EqualTo(23));
            Assert.That(slopes.Length, Is.EqualTo(8));
            int[] followingRoads = { 70, 73, 87, 90, 151, 154, 156, 159 };
            for (int i = 0; i < slopes.Length; i++)
            {
                Transform before = map.Find("Roads").GetChild(50 + followingRoads[i] - 1);
                Transform after = map.Find("Roads").GetChild(50 + followingRoads[i]);
                Vector3 direction = after.position - before.position;
                float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                Assert.That(Vector3.Distance(slopes[i].transform.position, before.position), Is.LessThan(0.01f));
                Assert.That(slopes[i].TargetXDegrees, Is.EqualTo(pitch).Within(0.01f));
                Assert.That(slopes[i].TurnDurationSeconds, Is.EqualTo(0.25f));
                Assert.That(slopes[i].GetComponent<BoxCollider>().size, Is.EqualTo(new Vector3(8, 2, 0.8f)));
            }
            Assert.That(ChapterEnemyProgression.CollectRouteTurns(scene).Count, Is.EqualTo(15));
            Assert.That(NoryangjinTurnSpot.TryGetRouteProgress(scene, out int completed, out int total), Is.True);
            Assert.That(total, Is.EqualTo(15));
            Assert.That(completed, Is.Zero);
            var follower = FindInScene(scene, "Noryangjin_Player").GetComponent<NoryangjinRoadHeightFollower>();
            Assert.That(follower, Is.Not.Null);
            Assert.That(follower.RoadRoot, Is.EqualTo(map.Find("Roads")));
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void Sr18HeightFollower_TraversesBothElevationRunsAndStaysOnTheCurrentDeck()
    {
        Scene scene = SceneManager.GetSceneByPath(Sr18Path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(Sr18Path, OpenSceneMode.Additive);
        try
        {
            Transform roads = FindRoot(scene, "Noryangjin_MapTool").transform.Find("Roads");
            var follower = FindInScene(scene, "Noryangjin_Player").GetComponent<NoryangjinRoadHeightFollower>();
            Assert.That(follower, Is.Not.Null);
            Physics.SyncTransforms();
            foreach (var span in new[] { (First: 69, Last: 90), (First: 150, Last: 159) })
            foreach (float lane in new[] { -2f, 0f, 2f })
            {
                float height = 0.12f;
                for (int index = span.First; index <= span.Last; index++)
                {
                    Vector3 from = roads.GetChild(50 + index - 1).position;
                    Vector3 to = roads.GetChild(50 + index).position;
                    Vector3 forward = Vector3.ProjectOnPlane(to - from, Vector3.up).normalized;
                    Vector3 side = Vector3.Cross(Vector3.up, forward);
                    for (int step = 1; step <= 45; step++)
                    {
                        float t = step / 45f;
                        Vector3 proposed = Vector3.Lerp(from, to, t) + side * lane;
                        proposed.y = height;
                        Assert.That(follower.TryProjectPosition(proposed, forward, out Vector3 supported), Is.True,
                            $"road {index}, lane {lane}, step {step}");
                        float expected = Mathf.Lerp(DeckHeightAt(index - 1), DeckHeightAt(index), t) + 0.12f;
                        Assert.That(supported.y, Is.EqualTo(expected).Within(0.2f));
                        height = supported.y;
                    }
                }
                Assert.That(height, Is.EqualTo(0.12f).Within(0.2f));
            }
            // The first upper crossing lies over an earlier ground-level path.
            Vector3 crossing = roads.GetChild(50 + 85).position;
            crossing.y = 0.12f;
            Assert.That(follower.TryProjectPosition(crossing, Vector3.back, out Vector3 lower), Is.True);
            Assert.That(lower.y, Is.LessThan(0.5f));
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void Sr18Scene_PreservesTheRouteWithTheApprovedDenseMarket()
    {
        Assert.That(File.Exists(Sr18Path), Is.True, "SR18 scene has not been baked yet.");
        // The report's hash identifies its historical input; later authorized UI edits
        // must not invalidate the geometric copy contract checked below.
        string map1Before = HashFile(Map1Path);
        Assert.That(HashFile(Map2Path), Is.EqualTo(Map2ReviewedSha256), "Reviewed Map2 baseline changed.");
        string sr18Before = HashFile(Sr18Path);

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
            AssertCopiedTurnSpots(sourceTurnSpots, targetTurnSpots);
            AssertGeneratedTurnSpots(report, targetTurnSpots);
            AssertDenseMarket(sr18, targetRoot, targetProps);

            AssertEncounterLayout(targetRoot);
            Assert.That(targetRoot.Find("Water").childCount, Is.EqualTo(1));
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
            Assert.That(buildScenes, Does.Contain(Sr18Path));
            Assert.That(buildScenes, Does.Contain(HighwaySceneBuilder.ScenePath));
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded)
                EditorSceneManager.SetActiveScene(previousActive);
            if (openedSr18 && sr18.IsValid() && sr18.isLoaded)
                EditorSceneManager.CloseScene(sr18, true);
            if (openedMap1 && map1.IsValid() && map1.isLoaded)
                EditorSceneManager.CloseScene(map1, true);

            Assert.That(HashFile(Map1Path), Is.EqualTo(map1Before), "Validation must not write Map1.");
            Assert.That(HashFile(Map2Path), Is.EqualTo(Map2ReviewedSha256));
            Assert.That(HashFile(Sr18Path), Is.EqualTo(sr18Before), "Validation must not write the authored scene.");
            if (previousActive.IsValid() && previousActive.isLoaded)
                Assert.That(previousActive.isDirty, Is.EqualTo(previousDirty));
        }
    }

    [Test]
    public void Sr18Encounters_UseRealScaleLinkedEventsAndRoadHeight()
    {
        Scene scene = SceneManager.GetSceneByPath(Sr18Path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(Sr18Path, OpenSceneMode.Additive);
        try
        {
            AssertLoadedSceneIsClean(scene, "SR18");
            Transform root = FindRoot(scene, "Noryangjin_MapTool").transform;
            AssertEncounterLayout(root);
            Physics.SyncTransforms();
            Collider[] flatRoads = root.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
            Transform[] placements = root.Find("Enemies").Cast<Transform>()
                .Concat(root.Find("Bonuses").Cast<Transform>())
                .Concat(root.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>().Select(s => s.transform))
                .Concat(root.Find("SR18_EnemyTargets").Cast<Transform>()).ToArray();
            foreach (Transform placed in placements)
            {
                Assert.That(placed.position.y, Is.InRange(-.2f, 12.5f), placed.name);
                bool supported = new[] { Vector3.zero, Vector3.forward * .12f, Vector3.back * .12f,
                    Vector3.left * .12f, Vector3.right * .12f }.Any(offset =>
                    flatRoads.Any(c => c.Raycast(new Ray(placed.position + offset + Vector3.up * .4f,
                        Vector3.down), out RaycastHit hit, .8f) && hit.normal.y > .7f));
                Assert.That(supported, Is.True, placed.name + " requires its own deck, not the other crossing level");
            }
            CapsuleCollider[] bodies = root.Find("Enemies").GetComponentsInChildren<EnemyEventController>()
                .Select(e => e.GetComponent<CapsuleCollider>()).ToArray();
            for (int first = 0; first < bodies.Length; first++)
                for (int second = first + 1; second < bodies.Length; second++)
                    Assert.That(bodies[first].bounds.Intersects(bodies[second].bounds), Is.False,
                        bodies[first].name + " / " + bodies[second].name);
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void Sr18Encounters_ActivationGatesCoverLanesAndNudgedWallsKeepTheirDeck()
    {
        Scene scene = SceneManager.GetSceneByPath(Sr18Path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(Sr18Path, OpenSceneMode.Additive);
        try
        {
            AssertLoadedSceneIsClean(scene, "SR18");
            Transform root = FindRoot(scene, "Noryangjin_MapTool").transform;
            Physics.SyncTransforms();
            foreach (EnemyEventActivationSpot spot in root.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>())
            {
                BoxCollider box = spot.GetComponent<BoxCollider>();
                Assert.That(box.isTrigger && box.enabled, Is.True, spot.name);
                Assert.That(Vector3.Scale(box.size, spot.transform.lossyScale),
                    Is.EqualTo(new Vector3(8.8f, 2f, 1.2f)).Using(Vector3ComparerWithEqualsOperator.Instance));
                foreach (float lane in new[] { -2f, 0f, 2f })
                {
                    Vector3 point = spot.transform.position + spot.transform.right * lane + Vector3.up;
                    Assert.That(Vector3.Distance(point, box.ClosestPoint(point)), Is.LessThan(.01f), spot.name);
                }
                if (spot.transform.position.y > 6)
                {
                    Vector3 below = spot.transform.position + Vector3.down * 11;
                    Assert.That(Vector3.Distance(below, box.ClosestPoint(below)), Is.GreaterThan(8), spot.name);
                }
            }
            var pairs = root.Find("Bonuses").GetComponentsInChildren<BonusWallChoicePair>(true);
            Assert.That(pairs.Length, Is.EqualTo(25));
            foreach (var pair in pairs)
            {
                Assert.That(pair.IsConfigured, Is.True, pair.name);
                Assert.That(Vector3.Distance(pair.Left.transform.position, pair.Right.transform.position), Is.EqualTo(3.8f).Within(.01f));
                Vector3 side = (pair.Right.transform.position - pair.Left.transform.position).normalized;
                var leftBox = pair.Left.GetComponentInChildren<BoxCollider>();
                var rightBox = pair.Right.GetComponentInChildren<BoxCollider>();
                float Half(BoxCollider box)
                {
                    Vector3 e = box.size * .5f;
                    return Mathf.Abs(Vector3.Dot(side, box.transform.TransformVector(new Vector3(e.x, 0, 0)))) +
                        Mathf.Abs(Vector3.Dot(side, box.transform.TransformVector(new Vector3(0, e.y, 0)))) +
                        Mathf.Abs(Vector3.Dot(side, box.transform.TransformVector(new Vector3(0, 0, e.z))));
                }
                float gap = Vector3.Dot(rightBox.transform.TransformPoint(rightBox.center) - leftBox.transform.TransformPoint(leftBox.center), side) - Half(leftBox) - Half(rightBox);
                Assert.That(gap, Is.GreaterThan(1.48f), pair.name + " needs a real neutral lane between choices");
            }
            foreach (var expected in new[] { (Time: "T161", X: -19.725851f), (Time: "T177", X: 115.437282f), (Time: "T269", X: 312.827708f) })
            {
                var wall = root.Find("Bonuses").Cast<Transform>().Single(t => t.name.EndsWith(expected.Time));
                Assert.That(wall.position.x, Is.EqualTo(expected.X).Within(.001f));
                Assert.That(wall.position.y, Is.GreaterThan(10), "Never put the user's elevated wall on the lower crossing");
            }
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void AssertEncounterLayout(Transform root)
    {
        Transform enemies = root.Find("Enemies"), bonuses = root.Find("Bonuses");
        EnemyEventController[] controllers = enemies.GetComponentsInChildren<EnemyEventController>(true);
        AuthoredBonusWall[] altars = bonuses.GetComponentsInChildren<AuthoredBonusWall>(true);
        EnemyEventActivationSpot[] spots = root.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>(true);
        Assert.That(enemies.childCount, Is.EqualTo(27));
        Assert.That(controllers.Length, Is.EqualTo(27));
        Assert.That(bonuses.childCount, Is.EqualTo(50));
        Assert.That(altars.Length, Is.EqualTo(50));
        Assert.That(spots.Length, Is.EqualTo(27));
        Assert.That(root.Find("SR18_EnemyTargets").childCount, Is.EqualTo(10));
        Assert.That(spots.SelectMany(s => s.Targets).ToArray(), Is.EquivalentTo(controllers));
        Assert.That(controllers.Count(c => EnemyEventController.RequiresTarget(c.EventMode)), Is.EqualTo(10));
        Assert.That(controllers.Count(c => c.EventMode == EnemyEventMode.PatrolBetweenStartAndTarget), Is.EqualTo(5));
        Assert.That(controllers.Count(c => c.EventMode == EnemyEventMode.AmbushMoveThenShoot), Is.EqualTo(5));
        Assert.That(spots.All(s => s.Targets.Length == 1), Is.True);
        foreach (EnemyEventController enemy in controllers)
        {
            var clearance = enemy.GetComponent<EnemyCornerClearance>();
            Assert.That(clearance, Is.Not.Null, enemy.name);
            Assert.That(clearance.DistanceAfterCorner(enemy.transform.position), Is.GreaterThanOrEqualTo(27.98f), enemy.name);
            if (enemy.HasUsableTarget) Assert.That(clearance.DistanceAfterCorner(enemy.TargetPoint.position), Is.GreaterThanOrEqualTo(27.98f), enemy.name);
            var gate = spots.Single(s => s.Targets.Contains(enemy));
            Assert.That(clearance.DistanceAfterCorner(gate.transform.position), Is.GreaterThanOrEqualTo(19.98f), gate.name);
            Assert.That(enemy.gameObject.activeSelf && enemy.enabled, Is.True, enemy.name);
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(enemy.gameObject), Is.EqualTo(PrefabInstanceStatus.Connected));
            float scale = (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemy.gameObject).EndsWith("Enemy_Woman.prefab") ? 2.5f : 2.25f) * .75f;
            Assert.That(enemy.transform.localScale, Is.EqualTo(Vector3.one * scale).Using(Vector3ComparerWithEqualsOperator.Instance));
            if (EnemyEventController.RequiresTarget(enemy.EventMode))
            {
                Assert.That(enemy.HasUsableTarget, Is.True, enemy.name);
                Assert.That(enemy.TargetPoint.parent, Is.EqualTo(root.Find("SR18_EnemyTargets")), enemy.name);
                Assert.That(enemy.MoveSpeed, Is.GreaterThan(0), enemy.name);
            }
        }
        Assert.That(altars.Count(a => a.Rarity == Rarity.Normal), Is.EqualTo(36));
        Assert.That(altars.Count(a => a.Rarity == Rarity.Rare), Is.EqualTo(12));
        Assert.That(altars.Count(a => a.Rarity == Rarity.Unique), Is.EqualTo(2));
        foreach (AuthoredBonusWall altar in altars)
        {
            Assert.That(Vector3.Distance(altar.transform.localScale, new Vector3(2.25f, 2.9f, 2.9f)), Is.LessThan(.001f));
            Assert.That(altar.ChoicePair, Is.Not.Null);
            Assert.That(altar.Wall.rarity, Is.EqualTo(altar.Rarity));
            Assert.That(altar.Wall.GetComponent<RuntimeBonusWall>().RemoveWhenPreparingStage, Is.False, altar.name);
        }
        var hazards = root.Find("Props").Cast<Transform>().Where(t => t.name.StartsWith("SR18_L_G")).ToArray();
        Assert.That(hazards.Length, Is.EqualTo(24));
        Physics.SyncTransforms();
        foreach (var hazard in hazards)
        {
            Assert.That(hazard.gameObject.activeInHierarchy, Is.True, hazard.name);
            var active = hazard.GetComponentsInChildren<ObstacleStats>();
            Assert.That(active.Length, Is.EqualTo(hazard.name.EndsWith("Bucket") ? 3 : 1), hazard.name + " retains its authored parts");
            if(active[0].obstaclePattern==ObstaclePattern.Seagull)
            {
                Assert.That(active[0].GetComponent<BoxCollider>().enabled,Is.False,"Only the descending bird may hit the player.");
                Assert.That(active[0].balloon,Is.Not.Null);Assert.That(active[0].shadowSprite,Is.Not.Null);
                continue;
            }
            var box = active[0].GetComponent<BoxCollider>();
            Assert.That(box.enabled && box.isTrigger, Is.True, hazard.name);
            // A player capsule (radius .69) must fit through at least one permitted lateral edge.
            // EditMode can omit Rigidbody-owned shapes from PhysX (zero bounds).
            // Inspect the authored oriented box; the PlayMode probe checks actual registered shapes.
            Vector3 center = box.transform.TransformPoint(box.center);
            float side = Vector3.Dot(center - hazard.position, hazard.right);
            float authoredOffset = int.Parse(hazard.name.Substring(8, 2)) % 2 == 0 ? .8f : -.8f;
            Vector3 laneOrigin = center - hazard.right * (side + authoredOffset);
            float clearance = new[] { -2f, 2f }.Max(x =>
            {
                Vector3 lane = laneOrigin + hazard.right * x;
                Vector3 local = box.transform.InverseTransformPoint(lane) - box.center;
                Vector3 ext = box.size * .5f;
                Vector3 closest = box.transform.TransformPoint(box.center + new Vector3(
                    Mathf.Clamp(local.x, -ext.x, ext.x), Mathf.Clamp(local.y, -ext.y, ext.y), Mathf.Clamp(local.z, -ext.z, ext.z)));
                return Vector3.Distance(lane, closest);
            });
            Assert.That(clearance, Is.GreaterThan(.72f), hazard.name + " blocks both available lane edges");
        }
        var report = JObject.Parse(File.ReadAllText(NoryangjinSr18LatestEncounters.RecordPath + "/placement-report.json"));
        var planned = JObject.Parse(File.ReadAllText(NoryangjinSr18LatestEncounters.PlanPath))["events"].Children<JObject>().ToArray();
        var installed = report["placements"].Children<JObject>().ToArray();
        Assert.That(installed.Length, Is.EqualTo(74));
        foreach (var item in planned)
        {
            var row = installed.Single(r => (int)r["time"] == (int)item["time"]);
            Assert.That((string)row["kind"], Is.EqualTo((string)item["kind"]));
            Assert.That((string)row["role"], Is.EqualTo((string)item["enemy_role"]));
            Assert.That((float)item["time"], Is.GreaterThanOrEqualTo(5));
        }
        foreach (var gate in report["activationSpots"].Children<JObject>())
        {
            var enemy = controllers.Single(c => c.name == (string)gate["enemy"]);
            if (enemy.EventMode == EnemyEventMode.AmbushMoveThenShoot)
                Assert.That((float)gate["ahead"], Is.GreaterThanOrEqualTo(30), enemy.name + " must emerge in front, never beside player");
        }
        var latest = JObject.Parse(File.ReadAllText(NoryangjinSr18ChoiceLayout.RecordPath + "/applied.json"));
        var opening = JObject.Parse(File.ReadAllText("map-concepts/sr18-opening-rhythm-2026-09-10/applied.json"));
        var openingCenters = opening["centerOverrides"].Children<JObject>().ToDictionary(r => (string)r["id"]);
        var contacts = JObject.Parse(File.ReadAllText("map-concepts/sr18-contact-pairs-2026-09-10/applied.json"));
        foreach (var row in contacts["centerOverrides"].Children<JObject>()) openingCenters[(string)row["id"]] = row;
        var presentation = JObject.Parse(File.ReadAllText("map-concepts/sr18-presentation-progression-2026-09-10/placements.json"));
        foreach (var row in presentation["centerOverrides"].Children<JObject>()) openingCenters[(string)row["id"]] = row;
        var refinement=JObject.Parse(File.ReadAllText("map-concepts/noryangjin-refinement-2026-09-11/changes.json"))["replacements"].Children<JObject>().ToDictionary(r=>(string)r["previous"]);
        float previousStation = float.NegativeInfinity;
        foreach (var row in latest["events"].Children<JObject>())
        {
            float distance = (float)row["routeDistance"];
            Assert.That(distance - previousStation, Is.GreaterThanOrEqualTo(23.98f), (string)row["id"]);
            previousStation = distance;
            string kind = (string)row["kind"];
            refinement.TryGetValue((string)row["id"],out var replacement);
            var placed = root.Find(kind == "enemy" ? "Enemies" : kind == "bonus" ? "Bonuses" : "Props").Find(replacement!=null?(string)replacement["id"]:(string)row["id"]);
            Vector3 center = placed.position;
            if (kind == "bonus") center = (placed.GetComponent<BonusWallChoicePair>().Left.transform.position + placed.GetComponent<BonusWallChoicePair>().Right.transform.position) * .5f;
            if (kind == "enemy")
            {
                var enemy = placed.GetComponent<EnemyEventController>();
                if (enemy.EventMode == EnemyEventMode.PatrolBetweenStartAndTarget) center = (center + enemy.TargetPoint.position) * .5f;
                if (enemy.EventMode == EnemyEventMode.AmbushMoveThenShoot) center = enemy.TargetPoint.position;
            }
            var expected = replacement ?? (openingCenters.TryGetValue(placed.name, out var changed) ? changed : row);
            AssertVector(center, expected["center"].ToObject<float[]>(), .03f, placed.name + " actual reflow center");
        }
    }

    private static void AssertDenseMarket(Scene scene, Transform root, Transform props)
    {
        const string appliedPath = "map-concepts/sr18-roadside-market-2026-09-05/applied-layout.json";
        JObject applied = JObject.Parse(File.ReadAllText(appliedPath));
        Transform[] shops = props.Cast<Transform>().Where(t =>
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("_BLD_")).ToArray();
        Transform[] ground = props.Cast<Transform>().Where(t =>
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("049_STAGE01")).ToArray();
        Assert.That(shops.Length, Is.EqualTo(306));
        Assert.That(ground.Length, Is.EqualTo((int)applied["groundCount"]));
        Assert.That(props.Cast<Transform>().Count(t=>!t.name.StartsWith("SR18_Polish_")), Is.EqualTo(717));
        Assert.That(props.Cast<Transform>().Count(t => t.GetComponent<EnemyEventActivationSpot>() == null && !t.name.StartsWith("SR18_L_G") && !t.name.StartsWith("SR18_Polish_")), Is.EqualTo(666));
        Assert.That((bool)applied["roadsideOnly"], Is.True);
        Assert.That(((JArray)applied["placements"]).Count, Is.EqualTo(612));

        foreach (JObject expected in ((JArray)applied["placements"]).Children<JObject>())
        {
            string name = (string)expected["name"];
            Transform actual = props.Find(name) ?? root.Find("Water").Find(name);
            Assert.That(actual, Is.Not.Null, name);
            Assert.That(actual.gameObject.scene, Is.EqualTo(scene));
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(actual.gameObject), Is.EqualTo(PrefabInstanceStatus.Connected), name);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actual.gameObject), Is.EqualTo((string)expected["prefab"]));
            AssertVector(actual.position, expected["position"].ToObject<float[]>(), 0.02f, name + " position");
            AssertVector(actual.localScale, expected["scale"].ToObject<float[]>(), 0.02f, name + " scale");
            float[] angles = expected["rotation"].ToObject<float[]>();
            Assert.That(Quaternion.Angle(actual.rotation, Quaternion.Euler(angles[0], angles[1], angles[2])), Is.LessThan(0.1f), name);
        }

        Bounds[] floors = ground.Select(CalculateRendererBounds).ToArray();
        foreach (Transform shop in shops)
        {
            Bounds b = CalculateRendererBounds(shop);
            // The narrow roadside quay supports the full storefront, including its awning.
            Vector3[] points = { b.center,
                new Vector3(b.min.x, 0, b.min.z),
                new Vector3(b.max.x, 0, b.min.z),
                new Vector3(b.min.x, 0, b.max.z),
                new Vector3(b.max.x, 0, b.max.z) };
            Assert.That(points.All(p => floors.Any(f =>
                p.x >= f.min.x - 0.05f && p.x <= f.max.x + 0.05f &&
                p.z >= f.min.z - 0.05f && p.z <= f.max.z + 0.05f)), Is.True, shop.name + " footing");
            foreach (Renderer renderer in shop.GetComponentsInChildren<Renderer>(true))
                foreach (Material material in renderer.sharedMaterials)
                {
                    Assert.That(AssetDatabase.GetAssetPath(material), Does.StartWith("Assets/ShooterSurvival/Materials/Generated/SR18DenseMarket/"));
                    Assert.That(material.GetFloat("_OutlineWidth"), Is.Zero);
                }
        }
        Transform water = root.Find("Water").Find("Background_Water");
        Assert.That(water, Is.Not.Null);
        Assert.That(CalculateRendererBounds(water).size.x, Is.EqualTo(920f).Within(0.1f));
        Assert.That(CalculateRendererBounds(water).size.z, Is.EqualTo(1050f).Within(0.1f));
        Assert.That(water.GetComponentsInChildren<Collider>(true).Any(c => c.enabled), Is.False);
    }

    private static void AssertPlacementReportContract(PlacementReport report, JObject manifest)
    {
        Assert.That((string)manifest["id"], Is.EqualTo("super-radical-18"));
        Assert.That(report.source.scenePath, Is.EqualTo(Map1Path));
        Assert.That(report.source.sha256, Is.EqualTo(Map1ReviewedSha256));
        Assert.That(File.Exists(Map1Path), Is.True);
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
                    Is.EqualTo(report.target.routeOriginWorld[0] + x * report.source.pitch).Within(0.001f),
                    $"Road {expectedIndex} world X");
                Assert.That(road.world[2],
                    Is.EqualTo(report.target.routeOriginWorld[2] + z * report.source.pitch).Within(0.001f),
                    $"Road {expectedIndex} world Z");
                Assert.That(road.yaw, Is.EqualTo(DirectionYaw(leg.Direction)).Within(0.01f), $"Road {expectedIndex} yaw");

                string expectedKind = ExpectedRoadKind(legs, legIndex, stepInLeg, expectedIndex);
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
            Assert.That(turn.world[0], Is.EqualTo(report.target.routeOriginWorld[0] + x * report.source.pitch).Within(0.001f));
            Assert.That(turn.world[1], Is.EqualTo(report.source.endpointWorld[1]).Within(0.001f));
            Assert.That(turn.world[2], Is.EqualTo(report.target.routeOriginWorld[2] + z * report.source.pitch).Within(0.001f));
        }

        Assert.That(roadCursor, Is.EqualTo(report.roads.Length));
        Assert.That(report.target.finalDirection, Is.EqualTo(legs[^1].Direction));
        Assert.That(report.target.finalDirection, Is.EqualTo((string)manifest["highwayDirection"]));
        Assert.That(report.roads[^1].direction, Is.EqualTo(report.target.finalDirection));
        Assert.That(report.roads[^1].@abstract, Is.EqualTo(new[] { x, z }));
    }

    private static string ExpectedRoadKind(
        RouteLeg[] legs,
        int legIndex,
        int stepInLeg,
        int roadIndex)
    {
        if (legIndex > 0 && stepInLeg == 1)
            return TurnKind(legs[legIndex - 1].Direction, legs[legIndex].Direction);

        if (DeckHeightAt(roadIndex) > DeckHeightAt(roadIndex - 1))
            return "Uphill";
        if (DeckHeightAt(roadIndex) < DeckHeightAt(roadIndex - 1))
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
            Assert.That(interval.endExclusive, Is.EqualTo((int)feature["endModule"] + 2),
                "The physical three-module descent extends two modules beyond the concept's single descent.");
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
                report.target.routeOriginWorld[0] + x * report.source.pitch,
                report.target.routeOriginWorld[2] + z * report.source.pitch
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
            Mesh mesh = actual.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(AssetDatabase.GetAssetPath(mesh), Is.EqualTo(expected.surfaceMeshPath));
            Assert.That(actual.GetComponent<MeshCollider>().sharedMesh, Is.SameAs(mesh),
                actual.name + " must collide with the visible deck, not a different bounding shape.");
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
        AssertVector(targetMatches[0].TransformPoint(new Vector3(-1.5f, 0f, -4.9f)),
            report.target.routeOriginWorld, 0.001f, "Actual source deck exit");
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
            .Where(spot => spot.gameObject.scene == scene && !spot.IsSlopeTransition)
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
        public float[] routeOriginWorld;
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
        public string surfaceMeshPath;
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
