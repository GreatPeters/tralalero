using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndianOceanAssets.ShooterSurvival
{
    public sealed class PlayerStatusHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI healthValueText;
        [SerializeField] private Image healthFill;
        [SerializeField] private TextMeshProUGUI attackValueText;

        public bool IsConfigured =>
            healthValueText != null && healthFill != null && attackValueText != null;

        public void Configure(
            TextMeshProUGUI configuredHealthValueText,
            Image configuredHealthFill,
            TextMeshProUGUI configuredAttackValueText)
        {
            healthValueText = configuredHealthValueText;
            healthFill = configuredHealthFill;
            attackValueText = configuredAttackValueText;
        }

        public void SetHealth(float currentHealth, float maxHealth)
        {
            float safeMaxHealth = Mathf.Max(0f, maxHealth);
            float safeCurrentHealth = safeMaxHealth > 0f
                ? Mathf.Clamp(currentHealth, 0f, safeMaxHealth)
                : 0f;

            if (healthValueText != null)
            {
                healthValueText.text =
                    $"{Mathf.RoundToInt(safeCurrentHealth)} / {Mathf.RoundToInt(safeMaxHealth)}";
            }

            if (healthFill != null)
            {
                healthFill.fillAmount = safeMaxHealth > 0f
                    ? safeCurrentHealth / safeMaxHealth
                    : 0f;
            }
        }

        public void SetAttack(float currentDamage)
        {
            if (attackValueText != null)
                attackValueText.text = Mathf.RoundToInt(Mathf.Max(0f, currentDamage)).ToString();
        }
    }
}
