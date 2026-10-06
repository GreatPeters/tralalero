using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Essential proposal 13: replaces the "새벽 시장 · 4:00" clock with a campaign track (chapters 1-5,
// the current one highlighted) and the current chapter's real progress. Original art, built at
// runtime so every chapter scene shares it. Best record is saved and shown in Settings.
public sealed class ChapterProgressHud : MonoBehaviour
{
    public const string ObjectName = "ChapterProgressHud";
    private static readonly Color Done = new Color(1f, .80f, .16f), Future = new Color(.55f, .62f, .72f, .85f),
        Ink = new Color(.03f, .08f, .20f, .92f), Track = new Color(.85f, .90f, .97f, .55f);
    private readonly Image[] nodes = new Image[5];
    private Image fill;
    private TMP_Text caption;
    private CanvasScript canvas;
    private ChapterProgression progression;
    private PlayerScript player;
    private readonly ChapterRunProgress progress = new();
    private bool wasRunning;
    private int lastPercent = -1;

    public static ChapterProgressHud Create(CanvasScript canvas)
    {
        if (canvas == null) return null;
        var existing = canvas.transform.Find(ObjectName);
        if (existing != null) return existing.GetComponent<ChapterProgressHud>();
        var root = new GameObject(ObjectName, typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        // Directly under the pause gear (anchors .92-.98). One compact row so it ends above y=-240,
        // where the Ch4/Ch5 objective panel starts, and stays clear of the HP plate on the left.
        rect.sizeDelta = new Vector2(300f, 46f);
        rect.anchoredPosition = new Vector2(-26f, -190f);
        var hud = root.AddComponent<ChapterProgressHud>();
        hud.canvas = canvas;
        hud.group = root.AddComponent<CanvasGroup>();
        hud.group.alpha = 0f; hud.group.blocksRaycasts = false; hud.group.interactable = false;
        hud.Build(rect);
        return hud;
    }

    private void Build(RectTransform root)
    {
        var plate = Make<Image>(root, "Plate", 0, 0, 1, 1); plate.color = Ink; plate.sprite = Circle(); plate.type = Image.Type.Sliced; plate.pixelsPerUnitMultiplier = 2.6f; plate.raycastTarget = false;
        var line = Make<Image>(root, "Track", .07f, .62f, .69f, .67f); line.color = Track; line.raycastTarget = false;
        for (int i = 0; i < nodes.Length; i++)
        {
            float x = .07f + .155f * i;
            var node = Make<Image>(root, "Chapter" + (i + 1), x - .05f, .32f, x + .05f, .97f);
            node.sprite = Circle(); node.preserveAspect = true; node.raycastTarget = false;
            var number = Make<TextMeshProUGUI>(node.rectTransform, "Number", 0, 0, 1, 1);
            Style(number, (i + 1).ToString(), 20); number.color = Ink;
            nodes[i] = node;
        }
        var bar = Make<Image>(root, "Bar", .03f, .06f, .71f, .20f); bar.color = Track; bar.raycastTarget = false;
        fill = Make<Image>(bar.rectTransform, "Fill", 0, 0, 1, 1);
        fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.color = Done; fill.raycastTarget = false;
        caption = Make<TextMeshProUGUI>(root, "Percent", .72f, .02f, .98f, .98f);
        Style(caption, "", 26); caption.color = new Color(1f, .95f, .82f); caption.alignment = TextAlignmentOptions.Center;
    }

    private static T Make<T>(RectTransform parent, string name, float x0, float y0, float x1, float y1) where T : Component
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1); rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect.gameObject.AddComponent<T>();
    }

    private static void Style(TMP_Text text, string value, float size)
    {
        var font = Resources.Load<TMP_FontAsset>("UI/CoastalRoundedJua SDF");
        if (font != null) text.font = font;
        text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
    }

    private static Sprite circle;
    private static Sprite Circle()
    {
        if (circle != null) return circle;
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Progress node", wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size * .5f, size * .5f));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(size * .5f - d)));
            }
        tex.SetPixels32(px); tex.Apply(false, true);
        circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(31, 31, 31, 31));
        return circle;
    }

    private CanvasGroup group;
    private float nextMeasure;

    private void Update()
    {
        if (canvas == null) return;
        if (progression == null) progression = canvas.GetComponent<ChapterProgression>();
        if (progression == null) progression = FindFirstObjectByType<ChapterProgression>();
        if (player == null) player = FindFirstObjectByType<PlayerScript>();
        bool running = TimeManager.isGameRunning && !CanvasScript.isGameOver;
        if (running && !wasRunning && progression != null && progression.Elapsed < .5f) { progress.Reset(); nextMeasure = 0f; }
        if (!running && wasRunning) SaveBest();
        wasRunning = running;
        bool completed = progression != null && progression.Completed;
        if (group != null) group.alpha = running || completed ? 1f : 0f;
        if (!running && !completed) return;
        if (Time.unscaledTime >= nextMeasure || completed)
        {
            nextMeasure = Time.unscaledTime + .25f;
            if (completed) progress.Complete();
            else if (ChapterRunProgress.TryMeasure(progression, player, out var reading)) progress.Update(reading, false);
        }
        Render(progression != null ? progression.chapter : 1, progress.Value);
        if (completed) SaveBest();
    }

    private void Render(int chapter, float value)
    {
        chapter = Mathf.Clamp(chapter, 1, 5);
        for (int i = 0; i < nodes.Length; i++)
        {
            int n = i + 1;
            nodes[i].color = n < chapter ? Done : n == chapter ? Color.white : Future;
            nodes[i].rectTransform.localScale = Vector3.one * (n == chapter ? 1.25f : 1f);
        }
        fill.fillAmount = value;
        int percent = Mathf.FloorToInt(value * 100f);
        if (percent != lastPercent) { lastPercent = percent; caption.text = percent + "%"; }
    }

    private void SaveBest()
    {
        if (progression != null) ChapterRunProgress.SaveBestIfHigher(progression.chapter, progress.Value);
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveBest(); }
    private void OnDestroy() => SaveBest();
}
