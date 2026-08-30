#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Utils;

public sealed class EnemyEventControllerTests
{
    private const BindingFlags PrivateInstance =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();
    private Scene previewScene;

    [SetUp]
    public void SetUp()
    {
        TimeManager.isGameRunning = false;
        TimeManager.timeFactor = 1f;
        EnemyEventController.ResetAllForNewRun();
        EnemyEventActivationSpot.ResetAllForNewRun();
        previewScene = EditorSceneManager.NewPreviewScene();
    }

    [TearDown]
    public void TearDown()
    {
        TimeManager.isGameRunning = false;
        TimeManager.timeFactor = 1f;
        EnemyEventController.ResetAllForNewRun();
        EnemyEventActivationSpot.ResetAllForNewRun();

        if (previewScene.IsValid())
            EditorSceneManager.ClosePreviewScene(previewScene);
        createdObjects.Clear();
    }

    [Test]
    public void AddedEnumValues_PreserveExistingSerializedNumbers()
    {
        Assert.That((int)EnemyMoveAnimation.None, Is.EqualTo(-1));
        Assert.That((int)EnemyMoveAnimation.Walk, Is.Zero);
        Assert.That((int)EnemyMoveAnimation.Run, Is.EqualTo(1));
        Assert.That((int)EnemyEventMode.AttackOnce, Is.EqualTo(5));
    }

