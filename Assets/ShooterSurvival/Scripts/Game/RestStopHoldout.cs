using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// A scene-owned, bounded encounter. It never pauses the global combat clock.
public sealed class RestStopHoldout : MonoBehaviour
{
    public Transform center, exitShutter, frontShutter;
    public Transform[] entrances;
    public Renderer[] entranceSignals;
    public EnemyEventController[] police;
    public ChapterPatternHUD hud;
    public float Duration { get; private set; } = 30;
    public float Elapsed { get; private set; }
    public bool Active { get; private set; }
    public bool Completed { get; private set; }
    public int Spawned { get; private set; }
    public int Killed { get; private set; }
    public int[] SpawnedByDoor { get; private set; } = new int[4];
    public float MaximumSpawnPositionError { get; private set; }
    public Vector3 LockedPosition { get; private set; }
    private PlayerScript player;
    private Camera battleCamera;
    private Transform visual;
    private Vector3 cameraPosition, exitPosition, frontPosition;
    private Quaternion cameraRotation, visualRotation;
    private float cameraFov, nextSpawn, policeHealth = 700, policeSpeed = 3.8f, spawnInterval = 1.05f;
    private bool cameraCaptured;
    private MaterialPropertyBlock signalColor;
    private EnemyScript_space target;
    private bool[] alive;

    private void Awake()
    {
        if (exitShutter != null) exitPosition = exitShutter.localPosition;
        if (frontShutter != null) frontPosition = frontShutter.localPosition;
    }

