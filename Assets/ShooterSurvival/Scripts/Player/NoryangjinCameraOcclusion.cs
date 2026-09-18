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
        [SerializeField] private Transform[] additionalOccluderGroups = System.Array.Empty<Transform>();
        [SerializeField, Min(0f)] private float viewAhead = 18f;
        [SerializeField, Min(0f)] private float clearance = 2f;
        private readonly List<(Renderer[] renderers, bool explicitScenery)> candidateGroups = new();
        private readonly Dictionary<Renderer, bool> hidden = new();
        private readonly List<Renderer> restored = new();
        public int HiddenCount => hidden.Count;

        public void Configure(Transform target, Transform roads)
        {
            RestoreAll(); player = target; roadRoot = roads; CacheRenderers();
        }
        public void ConfigureAdditionalOccluders(Transform[] groups)
        {
            RestoreAll(); additionalOccluderGroups = groups ?? System.Array.Empty<Transform>(); CacheRenderers();
        }
        private void OnEnable() => CacheRenderers();
        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();
        private void CacheRenderers()
        {
            candidateGroups.Clear();var seen = new HashSet<Renderer>();
            if (additionalOccluderGroups != null) foreach (var group in additionalOccluderGroups)
            {
                if (group == null) continue;
                var renderers = new List<Renderer>();
                foreach (var renderer in group.GetComponentsInChildren<Renderer>(true)) if (seen.Add(renderer)) renderers.Add(renderer);
                if (renderers.Count > 0) candidateGroups.Add((renderers.ToArray(),true));
            }
            if (roadRoot != null) foreach (var renderer in roadRoot.GetComponentsInChildren<Renderer>(true))
                if (seen.Add(renderer)) candidateGroups.Add((new[]{renderer},false));
        }
        private void LateUpdate() { if (Application.isPlaying) RefreshVisibility(); }

        public void RefreshVisibility()
        {
            if (player == null || roadRoot == null) { RestoreAll(); return; }
            Vector3 head = player.position + Vector3.up * 1.4f;
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            foreach (var candidate in candidateGroups)
            {
                var group=candidate.renderers;
                bool blocks = false;
                foreach (var renderer in group)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    Bounds bounds = renderer.bounds;
                    // Explicit props may combine ground-level posts and overhead
                    // beams in one mesh. The floor-height guard is for roads only.
                    if (candidate.explicitScenery || bounds.min.y > player.position.y + clearance)
                    {
                        bounds.Expand(1f);
                        for (int i = 0; i < 3 && !blocks; i++)
                        {
                            Vector3 target = head + forward * (i == 0 ? 0 : viewAhead) + side * (i == 1 ? -2f : i == 2 ? 2f : 0);
                            blocks = IntersectsView(bounds, transform.position, target);
                        }
                    }
                    if (blocks) break;
                }
                // Treat an authored sign/arch as one visual: its lettering and beam
                // must not remain floating when a panel hides.
                foreach (var renderer in group)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
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
