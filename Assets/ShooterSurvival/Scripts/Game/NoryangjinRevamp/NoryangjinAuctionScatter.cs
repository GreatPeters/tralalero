using System;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// The live-fish auction crowd stands in the middle of the aisle and scatters to the sides when the
// shark bursts in (user 2026-09-29: people belong in the middle). Noncombat: no colliders, no damage.
[DefaultExecutionOrder(510)]
public sealed class NoryangjinAuctionScatter : MonoBehaviour
{
    [Serializable] public sealed class Member
    {
        public Transform body;
        public Animator animator;
        public Vector3 home, flee;
        public Quaternion homeRotation;
        [Tooltip("Extra metres of warning; staggers who reacts first.")] public float nerve;
        [NonSerialized] public float t = -1;
    }
    public Member[] members = Array.Empty<Member>();
    [Tooltip("A member starts running when the shark is this far before them along the aisle.")]
    public float triggerAhead = 15;
    public float runSeconds = .8f;
    public AudioClip shout;
    public string banner = "상어 난입! 경매장이 난리 났다!";
    private bool shouted, dirty;

    public bool IsFleeing(Animator animator)
    {
        foreach (var m in members) if (m.animator == animator) return m.t >= 0;
        return false;
    }

    private void Update()
    {
        var d = NoryangjinRevampDirector.Active;
        if (d == null || d.Player == null || !d.Running || d.Elapsed < .5f) { if (dirty) ResetCrowd(); return; }
        float dt = Time.deltaTime * TimeManager.timeFactor;
        var offset = d.Player.transform.position - transform.position;
        float playerAlong = Vector3.Dot(offset, transform.forward);
        // Only a shark actually coming down this aisle scares the crowd (not one passing elsewhere).
        bool inAisle = Mathf.Abs(Vector3.Dot(offset, transform.right)) < 9 && Mathf.Abs(offset.y) < 4
            && Vector3.Dot(Vector3.ProjectOnPlane(d.Player.transform.forward, Vector3.up).normalized, transform.forward) > .5f;
        foreach (var m in members)
        {
            if (m.body == null) continue;
            if (m.t < 0)
            {
                if (!inAisle) continue;
                float along = Vector3.Dot(m.home - transform.position, transform.forward);
                if (along - playerAlong > triggerAhead + m.nerve || along - playerAlong < -30) continue;
                m.t = 0; dirty = true;
                Play(m.animator, "run");
                if (!shouted)
                {
                    shouted = true;
                    if (!string.IsNullOrEmpty(banner)) d.Hud?.Publish(banner, 2f);
                    if (shout != null) d.Speak(shout, true);
                    d.Timeline.Add($"auction crowd scattered at {d.Elapsed:F1}");
                }
            }
            if (m.t >= runSeconds)
            {
                // Settled at the side: keep turning to watch the shark go past.
                var look = Vector3.ProjectOnPlane(d.Player.transform.position - m.body.position, Vector3.up);
                if (look.sqrMagnitude > .01f) m.body.rotation = Quaternion.Slerp(m.body.rotation, Quaternion.LookRotation(look.normalized), 6 * dt);
                continue;
            }
            m.t += dt;
            float u = Mathf.SmoothStep(0, 1, Mathf.Clamp01(m.t / runSeconds));
            m.body.position = Vector3.Lerp(m.home, m.flee, u);
            var away = m.flee - m.home; away.y = 0;
            if (away.sqrMagnitude > .01f) m.body.rotation = Quaternion.LookRotation(away.normalized);
            if (m.t >= runSeconds) Play(m.animator, "idle");
        }
    }

    private static void Play(Animator animator, string state)
    {
        if (animator != null && animator.HasState(0, Animator.StringToHash(state))) animator.CrossFadeInFixedTime(state, .12f, 0, 0);
    }

    private void ResetCrowd()
    {
        foreach (var m in members)
        {
            m.t = -1;
            if (m.body != null) m.body.SetPositionAndRotation(m.home, m.homeRotation);
            Play(m.animator, "idle");
        }
        shouted = false; dirty = false;
    }
}
