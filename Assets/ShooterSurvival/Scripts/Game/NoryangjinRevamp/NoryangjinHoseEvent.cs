using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

public sealed class NoryangjinHoseEvent : NoryangjinRevampEvent
{
    [Serializable] public sealed class Jet
    {
        public float along;
        [Tooltip("-1 sprays from the left edge, +1 from the right edge.")] public int side = -1;
        [Tooltip("How far across the aisle the water reaches (m from the spraying edge).")] public float reach = 4.2f;
        public float period = 2.6f, onSeconds = 1.4f, phase;
        public Transform visual;
        public Transform warning;
        public NoryangjinWetPatch wetPatch;
    }
    public Jet[] jets = Array.Empty<Jet>();
    public float halfWidth = 3.4f, bandHalfDepth = .9f;
    public float spinSeconds = 2f, drainShare = .1f;
    public float warningSeconds = .45f;
    private float clock;
    private bool finished;
    private MaterialPropertyBlock warningTint;
    public static bool WarningPhase(float clock,float phase,float period,float onSeconds,float warningSeconds)
        => period>0&&Mathf.Repeat(clock+phase,period)>=Mathf.Max(onSeconds,period-Mathf.Max(0,warningSeconds));

    public override void ResetForRun() { base.ResetForRun(); clock = 0; finished = false; foreach (var j in jets) {if (j.visual != null) j.visual.gameObject.SetActive(false);if(j.warning!=null)j.warning.gameObject.SetActive(false);j.wetPatch?.ResetPatch();} }
    protected override void OnTriggered() { clock = 0; }
    public static bool IsInJet(float along, float lateral, float halfWidth, float bandHalfDepth, int side, float reach)
        => Mathf.Abs(along) <= bandHalfDepth && Mathf.Abs(lateral) <= halfWidth + .5f
            && (side < 0 ? lateral + halfWidth : halfWidth - lateral) <= reach;

    protected override void OnTick(float dt)
    {
        clock += dt;
        Vector3 p = Player.transform.position;
        bool past = Ahead(p) < -40;
        if (past && OnStretch(p)) finished = true;
        foreach (var jet in jets)
        {
            bool on = !finished && !past && Mathf.Repeat(clock + jet.phase, jet.period) < jet.onSeconds;
            if (jet.visual != null && jet.visual.gameObject.activeSelf != on) jet.visual.gameObject.SetActive(on);
            jet.wetPatch?.SetSpraying(on);
            if(jet.warning!=null)
            {
                bool warn=!finished&&!past&&WarningPhase(clock,jet.phase,jet.period,jet.onSeconds,warningSeconds);
                jet.warning.gameObject.SetActive(on||warn);
                warningTint??=new MaterialPropertyBlock();
                warningTint.SetColor("_BaseColor",on?new Color(.1f,.65f,1,.32f):new Color(1,.65f,.08f,.35f));
                jet.warning.GetComponent<Renderer>()?.SetPropertyBlock(warningTint);
            }
            if (!on || !OnStretch(p) || Mathf.Abs(Lateral(p)) > halfWidth + .5f) continue;
            float along = -Ahead(p) - jet.along;
            float lateral = Lateral(p);
            if (IsInJet(along, lateral, halfWidth, bandHalfDepth, jet.side, jet.reach)) Director.StartSpin(spinSeconds, drainShare, PlayerDamageCause.WaterJet);
        }
    }
}
