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
            moving = false;
            float hit = turret.Health;
            Player.ApplyDamage(hit, PlayerDamageCause.Traffic);
            turret.Damage(turret.Health);
            Director.Hud?.Publish("터렛트와 정면 충돌!", 2f);
            return;
        }
        if (Vector3.Dot(turret.transform.position - p, transform.forward) <= stopDistance) { moving = false; Director.Hud?.Publish("터렛트가 길을 막았다! 부숴라!", 2f); return; }
        turret.transform.position -= transform.forward * chargeSpeed * dt;
        turret.restLocalWithMotion = turret.transform.localPosition;
    }
}
