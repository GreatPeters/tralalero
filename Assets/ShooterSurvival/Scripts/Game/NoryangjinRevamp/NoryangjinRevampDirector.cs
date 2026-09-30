using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Runs the Noryangjin revamp events (docs/design: 노량진 맵 개편 기획) in the safe-copy scene
// Noryangjin_MapTool_Mode_SR18_Revamp. PlayerScript asks it for forward stops (box walls, hose spin),
// lateral locks and the one-hit shield; every event is a child NoryangjinRevampEvent.
[DefaultExecutionOrder(160)]
[DisallowMultipleComponent]
public sealed class NoryangjinRevampDirector : MonoBehaviour
{
    [Header("Shield / heal rewards")]
    [Tooltip("Hits smaller than this share of max HP (hose drain ticks) never consume the shield.")]
    public float shieldMinimumShare = .05f;
    public Transform shieldAura;

    [Header("Box walls")]
    [Tooltip("The shark stops this far in front of a blocking wall.")]
    public float stopGap = 2.6f;
    public float playerHalfWidth = .45f;

    [Header("Time of day (0 = start, 1 = expected finish)")]
    public Light sun;
    public float expectedRunSeconds = 300;
    public Gradient sunColor;
    public AnimationCurve sunIntensity = AnimationCurve.Linear(0, 1, 1, 1);

    [Header("Effects")]
    public GameObject breakEffect;
    public GameObject bigBreakEffect;
    public GameObject splashEffect;
    private AudioSource marketVoice;

    public static NoryangjinRevampDirector Active { get; private set; }
    public PlayerScript Player { get; private set; }
    public NoryangjinRevampHud Hud { get; private set; }
    public bool Running { get; private set; }
    public bool ShieldReady { get; private set; }
    public bool Spinning => spinLeft > 0;
    public bool Blocked { get; private set; }
    public float Elapsed { get; private set; }
    public NoryangjinMarketBranch Branch { get; private set; }
    public float QuietUntil { get; private set; }
    public readonly List<string> Timeline = new();

    private NoryangjinRevampEvent[] events = Array.Empty<NoryangjinRevampEvent>();
    private NoryangjinBreakable[] breakables = Array.Empty<NoryangjinBreakable>();
    private float spinLeft, spinDrain, spinImmuneUntil, spinAngle, blockedDrain;
    private PlayerDamageCause spinCause;
    private Transform[] spinVisuals = Array.Empty<Transform>();
    private Quaternion[] spinRest = Array.Empty<Quaternion>();
    private Color sunRestColor; private float sunRestIntensity; private bool sunCaptured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Active = null;

    public static bool IsRevampScene(string sceneName) => sceneName == ChapterSceneKey.Sr18Revamp;

    private static NoryangjinRevampDirector For(PlayerScript player)
        => Active != null && Active.Running && player != null && Active.Player == player ? Active : null;
    public static float ForwardMultiplier(PlayerScript player)
    {
        var d = For(player);
        return d == null ? 1f : d.Spinning || d.Blocked || d.Branch != null && d.Branch.Waiting ? 0f
            : d.Branch != null && d.Branch.Complete ? 1.3f : 1f;
    }
    public static bool LateralLocked(PlayerScript player)
    {
        var d = For(player);
        return d != null && (d.Spinning || d.Branch != null && d.Branch.Waiting);
    }
    public static bool AdvanceBranch(PlayerScript player, float seconds)
    {
        var d = For(player);
        return d != null && d.Branch != null && d.Branch.Advance(player, seconds, ForwardMultiplier(player));
    }
    public void QuietFor(float seconds) => QuietUntil = Mathf.Max(QuietUntil, Elapsed + seconds);
    public void Speak(AudioClip clip, bool priority = true, float pitch = 1)
    {
        if (clip == null) return;
        if (marketVoice == null) { marketVoice = gameObject.AddComponent<AudioSource>(); marketVoice.playOnAwake = false; marketVoice.spatialBlend = 0; marketVoice.volume = .28f; }
        if (!priority && marketVoice.isPlaying) return;
        marketVoice.pitch = pitch; marketVoice.clip = clip; marketVoice.Play();
    }
    public bool CanStartHazard => Elapsed >= QuietUntil && !Player.IsWorldYawTurnActive && !(Branch != null && Branch.Waiting);
    public static bool ConsumeShield(PlayerScript player, float amount, PlayerDamageCause cause)
    {
        var d = For(player);
        if (d == null || !d.ShieldReady || cause == PlayerDamageCause.Wave || cause == PlayerDamageCause.Hole
            || amount < player.MaxHealth * d.shieldMinimumShare) return false;
        d.ShieldReady = false;
        d.Hud?.Publish("1회 막기 쉴드가 공격을 막았다!", 2f);
        d.Timeline.Add($"shield blocked {amount:F0} {cause} at {d.Elapsed:F1}");
        return true;
    }

