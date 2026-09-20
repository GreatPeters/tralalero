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
                if (renderer.GetComponentInParent<BonusTalismanVisual>() == null) renderer.enabled = false;
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
            visual.SetContent(sprite, LabelFor(wall), BonusChoiceAltarVfx.ResolveUiAccent(wall.buffType));
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
