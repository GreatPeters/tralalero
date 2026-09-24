using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndianOceanAssets.ShooterSurvival
{
    // Scene-owned, bounded presentation only. Saturation replaces the oldest number.
    public sealed class DamagePopupPool : MonoBehaviour
    {
        private const int Capacity = 64;
        private const float Duration = .75f;
        private sealed class Popup
        {
            public Canvas canvas;
            public TextMeshProUGUI text;
            public Vector3 origin;
            public Color color;
            public float age;
            public bool active;
            public bool playerDamage;
            public float amount;
            public int receivedFrame;
        }

        private Popup[] popups;
        private int next;
        private Camera cachedCamera;

        public void Initialize(TextMeshProUGUI template)
        {
            if (popups != null) return;
            popups = new Popup[Capacity];
            cachedCamera = Camera.main;
            for (int i = 0; i < popups.Length; i++)
            {
                var root = new GameObject("DamagePopup", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                root.transform.SetParent(transform, false);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = cachedCamera;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 32767;
                root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
                ((RectTransform)root.transform).sizeDelta = new Vector2(240f, 120f);

                TextMeshProUGUI text;
                if (template != null) text = Instantiate(template, root.transform, false);
                else
                {
                    text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                    text.transform.SetParent(root.transform, false);
                    text.fontSize = 72f;
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.outlineWidth = .2f;
                    text.outlineColor = new Color(0f, 0f, 0f, .8f);
                }
                text.font = GameUIFont.Load();
                text.raycastTarget = false;
                text.gameObject.SetActive(true);
                text.enabled = true;
                var rect = text.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                // Build glyph/mesh capacity in the lobby, before the first combat hit.
                text.SetText("+0123456789-");
                text.ForceMeshUpdate();
                popups[i] = new Popup { canvas = canvas, text = text };
                root.SetActive(false);
            }
        }

        public void Show(Vector3 position, int amount, bool coin, bool playerDamage = false)
            => ShowExact(position, amount, coin, playerDamage);

        public void ShowPlayerDamage(Vector3 position, float amount)
            => ShowExact(position, amount, false, true);

        private void ShowExact(Vector3 position, float amount, bool coin, bool playerDamage)
        {
            if (popups == null) Initialize(null);
            // Reserve the last prewarmed slot so enemy/coin bursts cannot erase
            // the player's own loss. Consecutive hits share one readable number.
            Popup popup = popups[playerDamage ? popups.Length - 1 : next];
            if (!playerDamage) next = (next + 1) % (popups.Length - 1);
            popup.amount = playerDamage && popup.active && popup.age < .35f ? popup.amount + amount : amount;
            popup.playerDamage = playerDamage;
            popup.receivedFrame = Time.frameCount;
            popup.age = 0f;
            popup.active = true;
            popup.origin = position + new Vector3(Random.Range(-.15f, .15f), .35f, 0f);
            popup.color = coin ? new Color(1f, .88f, .22f, 1f) : new Color(1f, .25f, .25f, 1f);
            var root = popup.canvas.transform;
            root.position = popup.origin;
            root.localScale = Vector3.one * (playerDamage ? .012f : coin ? .007f : .01f);
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled) cachedCamera = Camera.main;
            popup.canvas.worldCamera = cachedCamera;
            if (cachedCamera != null) root.forward = cachedCamera.transform.forward;
            popup.text.color = popup.color;
            popup.text.SetText(playerDamage ? "-{0}" : coin ? "+{0}" : "{0}", playerDamage ? Mathf.Max(1, Mathf.RoundToInt(popup.amount)) : amount);
            root.gameObject.SetActive(true);
        }

        private void Update() => Tick(Time.deltaTime);

        private void Tick(float deltaTime)
        {
            if (popups == null) return;
            foreach (Popup popup in popups)
            {
                if (!popup.active) continue;
                popup.age += popup.playerDamage && Application.isPlaying
                    ? PlayerDamageFeedback.VisibleFrameDelta(Time.unscaledDeltaTime, Time.frameCount == popup.receivedFrame)
                    : deltaTime;
                float t = Mathf.Clamp01(popup.age / (popup.playerDamage ? 1.1f : Duration));
                if (t >= 1f)
                {
                    popup.active = false;
                    popup.canvas.gameObject.SetActive(false);
                    continue;
                }
                float remaining = 1f - t;
                popup.canvas.transform.position = popup.origin + Vector3.up * (.8f * (1f - remaining * remaining * remaining));
                Color color = popup.color;
                color.a = 1f - t * t;
                popup.text.color = color;
            }
        }
    }
}
