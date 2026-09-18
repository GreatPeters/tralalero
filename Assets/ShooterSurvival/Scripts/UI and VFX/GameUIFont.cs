using TMPro;
using UnityEngine;

public static class GameUIFont
{
    public const string ResourcePath = "UI/GmarketHarbor SDF";
    private static TMP_FontAsset cached;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => cached = null;
    public static TMP_FontAsset Load() => cached != null ? cached : cached = Resources.Load<TMP_FontAsset>(ResourcePath);
}