    [Test]
    public void AttackLoop_ActivatesOnceAndStaysAtAuthoredPosition()
    {
        GameObject enemy = CreateObject("Attack Loop Enemy");
        enemy.transform.position = new Vector3(2f, 0f, 3f);
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.AttackLoop;
        Vector3 authoredPosition = enemy.transform.position;

        Assert.That(controller.ActivateFromSpot(), Is.True);
        Assert.That(controller.ActivateFromSpot(), Is.False);
        Advance(controller, 5f);

        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.Attacking));
        Assert.That(
            enemy.transform.position,
            Is.EqualTo(authoredPosition)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void AttackOnce_ActivatesOnceWithoutMoving()
    {
        GameObject enemy = CreateObject("Attack Once Enemy");
        enemy.transform.position = new Vector3(2f, 0f, 3f);
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.AttackOnce;
        Vector3 authoredPosition = enemy.transform.position;

        Assert.That(controller.ActivateFromSpot(), Is.True);
        Assert.That(controller.ActivateFromSpot(), Is.False);
        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.Attacking));
        Assert.That(
            enemy.transform.position,
            Is.EqualTo(authoredPosition)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void MoveToTargetThenAttack_MovesToOneTargetAndStartsAttack()
    {
        GameObject enemy = CreateObject("Move Then Attack Enemy");
        GameObject target = CreateObject("Move Target");
        target.transform.position = new Vector3(3f, 4f, 4f);
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.MoveToTargetThenAttack;
        controller.TargetPoint = target.transform;
        controller.MoveSpeed = 5f;

        Assert.That(controller.ActivateFromSpot(), Is.True);
        Advance(controller, 1f);

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(new Vector3(3f, 0f, 4f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));
        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.Attacking));
    }

    [Test]
    public void MoveModeWithoutTarget_DoesNotConsumeActivation()
    {
        GameObject enemy = CreateObject("Missing Target Enemy");
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.MoveToTargetThenAttack;

        Assert.That(controller.ActivateFromSpot(), Is.False);
        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.Waiting));
    }

    [Test]
    public void MoveModeWithZeroSpeed_DoesNotConsumeActivation()
    {
        GameObject enemy = CreateObject("Zero Speed Enemy");
        GameObject target = CreateObject("Zero Speed Target");
        target.transform.position = Vector3.right;
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.MoveToTargetThenAttack;
        controller.TargetPoint = target.transform;
        controller.MoveSpeed = 0f;
        GameObject spotObject = CreateObject("Zero Speed Spot");
        EnemyEventActivationSpot spot =
            spotObject.AddComponent<EnemyEventActivationSpot>();
        spot.Targets = new[] { controller };

        Assert.That(spot.ActivateTargets(), Is.False);
        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.Waiting));
        Assert.That(spot.GetComponent<BoxCollider>().enabled, Is.True);
    }

    [Test]
    public void DestroyedMovementTarget_CancelsWithoutRepeatingExceptions()
    {
        GameObject enemy = CreateObject("Destroyed Target Enemy");
        GameObject target = CreateObject("Disposable Target");
        target.transform.position = Vector3.right * 4f;
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.MoveToTargetThenAttack;
        controller.TargetPoint = target.transform;
        controller.MoveSpeed = 2f;
        Assert.That(controller.ActivateFromSpot(), Is.True);

        Object.DestroyImmediate(target);
        UnityEngine.TestTools.LogAssert.Expect(
            LogType.Warning,
            new System.Text.RegularExpressions.Regex("movement target is missing"));
        Advance(controller, 0.1f);
        Advance(controller, 0.1f);

        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.Waiting));
    }

    [Test]
    public void Patrol_AttacksAtBothEndpointsAndContinuesBetweenThem()
    {
        GameObject enemy = CreateObject("Patrol Enemy");
        GameObject target = CreateObject("Patrol Target");
        target.transform.position = Vector3.right * 2f;
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.PatrolBetweenStartAndTarget;
        controller.TargetPoint = target.transform;
        controller.MoveSpeed = 2f;

        Assert.That(controller.ActivateFromSpot(), Is.True);
        Advance(controller, 1f);
        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.PatrolAttack));
        Assert.That(enemy.transform.position, Is.EqualTo(Vector3.right * 2f));

        Advance(controller, 1.01f);
        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.MovingToStart));
        Advance(controller, 1f);
        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.PatrolAttack));
        Assert.That(enemy.transform.position, Is.EqualTo(Vector3.zero));

        Advance(controller, 1.01f);
        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.MovingToTarget));
    }

    [Test]
    public void MovingEnemy_FacesItsActualTravelDirection()
    {
        GameObject enemy = CreateObject("Facing Enemy");
        Animator animator = CreateObject("Visual").AddComponent<Animator>();
        animator.transform.SetParent(enemy.transform, false);
        GameObject target = CreateObject("Diagonal Target");
        target.transform.position = new Vector3(4f, 0f, 3f);
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.MoveToTargetThenAttack;
        controller.TargetPoint = target.transform;
        controller.MoveSpeed = 1f;

        Assert.That(controller.ActivateFromSpot(), Is.True);
        Advance(controller, 0.25f);

        Assert.That(
            Vector3.Angle(animator.transform.forward, new Vector3(4f, 0f, 3f)),
            Is.LessThan(0.01f));
    }

    [Test]
    public void AttackFacing_SnapsToClosestRouteOrthogonalAxis()
    {
        Vector3 facing = EnemyEventController.ResolveOrthogonalFacingDirection(
            new Vector3(8f, 2f, 3f),
            Vector3.forward,
            Vector3.right);

        Assert.That(facing, Is.EqualTo(Vector3.right));
        Assert.That(Vector3.Dot(facing, Vector3.forward), Is.Zero.Within(0.0001f));

        facing = EnemyEventController.ResolveOrthogonalFacingDirection(
            new Vector3(-2f, 0f, -7f),
            Vector3.forward,
            Vector3.right);
        Assert.That(facing, Is.EqualTo(Vector3.back));
    }

    [Test]
    public void RouteFacing_PreservesAuthoredAnimatorRotationOffset()
    {
        GameObject enemy = CreateObject("Offset Enemy");
        enemy.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        GameObject visual = CreateObject("Offset Visual");
        visual.transform.SetParent(enemy.transform, false);
        visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        visual.AddComponent<Animator>();
        Quaternion authoredRotation = visual.transform.rotation;

        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.SnapToRouteDirection();

        Assert.That(
            Quaternion.Angle(visual.transform.rotation, authoredRotation),
            Is.LessThan(0.01f));
    }

    [Test]
    public void AuthoringMove_RebasesTheNewRunStartPosition()
    {
        GameObject enemy = CreateObject("Rebased Enemy");
        enemy.transform.position = new Vector3(2f, 0f, 4f);
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        Initialize(controller);
        Vector3 previousPosition = enemy.transform.position;

        enemy.transform.position += new Vector3(3f, 0f, -1f);
        controller.RefreshPlacementAfterAuthoringChange(
            previousPosition,
            rotationChanged: false);
        Assert.That(controller.ActivateFromSpot(), Is.True);
        EnemyEventController.ResetAllForNewRun();

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(new Vector3(5f, 0f, 3f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void SceneEnemyReenable_RestoresCapturedStartPosition()
    {
        GameObject enemy = CreateObject("Scene Reset Enemy");
        enemy.transform.position = new Vector3(2f, 0f, 4f);
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        Initialize(controller);
        enemy.transform.position = new Vector3(9f, 0f, 9f);

        enemy.SetActive(false);
        enemy.SetActive(true);

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(new Vector3(2f, 0f, 4f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));
        Assert.That(controller.RuntimeState, Is.EqualTo(EnemyEventRuntimeState.Waiting));
    }

    [Test]
    public void LegacySideEntranceMode_DeserializesAsMoveThenAttack()
    {
        GameObject enemy = CreateObject("Legacy Mode Enemy");
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        FieldInfo eventMode = typeof(EnemyEventController).GetField(
            "eventMode",
            PrivateInstance);
        Assert.That(eventMode, Is.Not.Null);
        eventMode.SetValue(controller, (EnemyEventMode)3);

        ((ISerializationCallbackReceiver)controller).OnAfterDeserialize();

        Assert.That(
            controller.EventMode,
            Is.EqualTo(EnemyEventMode.MoveToTargetThenAttack));
    }

    [Test]
    public void Spot_OnlyConnectsTargetsAndAlwaysConsumesAfterAcceptedActivation()
    {
        GameObject enemy = CreateObject("Connected Enemy");
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        GameObject spotObject = CreateObject("Event Spot");
        EnemyEventActivationSpot spot =
            spotObject.AddComponent<EnemyEventActivationSpot>();
        spot.Targets = new[] { controller };
        BoxCollider collider = spot.GetComponent<BoxCollider>();

        Assert.That(spot.ActivateTargets(), Is.True);
        Assert.That(collider.enabled, Is.False);
        Assert.That(
            typeof(EnemyEventActivationSpot).GetField(
                "oneShot",
                PrivateInstance),
            Is.Null,
            "The spot must not expose a one-shot/on-shot option; it only stores links.");

        EnemyEventActivationSpot.ResetAllForNewRun();
        Assert.That(collider.enabled, Is.True);
    }

    [Test]
    public void Spot_WithInvalidMoveTarget_RemainsAvailable()
    {
        GameObject enemy = CreateObject("Invalid Move Enemy");
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.MoveToTargetThenAttack;
        GameObject spotObject = CreateObject("Event Spot");
        EnemyEventActivationSpot spot =
            spotObject.AddComponent<EnemyEventActivationSpot>();
        spot.Targets = new[] { controller };

        Assert.That(spot.ActivateTargets(), Is.False);
        Assert.That(spot.GetComponent<BoxCollider>().enabled, Is.True);
    }

    [Test]
    public void Spot_ResolvesOnlyThePlayerRootCollider()
    {
        GameObject playerObject = CreateObject("Player");
        PlayerScript player = playerObject.AddComponent<PlayerScript>();
        BoxCollider rootCollider = playerObject.AddComponent<BoxCollider>();
        GameObject child = CreateObject("Player Child");
        child.transform.SetParent(playerObject.transform, false);
        BoxCollider childCollider = child.AddComponent<BoxCollider>();

        Assert.That(
            EnemyEventActivationSpot.ResolveTriggerPlayer(rootCollider),
            Is.SameAs(player));
        Assert.That(
            EnemyEventActivationSpot.ResolveTriggerPlayer(childCollider),
            Is.Null);
    }

    [Test]
    public void Shoot_RequestsTheCombatProjectileOnlyOnce()
    {
        GameObject playerObject = CreateObject("Shoot Player");
        PlayerScript player = playerObject.AddComponent<PlayerScript>();
        GameObject enemy = CreateObject("Shoot Enemy");
        GameObject projectile = CreateObject("Held Projectile");
        projectile.transform.SetParent(enemy.transform, false);
        projectile.AddComponent<SimpleProjectile>();
        Animator animator = CreateObject("Shoot Animator").AddComponent<Animator>();
        animator.transform.SetParent(enemy.transform, false);
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.EventMode = EnemyEventMode.Shoot;
        EnemyScript_space combat = enemy.AddComponent<EnemyScript_space>();
        SetPrivate(combat, "heldProjectile", projectile.transform);
        SetPrivate(combat, "playerScript", player);
        SetPrivate(combat, "enemyAnimator", animator);

        Assert.That(controller.ActivateFromSpot(), Is.True);
        Assert.That(controller.ActivateFromSpot(), Is.False);
    }

    [Test]
    public void FirstInitialization_CapturesPlacementAssignedAfterEnable()
    {
        GameObject enemy = CreateObject("Late Placed Enemy");
        EnemyEventController controller =
            enemy.AddComponent<EnemyEventController>();
        controller.SnapToRouteDirection();
        enemy.transform.position = new Vector3(9f, 0f, -3f);
        Initialize(controller);
        Assert.That(controller.ActivateFromSpot(), Is.True);

        enemy.transform.position = Vector3.zero;
        EnemyEventController.ResetAllForNewRun();

        Assert.That(
            enemy.transform.position,
            Is.EqualTo(new Vector3(9f, 0f, -3f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void CanonicalPrefabs_UseEventControllerWithSafeDefault()
    {
        foreach (string prefabPath in ForwardEnemyMovementSetup.EnemyPrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            EnemyEventController controller =
                prefab.GetComponent<EnemyEventController>();
            Assert.That(controller, Is.Not.Null, prefabPath);
            Assert.That(
                controller.EventMode,
                Is.EqualTo(EnemyEventMode.AttackLoop),
                prefabPath);
        }
    }

    [Test]
    public void EventSpotPrefab_UsesTheSimpleConnectionComponent()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            ForwardEnemyMovementSetup.TriggerPrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<EnemyEventActivationSpot>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<BoxCollider>().isTrigger, Is.True);
    }

    private GameObject CreateObject(string name)
    {
        var gameObject = new GameObject(name);
        SceneManager.MoveGameObjectToScene(gameObject, previewScene);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void Initialize(EnemyEventController controller)
    {
        InvokePrivate(controller, "EnsureInitialized", null);
    }

    private static void Advance(EnemyEventController controller, float deltaTime)
    {
        InvokePrivate(controller, "AdvanceEvent", new object[] { deltaTime });
    }

    private static void InvokePrivate(
        EnemyEventController controller,
        string methodName,
        object[] arguments)
    {
        MethodInfo method = typeof(EnemyEventController).GetMethod(
            methodName,
            PrivateInstance);
        Assert.That(method, Is.Not.Null, methodName);
        method.Invoke(controller, arguments);
    }

    private static void SetPrivate<T>(EnemyScript_space enemy, string fieldName, T value)
    {
        FieldInfo field = typeof(EnemyScript_space).GetField(
            fieldName,
            PrivateInstance);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(enemy, value);
    }

}
#endif
