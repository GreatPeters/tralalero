using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NoryangjinSlopeTests
{
    private Scene preview;
    private GameObject root;
    private bool wasRunning;
    private bool wasGameOver;
    private float timeFactor;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        wasRunning = TimeManager.isGameRunning;
        wasGameOver = CanvasScript.isGameOver;
        timeFactor = TimeManager.timeFactor;
        preview = EditorSceneManager.NewPreviewScene();
        root = new GameObject("Slope test fixture");
        SceneManager.MoveGameObjectToScene(root, preview);
        TimeManager.isGameRunning = true;
        CanvasScript.isGameOver = false;
        TimeManager.timeFactor = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        NoryangjinTurnSpot.ResetAllForNewRun();
        EditorSceneManager.ClosePreviewScene(preview);
        TimeManager.isGameRunning = wasRunning;
        CanvasScript.isGameOver = wasGameOver;
        TimeManager.timeFactor = timeFactor;
    }

    private GameObject Child(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform);
        return go;
    }

    private Transform Decks()
    {
        Transform roads = Child("Roads").transform;
        foreach (float height in new[] { 0f, 12f })
        {
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.transform.SetParent(roads);
            deck.transform.position = new Vector3(0f, height - 0.5f, 0f);
            deck.transform.localScale = new Vector3(10f, 1f, 100f);
            Object.DestroyImmediate(deck.GetComponent<BoxCollider>());
            deck.AddComponent<MeshCollider>().sharedMesh = deck.GetComponent<MeshFilter>().sharedMesh;
        }
        Physics.SyncTransforms();
        return roads;
    }

    private PlayerScript Player(bool follow = true)
    {
        GameObject go = Child("Player");
        go.tag = "Player";
        go.transform.position = Vector3.up * 0.12f;
        Rigidbody body = go.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        NoryangjinRoadHeightFollower follower = follow ? go.AddComponent<NoryangjinRoadHeightFollower>() : null;
        if (follower != null)
            follower.Configure(Decks(), 0.12f);
        PlayerScript player = go.AddComponent<PlayerScript>();
        player.currentHealth = 100f;
        typeof(PlayerScript).GetField("roadHeightFollower", Private).SetValue(player, follower);
        typeof(PlayerScript).GetField("playerRigidbody", Private).SetValue(player, body);
        typeof(PlayerScript).GetField("currentForwardMoveSpeed", Private).SetValue(player, 8f);
        return player;
    }

    [TestCase(0.12f)]
    [TestCase(12.12f)]
    public void HeightProjection_UsesTheNearbyDeckNotTheOtherCrossing(float height)
    {
        PlayerScript player = Player();
        var follower = player.GetComponent<NoryangjinRoadHeightFollower>();
        Assert.That(follower.TryProjectPosition(new Vector3(0, height + 0.1f, 1), Vector3.forward, out Vector3 result), Is.True);
        Assert.That(result.y, Is.EqualTo(height).Within(0.001f));
        Assert.That(follower.TryProjectPosition(new Vector3(20, height, 1), Vector3.forward, out result), Is.False);
        Assert.That(result.y, Is.EqualTo(height));
    }

    [Test]
    public void SlopePitch_KeepsMovingAndDoesNotLockOrRebaseTheLane()
    {
        PlayerScript player = Player();
        var laneField = typeof(PlayerScript).GetField("routeLaneOrigin", Private);
        Vector3 laneOrigin = new Vector3(0, 0, -10);
        laneField.SetValue(player, laneOrigin);
        RigidbodyConstraints constraints = player.GetComponent<Rigidbody>().constraints;
        Assert.That(player.RequestSlopePitch(-20f, 0f, 0.25f), Is.True);
        Vector3 start = player.transform.position;
        var advance = typeof(PlayerScript).GetMethod("ApplyForwardMovement", Private);
        for (int i = 0; i < 20; i++)
            advance.Invoke(player, null);
        Assert.That(player.transform.position.z - start.z, Is.EqualTo(20 * 8f * Time.fixedDeltaTime).Within(0.001f));
        Assert.That(Mathf.DeltaAngle(0, player.transform.eulerAngles.x), Is.EqualTo(-20).Within(0.001f));
        Assert.That(player.transform.position.y, Is.EqualTo(0.12f).Within(0.001f));
        Assert.That(player.IsWorldYawTurnActive, Is.False);
        Assert.That(player.GetComponent<Rigidbody>().constraints, Is.EqualTo(constraints));
        Assert.That((Vector3)laneField.GetValue(player), Is.EqualTo(laneOrigin));
    }

    [TestCase(-16f)] [TestCase(16f)]
    public void RoadIndexKeepsNegativeCellEdgesAndReconfiguredRoads(float x)
    {
        var player = Player();var follower = player.GetComponent<NoryangjinRoadHeightFollower>();
        var roads = follower.RoadRoot;roads.position = new Vector3(x, 0, 0);Physics.SyncTransforms();
        follower.Configure(roads, .12f);
        Assert.That(follower.TryProjectPosition(new Vector3(x, .12f, 1), Vector3.forward, out var low), Is.True);
        Assert.That(low.y, Is.EqualTo(.12f).Within(.001f));
        Assert.That(follower.TryProjectPosition(new Vector3(x, 12.12f, 1), Vector3.forward, out var high), Is.True);
        Assert.That(high.y, Is.EqualTo(12.12f).Within(.001f));
        Assert.That(follower.TryProjectPosition(new Vector3(x + 20, .12f, 1), Vector3.forward, out _), Is.False);
    }

    [Test]
    public void SlopePitch_RejectsMissingFollowerAndWrongIncomingDirection()
    {
        Assert.That(Player(false).RequestSlopePitch(-20, 0, 0.25f), Is.False);
        Assert.That(Player().RequestSlopePitch(-20, 90, 0.25f), Is.False);
    }

    [Test]
    public void PitchTransition_PausesWithZeroTimeAndResetsWithoutMovingThePlayer()
    {
        PlayerScript player = Player();
        var follower = player.GetComponent<NoryangjinRoadHeightFollower>();
        follower.BeginPitch(0, -20, 0.25f);
        Assert.That(follower.AdvancePitch(0, out float pitch), Is.True);
        Assert.That(pitch, Is.Zero);
        Assert.That(follower.AdvancePitch(0.125f, out pitch), Is.True);
        Assert.That(pitch, Is.EqualTo(-10).Within(0.001f));
        player.transform.position = new Vector3(2, 0.12f, 4);
        player.ResetState();
        Assert.That(follower.IsBlendingPitch, Is.False);
        Assert.That(player.transform.position, Is.EqualTo(new Vector3(2, 0.12f, 4)));
    }

    [Test]
    public void PitchSpots_AreOneShotButDoNotBecomeCornerCheckpoints()
    {
        PlayerScript player = Player();
        NoryangjinTurnSpot corner = Child("Corner").AddComponent<NoryangjinTurnSpot>();
        NoryangjinTurnSpot slope = Child("Slope").AddComponent<NoryangjinTurnSpot>();
        slope.IsSlopeTransition = true;
        slope.TargetXDegrees = -20;
        Assert.That(NoryangjinTurnSpot.TryGetRouteProgress(preview, out int completed, out int total), Is.True);
        Assert.That(total, Is.EqualTo(1));
        Assert.That(completed, Is.Zero);
        bool activated = (bool)typeof(NoryangjinTurnSpot).GetMethod("TryActivate", Private).Invoke(slope, new object[] { player });
        Assert.That(activated, Is.True);
        Assert.That(slope.gameObject.activeSelf, Is.False);
        Assert.That(player.IsWorldYawTurnActive, Is.False);
        NoryangjinTurnSpot.TryGetRouteProgress(preview, out completed, out total);
        Assert.That(total, Is.EqualTo(1));
        Assert.That(completed, Is.Zero);
        NoryangjinTurnSpot.ResetAllForNewRun();
        Assert.That(slope.gameObject.activeSelf, Is.True);
    }
}