    private void Awake()
    {
        if (!IsRevampScene(gameObject.scene.name)) { enabled = false; return; }
        Active = this;
        Player = FindFirstObjectByType<PlayerScript>();
        Branch = GetComponentInChildren<NoryangjinMarketBranch>(true);
        events = GetComponentsInChildren<NoryangjinRevampEvent>(true);
        breakables = GetComponentsInChildren<NoryangjinBreakable>(true);
        foreach (var e in events) e.Bind(this);
        foreach (var b in breakables) b.Bind(this);
        if (Player != null)
        {
            var list = new List<Transform>();
            foreach (Transform child in Player.transform)
                if (child.name == "PlayerMesh" || child.name == "Original" || child.name == "Sharks" || child.name.StartsWith("model_Animation")) list.Add(child);
            spinVisuals = list.ToArray();
            spinRest = new Quaternion[spinVisuals.Length];
        }
    }

    private void Start()
    {
        var canvas = FindFirstObjectByType<CanvasScript>();
        if (canvas != null) Hud = NoryangjinRevampHud.Create(canvas.transform);
    }

    public void BeginRun()
    {
        if (!enabled) return;
        Active = this; Running = true; Elapsed = 0; ShieldReady = false; Blocked = false;
        spinLeft = 0; spinImmuneUntil = 0; blockedDrain = 0; Timeline.Clear();
        QuietUntil = 0;
        if (marketVoice != null) marketVoice.Stop();
        RestoreSpinPose();
        foreach (var b in breakables) b.ResetForRun();
        foreach (var e in events) e.ResetForRun();
        Hud?.ResetForRun();
        if (sun != null && !sunCaptured) { sunRestColor = sun.color; sunRestIntensity = sun.intensity; sunCaptured = true; }
    }

    private void Update()
    {
        if (!Running || Player == null) return;
        if (!TimeManager.isGameRunning || Player.currentHealth <= 0)
        {
            if (Player.currentHealth <= 0) { Running = false; RestoreSpinPose(); Hud?.HideTimer(); }
            return;
        }
        float dt = Time.deltaTime * Mathf.Max(0, TimeManager.timeFactor);
        if (dt <= 0) return;
        Elapsed += dt;

        Blocked = ComputeBlocked(out var blocker);
        if (Blocked && blocker != null && blocker.drainWhileBlocked > 0)
        {
            blockedDrain += Player.MaxHealth * blocker.drainWhileBlocked * dt;
            if (blockedDrain >= 1) { Player.ApplyDamage(blockedDrain, PlayerDamageCause.Roadblock); blockedDrain = 0; }
        }
        foreach (var e in events) if (e.isActiveAndEnabled) e.Tick(dt);
        TickSpin(dt);
        TickSun();
        if (shieldAura != null)
        {
            shieldAura.gameObject.SetActive(ShieldReady);
            if (ShieldReady)
            {
                shieldAura.position = Player.transform.position + Vector3.up * 1.1f;
                shieldAura.Rotate(Vector3.up, 90 * dt, Space.World);
            }
        }
    }

