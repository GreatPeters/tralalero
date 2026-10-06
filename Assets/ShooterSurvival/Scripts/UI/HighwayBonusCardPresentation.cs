using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

// Presentation for the two physical toll pickups. The controller still owns
// selection, touch validation, random rewards and the resulting transaction.
[DisallowMultipleComponent]
[DefaultExecutionOrder(250)]
public sealed class HighwayBonusCardPresentation : MonoBehaviour
{
    public HighwayChapter2Controller owner;
    public int index;
    public TMP_Text heading, title, effect, instruction;
    public Renderer laneHighlight;
    private bool valid, previousEnglish;
    private HighwayUniqueBonus previousBonus;

    private void OnEnable() => valid = false;
    private void LateUpdate()
    {
        if (owner == null || !owner.Running) return;
        // Keep these readable cards steady while the legacy card path pulses.
        transform.localScale = Vector3.one;
        var bonus = index == 0 ? owner.LeftBonus : owner.RightBonus;
        bool english = LocalizationSettings.SelectedLocale != null &&
            LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("en");
        if (!valid || bonus != previousBonus || english != previousEnglish)
        {
            heading.text = english ? (index == 0 ? "LEFT · HI-PASS" : "RIGHT · CASH")
                : (index == 0 ? "왼쪽 · 하이패스" : "오른쪽 · 현금");
            title.text = Name(bonus, english);
            effect.text = Effect(bonus, english);
            instruction.text = english ? "MOVE HERE TO COLLECT" : "이쪽으로 이동해 획득";
            // Sprite source images have different native sizes and pivots.
            // Fit the artwork to its reserved box without changing the pickup.
            var icon = owner.uniqueIcons[index];
            if (icon != null && icon.sprite != null)
            {
                var bounds = icon.sprite.bounds;
                float size = Mathf.Max(bounds.size.x, bounds.size.y);
                if (size > .0001f)
                {
                    float scale = 1.35f / size;
                    icon.transform.localScale = Vector3.one * scale;
                    icon.transform.localPosition = new Vector3(0, 1.25f, -.51f)
                        - new Vector3(bounds.center.x, bounds.center.y, 0) * scale;
                }
            }
            valid = true; previousBonus = bonus; previousEnglish = english;
        }
        if (laneHighlight != null && owner.Player != null && owner.Route != null)
        {
            var delta = owner.Player.transform.position - transform.position;
            // Preview the same side used by the physical pickup's selection rule.
            // The actual trigger still decides whether a bonus is collected.
            float lane = RouteFrame.Lane(owner.Player.transform.position, owner.Route.Distance);
            laneHighlight.enabled = owner.ExitChoice < 0 && delta.sqrMagnitude < 1600 &&
                index == (lane > 0 ? 1 : 0);
        }
    }

    public static string Name(HighwayUniqueBonus bonus, bool english)
        => english ? bonus switch {
            HighwayUniqueBonus.ChainMissile => "CHAIN MISSILE",
            HighwayUniqueBonus.GoldenShield => "GOLDEN SHIELD",
            HighwayUniqueBonus.Shatter => "SHATTER",
            _ => "COIN MAGNET"
        } : HighwayChapter2UI.BonusName(bonus);

    public static string Effect(HighwayUniqueBonus bonus, bool english)
    {
        float damage = HighwayChapter2Data.Value("chainDamage") * 100;
        switch (bonus)
        {
            case HighwayUniqueBonus.ChainMissile:
                int count = HighwayChapter2Data.Count("chainBounces");
                return english ? $"{count} nearby targets\n{damage:0}% damage" : $"주변 차량 {count}대\n피해 {damage:0}%";
            case HighwayUniqueBonus.GoldenShield:
                float threshold = HighwayChapter2Data.Value("shieldThreshold") * 100;
                return english ? $"BLOCK 1 BIG HIT\n≥{threshold:0}% HP · no holes" : $"큰 충격 1회 방어\nHP {threshold:0}%↑ · 구멍 제외";
            case HighwayUniqueBonus.Shatter:
                float radius = HighwayChapter2Data.Value("chainRadius") * HighwayChapter2Data.Value("shatterMultiplier");
                return english ? $"{radius:0.#}m splash radius\n{damage:0}% damage" : $"폭발 반경 {radius:0.#}m\n피해 {damage:0}%";
            default:
                float magnet = HighwayChapter2Data.Value("magnetRadius");
                return english ? $"{magnet:0.#}m coin pickup\nAUTO COLLECT" : $"반경 {magnet:0.#}m\n코인 자동 수집";
        }
    }
}
