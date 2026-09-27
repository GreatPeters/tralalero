using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DisallowMultipleComponent]
    public sealed class BonusTalismanPresentation : MonoBehaviour
    {
        public const string ResourcePath = "BonusTalisman/Talisman";
        [SerializeField] private BonusTalismanVisual visual;
        private bool claimed;
        public BonusTalismanVisual Visual => visual;

        public static BonusTalismanPresentation Refresh(WallScript wall)
        {
            if (wall.wallType != WallType.BuffWall) return null;
            var presentation = wall.GetComponent<BonusTalismanPresentation>();
            if (presentation == null) presentation = wall.gameObject.AddComponent<BonusTalismanPresentation>();
            presentation.RefreshContent(wall);
            return presentation;
        }

        private void OnEnable() { claimed = false; }

        public void RefreshContent(WallScript wall)
        {
            var root = wall.GetComponentInParent<BonusWallLifetimeRoot>();
            var scope = root != null ? root.transform : wall.transform;
            if (visual == null || visual.emblemRenderer == null || visual.idleGlints == null || visual.idleGlints.Length == 0)
            {
                var prefab = Resources.Load<BonusTalismanVisual>(ResourcePath);
                if (prefab == null) return; // Leave the legacy presentation intact if authoring is incomplete.
                if(visual!=null){visual.gameObject.SetActive(false);visual.name="LegacyTalisman";}
                visual = Instantiate(prefab, scope);
                visual.name = "BonusTalisman";
            }
            var oldVisual = scope.Find("ChoiceAltarVisual");
            if (oldVisual != null) oldVisual.gameObject.SetActive(false);
            var oldGlow = scope.Find("CollectibleGlow");
            if (oldGlow != null) oldGlow.gameObject.SetActive(false);
            foreach (var cue in scope.GetComponentsInChildren<BonusRewardCue>(true)) cue.enabled = false;
            foreach (var vfx in scope.GetComponentsInChildren<BonusChoiceAltarVfx>(true)) vfx.enabled = false;
            foreach (var renderer in scope.GetComponentsInChildren<Renderer>(true))
                if (renderer.GetComponentInParent<BonusTalismanVisual>() == null && renderer.GetComponentInParent<BonusPadVisual>() == null) renderer.enabled = false;
            foreach (var canvas in scope.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;

            // Existing wall roots have nonuniform scales and rotated, 25x GFX children.
            // Counter-scale our art so every spawn path has the same world dimensions.
            visual.transform.localScale = Vector3.one;
            visual.transform.position = scope.position + Vector3.up * 2.2f;
            var camera = Camera.main;
            if (camera != null) visual.transform.rotation = camera.transform.rotation;
            Vector3 scale = visual.transform.lossyScale;
            visual.transform.localScale = new Vector3(1 / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                1 / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1 / Mathf.Max(.001f, Mathf.Abs(scale.z)));
            visual.paper.localScale = Vector3.one;
            visual.SetOpen(0);
            visual.gameObject.SetActive(true);
            visual.RememberRestPose();
            var sprite = Resources.Load<Sprite>("WallBonusIcons/" + BonusAltarRules.ResolveIconResourceName(wall.buffType));
            var padColor = PadColor(wall.buffType);
            visual.SetContent(sprite, LabelFor(wall), padColor);
            ApplyPad(scope, visual, padColor);
        }

        /// <summary>Floor pad + hologram presentation (bonus-wall concept 08). Keeps the talisman if the pad asset is missing.</summary>
        public static bool ApplyPad(Transform scope, BonusTalismanVisual visual, Color color, BonusPadVisual.Mode mode = BonusPadVisual.Mode.Normal)
        {
            var pad = BonusPadVisual.Ensure(scope);
            if (pad == null) return false;
            float width = BonusPadVisual.WidthForSpacing(NearestBonusSpacing(scope));
            pad.Configure(visual.transform, color, mode, width);
            float s = width / BonusPadVisual.DesignWidth;
            // The floating icon becomes the hologram: the player-drawn sprite, larger, above the pad.
            if (visual.paper != null) visual.paper.gameObject.SetActive(false);
            if (visual.readyHalo != null) visual.readyHalo.enabled = false;
            if (visual.emblemRenderer != null) visual.emblemRenderer.enabled = false;
            if (visual.icon != null && visual.icon.sprite != null)
            {
                visual.icon.enabled = true;
                float size = Mathf.Max(visual.icon.sprite.bounds.size.x, visual.icon.sprite.bounds.size.y);
                visual.icon.transform.localScale = Vector3.one * (1.4f * s / Mathf.Max(.01f, size));
                visual.icon.transform.localPosition = new Vector3(0, 0, -.15f);
            }
            if (visual.labelRoot != null)
            {
                visual.labelRoot.transform.localPosition = new Vector3(0, .95f * s + .2f, -.08f);
                visual.labelRoot.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.2f, s);
                foreach (var part in new[] { "Rim", "Badge" })
                {
                    var t = visual.labelRoot.transform.Find(part);
                    if (t != null && t.TryGetComponent(out Renderer r)) r.enabled = false;
                }
            }
            if (visual.label != null)
            {
                // Shared outlined copy of the caption material: TMP's outlineWidth setter would instance one per label.
                visual.label.fontSharedMaterial = OutlinedCaption(visual.label.fontSharedMaterial);
                visual.label.fontStyle |= TMPro.FontStyles.Bold;
            }
            visual.transform.position = scope.position + Vector3.up * (1.75f * s);
            visual.RememberRestPose();
            return true;
        }

        private static readonly System.Collections.Generic.Dictionary<Material, Material> outlined = new();
        private static Material OutlinedCaption(Material source)
        {
            if (source == null || outlined.ContainsValue(source)) return source;
            if (outlined.TryGetValue(source, out var copy) && copy != null) return copy;
            copy = new Material(source) { name = source.name + " (bonus pad outline)", hideFlags = HideFlags.DontSave };
            copy.SetFloat("_OutlineWidth", .28f);
            copy.SetColor("_OutlineColor", new Color32(12, 14, 24, 255));
            copy.EnableKeyword("OUTLINE_ON");
            outlined[source] = copy;
            return copy;
        }

        public static Color PadColor(BuffType type) => type switch
        {
            BuffType.att_normmal or BuffType.attPer_normal or BuffType.att_unique or BuffType.attPer_unique => new Color(.96f, .27f, .22f),
            BuffType.HealthBoost or BuffType.hp_normal or BuffType.hpPer_normal or BuffType.hp_unique or BuffType.hpPer_unique => new Color(.25f, .88f, .42f),
            BuffType.FireRateIncrease or BuffType.attackSpeed_normal or BuffType.attackSpeed_unique => new Color(1f, .8f, .14f),
            BuffType.missileDistance_normal or BuffType.missileDistance_unique or BuffType.missileAdd_unique => new Color(.26f, .56f, 1f),
            BuffType.tungtung_rare => new Color(1f, .56f, .2f),
            BuffType.boombar_rare or BuffType.ExtraHelp => new Color(.3f, .88f, .96f),
            _ => new Color(1f, .8f, .3f)
        };

        private static float NearestBonusSpacing(Transform scope)
        {
            float best = 99;
            foreach (var other in FindObjectsByType<BonusWallLifetimeRoot>(FindObjectsSortMode.None))
            {
                if (other.transform == scope) continue;
                Vector3 delta = other.transform.position - scope.position;
                float d = new Vector2(delta.x, delta.z).magnitude;
                if (d > .1f && d < best && Mathf.Abs(delta.y) < 2) best = d;
            }
            return best;
        }

        public bool PlayPickup(PlayerScript player)
        {
            if (claimed || visual == null || player == null) return false;
            claimed = true;
            BonusTalismanPickup.Spawn(visual, player);
            return true;
        }

        public static string LabelFor(WallScript wall)
        {
            string title = wall.CurrentBonusDisplayName;
            if (string.IsNullOrWhiteSpace(title)) title = wall.buffType switch
            {
                BuffType.HealthBoost or BuffType.hp_normal or BuffType.hp_unique or BuffType.hpPer_normal or BuffType.hpPer_unique => "체력",
                BuffType.FireRateIncrease or BuffType.attackSpeed_normal or BuffType.attackSpeed_unique => "연사",
                BuffType.missileDistance_normal or BuffType.missileDistance_unique => "사거리",
                BuffType.missileAdd_unique => "추가 탄환",
                BuffType.tungtung_rare => "퉁퉁퉁 지원",
                BuffType.boombar_rare => "봄바르디노 지원",
                BuffType.ExtraHelp => "지원군",
                _ => "공격력"
            };
            string value = wall.statValueTmp != null ? wall.statValueTmp.text : string.Empty;
            if(string.IsNullOrWhiteSpace(value))value=wall.CurrentBonusDisplayText;
            if (wall.buffType == BuffType.HealthBoost) value = "+" + wall.healthBoostAmt;
            if (wall.buffType == BuffType.FireRateIncrease) value = "×" + wall.fireRateIncMultipier.ToString("0.##");
            if (wall.buffType == BuffType.ExtraHelp) value = string.Empty;
            return (title + " " + value).Trim();
        }
    }
}
