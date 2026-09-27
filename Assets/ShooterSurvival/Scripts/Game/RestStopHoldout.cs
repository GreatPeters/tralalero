using IndianOceanAssets.ShooterSurvival;
using UnityEngine;
using UnityEngine.EventSystems;

// A scene-owned, bounded encounter. It never pauses the global combat clock.
[DefaultExecutionOrder(200)]
public sealed class RestStopHoldout : MonoBehaviour
{
    public Transform center, exitShutter, frontShutter;
    public Transform[] entrances;
    public Renderer[] entranceSignals;
    public Renderer[] overheadOccluders = System.Array.Empty<Renderer>();
    public EnemyEventController[] police;
    public ChapterPatternHUD hud;
    public float Duration { get; private set; } = 30;
    public const float TurnDegreesPerSecond = 90f;
    public float AimYaw { get; private set; }
    public Vector3 AimDirection => Quaternion.Euler(0, AimYaw, 0) * Vector3.forward;
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
    private float cameraFov, nextSpawn, policeDamage = 350, policeHealth = 700, policeSpeed = 3.8f, spawnInterval = 1.05f;
    private bool cameraOrthographic, followEnabled, occlusionEnabled;
    private StableGameplayCamera followCamera;
    private NoryangjinCameraOcclusion occlusion;
    private Vector3 cameraWorldPosition;
    private Quaternion cameraWorldRotation;
    private float cameraHeading, dragOrigin;
    private int dragFinger = -1;
    private bool dragging;
    private LineRenderer aimMarker;
    private Material aimMaterial;
    private bool cameraCaptured;
    private bool[] overheadVisibility;
    private MaterialPropertyBlock signalColor;
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
        // Latest user request (2026-09-25): survive 30 seconds; the workbook row can still tune it.
        Duration = HighwayRoute.Setting("reststopHoldoutSeconds", 30, 15, 60);
        policeHealth = HighwayRoute.Setting("reststopPoliceHealth", 700, 50, 3000);
        // Separate from health so a hit costs ~3% of a mid-progress shark instead of ~6%.
        policeDamage = HighwayRoute.Setting("reststopPoliceDamage", 350, 20, 3000);
        policeSpeed = HighwayRoute.Setting("reststopPoliceSpeed", 3.8f, 2, 7);
        spawnInterval = HighwayRoute.Setting("reststopPoliceInterval", 1.05f, .6f, 2);
        Elapsed = 0; Completed = false; Spawned = Killed = 0; dragging = false;
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
        RotateAim(ReadTurnInput(), delta);
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
            // Open with all approaches visible, then sustain overlapping groups.
            // 2026-09-26: farther doors (18) and slightly fewer officers per wave.
            int count = Spawned == 0 ? 3 : phase == 2 ? 3 : 2;
            for (int i = 0; i < count; i++)
                if (!Spawn(Door(Spawned, phase))) break;
            // 30-second tuning (2026-09-25): ~0.9 officers/s in phases 1-2, ~1.1/s in the final third,
            // slightly below a mid-progress shark's kill rate so pressure builds without an automatic wipe.
            nextSpawn = Elapsed + spawnInterval * (phase == 2 ? 2.9f : phase == 1 ? 2.4f : 2.7f);
        }
        // The countdown lives in RestStopHoldoutBanner; this panel keeps only controls and score.
        hud?.Show(this, $"처치 {Killed}", "좌우로 드래그해 회전 · 손을 떼면 정지", 3);
        if (Elapsed >= Duration) End(true);
    }
    private void BeginEncounter()
    {
        GameAudioService.Play(GameSound.Holdout);
        Active = true; LockedPosition = player.transform.position; nextSpawn = .9f;
        AimYaw = player.transform.eulerAngles.y; cameraHeading = AimYaw + 30f; dragging = false;
        player.SetStationaryCombat(this, true);
        visual = player.transform.Find("Original");
        if (visual != null) visualRotation = visual.localRotation;
        if (battleCamera != null)
        {
            cameraPosition = battleCamera.transform.localPosition; cameraRotation = battleCamera.transform.localRotation; cameraFov = battleCamera.fieldOfView; cameraCaptured = true;
            cameraWorldPosition = battleCamera.transform.position; cameraWorldRotation = battleCamera.transform.rotation;
            cameraOrthographic = battleCamera.orthographic;
            followCamera = battleCamera.GetComponent<StableGameplayCamera>();
            followEnabled = followCamera != null && followCamera.enabled;
            if (followCamera != null) followCamera.enabled = false;
            occlusion = battleCamera.GetComponent<NoryangjinCameraOcclusion>();
            occlusionEnabled = occlusion != null && occlusion.enabled;
            if (occlusion != null) occlusion.enabled = false;
        }
        CreateAimMarker();
        overheadVisibility = new bool[overheadOccluders.Length];
        for (int i = 0; i < overheadOccluders.Length; i++)
        {
            if (overheadOccluders[i] == null) continue;
            overheadVisibility[i] = overheadOccluders[i].forceRenderingOff;
            overheadOccluders[i].forceRenderingOff = true;
        }
        SetDoors(true);
    }
    private void LateUpdate()
    {
        if (!Active || player == null) return;
        if (visual != null)
            visual.localRotation = Quaternion.Inverse(player.transform.rotation) * Quaternion.LookRotation(AimDirection) * visualRotation;
        if (battleCamera != null)
        {
            Quaternion rotation = Quaternion.Euler(55, cameraHeading, 0);
            float radius = 12f;
            if (entrances != null) foreach (var entry in entrances)
                if (entry != null) radius = Mathf.Max(radius, Vector3.ProjectOnPlane(entry.position - LockedPosition, Vector3.up).magnitude + 1.5f);
            float distance = radius / (Mathf.Tan(22.5f * Mathf.Deg2Rad) * Mathf.Min(1f, Mathf.Max(.3f, battleCamera.aspect))) + radius * .45f;
            Vector3 focus = LockedPosition + Vector3.up * 1.1f;
            Vector3 position = focus - rotation * Vector3.forward * distance;
            float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Elapsed / .8f));
            battleCamera.orthographic = false;
            battleCamera.fieldOfView = Mathf.Lerp(cameraFov, 45, blend);
            battleCamera.transform.SetPositionAndRotation(Vector3.Lerp(cameraWorldPosition, position, blend), Quaternion.Slerp(cameraWorldRotation, rotation, blend));
        }
        UpdateAimMarker();
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
            var spawn = start.position + start.right * (((Spawned / 4) % 3 - 1) * 1.4f);
            actor.PrepareSpawnAt(spawn, Quaternion.LookRotation(LockedPosition - start.position));
            actor.TargetPoint.position = LockedPosition;
            actor.EventMode = EnemyEventMode.MoveToTargetThenAttack;
            actor.MoveAnimation = EnemyMoveAnimation.Run;
            actor.MoveSpeed = policeSpeed * (Phase(Elapsed, Duration) == 2 ? 1.12f : 1);
            actor.gameObject.SetActive(true);
            var combat = actor.GetComponent<EnemyScript_space>();
            combat.ConfigureRewards(false, 0);
            combat.ApplyStat(policeDamage, policeHealth, EnemyTier.Normal);
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
        // Assist only within the player's narrow facing cone; never turn toward
        // an unselected attacker behind them. Empty directions still fire.
        EnemyScript_space target = null; float best = float.PositiveInfinity;
        foreach (var actor in police)
        {
            if (!actor.gameObject.activeInHierarchy || actor.RuntimeState == EnemyEventRuntimeState.Dead) continue;
            var combat = actor.GetComponent<EnemyScript_space>();
            if (combat.CurrentHealth <= 0) continue;
            Vector3 offset = actor.transform.position - LockedPosition;
            if (Vector3.Angle(AimDirection, Vector3.ProjectOnPlane(offset, Vector3.up)) > 10) continue;
            float distance = offset.sqrMagnitude;
            if (distance < best) { best = distance; target = combat; }
        }
        direction = target != null ? (target.transform.position + Vector3.up * 1.05f - muzzle).normalized : AimDirection;
        return Active;
    }
    public void RotateAim(float input, float deltaSeconds)
    {
        if (!Active) return;
        AimYaw = AdvanceYaw(AimYaw, input, deltaSeconds);
    }
    public static float AdvanceYaw(float yaw, float input, float deltaSeconds)
        => Mathf.Repeat(yaw + Mathf.Clamp(input, -1, 1) * TurnDegreesPerSecond * Mathf.Max(0, deltaSeconds), 360);
    private float ReadTurnInput()
    {
        if (Application.isMobilePlatform)
        {
            if (!dragging)
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase != TouchPhase.Began || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))) continue;
                    dragging = true; dragFinger = touch.fingerId; dragOrigin = touch.position.x; break;
                }
            for (int i = 0; i < Input.touchCount && dragging; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.fingerId != dragFinger) continue;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) { dragging = false; return 0; }
                return Mathf.Clamp((touch.position.x - dragOrigin) / Mathf.Max(1, Screen.width * .15f), -1, 1);
            }
            dragging = false; return 0;
        }
        if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        { dragging = true; dragOrigin = Input.mousePosition.x; }
        if (!Input.GetMouseButton(0)) dragging = false;
        float keyboard = Input.GetAxisRaw("Horizontal");
        return keyboard != 0 ? keyboard : dragging ? Mathf.Clamp((Input.mousePosition.x - dragOrigin) / Mathf.Max(1, Screen.width * .15f), -1, 1) : 0;
    }
    private void OnApplicationFocus(bool focused) { if (!focused) dragging = false; }
    private void CreateAimMarker()
    {
        if (aimMarker == null)
        {
            var go = new GameObject("Holdout aim direction"); go.transform.SetParent(transform, false);
            aimMarker = go.AddComponent<LineRenderer>(); aimMarker.useWorldSpace = true;
            aimMarker.positionCount = 5; aimMarker.widthMultiplier = .14f;
            aimMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            aimMaterial.SetColor("_BaseColor", new Color(1, .72f, .15f)); aimMarker.sharedMaterial = aimMaterial;
            aimMarker.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        aimMarker.gameObject.SetActive(true);
    }
    private void UpdateAimMarker()
    {
        if (aimMarker == null) return;
        Vector3 origin = LockedPosition + Vector3.up * .18f, forward = AimDirection;
        Vector3 tip = origin + forward * 4.6f, side = Vector3.Cross(Vector3.up, forward) * .6f;
        aimMarker.SetPosition(0, origin + forward * 2.3f); aimMarker.SetPosition(1, tip);
        aimMarker.SetPosition(2, tip - forward + side); aimMarker.SetPosition(3, tip);
        aimMarker.SetPosition(4, tip - forward - side);
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
            player.Heal(player.MaxHealth * fraction);
        }
        Active = false;
        if (overheadVisibility != null)
            for (int i = 0; i < overheadVisibility.Length; i++)
                if (overheadOccluders[i] != null) overheadOccluders[i].forceRenderingOff = overheadVisibility[i];
        overheadVisibility = null;
        dragging = false;
        if (aimMarker != null) aimMarker.gameObject.SetActive(false);
        if (player != null) player.SetStationaryCombat(this, false);
        if (visual != null) visual.localRotation = visualRotation;
        if (cameraCaptured && battleCamera != null)
        {
            battleCamera.transform.localPosition = cameraPosition; battleCamera.transform.localRotation = cameraRotation; battleCamera.fieldOfView = cameraFov;
            battleCamera.orthographic = cameraOrthographic;
            if (followCamera != null) followCamera.enabled = followEnabled;
            if (occlusion != null) occlusion.enabled = occlusionEnabled;
        }
        cameraCaptured = false;
        if (police != null) foreach (var actor in police) if (actor != null) actor.gameObject.SetActive(false);
        SetDoors(false); hud?.Clear(this);
    }
    private void OnDisable() { if (Application.isPlaying) End(false); }
    private void OnDestroy()
    {
        if (aimMaterial == null) return;
        if (Application.isPlaying) Destroy(aimMaterial); else DestroyImmediate(aimMaterial);
    }
    public static int Phase(float elapsed, float duration) => Mathf.Min(2, Mathf.FloorToInt(Mathf.Max(0, elapsed) / Mathf.Max(1, duration / 3)));
    public static int Door(int index, int phase) => index % 4;
}
