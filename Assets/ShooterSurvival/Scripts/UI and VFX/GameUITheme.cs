using UnityEngine;

[CreateAssetMenu(menuName = "Shooter/Mobile UI Theme")]
public sealed class GameUITheme : ScriptableObject
{
    public const string ResourcePath = "UI/MobileTheme";
    public string paletteName;
    public Color page, card, ink, muted, primary, accent, line;
    public Color buttonInk = new Color(.09f, .24f, .30f);
    public Color SelectedCard => Color.Lerp(card, accent, .18f);
    public Color DisabledButton => Color.Lerp(card, muted, .35f);
    private static GameUITheme cached;
    public static GameUITheme Current => cached != null ? cached : cached = Resources.Load<GameUITheme>(ResourcePath);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => cached = null;
}
