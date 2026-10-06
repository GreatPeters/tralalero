using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

// A real projectile target. The switch and its panel share one health/open state.
[DisallowMultipleComponent]
public sealed class Chapter45Target : MonoBehaviour
{
    public float distance, lane, halfWidth = 2, health = 200, activationLead = 32;
    public int floor, choiceIndex = -1, requiredChoice;
    public bool blocking = true, blocksAllLanes, captain, requiredForGoal;
    public string healthKey = "c45_targetHealth";
    public Transform panel;
    public Vector3 panelOpenOffset = Vector3.up * 6;
    public Collider panelCollider, hitCollider;
    public TMP_Text healthLabel;
    public GameObject shieldVisual;
    public Renderer[] targetRenderers = System.Array.Empty<Renderer>();
    public Chapter45Hazard[] pressureHazards = System.Array.Empty<Chapter45Hazard>();
    public float Health { get; private set; }
    public float MaximumHealth { get; private set; }
    public bool Alive => Health > 0;
    public bool Open => !Alive && opening >= .65f;
    public bool Shielded { get; private set; }
    public int Phase { get; private set; }
    public float CurrentLane => captain ? (Phase - 1) * 3 : lane;
    public bool Eligible => director != null && director.ChoiceMatches(choiceIndex, requiredChoice);
    private Chapter45Director director;
    private Vector3 panelRest;
    private bool captured, admitted;
    private float opening, phaseClock;
    private int attackCycle;
    private TMP_Text displayedLabel;
    private float displayedHealth = float.NaN;
    private bool displayedShielded;

    public void ResetForRun(Chapter45Director owner)
    {
        director = owner;
        displayedLabel = null; displayedHealth = float.NaN;
        if (!captured) { if (panel != null) panelRest = panel.localPosition; captured = true; }
        Health = MaximumHealth = Chapter45Director.Setting(healthKey, health, 1, 10000000);
        Phase = 0; opening = phaseClock = 0; attackCycle = -1; admitted = Shielded = false;
        if (panel != null) panel.localPosition = panelRest;
        if (panelCollider != null) panelCollider.enabled = true;
        if (hitCollider == null) hitCollider = GetComponent<Collider>();
        if (hitCollider != null) hitCollider.enabled = false;
        foreach (var renderer in targetRenderers) if (renderer != null) renderer.enabled = true;
        if (shieldVisual != null) shieldVisual.SetActive(false);
        UpdateLabel();
    }
    public void Tick(float dt)
    {
        bool onDeck = Eligible && director.CurrentFloor == floor && !director.IsTransferring;
        bool near = onDeck && director.Distance >= distance - activationLead;
        if (!admitted && near) { admitted = true; director.Record("target admitted " + name); }
        if (hitCollider != null) hitCollider.enabled = admitted && onDeck && Alive;
        if (!Alive)
        {
            opening = Mathf.Min(1, opening + dt / .75f);
            if (panel != null) panel.localPosition = panelRest + panelOpenOffset * Mathf.SmoothStep(0, 1, opening);
            if (panelCollider != null && Open) panelCollider.enabled = false;
        }
        if (captain && admitted && onDeck && Alive)
        {
            phaseClock += dt;
            int cycle = Mathf.FloorToInt(phaseClock / 6);
            Shielded = phaseClock % 6 >= 4;
            if (Shielded && attackCycle != cycle)
            {
                attackCycle = cycle;
                if (pressureHazards.Length > 0) pressureHazards[(cycle + Phase) % pressureHazards.Length]?.Trigger();
                director.Announce("방패 재충전", "경고 구역을 피하고 빛나는 코어를 노리세요", 2);
            }
            director.Sample(distance, out var center, out var forward);
            Vector3 desired = center + Vector3.Cross(Vector3.up, forward) * CurrentLane;
            desired.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, desired, dt * 7);
        }
        if (shieldVisual != null) shieldVisual.SetActive(Alive && Shielded && onDeck);
        UpdateLabel();
    }
    public bool ReceiveProjectile(BulletScript projectile)
    {
        if (projectile == null || !Alive || !admitted || !Eligible || director.IsTransferring || director.CurrentFloor != floor || !projectile.CanHitTarget(transform)) return false;
        if (Shielded) { GameAudioService.PlayAt(GameSound.EnemyHit, transform.position); return true; }
        float damage = projectile.HasDamagePayload ? projectile.LaunchDamage : director.Player.ResolvedAttackDamage;
        if (damage <= 0) return true;
        int nextPhase = Phase;
        float next = captain ? Chapter45Rules.CaptainHealthAfterHit(Health, MaximumHealth, Phase, damage, out nextPhase) : Mathf.Max(0, Health - damage);
        // Threshold clamps start an explicit amber phase. Lost HP never returns.
        if (captain && nextPhase != Phase)
        {
            Phase = nextPhase; phaseClock = 4; Shielded = true; attackCycle = -1;
            director.Record("captain phase " + (Phase + 1));
            director.Announce("보안 코어 " + (Phase + 1) + "단계", "방패 재충전 · 다음 빛나는 코어를 노리세요", 2);
        }
        float applied = Health - next; Health = next;
        DamagePopupFX.Show(transform.position + Vector3.up * 2, applied);
        GameAudioService.PlayAt(GameSound.EnemyHit, transform.position);
        UpdateLabel();
        if (!Alive)
        {
            if (hitCollider != null) hitCollider.enabled = false;
            foreach (var renderer in targetRenderers) if (renderer != null) renderer.enabled = false;
            if (shieldVisual != null) shieldVisual.SetActive(false);
            foreach (var hazard in pressureHazards) if (hazard != null) hazard.Cancel();
            director.Record("target destroyed " + name);
            director.Announce(captain ? "전시실 개방" : "보안문 개방", captain ? "신발 안으로 들어가 제물을 찾으세요" : "열린 통로로 이동하세요", 3);
            GameAudioService.PlayAt(GameSound.EnemyDeath, transform.position);
        }
        return true;
    }
    public bool Blocks(float atDistance, float playerLane)
    {
        if (!blocking || Open || !Eligible || director.CurrentFloor != floor) return false;
        return atDistance >= distance - 6 && director.Distance < distance - .25f
            && (blocksAllLanes || Mathf.Abs(playerLane - lane) <= halfWidth + .65f);
    }
    private void UpdateLabel()
    {
        if (healthLabel == null) return;
        bool showShield = captain && Shielded;
        if (displayedLabel == healthLabel && displayedHealth == Health && displayedShielded == showShield) return;
        displayedLabel = healthLabel; displayedHealth = Health; displayedShielded = showShield;
        healthLabel.text = Alive ? (captain && Shielded ? "방패 · " : "") + Health.ToString("0") : "개방";
    }
}
