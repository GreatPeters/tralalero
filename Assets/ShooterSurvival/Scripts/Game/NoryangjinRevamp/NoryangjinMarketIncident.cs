using System;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Authored one-at-a-time market incidents. The installer supplies distinct props and paths.
public sealed class NoryangjinMarketIncident : NoryangjinRevampEvent
{
    public enum Kind { CartCrossing, BoxToss, ReversingTruck, ContainerDrop, EscapingCart, Seagulls }
    public Kind kind;
    public Transform[] actors = Array.Empty<Transform>();
    public NoryangjinBreakable[] destructibles = Array.Empty<NoryangjinBreakable>();
    public Transform warning, puddle;
    public float halfWidth = 3, duration = 8, warningSeconds = 2, damageShare = .1f;
    public float lane = -1.5f;
    public AudioClip warningVoice;
    public GameObject impactEffect;
    public float dropSeconds=.8f;
    private Vector3 warningRestScale;
    private bool warningCaptured;
    private MaterialPropertyBlock shadowTint;
    private Vector3[] rest;
    private float clock;
    private bool hit, burst, slipApplied, subscribed;

    public override void ResetForRun()
    {
        base.ResetForRun(); clock = 0; hit = burst = slipApplied = false;
        if (rest == null)
        {
            rest = new Vector3[actors.Length];
            for (int i = 0; i < actors.Length; i++) if (actors[i] != null) rest[i] = actors[i].localPosition;
        }
        for (int i = 0; i < actors.Length; i++) if (actors[i] != null)
        {
            actors[i].localPosition = rest[i]; actors[i].gameObject.SetActive(false);
        }
        if (warning != null) {if(!warningCaptured){warningRestScale=warning.localScale;warningCaptured=true;}warning.localScale=warningRestScale;warning.gameObject.SetActive(false);}
        if (puddle != null) puddle.gameObject.SetActive(false);
        if (!subscribed)
        {
            foreach (var b in destructibles) if (b != null) b.Broke += OnBroken;
            subscribed = true;
        }
    }
    protected override void OnTriggered()
    {
        clock = 0;
        if (warning != null) warning.gameObject.SetActive(true);
        foreach (var t in actors) if (t != null) t.gameObject.SetActive(true);
        foreach (var b in destructibles) if (b != null) b.Arm();
        GameAudioService.Play(GameSound.Warning);
        Director.Speak(warningVoice);
    }
    private void OnBroken(NoryangjinBreakable b)
    {
        if (kind != Kind.ReversingTruck && kind != Kind.EscapingCart) return;
        if (puddle != null) puddle.gameObject.SetActive(true);
        Director.Burst(Director.splashEffect, b.transform.position + Vector3.up, 1.5f);
        Director.Hud?.Publish(kind == Kind.EscapingCart ? "수조 카트 포획! 코인이 쏟아진다" : "활어가 쏟아졌다! 미끄럼 주의", 2);
        Director.Timeline.Add($"{kind} broken at {Director.Elapsed:F1}");
    }
    protected override void OnTick(float dt)
    {
        clock += dt;
        if(kind==Kind.ContainerDrop&&warning!=null&&warning.gameObject.activeSelf)
        {
            float growth=Mathf.Clamp01(clock/Mathf.Max(.1f,warningSeconds));
            warning.localScale=Vector3.Scale(warningRestScale,new Vector3(Mathf.Lerp(.6f,1,growth),Mathf.Lerp(.6f,1,growth),1));
            shadowTint??=new MaterialPropertyBlock();shadowTint.SetColor("_BaseColor",new Color(.005f,.008f,.012f,Mathf.Lerp(.45f,.9f,growth)));
            warning.GetComponent<Renderer>()?.SetPropertyBlock(shadowTint);
        }
        float playerAlong = -Ahead(Player.transform.position);
        float playerLane = Lateral(Player.transform.position);
        if (puddle != null && puddle.gameObject.activeSelf && !slipApplied
            && OnStretch(Player.transform.position) && Mathf.Abs(playerAlong) < 7 && Mathf.Abs(playerLane - lane) < 1.5f)
        {
            slipApplied = true;
            var effect = Player.GetComponent<NoryangjinWetSteering>() ?? Player.gameObject.AddComponent<NoryangjinWetSteering>();
            effect.Apply(Player, 1.5f);
            Director.Timeline.Add($"wet steering at {Director.Elapsed:F1}");
        }
        if (clock > duration)
        {
            foreach (var actor in actors) if (actor != null) actor.gameObject.SetActive(false);
            if (warning != null) warning.gameObject.SetActive(false);
            return;
        }
        if (clock < warningSeconds) return;
        if (warning != null && kind != Kind.ContainerDrop) warning.gameObject.SetActive(false);
        float t = clock - warningSeconds;
        for (int i = 0; i < actors.Length; i++)
        {
            var actor = actors[i]; if (actor == null) continue;
            var b = i < destructibles.Length ? destructibles[i] : null;
            if (b != null && !b.Alive) continue;
            Vector3 pos = rest[i];
            switch (kind)
            {
                case Kind.CartCrossing:
                    pos.x = Mathf.Lerp(-halfWidth - 4, halfWidth + 4, Mathf.Clamp01((t - i * 1.5f) / 3.4f));
                    break;
                case Kind.BoxToss:
                    float u = Mathf.Repeat(t - i * 1.1f + 10, 3.4f) / 3.4f;
                    pos.x = Mathf.Lerp(-halfWidth - 3, halfWidth + 3, u);
                    pos.y = .7f + Mathf.Sin(u * Mathf.PI) * 3.2f;
                    break;
                case Kind.ReversingTruck:
                    pos.x = Mathf.Lerp(lane < 0 ? -halfWidth - 5 : halfWidth + 5, lane, Mathf.Clamp01(t / 2.4f));
                    break;
                case Kind.ContainerDrop:
                    pos.y = Mathf.Lerp(11, 0, Mathf.Pow(Mathf.Clamp01(t / Mathf.Max(.05f,dropSeconds)),2));
                    if (t >= dropSeconds && !burst)
                    {
                        burst = true; Director.Burst(impactEffect!=null?impactEffect:Director.bigBreakEffect, At(0, lane), .6f);
                        Director.Timeline.Add($"container impact at {Director.Elapsed:F1}");
                        if (warning != null) warning.gameObject.SetActive(false);
                    }
                    break;
                case Kind.EscapingCart:
                    pos.z = Mathf.Min(34, t * 2.8f);
                    break;
                case Kind.Seagulls:
                    pos.x = Mathf.Lerp(-halfWidth - 4, halfWidth + 4, Mathf.Clamp01(t / 2.6f));
                    pos.y = 1.4f + Mathf.Sin(t * 8 + i) * .35f;
                    break;
            }
            actor.localPosition = pos;
            if (b != null) b.restLocalWithMotion = pos;
            if (hit || kind == Kind.EscapingCart || Mathf.Abs(pos.y) > 2.4f) continue;
            if (Mathf.Abs(playerAlong - pos.z) < 1.6f && Mathf.Abs(playerLane - pos.x) < 1.4f && OnStretch(Player.transform.position))
            {
                hit = true; Player.ApplyDamage(Player.MaxHealth * damageShare, PlayerDamageCause.Roadblock);
                Director.Timeline.Add($"{kind} contact at {Director.Elapsed:F1}");
            }
        }
    }
    private void OnDestroy()
    {
        foreach (var b in destructibles) if (b != null) b.Broke -= OnBroken;
    }
}
