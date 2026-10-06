using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

public sealed class NoryangjinShutterEvent : NoryangjinRevampEvent
{
    public NoryangjinBreakable[] walls = Array.Empty<NoryangjinBreakable>();
    public NoryangjinBreakable shutter;
    public Transform shutterPanel;
    public NoryangjinRollerShutterVisual roller;
    public float openHeight = 4.2f;
    public float passClearance = 3.6f, closeSeconds = .35f;
    public float slackSeconds = 4f;
    public float closedHitsMultiplier = 5f;
    public string timerCaption = "셔터까지";
    [Tooltip("Essential proposal 11: fixed countdown (was a derived ~50 s). Game time, so X2 halves real time.")]
    public float countdownSeconds = 20f;
    // User 2026-10-07: "다 부수려면 공격력 업그레이드를 해야 해. 어느 정도 업그레이드를 해야 돌파 가능".
    // So box HP is FIXED, not scaled by the shark's attack (the old Arm() scaling made upgrades useless here).
    [Tooltip("Fixed HP per box wall. All seven walls stand in line, so every one must break inside the countdown.")]
    public float wallHealth = 95f;
    private float total, remaining;
    private bool passed, closed;
    private Vector3 panelRest;
    private bool panelCaptured;
    public static float PanelLift(float remaining,float total,float openHeight,float clearance)
        => remaining<=0?0:Mathf.Lerp(Mathf.Min(clearance,openHeight),openHeight,Mathf.Clamp01(remaining/Mathf.Max(.1f,total)));
    public float CurrentOpening=>roller!=null?roller.Opening:shutterPanel!=null?Vector3.Distance(shutterPanel.localPosition,panelRest):0;
    private void SetOpening(float lift)
    {if(roller!=null)roller.SetOpening(lift);else if(shutterPanel!=null)shutterPanel.localPosition=panelRest+Vector3.up*lift;}

    public override void ResetForRun()
    {
        base.ResetForRun();
        passed = closed = counting = false; remaining = total = 0;
        if (shutterPanel != null)
        {
            if (!panelCaptured) { panelRest = shutterPanel.localPosition; panelCaptured = true; }
            SetOpening(openHeight);
        }
    }

    [Tooltip("Spoken with the banner when the countdown starts (\"문이 곧 닫힙니다\").")]
    public AudioClip announceVoice;

    protected override void OnTriggered()
    {
        GameAudioService.Play(GameSound.Warning);
        if (announceVoice != null) Director.Speak(announceVoice, true);
        foreach (var w in walls) if (w != null) w.ArmFixed(wallHealth);
        total = remaining = CountdownFor(countdownSeconds);
        Director.Timeline.Add($"shutter countdown {total:F0}s walls {wallHealth:F0}HP attack {Player.ResolvedAttackDamage:F0}");
    }

    public static float CountdownFor(float configured) => configured > 0 ? configured : 20f;

    [Tooltip("The countdown starts this far before the first (farthest) box wall.")]
    public float countdownLeadBeforeWalls = 4f;
    private bool counting;
    public float CountdownStartAhead()
    {
        // Ahead(p) = how far the shutter lies ahead of p, so for a wall it is that wall's distance before the shutter.
        float farthest = 0f;
        foreach (var w in walls) if (w != null) farthest = Mathf.Max(farthest, Ahead(w.transform.position));
        return farthest > 0f ? Mathf.Min(triggerAhead, farthest + countdownLeadBeforeWalls) : triggerAhead;
    }

    protected override void OnTick(float dt)
    {
        if (passed) return;
        if (Ahead(Player.transform.position) < -1)
        {
            passed = true; Director.Hud?.HideTimer();
            Director.Timeline.Add($"shutter passed at {Director.Elapsed:F1}");
            Director.Hud?.Publish(closed ? "셔터를 부수고 탈출!" : "셔터 통과! 탈출 성공", 2f);
            return;
        }
        if (closed)
        {
            SetOpening(Mathf.MoveTowards(CurrentOpening,0,passClearance/Mathf.Max(.05f,closeSeconds)*dt));
            if(shutter!=null&&!shutter.Armed&&CurrentOpening<.01f)
            {shutter.Arm(closedHitsMultiplier);Director.Timeline.Add($"shutter closed at {Director.Elapsed:F1}");}
            return;
        }
        // The 20 s run from the box-wall section, not from the 98 m warning: at market speed (~5.6 m/s
        // measured) the warning point alone is ~18 s from the shutter, which made 20 s unwinnable at any attack.
        if (!counting)
        {
            counting = Ahead(Player.transform.position) <= CountdownStartAhead();
            Director.Hud?.ShowTimer(timerCaption, remaining, total);
            if (!counting) return;
            Director.Timeline.Add($"shutter countdown started at {Director.Elapsed:F1}");
        }
        remaining -= dt;
        Director.Hud?.ShowTimer(timerCaption, remaining, total);
        if (shutterPanel != null&&remaining>0)
            SetOpening(PanelLift(remaining,total,openHeight,passClearance));
        if (remaining > 0) return;
        closed = true;
        Director.Hud?.HideTimer();
        Director.Hud?.Publish("셔터가 닫혔다! 부숴야 나갈 수 있다", 2.5f);
        Director.Timeline.Add($"shutter closing at {Director.Elapsed:F1}");
    }
}
