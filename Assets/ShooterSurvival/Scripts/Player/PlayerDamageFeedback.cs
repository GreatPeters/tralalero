using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IndianOceanAssets.ShooterSurvival
{
    // One overlay per scene player. No render textures or per-hit object creation.
    [DefaultExecutionOrder(1200)]
    public sealed class PlayerDamageFeedback : MonoBehaviour
    {
        private Image overlay;
        private Texture2D texture;
        private Sprite sprite;
        private float flash;
        private Transform fallingModel;
        private Vector3 modelPosition;
        private Quaternion modelRotation;
        private float fallTime;
        private CanvasGroup notice;
        private TextMeshProUGUI amountText, causeText;
        private float noticeAge = 10f, accumulatedDamage;
        private PlayerDamageCause noticeCause;
        private bool mixedDamage;
        private PlayerScript owner;
        private float noticeLifetime = 1.55f;
        private int receivedFrame = -1;
        public string VisibleDamageText => notice != null && notice.alpha > 0f ? amountText.text : string.Empty;
        public int ShownHitCount { get; private set; }

        private void Awake()
        {
            owner = GetComponent<PlayerScript>();
            var root = new GameObject("Player damage vignette", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2340);
            scaler.matchWidthOrHeight = 0f;
            overlay = new GameObject("Red edges", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            overlay.transform.SetParent(root.transform, false);
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;
            overlay.rectTransform.offsetMin = overlay.rectTransform.offsetMax = Vector2.zero;
            overlay.raycastTarget = false;
            texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var colors = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float edge = Mathf.Max(Mathf.Abs((x - 31.5f) / 31.5f), Mathf.Abs((y - 31.5f) / 31.5f));
                colors[y * 64 + x] = new Color(.92f, .015f, .055f, Mathf.Pow(Mathf.InverseLerp(.52f, 1f, edge), 2));
            }
            texture.SetPixels(colors); texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * .5f);
            overlay.sprite = sprite;
            overlay.color = Color.clear;

            var panel = new GameObject("Health lost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .055f);
            rect.sizeDelta = new Vector2(550, 124);
            var background = panel.GetComponent<Image>();
            background.color = new Color(.12f, .025f, .035f, .9f);
            background.raycastTarget = false;
            notice = panel.GetComponent<CanvasGroup>();
            notice.blocksRaycasts = false; notice.interactable = false; notice.alpha = 0;
            amountText = CreateText(panel.transform, "Amount", 50, new Color(1, .38f, .3f), 22);
            causeText = CreateText(panel.transform, "Cause", 28, new Color(1, .92f, .83f), -33);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, float size, Color color, float y)
        {
            var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.font = GameUIFont.Load(); text.fontSize = size;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color; text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(530, 58);
            text.rectTransform.anchoredPosition = new Vector2(0, y);
            return text;
        }

        public void Show(float damage, float maximum, PlayerDamageCause cause = PlayerDamageCause.Other)
        {
            if (damage <= 0f) return;
            ShownHitCount++;
            bool continuing = noticeAge < .35f;
            accumulatedDamage = continuing ? accumulatedDamage + damage : damage;
            mixedDamage = continuing && (mixedDamage || noticeCause != cause);
            noticeAge = 0; noticeCause = cause;
            receivedFrame = Time.frameCount;
            noticeLifetime = owner != null && owner.currentHealth <= 0f ? .85f : 1.55f;
            amountText.SetText("-{0} HP", Mathf.Max(1, Mathf.RoundToInt(accumulatedDamage)));
            causeText.text = (mixedDamage ? "연속 피해 · " : "") + PlayerDamageCauseText.Label(cause);
            notice.alpha = 1;
            flash = Mathf.Max(flash, Mathf.Lerp(.5f, .9f, Mathf.Clamp01(damage / Mathf.Max(1f, maximum))));
            overlay.color = new Color(1, 1, 1, flash);
            DamagePopupFX.ShowPlayerDamage(transform.position + Vector3.up * 3.2f, damage);
        }

        public void FallIntoHole()
        {
            fallingModel = GetComponentInChildren<PlayerCosmeticCustomizer>(true)?.modelRoot;
            if (fallingModel == null) return;
            fallingModel.GetComponent<CosmeticHitSpin>()?.ResetPose();
            modelPosition = fallingModel.localPosition;
            modelRotation = fallingModel.localRotation;
            fallTime = 0f;
        }

        public static float VisibleFrameDelta(float unscaledDelta, bool receivedThisFrame)
            => receivedThisFrame ? 0f : Mathf.Clamp(unscaledDelta, 0f, .1f);

        private void LateUpdate()
        {
            float visibleDelta = VisibleFrameDelta(Time.unscaledDeltaTime, Time.frameCount == receivedFrame);
            flash = Mathf.MoveTowards(flash, 0f, visibleDelta * 1.65f);
            overlay.color = new Color(1, 1, 1, flash);
            noticeAge += visibleDelta;
            notice.alpha = 1f - Mathf.InverseLerp(noticeLifetime - .25f, noticeLifetime, noticeAge);
            if (fallingModel == null) return;
            fallTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fallTime / .85f);
            fallingModel.localRotation = modelRotation * Quaternion.Euler(110f * t, 0, -20f * t);
            fallingModel.localPosition = modelPosition + Vector3.down * (4f * t * t);
        }

        public void ResetFeedback()
        {
            flash = 0f;
            noticeAge = 10f; accumulatedDamage = 0; ShownHitCount = 0; mixedDamage = false;
            if (notice != null) notice.alpha = 0;
            if (overlay != null) overlay.color = Color.clear;
            if (fallingModel == null) return;
            fallingModel.localPosition = modelPosition;
            fallingModel.localRotation = modelRotation;
            fallingModel = null;
        }

        private void OnDisable() => ResetFeedback();
        private void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }
    }
}
