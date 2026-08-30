using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace IndianOceanAssets.ShooterSurvival
{
    public static class ForwardEnemyAnimationContract
    {
        public const string Idle = "idle";
        public const string AttackLoop = "attack_loop";
        public const string Walk = "walk";
        public const string Run = "run";
        public const string Die = "die";
        public const string AttackOnce = "attack_once";
    }

    public enum EnemyEventMode
    {
        [InspectorName("공격 반복")]
        AttackLoop = 0,

        [InspectorName("공격 한 번")]
        AttackOnce = 5,

        [InspectorName("발사")]
        Shoot = 4,

        [InspectorName("지정 위치 이동 후 공격")]
        MoveToTargetThenAttack = 2,

        [InspectorName("시작점과 지정 위치 왕복")]
        PatrolBetweenStartAndTarget = 1
    }

    public enum EnemyMoveAnimation
    {
        [InspectorName("없음")]
        None = -1,

        [InspectorName("걷기")]
        Walk = 0,

        [InspectorName("달리기")]
        Run = 1
    }

    public enum EnemyEventRuntimeState
    {
        Waiting,
        MovingToTarget,
        MovingToStart,
        Attacking,
        PatrolAttack,
        Dead
    }

    [MovedFrom(
        true,
        sourceNamespace: "IndianOceanAssets.ShooterSurvival",
        sourceAssembly: "Assembly-CSharp",
        sourceClassName: "EnemyMovementController")]
    [AddComponentMenu("Shooter Survival/Enemy Event Controller")]
    [DisallowMultipleComponent]
    public sealed class EnemyEventController : MonoBehaviour, ISerializationCallbackReceiver
    {
        private const float DirectionEpsilonSqr = 0.000001f;
        private const float PatrolAttackFallbackSeconds = 1f;
        private const float AnimationTransitionSeconds = 0.05f;

        private static readonly HashSet<EnemyEventController> ActiveControllers = new();
        private static readonly int IdleStateHash =
            Animator.StringToHash(ForwardEnemyAnimationContract.Idle);
        private static readonly int AttackLoopStateHash =
            Animator.StringToHash(ForwardEnemyAnimationContract.AttackLoop);
        private static readonly int WalkStateHash =
            Animator.StringToHash(ForwardEnemyAnimationContract.Walk);
        private static readonly int RunStateHash =
            Animator.StringToHash(ForwardEnemyAnimationContract.Run);
        private static readonly int DieStateHash =
            Animator.StringToHash(ForwardEnemyAnimationContract.Die);
        private static readonly int AttackOnceStateHash =
            Animator.StringToHash(ForwardEnemyAnimationContract.AttackOnce);

        [InspectorName("이벤트")]
        [Tooltip("적 발동 스팟에 연결된 뒤 플레이어가 스팟에 닿았을 때 실행할 동작입니다.")]
        [FormerlySerializedAs("movementMode")]
        [SerializeField] private EnemyEventMode eventMode = EnemyEventMode.AttackLoop;

        [InspectorName("이동 속도")]
        [Tooltip("이동 이벤트에서 사용하는 초당 이동 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float moveSpeed = 2f;

        [InspectorName("이동 애니메이션")]
        [Tooltip("지정 위치로 이동할 때 재생할 애니메이션입니다.")]
        [SerializeField] private EnemyMoveAnimation moveAnimation = EnemyMoveAnimation.Walk;

        [InspectorName("이동 목표")]
        [Tooltip("이동 후 공격과 왕복 이동이 사용할 목표 위치입니다. 적 자신의 자식은 목표로 사용할 수 없습니다.")]
        [SerializeField] private Transform targetPoint;

        [InspectorName("도착 판정 거리")]
        [Min(0.001f)]
        [SerializeField] private float arrivalDistance = 0.05f;

        private bool initialized;
        private bool patrolAttackStateObserved;
        private bool patrolReturningToStart;
        private bool visualRotationOffsetCaptured;
        private float patrolAttackFallbackRemaining;
        private Vector3 startPosition;
        private Vector3 routeForward;
        private Vector3 routeRight;
        private Quaternion visualRotationOffset = Quaternion.identity;
        private Animator enemyAnimator;
        private EnemyScript_space combat;
        private PlayerScript player;

        public EnemyEventMode EventMode
        {
            get => eventMode;
            set
            {
                if (eventMode == value)
                    return;

                eventMode = value;
                ResetForNewRun();
            }
        }

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        public EnemyMoveAnimation MoveAnimation
        {
            get => moveAnimation;
            set => moveAnimation = value;
        }

        public Transform TargetPoint
        {
            get => targetPoint;
            set => targetPoint = value;
        }

        public bool HasUsableTarget =>
            targetPoint != null && !targetPoint.IsChildOf(transform);
        public EnemyEventRuntimeState RuntimeState { get; private set; }

        private void Awake()
        {
            ResolveRuntimeReferences();
        }

        private void OnEnable()
        {
            ResolveRuntimeReferences();
            if (initialized && !IsQueuedPoolObject())
                ResetForNewRun();
            else
                PrepareForPlacementCapture();
            ActiveControllers.Add(this);
        }

        private void OnDisable()
        {
            ActiveControllers.Remove(this);
        }

        private void OnValidate()
        {
            NormalizeSerializedEventMode();
            moveSpeed = Mathf.Max(0f, moveSpeed);
            arrivalDistance = Mathf.Max(0.001f, arrivalDistance);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            NormalizeSerializedEventMode();
        }

        private void Update()
        {
            EnsureInitialized();

            bool isGameRunning = TimeManager.isGameRunning;
            if (enemyAnimator != null && enemyAnimator.enabled != isGameRunning)
                enemyAnimator.enabled = isGameRunning;

            if (!isGameRunning || RuntimeState == EnemyEventRuntimeState.Dead)
                return;

            if (RuntimeState != EnemyEventRuntimeState.MovingToTarget &&
                RuntimeState != EnemyEventRuntimeState.MovingToStart &&
                RuntimeState != EnemyEventRuntimeState.PatrolAttack)
            {
                return;
            }

            float scaledDeltaTime =
                Time.deltaTime * Mathf.Max(0f, TimeManager.timeFactor);
            AdvanceEvent(scaledDeltaTime);
        }

        public bool ActivateFromSpot()
        {
            ResolveRuntimeReferences();
            EnsureInitialized();
            if (Application.isPlaying && !TimeManager.isGameRunning)
                return false;

            if (RuntimeState != EnemyEventRuntimeState.Waiting)
                return false;

            switch (eventMode)
            {
                case EnemyEventMode.AttackLoop:
                    FacePlayerOrthogonally();
                    PlayAttackLoop();
                    RuntimeState = EnemyEventRuntimeState.Attacking;
                    return true;

                case EnemyEventMode.AttackOnce:
                    FacePlayerOrthogonally();
                    PlayAttackOnce();
                    RuntimeState = EnemyEventRuntimeState.Attacking;
                    return true;

                case EnemyEventMode.Shoot:
                    if (combat == null || !combat.TryBeginTriggeredFire())
                        return false;

                    FacePlayerOrthogonally();
                    RuntimeState = EnemyEventRuntimeState.Attacking;
                    return true;

                case EnemyEventMode.MoveToTargetThenAttack:
                case EnemyEventMode.PatrolBetweenStartAndTarget:
                    if (!CanStartMovementEvent())
                        return false;

                    RuntimeState = EnemyEventRuntimeState.MovingToTarget;
                    PlayLocomotion();
                    return true;

                default:
                    return false;
            }
        }

        public static bool RequiresTarget(EnemyEventMode mode)
        {
            return mode == EnemyEventMode.MoveToTargetThenAttack ||
                   mode == EnemyEventMode.PatrolBetweenStartAndTarget;
        }

        public static void ResetAllForNewRun()
        {
            var controllers = new List<EnemyEventController>(ActiveControllers);
            foreach (EnemyEventController controller in controllers)
            {
                if (controller != null)
                    controller.ResetForNewRun();
            }
        }

        public void RefreshPlacementAfterAuthoringChange(
            Vector3 previousPosition,
            bool rotationChanged)
        {
            if (!initialized)
                return;

            startPosition += transform.position - previousPosition;
            if (!rotationChanged)
                return;

            routeForward = HorizontalDirection(transform.forward, Vector3.forward);
            routeRight = HorizontalDirection(transform.right, Vector3.right);
            if (RuntimeState == EnemyEventRuntimeState.Waiting)
                SnapToRouteDirection();
        }

        public void PlayAttackOnce()
        {
            PlayAnimationState(AttackOnceStateHash);
        }

        public void PlayDie()
        {
            RuntimeState = EnemyEventRuntimeState.Dead;
            PlayAnimationState(DieStateHash);
        }

        public void SnapToRouteDirection()
        {
            Vector3 direction = initialized
                ? routeForward
                : HorizontalDirection(transform.forward, Vector3.forward);
            FaceDirection(direction);
        }

        private void ResolveRuntimeReferences()
        {
            if (enemyAnimator == null)
                enemyAnimator = GetComponentInChildren<Animator>();
            if (enemyAnimator != null && !visualRotationOffsetCaptured)
            {
                visualRotationOffset =
                    Quaternion.Inverse(transform.rotation) *
                    enemyAnimator.transform.rotation;
                visualRotationOffsetCaptured = true;
            }
            if (combat == null)
                combat = GetComponent<EnemyScript_space>();
            if (player == null)
                player = FindFirstObjectByType<PlayerScript>();
        }

        private void PrepareForPlacementCapture()
        {
            initialized = false;
            RuntimeState = EnemyEventRuntimeState.Waiting;
            patrolAttackStateObserved = false;
            patrolAttackFallbackRemaining = 0f;
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            ResolveRuntimeReferences();
            startPosition = transform.position;
            routeForward = HorizontalDirection(transform.forward, Vector3.forward);
            routeRight = HorizontalDirection(transform.right, Vector3.right);
            initialized = true;
            PlayIdle();
            FaceDirection(routeForward);
        }

        private void AdvanceEvent(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            switch (RuntimeState)
            {
                case EnemyEventRuntimeState.MovingToTarget:
                    if (!HasUsableTarget)
                    {
                        CancelMovementBecauseTargetIsMissing();
                        break;
                    }

                    AdvanceMovement(GetTargetPosition(), deltaTime, arrivedAtTarget: true);
                    break;

                case EnemyEventRuntimeState.MovingToStart:
                    AdvanceMovement(startPosition, deltaTime, arrivedAtTarget: false);
                    break;

                case EnemyEventRuntimeState.PatrolAttack:
                    AdvancePatrolAttack(deltaTime);
                    break;
            }
        }

        private void AdvanceMovement(
            Vector3 destination,
            float deltaTime,
            bool arrivedAtTarget)
        {
            Vector3 movement = destination - transform.position;
            if (movement.sqrMagnitude > DirectionEpsilonSqr)
                FaceDirection(movement);

            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * deltaTime);

            float clampedArrivalDistance = Mathf.Max(0.001f, arrivalDistance);
            if ((destination - transform.position).sqrMagnitude >
                clampedArrivalDistance * clampedArrivalDistance)
            {
                return;
            }

            transform.position = destination;
            FacePlayerOrthogonally();
            if (eventMode == EnemyEventMode.MoveToTargetThenAttack)
            {
                PlayAttackLoop();
                RuntimeState = EnemyEventRuntimeState.Attacking;
                return;
            }

            BeginPatrolAttack(arrivedAtTarget);
        }

        private void BeginPatrolAttack(bool arrivedAtTarget)
        {
            RuntimeState = EnemyEventRuntimeState.PatrolAttack;
            patrolAttackStateObserved = false;
            patrolAttackFallbackRemaining = PatrolAttackFallbackSeconds;
            PlayAttackOnce();
            patrolReturningToStart = arrivedAtTarget;
        }

        private void AdvancePatrolAttack(float deltaTime)
        {
            patrolAttackFallbackRemaining -= deltaTime;
            if (!HasAttackOnceFinished())
                return;

            RuntimeState = patrolReturningToStart
                ? EnemyEventRuntimeState.MovingToStart
                : EnemyEventRuntimeState.MovingToTarget;
            PlayLocomotion();
        }

        private bool HasAttackOnceFinished()
        {
            if (enemyAnimator == null || enemyAnimator.runtimeAnimatorController == null)
                return patrolAttackFallbackRemaining <= 0f;

            AnimatorStateInfo state = enemyAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == AttackOnceStateHash)
            {
                patrolAttackStateObserved = true;
                return state.normalizedTime >= 1f && !enemyAnimator.IsInTransition(0);
            }

            return patrolAttackStateObserved || patrolAttackFallbackRemaining <= 0f;
        }

        private Vector3 GetTargetPosition()
        {
            Vector3 destination = targetPoint.position;
            destination.y = startPosition.y;
            return destination;
        }

        private bool CanStartMovementEvent()
        {
            if (!HasUsableTarget)
                return false;

            Vector3 destination = GetTargetPosition();
            float clampedArrivalDistance = Mathf.Max(0.001f, arrivalDistance);
            bool alreadyAtTarget =
                (destination - transform.position).sqrMagnitude <=
                clampedArrivalDistance * clampedArrivalDistance;
            return alreadyAtTarget || moveSpeed > Mathf.Epsilon;
        }

        private void CancelMovementBecauseTargetIsMissing()
        {
            RuntimeState = EnemyEventRuntimeState.Waiting;
            PlayIdle();
            FaceDirection(routeForward);
            Debug.LogWarning(
                $"[{nameof(EnemyEventController)}] '{name}' stopped because its movement target is missing.",
                this);
        }

        private void FacePlayerOrthogonally()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerScript>();

            Vector3 toPlayer = player != null
                ? player.transform.position - transform.position
                : routeForward;
            FaceDirection(ResolveOrthogonalFacingDirection(
                toPlayer,
                routeForward,
                routeRight));
        }

        public static Vector3 ResolveOrthogonalFacingDirection(
            Vector3 toTarget,
            Vector3 forward,
            Vector3 right)
        {
            Vector3 horizontalTarget = HorizontalDirection(toTarget, forward);
            Vector3 horizontalForward = HorizontalDirection(forward, Vector3.forward);
            Vector3 horizontalRight = HorizontalDirection(right, Vector3.right);
            float forwardDot = Vector3.Dot(horizontalTarget, horizontalForward);
            float rightDot = Vector3.Dot(horizontalTarget, horizontalRight);

            return Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot)
                ? horizontalForward * (forwardDot < 0f ? -1f : 1f)
                : horizontalRight * (rightDot < 0f ? -1f : 1f);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (enemyAnimator == null)
                return;

            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (horizontal.sqrMagnitude <= DirectionEpsilonSqr)
                return;

            Quaternion targetRotation =
                Quaternion.LookRotation(horizontal.normalized, Vector3.up) *
                visualRotationOffset;
            if (Quaternion.Angle(
                    enemyAnimator.transform.rotation,
                    targetRotation) <= 0.01f)
            {
                return;
            }

            enemyAnimator.transform.rotation = targetRotation;
        }

        private void PlayIdle()
        {
            PlayAnimationState(IdleStateHash);
        }

        private void PlayAttackLoop()
        {
            PlayAnimationState(AttackLoopStateHash);
        }

        private void PlayLocomotion()
        {
            int stateHash = moveAnimation switch
            {
                EnemyMoveAnimation.None => IdleStateHash,
                EnemyMoveAnimation.Run => RunStateHash,
                _ => WalkStateHash
            };
            PlayAnimationState(stateHash);
        }

        private void PlayAnimationState(int stateHash)
        {
            if (enemyAnimator == null || enemyAnimator.runtimeAnimatorController == null)
                return;

            AnimatorStateInfo currentState =
                enemyAnimator.GetCurrentAnimatorStateInfo(0);
            if (currentState.shortNameHash == stateHash &&
                !enemyAnimator.IsInTransition(0))
            {
                return;
            }

            enemyAnimator.CrossFadeInFixedTime(
                stateHash,
                AnimationTransitionSeconds,
                0,
                0f);
        }

        private void ResetForNewRun()
        {
            RuntimeState = EnemyEventRuntimeState.Waiting;
            patrolAttackStateObserved = false;
            patrolAttackFallbackRemaining = 0f;
            patrolReturningToStart = false;

            if (!initialized)
                return;

            transform.position = startPosition;
            PlayIdle();
            FaceDirection(routeForward);
        }

        private bool IsQueuedPoolObject()
        {
            return GetComponentInParent<EnemyPooler>(includeInactive: true) != null;
        }

        private void NormalizeSerializedEventMode()
        {
            if ((int)eventMode == 3)
            {
                eventMode = EnemyEventMode.MoveToTargetThenAttack;
                return;
            }

            if (eventMode != EnemyEventMode.AttackLoop &&
                eventMode != EnemyEventMode.AttackOnce &&
                eventMode != EnemyEventMode.Shoot &&
                eventMode != EnemyEventMode.MoveToTargetThenAttack &&
                eventMode != EnemyEventMode.PatrolBetweenStartAndTarget)
            {
                eventMode = EnemyEventMode.AttackLoop;
            }
        }

        private static Vector3 HorizontalDirection(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= DirectionEpsilonSqr)
            {
                fallback.y = 0f;
                return fallback.sqrMagnitude > DirectionEpsilonSqr
                    ? fallback.normalized
                    : Vector3.forward;
            }

            return direction.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 previewStart = Application.isPlaying && initialized
                ? startPosition
                : transform.position;
            Color previousColor = Gizmos.color;

            if (RequiresTarget(eventMode))
            {
                if (HasUsableTarget)
                {
                    Vector3 destination = targetPoint.position;
                    destination.y = previewStart.y;
                    Gizmos.color = new Color(1f, 0.55f, 0.05f, 0.95f);
                    DrawArrow(previewStart, destination);
                    Gizmos.DrawWireSphere(destination, 0.3f);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(previewStart, 0.45f);
                }
            }
            else
            {
                Gizmos.color = new Color(1f, 0.55f, 0.05f, 0.95f);
                Gizmos.DrawWireSphere(previewStart, 0.35f);
            }

            Gizmos.color = previousColor;
        }

        private static void DrawArrow(Vector3 start, Vector3 end)
        {
            Gizmos.DrawLine(start, end);
            Vector3 direction = end - start;
            if (direction.sqrMagnitude <= DirectionEpsilonSqr)
                return;

            direction.Normalize();
            Vector3 right = Quaternion.Euler(0f, 25f, 0f) * -direction;
            Vector3 left = Quaternion.Euler(0f, -25f, 0f) * -direction;
            Gizmos.DrawLine(end, end + right * 0.55f);
            Gizmos.DrawLine(end, end + left * 0.55f);
        }
    }
}