    public void BeginRun()
    {
        End(false);
        player = FindFirstObjectByType<PlayerScript>();
        battleCamera = Camera.main;
        Duration = HighwayRoute.Setting("reststopHoldoutSeconds", 30, 15, 60);
        policeHealth = HighwayRoute.Setting("reststopPoliceHealth", 700, 50, 3000);
        policeSpeed = HighwayRoute.Setting("reststopPoliceSpeed", 3.8f, 2, 7);
        spawnInterval = HighwayRoute.Setting("reststopPoliceInterval", 1.05f, .6f, 2);
        Elapsed = 0; Completed = false; Spawned = Killed = 0; target = null;
        SpawnedByDoor = new int[4]; MaximumSpawnPositionError = 0;
        alive = new bool[police.Length];
        signalColor = new MaterialPropertyBlock();
        foreach (var actor in police) actor.gameObject.SetActive(false);
        SetDoors(false);
    }
    private void Update()
    {
        if (player == null || Completed) return;
        if (Active && (player.currentHealth <= 0 || CanvasScript.isGameOver)) { End(false); return; }
        if (!TimeManager.isGameRunning || TimeManager.timeFactor <= 0) return;
        if (!Active)
        {
            var offset = player.transform.position - center.position;
            if (Mathf.Abs(Vector3.Dot(offset, center.forward)) <= .8f && Mathf.Abs(Vector3.Dot(offset, center.right)) < 6) BeginEncounter();
            return;
        }
        float delta = Time.deltaTime * TimeManager.timeFactor;
        Elapsed = Mathf.Min(Duration, Elapsed + delta);
        CountKills();
        int phase = Phase(Elapsed, Duration);
        int door = Door(Spawned, phase);
        for (int i = 0; i < entranceSignals.Length; i++)
        {
            var color = i == door ? new Color(1, .20f, .04f) : new Color(.05f, .4f, .5f);
            signalColor.SetColor("_BaseColor", color); signalColor.SetColor("_EmissionColor", color * 2);
            entranceSignals[i].SetPropertyBlock(signalColor);
        }
        if (Elapsed >= nextSpawn && Elapsed < Duration - 2)
        {
            if (Spawn(door)) nextSpawn = Elapsed + spawnInterval * (phase == 2 ? .75f : 1);
            else nextSpawn = Elapsed + .25f;
        }
        hud?.Show(this, $"식당 포위전  {Mathf.CeilToInt(Duration - Elapsed):00}초", phase == 0 ? "위치 고정 · 360° 자동 조준" : phase == 1 ? "앞뒤 출입구 접근 · 몸체 회전 사격" : "사방에서 접근 중 · 곧 탈출로가 열립니다", 3);
        if (Elapsed >= Duration) End(true);
    }
    private void BeginEncounter()
    {
        GameAudioService.Play(GameSound.Holdout);
        Active = true; LockedPosition = player.transform.position; nextSpawn = 1.8f;
        player.SetStationaryCombat(this, true);
        visual = player.transform.Find("Original");
        if (visual != null) visualRotation = visual.localRotation;
        if (battleCamera != null)
        {
            cameraPosition = battleCamera.transform.localPosition; cameraRotation = battleCamera.transform.localRotation; cameraFov = battleCamera.fieldOfView; cameraCaptured = true;
        }
        SetDoors(true);
    }
    private void LateUpdate()
    {
        if (!Active || player == null) return;
        // Keep the ordinary gameplay camera. Only the visible body changes yaw;
        // rear attackers may be outside the view and do not justify reframing it.
        if (visual != null && target != null && target.gameObject.activeInHierarchy && target.CurrentHealth > 0)
        {
            var direction = Vector3.ProjectOnPlane(target.transform.position - player.transform.position, Vector3.up);
            if (direction.sqrMagnitude > .01f)
                visual.localRotation = Quaternion.Inverse(player.transform.rotation) * Quaternion.LookRotation(direction) * visualRotation;
        }
        if (battleCamera != null)
            foreach (var actor in police)
                if (actor.gameObject.activeInHierarchy)
                {
                    var label = actor.GetComponentInChildren<TMPro.TMP_Text>();
                    if (label != null) label.transform.rotation = battleCamera.transform.rotation;
                }
    }
    private bool Spawn(int door)
    {
        for (int i = 0; i < police.Length; i++)
        {
            var actor = police[i];
            if (actor.gameObject.activeSelf) continue;
            var start = entrances[door];
            var spawn = start.position + start.right * ((Spawned % 3 - 1) * .65f);
            actor.PrepareSpawnAt(spawn, Quaternion.LookRotation(LockedPosition - start.position));
            actor.TargetPoint.position = LockedPosition;
            actor.EventMode = EnemyEventMode.MoveToTargetThenAttack;
            actor.MoveAnimation = EnemyMoveAnimation.Run;
            actor.MoveSpeed = policeSpeed * (Phase(Elapsed, Duration) == 2 ? 1.12f : 1);
            actor.gameObject.SetActive(true);
            var combat = actor.GetComponent<EnemyScript_space>();
            combat.ConfigureRewards(false, 0);
            combat.ApplyStat(policeHealth, policeHealth, EnemyTier.Normal);
            actor.ActivateFromSpot();
            MaximumSpawnPositionError = Mathf.Max(MaximumSpawnPositionError, Vector3.Distance(actor.transform.position, spawn));
            SpawnedByDoor[door]++;
            alive[i] = true; Spawned++; return true;
        }
        return false;
    }
    private void CountKills()
    {
        for (int i = 0; i < police.Length; i++)
            if (alive[i] && (!police[i].gameObject.activeSelf || police[i].RuntimeState == EnemyEventRuntimeState.Dead))
            { alive[i] = false; Killed++; }
    }
    public bool TryAim(Vector3 muzzle, out Vector3 direction)
    {
        target = null; float best = float.PositiveInfinity;
        foreach (var actor in police)
        {
            if (!actor.gameObject.activeInHierarchy || actor.RuntimeState == EnemyEventRuntimeState.Dead) continue;
            var combat = actor.GetComponent<EnemyScript_space>();
            if (combat.CurrentHealth <= 0) continue;
            float distance = (actor.transform.position - player.transform.position).sqrMagnitude;
            if (distance < best) { best = distance; target = combat; }
        }
        direction = target != null ? (target.transform.position + Vector3.up * 1.05f - muzzle).normalized : Vector3.forward;
        return target != null;
    }
    private void SetDoors(bool closed)
    {
        if (exitShutter != null) exitShutter.localPosition = exitPosition + Vector3.up * (closed ? 0 : 6);
        if (frontShutter != null) frontShutter.localPosition = frontPosition + Vector3.up * (closed ? 0 : 6);
    }
    private void End(bool success)
    {
        if (Active && success && player != null)
        {
            Completed = true;
            float fraction = HighwayRoute.Setting("reststopHoldoutHeal", .12f, 0, .3f);
            player.currentHealth = HighwayRoute.RecoveredHealth(player.currentHealth, player.MaxHealth, fraction); player.UpdateHealth();
        }
        Active = false;
        if (player != null) player.SetStationaryCombat(this, false);
        if (visual != null) visual.localRotation = visualRotation;
        if (cameraCaptured && battleCamera != null)
        { battleCamera.transform.localPosition = cameraPosition; battleCamera.transform.localRotation = cameraRotation; battleCamera.fieldOfView = cameraFov; }
        cameraCaptured = false;
        if (police != null) foreach (var actor in police) if (actor != null) actor.gameObject.SetActive(false);
        SetDoors(false); hud?.Clear(this);
    }
    private void OnDisable() { if (Application.isPlaying) End(false); }
    public static int Phase(float elapsed, float duration) => Mathf.Min(2, Mathf.FloorToInt(Mathf.Max(0, elapsed) / Mathf.Max(1, duration / 3)));
    public static int Door(int index, int phase) => phase == 0 ? index % 2 : phase == 1 ? (index % 2 == 0 ? 2 : 3) : index % 4;
}
