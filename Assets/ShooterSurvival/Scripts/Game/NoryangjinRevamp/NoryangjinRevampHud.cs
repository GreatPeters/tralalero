using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top-of-screen banner ("길이 막혔다! 부숴서 뚫어라", market announcements) and the shutter
// countdown bar. Built at runtime under the scene Canvas so the copied scene needs no UI edits.
public sealed class NoryangjinRevampHud : MonoBehaviour
{
    private TextMeshProUGUI bannerText, timerText;
    private Image bannerPanel, timerFill;
    private GameObject timerRoot;
    private float bannerLeft;
    public string CurrentMessage => bannerText != null ? bannerText.text : "";

    public static NoryangjinRevampHud Create(Transform canvas)
    {
        var root = new GameObject("NoryangjinRevampHud", typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        var rt = (RectTransform)root.transform;
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(.5f, 1);
        rt.anchoredPosition = new Vector2(0, -330); rt.sizeDelta = new Vector2(0, 260);
        var hud = root.AddComponent<NoryangjinRevampHud>();
        var font = Resources.Load<TMP_FontAsset>("UI/GmarketHarbor SDF");

        hud.bannerPanel = Panel(root.transform, "Banner", new Vector2(0, 0), new Vector2(940, 118), new Color(.07f, .12f, .32f, .9f));
        hud.bannerText = Label(hud.bannerPanel.transform, font, 50, Color.white);

        hud.timerRoot = new GameObject("ShutterTimer", typeof(RectTransform));
        hud.timerRoot.transform.SetParent(root.transform, false);
        var tr = (RectTransform)hud.timerRoot.transform;
        tr.anchorMin = tr.anchorMax = new Vector2(.5f, 1); tr.pivot = new Vector2(.5f, 1);
        tr.anchoredPosition = new Vector2(0, -132); tr.sizeDelta = new Vector2(780, 90);
        var back = Panel(hud.timerRoot.transform, "Back", Vector2.zero, new Vector2(780, 90), new Color(.1f, .08f, .08f, .88f));
        hud.timerFill = Panel(back.transform, "Fill", new Vector2(0, -8), new Vector2(740, 26), new Color(.95f, .25f, .18f, 1));
        var fr = hud.timerFill.rectTransform; fr.anchorMin = fr.anchorMax = new Vector2(.5f, 0); fr.pivot = new Vector2(.5f, 0); fr.anchoredPosition = new Vector2(0, 10);
        hud.timerFill.type = Image.Type.Filled; hud.timerFill.fillMethod = Image.FillMethod.Horizontal;
        hud.timerFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
        hud.timerText = Label(back.transform, font, 40, Color.white);
        var ttr = hud.timerText.rectTransform; ttr.offsetMin = new Vector2(0, 34);
        hud.timerRoot.SetActive(false);
        hud.bannerPanel.gameObject.SetActive(false);
        return hud;
    }

    private static Image Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(.5f, 1); r.pivot = new Vector2(.5f, 1);
        r.anchoredPosition = pos; r.sizeDelta = size;
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI Label(Transform parent, TMP_FontAsset font, float size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(16, 4); r.offsetMax = new Vector2(-16, -4);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = font; t.fontSize = size; t.color = color; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center; t.enableAutoSizing = true; t.fontSizeMin = 24; t.fontSizeMax = size;
        t.raycastTarget = false;
        return t;
    }

    public void ResetForRun() { bannerLeft = 0; bannerPanel.gameObject.SetActive(false); HideTimer(); }

    public void Publish(string message, float seconds)
    {
        bannerText.text = message; bannerLeft = seconds;
        bannerPanel.gameObject.SetActive(true);
    }

    public void ShowTimer(string caption, float remaining, float total)
    {
        timerRoot.SetActive(true);
        timerText.text = $"{caption} {Mathf.CeilToInt(Mathf.Max(0, remaining))}초";
        timerFill.fillAmount = total > 0 ? Mathf.Clamp01(remaining / total) : 0;
    }

    public void HideTimer() { if (timerRoot != null) timerRoot.SetActive(false); }

    private void Update()
    {
        if (bannerLeft <= 0) return;
        bannerLeft -= Time.unscaledDeltaTime;
        if (bannerLeft <= 0) bannerPanel.gameObject.SetActive(false);
    }
}
