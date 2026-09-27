using System;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyScript_space))]
public sealed class HighwayVehicleEnemy : MonoBehaviour
{
    public Transform body;
    public TMP_Text healthNumber;
    public Renderer[] lamps = Array.Empty<Renderer>();
    public TrailRenderer[] speedTrails = Array.Empty<TrailRenderer>();
    public ParticleSystem[] idleSmoke = Array.Empty<ParticleSystem>();
    public Material blurMaterial;
    public Vector2 footprint;
    public HighwayVehicleKind kind;
    public HighwayVehicleRoute routeChoice;
    public float station, laneIndex, spawnAt, speed;
    public int group;
    public bool accidentFront;
    public Action<HighwayVehicleEnemy> Defeated;
    public Action<HighwayVehicleEnemy, BulletScript> ProjectileHit;
    public EnemyScript_space Combat { get; private set; }
    public float Distance { get; set; }
    public float Lane { get; private set; }
    public bool Active { get; private set; }
    public bool Spawned { get; private set; }
    public bool FastContact { get; private set; }
    public bool Dead => Combat != null && Combat.IsDead;
    public float HalfLength => footprint.y;
    public float HalfWidth => footprint.x;
    private Collider contact;
    private EncounterPlacementRow placement;
    private MaterialPropertyBlock tint;
    private bool contacted;
    private MeshFilter[] bodyMeshes = Array.Empty<MeshFilter>();
    private Vector3 lastPosition;

    private void Awake()
    {
        Combat = GetComponent<EnemyScript_space>();
        contact = GetComponent<Collider>();
        tint = new MaterialPropertyBlock();
        if (body != null) bodyMeshes = body.GetComponentsInChildren<MeshFilter>(true);
        lastPosition = transform.position;
    }

    public void ConfigurePlacement(EncounterPlacementRow row)
    {
        placement = row;
        kind = row.vehicleKind; routeChoice = row.vehicleRoute;
        station = row.vehicleStation; laneIndex = row.vehicleLane; spawnAt = row.vehicleSpawnAt;
        speed = row.moveSpeed; group = row.vehicleGroup; accidentFront = row.vehicleFront;
        if (Combat == null) Combat = GetComponent<EnemyScript_space>();
        Combat.ApplyStat(row.damage, row.health, row.tier);
        Combat.ConfigureRewards(row.dropBonusAltar, row.coinReward);
    }

    public void ResetForRun()
    {
        gameObject.SetActive(true);
        if (Combat == null) Combat = GetComponent<EnemyScript_space>();
        // EnemyScript_space's existing OnEnable is the combat-life reset, including score/reward ownership.
        Combat.enabled = false; Combat.enabled = true;
        if (placement != null) ConfigurePlacement(placement);
        Distance = station; Lane = HighwayChapter2Data.Lane(laneIndex);
        Active = Spawned = contacted = false;
        Show(false);
    }

    public void Activate(bool fast, float coinMultiplier)
    {
        if (Spawned || placement == null || !placement.enabled) return;
        FastContact = fast; Spawned = Active = true;
        Combat.ConfigureRewards(placement.dropBonusAltar, Mathf.RoundToInt(placement.coinReward * coinMultiplier));
        Show(true);
    }

    public void Retire()
    {
        Active = false;
        Show(false);
    }
    public void Skip() { Spawned = true; Retire(); }

    private void Show(bool visible)
    {
        if (body != null) body.gameObject.SetActive(visible);
        if (healthNumber != null) healthNumber.transform.parent.gameObject.SetActive(visible);
        if (contact == null) contact = GetComponent<Collider>();
        if (contact != null) contact.enabled = visible && !Dead;
        foreach (var trail in speedTrails) if (trail != null) { trail.emitting = visible && FastContact; if (!visible) trail.Clear(); }
    }

    public void Place(HighwayRoute route, bool branch)
    {
        route.Sample(Distance, branch, out var centre, out var forward);
        var slope = route.Point(Mathf.Min(route.length, Distance + .5f), branch) - route.Point(Mathf.Max(0, Distance - .5f), branch);
        transform.SetPositionAndRotation(centre + Vector3.Cross(Vector3.up, forward) * Lane, Quaternion.LookRotation(slope.sqrMagnitude > .001f ? -slope.normalized : -forward));
    }

    public bool ResolveFastContact(PlayerScript player)
    {
        if (!Active || contacted || Dead) return true;
        if (!FastContact) return false; // Preserve the existing remaining-health exchange for slow traffic.
        contacted = true;
        player.ApplyDamage(HighwayChapter2Rules.ContactDamage(Combat.CurrentHealth, player.MaxHealth, true, HighwayChapter2Data.Value("fastDamage")), PlayerDamageCause.Traffic);
        Combat.EnemyDeath();
        return true;
    }

    public void OnCombatDeath()
    {
        if (!Active) return;
        Active = false;
        Defeated?.Invoke(this);
        Show(false);
    }

    private void LateUpdate()
    {
        if (!Active || Dead) return;
        bool moved = (transform.position - lastPosition).sqrMagnitude > .0001f;
        lastPosition = transform.position;
        foreach (var trail in speedTrails) if (trail != null) trail.emitting = FastContact && moved;
        if (FastContact && moved && blurMaterial != null)
        {
            foreach (var mesh in bodyMeshes)
                for (int i = 1; i <= 2; i++)
                    Graphics.DrawMesh(mesh.sharedMesh, Matrix4x4.Translate(-transform.forward * speed * .025f * i) * mesh.transform.localToWorldMatrix,
                        blurMaterial, gameObject.layer, null, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }
        var player = HighwayChapter2Controller.Active != null ? HighwayChapter2Controller.Active.Player : null;
        bool clearView = player != null && Vector3.Dot(transform.position - player.transform.position, player.transform.forward) < HighwayChapter2Data.Value("clearViewAhead");
        foreach (var smoke in idleSmoke)
            if (smoke != null)
            {
                if (clearView) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                else if (!smoke.isPlaying) smoke.Play(true);
            }
        if (healthNumber != null)
        {
            healthNumber.text = Mathf.CeilToInt(Combat.CurrentHealth).ToString("N0");
            healthNumber.color = kind == HighwayVehicleKind.Tanker ? new Color(1, .86f, .1f) : Color.white;
            if (Camera.main != null) healthNumber.transform.parent.rotation = Camera.main.transform.rotation;
        }
        if (lamps.Length == 0) return;
        if (tint == null) tint = new MaterialPropertyBlock();
        for (int i = 0; i < lamps.Length; i++)
        {
            if (lamps[i] == null) continue;
            Color color = kind == HighwayVehicleKind.Police ? (i % 2 == 0 ? Color.red : Color.blue) : new Color(1, .55f, .02f);
            color *= Mathf.Repeat(Time.time * 3 + i * .5f, 1) < .5f ? 1 : .15f;
            tint.SetColor("_BaseColor", color); tint.SetColor("_EmissionColor", color * 2);
            lamps[i].SetPropertyBlock(tint);
        }
    }
}
