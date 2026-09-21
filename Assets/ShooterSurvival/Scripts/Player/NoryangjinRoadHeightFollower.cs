using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.ShooterSurvival
{
    // Opt-in height support for authored Noryangjin roads; other modes keep their existing movement.
    [DisallowMultipleComponent]
    public sealed class NoryangjinRoadHeightFollower : MonoBehaviour
    {
        [SerializeField] private Transform roadRoot;
        [Min(0f)] [SerializeField] private float footOffset = 0.12f;
        [Min(0.1f)] [SerializeField] private float probeReach = 0.75f;
        private MeshCollider[] roadColliders;
        private const float RoadCellSize = 16f;
        private readonly Dictionary<Vector2Int, List<MeshCollider>> roadCells = new();
        private bool blendingPitch;
        private float startPitch;
        private float targetPitch;
        private float pitchElapsed;
        private float pitchDuration;

        public Transform RoadRoot => roadRoot;
        public float FootOffset => footOffset;
        public bool IsBlendingPitch => blendingPitch;

        private void Awake() => CacheRoads();
        private void OnDisable() => CancelPitch();

        public void Configure(Transform roads, float offset)
        {
            roadRoot = roads;
            footOffset = Mathf.Max(0f, offset);
            CacheRoads();
        }

        private void CacheRoads()
        {
            roadColliders = roadRoot != null
                ? roadRoot.GetComponentsInChildren<MeshCollider>(false)
                : System.Array.Empty<MeshCollider>();
            // Authored road tiles do not move during a run. Configure rebuilds this
            // index when the road layout changes; each query visits only its cell.
            roadCells.Clear();
            foreach (var collider in roadColliders)
            {
                Bounds bounds = collider.bounds;
                Vector2Int min = Cell(bounds.min), max = Cell(bounds.max);
                for (int x = min.x; x <= max.x; x++)
                    for (int z = min.y; z <= max.y; z++)
                    {
                        var key = new Vector2Int(x, z);
                        if (!roadCells.TryGetValue(key, out var list)) roadCells.Add(key, list = new List<MeshCollider>());
                        list.Add(collider);
                    }
            }
        }

        private static Vector2Int Cell(Vector3 point) => new Vector2Int(
            Mathf.FloorToInt(point.x / RoadCellSize), Mathf.FloorToInt(point.z / RoadCellSize));

        public bool TryProjectPosition(Vector3 proposed, Vector3 forward, out Vector3 supported)
            => TryProjectPosition(proposed, forward, out supported, out _);

        public bool TryProjectPosition(Vector3 proposed, Vector3 forward, out Vector3 supported, out Collider support)
        {
            supported = proposed;
            support = null;
            if (!isActiveAndEnabled || roadRoot == null)
                return false;
            if (roadColliders == null)
                CacheRoads();

            Vector3 planarForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            for (int sample = 0; sample < 3; sample++)
            {
                float nudge = sample == 0 ? 0f : sample == 1 ? -0.06f : 0.06f;
                Vector3 origin = proposed + planarForward * nudge;
                origin.y = proposed.y - footOffset + probeReach;
                if (!roadCells.TryGetValue(Cell(origin), out var candidates)) continue;
                var ray = new Ray(origin, Vector3.down);
                bool found = false;
                float nearest = float.PositiveInfinity;
                foreach (MeshCollider collider in candidates)
                {
                    if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                        continue;
                    Bounds bounds = collider.bounds;
                    if (origin.x < bounds.min.x || origin.x > bounds.max.x ||
                        origin.z < bounds.min.z || origin.z > bounds.max.z)
                        continue;
                    if (!collider.Raycast(ray, out RaycastHit hit, probeReach * 2f) ||
                        hit.normal.y < 0.55f || hit.distance >= nearest)
                        continue;
                    nearest = hit.distance;
                    supported.y = hit.point.y + footOffset;
                    support = collider;
                    found = true;
                }
                if (found)
                    return true;
            }
            return false;
        }

        public void BeginPitch(float currentDegrees, float targetDegrees, float duration)
        {
            startPitch = Mathf.DeltaAngle(0f, currentDegrees);
            targetPitch = Mathf.Clamp(Mathf.DeltaAngle(0f, targetDegrees), -60f, 60f);
            pitchDuration = Mathf.Max(0f, duration);
            pitchElapsed = 0f;
            blendingPitch = true;
        }

        public bool AdvancePitch(float deltaSeconds, out float pitch)
        {
            pitch = targetPitch;
            if (!blendingPitch)
                return false;
            pitchElapsed += Mathf.Max(0f, deltaSeconds);
            float t = pitchDuration <= 0f ? 1f : Mathf.Clamp01(pitchElapsed / pitchDuration);
            pitch = Mathf.LerpAngle(startPitch, targetPitch, t * t * (3f - 2f * t));
            if (t >= 1f)
                blendingPitch = false;
            return true;
        }

        public void CancelPitch() => blendingPitch = false;
    }
}
