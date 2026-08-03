using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.ShooterSurvival
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class NoryangjinTurnSpot : MonoBehaviour
    {
        public const float DefaultTurnDurationSeconds = 0.5f;
        private static readonly HashSet<NoryangjinTurnSpot> ConsumedTurnSpots = new();
        private static readonly Dictionary<int, List<NoryangjinTurnSpot>>
            RouteSpotsBySceneHandle = new();

        [Tooltip("플레이어가 도착할 절대 월드 X 회전값입니다.")]
        [SerializeField] private float targetXDegrees;

        [Tooltip("플레이어가 도착할 절대 월드 Y 회전값입니다.")]
        [SerializeField] private float targetYawDegrees;

        [Tooltip("이동과 좌우 입력을 멈추고 회전하는 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float turnDurationSeconds = DefaultTurnDurationSeconds;

        public float TargetXDegrees
        {
            get => targetXDegrees;
            set => targetXDegrees = value;
        }

        public float TargetYawDegrees
        {
            get => targetYawDegrees;
            set => targetYawDegrees = value;
        }

        public float TurnDurationSeconds
        {
            get => turnDurationSeconds;
            set => turnDurationSeconds = ClampDuration(value);
        }

        public Vector3 TargetWorldDirection =>
            DirectionFromRotation(targetXDegrees, targetYawDegrees);

        private void Reset()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, 0f);
            trigger.size = new Vector3(4f, 2f, 0.8f);
        }

        private void Awake()
        {
            InvalidateRouteSpotCache();
            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
                trigger.isTrigger = true;
        }

        private void OnEnable()
        {
            InvalidateRouteSpotCache();
        }

        private void OnValidate()
        {
            turnDurationSeconds = ClampDuration(turnDurationSeconds);

            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
                trigger.isTrigger = true;
        }

        private void OnDestroy()
        {
            ConsumedTurnSpots.Remove(this);
            InvalidateRouteSpotCache();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null)
                return;

            PlayerScript player = ResolveTriggerPlayer(other);
            if (player == null || !player.CompareTag("Player"))
                return;

            TryActivate(player);
        }

        public static PlayerScript ResolveTriggerPlayer(Collider other)
        {
            // Only the player's root movement collider may activate a spot.
            // Weapon and visual child colliders otherwise produce duplicate turns.
            return other != null ? other.GetComponent<PlayerScript>() : null;
        }

        internal bool TryActivate(PlayerScript player)
        {
            bool accepted = player != null &&
                            player.RequestWorldRotation(
                                targetXDegrees,
                                targetYawDegrees,
                                turnDurationSeconds,
                                this);
            if (accepted)
            {
                ConsumedTurnSpots.Add(this);
                gameObject.SetActive(false);
            }

            return accepted;
        }

        public static float ClampDuration(float duration)
        {
            return Mathf.Max(0f, duration);
        }

        public static Vector3 DirectionFromYaw(float yawDegrees)
        {
            return DirectionFromRotation(0f, yawDegrees);
        }

        public static Vector3 DirectionFromRotation(
            float xDegrees,
            float yawDegrees)
        {
            return Quaternion.Euler(xDegrees, yawDegrees, 0f) * Vector3.forward;
        }

        public static void ResetAllForNewRun()
        {
            foreach (NoryangjinTurnSpot turnSpot in ConsumedTurnSpots)
            {
                if (turnSpot != null)
                    turnSpot.ResetForNewRun();
            }

            ConsumedTurnSpots.Clear();
        }

        public static bool TryGetRouteProgress(
            Scene scene,
            out int completedCheckpointCount,
            out int totalCheckpointCount)
        {
            completedCheckpointCount = 0;
            totalCheckpointCount = 0;

            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            foreach (NoryangjinTurnSpot turnSpot in GetRouteSpots(scene))
            {
                if (turnSpot == null)
                    continue;

                HideFlags combinedHideFlags =
                    turnSpot.hideFlags | turnSpot.gameObject.hideFlags;
                if ((combinedHideFlags & HideFlags.DontSave) != 0)
                    continue;

                bool consumed = ConsumedTurnSpots.Contains(turnSpot);
                if (!consumed && !turnSpot.isActiveAndEnabled)
                    continue;

                totalCheckpointCount++;
                if (consumed)
                    completedCheckpointCount++;
            }

            return totalCheckpointCount > 0;
        }

        private static IReadOnlyList<NoryangjinTurnSpot> GetRouteSpots(
            Scene scene)
        {
            int sceneHandle = scene.handle;
            if (RouteSpotsBySceneHandle.TryGetValue(
                    sceneHandle,
                    out List<NoryangjinTurnSpot> cachedSpots) &&
                !cachedSpots.Exists(turnSpot => turnSpot == null))
            {
                return cachedSpots;
            }

            var routeSpots = new List<NoryangjinTurnSpot>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                routeSpots.AddRange(
                    root.GetComponentsInChildren<NoryangjinTurnSpot>(true));
            }

            RouteSpotsBySceneHandle[sceneHandle] = routeSpots;
            return routeSpots;
        }

        private void InvalidateRouteSpotCache()
        {
            Scene scene = gameObject.scene;
            if (scene.IsValid())
                RouteSpotsBySceneHandle.Remove(scene.handle);
        }

        internal void ResetForNewRun()
        {
            gameObject.SetActive(true);
        }
    }
}
