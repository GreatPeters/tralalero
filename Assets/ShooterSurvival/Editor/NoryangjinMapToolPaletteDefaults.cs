#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

[Serializable]
public sealed class NoryangjinMapToolPalettePlacementEntry
{
    public string prefabPath;
    public Vector3 scale = Vector3.one;
    public Vector2 positionOffset;
    public float yawOffset;
    public float heightOffset;
    public bool useManualFootprint;
    public Vector2Int manualFootprint = Vector2Int.one;
    public Rarity bonusWallRarity = Rarity.Normal;

    public static NoryangjinMapToolPalettePlacementEntry CreateDefault(string prefabPath)
    {
        return new NoryangjinMapToolPalettePlacementEntry
        {
            prefabPath = prefabPath,
            scale = Vector3.one,
            positionOffset = Vector2.zero,
            yawOffset = 0f,
            heightOffset = 0f,
            useManualFootprint = false,
            manualFootprint = Vector2Int.one,
            bonusWallRarity = Rarity.Normal
        };
    }
}

[Serializable]
public sealed class NoryangjinMapToolPaletteLabelEntry
{
    public string prefabPath;
    public string displayName;
}

public sealed class NoryangjinMapToolPaletteDefaults : ScriptableObject
{
    [SerializeField] private List<NoryangjinMapToolPalettePlacementEntry> entries = new();
    [SerializeField] private List<NoryangjinMapToolPaletteLabelEntry> labelEntries = new();

    public NoryangjinMapToolPalettePlacementEntry GetOrCreateEntry(string prefabPath)
    {
        foreach (NoryangjinMapToolPalettePlacementEntry entry in entries)
        {
            if (entry != null && string.Equals(entry.prefabPath, prefabPath, StringComparison.Ordinal))
                return entry;
        }

        NoryangjinMapToolPalettePlacementEntry created = NoryangjinMapToolPalettePlacementEntry.CreateDefault(prefabPath);
        entries.Add(created);
        return created;
    }

    public void ResetEntry(string prefabPath)
    {
        NoryangjinMapToolPalettePlacementEntry entry = GetOrCreateEntry(prefabPath);
        entry.scale = Vector3.one;
        entry.positionOffset = Vector2.zero;
        entry.yawOffset = 0f;
        entry.heightOffset = 0f;
        entry.useManualFootprint = false;
        entry.manualFootprint = Vector2Int.one;
        entry.bonusWallRarity = Rarity.Normal;
    }

    public string GetCustomLabel(string prefabPath)
    {
        foreach (NoryangjinMapToolPaletteLabelEntry entry in labelEntries)
        {
            if (entry != null && string.Equals(entry.prefabPath, prefabPath, StringComparison.Ordinal))
                return entry.displayName;
        }

        return string.Empty;
    }

    public void SetCustomLabel(string prefabPath, string displayName)
    {
        foreach (NoryangjinMapToolPaletteLabelEntry entry in labelEntries)
        {
            if (entry != null && string.Equals(entry.prefabPath, prefabPath, StringComparison.Ordinal))
            {
                entry.displayName = displayName;
                return;
            }
        }

        labelEntries.Add(new NoryangjinMapToolPaletteLabelEntry
        {
            prefabPath = prefabPath,
            displayName = displayName
        });
    }
}
#endif
