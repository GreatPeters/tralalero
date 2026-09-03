#if UNITY_EDITOR
using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class YellowManVariantSetup
{
    public const string MenuPath =
        "Tools/Shooter Survival/Forward Enemy/Build Yellow Man Variants";

    internal const string LegacyPrefabPath =
        "Assets/JH/Model/Prefab/Enemy_YllowMan.prefab";
    internal const string NetPrefabPath =
        "Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab";
    internal const string SwordPrefabPath =
        "Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab";
    internal const string WomanPrefabPath =
        "Assets/JH/Model/Prefab/Enemy_Woman.prefab";
    internal const string LegacyOverridePath =
        "Assets/JH/Model/Animatior/ForwardEnemyShared/Overrides/Enemy_YllowMan.overrideController";
    internal const string NetOverridePath =
        "Assets/JH/Model/Animatior/ForwardEnemyShared/Overrides/Enemy_YllowMan_Net.overrideController";
    internal const string PaletteDefaultsPath =
        "Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset";

    internal const string NetPropName = "Waffle_Grid_0811164826_texture";
    internal const string SwordPropName = "Chef_s_Steel_0811163218_texture";

    [MenuItem(MenuPath, false, 2321)]
    public static void Configure()
    {
        ValidateMoveState(LegacyPrefabPath, NetPrefabPath);
        ValidateMoveState(LegacyOverridePath, NetOverridePath);
        MoveAssetIfNeeded(LegacyPrefabPath, NetPrefabPath);
        MoveAssetIfNeeded(LegacyOverridePath, NetOverridePath);
        MigratePaletteDefaults();
        ConfigureNetPrefab();
        ConfigureSwordPrefab();
        ForwardEnemyAnimatorSetup.Configure();
        ForwardEnemyMovementSetup.Configure();
        RenameOpenSceneNetInstances();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        NoryangjinMapToolWindow.RefreshOpenWindowPaletteAssets();

        Debug.Log(
            "[Yellow Man Variants] Built 옐로우맨_그물 and 옐로우맨_칼. " +
            "The sword variant reuses the Woman boss sword/slash and defaults to run at speed 4.");
    }

    private static void ValidateMoveState(
        string sourcePath,
        string destinationPath)
    {
        bool sourceExists = AssetDatabase.LoadMainAssetAtPath(sourcePath) != null;
        bool destinationExists =
            AssetDatabase.LoadMainAssetAtPath(destinationPath) != null;
        if (sourceExists == destinationExists)
        {
            string state = sourceExists
                ? "both source and destination exist"
                : "neither source nor destination exists";
            throw new InvalidOperationException(
                $"Invalid Yellow Man migration state ({state}): " +
                $"{sourcePath} -> {destinationPath}");
        }
    }

    private static void MoveAssetIfNeeded(string sourcePath, string destinationPath)
    {
        UnityEngine.Object destination = AssetDatabase.LoadMainAssetAtPath(destinationPath);
        UnityEngine.Object source = AssetDatabase.LoadMainAssetAtPath(sourcePath);
        if (destination != null && source == null)
            return;
        if (destination != null)
        {
            throw new InvalidOperationException(
                $"Both legacy and migrated Yellow Man assets exist: " +
                $"{sourcePath} and {destinationPath}");
        }
        if (source == null)
            throw new InvalidOperationException(
                $"Missing source asset for Yellow Man migration: {sourcePath}");

        string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);
    }

    private static void ConfigureNetPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(NetPrefabPath);
        try
        {
            root.name = "Enemy_YllowMan_Net";
            RemoveChildrenNamed(root.transform, SwordPropName);

            if (FindDescendant(root.transform, NetPropName) == null)
            {
                throw new InvalidOperationException(
                    "The Yellow Man net prefab lost its held net prop.");
            }

            EnemyEventController controller =
                root.GetComponent<EnemyEventController>() ??
                root.AddComponent<EnemyEventController>();
            controller.MoveAnimation = EnemyMoveAnimation.Walk;

            SavePrefab(root, NetPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void MigratePaletteDefaults()
    {
        NoryangjinMapToolPaletteDefaults defaults =
            AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(
                PaletteDefaultsPath);
        if (defaults == null)
            throw new InvalidOperationException("Missing map-tool palette defaults asset.");

        var serialized = new SerializedObject(defaults);
        SerializedProperty entries = serialized.FindProperty("entries");
        for (int index = 0; index < entries.arraySize; index++)
        {
            SerializedProperty path = entries
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("prefabPath");
            if (path.stringValue == LegacyPrefabPath)
                path.stringValue = NetPrefabPath;
        }
        SerializedProperty labels = serialized.FindProperty("labelEntries");
        for (int index = 0; index < labels.arraySize; index++)
        {
            SerializedProperty path = labels
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("prefabPath");
            if (path.stringValue == LegacyPrefabPath)
                path.stringValue = NetPrefabPath;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        NoryangjinMapToolPalettePlacementEntry net =
            defaults.GetOrCreateEntry(NetPrefabPath);
        NoryangjinMapToolPalettePlacementEntry sword =
            defaults.GetOrCreateEntry(SwordPrefabPath);
        sword.scale = net.scale;
        sword.positionOffset = net.positionOffset;
        sword.yawOffset = net.yawOffset;
        sword.heightOffset = net.heightOffset;
        sword.useManualFootprint = net.useManualFootprint;
        sword.manualFootprint = net.manualFootprint;
        sword.bonusWallRarity = net.bonusWallRarity;
        defaults.SetCustomLabel(NetPrefabPath, "옐로우맨_그물");
        defaults.SetCustomLabel(SwordPrefabPath, "옐로우맨_칼");
        EditorUtility.SetDirty(defaults);
    }

    private static void ConfigureSwordPrefab()
    {
        GameObject swordRoot = PrefabUtility.LoadPrefabContents(NetPrefabPath);
        GameObject womanRoot = PrefabUtility.LoadPrefabContents(WomanPrefabPath);
        try
        {
            swordRoot.name = "Enemy_YllowMan_Sword";
            Transform yellowRightHand =
                FindDescendant(swordRoot.transform, "mixamorig:RightHand");
            Transform womanSword = FindDescendant(womanRoot.transform, SwordPropName);
            if (yellowRightHand == null || womanSword == null)
            {
                throw new InvalidOperationException(
                    "Could not resolve the Yellow Man right hand or Woman boss sword.");
            }

            RemoveChildrenNamed(swordRoot.transform, NetPropName);
            RemoveChildrenNamed(swordRoot.transform, SwordPropName);

            GameObject sword = UnityEngine.Object.Instantiate(womanSword.gameObject);
            sword.name = SwordPropName;
            sword.transform.SetParent(yellowRightHand, false);
            sword.transform.localPosition = womanSword.localPosition;
            sword.transform.localRotation = womanSword.localRotation;
            sword.transform.localScale = womanSword.localScale;

            EnemyEventController controller =
                swordRoot.GetComponent<EnemyEventController>() ??
                swordRoot.AddComponent<EnemyEventController>();
            controller.MoveAnimation = EnemyMoveAnimation.Run;
            controller.MoveSpeed = 4f;

            SavePrefab(swordRoot, SwordPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(womanRoot);
            PrefabUtility.UnloadPrefabContents(swordRoot);
        }
    }

    private static void RenameOpenSceneNetInstances()
    {
        EnemyEventController[] controllers = UnityEngine.Object.FindObjectsByType<
            EnemyEventController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        foreach (EnemyEventController controller in controllers)
        {
            if (controller == null || !controller.gameObject.scene.IsValid())
                continue;

            string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                controller.gameObject);
            if (!string.Equals(sourcePath, NetPrefabPath, StringComparison.Ordinal))
                continue;
            if (controller.gameObject.name.Contains(
                    "Enemy_YllowMan_Net",
                    StringComparison.Ordinal))
            {
                continue;
            }

            string nextName = controller.gameObject.name.Replace(
                "Enemy_YllowMan",
                "Enemy_YllowMan_Net",
                StringComparison.Ordinal);
            if (nextName == controller.gameObject.name)
                continue;

            Undo.RecordObject(controller.gameObject, "Rename Yellow Man Net Instance");
            controller.gameObject.name = nextName;
            EditorUtility.SetDirty(controller.gameObject);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
    }

    private static Transform FindDescendant(Transform root, string exactName)
    {
        return root
            .GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.name, exactName, StringComparison.Ordinal));
    }

    private static void RemoveChildrenNamed(Transform root, string exactName)
    {
        Transform[] matches = root
            .GetComponentsInChildren<Transform>(true)
            .Where(candidate =>
                candidate != root &&
                string.Equals(candidate.name, exactName, StringComparison.Ordinal))
            .ToArray();
        foreach (Transform match in matches)
            UnityEngine.Object.DestroyImmediate(match.gameObject);
    }

    private static void SavePrefab(GameObject root, string assetPath)
    {
        EditorUtility.SetDirty(root);
        if (PrefabUtility.SaveAsPrefabAsset(root, assetPath) == null)
            throw new InvalidOperationException($"Failed to save prefab: {assetPath}");
    }
}
#endif
