using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Session-only controls. Never write debug balance to assets, PlayerPrefs or Data.xlsx.
[InitializeOnLoad]
internal static class NoryangjinMapToolTestOverrides
{
    private const string PowerKey = "NoryangjinMapTool.TestPower9999";
    private const string MoveKey = "NoryangjinMapTool.TestFastLateral";
    private static readonly FieldInfo MaxHealthField = typeof(PlayerScript).GetField("maxHealthWithUpgrades", BindingFlags.NonPublic | BindingFlags.Instance);
    private static PlayerScript tracked;
    private static float originalDivider;
    private static bool powerApplied;

    static NoryangjinMapToolTestOverrides()
    {
        PlayerScript.EditorRunPrepared -= OnRunPrepared;
        PlayerScript.EditorRunPrepared += OnRunPrepared;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Restore();
            if (state == PlayModeStateChange.EnteredEditMode) { tracked = null; powerApplied = false; }
        };
    }

    internal static bool PowerEnabled => SessionState.GetBool(PowerKey, false);
    internal static bool FastLateralEnabled => SessionState.GetBool(MoveKey, false);
    internal static void Select(bool power, bool fastLateral)
    {
        SessionState.SetBool(PowerKey, power); SessionState.SetBool(MoveKey, fastLateral);
        if (NoryangjinMapToolTestSpeed.CanApply(EditorApplication.isPlaying, SceneManager.GetActiveScene().path))
            Apply(Object.FindFirstObjectByType<PlayerScript>(), power, fastLateral);
    }
    private static void OnRunPrepared(PlayerScript player)
    {
        if (NoryangjinMapToolTestSpeed.CanApply(EditorApplication.isPlaying, player.gameObject.scene.path))
            Apply(player, PowerEnabled, FastLateralEnabled);
    }
    internal static void Apply(PlayerScript player, bool power, bool fastLateral)
    {
        if (player == null) return;
        if (tracked != player) { tracked = player; originalDivider = player.moveSensitivity_Devision; powerApplied = false; }
        if (power)
        {
            MaxHealthField.SetValue(player, 9999f);
            player.currentHealth = 9999;
            player.originalDamage = player.currentDamage = 9999;
            foreach (var weapon in player.GetComponentsInChildren<WeaponScript>(true)) weapon.damage = 9999;
            powerApplied = true;
        }
        else if (powerApplied) RestorePower();
        player.moveSensitivity_Devision = fastLateral ? originalDivider * .25f : originalDivider;
        player.UpdateHealth();
    }
    private static void RestorePower()
    {
        if (tracked == null) return;
        tracked.ClearRunHealthBonuses();
        tracked.ReloadCharacterDefaults();
        float damage = UpgradeStatManager.S != null
            ? UpgradeStatManager.S.ApplyToBase(UpgradeStatManager.UpgradeType.ATT, tracked.DefaultAttackDamage)
            : tracked.DefaultAttackDamage;
        tracked.currentDamage = damage;
        foreach (var weapon in tracked.GetComponentsInChildren<WeaponScript>(true)) weapon.damage = damage;
        powerApplied = false;
    }
    internal static void Restore()
    {
        if (tracked != null)
        {
            if (powerApplied) RestorePower();
            tracked.moveSensitivity_Devision = originalDivider;
        }
        tracked = null; powerApplied = false;
    }
}
