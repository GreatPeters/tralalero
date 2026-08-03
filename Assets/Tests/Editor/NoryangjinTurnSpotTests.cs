using System.Collections;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Utils;

public sealed class NoryangjinTurnSpotTests
{
    [TearDown]
    public void TearDown()
    {
        NoryangjinTurnSpot.ResetAllForNewRun();
        TimeManager.isGameRunning = false;
        TimeManager.timeFactor = 1f;
        CanvasScript.isGameOver = false;
    }

    [Test]
    public void DirectionFromYaw_UsesAbsoluteWorldYaw()
    {
        Assert.That(
            NoryangjinTurnSpot.DirectionFromYaw(0f),
            Is.EqualTo(Vector3.forward).Using(Vector3ComparerWithEqualsOperator.Instance));
        Assert.That(
            NoryangjinTurnSpot.DirectionFromYaw(90f),
            Is.EqualTo(Vector3.right).Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void DirectionFromRotation_UsesAbsoluteWorldXAndY()
    {
        Vector3 expected =
            Quaternion.Euler(30f, 90f, 0f) * Vector3.forward;

        Assert.That(
            NoryangjinTurnSpot.DirectionFromRotation(30f, 90f),
            Is.EqualTo(expected).Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void EvaluateWorldYawTurn_UsesSmoothMidpointAndExactEnd()
    {
        Quaternion start = Quaternion.Euler(0f, 0f, 0f);
        Quaternion target = Quaternion.Euler(0f, 90f, 0f);

        Quaternion midpoint = PlayerScript.EvaluateWorldYawTurn(start, target, 0.5f);
        Quaternion end = PlayerScript.EvaluateWorldYawTurn(start, target, 1f);

        Assert.That(Mathf.DeltaAngle(midpoint.eulerAngles.y, 45f), Is.EqualTo(0f).Within(0.01f));
        Assert.That(Mathf.DeltaAngle(end.eulerAngles.y, 90f), Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void Awake_UsesInspectorForwardMoveSpeedWhenExcelDefaultsAreDisabled()
    {
        var gameObject = new GameObject("Player Speed Test");
        try
        {
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            FieldInfo useExcelField = typeof(PlayerScript).GetField(
                "useExcelCharacterDefaults",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo speedField = typeof(PlayerScript).GetField(
                "defaultForwardMoveSpeed",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo awakeMethod = typeof(PlayerScript).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(useExcelField, Is.Not.Null);
            Assert.That(speedField, Is.Not.Null);
            Assert.That(awakeMethod, Is.Not.Null);

            useExcelField.SetValue(player, false);
            speedField.SetValue(player, 7f);
            awakeMethod.Invoke(player, null);

            Assert.That(player.ForwardMoveSpeed, Is.EqualTo(7f));
            Assert.That(player.originalMoveSpeed, Is.EqualTo(7f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void TurnSpotDuration_ClampsNegativeValuesToImmediateTurn()
    {
        var gameObject = new GameObject("Turn Spot Test");
        try
        {
            NoryangjinTurnSpot turnSpot = gameObject.AddComponent<NoryangjinTurnSpot>();

            turnSpot.TurnDurationSeconds = -2f;

            Assert.That(turnSpot.TurnDurationSeconds, Is.EqualTo(0f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void ResolveTriggerPlayer_AcceptsRootColliderAndIgnoresWeaponChildCollider()
    {
        var playerObject = new GameObject("Turn Spot Trigger Player");
        try
        {
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            BoxCollider rootCollider = playerObject.AddComponent<BoxCollider>();
            var weaponObject = new GameObject("Weapon Collider");
            weaponObject.transform.SetParent(playerObject.transform);
            BoxCollider childCollider = weaponObject.AddComponent<BoxCollider>();

            Assert.That(
                NoryangjinTurnSpot.ResolveTriggerPlayer(rootCollider),
                Is.SameAs(player));
            Assert.That(
                NoryangjinTurnSpot.ResolveTriggerPlayer(childCollider),
                Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void SuccessfulActivation_DisablesTurnSpotUntilNextRunReset()
    {
        var turnSpotObject = new GameObject("One Shot Turn Spot");
        var playerObject = new GameObject("One Shot Turn Player");
        try
        {
            NoryangjinTurnSpot turnSpot =
                turnSpotObject.AddComponent<NoryangjinTurnSpot>();
            playerObject.tag = "Player";
            playerObject.AddComponent<Rigidbody>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;
            turnSpot.TargetXDegrees = 25f;
            turnSpot.TargetYawDegrees = 90f;
            turnSpot.TurnDurationSeconds = 0f;

            MethodInfo tryActivate = typeof(NoryangjinTurnSpot).GetMethod(
                "TryActivate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tryActivate, Is.Not.Null);

            bool accepted = (bool)tryActivate.Invoke(turnSpot, new object[] { player });

            Assert.That(accepted, Is.True);
            Assert.That(turnSpotObject.activeSelf, Is.False);
            Assert.That(
                Mathf.DeltaAngle(playerObject.transform.eulerAngles.y, 90f),
                Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                Mathf.DeltaAngle(playerObject.transform.eulerAngles.x, 25f),
                Is.EqualTo(0f).Within(0.01f));

            NoryangjinTurnSpot.ResetAllForNewRun();

            Assert.That(turnSpotObject.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(turnSpotObject);
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void RouteProgress_CountsActiveAndConsumedCheckpointsOnly()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        Scene otherScene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Route Progress Root");
            SceneManager.MoveGameObjectToScene(root, scene);

            var activeObject = new GameObject("Active Checkpoint");
            activeObject.transform.SetParent(root.transform);
            NoryangjinTurnSpot active =
                activeObject.AddComponent<NoryangjinTurnSpot>();
            active.TurnDurationSeconds = 0f;

            var inactiveObject = new GameObject("Authored Inactive Checkpoint");
            inactiveObject.transform.SetParent(root.transform);
            inactiveObject.AddComponent<NoryangjinTurnSpot>();
            inactiveObject.SetActive(false);

            var previewObject = new GameObject("Placement Preview Checkpoint");
            previewObject.transform.SetParent(root.transform);
            previewObject.AddComponent<NoryangjinTurnSpot>();
            previewObject.hideFlags = HideFlags.HideAndDontSave;

            var otherSceneObject = new GameObject("Other Scene Checkpoint");
            SceneManager.MoveGameObjectToScene(otherSceneObject, otherScene);
            otherSceneObject.AddComponent<NoryangjinTurnSpot>();

            Assert.That(
                NoryangjinTurnSpot.TryGetRouteProgress(
                    scene,
                    out int completed,
                    out int total),
                Is.True);
            Assert.That(completed, Is.Zero);
            Assert.That(total, Is.EqualTo(1));

            var playerObject = new GameObject("Route Progress Player");
            SceneManager.MoveGameObjectToScene(playerObject, scene);
            playerObject.tag = "Player";
            playerObject.AddComponent<Rigidbody>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;

            MethodInfo tryActivate = typeof(NoryangjinTurnSpot).GetMethod(
                "TryActivate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tryActivate, Is.Not.Null);
            Assert.That(
                (bool)tryActivate.Invoke(active, new object[] { player }),
                Is.True);

            Assert.That(
                NoryangjinTurnSpot.TryGetRouteProgress(
                    scene,
                    out completed,
                    out total),
                Is.True);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(total, Is.EqualTo(1));
        }
        finally
        {
            NoryangjinTurnSpot.ResetAllForNewRun();
            EditorSceneManager.ClosePreviewScene(scene);
            EditorSceneManager.ClosePreviewScene(otherScene);
        }
    }

    [Test]
    public void RejectedActivation_LeavesTurnSpotEnabled()
    {
        var turnSpotObject = new GameObject("Rejected Turn Spot");
        var playerObject = new GameObject("Rejected Turn Player");
        try
        {
            NoryangjinTurnSpot turnSpot =
                turnSpotObject.AddComponent<NoryangjinTurnSpot>();
            playerObject.tag = "Player";
            playerObject.AddComponent<Rigidbody>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = false;

            MethodInfo tryActivate = typeof(NoryangjinTurnSpot).GetMethod(
                "TryActivate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tryActivate, Is.Not.Null);

            bool accepted = (bool)tryActivate.Invoke(turnSpot, new object[] { player });

            Assert.That(accepted, Is.False);
            Assert.That(turnSpotObject.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(turnSpotObject);
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void ImmediateWorldRotation_AppliesTargetXAndYAndRestoresConstraints()
    {
        var gameObject = new GameObject("Player Turn Test");
        try
        {
            gameObject.tag = "Player";
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.constraints = (RigidbodyConstraints)124;
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;

            bool accepted =
                player.RequestWorldRotation(25f, 90f, 0f, gameObject);

            Assert.That(accepted, Is.True);
            Assert.That(player.IsWorldYawTurnActive, Is.False);
            Assert.That(Mathf.DeltaAngle(gameObject.transform.eulerAngles.x, 25f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Mathf.DeltaAngle(gameObject.transform.eulerAngles.y, 90f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(rigidbody.constraints, Is.EqualTo((RigidbodyConstraints)124));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void ImmediateWorldRotation_RebasesLateralRangeAtTurnSpotCenter()
    {
        var turnSpotObject = new GameObject("Lane Center Turn Spot");
        var playerObject = new GameObject("Offset Turn Player");
        try
        {
            turnSpotObject.transform.position = new Vector3(10f, 0f, 20f);
            NoryangjinTurnSpot turnSpot =
                turnSpotObject.AddComponent<NoryangjinTurnSpot>();
            playerObject.tag = "Player";
            playerObject.transform.position = new Vector3(11.5f, 0f, 19.5f);
            playerObject.AddComponent<Rigidbody>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;

            Assert.That(
                player.RequestWorldRotation(0f, 90f, 0f, turnSpot),
                Is.True);

            FieldInfo routeLaneOriginField = typeof(PlayerScript).GetField(
                "routeLaneOrigin",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(routeLaneOriginField, Is.Not.Null);
            Assert.That(
                (Vector3)routeLaneOriginField.GetValue(player),
                Is.EqualTo(turnSpotObject.transform.position)
                    .Using(Vector3ComparerWithEqualsOperator.Instance),
                "After a corner, xRange must remain centered on the route trigger, " +
                "not on the player's offset trigger-entry position.");
        }
        finally
        {
            Object.DestroyImmediate(turnSpotObject);
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void TimedTurnSpotRotation_UsesDisabledTurnSpotAsLateralRangeCenter()
    {
        var turnSpotObject = new GameObject("Timed Lane Center Turn Spot");
        var playerObject = new GameObject("Timed Offset Turn Player");
        try
        {
            turnSpotObject.transform.position = new Vector3(-4f, 0f, 12f);
            NoryangjinTurnSpot turnSpot =
                turnSpotObject.AddComponent<NoryangjinTurnSpot>();
            turnSpot.TargetYawDegrees = 90f;
            turnSpot.TurnDurationSeconds = Time.fixedDeltaTime;
            playerObject.tag = "Player";
            playerObject.transform.position = new Vector3(-3f, 0f, 11.5f);
            playerObject.AddComponent<Rigidbody>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;

            MethodInfo tryActivate = typeof(NoryangjinTurnSpot).GetMethod(
                "TryActivate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo updateTurn = typeof(PlayerScript).GetMethod(
                "UpdateWorldYawTurn",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo routeLaneOriginField = typeof(PlayerScript).GetField(
                "routeLaneOrigin",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tryActivate, Is.Not.Null);
            Assert.That(updateTurn, Is.Not.Null);
            Assert.That(routeLaneOriginField, Is.Not.Null);

            Assert.That(
                (bool)tryActivate.Invoke(turnSpot, new object[] { player }),
                Is.True);
            Assert.That(turnSpotObject.activeSelf, Is.False);

            updateTurn.Invoke(player, null);

            Assert.That(player.IsWorldYawTurnActive, Is.False);
            Assert.That(
                (Vector3)routeLaneOriginField.GetValue(player),
                Is.EqualTo(turnSpotObject.transform.position)
                    .Using(Vector3ComparerWithEqualsOperator.Instance));
        }
        finally
        {
            Object.DestroyImmediate(turnSpotObject);
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void TimedWorldRotation_LocksPositionAndRestoresConstraintsOnCompletion()
    {
        var gameObject = new GameObject("Timed Player Turn Test");
        try
        {
            gameObject.tag = "Player";
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.constraints = (RigidbodyConstraints)124;
            gameObject.transform.position = new Vector3(2f, 3f, 4f);
            rigidbody.position = gameObject.transform.position;
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;
            TimeManager.timeFactor = 1f;

            float duration = Time.fixedDeltaTime * 2f;
            Assert.That(
                player.RequestWorldRotation(25f, 90f, duration, gameObject),
                Is.True);

            MethodInfo updateTurn = typeof(PlayerScript).GetMethod(
                "UpdateWorldYawTurn",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateTurn, Is.Not.Null);

            gameObject.transform.position = new Vector3(20f, 30f, 40f);
            rigidbody.position = gameObject.transform.position;
            updateTurn.Invoke(player, null);

            Assert.That(player.IsWorldYawTurnActive, Is.True);
            Assert.That(
                gameObject.transform.position,
                Is.EqualTo(new Vector3(2f, 3f, 4f))
                    .Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(
                rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationX),
                Is.False);
            Assert.That(
                rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY),
                Is.False);

            updateTurn.Invoke(player, null);

            Assert.That(player.IsWorldYawTurnActive, Is.False);
            Assert.That(
                Mathf.DeltaAngle(gameObject.transform.eulerAngles.x, 25f),
                Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                Mathf.DeltaAngle(gameObject.transform.eulerAngles.y, 90f),
                Is.EqualTo(0f).Within(0.01f));
            Assert.That(rigidbody.constraints, Is.EqualTo((RigidbodyConstraints)124));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void ResetState_DuringTurnPreservesExternallyAssignedPosition()
    {
        var gameObject = new GameObject("Player Reset During Turn Test");
        try
        {
            gameObject.tag = "Player";
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.constraints = (RigidbodyConstraints)124;
            gameObject.transform.position = new Vector3(1f, 2f, 3f);
            rigidbody.position = gameObject.transform.position;
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;

            Assert.That(
                player.RequestWorldRotation(25f, 90f, 1f, gameObject),
                Is.True);

            Vector3 resetPosition = new Vector3(10f, 20f, 30f);
            gameObject.transform.position = resetPosition;
            rigidbody.position = resetPosition;
            player.ResetState();

            Assert.That(player.IsWorldYawTurnActive, Is.False);
            Assert.That(
                gameObject.transform.position,
                Is.EqualTo(resetPosition)
                    .Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.constraints, Is.EqualTo((RigidbodyConstraints)124));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void CancelWorldYawTurn_RestoresConstraintsWithoutChangingMovementFlag()
    {
        var gameObject = new GameObject("Player Turn Cancel Test");
        try
        {
            gameObject.tag = "Player";
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.constraints = (RigidbodyConstraints)124;
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            player.movement = false;
            rigidbody.linearVelocity = new Vector3(3f, 0f, 0f);
            rigidbody.angularVelocity = new Vector3(0f, 2f, 0f);
            TimeManager.isGameRunning = true;

            Assert.That(player.RequestWorldYawTurn(135f, 1f, gameObject), Is.True);
            Assert.That(player.IsWorldYawTurnActive, Is.True);
            Assert.That(
                rigidbody.linearVelocity,
                Is.EqualTo(Vector3.zero).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(
                rigidbody.angularVelocity,
                Is.EqualTo(Vector3.zero).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(
                rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY),
                Is.False);
            Assert.That(
                rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationX),
                Is.False);
            Assert.That(
                rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezePositionX),
                Is.True);
            Assert.That(
                rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezePositionZ),
                Is.True);

            player.CancelWorldYawTurn();

            Assert.That(player.IsWorldYawTurnActive, Is.False);
            Assert.That(player.movement, Is.False);
            Assert.That(rigidbody.constraints, Is.EqualTo((RigidbodyConstraints)124));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void LegacyWorldYawTurn_PreservesCurrentWorldX()
    {
        var gameObject = new GameObject("Legacy Player Yaw Turn Test");
        try
        {
            gameObject.tag = "Player";
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.rotation = Quaternion.Euler(20f, 0f, 0f);
            gameObject.transform.rotation = rigidbody.rotation;
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            TimeManager.isGameRunning = true;

            bool accepted =
                player.RequestWorldYawTurn(90f, 0f, gameObject);

            Assert.That(accepted, Is.True);
            Assert.That(
                Mathf.DeltaAngle(gameObject.transform.eulerAngles.x, 20f),
                Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                Mathf.DeltaAngle(gameObject.transform.eulerAngles.y, 90f),
                Is.EqualTo(0f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void TurnSpotPrefab_IsRendererlessTriggerWithRuntimeComponent()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            NoryangjinMapToolWindow.TurnSpotPrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<NoryangjinTurnSpot>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<BoxCollider>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<BoxCollider>().isTrigger, Is.True);
        Assert.That(prefab.GetComponentsInChildren<Renderer>(true), Is.Empty);
    }

    [Test]
    public void MapToolPalette_ContainsOneTurnSpotWithDedicatedIcon()
    {
        var window = ScriptableObject.CreateInstance<NoryangjinMapToolWindow>();
        try
        {
            MethodInfo getPaletteItems = typeof(NoryangjinMapToolWindow).GetMethod(
                "GetPaletteItems",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(getPaletteItems, Is.Not.Null);

            var items = (IEnumerable)getPaletteItems.Invoke(window, null);
            int matches = items.Cast<object>().Count(item =>
            {
                PropertyInfo prefabPath = item.GetType().GetProperty("PrefabPath");
                return NoryangjinMapToolWindow.IsTurnSpotPrefabPath(
                    prefabPath?.GetValue(item) as string);
            });

            Assert.That(matches, Is.EqualTo(1));
            Assert.That(
                NoryangjinMapToolWindow.GetSpecialPaletteIconText(
                    NoryangjinMapToolWindow.TurnSpotPrefabPath),
                Is.EqualTo(NoryangjinMapToolWindow.TurnSpotPaletteItemIconText));
        }
        finally
        {
            Object.DestroyImmediate(window);
        }
    }

    [Test]
    public void MapToolTurnArrowDirection_MatchesRuntimeYaw()
    {
        Assert.That(
            NoryangjinMapToolWindow.BuildTurnSpotArrowDirection(0f),
            Is.EqualTo(Vector3.forward).Using(Vector3ComparerWithEqualsOperator.Instance));
        Assert.That(
            NoryangjinMapToolWindow.BuildTurnSpotArrowDirection(90f),
            Is.EqualTo(Vector3.right).Using(Vector3ComparerWithEqualsOperator.Instance));
        Assert.That(
            NoryangjinMapToolWindow.BuildTurnSpotArrowDirection(30f, 90f),
            Is.EqualTo(NoryangjinTurnSpot.DirectionFromRotation(30f, 90f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));

        Quaternion verticalArrowRotation =
            NoryangjinMapToolWindow.BuildTurnSpotArrowRotation(Vector3.down);
        Assert.That(
            verticalArrowRotation * Vector3.forward,
            Is.EqualTo(Vector3.down)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [TestCase(0f, "↑")]
    [TestCase(45f, "↗")]
    [TestCase(90f, "→")]
    [TestCase(135f, "↘")]
    [TestCase(180f, "↓")]
    [TestCase(-135f, "↙")]
    [TestCase(-90f, "←")]
    [TestCase(-45f, "↖")]
    [TestCase(360f, "↑")]
    public void MapToolTurnSpotYawDirectionGlyph_MatchesCompassDirection(float yaw, string expected)
    {
        Assert.That(NoryangjinMapToolWindow.BuildTurnSpotYawDirectionGlyph(yaw), Is.EqualTo(expected));
    }

    [Test]
    public void MapToolTurnSpotSettingsAndLabels_KeepXAboveYDataTogether()
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            var turnSpotObject = new GameObject("Turn Spot Settings Test");
            SceneManager.MoveGameObjectToScene(turnSpotObject, previewScene);
            NoryangjinTurnSpot turnSpot =
                turnSpotObject.AddComponent<NoryangjinTurnSpot>();

            NoryangjinMapToolWindow.ApplyTurnSpotSettings(
                turnSpot,
                25f,
                90f,
                0.5f);

            Assert.That(turnSpot.TargetXDegrees, Is.EqualTo(25f));
            Assert.That(turnSpot.TargetYawDegrees, Is.EqualTo(90f));
            Assert.That(turnSpot.TurnDurationSeconds, Is.EqualTo(0.5f));
            Assert.That(
                NoryangjinMapToolWindow.BuildTurnSpotMarkerLabel(
                    25f,
                    90f,
                    0.5f,
                    false),
                Is.EqualTo("↻ X 25° Y 90° →"));
            Assert.That(
                NoryangjinMapToolWindow.BuildTurnSpotMarkerLabel(
                    25f,
                    90f,
                    0.5f,
                    true),
                Is.EqualTo("↻ X 25° Y 90° → · 0.5초"));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    [Test]
    public void MapToolTurnSpotMarkers_CollectAllSpotsOnlyFromTargetScene()
    {
        Scene targetScene = EditorSceneManager.NewPreviewScene();
        Scene otherScene = EditorSceneManager.NewPreviewScene();
        try
        {
            var targetRoot = new GameObject("Target Scene Root");
            SceneManager.MoveGameObjectToScene(targetRoot, targetScene);

            var activeSpotObject = new GameObject("Active Turn Spot");
            activeSpotObject.transform.SetParent(targetRoot.transform);
            NoryangjinTurnSpot activeSpot =
                activeSpotObject.AddComponent<NoryangjinTurnSpot>();

            var inactiveSpotObject = new GameObject("Inactive Turn Spot");
            inactiveSpotObject.transform.SetParent(targetRoot.transform);
            NoryangjinTurnSpot inactiveSpot =
                inactiveSpotObject.AddComponent<NoryangjinTurnSpot>();
            inactiveSpotObject.SetActive(false);

            var previewSpotObject = new GameObject("Placement Preview Turn Spot");
            previewSpotObject.transform.SetParent(targetRoot.transform);
            NoryangjinTurnSpot previewSpot =
                previewSpotObject.AddComponent<NoryangjinTurnSpot>();
            previewSpotObject.hideFlags = HideFlags.HideAndDontSave;

            var otherSpotObject = new GameObject("Other Scene Turn Spot");
            SceneManager.MoveGameObjectToScene(otherSpotObject, otherScene);
            NoryangjinTurnSpot otherSpot =
                otherSpotObject.AddComponent<NoryangjinTurnSpot>();

            NoryangjinTurnSpot[] result =
                NoryangjinMapToolWindow.CollectTurnSpots(targetScene);

            Assert.That(result, Is.EquivalentTo(new[] { activeSpot, inactiveSpot }));
            Assert.That(result.Any(spot => spot == previewSpot), Is.False);
            Assert.That(result.Any(spot => spot == otherSpot), Is.False);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(targetScene);
            EditorSceneManager.ClosePreviewScene(otherScene);
        }
    }

    [Test]
    public void MapToolTurnSpotSelectionFootprint_UsesEntireTriggerArea()
    {
        var turnSpotObject = new GameObject("Turn Spot Selection Footprint");
        try
        {
            NoryangjinTurnSpot turnSpot =
                turnSpotObject.AddComponent<NoryangjinTurnSpot>();
            BoxCollider trigger = turnSpotObject.GetComponent<BoxCollider>();
            trigger.size = new Vector3(3f, 1f, 1f);

            var cells = NoryangjinMapToolWindow.BuildTurnSpotSelectionFootprintCells(
                turnSpot,
                Vector3.zero,
                1f);

            Assert.That(
                cells,
                Is.EquivalentTo(
                    new[]
                    {
                        new Vector2Int(-1, 0),
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0)
                    }));
        }
        finally
        {
            Object.DestroyImmediate(turnSpotObject);
        }
    }

    [Test]
    public void ForwardInstaller_PrefersAttachedOriginalOverNestedForwardNames()
    {
        var playerObject = new GameObject("Installer Player Test");
        try
        {
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            var sharks = new GameObject("Sharks");
            sharks.transform.SetParent(playerObject.transform);
            var nestedForwardOriginal = new GameObject("Original");
            nestedForwardOriginal.transform.SetParent(sharks.transform);
            var attachedOriginal = new GameObject("Original");
            attachedOriginal.transform.SetParent(playerObject.transform);

            GameObject result = NoryangjinForwardGameplayInstaller.FindOriginalVisual(
                playerObject.scene,
                player);

            Assert.That(result, Is.SameAs(attachedOriginal));
        }
        finally
        {
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void ForwardInstaller_RestoresDisabledOriginalRenderer()
    {
        GameObject original = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            original.name = "Original";
            Renderer renderer = original.GetComponent<Renderer>();
            renderer.enabled = false;

            NoryangjinForwardGameplayInstaller.RestoreOriginalVisualRenderers(original);

            Assert.That(renderer.enabled, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(original);
        }
    }

    [Test]
    public void RefreshSharkAnimator_PrefersVisibleDirectOriginal()
    {
        var playerObject = new GameObject("Visible Original Animator Player");
        try
        {
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            var sharks = new GameObject("Sharks");
            sharks.transform.SetParent(playerObject.transform);
            var hiddenOriginal = new GameObject("Original");
            hiddenOriginal.transform.SetParent(sharks.transform);
            Animator hiddenAnimator = hiddenOriginal.AddComponent<Animator>();
            var visibleOriginal = new GameObject("Original");
            visibleOriginal.transform.SetParent(playerObject.transform);
            Animator visibleAnimator = visibleOriginal.AddComponent<Animator>();
            FieldInfo sharksField = typeof(PlayerScript).GetField(
                "sharksGO",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(sharksField, Is.Not.Null);
            sharksField.SetValue(player, sharks);

            player.RefreshSharkAnimator();

            Assert.That(player.sharkAnim, Is.SameAs(visibleAnimator));
            Assert.That(player.sharkAnim, Is.Not.SameAs(hiddenAnimator));
        }
        finally
        {
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void ForwardInstaller_EnsuresVisibleOriginalAnimatorConfigurationOnce()
    {
        const string controllerPath = "Assets/JH/Anim/Shark/Original.controller";
        const string avatarPath =
            "Assets/JH/Model/Player/TungTungTung/Animation_Run_03_withSkin.fbx";
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(avatarPath)
            .OfType<Avatar>()
            .FirstOrDefault();
        var playerObject = new GameObject("Installer Animator Player");

        try
        {
            Assert.That(controller, Is.Not.Null, $"Missing controller at {controllerPath}.");
            Assert.That(avatar, Is.Not.Null, $"Missing Avatar sub-asset at {avatarPath}.");

            var sharks = new GameObject("Sharks");
            sharks.transform.SetParent(playerObject.transform);
            var sourceOriginal = new GameObject("Original");
            sourceOriginal.transform.SetParent(sharks.transform);
            Animator sourceAnimator = sourceOriginal.AddComponent<Animator>();
            sourceAnimator.runtimeAnimatorController = controller;
            sourceAnimator.avatar = avatar;
            sourceAnimator.applyRootMotion = true;
            sourceAnimator.updateMode = AnimatorUpdateMode.Fixed;
            sourceAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var visibleOriginal = new GameObject("Original");
            visibleOriginal.transform.SetParent(playerObject.transform);

            Animator first = NoryangjinForwardGameplayInstaller.EnsureOriginalAnimator(
                playerObject,
                visibleOriginal);
            Animator second = NoryangjinForwardGameplayInstaller.EnsureOriginalAnimator(
                playerObject,
                visibleOriginal);

            Assert.That(second, Is.SameAs(first));
            Assert.That(first, Is.SameAs(visibleOriginal.GetComponent<Animator>()));
            Assert.That(visibleOriginal.GetComponents<Animator>(), Has.Length.EqualTo(1));
            Assert.That(first.enabled, Is.True);
            Assert.That(first.runtimeAnimatorController, Is.SameAs(controller));
            Assert.That(first.avatar, Is.SameAs(avatar));
            Assert.That(first.applyRootMotion, Is.True);
            Assert.That(first.updateMode, Is.EqualTo(AnimatorUpdateMode.Fixed));
            Assert.That(
                first.cullingMode,
                Is.EqualTo(AnimatorCullingMode.CullUpdateTransforms));
        }
        finally
        {
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void ForwardInstaller_RepairsExistingOriginalAnimatorWithoutDuplicatingIt()
    {
        const string controllerPath = "Assets/JH/Anim/Shark/Original.controller";
        const string avatarPath =
            "Assets/JH/Model/Player/TungTungTung/Animation_Run_03_withSkin.fbx";
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(avatarPath)
            .OfType<Avatar>()
            .FirstOrDefault();
        var playerObject = new GameObject("Installer Existing Animator Player");

        try
        {
            Assert.That(controller, Is.Not.Null, $"Missing controller at {controllerPath}.");
            Assert.That(avatar, Is.Not.Null, $"Missing Avatar sub-asset at {avatarPath}.");

            var sharks = new GameObject("Sharks");
            sharks.transform.SetParent(playerObject.transform);
            var sourceOriginal = new GameObject("Original");
            sourceOriginal.transform.SetParent(sharks.transform);
            Animator sourceAnimator = sourceOriginal.AddComponent<Animator>();
            sourceAnimator.runtimeAnimatorController = controller;
            sourceAnimator.avatar = avatar;
            sourceAnimator.applyRootMotion = true;
            sourceAnimator.updateMode = AnimatorUpdateMode.Fixed;
            sourceAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var visibleOriginal = new GameObject("Original");
            visibleOriginal.transform.SetParent(playerObject.transform);
            Animator existingAnimator = visibleOriginal.AddComponent<Animator>();
            existingAnimator.runtimeAnimatorController = null;
            existingAnimator.avatar = null;
            existingAnimator.applyRootMotion = false;
            existingAnimator.updateMode = AnimatorUpdateMode.Normal;
            existingAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            existingAnimator.enabled = false;

            Animator repaired = NoryangjinForwardGameplayInstaller.EnsureOriginalAnimator(
                playerObject,
                visibleOriginal);

            Assert.That(repaired, Is.SameAs(existingAnimator));
            Assert.That(visibleOriginal.GetComponents<Animator>(), Has.Length.EqualTo(1));
            Assert.That(repaired.enabled, Is.True);
            Assert.That(repaired.runtimeAnimatorController, Is.SameAs(controller));
            Assert.That(repaired.avatar, Is.SameAs(avatar));
            Assert.That(repaired.applyRootMotion, Is.True);
            Assert.That(repaired.updateMode, Is.EqualTo(AnimatorUpdateMode.Fixed));
            Assert.That(
                repaired.cullingMode,
                Is.EqualTo(AnimatorCullingMode.CullUpdateTransforms));
        }
        finally
        {
            Object.DestroyImmediate(playerObject);
        }
    }

    [TestCase("Assets/ShooterSurvival/Prefabs/Weapons/PrefabBullet.prefab")]
    [TestCase("Assets/ShooterSurvival/Prefabs/Weapons/PrefabBullet_Bomb.prefab")]
    public void ProjectileRotation_AlignsVisualAxisAndOverwritesStalePose(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        MethodInfo buildRotation = typeof(WeaponScript).GetMethod(
            "BuildProjectileRotation",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(buildRotation, Is.Not.Null);

        GameObject projectile = Object.Instantiate(prefab);
        try
        {
            Transform gfx = projectile.transform.Find("GFX");
            Assert.That(gfx, Is.Not.Null);

            Vector3 firstDirection = Vector3.right;
            projectile.transform.rotation = Quaternion.Euler(15f, 127f, 33f);
            projectile.transform.rotation =
                (Quaternion)buildRotation.Invoke(null, new object[] { firstDirection });

            Assert.That(
                Vector3.Angle(projectile.transform.forward, firstDirection),
                Is.LessThan(0.01f));
            Assert.That(
                Vector3.Angle(gfx.up, firstDirection),
                Is.LessThan(0.01f));

            Vector3 reusedDirection = Vector3.back;
            projectile.transform.rotation = Quaternion.Euler(40f, 19f, 71f);
            projectile.transform.rotation =
                (Quaternion)buildRotation.Invoke(null, new object[] { reusedDirection });

            Assert.That(
                Vector3.Angle(projectile.transform.forward, reusedDirection),
                Is.LessThan(0.01f));
            Assert.That(
                Vector3.Angle(gfx.up, reusedDirection),
                Is.LessThan(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(projectile);
        }
    }

    [TestCase(
        "Assets/ShooterSurvival/Prefabs/Weapons/PrefabBullet.prefab",
        BulletKind.Water)]
    [TestCase(
        "Assets/ShooterSurvival/Prefabs/Weapons/PrefabBullet_Bomb.prefab",
        BulletKind.Bomb)]
    public void ProjectileDurationExpiry_ReusesRegisteredRootWithVisualIntact(
        string prefabPath,
        BulletKind bulletKind)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        FieldInfo reverseField = typeof(BulletPooler).GetField(
            "reverse",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo poolerField = typeof(BulletScript).GetField(
            "bulletPooler",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo advanceLifetime = typeof(BulletScript).GetMethod(
            "AdvanceLifetime",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo resetStatics = typeof(BulletScript).GetMethod(
            "ResetStatics",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(reverseField, Is.Not.Null);
        Assert.That(poolerField, Is.Not.Null);
        Assert.That(advanceLifetime, Is.Not.Null);
        Assert.That(resetStatics, Is.Not.Null);

        var poolerObject = new GameObject("Projectile Root Pool Test");
        poolerObject.SetActive(false);
        BulletPooler pooler = poolerObject.AddComponent<BulletPooler>();
        var caller = new GameObject("Projectile Root Pool Caller");
        GameObject projectileRoot = Object.Instantiate(prefab);

        try
        {
            BulletScript projectile =
                projectileRoot.GetComponentInChildren<BulletScript>(true);
            Assert.That(projectile, Is.Not.Null);

            var reverse = (IDictionary)reverseField.GetValue(pooler);
            reverse.Add(projectileRoot, bulletKind);
            poolerField.SetValue(projectile, pooler);

            BulletScript.ConfigureMissileDefaults(16f, 0.01f);
            projectile.SetDirection(Vector3.right);
            advanceLifetime.Invoke(projectile, new object[] { 0.02f });

            GameObject reused = pooler.Get(bulletKind, caller.transform);

            Assert.That(reused, Is.SameAs(projectileRoot));
            Assert.That(reused.GetComponentInChildren<BulletScript>(true), Is.SameAs(projectile));
            Assert.That(reused.transform.Find("GFX"), Is.Not.Null);
        }
        finally
        {
            resetStatics.Invoke(null, null);
            Object.DestroyImmediate(caller);
            Object.DestroyImmediate(poolerObject);
            if (projectileRoot != null)
                Object.DestroyImmediate(projectileRoot);
        }
    }

    [TestCase(
        "Assets/ShooterSurvival/Prefabs/Weapons/PrefabBullet.prefab",
        BulletKind.Water,
        "EnemyTag")]
    [TestCase(
        "Assets/ShooterSurvival/Prefabs/Weapons/PrefabBullet.prefab",
        BulletKind.Water,
        "Obstacle")]
    [TestCase(
        "Assets/ShooterSurvival/Prefabs/Weapons/PrefabBullet_Bomb.prefab",
        BulletKind.Bomb,
        "EnemyTag")]
    [TestCase(
        "Assets/ShooterSurvival/Prefabs/Weapons/PrefabBullet_Bomb.prefab",
        BulletKind.Bomb,
        "Obstacle")]
    public void ProjectileCollision_ReturnsAndReusesRegisteredRootWithVisualIntact(
        string prefabPath,
        BulletKind bulletKind,
        string collisionTag)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        FieldInfo reverseField = typeof(BulletPooler).GetField(
            "reverse",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo poolerField = typeof(BulletScript).GetField(
            "bulletPooler",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo onTriggerEnter = typeof(BulletScript).GetMethod(
            "OnTriggerEnter",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(reverseField, Is.Not.Null);
        Assert.That(poolerField, Is.Not.Null);
        Assert.That(onTriggerEnter, Is.Not.Null);

        var poolerObject = new GameObject("Projectile Collision Pool Test");
        poolerObject.SetActive(false);
        BulletPooler pooler = poolerObject.AddComponent<BulletPooler>();
        var caller = new GameObject("Projectile Collision Pool Caller");
        var collisionObject = new GameObject($"Projectile Collision {collisionTag}");
        collisionObject.tag = collisionTag;
        Collider collision = collisionObject.AddComponent<BoxCollider>();
        GameObject projectileRoot = Object.Instantiate(prefab);

        try
        {
            BulletScript projectile =
                projectileRoot.GetComponentInChildren<BulletScript>(true);
            Transform gfx = projectileRoot.transform.Find("GFX");
            Assert.That(projectile, Is.Not.Null);
            Assert.That(gfx, Is.Not.Null);

            var reverse = (IDictionary)reverseField.GetValue(pooler);
            reverse.Add(projectileRoot, bulletKind);
            poolerField.SetValue(projectile, pooler);
            projectile.SetDirection(Vector3.forward);

            onTriggerEnter.Invoke(projectile, new object[] { collision });
            GameObject reused = pooler.Get(bulletKind, caller.transform);

            Assert.That(reused, Is.SameAs(projectileRoot));
            Assert.That(reused.activeInHierarchy, Is.True);
            Assert.That(reused.GetComponentInChildren<BulletScript>(true), Is.SameAs(projectile));
            Assert.That(reused.transform.Find("GFX"), Is.SameAs(gfx));
            Assert.That(gfx.gameObject.activeInHierarchy, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(collisionObject);
            Object.DestroyImmediate(caller);
            Object.DestroyImmediate(poolerObject);
            if (projectileRoot != null)
                Object.DestroyImmediate(projectileRoot);
        }
    }

    [Test]
    public void ForwardInstaller_ProjectileMuzzleIsIdempotentAndAlignedWithVisibleMouth()
    {
        var playerRoot = new GameObject("Noryangjin Player Muzzle Test");
        var originalVisual = new GameObject("Original");
        originalVisual.transform.SetParent(playerRoot.transform);
        var armature = new GameObject("Armature");
        armature.transform.SetParent(originalVisual.transform);
        var hips = new GameObject("Hips");
        hips.transform.SetParent(armature.transform);
        var chest = new GameObject("chest");
        chest.transform.SetParent(hips.transform);
        var head = new GameObject("head");
        head.transform.SetParent(chest.transform);
        var mouth = new GameObject("headend");
        mouth.transform.SetParent(head.transform);

        try
        {
            playerRoot.transform.rotation = Quaternion.Euler(0f, 73f, 0f);
            mouth.transform.position = new Vector3(4f, 2f, -3f);

            Transform first = NoryangjinForwardGameplayInstaller
                .EnsureOriginalProjectileMuzzle(playerRoot, originalVisual);
            Transform second = NoryangjinForwardGameplayInstaller
                .EnsureOriginalProjectileMuzzle(playerRoot, originalVisual);

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.parent, Is.SameAs(mouth.transform));
            Assert.That(
                mouth.transform.Cast<Transform>()
                    .Count(child =>
                        child.name ==
                        NoryangjinForwardGameplayInstaller.OriginalProjectileMuzzleName),
                Is.EqualTo(1));

            Vector3 mouthOffset = first.position - mouth.transform.position;
            Assert.That(
                Vector3.Angle(mouthOffset, playerRoot.transform.forward),
                Is.LessThan(0.01f));
            Assert.That(
                Vector3.Dot(mouthOffset, playerRoot.transform.forward),
                Is.EqualTo(
                        NoryangjinForwardGameplayInstaller
                            .OriginalProjectileMuzzleForwardOffset)
                    .Within(0.001f));
            Assert.That(
                Vector3.Angle(first.up, playerRoot.transform.forward),
                Is.LessThan(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(playerRoot);
        }
    }

    [Test]
    public void ForwardInstaller_BuildSceneListIsOrderedEnabledAndDeduplicated()
    {
        const string otherScene = "Assets/Scenes/Other.unity";
        EditorBuildSettingsScene[] result =
            NoryangjinForwardGameplayInstaller.BuildTargetSceneList(
                new[]
                {
                    new EditorBuildSettingsScene(
                        NoryangjinForwardGameplayInstaller.TargetScenePath,
                        false),
                    new EditorBuildSettingsScene(otherScene, true),
                    new EditorBuildSettingsScene(
                        NoryangjinForwardGameplayInstaller.SourceScenePath,
                        false),
                    new EditorBuildSettingsScene(
                        NoryangjinForwardGameplayInstaller.TargetScenePath,
                        true),
                });

        Assert.That(result[0].path, Is.EqualTo(NoryangjinForwardGameplayInstaller.TargetScenePath));
        Assert.That(result[0].enabled, Is.True);
        Assert.That(result[1].path, Is.EqualTo(NoryangjinForwardGameplayInstaller.SourceScenePath));
        Assert.That(result[1].enabled, Is.True);
        Assert.That(
            result.Count(scene => scene.path == NoryangjinForwardGameplayInstaller.SourceScenePath),
            Is.EqualTo(1));
        Assert.That(
            result.Count(scene => scene.path == NoryangjinForwardGameplayInstaller.TargetScenePath),
            Is.EqualTo(1));
        Assert.That(result.Any(scene => scene.path == otherScene && scene.enabled), Is.True);
    }
}
