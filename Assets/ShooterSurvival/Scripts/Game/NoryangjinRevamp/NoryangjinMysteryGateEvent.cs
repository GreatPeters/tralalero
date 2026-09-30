using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

public sealed class NoryangjinMysteryGateEvent : NoryangjinRevampEvent
{
    public float largeChance = .2f, normalChance = .55f, coinChance = .2f;
    public float largePercent = 15, normalPercent = 8, missShare = .05f;
    public int coins = 60;
    public GameObject display;
    private bool claimed;

    public override void ResetForRun() { base.ResetForRun(); claimed = false; if (display) display.SetActive(true); }
    protected override void OnTriggered() { }

    protected override void OnTick(float dt)
    {
        if (claimed || Ahead(Player.transform.position) > 0) return;
        claimed = true;
        if (display) display.SetActive(false);
        float roll = UnityEngine.Random.value;
        string message;
        if (roll < largeChance + normalChance)
        {
            float percent = roll < largeChance ? largePercent : normalPercent;
            int stat = UnityEngine.Random.Range(0, 3);
            if (stat == 0) { Player.ApplyRunAttackPercent(percent); message = $"공격력 +{percent}%"; }
            else if (stat == 1) { Player.ApplyRunHealthBonus(percent, true); message = $"체력 +{percent}%"; }
            else { BulletScript.AddMissileDurationPercent(percent); message = $"미사일 사거리 +{percent}%"; }
        }
        else if (roll < largeChance + normalChance + coinChance)
        {
            int amount = CoinDropUtility.ApplyCoinBonus(coins);
            MoneyScript.S?.GetCoin(amount); DamagePopupFX.ShowCoin(Player.transform.position + Vector3.up * 2, amount);
            message = $"코인 +{amount}";
        }
        else { Player.ApplyDamage(Player.MaxHealth * missShare, PlayerDamageCause.NegativeBonus); message = "꽝! 체력 -5%"; }
        Director.Hud?.Publish("랜덤 보너스: " + message, 2.2f);
        Director.Timeline.Add($"mystery {message} at {Director.Elapsed:F1}");
    }
}
