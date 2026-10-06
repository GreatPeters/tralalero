using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Essential proposal 7 (2026-10-06): scene loads show a spinner and a tip, hide the world and
// block input until the new scene has rendered. No percentage is shown because Unity's
// single-scene progress jumps from 0.9 to done and would be a fake number.
public sealed class LoadingOverlay : MonoBehaviour
{
    // Every tip states a rule that the code enforces; keep them in sync with gameplay.
    public static readonly string[] Tips =
    {
        "챕터를 클리어하면 다음 챕터를 시작할 수 있어요.",
        "적과 부딪히면 서로 남은 체력만큼 피해를 주고받아요.",
        "모은 코인으로 공격력과 체력을 강화할 수 있어요.",
        "오른쪽 아래 X2 버튼으로 게임 속도를 두 배로 올릴 수 있어요.",
        "줄지어 선 적은 최소 한 명을 쓰러뜨려야 지나갈 수 있어요.",
    };

    private static LoadingOverlay instance;
    private static int tipCursor = -1;
    private RectTransform spinner;
    private TMP_Text tip;
    private CanvasGroup group;
    private bool loading;

    public static bool IsLoading => instance != null && instance.loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; tipCursor = -1; }

    public static string NextTip()
    {
        tipCursor = (tipCursor + 1) % Tips.Length;
        return Tips[tipCursor];
    }

    // Shows the overlay first, waits one rendered frame, then loads. Falls back to a plain load
    // when called outside Play Mode.
    public static void LoadScene(string sceneName)
    {
        if (!Application.isPlaying) { SceneManager.LoadScene(sceneName); return; }
        Begin(sceneName, null);
    }

    // For callers that already own a coroutine (chapter transition). The load itself runs on the
    // persistent overlay, so it finishes even though the caller is destroyed with its scene.
    public static IEnumerator LoadSceneRoutine(string sceneName, System.Action beforeLoad = null)
    {
        var routine = Begin(sceneName, beforeLoad);
        if (routine != null) yield return routine;
    }

    private static Coroutine Begin(string sceneName, System.Action beforeLoad)
    {
        var overlay = Ensure();
        if (overlay.loading) return null;
        overlay.loading = true;
        overlay.gameObject.SetActive(true);
        return overlay.StartCoroutine(overlay.Load(sceneName, beforeLoad));
    }

    private static LoadingOverlay Ensure()
    {
        if (instance != null) return instance;
        var root = new GameObject("Loading Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
        instance = root.AddComponent<LoadingOverlay>();
        instance.group = root.GetComponent<CanvasGroup>();
        instance.Build(root.transform);
        root.SetActive(false);
        return instance;
    }

    private void Build(Transform root)
    {
        var shade = Child(root, "Shade", 0, 0, 1, 1).gameObject.AddComponent<Image>();
        shade.color = new Color(.02f, .06f, .14f, 1f); shade.raycastTarget = true; // blocks every touch
        var font = Resources.Load<TMP_FontAsset>("UI/CoastalRoundedJua SDF");
        spinner = Child(root, "Spinner", .5f, .5f, .5f, .5f);
        spinner.sizeDelta = new Vector2(150, 150); spinner.anchoredPosition = new Vector2(0, 120);
        var ring = spinner.gameObject.AddComponent<RawImage>(); ring.texture = SpinnerTexture(); ring.raycastTarget = false;
        var title = Text(root, "Title", font, "불러오는 중", 58, .1f, .44f, .9f, .50f);
        title.color = new Color(1f, .93f, .72f);
        tip = Text(root, "Tip", font, "", 40, .1f, .30f, .9f, .41f);
        tip.color = new Color(.86f, .93f, 1f);
        tip.textWrappingMode = TextWrappingModes.Normal;
    }

    private static RectTransform Child(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1); rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, string value, float size, float x0, float y0, float x1, float y1)
    {
        var text = Child(parent, name, x0, y0, x1, y1).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }

    private static Texture2D SpinnerTexture()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Loading spinner", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size - .5f, dy = (y + .5f) / size - .5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = (Mathf.Atan2(dy, dx) / (2 * Mathf.PI) + 1f) % 1f;
                float band = Mathf.Clamp01(1f - Mathf.Abs(r - .38f) / .07f);
                byte a = (byte)(255 * Mathf.Clamp01(band * 2f) * Mathf.Lerp(.15f, 1f, angle));
                pixels[y * size + x] = new Color32(255, 214, 70, a);
            }
        tex.SetPixels32(pixels); tex.Apply(false, true);
        return tex;
    }

    private IEnumerator Load(string sceneName, System.Action beforeLoad)
    {
        loading = true;
        tip.text = NextTip();
        gameObject.SetActive(true);
        group.alpha = 1; group.blocksRaycasts = true;
        yield return null; // let the overlay render before the synchronous part of the load
        yield return new WaitForEndOfFrame();
        beforeLoad?.Invoke();
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (op != null) while (!op.isDone) yield return null;
        // Keep the world covered until the new scene has started and drawn a frame.
        yield return null;
        yield return new WaitForEndOfFrame();
        yield return null;
        loading = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (spinner != null) spinner.Rotate(0, 0, -300f * Time.unscaledDeltaTime);
    }
}
