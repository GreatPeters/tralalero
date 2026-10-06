using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Opt-in behavior for authored mall/street actors. The ordinary enemy component
// continues to own health, bullets, rewards and death. This component owns only
// role motion, readable wind-ups and its own bounded contact damage.
[DisallowMultipleComponent, DefaultExecutionOrder(190)]
public sealed class Chapter45RoleAction : MonoBehaviour
{
    public enum Role { Tourist, Guard, ShoppingBags, LinkedCouple, Basketball, Golfer, StudentGrab, PlateThrower, TicketCrowd, PopcornThrower, Cleaner, CalledQueue, Manager, Preacher, Promoter, PhoneShopper }
    public enum ActionPhase { Moving, Warning, Acting, Recovering }
    public Role role;
    public Chapter45Director director;
    public Chapter45Encounter encounter;
    public Chapter45RoleAction linkedLeader;
    public Vector3 linkedOffset;
    public Transform warningMarker, heldProp;
    // Authored only on actors with verified role-specific skeletal clips.
    public bool animationBeginsDuringWarning, propFollowsSkeleton;
    public Transform projectileOrigin;
    public GameObject projectileTemplate;
    public float moveSpeed = 2.2f, actionDamage = 3, cooldown = 3.8f, warningSeconds = 1.1f, phaseOffset;
    public string warningText;
    public int callNumber;
    private int announcedCall = -1;
    private Chapter45RoleAction queueLeader;
    private float queueStartedAt;
    private float observedDistance, observedForwardSpeed, recoveryDistance;
    public ActionPhase Phase { get; private set; }
    public int Actions { get; private set; }
    public int Contacts { get; private set; }
    public float PhoneFilmingWeight { get; private set; }
    private float phoneReactionStarted = -1;
    private Vector3 phoneStepAway;
    public bool CanAct => director != null && encounter != null && encounter.Activated && encounter.Eligible
        && !encounter.Complete && (encounter.requiredToProceed || director.Distance <= encounter.stopDistance + 10) && director.Running && director.Player != null && director.Player.currentHealth > 0
        && !director.IsTransferring && director.CurrentFloor == encounter.floor && director.gameObject.scene == gameObject.scene
        && combat != null && !combat.IsDead && TimeManager.isGameRunning && TimeManager.timeFactor > 0;

    private EnemyScript_space combat;
    private EnemyEventController controller;
    private Animator animator;
    private Vector3 origin, right, lockedTarget, actionStart, recoveryAnchor;
    private Quaternion propRest;
    private Mesh chargeWarningMesh, authoredWarningMesh;
    private Quaternion authoredWarningRotation;
    private Vector3 authoredWarningScale;
    private bool warningShapeCaptured;
    private float age, remaining, lastContact = -100, floorY;
    private int animationHash;
    private bool hitThisAction;
    private bool authoredAnimatorEnabled;
    private float authoredAnimatorSpeed;
    private static readonly int Filming = Animator.StringToHash("phone_filming");
    private static readonly int Walk = Animator.StringToHash(ForwardEnemyAnimationContract.Walk);
    private static readonly int Idle = Animator.StringToHash(ForwardEnemyAnimationContract.Idle);
    private static readonly int Attack = Animator.StringToHash(ForwardEnemyAnimationContract.AttackOnce);

