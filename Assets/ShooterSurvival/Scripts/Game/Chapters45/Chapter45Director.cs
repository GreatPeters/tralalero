using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Owns only Jamsil/ShoeTower. No timers force chapter completion: a physical goal
// after the required encounters performs the normal campaign transaction.
[DefaultExecutionOrder(170)]
[DisallowMultipleComponent]
public sealed class Chapter45Director : MonoBehaviour
{
    public int chapter = 4;
    public bool useExplicitRegistrations;
    public Chapter45Route route;
    public ChapterPatternHUD hud;
    public Transform shieldAura;
    public float footOffset = .12f;
    [Min(.1f)] public float authoredForwardScale = 1f;
    public float RouteSpeed => Player == null ? 0 : Player.ForwardMoveSpeed * Mathf.Max(.1f, authoredForwardScale);
    public Chapter45Encounter[] encounters = Array.Empty<Chapter45Encounter>();
    public Chapter45Choice[] choices = Array.Empty<Chapter45Choice>();
    public Chapter45Target[] targets = Array.Empty<Chapter45Target>();
    public Chapter45Hazard[] hazards = Array.Empty<Chapter45Hazard>();
    public Chapter45Lift[] lifts = Array.Empty<Chapter45Lift>();
    public Chapter45Goal[] goals = Array.Empty<Chapter45Goal>();
    public static Chapter45Director Active { get; private set; }
    public PlayerScript Player { get; private set; }
    public bool Running { get; private set; }
    public float Distance { get; private set; }
    public float Elapsed { get; private set; }
    public float FloorElapsed { get; private set; }
    private float grabbedUntil;
    public float CrowdMoveScale => Running && Elapsed < grabbedUntil ? .35f : 1f;
    public void ApplyCrowdGrab(float seconds)
    {
        if (!Running || IsTransferring || !TimeManager.isGameRunning || TimeManager.timeFactor <= 0) return;
        grabbedUntil = Mathf.Max(grabbedUntil, Elapsed + Mathf.Clamp(seconds, 0, 1.2f));
    }
    public bool CurrentFloorEncountersComplete
    {
        get
        {
            foreach (var encounter in encounters)
                if (encounter.floor == CurrentFloor && encounter.requiredToProceed && encounter.Eligible && !encounter.Complete) return false;
            foreach (var target in targets)
                if (target.floor == CurrentFloor && (target.requiredForGoal || target.captain) && target.Eligible && !target.Open) return false;
            return true;
        }
    }
    public int CurrentSegment { get; private set; }
    public int CurrentFloor => route != null && route.segments.Length > CurrentSegment ? route.segments[CurrentSegment].floor : 0;
    public bool IsTransferring => riding != null;
    public bool ShieldReady { get; private set; }
    public bool GoalClaimed { get; private set; }
    public int LiftCount { get; private set; }
    public float LastLiftSeconds { get; private set; }
    public readonly List<string> Timeline = new();
    public float Lane
    {
        get
        {
            if (Player == null || route == null) return 0;
            SampleCurrent(Distance, out var center, out var forward);
            return Vector3.Dot(Player.transform.position - center, Vector3.Cross(Vector3.up, forward));
        }
    }
    public bool RequiredEncountersComplete
    {
        get
        {
            foreach (var encounter in encounters) if (encounter.requiredToProceed && encounter.Eligible && !encounter.Complete) return false;
            foreach (var target in targets) if ((target.requiredForGoal || target.captain) && target.Eligible && !target.Open) return false;
            return true;
        }
    }
    private Chapter45SceneryGroup[] scenery = Array.Empty<Chapter45SceneryGroup>();
    private bool announcementLayoutCaptured;
    private Vector2 announcementPanelSize, announcementDetailSize, announcementTitlePosition;
    private Chapter45Lift riding;
    private int rideDestinationSegment;
    private StableGameplayCamera transferCamera;
    private Vector3 cameraOffsetBeforeRide;
    private Quaternion cameraRotationBeforeRide;
    private bool cameraAdjustedForRide;
    private float rideElapsed, rideDuration, visibilityClock;
    private readonly Chapter45NoticeQueue notices = new();
    private Vector3 rideFrom, rideTo, rideForward, landingForward, escalatorBoardingCenter;
    private bool movementBeforeRide, bodyWasKinematic, heightWasEnabled;
    private Rigidbody playerBody;
    private NoryangjinRoadHeightFollower heightFollower;
    private bool capturedRide;
    private struct CarriedHelper { public ExtraHelpBuffScript helper; public Vector3 localOffset; }
    private readonly List<CarriedHelper> carriedHelpers = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Active = null;
    public static bool IsChapterScene(string name) => name == "Jamsil" || name == "ShoeTower";
    public static Chapter45Director For(PlayerScript player)
        => Active != null && Active.enabled && Active.Player == player && player != null && Active.gameObject.scene == player.gameObject.scene ? Active : null;
    public static bool TryAdvance(PlayerScript player, float seconds)
    {
        var director = For(player);
        if (director == null || !director.Running) return false;
        director.Advance(seconds); return true;
    }
    public static bool LateralLocked(PlayerScript player) => For(player)?.IsTransferring == true;
    public static bool CanActorContact(PlayerScript player, Transform actor)
    {
        var owner = For(player);
        if (owner == null) return true;
        if (!owner.Running || owner.IsTransferring || !TimeManager.isGameRunning || TimeManager.timeFactor <= 0
            || actor == null || actor.gameObject.scene != owner.gameObject.scene) return false;
        var deck = actor.GetComponentInParent<Chapter45Deck>();
        return Chapter45Rules.SameDeck(owner.CurrentFloor, player.transform.position.y, deck != null ? deck.floor : -1, actor.position.y);
    }
    public static bool CanHelperContact(ExtraHelpBuffScript helper, Transform target)
    {
        var owner = helper != null ? For(helper.Owner) : null;
        if (owner == null) return true;
        if (!owner.Running || owner.IsTransferring || !TimeManager.isGameRunning || TimeManager.timeFactor <= 0 || target == null || target.gameObject.scene != owner.gameObject.scene) return false;
        var deck = target.GetComponentInParent<Chapter45Deck>();
        return Chapter45Rules.SameDeck(owner.CurrentFloor, owner.Player.transform.position.y, deck != null ? deck.floor : -1, target.position.y);
    }
    public static float ConstrainLane(PlayerScript player, float lane)
    {
        var director = For(player); if (director == null) return lane;
        foreach (var choice in director.choices)
            if (choice.Selected && choice.kind == Chapter45Choice.ChoiceKind.RouteFork && director.Distance >= choice.distance + choice.transitionLength && director.Distance <= choice.endDistance - choice.transitionLength)
                return Mathf.Clamp(lane, -choice.branchHalfWidth, choice.branchHalfWidth);
        return lane;
    }
    public static bool PreventDamage(PlayerScript player, float amount)
    {
        var director = For(player);
        if (director == null || !director.Running || amount <= 0) return false;
        if (director.IsTransferring) return true;
        if (!director.ShieldReady) return false;
        director.ShieldReady = false; director.Record("shield blocked " + amount.ToString("0"));
        director.Announce("보호막 사용", "공격을 한 번 막았습니다", 2); return true;
    }
    public static float Setting(string key, float fallback, float minimum, float maximum)
    {
        return !string.IsNullOrEmpty(key) && EnvironmentVariableTables.TryGetFloat(key, out var value) && !float.IsNaN(value) && !float.IsInfinity(value)
            ? Mathf.Clamp(value, minimum, maximum) : Mathf.Clamp(fallback, minimum, maximum);
    }
    private void Awake()
    {
        if (!IsChapterScene(gameObject.scene.name)) { enabled = false; return; }
        Active = this; Player = FindFirstObjectByType<PlayerScript>();
        if (route == null) route = GetComponent<Chapter45Route>();
        if (!useExplicitRegistrations && encounters.Length == 0) encounters = GetComponentsInChildren<Chapter45Encounter>(true);
        if (!useExplicitRegistrations && choices.Length == 0) choices = GetComponentsInChildren<Chapter45Choice>(true);
        if (!useExplicitRegistrations && targets.Length == 0) targets = GetComponentsInChildren<Chapter45Target>(true);
        if (!useExplicitRegistrations && hazards.Length == 0) hazards = GetComponentsInChildren<Chapter45Hazard>(true);
        if (!useExplicitRegistrations && lifts.Length == 0) lifts = GetComponentsInChildren<Chapter45Lift>(true);
        if (!useExplicitRegistrations && goals.Length == 0) goals = GetComponentsInChildren<Chapter45Goal>(true);
        scenery = GetComponentsInChildren<Chapter45SceneryGroup>(true);
        if (Player != null) { playerBody = Player.GetComponent<Rigidbody>(); heightFollower = Player.GetComponent<NoryangjinRoadHeightFollower>(); }
    }
    private void Start()
    {
        // Lobby rendering needs the same initial visibility as a run. Start runs
        // after scene Awake initialization and does not start or reset gameplay.
        if (!enabled || Running || route == null || route.segments == null || route.segments.Length == 0 || Player == null) return;
        Distance = 0; CurrentSegment = 0;
        RefreshVisibility();
    }
    public void BeginRun()
    {
        if (!enabled || route == null || route.segments.Length == 0 || Player == null) return;
        AbortTransfer();
        Active = this; Running = true; Distance = Elapsed = FloorElapsed = 0; CurrentSegment = 0;
        GoalClaimed = ShieldReady = false; grabbedUntil = 0; LiftCount = 0; LastLiftSeconds = 0; Timeline.Clear();
        notices.Clear(); visibilityClock = 0;
        foreach (var choice in choices) choice.ResetForRun(this);
        foreach (var encounter in encounters) encounter.ResetForRun(this);
        foreach (var target in targets) target.ResetForRun(this);
        foreach (var hazard in hazards) hazard.ResetForRun(this);
        foreach (var lift in lifts) lift.ResetForRun();
        foreach (var goal in goals) goal.ResetForRun(this);
        SampleCurrent(0, out var center, out var forward);
        Player.ApplyContinuousRoutePose(center + Vector3.up * footOffset, forward, 0);
        RefreshVisibility();
        Record("run begin chapter " + chapter);
        Announce(chapter == 4 ? "잠실 · 하늘의 신발" : "슈 타워 · 최상층으로", chapter == 4 ? "신발이 있는 탑으로 향하세요" : "매장을 지나 최상층의 운동화를 찾으세요", 4);
    }
    private void Update()
    {
        if (!Running || Player == null) return;
        if (Player.currentHealth <= 0 || CanvasScript.isGameOver && !GoalClaimed) { StopRun(); return; }
        if (!TimeManager.isGameRunning || TimeManager.timeFactor <= 0) return;
        float dt = Time.deltaTime * TimeManager.timeFactor; Elapsed += dt;
        if (!IsTransferring) FloorElapsed += dt;
        foreach (var choice in choices) choice.Tick();
        foreach (var encounter in encounters) encounter.Tick();
        foreach (var target in targets) target.Tick(dt);
        foreach (var hazard in hazards) hazard.Tick(dt);
        RenderAnnouncement();
        visibilityClock -= dt;
        if (visibilityClock <= 0) { visibilityClock = .2f; RefreshVisibility(); }
        if (shieldAura != null)
        {
            shieldAura.gameObject.SetActive(ShieldReady);
            if (ShieldReady) shieldAura.position = Player.transform.position + Vector3.up;
        }
    }
    private void Advance(float dt)
    {
        if (dt <= 0 || Player == null || !TimeManager.isGameRunning || Player.currentHealth <= 0) return;
        if (IsTransferring) { AdvanceLift(dt); return; }
        float lane = Lane;
        float next = Mathf.Min(route.Length, Distance + RouteSpeed * CrowdMoveScale * dt);
        foreach (var choice in choices) if (!choice.Selected && choice.OnCurrentFloor && next >= choice.distance) choice.Commit(lane);
        lane = ConstrainLane(Player, lane);
        foreach (var encounter in encounters)
            if (encounter.requiredToProceed && encounter.Eligible && !encounter.Complete && encounter.floor == CurrentFloor && next >= encounter.stopDistance)
                next = Mathf.Min(next, Mathf.Max(Distance, encounter.stopDistance));
        foreach (var target in targets)
            if (target.Blocks(next, lane)) next = Mathf.Min(next, Mathf.Max(Distance, target.distance - 6));
        // Consume the current frame across internal waypoints. Clamping every
        // short piece discarded travel and produced a repeated stop/turn cadence.
        while (next >= route.SegmentEnd(CurrentSegment) && CurrentSegment + 1 < route.segments.Length)
        {
            bool connected = false;
            foreach (var lift in lifts) if (lift.afterSegment == CurrentSegment) { connected = true; break; }
            var here = route.segments[CurrentSegment]; var following = route.segments[CurrentSegment + 1];
            if (connected || here.floor != following.floor || (here.end - following.start).sqrMagnitude > .0001f) break;
            EnterSegment(CurrentSegment + 1);
        }
        float boundary = route.SegmentEnd(CurrentSegment);
        bool atEnd = next >= boundary - .001f;
        if (atEnd) next = boundary;
        Distance = next;
        SampleCurrent(Distance, out var center, out var forward);
        Player.ApplyContinuousRoutePose(center + Vector3.up * footOffset, forward, lane);
        if (!atEnd) return;
        bool hasConnection = false;
        foreach (var lift in lifts)
        {
            if (lift.afterSegment != CurrentSegment) continue;
            hasConnection = true;
            if (!lift.Ready(this)) continue;
            int destination = lift.Destination(CurrentSegment);
            if (destination < 0 || destination >= route.segments.Length || destination == CurrentSegment) continue;
            BeginLift(lift, destination); return;
        }
        // An authored connection remains closed until its time/combat gate opens.
        // It must never fall through into the unselected branch.
        if (hasConnection || CurrentSegment + 1 >= route.segments.Length) return;
        EnterSegment(CurrentSegment + 1);
    }
    private void EnterSegment(int segment)
    {
        int previousFloor = CurrentFloor;
        CurrentSegment = segment;
        if (CurrentFloor != previousFloor) FloorElapsed = 0;
    }
    // Human-scale mall actors are below the shark's mouth. Match only the
    // vertical pitch of the existing firing lane; do not auto-turn across lanes.
    public Vector3 AimAtRoleHeight(Vector3 muzzle, Vector3 direction)
    {
        if (!Running || IsTransferring || Player == null) return direction;
        Vector3 forward = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        if (forward.sqrMagnitude < .5f) return direction;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float closest = 28; float height = muzzle.y; bool found = false;
        foreach (var encounter in encounters)
        {
            if (!encounter.Activated || encounter.Complete || !encounter.Eligible || encounter.floor != CurrentFloor) continue;
            foreach (var actor in encounter.actors)
            {
                if (actor == null || actor.IsDead || !actor.gameObject.activeInHierarchy || actor.GetComponent<Chapter45RoleAction>() == null) continue;
                var collider = actor.GetComponent<Collider>();
                if (collider == null || !collider.enabled) continue;
                Vector3 delta = collider.bounds.center - muzzle;
                float ahead = Vector3.Dot(delta, forward);
                if (ahead <= .3f || ahead >= closest || Mathf.Abs(Vector3.Dot(delta, right)) > .55f) continue;
                closest = ahead; height = collider.bounds.center.y; found = true;
            }
        }
        return found ? (forward * closest + Vector3.up * (height - muzzle.y)).normalized : direction;
    }
    public void Sample(float distance, out Vector3 center, out Vector3 forward)
        => SampleAt(route.SegmentAt(distance), distance, out center, out forward);
    public void SampleOnCurrentDeck(float distance, out Vector3 center, out Vector3 forward)
        => SampleCurrent(distance, out center, out forward);
    private void SampleCurrent(float distance, out Vector3 center, out Vector3 forward)
        => SampleAt(CurrentSegment, distance, out center, out forward);
    private void SampleAt(int segment, float distance, out Vector3 center, out Vector3 forward)
    {
        route.SampleSegment(segment, distance, out center, out forward);
        foreach (var choice in choices)
        {
            if (!choice.Selected || choice.kind != Chapter45Choice.ChoiceKind.RouteFork) continue;
            float offset = choice.Offset(distance, choice.Selection);
            if (Mathf.Abs(offset) < .0001f) continue;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            center += right * offset;
            float slope = (choice.Offset(distance + .25f, choice.Selection) - choice.Offset(distance - .25f, choice.Selection)) / .5f;
            forward = (forward + right * slope).normalized;
        }
    }
    private void BeginLift(Chapter45Lift lift, int destinationSegment)
    {
        riding = lift; rideDestinationSegment = destinationSegment; rideElapsed = 0;
        rideDuration = lift.exactDuration ? Mathf.Clamp(lift.duration, 1, 15) : Setting("c45_liftSeconds", lift.duration, 1, 7);
        rideFrom = Player.transform.position;
        escalatorBoardingCenter = route.segments[CurrentSegment].end + Vector3.up * footOffset;
        // Preserve the existing formation before the player turns from the
        // aisle tangent toward the actual escalator ramp.
        carriedHelpers.Clear();
        foreach (var helper in FindObjectsByType<ExtraHelpBuffScript>(FindObjectsSortMode.None))
            if (helper.Owner == Player && helper.currentHealth > 0)
                carriedHelpers.Add(new CarriedHelper { helper = helper, localOffset = Quaternion.Inverse(Player.transform.rotation) * (helper.transform.position - Player.transform.position) });
        route.SampleSegment(CurrentSegment, Distance, out _, out rideForward);
        route.SampleSegment(rideDestinationSegment, route.SegmentStart(rideDestinationSegment), out var destination, out landingForward);
        rideTo = destination + Vector3.up * footOffset;
        if (lift.escalator)
        {
            // Face the actual diagonal ramp, not the curved aisle tangent.
            rideForward = Vector3.ProjectOnPlane(rideTo - (lift.continuousEscalator ? escalatorBoardingCenter : rideFrom), Vector3.up).normalized;
            Player.ApplyContinuousRoutePose(rideFrom, rideForward, 0);
            foreach (var carried in carriedHelpers)
                if (carried.helper != null)
                    carried.helper.transform.position = rideFrom + Player.transform.rotation * carried.localOffset;
        }
        movementBeforeRide = Player.movement; Player.movement = false;
        Player.SetBucketShootBlock(this, true);
        BulletScript.RetireChapterShots(Player);
        if (playerBody != null) { bodyWasKinematic = playerBody.isKinematic; playerBody.isKinematic = true; }
        if (heightFollower != null) { heightWasEnabled = heightFollower.enabled; heightFollower.enabled = false; }
        capturedRide = true;
        // A ramp can rise into the ordinary low trailing camera. Keep the player
        // visible above the steps, and restore the exact authored view on exit.
        if (lift.escalator && Camera.main != null)
        {
            transferCamera = Camera.main.GetComponent<StableGameplayCamera>();
            if (transferCamera != null)
            {
                cameraOffsetBeforeRide = transferCamera.yawRelativeOffset;
                cameraRotationBeforeRide = transferCamera.yawRelativeRotation;
                if (!lift.continuousEscalator)
                {
                    transferCamera.yawRelativeOffset = new Vector3(-1.5f, 5, -7);
                    transferCamera.yawRelativeRotation = Quaternion.LookRotation(-transferCamera.yawRelativeOffset + Vector3.up * 1.5f);
                    transferCamera.SnapToTarget();
                }
                cameraAdjustedForRide = true;
            }
        }
        lift.SetDoorOpening(0);
        foreach (var hazard in hazards) if (hazard.floor == CurrentFloor) hazard.Cancel();
        notices.Suspend();
        Record("lift board " + lift.name + " floor " + CurrentFloor);
        Announce(lift.escalator ? "에스컬레이터 이동" : "엘리베이터 이동", lift.destinationLabel, rideDuration);
        RefreshVisibility();
    }
    private void AdvanceLift(float dt)
    {
        rideElapsed = Mathf.Min(rideDuration, rideElapsed + dt);
        float t = rideElapsed / rideDuration;
        if (riding.continuousEscalator && transferCamera != null)
        {
            // Ease inside the aligned stair opening, then return exactly to the
            // saved corridor view. A long trailing camera otherwise crosses the
            // upper-floor edge while the shark is still below that floor.
            float weight = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .12f))
                * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.82f, 1, t)));
            transferCamera.yawRelativeOffset = Vector3.Lerp(cameraOffsetBeforeRide, new Vector3(0, 6, -7.5f), weight);
            transferCamera.yawRelativeRotation = Quaternion.Slerp(cameraRotationBeforeRide, Quaternion.Euler(28, 0, 0), weight);
        }
        Vector3 position = riding.continuousEscalator && riding.escalator
            ? Chapter45Rules.EscalatorPosition(escalatorBoardingCenter, rideTo, t)
                + (rideFrom - escalatorBoardingCenter) * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .12f)))
            : Chapter45Rules.LiftPosition(rideFrom, rideTo, rideElapsed, rideDuration);
        Quaternion yaw = Quaternion.Slerp(Quaternion.LookRotation(rideForward), Quaternion.LookRotation(landingForward), Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.7f, 1, t)));
        Player.ApplyContinuousRoutePose(position, yaw * Vector3.forward, 0);
        foreach (var carried in carriedHelpers)
            if (carried.helper != null) carried.helper.transform.position = position + yaw * carried.localOffset;
        riding.MovePlatform(position - rideFrom);
        riding.SetDoorOpening(Mathf.InverseLerp(.9f, 1, t));
        if (t < 1) return;
        var arrived = riding;
        EnterSegment(rideDestinationSegment); Distance = route.SegmentStart(CurrentSegment);
        arrived.Used = true; LiftCount++; LastLiftSeconds = rideElapsed;
        ReleaseTransfer();
        Player.ApplyContinuousRoutePose(rideTo, landingForward, 0);
        foreach (var encounter in encounters) if (encounter.floor != CurrentFloor) encounter.Retire();
        Record("lift land " + arrived.name + " seconds=" + LastLiftSeconds.ToString("F2") + " floor=" + CurrentFloor);
        Announce(arrived.destinationLabel, "문이 열렸습니다 · 앞으로 이동하세요", 2);
        RefreshVisibility();
    }
    private void ReleaseTransfer()
    {
        riding = null;
        if (cameraAdjustedForRide && transferCamera != null)
        {
            transferCamera.yawRelativeOffset = cameraOffsetBeforeRide;
            transferCamera.yawRelativeRotation = cameraRotationBeforeRide;
            transferCamera.SnapToTarget();
        }
        cameraAdjustedForRide = false; transferCamera = null;
        notices.Resume(Elapsed);
        if (!capturedRide) return;
        capturedRide = false;
        if (Player != null) { Player.SetBucketShootBlock(this, false); Player.movement = movementBeforeRide && Player.currentHealth > 0; }
        if (playerBody != null) playerBody.isKinematic = bodyWasKinematic;
        if (heightFollower != null) heightFollower.enabled = heightWasEnabled;
        foreach (var carried in carriedHelpers)
            if (carried.helper != null) carried.helper.GetComponent<Chapter45HelperFollower>()?.Rebase(this);
        carriedHelpers.Clear();
    }
    private void AbortTransfer()
    {
        if (riding != null) { riding.SetDoorOpening(1); Record("lift interrupted " + riding.name); }
        ReleaseTransfer();
        if (Player != null) Player.SetBucketShootBlock(this, false);
    }
    public bool ChoiceMatches(int index, int selection)
        => index < 0 || index < choices.Length && choices[index].Selected && choices[index].Selection == selection;
    public bool ChoiceEncountersComplete(Chapter45Choice choice)
    {
        int index = Array.IndexOf(choices, choice);
        foreach (var encounter in encounters) if (encounter.choiceIndex == index && encounter.requiredToProceed && encounter.Eligible && !encounter.Complete) return false;
        foreach (var target in targets) if (target.choiceIndex == index && target.requiredForGoal && target.Eligible && !target.Open) return false;
        return true;
    }
    public void GrantShield() { ShieldReady = true; Record("shield granted"); }
    public void Announce(string title, string detail, float seconds)
    { notices.Information(title, detail, seconds, Elapsed); RenderAnnouncement(); }
    public void AnnounceReward(string title, string detail, float seconds)
    { notices.Reward(title, detail, seconds, Elapsed); RenderAnnouncement(); }
    public void AnnounceWarning(string title, string detail, float seconds)
    { notices.Warning(title, detail, seconds, Elapsed); RenderAnnouncement(); }
    private void RenderAnnouncement()
    {
        if (hud == null) return;
        if (!IsTransferring && route != null && Distance >= route.SegmentEnd(CurrentSegment) - .05f)
        {
            foreach (var lift in lifts)
            {
                if (lift.afterSegment != CurrentSegment || !lift.Eligible(this) || lift.Ready(this)) continue;
                string reason = FloorElapsed < lift.minimumFloorSeconds
                    ? Mathf.CeilToInt(lift.minimumFloorSeconds - FloorElapsed) + "초 후 출발"
                    : lift.requiredHoldout != null && !lift.requiredHoldout.Completed ? "상영관 생존을 마치면 이동합니다"
                    : "남은 적을 정리하세요 · 좌우 이동과 사격이 가능합니다";
                ShowAnnouncement(lift.destinationLabel, reason); return;
            }
        }
        if (IsTransferring) { ShowAnnouncement(riding.escalator ? "에스컬레이터 이동" : "엘리베이터 이동", riding.destinationLabel); return; }
        // Re-show retained notices each active update: the shared HUD hides its
        // panel while paused, but the chapter's scaled clock has not advanced.
        if (notices.TryRead(Elapsed, out var notice)) ShowAnnouncement(notice.Title, notice.Detail);
        else
        {
            // The first mall floor has a30-active-second gate. Show why forward
            // travel paused instead of leaving a cleared aisle silently blocked.
            foreach (var lift in lifts)
                if (lift.afterSegment == CurrentSegment && lift.Eligible(this)
                    && FloorElapsed < lift.minimumFloorSeconds
                    && Distance >= route.SegmentEnd(CurrentSegment) - .05f)
                {
                    int seconds = Mathf.CeilToInt(lift.minimumFloorSeconds - FloorElapsed);
                    ShowAnnouncement(lift.escalator ? "에스컬레이터 준비" : "엘리베이터 준비",
                        seconds + "초 후 · " + lift.destinationLabel);
                    return;
                }
            hud.Clear(this);
        }
    }
    private void ShowAnnouncement(string title, string detail)
    {
        title = Chapter45PresentationText.Localize(title);
        detail = Chapter45PresentationText.Localize(detail);
        // Runtime-only sizing for this chapter HUD. Preserve the top edge and
        // title position, extending downward only for concurrent danger lines.
        var panel = hud.panel != null ? hud.panel.GetComponent<RectTransform>() : null;
        if (panel != null && hud.description != null && hud.title != null)
        {
            if (!announcementLayoutCaptured)
            {
                announcementPanelSize = panel.sizeDelta;
                announcementDetailSize = hud.description.rectTransform.sizeDelta;
                announcementTitlePosition = hud.title.rectTransform.anchoredPosition;
                announcementLayoutCaptured = true;
            }
            int lines = string.IsNullOrEmpty(detail) ? 1 : detail.Split('\n').Length;
            float extra = Mathf.Max(0, lines - 2) * 26f;
            panel.sizeDelta = announcementPanelSize + Vector2.up * extra;
            hud.description.rectTransform.sizeDelta = announcementDetailSize + Vector2.up * extra;
            hud.title.rectTransform.anchoredPosition = announcementTitlePosition + Vector2.up * (extra * .5f);
        }
        hud.Show(this, title, detail, 3);
    }
    public void Record(string message) => Timeline.Add(Elapsed.ToString("F2") + "s @" + Distance.ToString("F1") + "m " + message);
    public void ClaimGoal(Chapter45Goal goal)
    {
        if (GoalClaimed || !Running || goal == null || !goal.Claimed || !RequiredEncountersComplete) return;
        GoalClaimed = true; Record(goal.offering ? "offering physically collected" : "tower threshold physically crossed");
        notices.Clear(); hud?.Clear(this); // Actual result panel owns terminal story feedback.
        foreach (var hazard in hazards) hazard.Cancel();
        AbortTransfer(); Running = false;
        FindFirstObjectByType<CanvasScript>()?.YouWin();
    }
    private void RefreshVisibility()
    {
        int nextFloor = IsTransferring ? route.segments[rideDestinationSegment].floor : CurrentFloor;
        foreach (var group in scenery)
        {
            bool deckVisible = group.floor < 0 || group.floor == CurrentFloor || IsTransferring && group.floor == nextFloor;
            // Do not render another connection across the active escalator camera.
            var connection = group.GetComponent<Chapter45Lift>();
            if (IsTransferring && connection != null && connection != riding) deckVisible = false;
            float visibleDistance = IsTransferring && group.floor == nextFloor ? route.SegmentStart(rideDestinationSegment) : Distance;
            bool nearby = group.alwaysVisible || group.floor < 0 && group.endDistance <= group.startDistance || visibleDistance >= group.startDistance - 100 && visibleDistance <= group.endDistance + 100;
            group.SetVisible(deckVisible && nearby && !(IsTransferring && (group.hideDuringTransfer || group.hideWhileArriving && group.floor == nextFloor)) && (group.hiddenOnFloor < 0 || group.hiddenOnFloor != CurrentFloor));
        }
    }
    private void StopRun()
    {
        AbortTransfer(); Running = ShieldReady = false; notices.Clear();
        foreach (var hazard in hazards) hazard.Cancel();
        if (shieldAura != null) shieldAura.gameObject.SetActive(false);
        hud?.Clear(this);
    }
    private void OnDisable()
    {
        StopRun();
        foreach (var group in scenery) group.Restore();
        if (Active == this) Active = null;
    }
}
