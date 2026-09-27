using System;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HighwayChapter2UI : MonoBehaviour
{
    public TMP_FontAsset font;
    public Sprite enamelPanel, jamPicture, openPicture;
    public Sprite missileIcon, shieldIcon, shatterIcon, magnetIcon;
    public Action<int> Chosen;
    public Action Cancelled;
    public bool Pending { get; private set; }
    public bool TollChoice { get; private set; }
    public float Remaining { get; private set; }
    public int PopupCount { get; private set; }
    public int TimeoutCount { get; private set; }
    private RectTransform popup, news, whoosh;
    private TMP_Text title, countdown, ticker, whooshText;
    private readonly TMP_Text[] labels = new TMP_Text[2], descriptions = new TMP_Text[2];
    private readonly Image[] pictures = new Image[2];
    private Image timer;
    private Sprite timerSprite;
    private float duration, newsUntil, whooshUntil, clock;
    private readonly Color navy = new(.045f, .10f, .25f), cream = new(1, .965f, .85f);

    private void Awake() => Build();
    private RectTransform Box(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        return rect;
    }
    private TMP_Text Text(Transform parent, string name, string value, Vector2 size, Vector2 position, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var text = go.GetComponent<TextMeshProUGUI>(); text.rectTransform.SetParent(parent, false);
        text.rectTransform.sizeDelta = size; text.rectTransform.anchoredPosition = position;
        text.font = font; text.text = value; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private void Build()
    {
        if (popup != null) return;
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 550;
        var scaler = GetComponent<CanvasScaler>(); if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 2340); scaler.matchWidthOrHeight = .5f;
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        popup = Box(transform, "Choice dimmer", Vector2.zero, Vector2.zero, new Color(0, 0, 0, .62f));
        popup.anchorMin = Vector2.zero; popup.anchorMax = Vector2.one; popup.offsetMin = popup.offsetMax = Vector2.zero;
        popup.GetComponent<Image>().raycastTarget = true;
        var border = Box(popup, "Cream enamel choice panel", new Vector2(980, 900), new Vector2(0, -50), navy);
        var panel = Box(border, "Cream inset", new Vector2(960, 880), Vector2.zero, cream);
        if (enamelPanel != null) { panel.GetComponent<Image>().sprite = enamelPanel; panel.GetComponent<Image>().type = Image.Type.Sliced; panel.GetComponent<Image>().color = Color.white; }
        title = Text(panel, "Title", "어디로 갈까?", new Vector2(900, 100), new Vector2(0, 345), 70, navy);
        for (int i = 0; i < 2; i++)
        {
            int choice = i;
            var card = Box(panel, "Choice " + i, new Vector2(414, 560), new Vector2(i == 0 ? -222 : 222, -2), navy);
            var face = Box(card, "Card face", new Vector2(404, 550), Vector2.zero, new Color(1, .98f, .9f));
            face.GetComponent<Image>().raycastTarget = true;
            var button = face.gameObject.AddComponent<Button>(); button.targetGraphic = face.GetComponent<Image>();
            button.onClick.AddListener(() => Select(choice));
            pictures[i] = Box(face, "Road picture", new Vector2(376, 286), new Vector2(0, 106), Color.white).GetComponent<Image>();
            pictures[i].preserveAspect = true;
            labels[i] = Text(face, "Road name", "", new Vector2(390, 90), new Vector2(0, -92), 49, navy);
            descriptions[i] = Text(face, "Road description", "", new Vector2(380, 74), new Vector2(0, -177), 33, navy);
        }
        var ring = Box(panel, "Timer ring", new Vector2(104, 104), new Vector2(0, -352), new Color(.72f, .77f, .82f));
        timer = Box(ring, "Timer fill", new Vector2(104, 104), Vector2.zero, new Color(.12f, .5f, .95f)).GetComponent<Image>();
        timer.sprite = RoundSprite(); timer.type = Image.Type.Filled; timer.fillMethod = Image.FillMethod.Radial360; timer.fillOrigin = 2; timer.fillClockwise = false;
        ring.GetComponent<Image>().sprite = timer.sprite;
        var timerFace = Box(ring, "Timer centre", new Vector2(78, 78), Vector2.zero, cream); timerFace.GetComponent<Image>().sprite = timer.sprite;
        countdown = Text(ring, "Seconds", "5", new Vector2(100, 100), Vector2.zero, 62, navy);
        popup.gameObject.SetActive(false);

        news = Box(transform, "Breaking news", new Vector2(1040, 70), Vector2.zero, new Color(.75f, .045f, .06f));
        news.anchorMin = news.anchorMax = new Vector2(.5f, 1); news.anchoredPosition = new Vector2(0, -320);
        Text(news, "News label", "속보", new Vector2(136, 62), new Vector2(-440, 0), 42, Color.white);
        var viewport = Box(news, "Ticker viewport", new Vector2(877, 62), new Vector2(75, 0), cream);
        viewport.gameObject.AddComponent<RectMask2D>();
        ticker = Text(viewport, "Ticker", "", new Vector2(1600, 62), Vector2.zero, 37, navy);
        news.gameObject.SetActive(false);
        whoosh = Box(transform, "Near miss", new Vector2(350, 160), new Vector2(380, -180), Color.clear);
        whooshText = Text(whoosh, "Whoosh", "쌩!", new Vector2(350, 160), Vector2.zero, 110, Color.white);
        whooshText.outlineColor = navy; whooshText.outlineWidth = .25f;
        whoosh.gameObject.SetActive(false);
    }

    private Sprite RoundSprite()
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) pixels[y * 64 + x] = (new Vector2(x - 31.5f, y - 31.5f)).sqrMagnitude <= 31 * 31 ? Color.white : Color.clear;
        texture.SetPixels(pixels); texture.Apply();
        timerSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * .5f);
        return timerSprite;
    }

    public void Open(bool toll, HighwayUniqueBonus left, HighwayUniqueBonus right)
    {
        Build();
        if (Pending) return;
        TollChoice = toll; Pending = true; PopupCount++;
        duration = Remaining = HighwayChapter2Data.Value("popupSeconds");
        title.text = toll ? "요금소 출구" : "어디로 갈까?";
        labels[0].text = toll ? "하이패스" : "정체 구간";
        labels[1].text = toll ? "현금" : "뻥 뚫린 길";
        descriptions[0].text = toll ? BonusName(left) : "차 많음·느림";
        descriptions[1].text = toll ? BonusName(right) : "차 적음·빠름";
        pictures[0].sprite = toll ? BonusSprite(left) : jamPicture;
        pictures[1].sprite = toll ? BonusSprite(right) : openPicture;
        timer.fillAmount = 1; countdown.text = Mathf.CeilToInt(Remaining).ToString();
        popup.gameObject.SetActive(true);
    }

    public static string BonusName(HighwayUniqueBonus value) => value switch
    {
        HighwayUniqueBonus.ChainMissile => "연쇄 미사일", HighwayUniqueBonus.GoldenShield => "황금 방패",
        HighwayUniqueBonus.Shatter => "파쇄탄", _ => "코인 자석"
    };
    public Sprite BonusSprite(HighwayUniqueBonus value) => value switch
    {
        HighwayUniqueBonus.ChainMissile => missileIcon, HighwayUniqueBonus.GoldenShield => shieldIcon,
        HighwayUniqueBonus.Shatter => shatterIcon, _ => magnetIcon
    };

    public void Select(int index)
    {
        if (!Pending || index < 0 || index > 1) return;
        Pending = false; popup.gameObject.SetActive(false);
        Chosen?.Invoke(index);
    }
    public void Publish(string message)
    {
        Build(); ticker.text = message; ticker.rectTransform.anchoredPosition = new Vector2(650, 0);
        newsUntil = clock + HighwayChapter2Data.Value("newsSeconds"); news.gameObject.SetActive(true);
    }
    public void NearMiss(float lane)
    {
        Build(); whoosh.anchoredPosition = new Vector2(Mathf.Sign(lane) * 340, -180);
        whooshUntil = clock + HighwayChapter2Data.Value("windSeconds"); whoosh.gameObject.SetActive(true);
    }
    public void ResetForRun()
    {
        Cancel(); PopupCount = TimeoutCount = 0; clock = 0;
        if (news != null) news.gameObject.SetActive(false);
        if (whoosh != null) whoosh.gameObject.SetActive(false);
    }
    private void Cancel()
    {
        bool wasPending = Pending; Pending = false;
        if (popup != null) popup.gameObject.SetActive(false);
        if (wasPending) Cancelled?.Invoke();
    }
    private void Update()
    {
        if (!TimeManager.isGameRunning) { Cancel(); return; }
        clock += Time.deltaTime;
        if (Pending)
        {
            Remaining = Mathf.Max(0, Remaining - Time.unscaledDeltaTime);
            countdown.text = Mathf.CeilToInt(Remaining).ToString(); timer.fillAmount = Remaining / duration;
            if (Remaining <= 0) { TimeoutCount++; Select(0); }
        }
        if (news != null && news.gameObject.activeSelf)
        {
            ticker.rectTransform.anchoredPosition -= Vector2.right * (400 * Time.deltaTime);
            if (clock >= newsUntil) news.gameObject.SetActive(false);
        }
        if (whoosh != null && whoosh.gameObject.activeSelf && clock >= whooshUntil) whoosh.gameObject.SetActive(false);
    }
    private void OnDisable() => Cancel();
    private void OnDestroy()
    {
        if (timerSprite == null) return;
        Destroy(timerSprite.texture); Destroy(timerSprite);
    }
}
