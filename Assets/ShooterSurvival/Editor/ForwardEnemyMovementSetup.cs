#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;

public static class ForwardEnemyMovementSetup
{
    public const string TriggerPrefabPath =
        "Assets/ShooterSurvival/Prefabs/Gameplay/Noryangjin_EnemyMovementTrigger.prefab";

    public static readonly string[] EnemyPrefabPaths =
    {
        "Assets/JH/Model/Prefab/Enemy_FatMan.prefab",
        "Assets/JH/Model/Prefab/Enemy_Guard.prefab",
        "Assets/JH/Model/Prefab/Enemy_OldMan.prefab",
        "Assets/JH/Model/Prefab/Enemy_Woman.prefab",
        "Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab",
        "Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab"
    };

    public static void Configure()
    {
        int changedPrefabCount = 0;
        foreach (string prefabPath in EnemyPrefabPaths)
        {
            if (EnsureEventController(prefabPath))
                changedPrefabCount++;
        }

        bool changedTriggerPrefab = EnsureTriggerPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        NoryangjinMapToolWindow.RefreshOpenWindowPaletteAssets();

        Debug.Log(
            $"[Forward Enemy Movement] Ready. Enemy prefabs changed: " +
            $"{changedPrefabCount}; trigger prefab changed: {changedTriggerPrefab}.");
    }

    private static bool EnsureEventController(string prefabPath)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            EnemyEventController controller =
                prefabRoot.GetComponent<EnemyEventController>();
            if (controller != null)
                return false;

            GameObject[] nestedPrefabRoots = prefabRoot
                .GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.gameObject)
                .Where(PrefabUtility.IsAnyPrefabInstanceRoot)
                .ToArray();
            var originalModificationKeys = nestedPrefabRoots.ToDictionary(
                nestedRoot => nestedRoot,
                nestedRoot => new HashSet<string>(
                    (PrefabUtility.GetPropertyModifications(nestedRoot) ??
                     Array.Empty<PropertyModification>())
                    .Select(GetModificationKey)));

            prefabRoot.AddComponent<EnemyEventController>();
            RemoveNewHumanoidPoseOverrides(
                nestedPrefabRoots,
                originalModificationKeys);

            if (PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath) == null)
                throw new InvalidOperationException(
                    $"Failed to save enemy prefab: {prefabPath}");

            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static bool EnsureTriggerPrefab()
    {
        GameObject existing =
            AssetDatabase.LoadAssetAtPath<GameObject>(TriggerPrefabPath);
        if (existing != null)
            return RepairTriggerPrefab();

        var triggerObject = new GameObject("Noryangjin Enemy Movement Trigger");
        try
        {
            EnemyEventActivationSpot trigger =
                triggerObject.AddComponent<EnemyEventActivationSpot>();
            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            EnemyEventActivationSpot.ConfigureCollider(
                collider,
                applyDefaultShape: true);

            if (PrefabUtility.SaveAsPrefabAsset(
                    triggerObject,
                    TriggerPrefabPath) == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create movement trigger prefab: {TriggerPrefabPath}");
            }

            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(triggerObject);
        }
    }

    private static bool RepairTriggerPrefab()
    {
        GameObject prefabRoot =
            PrefabUtility.LoadPrefabContents(TriggerPrefabPath);
        try
        {
            bool changed = false;
            bool needsDefaultColliderShape = false;
            EnemyEventActivationSpot trigger =
                prefabRoot.GetComponent<EnemyEventActivationSpot>();
            if (trigger == null)
            {
                trigger =
                    prefabRoot.AddComponent<EnemyEventActivationSpot>();
                changed = true;
                needsDefaultColliderShape = true;
            }

            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = prefabRoot.AddComponent<BoxCollider>();
                changed = true;
                needsDefaultColliderShape = true;
            }

            bool colliderWasTrigger = collider.isTrigger;

            bool applyDefaultShape =
                needsDefaultColliderShape || collider.size == Vector3.zero;
            EnemyEventActivationSpot.ConfigureCollider(
                collider,
                applyDefaultShape);
            if (!colliderWasTrigger || applyDefaultShape)
                changed = true;

            if (!changed)
                return false;

            if (PrefabUtility.SaveAsPrefabAsset(
                    prefabRoot,
                    TriggerPrefabPath) == null)
            {
                throw new InvalidOperationException(
                    $"Failed to repair movement trigger prefab: {TriggerPrefabPath}");
            }

            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void RemoveNewHumanoidPoseOverrides(
        IEnumerable<GameObject> nestedPrefabRoots,
        IReadOnlyDictionary<GameObject, HashSet<string>> originalModificationKeys)
    {
        foreach (GameObject nestedRoot in nestedPrefabRoots)
        {
            PropertyModification[] modifications =
                PrefabUtility.GetPropertyModifications(nestedRoot) ??
                Array.Empty<PropertyModification>();
            PropertyModification[] filtered = modifications
                .Where(modification =>
                    originalModificationKeys[nestedRoot].Contains(
                        GetModificationKey(modification)) ||
                    !IsTransientHumanoidPoseOverride(modification))
                .ToArray();
            PrefabUtility.SetPropertyModifications(nestedRoot, filtered);
        }
    }

    private static string GetModificationKey(PropertyModification modification)
    {
        int targetInstanceId =
            modification.target != null ? modification.target.GetInstanceID() : 0;
        return $"{targetInstanceId}:{modification.propertyPath}";
    }

    private static bool IsTransientHumanoidPoseOverride(
        PropertyModification modification)
    {
        if (!(modification.target is Transform))
            return false;

        return modification.propertyPath.StartsWith(
                   "m_LocalRotation.",
                   StringComparison.Ordinal) ||
               modification.propertyPath.StartsWith(
                   "m_LocalEulerAnglesHint.",
                   StringComparison.Ordinal);
    }

}
#endif
