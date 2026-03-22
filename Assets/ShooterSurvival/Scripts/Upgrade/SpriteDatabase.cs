using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteDatabase", menuName = "Game/Sprite Database")]
public class SpriteDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;
        public Sprite sprite;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    public bool TryGetSprite(string key, out Sprite sprite)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            sprite = null;
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                continue;

            if (!string.Equals(entry.key, key, StringComparison.OrdinalIgnoreCase))
                continue;

            sprite = entry.sprite;
            return sprite != null;
        }

        sprite = null;
        return false;
    }

    public Sprite GetSpriteOrDefault(string key)
    {
        return TryGetSprite(key, out var sprite) ? sprite : null;
    }
}
