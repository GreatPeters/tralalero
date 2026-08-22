#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class NoryangjinRuntimeCleanupContractTests
{
    private const BindingFlags InstanceMembers =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly string[] ForwardEnemyPrefabPaths =
        ForwardEnemyArchetypeCatalog.Definitions
            .Select(definition => definition.PrefabPath)
            .ToArray();

    private static readonly string[] PlayerPrefabPaths =
    {
        "Assets/ShooterSurvival/Prefabs/Entities/Pirate/Player.prefab",
        "Assets/ShooterSurvival/Prefabs/Entities/Space/Player_fwdMode.prefab"
    };

    private static readonly string[] ExtraHelpPrefabPaths =
    {
        "Assets/ShooterSurvival/Prefabs/Entities/Space/BomBarDino.prefab",
        "Assets/ShooterSurvival/Prefabs/Entities/Space/Player_EH.prefab",
        "Assets/ShooterSurvival/Prefabs/Entities/Space/TungTungTung.prefab"
    };

    private static readonly string[] ProjectilePrefabPaths =
        ForwardEnemyArchetypeCatalog.Definitions
            .Where(definition =>
                definition.Identity == "Enemy_FatMan" ||
                definition.Identity == "Enemy_Guard")
            .Select(definition => definition.PrefabPath)
            .Concat(new[]
            {
                "Assets/ShooterSurvival/Prefabs/Obstacle_Real/Boat.prefab",
                "Assets/ShooterSurvival/Prefabs/Obstacle_Real/Ship.prefab"
            })
            .ToArray();

    private static readonly string[] EnemyDataPaths =
    {
        "Assets/ShooterSurvival/Prefabs/Entities/SO_RusherEnemy.asset",
        "Assets/ShooterSurvival/Prefabs/Entities/SO_TankEnemy.asset",
        "Assets/ShooterSurvival/Prefabs/Entities/SO_WalkerEnemy.asset"
    };

    private static readonly string[] RemovedWallFieldNames =
    {
        "_initialized",
        "attSpr",
        "attPercentSpr",
        "missileAddSpr",
        "attackSpeedSpr",
        "missileDistanceSpr",
        "hpSpr",
        "hpPercentSpr",
        "tungtungRareSpr",
        "boombarRareSpr",
        "attUniqueSpr",
        "attPerUniqueSpr",
        "missileAddUniqueSpr",
        "attackSpeedUniqueSpr",
        "distanceUniqueSpr",
        "hpUniqueSpr",
        "hpPerUniqueSpr",
        "missileSpeedSpr",
        "missileSpeedUniqueSpr",
        "att",
        "attPercent",
        "missileAdd",
        "attackSpeed",
        "missileDistance",
        "missileSpeed",
        "hp",
        "hpPercent",
        "tungtungAdd",
        "boombarAdd"
    };

    [Test]
    public void PlayerScript_KeepsRuntimeStateOutOfPrefabSerialization()
    {
        AssertFieldsAreMissing<PlayerScript>(
            "currentWeapon",
            "currentFireRate",
            "enemyDetection");

        AssertFieldsAreNotSerialized<PlayerScript>(
            "playerScore",
            "currentHealth",
            "originalDamage",
            "currentDamage",
            "moveSensitivity",
            "nearestEnemy",
            "extraHelpWeaponScript",
            "extraHelpCount",
            "lastWallTouchTime",
            "canShoot",
            "sharkAnim",
            "originalMoveSpeed");
    }

    [Test]
    public void WallScript_KeepsGeneratedStatsPrivateAndRemovesUnusedInspectorData()
    {
        AssertFieldsAreMissing<WallScript>(
            "_initialized",
            "attSpr",
            "attPercentSpr",
            "missileAddSpr",
            "attackSpeedSpr",
            "missileDistanceSpr",
            "hpSpr",
            "hpPercentSpr",
            "tungtungRareSpr",
            "boombarRareSpr",
            "attUniqueSpr",
            "attPerUniqueSpr",
            "missileAddUniqueSpr",
            "attackSpeedUniqueSpr",
            "distanceUniqueSpr",
            "hpUniqueSpr",
            "hpPerUniqueSpr",
            "att",
            "attPercent",
            "missileAdd",
            "attackSpeed",
            "missileDistance",
            "hp",
            "hpPercent",
            "tungtungAdd",
            "boombarAdd",
            "isPercent");

        AssertFieldsArePrivateAndNotUnitySerialized<WallScript>(
            "bonusValue",
            "displayBonusValue",
            "bonusValueType",
            "selectedDisplayRow");

        AssertMethodIsMissing<WallScript>("FixedUpdate");
    }

    [Test]
    public void ExtraHelpBuffScript_StoresOnlyAuthoredConfiguration()
    {
        AssertFieldsAreMissing<ExtraHelpBuffScript>(
            "weaponPos",
            "healthBar");
        AssertFieldsAreNotSerialized<ExtraHelpBuffScript>(
            "currentHealth",
            "spawnIndex",
            "helpType");
        AssertFieldsArePrivateAndNotUnitySerialized<ExtraHelpBuffScript>(
            "healthText");
        AssertMethodIsMissing<ExtraHelpBuffScript>("FixedUpdate");
    }

    [Test]
    public void SimpleProjectile_ExposesOnlyItsRuntimeAssignmentContract()
    {
        AssertFieldsAreMissing<SimpleProjectile>("helperTag", "targetTag");
        AssertFieldsAreNotSerialized<SimpleProjectile>("damage");
        AssertFieldsArePrivateAndNotUnitySerialized<SimpleProjectile>("isAttacked");
        AssertMethodIsMissing<SimpleProjectile>("OnCollisionEnter");
    }

    [Test]
    public void EnemySO_DoesNotRetainUnusedDeathVfxReference()
    {
        AssertFieldsAreMissing<EnemySO>("enemyDeathVFX");
    }

    [Test]
    public void ForwardEnemy_DoesNotDependOnGlobalPostProcessing()
    {
        AssertFieldsAreMissing<EnemyScript_space>("effectOverlayVignette");
    }

    [Test]
    public void RuntimeBonusWall_DoesNotResolveGlobalPostProcessing()
    {
        GameObject rootObject = new GameObject("Runtime Bonus Wall Root Test");
        GameObject wallObject = new GameObject("Wall Logic");
        try
        {
            wallObject.transform.SetParent(rootObject.transform, false);
            WallScript wall = wallObject.AddComponent<WallScript>();
            RuntimeBonusWall marker = rootObject.AddComponent<RuntimeBonusWall>();
            MethodInfo resolveEffectOverlay = typeof(WallScript).GetMethod(
                "ResolveEffectOverlay",
                InstanceMembers);

            Assert.That(marker.RemoveWhenPreparingStage, Is.True);
            Assert.That(resolveEffectOverlay, Is.Not.Null);
            Assert.That(resolveEffectOverlay.Invoke(wall, null), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rootObject);
        }
    }

    [Test]
    public void AuthoredBonusWall_UsesConfiguredGradeForDataDrivenRoll()
    {
        GameObject wallObject = new GameObject("Authored Bonus Wall Test");
        try
        {
            WallScript wall = wallObject.AddComponent<WallScript>();
            AuthoredBonusWall authoredBonus = wallObject.AddComponent<AuthoredBonusWall>();
            authoredBonus.Configure(Rarity.Unique);

            wall.SetRandomStat();

            Assert.That(wall.wallType, Is.EqualTo(WallType.BuffWall));
            Assert.That(wall.rarity, Is.EqualTo(Rarity.Unique));
            Assert.That(wall.isRandom, Is.True);
            Assert.That(authoredBonus.RolledStat, Is.Not.Empty);
            Assert.That(wall.CurrentBonusAlias, Is.Not.Empty);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(wallObject);
        }
    }

    [Test]
    public void MigratedAssets_DoNotRetainRemovedSerializedKeysInYaml()
    {
        AssertComponentYamlOmits<PlayerScript>(
            PlayerPrefabPaths,
            "playerScore",
            "currentHealth",
            "originalDamage",
            "currentDamage",
            "currentFireRate",
            "moveSensitivity",
            "enemyDetection",
            "nearestEnemy",
            "extraHelpWeaponScript",
            "extraHelpCount",
            "lastWallTouchTime",
            "canShoot",
            "sharkAnim",
            "originalMoveSpeed");

        string[] wallPrefabPaths = AssetDatabase
            .FindAssets("t:Prefab", new[] { "Assets/ShooterSurvival/Prefabs/Walls" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();
        AssertComponentYamlOmits<WallScript>(wallPrefabPaths, RemovedWallFieldNames);

        AssertComponentYamlOmits<ExtraHelpBuffScript>(
            ExtraHelpPrefabPaths,
            "currentHealth",
            "weaponPos",
            "healthBar",
            "spawnIndex",
            "helpType");
        AssertComponentYamlOmits<SimpleProjectile>(
            ProjectilePrefabPaths,
            "isAttacked",
            "damage",
            "targetTag",
            "helperTag");
        AssertComponentYamlOmits<EnemySO>(EnemyDataPaths, "enemyDeathVFX");
    }

    [TestCaseSource(nameof(ForwardEnemyPrefabPaths))]
    public void ForwardEnemyPrefab_UsesLeanHealthDisplayAndNamedHitAnchor(
        string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.That(prefab, Is.Not.Null, $"Missing Forward enemy prefab at {prefabPath}.");

        Canvas[] canvases = prefab.GetComponentsInChildren<Canvas>(true);
        TextMeshProUGUI[] healthLabels =
            prefab.GetComponentsInChildren<TextMeshProUGUI>(true);

        Assert.That(canvases, Is.Not.Empty, $"{prefabPath} lost its health Canvas.");
        Assert.That(healthLabels, Is.Not.Empty, $"{prefabPath} lost its health TMP label.");
        Assert.That(
            prefab.GetComponentsInChildren<CanvasScaler>(true),
            Is.Empty,
            $"{prefabPath} still has an unnecessary CanvasScaler.");
        Assert.That(
            prefab.GetComponentsInChildren<GraphicRaycaster>(true),
            Is.Empty,
            $"{prefabPath} still has an unnecessary GraphicRaycaster.");
        Assert.That(
            healthLabels.All(label => !label.raycastTarget),
            Is.True,
            $"{prefabPath} health TMP labels must not receive raycasts.");

        Transform hitAnchor = prefab.transform.Find("Walker-HitPos");
        Assert.That(
            hitAnchor,
            Is.Not.Null,
            $"{prefabPath} needs a root-level Walker-HitPos child.");
    }

    private static void AssertFieldsAreMissing<T>(params string[] fieldNames)
    {
        foreach (string fieldName in fieldNames)
        {
            Assert.That(
                typeof(T).GetField(fieldName, InstanceMembers),
                Is.Null,
                $"{typeof(T).Name}.{fieldName} should have been removed.");
        }
    }

    private static void AssertFieldsAreNotSerialized<T>(params string[] fieldNames)
    {
        foreach (string fieldName in fieldNames)
        {
            FieldInfo field = GetRequiredField<T>(fieldName);
            Assert.That(
                (field.Attributes & FieldAttributes.NotSerialized) != 0,
                Is.True,
                $"{typeof(T).Name}.{fieldName} must remain runtime-only.");
        }
    }

    private static void AssertFieldsArePrivateAndNotUnitySerialized<T>(
        params string[] fieldNames)
    {
        foreach (string fieldName in fieldNames)
        {
            FieldInfo field = GetRequiredField<T>(fieldName);
            Assert.That(field.IsPrivate, Is.True, $"{typeof(T).Name}.{fieldName} must be private.");
            Assert.That(
                field.GetCustomAttribute<SerializeField>(),
                Is.Null,
                $"{typeof(T).Name}.{fieldName} must not be Unity-serialized.");
        }
    }

    private static FieldInfo GetRequiredField<T>(string fieldName)
    {
        FieldInfo field = typeof(T).GetField(fieldName, InstanceMembers);
        Assert.That(field, Is.Not.Null, $"Missing expected {typeof(T).Name}.{fieldName} field.");
        return field;
    }

    private static void AssertMethodIsMissing<T>(string methodName)
    {
        Assert.That(
            typeof(T).GetMethod(methodName, InstanceMembers),
            Is.Null,
            $"{typeof(T).Name}.{methodName} should have been removed.");
    }

    private static void AssertComponentYamlOmits<T>(
        IEnumerable<string> assetPaths,
        params string[] forbiddenKeys)
    {
        string scriptGuid = FindScriptGuid<T>();
        string scriptMarker = $"guid: {scriptGuid}";

        foreach (string assetPath in assetPaths)
        {
            string absoluteAssetPath = Path.GetFullPath(assetPath);
            Assert.That(
                File.Exists(absoluteAssetPath),
                Is.True,
                $"Missing asset at {assetPath}.");
            string yaml = File.ReadAllText(absoluteAssetPath);
            int searchIndex = 0;
            bool foundComponent = false;

            while ((searchIndex = yaml.IndexOf(scriptMarker, searchIndex, StringComparison.Ordinal)) >= 0)
            {
                int blockStart = yaml.LastIndexOf("--- !u!114", searchIndex, StringComparison.Ordinal);
                int blockEnd = yaml.IndexOf("\n--- !u!", searchIndex, StringComparison.Ordinal);
                if (blockStart < 0)
                    break;

                foundComponent = true;
                if (blockEnd < 0)
                    blockEnd = yaml.Length;

                string componentYaml = yaml.Substring(blockStart, blockEnd - blockStart);
                foreach (string forbiddenKey in forbiddenKeys)
                {
                    Assert.That(
                        Regex.IsMatch(
                            componentYaml,
                            $@"(?m)^\s*{Regex.Escape(forbiddenKey)}\s*:",
                            RegexOptions.CultureInvariant),
                        Is.False,
                        $"{assetPath} still serializes removed {typeof(T).Name}.{forbiddenKey}.");
                }

                searchIndex = blockEnd;
            }

            Assert.That(
                foundComponent,
                Is.True,
                $"{assetPath} does not contain a serialized {typeof(T).Name} component.");
        }
    }

    private static string FindScriptGuid<T>()
    {
        foreach (string guid in AssetDatabase.FindAssets($"{typeof(T).Name} t:MonoScript"))
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script != null && script.GetClass() == typeof(T))
                return guid;
        }

        Assert.Fail($"Could not find the MonoScript GUID for {typeof(T).FullName}.");
        return string.Empty;
    }
}
#endif
