using UnityEngine;
using UnityEngine.UI;

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

        private void Awake()
        {
            var root = new GameObject("Player damage vignette", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
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
        }

        public void Show(float damage, float maximum)
        {
            if (damage <= 0f) return;
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

        private void LateUpdate()
        {
            flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 1.65f);
            overlay.color = new Color(1, 1, 1, flash);
            if (fallingModel == null) return;
            fallTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fallTime / .85f);
            fallingModel.localRotation = modelRotation * Quaternion.Euler(110f * t, 0, -20f * t);
            fallingModel.localPosition = modelPosition + Vector3.down * (4f * t * t);
        }

        public void ResetFeedback()
        {
            flash = 0f;
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
