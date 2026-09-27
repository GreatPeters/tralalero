using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top-of-screen "survive N seconds" banner for the rest-stop food-court holdout.
// It only reads RestStopHoldout's public state, so holdout rules and this presentation evolve separately.
// The hierarchy is built once from the serialized plaque sprite and the shared game font.
public sealed class RestStopHoldoutBanner : MonoBehaviour
{
    public RestStopHoldout holdout;
    public Sprite plaque;
    public Color bandText = new(1f, .97f, .9f), timerColor = new(.05f, .29f, .36f), barColor = new(.93f, .66f, .16f), stampColor = new(.84f, .16f, .12f);

    private RectTransform root, fill, stamp;
    private TMP_Text title, timer, stampText;
    private Image[] pips;
    private CanvasGroup group;
    private float shownAt = -1, completedAt = -1;
    private bool wasActive;
    public const float SlideSeconds = .35f, PulseSeconds = 5f, StampSeconds = 1.6f;

    private void Awake() => Build();

    private void Build()
    {
        if (root != null) return;
        var font = GameUIFont.Load();
        root = Rect("HoldoutBanner", (RectTransform)transform);
        root.anchorMin = root.anchorMax = new Vector2(.5f, 1); root.pivot = new Vector2(.5f, 1);
        root.sizeDelta = new Vector2(760, 282); root.anchoredPosition = new Vector2(0, -24);
        group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
        var image = root.gameObject.AddComponent<Image>(); image.sprite = plaque; image.preserveAspect = true; image.raycastTarget = false;

        title = Text("Title", root, font, 44, bandText, new Vector2(0, -94), new Vector2(520, 70));
        title.fontStyle = FontStyles.Bold; title.outlineWidth = .18f; title.outlineColor = new Color(.02f, .18f, .24f);
        timer = Text("Timer", root, font, 84, timerColor, new Vector2(0, -172), new Vector2(360, 96));
        timer.fontStyle = FontStyles.Bold;

        var track = Rect("Track", root); Anchor(track, new Vector2(0, -236), new Vector2(470, 14));
        track.gameObject.AddComponent<Image>().color = new Color(.05f, .29f, .36f, .22f);
        fill = Rect("Fill", track); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.pivot = new Vector2(0, .5f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = barColor;

        pips = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var pip = Rect("Phase" + (i + 1), root); Anchor(pip, new Vector2(-300 + i * 32, -172), new Vector2(22, 22));
            pips[i] = pip.gameObject.AddComponent<Image>(); pips[i].raycastTarget = false;
        }

        stamp = Rect("Stamp", root); Anchor(stamp, new Vector2(210, -160), new Vector2(260, 110));
        stampText = Text("Label", stamp, font, 62, stampColor, Vector2.zero, new Vector2(260, 110));
        stampText.rectTransform.anchorMin = stampText.rectTransform.anchorMax = new Vector2(.5f, .5f);
        stampText.fontStyle = FontStyles.Bold; stampText.outlineWidth = .12f; stampText.outlineColor = Color.white;
        stamp.gameObject.SetActive(false);
        root.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (holdout == null) return;
        bool active = holdout.Active;
        if (active && !wasActive) { shownAt = Time.unscaledTime; completedAt = -1; root.gameObject.SetActive(true); stamp.gameObject.SetActive(false); }
        if (!active && wasActive && holdout.Completed) { completedAt = Time.unscaledTime; stamp.gameObject.SetActive(true); }
        if (!active && wasActive && !holdout.Completed) root.gameObject.SetActive(false);
        wasActive = active;
        if (!root.gameObject.activeSelf) return;

        float duration = Mathf.Max(1, holdout.Duration), remaining = Mathf.Max(0, duration - holdout.Elapsed);
        title.text = Mathf.RoundToInt(duration) + "초만 버텨라!";
        timer.text = remaining >= 10 ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("0.0");
        fill.anchorMax = new Vector2(Mathf.Clamp01(holdout.Elapsed / duration), 1);
        int phase = RestStopHoldout.Phase(holdout.Elapsed, duration);
        for (int i = 0; i < pips.Length; i++)
            pips[i].color = i < phase ? barColor : i == phase ? stampColor : new Color(.05f, .29f, .36f, .25f);

        float slide = Mathf.Clamp01((Time.unscaledTime - shownAt) / SlideSeconds);
        root.anchoredPosition = new Vector2(0, Mathf.Lerp(260, -24, 1 - Mathf.Pow(1 - slide, 3)));
        float pulse = active && remaining <= PulseSeconds ? 1 + .08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 2)) : 1;
        timer.rectTransform.localScale = Vector3.one * pulse;
        timer.color = active && remaining <= PulseSeconds ? Color.Lerp(timerColor, stampColor, .7f) : timerColor;

        if (completedAt >= 0)
        {
            float t = Time.unscaledTime - completedAt;
            stampText.text = "버텼다!";
            float land = Mathf.Clamp01(t / .22f);
            stamp.localScale = Vector3.one * Mathf.Lerp(2.2f, 1, land);
            stamp.localRotation = Quaternion.Euler(0, 0, -12);
            group.alpha = t < StampSeconds ? 1 : Mathf.Clamp01(1 - (t - StampSeconds) / .4f);
            if (t > StampSeconds + .4f) { root.gameObject.SetActive(false); completedAt = -1; group.alpha = 1; }
        }
        else group.alpha = 1;
    }

    private static RectTransform Rect(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false); return rt;
    }
    private static void Anchor(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1); rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = position; rt.sizeDelta = size;
    }
    private static TMP_Text Text(string name, RectTransform parent, TMP_FontAsset font, float size, Color color, Vector2 position, Vector2 box)
    {
        var rt = Rect(name, parent); Anchor(rt, position, box);
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false;
        return text;
    }
}
