#if UNITY_EDITOR
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CombatFeedbackRevisionTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private PlayerScript player;
    [SetUp] public void Setup()
    {
        root = new GameObject("Feedback regression player");
        player = root.AddComponent<PlayerScript>();
        typeof(PlayerScript).GetField("useExcelCharacterDefaults",Private).SetValue(player,false);
        player.originalHealth = 100;
        typeof(PlayerScript).GetMethod("Awake",Private).Invoke(player,null);
    }
    [TearDown] public void Cleanup()
    {
        Object.DestroyImmediate(root);
        BulletScript.ResetStatBonus();
        BulletScript.ApplyMissileDurationUpgrade(0,0);
        TimeManager.isGameRunning = false;
    }

    [Test] public void RetryClearsWallHealthAndMissileBonusesButRetainsPermanentDuration()
    {
        BulletScript.ApplyMissileDurationUpgrade(.5f,20);
        float baseline = BulletScript.CurrentMissileDuration;
        player.ApplyRunHealthBonus(50,false);
        BulletScript.AddMissileDurationPercent(80);
        Assert.That(player.MaxHealth,Is.EqualTo(150));
        player.ResetState();
        Assert.That(player.MaxHealth,Is.EqualTo(100));
        Assert.That(player.currentHealth,Is.EqualTo(100));
        Assert.That(BulletScript.CurrentMissileDuration,Is.EqualTo(baseline).Within(.001f));
        player.ResetState();
        Assert.That(BulletScript.CurrentMissileDuration,Is.EqualTo(baseline).Within(.001f));
    }

    [Test] public void NewScenePlayerClearsStaticWallDuration()
    {
        BulletScript.AddMissileDurationPercent(100);
        typeof(PlayerScript).GetMethod("Awake",Private).Invoke(player,null);
        Assert.That(BulletScript.CurrentMissileDuration,Is.EqualTo(player.DefaultMissileDuration).Within(.001f));
    }

    [Test] public void HoleKillsEvenWithHighHealthAndDoesNotRotateRoute()
    {
        root.transform.rotation = Quaternion.Euler(0,93,0);
        player.ApplyRunHealthBonus(5000,false);
        player.DieFromHazard(true);
        Assert.That(player.currentHealth,Is.Zero);
        Assert.That(player.movement,Is.False);
        Assert.That(player.canShoot,Is.False);
        Assert.That(Quaternion.Angle(root.transform.rotation,Quaternion.Euler(0,93,0)),Is.LessThan(.01));
    }

    [Test] public void FallenPoleCooldownIsSharedAcrossSourcesAndResetsForRetry()
    {
        Assert.That(player.TryTakeFallenPoleDamage(10),Is.True);
        Assert.That(player.TryTakeFallenPoleDamage(10.999f),Is.False);
        Assert.That(player.TryTakeFallenPoleDamage(11),Is.True);
        Assert.That(player.currentHealth,Is.EqualTo(40));
        player.ResetState();
        Assert.That(player.TryTakeFallenPoleDamage(10),Is.True);
        Assert.That(player.currentHealth,Is.EqualTo(70));
    }

    [Test] public void MeleeWaitingClipsDoNotSwingAndCarryMaskLeavesTorsoToLocomotion()
    {
        const string shared = "Assets/JH/Model/Animatior/ForwardEnemyShared/";
        foreach(string actor in new[]{"Enemy_Woman","Enemy_YllowMan_Sword"})
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(shared + "Overrides/" + actor + ".overrideController");
            Assert.That(controller["ForwardEnemy_Idle"],Is.Not.EqualTo(controller["ForwardEnemy_AttackLoop"]));
        }
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(shared+"ForwardEnemy_Carry.mask");
        Assert.That(mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body),Is.False);
        Assert.That(mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm),Is.True);
    }
}
#endif
