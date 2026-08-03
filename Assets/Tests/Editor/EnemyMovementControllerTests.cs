using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Utils;

public sealed class EnemyMovementControllerTests
{
    private const BindingFlags PrivateInstance =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private Scene previewScene;

    [SetUp]
    public void SetUp()
    {
        previewScene = EditorSceneManager.NewPreviewScene();
        TimeManager.isGameRunning = false;
        TimeManager.timeFactor = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        EnemyMovementController.ResetAllForNewRun();
        EnemyMovementActivationTrigger.ResetAllForNewRun();
        TimeManager.isGameRunning = false;
        TimeManager.timeFactor = 1f;

        if (previewScene.IsValid())
            EditorSceneManager.ClosePreviewScene(previewScene);
    }

    [Test]
    public void StayStill_DoesNotChangeAuthoredPosition()
    {
        GameObject enemy = CreatePreviewObject("Still Enemy");
        enemy.transform.position = new Vector3(3f, 0f, 9f);
        EnemyMovementController movement =
            enemy.AddComponent<EnemyMovementController>();
        Vector3 anchor = enemy.transform.position;

        InvokePrivate(movement, "PrepareForPlacementCapture", null);
        Advance(movement, 10f);

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(anchor)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void SideToSide_MovesAroundAuthoredCenterOnLocalRightAxis()
    {
        GameObject enemy = CreatePreviewObject("Side Enemy");
        enemy.transform.SetPositionAndRotation(
            new Vector3(10f, 0f, 20f),
            Quaternion.Euler(0f, 90f, 0f));
        EnemyMovementController movement =
            enemy.AddComponent<EnemyMovementController>();
        movement.MovementMode = EnemyMovementMode.MoveSideToSide;
        movement.MoveSpeed = 2f;
        movement.SideToSideDistance = 3f;
        Vector3 anchor = enemy.transform.position;
        Vector3 localRight = enemy.transform.right;

        Advance(movement, 1f);

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(anchor + localRight * 2f)
                .Using(Vector3ComparerWithEqualsOperator.Instance));

        Advance(movement, 20f);
        float sideOffset =
            Vector3.Dot(enemy.transform.position - anchor, localRight);
        Assert.That(sideOffset, Is.InRange(-3f, 3f));
    }

