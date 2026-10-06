using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

public sealed class NoryangjinTurretEvent : NoryangjinRevampEvent
{
    public NoryangjinBreakable turret;
    public float chargeSpeed = 7f, stopDistance = 7f;
    public float bossHits = 40;
    public Animator driver;
    private bool moving;
    private Vector3 rest;
    private bool restCaptured;

    // P' = max(0, P - E), E' = max(0, E - P). P = E leaves both at 0 (user left the tie open;
    // the proposal's recommended default). A dead player ends the run, so no clear reward follows.
    public static (float player, float boss) ExchangeHp(float player, float boss)
    {
        player = Mathf.Max(0, player); boss = Mathf.Max(0, boss);
        return (Mathf.Max(0, player - boss), Mathf.Max(0, boss - player));
    }

    public override void ResetForRun()
    {
        base.ResetForRun(); moving = false;
        if (turret != null)
        {
            if (!restCaptured) { rest = turret.transform.localPosition; restCaptured = true; }
            turret.transform.localPosition = rest; turret.restLocalWithMotion = rest;
        }
    }

    protected override void OnTriggered()
    {
        if (turret == null) return;
        turret.hitsToBreak = bossHits; turret.Arm();
        moving = true;
        if (driver != null && driver.HasState(0, Animator.StringToHash(ForwardEnemyAnimationContract.AttackLoop))) driver.Play(ForwardEnemyAnimationContract.AttackLoop);
    }

    protected override void OnTick(float dt)
    {
        if (turret == null || !turret.Alive || !moving) return;
        Vector3 p = Player.transform.position;
        float gap = Vector3.Dot(turret.transform.position - p, transform.forward) - turret.halfDepth;
        if (gap <= .6f)
        {
            moving = false; // one collision per charge: the exchange below runs once
            // Essential proposal 9: both sides lose the other's remaining HP from one snapshot.
            float playerBefore = Player.currentHealth, bossBefore = turret.Health;
            var (playerAfter, bossAfter) = ExchangeHp(playerBefore, bossBefore);
            Player.ApplyDamage(playerBefore - playerAfter, PlayerDamageCause.Traffic);
            turret.Damage(bossBefore - bossAfter);
            Director.Hud?.Publish(bossAfter <= 0 && playerAfter > 0 ? "정면 충돌! 오토바이를 부쉈다" : "오토바이와 정면 충돌!", 2f);
            Director.Timeline.Add($"turret collision P{playerBefore:F0} E{bossBefore:F0} -> P{playerAfter:F0} E{bossAfter:F0}");
            return;
        }
        if (Vector3.Dot(turret.transform.position - p, transform.forward) <= stopDistance) { moving = false; Director.Hud?.Publish("운반차가 길을 막았습니다. 앞쪽을 공격하세요!", 2f); return; }
        turret.transform.position -= transform.forward * chargeSpeed * dt;
        turret.restLocalWithMotion = turret.transform.localPosition;
    }
}
