using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

[RequireComponent(typeof(ObstacleStats))]
public sealed class HighwayHazard : MonoBehaviour
{
    public Transform visual, barrierArm;
    public Renderer signal;
    public GameObject warning;
    public int laneIndex;
    public float breakHealth = 70f, warningSeconds = 1.5f, cycleSeconds = 6f, crossingDistance = 6f;
    private ObstacleStats stats;
    private Collider contact;
    private ChapterProgression chapter;
    private PlayerScript player;
    private Vector3 startPosition;
    private float health, activatedAt = -1f;
    private bool hitPlayer, broken;
    private MaterialPropertyBlock tint;
    public bool IsOpen { get; private set; }
    public bool Broken => broken;

    private void Awake()
    {
        tint = new MaterialPropertyBlock();
        stats = GetComponent<ObstacleStats>(); contact = GetComponent<Collider>();
        chapter = FindFirstObjectByType<ChapterProgression>(); player = FindFirstObjectByType<PlayerScript>();
        startPosition = transform.position; ResetForRun();
    }
    public static int OpenLane(float elapsed, float cycle) => Mathf.FloorToInt(Mathf.Max(0, elapsed) / Mathf.Max(1, cycle)) % 3;
    public void ResetForRun()
    {
        health = breakHealth; hitPlayer = broken = false; activatedAt = -1;
        if (stats == null) return;
        transform.position = startPosition;
        if (visual != null) visual.gameObject.SetActive(true);
        if (contact != null) contact.enabled = true;
        if (warning != null) warning.SetActive(false);
    }
    private void Update()
    {
        if (!TimeManager.isGameRunning || broken || chapter == null) return;
        float clock = chapter.Elapsed;
        if (stats.obstaclePattern == ObstaclePattern.HighwayToll)
        {
            IsOpen = laneIndex == OpenLane(clock, cycleSeconds);
            if (barrierArm != null) barrierArm.localRotation = Quaternion.Euler(0, 0, IsOpen ? 82 : 0);
            if (contact != null) contact.enabled = !IsOpen;
            Color color = IsOpen ? new Color(.1f,1f,.35f) : new Color(1f,.13f,.055f);
            if (signal != null) { tint.SetColor("_BaseColor",color);tint.SetColor("_EmissionColor",color*2);signal.SetPropertyBlock(tint); }
        }
        else if (stats.obstaclePattern == ObstaclePattern.HighwayTraffic)
        {
            if (activatedAt < 0 && player != null && Vector3.Distance(player.transform.position, startPosition) < 44f)
            {
                activatedAt = clock;
                GameAudioService.Play(GameSound.Warning);
            }
            if (activatedAt < 0) return;
            float t = clock - activatedAt;
            if (warning != null) warning.SetActive(t < warningSeconds);
            if (t >= warningSeconds)
            {
                float progress = Mathf.Clamp01((t-warningSeconds)/Mathf.Max(1,cycleSeconds));
                transform.position = startPosition + transform.right * (crossingDistance * progress);
            }
        }
    }
    // The projectile delivers this before returning to its pool. Physics callback
    // ordering cannot guarantee a second callback after a collider is disabled.
    public void ReactToProjectile(BulletScript bullet)
    {
        if (broken || !TimeManager.isGameRunning || stats.obstaclePattern != ObstaclePattern.HighwayRoadblock ||
            bullet == null || !bullet.HasDamagePayload) return;
        health -= bullet.LaunchDamage;
        if (health <= 0)
        {
            broken = true;
            GameAudioService.PlayAt(GameSound.Explosion, transform.position);
            if (contact != null) contact.enabled = false;
            if (visual != null) visual.gameObject.SetActive(false);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (broken || !TimeManager.isGameRunning) return;
        if (hitPlayer || IsOpen) return;
        var target = other.GetComponentInParent<PlayerScript>(); if (target == null) return;
        hitPlayer = true; target.DieFromHazard(false);
    }
    private void OnCollisionEnter(Collision collision) => OnTriggerEnter(collision.collider);
}
