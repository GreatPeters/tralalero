#if UNITY_EDITOR
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class MissileDurationTests
{
    private const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [TearDown]
    public void TearDown()
    {
        MethodInfo resetStatics = typeof(BulletScript).GetMethod("ResetStatics", StaticFlags);
        resetStatics?.Invoke(null, null);
        TimeManager.Instance = null;
        TimeManager.timeFactor = 1f;
    }

    [Test]
    public void FixedUpdate_MovesAtConfiguredAbsoluteSpeed()
    {
        var timeManagerObject = new GameObject("Missile Speed Time Manager");
        var projectileObject = new GameObject("Absolute Speed Missile");
        try
        {
            TimeManager timeManager = timeManagerObject.AddComponent<TimeManager>();
            timeManager.isForwardMarchScene = true;
            TimeManager.timeFactor = 1f;
            BulletScript.ConfigureMissileDefaults(16f, 10f);
            BulletScript projectile = projectileObject.AddComponent<BulletScript>();
            projectile.SetDirection(Vector3.right);
            MethodInfo fixedUpdate = typeof(BulletScript).GetMethod("FixedUpdate", InstanceFlags);
            Assert.That(fixedUpdate, Is.Not.Null);

            fixedUpdate.Invoke(projectile, null);

            Assert.That(
                projectileObject.transform.position.x,
                Is.EqualTo(16f * Time.fixedDeltaTime).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(projectileObject);
            Object.DestroyImmediate(timeManagerObject);
        }
    }

    [Test]
    public void DurationUpgrade_IncreasesLifetimeWithoutChangingAbsoluteSpeed()
    {
        BulletScript.ConfigureMissileDefaults(16f, 1f);
        BulletScript.ApplyMissileDurationUpgrade(0f, 50f);

        Assert.That(BulletScript.BaseMissileSpeed, Is.EqualTo(16f));
        Assert.That(BulletScript.CurrentMissileDuration, Is.EqualTo(1.5f));
    }

    [Test]
    public void ResetStatBonus_RemovesRunBonusButPreservesPermanentDurationUpgrade()
    {
        BulletScript.ConfigureMissileDefaults(16f, 1f);
        BulletScript.ApplyMissileDurationUpgrade(0f, 50f);
        BulletScript.AddMissileDurationPercent(25f);
        Assert.That(BulletScript.CurrentMissileDuration, Is.EqualTo(1.75f));

        BulletScript.ResetStatBonus();

        Assert.That(BulletScript.CurrentMissileDuration, Is.EqualTo(1.5f));
    }

    [Test]
    public void SetDirection_ResetsElapsedDurationForPooledReuse()
    {
        var projectileObject = new GameObject("Pooled Missile Duration Test");
        try
        {
            BulletScript projectile = projectileObject.AddComponent<BulletScript>();
            FieldInfo elapsedDuration = typeof(BulletScript).GetField(
                "elapsedDuration",
                InstanceFlags);
            Assert.That(elapsedDuration, Is.Not.Null);
            elapsedDuration.SetValue(projectile, 0.75f);

            projectile.SetDirection(Vector3.forward);

            Assert.That((float)elapsedDuration.GetValue(projectile), Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(projectileObject);
        }
    }

    [Test]
    public void UpgradeUi_DescribesLegacyProjectileUpgradeAsMissileDuration()
    {
        var row = new UpgradeRow
        {
            type = UpgradeStatManager.UpgradeType.PROJECTILE_SPEED,
            item = "투사체 속도",
            note = "투사체 속도 증가"
        };
        MethodInfo getDisplayName = typeof(UpgradeUI).GetMethod(
            "GetDisplayName",
            StaticFlags);
        MethodInfo getDescription = typeof(UpgradeUI).GetMethod(
            "GetDescription",
            StaticFlags);
        Assert.That(getDisplayName, Is.Not.Null);
        Assert.That(getDescription, Is.Not.Null);

        Assert.That(
            getDisplayName.Invoke(null, new object[] { row }),
            Is.EqualTo("미사일 지속 시간"));
        Assert.That(
            getDescription.Invoke(null, new object[] { row }),
            Is.EqualTo("미사일 지속 시간이 증가합니다!"));
    }
}
#endif
