using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Small bottom-right X1/X2 button (essential proposal 10). Built at runtime from the pause button's
// art so all five chapter scenes get it without scene edits. Visible only while a run is live.
public sealed class GameSpeedToggle : MonoBehaviour
{
    public const string ObjectName = "GameSpeedToggle";
    // The pause-button art is navy enamel, so the caption carries the state: cream X1, gold X2.
    private static readonly Color NormalText = new Color(1f, .95f, .82f), FastText = new Color(1f, .80f, .12f);
    private TMP_Text label;
    private Image surface;
    private RectTransform rect;
    private Rect appliedSafeArea;

    public static GameSpeedToggle Create(CanvasScript canvas)
    {
        if (canvas == null || canvas.pauseButton == null) return null;
        var existing = canvas.transform.Find(ObjectName);
        if (existing != null) return existing.GetComponent<GameSpeedToggle>();
        var go = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetSiblingIndex(canvas.pauseButton.transform.GetSiblingIndex() + 1);
        var toggle = go.AddComponent<GameSpeedToggle>();
        toggle.rect = (RectTransform)go.transform;
        toggle.rect.anchorMin = toggle.rect.anchorMax = toggle.rect.pivot = new Vector2(1f, 0f);
        toggle.rect.sizeDelta = new Vector2(150f, 96f);
        toggle.surface = go.GetComponent<Image>();
        var source = canvas.pauseButton.GetComponent<Image>();
        if (source != null) { toggle.surface.sprite = source.sprite; toggle.surface.type = source.type; toggle.surface.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier; }
        var button = go.GetComponent<Button>();
        button.targetGraphic = toggle.surface;
        button.onClick.AddListener(() => { GameAudioService.Play(GameSound.Tab); GameSpeed.Toggle(); });
        var text = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(go.transform, false);
        var textRect = text.rectTransform; textRect.anchorMin = new Vector2(.08f, .1f); textRect.anchorMax = new Vector2(.92f, .9f); textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        text.font = Resources.Load<TMP_FontAsset>("UI/CoastalRoundedJua SDF") ?? text.font;
        text.alignment = TextAlignmentOptions.Center; text.fontSize = 46; text.fontStyle = FontStyles.Bold;
        text.color = NormalText; text.raycastTarget = false;
        text.outlineWidth = .18f; text.outlineColor = new Color32(4, 14, 40, 255);
        toggle.label = text;
        toggle.Refresh();
        go.SetActive(false);
        return toggle;
    }

    public static string Caption(bool fast) => fast ? "X2" : "X1";

    private void OnEnable() { GameSpeed.Changed += Refresh; Refresh(); }
    private void OnDisable() => GameSpeed.Changed -= Refresh;

    private void Refresh()
    {
        if (label != null) { label.text = Caption(GameSpeed.IsFast); label.color = GameSpeed.IsFast ? FastText : NormalText; }
    }

    private void LateUpdate()
    {
        // Keep clear of the gesture bar / rounded corners using the device safe area.
        var safe = Screen.safeArea;
        if (safe == appliedSafeArea || rect == null || Screen.width <= 0) return;
        appliedSafeArea = safe;
        var canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        float right = (Screen.width - safe.xMax) / Mathf.Max(.01f, scale);
        float bottom = safe.yMin / Mathf.Max(.01f, scale);
        rect.anchoredPosition = new Vector2(-28f - right, 40f + bottom);
    }
}
