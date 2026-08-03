using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public enum EnemyMovementMode
    {
        StayStill = 0,
        MoveSideToSide = 1,
        MoveForwardOnTrigger = 2,
        EnterFromSideOnTrigger = 3
    }

    public enum EnemyEntranceSide
    {
        Left = -1,
        Right = 1
    }

    [AddComponentMenu("Shooter Survival/Enemy Movement Controller")]
    [DisallowMultipleComponent]
    public sealed class EnemyMovementController : MonoBehaviour
    {
        private static readonly HashSet<EnemyMovementController> ActiveControllers = new();

        [Tooltip("적의 이동 동작입니다. 전진과 옆 등장은 플레이어 발동 트리거가 필요합니다.")]
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.StayStill;

        [Tooltip("초당 이동 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float moveSpeed = 2f;

        [Tooltip("배치 지점을 중심으로 좌우 각각 이동할 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float sideToSideDistance = 2f;

        [Tooltip("적이 배치 지점의 어느 쪽에서 등장할지 정합니다.")]
        [SerializeField] private EnemyEntranceSide entranceSide = EnemyEntranceSide.Left;

        [Tooltip("배치 지점에서 옆으로 떨어져 대기할 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float entranceDistance = 3f;

        private bool initialized;
        private bool activationRequested;
        private Vector3 anchorPosition;
        private Vector3 movementForward;
        private Vector3 movementRight;
        private float sideTravelDistance;

        public EnemyMovementMode MovementMode
        {
            get => movementMode;
            set
            {
                if (movementMode == value)
                    return;

                movementMode = value;
                ResetForNewRun();
                if (Application.isPlaying)
                    enabled = movementMode != EnemyMovementMode.StayStill;
            }
        }

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = ClampDistance(value);
        }

        public float SideToSideDistance
        {
            get => sideToSideDistance;
            set => sideToSideDistance = ClampDistance(value);
        }

        public EnemyEntranceSide EntranceSide
        {
            get => entranceSide;
            set
            {
                if (entranceSide == value)
                    return;

                entranceSide = value;
                ResetForNewRun();
            }
        }

        public float EntranceDistance
        {
            get => entranceDistance;
            set
            {
                entranceDistance = ClampDistance(value);
                ResetForNewRun();
            }
        }

        public bool RequiresPlayerTrigger => RequiresTrigger(movementMode);
        public bool IsActivated => activationRequested;

        private void OnEnable()
        {
            PrepareForPlacementCapture();

            if (Application.isPlaying &&
                movementMode == EnemyMovementMode.StayStill)
            {
                enabled = false;
                return;
            }

            ActiveControllers.Add(this);
        }

        private void OnDisable()
        {
            ActiveControllers.Remove(this);
        }

        private void OnValidate()
        {
            moveSpeed = ClampDistance(moveSpeed);
            sideToSideDistance = ClampDistance(sideToSideDistance);
            entranceDistance = ClampDistance(entranceDistance);
        }

        private void Update()
        {
            EnsureInitialized();

            if (!TimeManager.isGameRunning)
                return;

            float scaledDeltaTime =
                Time.deltaTime * Mathf.Max(0f, TimeManager.timeFactor);
            AdvanceMovement(scaledDeltaTime);
        }

        public bool ActivateFromTrigger()
        {
            if (!RequiresPlayerTrigger)
                return false;

            activationRequested = true;
            return true;
        }

        public static bool RequiresTrigger(EnemyMovementMode mode)
        {
            return mode == EnemyMovementMode.MoveForwardOnTrigger ||
                   mode == EnemyMovementMode.EnterFromSideOnTrigger;
        }

        public static void ResetAllForNewRun()
        {
            var controllers = new List<EnemyMovementController>(ActiveControllers);
            foreach (EnemyMovementController controller in controllers)
            {
                if (controller != null)
                    controller.ResetForNewRun();
            }
        }

        private void PrepareForPlacementCapture()
        {
            initialized = false;
            sideTravelDistance = 0f;
            activationRequested = !RequiresPlayerTrigger;
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            // Pooled enemies are enabled before EnemySpawnerScript assigns their
            // spawn transform. Waiting until the first Update captures the final
            // authored/spawned placement instead of the previous pool position.
            anchorPosition = transform.position;
            movementForward = HorizontalDirection(transform.forward, Vector3.forward);
            movementRight = HorizontalDirection(transform.right, Vector3.right);
            sideTravelDistance = 0f;
            initialized = true;

            ApplyInitialPosition();
        }

        private void ApplyInitialPosition()
        {
            transform.position = movementMode == EnemyMovementMode.EnterFromSideOnTrigger
                ? CalculateEntrancePosition(
                    anchorPosition,
                    movementRight,
                    entranceSide,
                    entranceDistance)
                : anchorPosition;
        }

        private void AdvanceMovement(float deltaTime)
        {
            EnsureInitialized();

            if (!activationRequested || deltaTime <= 0f)
                return;

            float distance = moveSpeed * deltaTime;
            switch (movementMode)
            {
                case EnemyMovementMode.StayStill:
                    return;

                case EnemyMovementMode.MoveSideToSide:
                    sideTravelDistance += distance;
                    float sideOffset = CalculateSideToSideOffset(
                        sideTravelDistance,
                        sideToSideDistance);
                    transform.position =
                        anchorPosition + movementRight * sideOffset;
                    return;

                case EnemyMovementMode.MoveForwardOnTrigger:
                    transform.position += movementForward * distance;
                    return;

                case EnemyMovementMode.EnterFromSideOnTrigger:
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        anchorPosition,
                        distance);
                    return;
            }
        }

        private void ResetForNewRun()
        {
            sideTravelDistance = 0f;
            activationRequested = !RequiresPlayerTrigger;

            if (initialized)
                ApplyInitialPosition();
        }

        public static float CalculateSideToSideOffset(
            float traveledDistance,
            float distanceFromCenter)
        {
            float clampedDistance = ClampDistance(distanceFromCenter);
            if (clampedDistance <= Mathf.Epsilon)
                return 0f;

            float cycleLength = clampedDistance * 2f;
            return Mathf.PingPong(
                       Mathf.Max(0f, traveledDistance) + clampedDistance,
                       cycleLength) -
                   clampedDistance;
        }

        public static Vector3 CalculateEntrancePosition(
            Vector3 anchor,
            Vector3 right,
            EnemyEntranceSide side,
            float distance)
        {
            float sign = side == EnemyEntranceSide.Right ? 1f : -1f;
            return anchor + right.normalized * (ClampDistance(distance) * sign);
        }

        private static Vector3 HorizontalDirection(
            Vector3 direction,
            Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return fallback;

            return direction.normalized;
        }

        private static float ClampDistance(float value)
        {
            return Mathf.Max(0f, value);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 previewAnchor = Application.isPlaying && initialized
                ? anchorPosition
                : transform.position;
            Vector3 previewForward = Application.isPlaying && initialized
                ? movementForward
                : HorizontalDirection(transform.forward, Vector3.forward);
            Vector3 previewRight = Application.isPlaying && initialized
                ? movementRight
                : HorizontalDirection(transform.right, Vector3.right);

            Color previousColor = Gizmos.color;
            Gizmos.color = new Color(1f, 0.55f, 0.05f, 0.95f);
            switch (movementMode)
            {
                case EnemyMovementMode.StayStill:
                    Gizmos.DrawWireSphere(previewAnchor, 0.35f);
                    break;

                case EnemyMovementMode.MoveSideToSide:
                    Vector3 left =
                        previewAnchor - previewRight * sideToSideDistance;
                    Vector3 right =
                        previewAnchor + previewRight * sideToSideDistance;
                    Gizmos.DrawLine(left, right);
                    Gizmos.DrawWireSphere(left, 0.25f);
                    Gizmos.DrawWireSphere(right, 0.25f);
                    break;

                case EnemyMovementMode.MoveForwardOnTrigger:
                    DrawArrow(
                        previewAnchor,
                        previewAnchor + previewForward * 4f);
                    break;

                case EnemyMovementMode.EnterFromSideOnTrigger:
                    Vector3 entrance = CalculateEntrancePosition(
                        previewAnchor,
                        previewRight,
                        entranceSide,
                        entranceDistance);
                    DrawArrow(entrance, previewAnchor);
                    Gizmos.DrawWireSphere(entrance, 0.25f);
                    break;
            }

            Gizmos.color = previousColor;
        }

        private static void DrawArrow(Vector3 start, Vector3 end)
        {
            Gizmos.DrawLine(start, end);

            Vector3 direction = end - start;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            direction.Normalize();
            Vector3 right = Quaternion.Euler(0f, 25f, 0f) * -direction;
            Vector3 left = Quaternion.Euler(0f, -25f, 0f) * -direction;
            Gizmos.DrawLine(end, end + right * 0.55f);
            Gizmos.DrawLine(end, end + left * 0.55f);
        }
    }
}
