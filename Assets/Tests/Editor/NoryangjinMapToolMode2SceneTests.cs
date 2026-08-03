#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NoryangjinMapToolMode2SceneTests
{
    private const string Map1Path =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";
    private const string Map2Path =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity";
    private const string Map1Guid = "594a2107b4741084ea468072f4b4642a";
    private const string Map2Guid = "e07c347ef8dc34844882b8b0c4fd1763";
    private const string Map1Hash =
        "2B590C9D07B1B67E1821DA4DC03E3767469F4BA37FE175BC45C40C83F686B957";

    [Test]
    public void Map1_RemainsTheExactProtectedSourceScene()
    {
        Assert.That(AssetDatabase.AssetPathToGUID(Map1Path), Is.EqualTo(Map1Guid));
        Assert.That(HashFile(Map1Path), Is.EqualTo(Map1Hash));
    }

    [Test]
    public void Map2_IsTheAuthoredCurrentMap1CopyWithCorrectedCampaignLayout()
    {
        Scene previousActive = SceneManager.GetActiveScene();
        bool previousDirty = previousActive.IsValid() && previousActive.isDirty;
        Scene map1 = SceneManager.GetSceneByPath(Map1Path);
        bool openedMap1 = !map1.IsValid() || !map1.isLoaded;
        Scene map2 = SceneManager.GetSceneByPath(Map2Path);
        bool openedMap2 = !map2.IsValid() || !map2.isLoaded;

        if (openedMap1)
            map1 = EditorSceneManager.OpenScene(Map1Path, OpenSceneMode.Additive);
        if (openedMap2)
            map2 = EditorSceneManager.OpenScene(Map2Path, OpenSceneMode.Additive);

        try
        {
            Assert.That(AssetDatabase.AssetPathToGUID(Map2Path), Is.EqualTo(Map2Guid));
            Assert.That(map1.rootCount, Is.EqualTo(9));
            Assert.That(map2.rootCount, Is.EqualTo(9));
            CollectionAssert.AreEquivalent(
                map1.GetRootGameObjects().Select(root => root.name),
                map2.GetRootGameObjects().Select(root => root.name));

            Transform map1Root = FindRoot(map1, "Noryangjin_MapTool").transform;
            Transform map2Root = FindRoot(map2, "Noryangjin_MapTool").transform;
            Transform map1Roads = map1Root.Find("Roads");
            Transform map2Roads = map2Root.Find("Roads");
            Transform map1Props = map1Root.Find("Props");
            Transform map2Props = map2Root.Find("Props");

            Assert.That(map1Roads.childCount, Is.EqualTo(24));
            Assert.That(map1Props.childCount, Is.EqualTo(162));
            Assert.That(map2Roads.childCount, Is.EqualTo(150));
            Assert.That(map2Props.childCount, Is.EqualTo(511));

            AssertCopiedPrefix(map1Roads, map2Roads);
            AssertCopiedPrefix(map1Props, map2Props);

            Transform[] roads = DirectChildren(map2Roads);
            Transform[] props = DirectChildren(map2Props);
            Assert.That(roads.Count(child => child.name.StartsWith("Mode2_Main_")), Is.EqualTo(118));
            Assert.That(roads.Count(child => child.name.StartsWith("Mode2_Branch_")), Is.EqualTo(8));
            Assert.That(props.Count(child => child.name.StartsWith("Mode2_Water_")), Is.EqualTo(196));
            Assert.That(props.Count(child => child.name.StartsWith("Mode2_Quay_")), Is.EqualTo(20));
            Assert.That(props.Count(child => child.name.StartsWith("Mode2_Market_")), Is.EqualTo(123));
            Assert.That(props.Count(child => child.name.StartsWith("Mode2_Highway_")), Is.EqualTo(4));
            Assert.That(props.Count(child => child.name.StartsWith("Mode2_TurnSpot_")), Is.EqualTo(6));

            AssertCorrectedRouteGeometry(roads);
            AssertTurnSpots(map2);
            AssertHighwayContact(props, roads);
            AssertAuthoredEnvironment(props, roads);

            Assert.That(FindRoot(map2, "Noryangjin_Player"), Is.Not.Null);
            Assert.That(FindRoot(map2, "Managers"), Is.Not.Null);
            Assert.That(FindRoot(map2, "Canvas"), Is.Not.Null);
            Assert.That(FindRoot(map2, "EventSystem"), Is.Not.Null);
            Assert.That(FindRoot(map2, "UpgradeServices"), Is.Not.Null);

            string[] buildScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            Assert.That(buildScenes, Does.Contain(Map1Path));
            Assert.That(buildScenes, Does.Not.Contain(Map2Path));
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded)
                EditorSceneManager.SetActiveScene(previousActive);
            if (openedMap2 && map2.IsValid() && map2.isLoaded)
                EditorSceneManager.CloseScene(map2, true);
            if (openedMap1 && map1.IsValid() && map1.isLoaded)
                EditorSceneManager.CloseScene(map1, true);

            Assert.That(HashFile(Map1Path), Is.EqualTo(Map1Hash));
            if (previousActive.IsValid() && previousActive.isLoaded)
                Assert.That(previousActive.isDirty, Is.EqualTo(previousDirty));
        }
    }

    private static void AssertCorrectedRouteGeometry(IReadOnlyList<Transform> roads)
    {
        int[] lengths = { 13, 18, 17, 12, 6, 19, 33 };
        Vector3[] directions =
        {
            Vector3.right,
            Vector3.forward,
            Vector3.left,
            Vector3.back,
            Vector3.left,
            Vector3.forward,
            Vector3.right
        };

        Vector3 cursor = new(109.65f, 0f, -10.45f);
        int mainIndex = 0;
        for (int segment = 0; segment < lengths.Length; segment++)
        {
            for (int step = 0; step < lengths[segment]; step++)
            {
                mainIndex++;
                cursor += directions[segment] * 11.25f;
                Transform road = roads.Single(
                    child => child.name.StartsWith($"Mode2_Main_{mainIndex:000}_"));
                Assert.That(
                    Vector3.Distance(road.position, cursor),
                    Is.LessThan(0.02f),
                    road.name);
            }
        }

        Assert.That(mainIndex, Is.EqualTo(118));
        Assert.That(cursor.x, Is.EqualTo(368.4f).Within(0.02f));
        Assert.That(cursor.z, Is.EqualTo(270.8f).Within(0.02f));

        Vector3 branchAnchor = roads.Single(
            child => child.name.StartsWith("Mode2_Main_097_")).position;
        Assert.That(branchAnchor.x, Is.EqualTo(132.15f).Within(0.02f));
        Assert.That(branchAnchor.z, Is.EqualTo(270.8f).Within(0.02f));
        foreach (string arm in new[] { "North", "South" })
        {
            float sign = arm == "North" ? 1f : -1f;
            for (int step = 1; step <= 4; step++)
            {
                Transform branch = roads.Single(
                    child => child.name == $"Mode2_Branch_{arm}_{step:00}_P5");
                Assert.That(branch.position.x, Is.EqualTo(branchAnchor.x).Within(0.02f));
                Assert.That(
                    branch.position.z,
                    Is.EqualTo(branchAnchor.z + sign * 11.25f * step).Within(0.02f));
            }
        }
    }

    private static void AssertTurnSpots(Scene map2)
    {
        NoryangjinTurnSpot[] all = map2.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<NoryangjinTurnSpot>(true))
            .ToArray();
        Assert.That(all.Length, Is.EqualTo(8));

        float[] expectedGeneratedYaw = { 0f, -90f, 180f, -90f, 0f, 90f };
        NoryangjinTurnSpot[] generated = all
            .Where(spot => spot.name.StartsWith("Mode2_TurnSpot_"))
            .OrderBy(spot => spot.name)
            .ToArray();
        Assert.That(generated.Length, Is.EqualTo(6));
        for (int i = 0; i < generated.Length; i++)
        {
            Assert.That(
                Mathf.DeltaAngle(generated[i].TargetYawDegrees, expectedGeneratedYaw[i]),
                Is.EqualTo(0f).Within(0.1f));
            Assert.That(
                generated[i].TurnDurationSeconds,
                Is.EqualTo(NoryangjinTurnSpot.DefaultTurnDurationSeconds).Within(0.001f));
            Assert.That(generated[i].GetComponent<BoxCollider>().isTrigger, Is.True);
        }
    }

    private static void AssertHighwayContact(
        IReadOnlyList<Transform> props,
        IReadOnlyList<Transform> roads)
    {
        Transform finalRoad = roads.Single(
            child => child.name.StartsWith("Mode2_Main_118_"));
        Transform highway = props.Single(child => child.name == "Mode2_Highway_Deck_90m");
        Bounds roadBounds = RendererBounds(finalRoad);
        Bounds highwayBounds = RendererBounds(highway);
        Assert.That(highwayBounds.min.x, Is.LessThanOrEqualTo(roadBounds.max.x + 0.1f));
        Assert.That(highwayBounds.max.x, Is.GreaterThan(finalRoad.position.x + 89f));
    }

    private static void AssertAuthoredEnvironment(
        IReadOnlyList<Transform> props,
        IReadOnlyList<Transform> roads)
    {
        Transform[] markets = props
            .Where(child => child.name.StartsWith("Mode2_Market_"))
            .OrderBy(child => child.name)
            .ToArray();
        Bounds[] marketBounds = markets.Select(RendererBounds).ToArray();

        for (int left = 0; left < marketBounds.Length; left++)
        {
            for (int right = left + 1; right < marketBounds.Length; right++)
            {
                Assert.That(
                    OverlapsXZ(marketBounds[left], marketBounds[right]),
                    Is.False,
                    $"{markets[left].name} overlaps {markets[right].name}");
            }
        }

        Vector2[] routePoints = SampleAuthoredRoute(roads).ToArray();
        float minimumClearance = marketBounds.Min(bounds =>
        {
            float sampleY = bounds.center.y;
            return routePoints.Min(point => Mathf.Sqrt(
                bounds.SqrDistance(new Vector3(point.x, sampleY, point.y))));
        });
        Assert.That(minimumClearance, Is.GreaterThanOrEqualTo(5f));

        Vector3[] anchors =
        {
            new(255.90f, 0f, 90.80f),
            new(98.40f, 0f, 192.05f),
            new(-2.85f, 0f, 158.30f),
            new(222.15f, 0f, 270.80f)
        };
        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.left,
            Vector3.forward,
            Vector3.right
        };
        int[] clusterCounts = new int[anchors.Length];
        int[] buildingCounts = new int[anchors.Length];
        for (int marketIndex = 0; marketIndex < markets.Length; marketIndex++)
        {
            Transform market = markets[marketIndex];
            Bounds bounds = marketBounds[marketIndex];
            Vector3 boundsCenter = new(bounds.center.x, 0f, bounds.center.z);
            int cluster = Enumerable.Range(0, anchors.Length)
                .OrderBy(index => Vector3.SqrMagnitude(boundsCenter - anchors[index]))
                .First();
            Vector3 direction = directions[cluster];
            Vector3 normal = new(direction.z, 0f, -direction.x);
            bool isBuilding = Mathf.Max(bounds.size.x, bounds.size.z) > 7f;
            if (isBuilding)
            {
                Vector3 offset = boundsCenter - anchors[cluster];
                float longitudinal = Mathf.Abs(Vector3.Dot(offset, direction));
                float lateral = Mathf.Abs(Vector3.Dot(offset, normal));
                float halfNormal = Mathf.Abs(normal.x) * bounds.extents.x +
                                   Mathf.Abs(normal.z) * bounds.extents.z;
                Assert.That(
                    new[] { 0f, 13f }.Any(slot => Mathf.Abs(longitudinal - slot) < 0.02f),
                    Is.True,
                    market.name);
                Assert.That(lateral - halfNormal, Is.EqualTo(9.5f).Within(0.02f), market.name);
                buildingCounts[cluster]++;
            }
            else
            {
                Vector3 offset = market.position - anchors[cluster];
                float longitudinal = Mathf.Abs(Vector3.Dot(offset, direction));
                float lateral = Mathf.Abs(Vector3.Dot(offset, normal));
                Assert.That(longitudinal, Is.LessThanOrEqualTo(56.01f), market.name);
                Assert.That(
                    new[] { 12f, 28f, 44f }.Any(row => Mathf.Abs(lateral - row) < 0.02f),
                    Is.True,
                    market.name);
            }
            clusterCounts[cluster]++;
        }
        CollectionAssert.AreEqual(new[] { 31, 31, 31, 30 }, clusterCounts);
        CollectionAssert.AreEqual(new[] { 6, 6, 5, 5 }, buildingCounts);

        Transform[] quays = props
            .Where(child => child.name.StartsWith("Mode2_Quay_"))
            .ToArray();
        int[] quayClusterCounts = new int[anchors.Length];
        foreach (Transform quay in quays)
        {
            int closest = Enumerable.Range(0, anchors.Length)
                .OrderBy(index => Vector3.SqrMagnitude(quay.position - anchors[index]))
                .First();
            Assert.That(
                Vector3.Distance(quay.position, anchors[closest]),
                Is.LessThan(17f),
                quay.name);
            quayClusterCounts[closest]++;
        }
        CollectionAssert.AreEqual(new[] { 5, 5, 5, 5 }, quayClusterCounts);
        Bounds[] quayBounds = quays.Select(RendererBounds).ToArray();
        foreach (Bounds building in marketBounds.Where(
                     bounds => Mathf.Max(bounds.size.x, bounds.size.z) > 7f))
        {
            Assert.That(
                quayBounds.Any(quay => OverlapsXZ(building, quay)),
                Is.True,
                "Every large market building should remain seated on a dock cluster.");
        }

        Transform[] water = props
            .Where(child => child.name.StartsWith("Mode2_Water_"))
            .ToArray();
        Assert.That(water.Length, Is.EqualTo(196));
        const string waterMaterialPath =
            "Assets/ShooterSurvival/Materials/Generated/Noryangjin_Map2_Water.mat";
        Assert.That(
            water.SelectMany(child => child.GetComponentsInChildren<Renderer>(true))
                .All(renderer =>
                    AssetDatabase.GetAssetPath(renderer.sharedMaterial) == waterMaterialPath),
            Is.True);
        Material waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(waterMaterialPath);
        Assert.That(waterMaterial, Is.Not.Null);
        foreach (string textureProperty in new[]
                 {
                     "_BaseMap",
                     "_MainTex",
                     "_BumpMap",
                     "_EmissionMap",
                     "_MetallicGlossMap"
                 })
        {
            Assert.That(
                waterMaterial.GetTextureScale(textureProperty),
                Is.EqualTo(new Vector2(0.5f, 0.5f)),
                textureProperty);
            Assert.That(
                waterMaterial.GetTextureOffset(textureProperty),
                Is.EqualTo(new Vector2(0.5f, 0f)),
                textureProperty);
        }

        Bounds roadEnvelope = CombinedBounds(roads);
        Bounds waterEnvelope = CombinedBounds(water);
        Assert.That(roadEnvelope.min.x - waterEnvelope.min.x, Is.GreaterThanOrEqualTo(70f));
        Assert.That(waterEnvelope.max.x - roadEnvelope.max.x, Is.GreaterThanOrEqualTo(70f));
        Assert.That(roadEnvelope.min.z - waterEnvelope.min.z, Is.GreaterThanOrEqualTo(70f));
        Assert.That(waterEnvelope.max.z - roadEnvelope.max.z, Is.GreaterThanOrEqualTo(70f));
    }

    private static IEnumerable<Vector2> SampleAuthoredRoute(
        IReadOnlyList<Transform> roads)
    {
        Transform[] mainRoads = roads
            .Where(child => child.name.StartsWith("Mode2_Main_"))
            .OrderBy(child => ExtractNumber(child.name, "Mode2_Main_"))
            .ToArray();
        Vector2 cursor = new(109.65f, -10.45f);
        foreach (Transform road in mainRoads)
        {
            Vector2 next = new(road.position.x, road.position.z);
            foreach (Vector2 point in SampleSegment(cursor, next))
                yield return point;
            cursor = next;
        }

        Transform branchAnchor = mainRoads.Single(
            child => ExtractNumber(child.name, "Mode2_Main_") == 97);
        foreach (string arm in new[] { "North", "South" })
        {
            cursor = new Vector2(branchAnchor.position.x, branchAnchor.position.z);
            foreach (Transform road in roads
                         .Where(child => child.name.StartsWith($"Mode2_Branch_{arm}_"))
                         .OrderBy(child => child.name))
            {
                Vector2 next = new(road.position.x, road.position.z);
                foreach (Vector2 point in SampleSegment(cursor, next))
                    yield return point;
                cursor = next;
            }
        }

        foreach (Transform road in roads.Take(24))
            yield return new Vector2(road.position.x, road.position.z);
    }

    private static IEnumerable<Vector2> SampleSegment(Vector2 from, Vector2 to)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / 0.5f));
        for (int step = 0; step <= steps; step++)
            yield return Vector2.Lerp(from, to, step / (float)steps);
    }

    private static int ExtractNumber(string name, string prefix)
    {
        int end = name.IndexOf('_', prefix.Length);
        return int.Parse(name.Substring(prefix.Length, end - prefix.Length));
    }

    private static bool OverlapsXZ(Bounds left, Bounds right)
    {
        return left.min.x < right.max.x &&
               left.max.x > right.min.x &&
               left.min.z < right.max.z &&
               left.max.z > right.min.z;
    }

    private static Bounds CombinedBounds(IEnumerable<Transform> roots)
    {
        Bounds[] bounds = roots.Select(RendererBounds).ToArray();
        Assert.That(bounds, Is.Not.Empty);
        Bounds combined = bounds[0];
        for (int index = 1; index < bounds.Length; index++)
            combined.Encapsulate(bounds[index]);
        return combined;
    }

    private static void AssertCopiedPrefix(Transform source, Transform target)
    {
        Assert.That(target.childCount, Is.GreaterThanOrEqualTo(source.childCount));
        for (int i = 0; i < source.childCount; i++)
        {
            Transform expected = source.GetChild(i);
            Transform actual = target.GetChild(i);
            Assert.That(actual.name, Is.EqualTo(expected.name));
            Assert.That(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actual.gameObject),
                Is.EqualTo(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(expected.gameObject)));
            Assert.That(Vector3.Distance(actual.position, expected.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(actual.rotation, expected.rotation), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(actual.localScale, expected.localScale), Is.LessThan(0.001f));
        }
    }

    private static Bounds RendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty, root.name);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Transform[] DirectChildren(Transform parent)
    {
        return Enumerable.Range(0, parent.childCount)
            .Select(parent.GetChild)
            .ToArray();
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().Single(root => root.name == name);
    }

    private static string HashFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
    }
}
#endif