    private bool ComputeBlocked(out NoryangjinBreakable blocker)
    {
        blocker = null;
        Vector3 p = Player.transform.position;
        Vector3 forward = Vector3.ProjectOnPlane(Player.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        foreach (var b in breakables)
        {
            if (!b.BlocksNow) continue;
            Vector3 rel = b.transform.position - p;
            if (Mathf.Abs(rel.y) > 3) continue;
            float front = Vector3.Dot(rel, forward) - b.halfDepth;
            float lateral = Vector3.Dot(rel, right);
            if (front >= -.4f && front <= stopGap && Mathf.Abs(lateral) <= b.halfWidth + playerHalfWidth) { blocker = b; return true; }
        }
        return false;
    }

    public float PlayerFireRate()
    {
        var manager = Player != null ? Player.GetComponent<WeaponManager>() : null;
        var weapon = manager != null && manager.currentWeapon != null ? manager.currentWeapon.GetComponentInChildren<WeaponScript>() : null;
        return weapon != null && weapon.fireRate > 0 ? weapon.fireRate : 2f;
    }

    public bool StartSpin(float seconds, float drainShare, PlayerDamageCause cause, string message = null)
    {
        if (Spinning || Elapsed < spinImmuneUntil || Player == null) return false;
        spinLeft = seconds; spinDrain = drainShare; spinCause = cause; spinAngle = 0;
        for (int i = 0; i < spinVisuals.Length; i++) if (spinVisuals[i] != null) spinRest[i] = spinVisuals[i].localRotation;
        Hud?.Publish(message ?? "물줄에 걸렸다! 빙글빙글~", Mathf.Max(1.5f, seconds));
        Timeline.Add($"spin {cause} at {Elapsed:F1}");
        return true;
    }

    private void TickSpin(float dt)
    {
        if (!Spinning) return;
        float activeDt = Mathf.Min(dt, spinLeft);
        spinLeft -= activeDt;
        spinAngle += 900 * activeDt;
        for (int i = 0; i < spinVisuals.Length; i++)
            if (spinVisuals[i] != null) spinVisuals[i].localRotation = Quaternion.Euler(0, spinAngle, 0) * spinRest[i];
        Player.ApplyDamage(Player.MaxHealth * spinDrain * activeDt, spinCause);
        if (spinLeft <= 0) { RestoreSpinPose(); spinImmuneUntil = Elapsed + 1.2f; }
    }

    private void RestoreSpinPose()
    {
        if (spinLeft > 0 || spinAngle != 0)
            for (int i = 0; i < spinVisuals.Length; i++) if (spinVisuals[i] != null) spinVisuals[i].localRotation = spinRest[i];
        spinLeft = 0; spinAngle = 0;
    }

    private void TickSun()
    {
        if (sun == null || sunColor == null) return;
        float t = Mathf.Clamp01(Elapsed / Mathf.Max(1, expectedRunSeconds));
        sun.color = sunColor.Evaluate(t);
        sun.intensity = sunIntensity.Evaluate(t);
    }

    public void GrantShield()
    {
        ShieldReady = true;
        Hud?.Publish("낙찰! 1회 막기 쉴드", 2.5f);
        Timeline.Add($"reward shield at {Elapsed:F1}");
    }

    public void GrantHeal(float share)
    {
        float restored = Player.Heal(Player.MaxHealth * share);
        Hud?.Publish($"낙찰! 체력 회복 +{share * 100:F0}% (+{restored:F0})", 2.5f);
        Timeline.Add($"reward heal {restored:F0} at {Elapsed:F1}");
    }


    public void Burst(GameObject effect, Vector3 position, float scale = 1)
    {
        if (effect == null) return;
        var fx = Instantiate(effect, position, Quaternion.identity);
        fx.transform.localScale *= scale;
        Destroy(fx, 4f);
    }

    public void ExplodeAround(NoryangjinBreakable source, float radius)
    {
        int count = 0;
        foreach (var b in breakables)
            if (b != source && b.Alive && Vector3.Distance(b.transform.position, source.transform.position) <= radius) { b.Damage(b.Health); count++; }
        if (count > 0) Hud?.Publish($"가스통 연쇄 폭발! {count}개 파괴", 2f);
        Timeline.Add($"chain {count} at {Elapsed:F1}");
    }

    private void OnDisable()
    {
        RestoreSpinPose();
        if (sun != null && sunCaptured) { sun.color = sunRestColor; sun.intensity = sunRestIntensity; }
        Running = false;
        if (marketVoice != null) marketVoice.Stop();
        if (Active == this) Active = null;
    }
}