    [Test]
    public void Forward_WaitsForTriggerThenUsesAuthoredLocalForward()
    {
        GameObject enemy = CreatePreviewObject("Forward Enemy");
        enemy.transform.SetPositionAndRotation(
            new Vector3(1f, 0f, 2f),
            Quaternion.Euler(0f, 90f, 0f));
        EnemyMovementController movement =
            enemy.AddComponent<EnemyMovementController>();
        movement.MovementMode = EnemyMovementMode.MoveForwardOnTrigger;
        movement.MoveSpeed = 3f;
        Vector3 anchor = enemy.transform.position;
        Vector3 localForward = enemy.transform.forward;

        Advance(movement, 2f);
        Assert.That(
            enemy.transform.position,
            Is.EqualTo(anchor)
                .Using(Vector3ComparerWithEqualsOperator.Instance));

        Assert.That(movement.ActivateFromTrigger(), Is.True);
        Advance(movement, 2f);

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(anchor + localForward * 6f)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void SideEntrance_WaitsAtConfiguredSideUntilTriggerThenReachesAnchor()
    {
        GameObject enemy = CreatePreviewObject("Entrance Enemy");
        enemy.transform.SetPositionAndRotation(
            new Vector3(5f, 0f, 7f),
            Quaternion.Euler(0f, 45f, 0f));
        EnemyMovementController movement =
            enemy.AddComponent<EnemyMovementController>();
        movement.MovementMode = EnemyMovementMode.EnterFromSideOnTrigger;
        movement.EntranceSide = EnemyEntranceSide.Left;
        movement.EntranceDistance = 4f;
        movement.MoveSpeed = 2f;
        Vector3 anchor = enemy.transform.position;
        Vector3 expectedStart = anchor - enemy.transform.right * 4f;

        Initialize(movement);
        Assert.That(
            enemy.transform.position,
            Is.EqualTo(expectedStart)
                .Using(Vector3ComparerWithEqualsOperator.Instance));

        Advance(movement, 1f);
        Assert.That(
            enemy.transform.position,
            Is.EqualTo(expectedStart)
                .Using(Vector3ComparerWithEqualsOperator.Instance));

        Assert.That(movement.ActivateFromTrigger(), Is.True);
        Advance(movement, 2f);

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(anchor)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void FirstTick_CapturesPositionAssignedAfterPoolEnable()
    {
        GameObject enemy = CreatePreviewObject("Pooled Entrance Enemy");
        enemy.transform.position = new Vector3(100f, 0f, 100f);
        EnemyMovementController movement =
            enemy.AddComponent<EnemyMovementController>();
        movement.MovementMode = EnemyMovementMode.EnterFromSideOnTrigger;
        movement.EntranceSide = EnemyEntranceSide.Right;
        movement.EntranceDistance = 3f;

        Vector3 spawnedPosition = new Vector3(8f, 0f, 12f);
        enemy.transform.position = spawnedPosition;
        Initialize(movement);

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(spawnedPosition + Vector3.right * 3f)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void NewRunReset_RestoresAnchorAndRequiresTriggerAgain()
    {
        GameObject enemy = CreatePreviewObject("Reset Enemy");
        enemy.transform.position = new Vector3(4f, 0f, 6f);
        EnemyMovementController movement =
            enemy.AddComponent<EnemyMovementController>();
        movement.MovementMode = EnemyMovementMode.MoveForwardOnTrigger;
        movement.MoveSpeed = 5f;
        Vector3 anchor = enemy.transform.position;

        InvokePrivate(movement, "OnEnable", null);
        movement.ActivateFromTrigger();
        Advance(movement, 1f);
        Assert.That(enemy.transform.position, Is.Not.EqualTo(anchor));

        EnemyMovementController.ResetAllForNewRun();

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(anchor)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
        Assert.That(movement.IsActivated, Is.False);
        InvokePrivate(movement, "OnDisable", null);
    }

    [Test]
    public void ActivationTrigger_ActivatesTargetsOnceAndResetsForNextRun()
    {
        GameObject enemy = CreatePreviewObject("Triggered Enemy");
        EnemyMovementController movement =
            enemy.AddComponent<EnemyMovementController>();
        movement.MovementMode = EnemyMovementMode.MoveForwardOnTrigger;

        GameObject triggerObject = CreatePreviewObject("Movement Trigger");
        EnemyMovementActivationTrigger trigger =
            triggerObject.AddComponent<EnemyMovementActivationTrigger>();
        trigger.Targets = new[] { movement, null };
        trigger.OneShot = true;
        BoxCollider collider = trigger.GetComponent<BoxCollider>();

        Assert.That(trigger.ActivateTargets(), Is.True);
        Assert.That(movement.IsActivated, Is.True);
        Assert.That(collider.enabled, Is.False);

        ResetMovement(movement);
        EnemyMovementActivationTrigger.ResetAllForNewRun();

        Assert.That(movement.IsActivated, Is.False);
        Assert.That(collider.enabled, Is.True);
    }

    [Test]
    public void ActivationTrigger_IgnoresPlayerChildCollider()
    {
        GameObject playerObject = CreatePreviewObject("Trigger Player");
        PlayerScript player = playerObject.AddComponent<PlayerScript>();
        BoxCollider rootCollider = playerObject.AddComponent<BoxCollider>();
        GameObject childObject = CreatePreviewObject("Weapon Collider");
        childObject.transform.SetParent(playerObject.transform);
        BoxCollider childCollider = childObject.AddComponent<BoxCollider>();

        Assert.That(
            EnemyMovementActivationTrigger.ResolveTriggerPlayer(rootCollider),
            Is.SameAs(player));
        Assert.That(
            EnemyMovementActivationTrigger.ResolveTriggerPlayer(childCollider),
            Is.Null);
    }

    [Test]
    public void ForwardEnemyPrefabs_HaveDefaultStayStillController()
    {
        foreach (string prefabPath in ForwardEnemyMovementSetup.EnemyPrefabPaths)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);

            EnemyMovementController movement =
                prefab.GetComponent<EnemyMovementController>();
            Assert.That(movement, Is.Not.Null, prefabPath);
            Assert.That(
                movement.MovementMode,
                Is.EqualTo(EnemyMovementMode.StayStill),
                prefabPath);
        }
    }

    [Test]
    public void MovementTriggerPrefab_IsReadyForMapToolPlacement()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            ForwardEnemyMovementSetup.TriggerPrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(
            prefab.GetComponent<EnemyMovementActivationTrigger>(),
            Is.Not.Null);
        Assert.That(prefab.GetComponent<BoxCollider>().isTrigger, Is.True);
        Assert.That(
            NoryangjinMapToolWindow.IsEnemyMovementTriggerPrefabPath(
                ForwardEnemyMovementSetup.TriggerPrefabPath),
            Is.True);
        Assert.That(
            NoryangjinMapToolWindow.GetSpecialPaletteIconText(
                ForwardEnemyMovementSetup.TriggerPrefabPath),
            Is.EqualTo(
                NoryangjinMapToolWindow
                    .EnemyMovementTriggerPaletteItemIconText));
    }

    private GameObject CreatePreviewObject(string name)
    {
        var gameObject = new GameObject(name);
        SceneManager.MoveGameObjectToScene(gameObject, previewScene);
        return gameObject;
    }

    private static void Initialize(EnemyMovementController movement)
    {
        InvokePrivate(movement, "EnsureInitialized", null);
    }

    private static void Advance(
        EnemyMovementController movement,
        float deltaTime)
    {
        InvokePrivate(
            movement,
            "AdvanceMovement",
            new object[] { deltaTime });
    }

    private static void ResetMovement(EnemyMovementController movement)
    {
        InvokePrivate(movement, "ResetForNewRun", null);
    }

    private static void InvokePrivate(
        EnemyMovementController movement,
        string methodName,
        object[] arguments)
    {
        MethodInfo method = typeof(EnemyMovementController).GetMethod(
            methodName,
            PrivateInstance);
        Assert.That(method, Is.Not.Null, methodName);
        method.Invoke(movement, arguments);
    }
}
