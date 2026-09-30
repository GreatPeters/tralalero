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
        passed = closed = false; remaining = total = 0;
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
        if (announceVoice != null) Director.Speak(announceVoice, true);
        float perWall = 0;
        foreach (var w in walls) if (w != null) { w.Arm(); perWall += w.hitsToBreak; }
        float fire = Director.PlayerFireRate();
        float speed=Director.Branch!=null&&Director.Branch.Driving?Director.Branch.travelSpeed:Player.ForwardMoveSpeed;
        float run = Mathf.Max(0, Ahead(Player.transform.position)) / Mathf.Max(1,speed);
        total = remaining = perWall / Mathf.Max(.5f, fire) + run + slackSeconds;
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
