#if UNITY_EDITOR
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class EnemyThrowDirectionTests
{
    private const BindingFlags StaticMethodFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags InstanceMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Test]
    public void GetPlayerAimPoint_UsesPlayerRootColliderCenter()
    {
        var playerObject = new GameObject("Throw Target Player");
        var enemyObject = new GameObject("Throw Aim Enemy");
        try
        {
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            CapsuleCollider playerCollider = playerObject.AddComponent<CapsuleCollider>();
            playerObject.transform.position = new Vector3(3f, 2f, -5f);
            playerCollider.center = new Vector3(0.2f, 0.8f, -0.1f);
            playerCollider.height = 2f;

            EnemyScript_space enemy = enemyObject.AddComponent<EnemyScript_space>();
            typeof(EnemyScript_space).GetField(
                "playerScript",
                InstanceMemberFlags)?.SetValue(enemy, player);
            MethodInfo getAimPoint = typeof(EnemyScript_space).GetMethod(
                "GetPlayerAimPoint",
                InstanceMemberFlags);

            Assert.That(getAimPoint, Is.Not.Null);
            Vector3 actual = (Vector3)getAimPoint.Invoke(enemy, null);

            Assert.That(Vector3.Distance(actual, playerCollider.bounds.center), Is.LessThan(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void GetPlayerAimPoint_WithoutPlayerFallsBackBehindReleasePoint()
    {
        var enemyObject = new GameObject("Throw Fallback Enemy");
        var releaseObject = new GameObject("Throw Point");
        try
        {
            releaseObject.transform.SetParent(enemyObject.transform);
            releaseObject.transform.position = new Vector3(2f, 3f, 4f);
            releaseObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            EnemyScript_space enemy = enemyObject.AddComponent<EnemyScript_space>();
            typeof(EnemyScript_space).GetField(
                "throwPoint",
                InstanceMemberFlags)?.SetValue(enemy, releaseObject.transform);
            MethodInfo getAimPoint = typeof(EnemyScript_space).GetMethod(
                "GetPlayerAimPoint",
                InstanceMemberFlags);

            Assert.That(getAimPoint, Is.Not.Null);
            Vector3 actual = (Vector3)getAimPoint.Invoke(enemy, null);
            Vector3 expected =
                releaseObject.transform.position - releaseObject.transform.forward;

            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
        }
    }

    [Test]
    public void CalculateThrowDirection_AimsFromReleasePointAtCurrentTarget()
    {
        MethodInfo calculateDirection = typeof(EnemyScript_space).GetMethod(
            "CalculateThrowDirection",
            StaticMethodFlags);
        Assert.That(calculateDirection, Is.Not.Null);

        var releasePosition = new Vector3(2f, 1.5f, 6f);
        var targetPosition = new Vector3(-1f, 0.75f, -4f);
        Vector3 expected = (targetPosition - releasePosition).normalized;

        Vector3 actual = (Vector3)calculateDirection.Invoke(
            null,
            new object[] { releasePosition, targetPosition, Vector3.back });

        Assert.That(Vector3.Angle(actual, expected), Is.LessThan(0.01f));
    }

    [Test]
    public void CalculateThrowDirection_UsesFallbackWhenTargetOverlapsReleasePoint()
    {
        MethodInfo calculateDirection = typeof(EnemyScript_space).GetMethod(
            "CalculateThrowDirection",
            StaticMethodFlags);
        Assert.That(calculateDirection, Is.Not.Null);

        Vector3 actual = (Vector3)calculateDirection.Invoke(
            null,
            new object[] { Vector3.one, Vector3.one, Vector3.left });

        Assert.That(Vector3.Angle(actual, Vector3.left), Is.LessThan(0.01f));
    }

    [Test]
    public void AttackFacing_SnapsVisualToRouteOrthogonalPlayerDirection()
    {
        var playerObject = new GameObject("Facing Target Player");
        var enemyObject = new GameObject("Facing Enemy Root");
        var visualObject = new GameObject("Facing Enemy Visual");
        try
        {
            visualObject.transform.SetParent(enemyObject.transform);
            visualObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Animator animator = visualObject.AddComponent<Animator>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();

            enemyObject.transform.position = new Vector3(1f, 0f, 2f);
            enemyObject.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            playerObject.transform.position = new Vector3(8f, 8f, 4f);
            Quaternion originalRootRotation = enemyObject.transform.rotation;

            EnemyEventController controller =
                enemyObject.AddComponent<EnemyEventController>();
            controller.EventMode = EnemyEventMode.AttackLoop;
            Assert.That(controller.ActivateFromSpot(), Is.True);

            Vector3 expected = EnemyEventController.ResolveOrthogonalFacingDirection(
                player.transform.position - enemyObject.transform.position,
                enemyObject.transform.forward,
                enemyObject.transform.right);
            Assert.That(
                Vector3.Angle(visualObject.transform.forward, expected),
                Is.LessThan(0.01f));
            Assert.That(
                Quaternion.Angle(enemyObject.transform.rotation, originalRootRotation),
                Is.LessThan(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(playerObject);
        }
    }

    [TestCase(0f)]
    [TestCase(90f)]
    [TestCase(180f)]
    [TestCase(270f)]
    public void BuildThrownProjectileRotation_AlignsProjectileLocalXAxisWithVelocity(
        float directionYaw)
    {
        MethodInfo buildRotation = typeof(EnemyScript_space).GetMethod(
            "BuildThrownProjectileRotation",
            StaticMethodFlags);
        Assert.That(buildRotation, Is.Not.Null);

        Vector3 direction =
            Quaternion.Euler(0f, directionYaw, 0f) * Vector3.forward;
        Quaternion rotation = (Quaternion)buildRotation.Invoke(
            null,
            new object[] { direction });

        Assert.That(
            Vector3.Angle(rotation * Vector3.right, direction),
            Is.LessThan(0.01f));
    }
}
#endif
