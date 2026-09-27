using TMPro;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    /// <summary>
    /// Random-bonus presentation in the floor-pad style: rainbow pad, a big "?" hologram and the
    /// player-drawn bonus icons cycling behind it like a slot reel. Purely visual; the owner decides
    /// the outcome and deactivates the root when claimed.
    /// </summary>
    public sealed class BonusPadRandomDisplay : MonoBehaviour
    {
        public float[] padOffsets = { -2.4f, 2.4f };
        public float padWidth = 2.4f;
        private Sprite[] reel;
        private SpriteRenderer[] cycling;
        private TMP_Text[] marks;
        private Transform[] holders;
        private Camera viewCamera;
        private float clock;

        private void Start()
        {
            var talisman = Resources.Load<BonusTalismanVisual>(BonusTalismanPresentation.ResourcePath);
            var list = new System.Collections.Generic.List<Sprite>();
            if (talisman != null && talisman.enhancementIcons != null)
                foreach (var e in talisman.enhancementIcons.Entries)
                    if (e != null && e.sprite != null && e.key != null && !e.key.StartsWith("src_") && !e.key.StartsWith("img_")) list.Add(e.sprite);
            reel = list.ToArray();
            var font = Resources.Load<TMP_FontAsset>("UI/GmarketHarbor SDF");
            holders = new Transform[padOffsets.Length];
            cycling = new SpriteRenderer[padOffsets.Length];
            marks = new TMP_Text[padOffsets.Length];
            for (int i = 0; i < padOffsets.Length; i++)
            {
                var anchor = new GameObject("Random pad anchor " + i).transform;
                anchor.SetParent(transform, false);
                anchor.localPosition = new Vector3(padOffsets[i], 0, 0);
                var pad = BonusPadVisual.Ensure(anchor);
                if (pad != null) pad.Configure(null, Color.white, BonusPadVisual.Mode.Random, padWidth);
                var holder = new GameObject("Random hologram " + i).transform;
                holder.SetParent(anchor, false);
                holders[i] = holder;
                var icon = new GameObject("Cycling icon").AddComponent<SpriteRenderer>();
                icon.transform.SetParent(holder, false);
                icon.color = new Color(1, 1, 1, .55f);
                cycling[i] = icon;
                var text = new GameObject("Question").AddComponent<TextMeshPro>();
                text.transform.SetParent(holder, false);
                text.font = font;
                text.text = "?";
                text.fontSize = 20;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(2, 2);
                text.outlineWidth = .3f;
                text.outlineColor = new Color32(20, 16, 40, 255);
                marks[i] = text;
            }
        }

        private void LateUpdate()
        {
            if (holders == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            clock += Time.deltaTime;
            float s = padWidth / BonusPadVisual.DesignWidth;
            for (int i = 0; i < holders.Length; i++)
            {
                var h = holders[i];
                h.position = h.parent.position + Vector3.up * (1.8f * s + Mathf.Sin(clock * 2.3f + i) * .06f);
                if (viewCamera != null) h.rotation = viewCamera.transform.rotation;
                h.localScale = Vector3.one;
                Vector3 lossy = h.lossyScale;
                h.localScale = new Vector3(1 / Mathf.Max(.001f, Mathf.Abs(lossy.x)), 1 / Mathf.Max(.001f, Mathf.Abs(lossy.y)), 1 / Mathf.Max(.001f, Mathf.Abs(lossy.z)));
                if (reel != null && reel.Length > 0)
                {
                    float spin = clock * 7f + i * 2.3f;
                    var sprite = reel[(int)spin % reel.Length];
                    cycling[i].sprite = sprite;
                    float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                    cycling[i].transform.localScale = Vector3.one * (1.45f * s / Mathf.Max(.01f, size));
                    cycling[i].transform.localPosition = new Vector3(0, .35f - Mathf.Repeat(spin, 1) * .7f, .05f);
                }
                marks[i].color = Color.HSVToRGB(Mathf.Repeat(clock * .18f + i * .3f, 1), .55f, 1);
            }
        }
    }
}
