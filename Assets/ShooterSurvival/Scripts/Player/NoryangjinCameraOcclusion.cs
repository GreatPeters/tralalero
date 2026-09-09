using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Opt-in for the SR18 gameplay camera. Geometry, colliders and shared materials stay intact.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class NoryangjinCameraOcclusion : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform roadRoot;
        [SerializeField, Min(0f)] private float viewAhead = 18f;
        [SerializeField, Min(0f)] private float clearance = 2f;
        private Renderer[] candidates;
        private readonly Dictionary<Renderer, bool> hidden = new();
        private readonly List<Renderer> restored = new();
        public int HiddenCount => hidden.Count;

        public void Configure(Transform target, Transform roads)
        {
            RestoreAll(); player = target; roadRoot = roads; CacheRenderers();
        }
        private void OnEnable() => CacheRenderers();
        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();
        private void CacheRenderers() => candidates = roadRoot != null
            ? roadRoot.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>();
        private void LateUpdate() { if (Application.isPlaying) RefreshVisibility(); }

        public void RefreshVisibility()
        {
            if (player == null || roadRoot == null) { RestoreAll(); return; }
            Vector3 head = player.position + Vector3.up * 1.4f;
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            foreach (var renderer in candidates)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                bool blocks = false;
                Bounds bounds = renderer.bounds;
                if (bounds.min.y > player.position.y + clearance)
                {
                    bounds.Expand(1f);
                    for (int i = 0; i < 3 && !blocks; i++)
                    {
                        Vector3 target = head + forward * (i == 0 ? 0 : viewAhead) + side * (i == 1 ? -2f : i == 2 ? 2f : 0);
                        blocks = IntersectsView(bounds, transform.position, target);
                    }
                }
                if (blocks && !hidden.ContainsKey(renderer))
                {
                    hidden.Add(renderer, renderer.forceRenderingOff);
                    renderer.forceRenderingOff = true;
                }
                else if (!blocks && hidden.TryGetValue(renderer, out bool original))
                {
                    renderer.forceRenderingOff = original;
                    hidden.Remove(renderer);
                }
            }
            restored.Clear();
            foreach (var pair in hidden)
                if (pair.Key == null || !pair.Key.enabled || !pair.Key.gameObject.activeInHierarchy) restored.Add(pair.Key);
            foreach (var renderer in restored)
            {
                if (renderer != null) renderer.forceRenderingOff = hidden[renderer];
                hidden.Remove(renderer);
            }
        }
        private static bool IntersectsView(Bounds bounds, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            return delta.sqrMagnitude > .0001f && bounds.IntersectRay(new Ray(from, delta.normalized), out float distance) && distance <= delta.magnitude;
        }
        private void RestoreAll()
        {
            foreach (var pair in hidden) if (pair.Key != null) pair.Key.forceRenderingOff = pair.Value;
            hidden.Clear();
        }
    }
}
