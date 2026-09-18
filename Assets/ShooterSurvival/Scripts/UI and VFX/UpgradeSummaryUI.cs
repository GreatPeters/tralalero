using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

public sealed class UpgradeSummaryUI : MonoBehaviour
{
    [SerializeField] private PlayerScript player;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI healthText;
    private UpgradeStatManager subscribedManager;
    private float displayedAttack = float.NaN;
    private float displayedHealth = float.NaN;

    public void Configure(PlayerScript source, TextMeshProUGUI attack, TextMeshProUGUI health)
    {
        player = source;
        attackText = attack;
        healthText = health;
        RefreshNow();
    }

    private void OnEnable()
    {
        BindManager();
        RefreshNow();
    }

    private void OnDisable()
    {
        if (subscribedManager != null) subscribedManager.StatsChanged -= RefreshNow;
        subscribedManager = null;
    }

    private void BindManager()
    {
        if (subscribedManager == UpgradeStatManager.S) return;
        if (subscribedManager != null) subscribedManager.StatsChanged -= RefreshNow;
        subscribedManager = UpgradeStatManager.S;
        if (subscribedManager != null) subscribedManager.StatsChanged += RefreshNow;
    }

    private void LateUpdate()
    {
        BindManager();
        if (player != null && (!Mathf.Approximately(displayedAttack, player.currentDamage) ||
                               !Mathf.Approximately(displayedHealth, player.MaxHealth)))
            RefreshNow();
    }

    public void RefreshNow()
    {
        if (player == null) player = FindFirstObjectByType<PlayerScript>();
        if (player == null) return;
        displayedAttack = player.currentDamage;
        displayedHealth = player.MaxHealth;
        if (attackText != null) attackText.text = displayedAttack.ToString("#,0.##");
        if (healthText != null) healthText.text = displayedHealth.ToString("#,0.##");
    }
}