    private bool Thrower => role == Role.PlateThrower || role == Role.PopcornThrower || role == Role.Promoter;
    private bool Crowd => role == Role.Tourist || role == Role.ShoppingBags || role == Role.LinkedCouple
        || role == Role.TicketCrowd || role == Role.CalledQueue || role == Role.PhoneShopper;
    private float Reach => role == Role.Golfer || role == Role.Cleaner ? 2.6f : 1.55f;
    private void Awake() { combat = GetComponent<EnemyScript_space>(); controller = GetComponent<EnemyEventController>(); animator = GetComponentInChildren<Animator>(true); if (animator != null) { authoredAnimatorEnabled = animator.enabled; authoredAnimatorSpeed = animator.speed; } }
    private void OnEnable()
    {
        if (combat == null) Awake();
        origin = transform.position; floorY = origin.y; right = transform.right;
        announcedCall = -1; queueLeader = null; queueStartedAt = director != null ? director.Elapsed : 0;
        observedDistance = director != null ? director.Distance : 0; observedForwardSpeed = 0;
        age = 0; remaining = 1.4f + phaseOffset; lastContact = -100; Actions = Contacts = 0;
        Phase = ActionPhase.Moving; hitThisAction = false; animationHash = 0;
        phoneReactionStarted = -1; PhoneFilmingWeight = 0; phoneStepAway = Vector3.zero;
        if (heldProp != null) propRest = heldProp.localRotation;
        ShowMarker(false);
    }
    private void OnDisable() { ShowMarker(false); RestoreWarningShape(); if (heldProp != null) heldProp.localRotation = propRest; if (animator != null) { animator.enabled = authoredAnimatorEnabled; animator.speed = authoredAnimatorSpeed; } }
    private void Update()
    {
        if (animator != null)
        {
            animator.enabled = authoredAnimatorEnabled && TimeManager.isGameRunning && TimeManager.timeFactor > 0 && director != null && director.Running && !director.IsTransferring && encounter != null && director.CurrentFloor == encounter.floor;
            animator.speed = authoredAnimatorSpeed * Mathf.Max(0, TimeManager.timeFactor);
        }
        TickRole(Time.deltaTime * Mathf.Max(0, TimeManager.timeFactor));
    }
    public void TickRole(float dt)
    {
        if (!CanAct) { if (combat == null || combat.IsDead || director == null || encounter == null || !director.Running || director.IsTransferring || director.CurrentFloor != encounter.floor) ShowMarker(false); return; }
        if (dt <= 0) return;
        age += dt; remaining -= dt;
        if (role == Role.Preacher && !encounter.requiredToProceed)
        {
            float speed = Mathf.Clamp((director.Distance - observedDistance) / dt, 0, director.RouteSpeed * 2);
            observedForwardSpeed = Mathf.Lerp(observedForwardSpeed, speed, 1 - Mathf.Exp(-10 * dt));
            observedDistance = director.Distance;
        }
        Vector3 player = director.Player.transform.position; player.y = floorY;
        if (Crowd) { MoveCrowd(dt); return; }
        if (Phase == ActionPhase.Warning)
        {
            Face(lockedTarget - transform.position);
            if (remaining > 0) return;
            Phase = ActionPhase.Acting; remaining = Thrower ? .25f : .65f;
            actionStart = transform.position; Actions++; Animate(Attack, !animationBeginsDuringWarning);
            if (Thrower) Fire();
            director.Record("role action " + role + " " + name);
        }
        if (Phase == ActionPhase.Acting)
        {
            if (!Thrower)
            {
                float t = Mathf.Clamp01(1 - remaining / .65f);
                if (role == Role.Basketball || role == Role.StudentGrab || role == Role.Preacher)
                    transform.position = Vector3.Lerp(actionStart, lockedTarget, t);
                if (heldProp != null && !propFollowsSkeleton) heldProp.localRotation = propRest * Quaternion.Euler(0, Mathf.Lerp(-65, 65, t), 0);
                // The strike only affects an overlapping spatial area. A warning
                // followed by a timer is never enough to damage a distant player.
                Vector3 strike = transform.position + transform.forward * Reach * .55f;
                strike.y = player.y;
                if (!hitThisAction && (strike - player).sqrMagnitude < Mathf.Pow(Reach * .55f + .45f, 2)) ContactPlayer(director.Player);
            }
            if (remaining > 0) return;
            ShowMarker(false); Phase = ActionPhase.Recovering; remaining = cooldown;
            // Commit the recovery lane once. Mirroring every lateral player move
            // kept an enemy permanently off the aim line throughout its cooldown.
            Vector3 recoveryForward = Vector3.ProjectOnPlane(director.Player.transform.forward, Vector3.up).normalized;
            Vector3 recoverySide = Vector3.Cross(Vector3.up, recoveryForward);
            float recoverySign = Vector3.Dot(origin - player, recoverySide) >= 0 ? 1 : -1;
            recoveryAnchor = player + recoveryForward * 5.5f + recoverySide * recoverySign * 1.4f;
            recoveryDistance = director.Distance;
            if (heldProp != null) heldProp.localRotation = propRest;
            Animate(Idle); return;
        }
        if (Phase == ActionPhase.Recovering)
        {
            if (!Thrower)
            {
                // The route is an advancing forward shooter. Return to an
                // attackable position in front after a grab/swing, including
                // when the player passed the original wind-up position.
                Vector3 forward = Vector3.ProjectOnPlane(director.Player.transform.forward, Vector3.up).normalized;
                // Preserve forward attackability for an advancing player without
                // chasing their crosshair sideways during the recovery window.
                float advance = Mathf.Max(0, director.Distance - recoveryDistance);
                recoveryAnchor += forward * advance;
                recoveryDistance = director.Distance;
                transform.position = Vector3.MoveTowards(transform.position, recoveryAnchor, (moveSpeed + 1) * dt);
                Face(player - transform.position); Animate(Walk);
            }
            if (remaining > 0) return; Phase = ActionPhase.Moving;
        }
        float range = Vector3.Distance(transform.position, player);
        if (Thrower)
        {
            Face(player - transform.position);
            if (range <= 23 && remaining <= 0) BeginWarning(player);
            return;
        }
        float beginRange = role == Role.Basketball ? 9 : role == Role.StudentGrab || role == Role.Preacher ? 4.5f : Reach + .4f;
        if (role == Role.Preacher && !encounter.requiredToProceed)
            beginRange = Mathf.Max(beginRange, observedForwardSpeed * (Mathf.Max(.8f, warningSeconds) + .25f) + 2.2f);
        if (range > beginRange)
        {
            var to = player - transform.position; Face(to);
            transform.position = Vector3.MoveTowards(transform.position, player, moveSpeed * dt);
            Animate(Walk);
        }
        else if (remaining <= 0) BeginWarning(player);
    }
    private void MoveCrowd(float dt)
    {
        if (role == Role.PhoneShopper) { MoveFilmingCrowd(dt); return; }
        Vector3 next;
        if (linkedLeader != null && linkedLeader.CanAct)
        { next = linkedLeader.transform.position + linkedOffset; Animate(Walk); }
        else
        {
            float range = role == Role.ShoppingBags ? 1.8f : role == Role.LinkedCouple ? 1.6f : .8f;
            if (role == Role.CalledQueue)
            {
                // One authored queue shares its call clock. Per-person phase offsets
                // must not move followers before the displayed number is called.
                if (queueLeader == null)
                    foreach (var actor in encounter.actors)
                    {
                        var candidate = actor != null ? actor.GetComponent<Chapter45RoleAction>() : null;
                        if (candidate != null && candidate.role == Role.CalledQueue)
                        { queueLeader = candidate; break; }
                    }
                if (queueLeader == null) queueLeader = this;
                float queueAge = Mathf.Max(0, director.Elapsed - queueLeader.queueStartedAt);
                float phase = Mathf.Repeat(queueAge, 7);
                int cycle = Mathf.FloorToInt(queueAge / 7);
                if (phase >= 1.5f && queueLeader.announcedCall != cycle)
                {
                    queueLeader.announcedCall = cycle;
                    string number = (Mathf.Max(1, queueLeader.callNumber) + cycle).ToString("000");
                    director.AnnounceWarning("주문 " + number + "번", "호출된 대기줄이 이동합니다", 1.2f);
                    director.Record("queue called " + encounter.name + " number=" + number);
                }
                // Keep the whole line still for 1.2 active seconds after the call,
                // then cross together and return continuously before the next call.
                float call = phase < 5.5f
                    ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((phase - 2.7f) / 1.6f))
                    : 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((phase - 5.5f) / 1.5f));
                next = origin + right * (call * 2.2f);
                Animate((next - transform.position).sqrMagnitude > .00001f ? Walk : Idle);
            }
            else { next = origin + right * (Mathf.Sin(age * .65f + phaseOffset) * range); Animate(role == Role.PhoneShopper ? Idle : Walk); }
        }
        next.y = floorY; Vector3 delta = next - transform.position;
        // Keep an adjacent linked pair facing the authored aisle direction.
        // Their shared side-step must not turn the fixed lateral link into a
        // front-to-back formation that the arm cannot physically reach.
        if (delta.sqrMagnitude > .001f && role != Role.PhoneShopper && role != Role.LinkedCouple) Face(delta);
        transform.position = Vector3.MoveTowards(transform.position, next, moveSpeed * dt);
        if (role == Role.PhoneShopper) Face(director.Player.transform.position - transform.position);
    }
    private void MoveFilmingCrowd(float dt)
    {
        Vector3 toPlayer = director.Player.transform.position - transform.position;
        toPlayer.y = 0;
        if (phoneReactionStarted < 0 && toPlayer.sqrMagnitude <= 28 * 28)
        {
            phoneReactionStarted = age;
            phoneStepAway = -toPlayer.normalized;
            director.Record("phone crowd notices " + name);
        }
        float reaction = phoneReactionStarted < 0 ? 0 : age - phoneReactionStarted;
        PhoneFilmingWeight = phoneReactionStarted < 0 ? 0
            : Mathf.SmoothStep(0, 1, Mathf.Clamp01((reaction - .12f) / .65f));
        // A short recoil, then a bounded sideways adjustment for a clear shot.
        // Keep the original .8m lateral envelope and the same movement speed.
        float recoil = Mathf.Sin(Mathf.Clamp01(reaction / .8f) * Mathf.PI) * .22f;
        float side = Mathf.Sin(phaseOffset + .55f) * .55f * PhoneFilmingWeight;
        Vector3 next = origin + right * side + phoneStepAway * recoil;
        next.y = floorY;
        transform.position = Vector3.MoveTowards(transform.position, next, moveSpeed * dt);
        Face(director.Player.transform.position - transform.position);
        Animate(PhoneFilmingWeight > .7f ? Filming : Idle); // Own-rig standing idle plants the feet while the arms film.
    }

    private void BeginWarning(Vector3 player)
    {
        lockedTarget = player;
        if (role == Role.Preacher && !encounter.requiredToProceed)
        {
            // A street runner keeps advancing during the wind-up. Telegraph a
            // fixed interception point, so straight running meets the grab but
            // a lateral response still escapes; never home after the warning.
            director.SampleOnCurrentDeck(director.Distance, out _, out var forward);
            lockedTarget += forward * observedForwardSpeed * (Mathf.Max(.8f, warningSeconds) + .35f);
        }
        if (role == Role.Basketball || role == Role.StudentGrab || role == Role.Preacher)
            lockedTarget -= (player - transform.position).normalized * 1.1f;
        hitThisAction = false; Phase = ActionPhase.Warning; remaining = Mathf.Max(.8f, warningSeconds);
        Face(lockedTarget - transform.position); Animate(animationBeginsDuringWarning ? Attack : Idle, animationBeginsDuringWarning);
        if (warningMarker != null)
        {
            RestoreWarningShape();
            warningMarker.position = lockedTarget + Vector3.up * .05f;
            if (role == Role.Basketball || role == Role.StudentGrab || role == Role.Preacher) ConfigureChargeWarning();
            else if (role == Role.Cleaner) ConfigureCleanerWarning();
            ShowMarker(true);
        }
        string notice = role == Role.Basketball ? "돌진 예고 · 주황 경로 옆으로 피하세요" : warningText;
        if (!string.IsNullOrEmpty(notice)) director.AnnounceWarning("행동 예고", notice, remaining);
        director.Record("role warning " + role + " " + name);
    }
    // A charge or moving grab can hit along its fixed travel segment. A circle at
    // the destination alone misleadingly leaves the approach looking safe.
    // Replace only this role's visual with a conservative ground corridor;
    // movement, aim lock, warning time, contact radius and damage stay unchanged.
    private void ConfigureChargeWarning()
    {
        var filter = warningMarker.GetComponent<MeshFilter>();
        if (filter == null) return;
        CaptureWarningShape(filter);
        Vector3 axis = Vector3.ProjectOnPlane(lockedTarget - transform.position, Vector3.up);
        if (axis.sqrMagnitude < .001f) axis = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        axis.Normalize();
        Vector3 lateral = Vector3.Cross(Vector3.up, axis);
        float padding = Mathf.Max(1.5f, Reach * .55f + .45f);
        // Include the actor capsule contact behind the starting position, too.
        Vector3 from = transform.position - axis * padding;
        Vector3 to = lockedTarget + axis * (Reach * .55f + padding);
        from.y = to.y = floorY + .05f;
        warningMarker.position = (from + to) * .5f;
        warningMarker.rotation = Quaternion.LookRotation(axis, Vector3.up);
        if (chargeWarningMesh == null)
        {
            chargeWarningMesh = new Mesh { name = "Runtime charge warning corridor" };
            chargeWarningMesh.vertices = new Vector3[4];
            chargeWarningMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        }
        chargeWarningMesh.vertices = new[]
        {
            warningMarker.InverseTransformPoint(from - lateral * padding),
            warningMarker.InverseTransformPoint(to - lateral * padding),
            warningMarker.InverseTransformPoint(to + lateral * padding),
            warningMarker.InverseTransformPoint(from + lateral * padding)
        };
        chargeWarningMesh.RecalculateNormals(); chargeWarningMesh.RecalculateBounds();
        filter.sharedMesh = chargeWarningMesh;
    }
    private void CaptureWarningShape(MeshFilter filter)
    {
        if (warningShapeCaptured) return;
        authoredWarningMesh = filter.sharedMesh;
        authoredWarningRotation = warningMarker.localRotation;
        authoredWarningScale = warningMarker.localScale;
        warningShapeCaptured = true;
    }
    private void ConfigureCleanerWarning()
    {
        var filter = warningMarker.GetComponent<MeshFilter>();
        var renderer = warningMarker.GetComponent<Renderer>();
        if (filter == null || renderer == null) return;
        CaptureWarningShape(filter);
        // The stationary mop strikes in front of its owner, not around the
        // player's old position. Keep the existing disk and material, but
        // show the actual strike area even when the hero obscures the tool.
        Vector3 strike = transform.position + transform.forward * Reach * .55f;
        warningMarker.position = new Vector3(strike.x, floorY + .05f, strike.z);
        float currentRadius = Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.z);
        if (currentRadius <= .001f) return;
        float ratio = (Reach * .55f + .45f + .1f) / currentRadius;
        Vector3 scale = warningMarker.localScale;
        warningMarker.localScale = new Vector3(scale.x * ratio, scale.y, scale.z * ratio);
    }
    private void RestoreWarningShape()
    {
        if (!warningShapeCaptured || warningMarker == null) return;
        var filter = warningMarker.GetComponent<MeshFilter>();
        if (filter != null) filter.sharedMesh = authoredWarningMesh;
        warningMarker.localRotation = authoredWarningRotation;
        warningMarker.localScale = authoredWarningScale;
    }
    private void OnDestroy()
    {
        RestoreWarningShape();
        if (chargeWarningMesh != null) Destroy(chargeWarningMesh);
    }
    private void Fire()
    {
        if (projectileTemplate == null) return;
        Vector3 from = projectileOrigin != null ? projectileOrigin.position : transform.position + Vector3.up * 1.05f;
        Vector3 aim = lockedTarget + Vector3.up - from;
        int count = role == Role.Promoter ? 5 : role == Role.PopcornThrower ? 3 : 1;
        for (int i = 0; i < count; i++)
        {
            var copy = Instantiate(projectileTemplate, from, projectileTemplate.transform.rotation, encounter.transform);
            copy.name = role + " active projectile"; copy.SetActive(true);
            var projectile = copy.GetComponent<SimpleProjectile>();
            if (projectile == null) { Destroy(copy); continue; }
            projectile.damageCause = PlayerDamageCause.EnemyProjectile;
            float yaw = count == 1 ? 0 : (i - (count-1)*.5f) * (role == Role.Promoter ? 8 : 12);
            projectile.Launch(Quaternion.Euler(0, yaw, 0) * aim, role == Role.Promoter ? 9 : 11, actionDamage, 4, transform);
        }
    }
    // Returning true suppresses legacy health-exchange contact only for this
    // explicitly authored role; health, rewards and death remain in combat.
    public bool ContactPlayer(PlayerScript player)
    {
        if (!CanAct || player != director.Player || !Chapter45Director.CanActorContact(player, transform)) return true;
        if (!Crowd && Phase != ActionPhase.Acting) return true;
        if (hitThisAction && !Crowd || age - lastContact < 1.2f) return true;
        // An absorbed contact still consumes this strike; OnTriggerStay must
        // never turn the same attack into a second hit after a shield is spent.
        lastContact = age; hitThisAction = true;
        float dealt = player.ApplyDamage(actionDamage, PlayerDamageCause.EnemyContact);
        if (dealt <= 0) return true;
        Contacts++;
        if (role == Role.StudentGrab || role == Role.Preacher || role == Role.ShoppingBags || role == Role.LinkedCouple)
            director.ApplyCrowdGrab(.8f);
        director.Record("role contact " + role + " " + name); return true;
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<PlayerScript>(out var player)) ContactPlayer(player);
    }
    private void Animate(int hash, bool restart = false)
    {
        if (animator == null || animator.runtimeAnimatorController == null || !animator.HasState(0, hash) || (!restart && animationHash == hash)) return;
        animator.CrossFadeInFixedTime(hash, .1f, 0, 0); animationHash = hash;
    }
    private void Face(Vector3 direction) { direction.y = 0; if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(direction); }
    private void ShowMarker(bool show) { if (warningMarker != null) warningMarker.gameObject.SetActive(show); }
}
