#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayerCharacterDefaultsTests
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [TearDown]
    public void TearDown()
    {
        TimeManager.timeFactor = 1f;
        CanvasScript.isGameOver = false;
        MethodInfo resetBulletStatics = typeof(BulletScript).GetMethod(
            "ResetStatics",
            StaticFlags);
        resetBulletStatics?.Invoke(null, null);
    }

    [Test]
    public void Awake_ExcelMode_UsesAbsoluteMissileSpeedAndDuration()
    {
        var gameObject = new GameObject("Excel Character Defaults Test");
        try
        {
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            SetField(player, "useExcelCharacterDefaults", true);
            player.originalHealth = 999f;
            SetField(player, "defaultAttackDamage", 999f);
            SetField(player, "defaultForwardMoveSpeed", 9f);
            SetField(player, "defaultFireRate", 1f);
            SetField(player, "defaultProjectileCount", 1);
            SetField(player, "defaultMissileSpeed", 3f);
            SetField(player, "defaultMissileDuration", 4f);

            Assert.That(
                EnvironmentVariableTables.TryGetFloat3("playerSpeed", out var playerSpeed),
                Is.True);
            Assert.That(
                EnvironmentVariableTables.TryGetFloat("misspleSpeed", out var missileSpeed),
                Is.True);
            Assert.That(
                EnvironmentVariableTables.TryGetFloat("missileDuration", out var missileDuration),
                Is.True);
            Assert.That(playerSpeed.value1, Is.EqualTo(8f));
            Assert.That(missileSpeed, Is.EqualTo(16f));
            Assert.That(missileDuration, Is.EqualTo(1f));
            InvokeAwake(player);

            Assert.That(player.originalHealth, Is.EqualTo(100f));
            Assert.That(player.originalDamage, Is.EqualTo(50f));
            Assert.That(player.ForwardMoveSpeed, Is.EqualTo(8f));
            Assert.That(GetProperty<float>(player, "DefaultFireRate"), Is.EqualTo(1f));
            Assert.That(GetProperty<int>(player, "DefaultProjectileCount"), Is.EqualTo(1));
            Assert.That(GetProperty<float>(player, "DefaultMissileSpeed"), Is.EqualTo(16f));
            Assert.That(GetProperty<float>(player, "DefaultMissileDuration"), Is.EqualTo(1f));
            Assert.That(GetStaticProperty<float>(typeof(BulletScript), "BaseMissileSpeed"), Is.EqualTo(16f));
            Assert.That(GetStaticProperty<float>(typeof(BulletScript), "BaseMissileDuration"), Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void Awake_InspectorMode_UsesOneCentralSetOfCharacterDefaults()
    {
        var gameObject = new GameObject("Inspector Character Defaults Test");
        try
        {
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            SetField(player, "useExcelCharacterDefaults", false);
            player.originalHealth = 321f;
            SetField(player, "defaultAttackDamage", 17f);
            SetField(player, "defaultForwardMoveSpeed", 7f);
            SetField(player, "defaultFireRate", 2.5f);
            SetField(player, "defaultProjectileCount", 3);
            SetField(player, "defaultMissileSpeed", 14f);
            SetField(player, "defaultMissileDuration", 2.5f);

            InvokeAwake(player);

            Assert.That(player.originalHealth, Is.EqualTo(321f));
            Assert.That(player.originalDamage, Is.EqualTo(17f));
            Assert.That(player.ForwardMoveSpeed, Is.EqualTo(7f));
            Assert.That(GetProperty<float>(player, "DefaultFireRate"), Is.EqualTo(2.5f));
            Assert.That(GetProperty<int>(player, "DefaultProjectileCount"), Is.EqualTo(3));
            Assert.That(GetProperty<float>(player, "DefaultMissileSpeed"), Is.EqualTo(14f));
            Assert.That(GetProperty<float>(player, "DefaultMissileDuration"), Is.EqualTo(2.5f));
            Assert.That(GetStaticProperty<float>(typeof(BulletScript), "BaseMissileSpeed"), Is.EqualTo(14f));
            Assert.That(GetStaticProperty<float>(typeof(BulletScript), "BaseMissileDuration"), Is.EqualTo(2.5f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void ReloadCharacterDefaults_LeavesMissileSpeedIndependentFromPlayerSpeed()
    {
        var gameObject = new GameObject("Reload Character Defaults Test");
        try
        {
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            SetField(player, "useExcelCharacterDefaults", false);
            SetField(player, "defaultForwardMoveSpeed", 7f);
            SetField(player, "defaultMissileSpeed", 16f);
            SetField(player, "defaultMissileDuration", 1f);
            InvokeAwake(player);
            Assert.That(player.ForwardMoveSpeed, Is.EqualTo(7f));
            Assert.That(GetProperty<float>(player, "DefaultMissileSpeed"), Is.EqualTo(16f));

            SetField(player, "defaultForwardMoveSpeed", 9f);
            MethodInfo reload = typeof(PlayerScript).GetMethod(
                "ReloadCharacterDefaults",
                InstanceFlags);

            Assert.That(
                reload,
                Is.Not.Null,
                "PlayerScript must expose a narrow refresh entry point for editor data reloads.");
            reload.Invoke(player, null);

            Assert.That(player.ForwardMoveSpeed, Is.EqualTo(9f));
            Assert.That(GetProperty<float>(player, "DefaultMissileSpeed"), Is.EqualTo(16f));
            Assert.That(GetProperty<float>(player, "DefaultMissileDuration"), Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void Awake_ExcelMode_PrefersCorrectedMissileSpeedKeyOverLegacyTypo()
    {
        FieldInfo mapField = typeof(EnvironmentVariableTables).GetField(
            "_float3Map",
            StaticFlags);
        Assert.That(mapField, Is.Not.Null);
        object previousMap = mapField.GetValue(null);
        var gameObject = new GameObject("Corrected Missile Speed Key Test");

        try
        {
            mapField.SetValue(
                null,
                new Dictionary<string, EnvironmentVariableTables.Float3>(
                    System.StringComparer.OrdinalIgnoreCase)
                {
                    ["missileSpeed"] = new EnvironmentVariableTables.Float3 { value1 = 20f },
                    ["misspleSpeed"] = new EnvironmentVariableTables.Float3 { value1 = 16f },
                    ["missileDuration"] = new EnvironmentVariableTables.Float3 { value1 = 1f }
                });
            PlayerScript player = gameObject.AddComponent<PlayerScript>();
            SetField(player, "useExcelCharacterDefaults", true);

            InvokeAwake(player);

            Assert.That(GetProperty<float>(player, "DefaultMissileSpeed"), Is.EqualTo(20f));
            Assert.That(GetProperty<float>(player, "DefaultMissileDuration"), Is.EqualTo(1f));
        }
        finally
        {
            mapField.SetValue(null, previousMap);
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void PlayerWeapon_UsesCentralAttackFireRateAndProjectileCount()
    {
        var playerObject = new GameObject("Central Weapon Defaults Player");
        WeaponSO weaponSo = ScriptableObject.CreateInstance<WeaponSO>();
        try
        {
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            SetField(player, "useExcelCharacterDefaults", false);
            SetField(player, "defaultAttackDamage", 17f);
            SetField(player, "defaultFireRate", 2.5f);
            SetField(player, "defaultProjectileCount", 3);
            InvokeAwake(player);

            var weaponObject = new GameObject("Central Weapon Defaults Weapon");
            weaponObject.transform.SetParent(playerObject.transform);
            WeaponScript weapon = weaponObject.AddComponent<WeaponScript>();
            weaponSo.weaponDamage = 999f;
            weaponSo.weaponFireRate = 999f;
            SetField(weapon, "playerScript", player);
            SetField(weapon, "weaponSO", weaponSo);

            Assert.That(Invoke<float>(weapon, "GetBaseDamage"), Is.EqualTo(17f));
            Assert.That(Invoke<float>(weapon, "GetBaseFireRate"), Is.EqualTo(2.5f));
            Assert.That(Invoke<int>(weapon, "GetBaseBulletCount"), Is.EqualTo(3));
        }
        finally
        {
            Object.DestroyImmediate(weaponSo);
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void NonPlayerWeapon_KeepsWeaponSoDefaults()
    {
        var weaponObject = new GameObject("Non Player Weapon Defaults");
        WeaponSO weaponSo = ScriptableObject.CreateInstance<WeaponSO>();
        try
        {
            WeaponScript weapon = weaponObject.AddComponent<WeaponScript>();
            weaponSo.weaponDamage = 8f;
            weaponSo.weaponFireRate = 0.75f;
            SetField(weapon, "weaponSO", weaponSo);

            Assert.That(Invoke<float>(weapon, "GetBaseDamage"), Is.EqualTo(8f));
            Assert.That(Invoke<float>(weapon, "GetBaseFireRate"), Is.EqualTo(0.75f));
            Assert.That(Invoke<int>(weapon, "GetBaseBulletCount"), Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(weaponSo);
            Object.DestroyImmediate(weaponObject);
        }
    }

    [Test]
    public void ExtraHelpWeaponUnderPlayer_KeepsWeaponSoDefaults()
    {
        var playerObject = new GameObject("Extra Help Parent Player");
        WeaponSO weaponSo = ScriptableObject.CreateInstance<WeaponSO>();
        try
        {
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            SetField(player, "useExcelCharacterDefaults", false);
            SetField(player, "defaultAttackDamage", 17f);
            SetField(player, "defaultFireRate", 2.5f);
            SetField(player, "defaultProjectileCount", 3);
            InvokeAwake(player);

            var weaponObject = new GameObject("Nested Extra Help Weapon");
            weaponObject.transform.SetParent(playerObject.transform);
            ExtraHelpBuffScript extraHelp = weaponObject.AddComponent<ExtraHelpBuffScript>();
            WeaponScript weapon = weaponObject.AddComponent<WeaponScript>();
            weaponSo.weaponDamage = 8f;
            weaponSo.weaponFireRate = 0.75f;
            SetField(weapon, "playerScript", player);
            SetField(weapon, "extraHelpBuffScript", extraHelp);
            SetField(weapon, "weaponSO", weaponSo);

            Assert.That(Invoke<float>(weapon, "GetBaseDamage"), Is.EqualTo(8f));
            Assert.That(Invoke<float>(weapon, "GetBaseFireRate"), Is.EqualTo(0.75f));
            Assert.That(Invoke<int>(weapon, "GetBaseBulletCount"), Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(weaponSo);
            Object.DestroyImmediate(playerObject);
        }
    }

    private static void InvokeAwake(PlayerScript player)
    {
        MethodInfo awake = typeof(PlayerScript).GetMethod("Awake", InstanceFlags);
        Assert.That(awake, Is.Not.Null);
        awake.Invoke(player, null);
    }

    private static void SetField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, InstanceFlags);
        Assert.That(
            field,
            Is.Not.Null,
            $"Missing {target.GetType().Name} field '{fieldName}'.");
        field.SetValue(target, value);
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, InstanceFlags);
        Assert.That(property, Is.Not.Null, $"Missing property '{propertyName}'.");
        return (T)property.GetValue(target);
    }

    private static T GetStaticProperty<T>(System.Type type, string propertyName)
    {
        PropertyInfo property = type.GetProperty(propertyName, StaticFlags);
        Assert.That(property, Is.Not.Null, $"Missing static property '{propertyName}'.");
        return (T)property.GetValue(null);
    }

    private static T Invoke<T>(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, InstanceFlags);
        Assert.That(method, Is.Not.Null, $"Missing method '{methodName}'.");
        return (T)method.Invoke(target, null);
    }
}
#endif
