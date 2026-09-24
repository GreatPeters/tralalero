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
        private readonly HashSet<Transform> traversedRoads = new();
        private readonly Dictionary<Renderer, TemporarySceneryFade> faded = new();
        private NoryangjinRoadHeightFollower heightFollower;
        public int HiddenCount => hidden.Count;
        public int FadedCount => faded.Count;
        public float SceneryOpacity(Renderer renderer) => faded.TryGetValue(renderer,out var fade)?fade.Opacity:1f;

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

        public void RefreshVisibility(float deltaTime = -1f)
        {
            if (player == null || roadRoot == null) { RestoreAll(); return; }
            if(deltaTime<0)deltaTime=Application.isPlaying?Time.deltaTime:.2f;
            Vector3 head = player.position + Vector3.up * 1.4f;
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            CacheTraversedRoads(forward);
            foreach (var candidate in candidateGroups)
            {
                var group=candidate.renderers;
                bool blocks = false;
                foreach (var renderer in group)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    Bounds bounds = renderer.bounds;
                    if(candidate.explicitScenery)
                    {
                        blocks=SeverelyBlocks(bounds,transform.position,head,forward,side);
                        if(blocks)break;
                        continue;
                    }
                    // A ramp ahead is ground we are about to walk on, even when its
                    // upper tiles are above the player's current floor-height guard.
                    if (!candidate.explicitScenery && traversedRoads.Contains(renderer.transform)) continue;
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
                    if(candidate.explicitScenery)
                    {
                        if(blocks&&!renderer.forceRenderingOff&&!faded.ContainsKey(renderer))faded.Add(renderer,new TemporarySceneryFade(renderer));
                        if(faded.TryGetValue(renderer,out var fade))
                        {
                            fade.Advance(blocks,deltaTime);
                            if(fade.Opacity>=1){fade.Restore();faded.Remove(renderer);}
                        }
                        continue;
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
            }
            restored.Clear();
            foreach (var pair in hidden)
                if (pair.Key == null || !pair.Key.enabled || !pair.Key.gameObject.activeInHierarchy) restored.Add(pair.Key);
            foreach (var renderer in restored)
            {
                if (renderer != null) renderer.forceRenderingOff = hidden[renderer];
                hidden.Remove(renderer);
            }
            restored.Clear();foreach(var pair in faded)if(pair.Key==null||!pair.Key.enabled||!pair.Key.gameObject.activeInHierarchy)restored.Add(pair.Key);
            foreach(var renderer in restored){faded[renderer].Restore();faded.Remove(renderer);}
        }
        public static bool SeverelyBlocks(Bounds bounds,Vector3 camera,Vector3 torso,Vector3 forward,Vector3 side)
        {
            int body=0,road=0;
            for(int i=-1;i<=1;i++)
            {
                if(IntersectsView(bounds,camera,torso+side*(i*.65f)))body++;
                if(IntersectsView(bounds,camera,torso+forward*6+side*(i*1.2f)))road++;
            }
            return body>=2&&road>=2;
        }
        private void CacheTraversedRoads(Vector3 forward)
        {
            traversedRoads.Clear();
            if (heightFollower == null || heightFollower.transform != player)
                heightFollower = player.GetComponent<NoryangjinRoadHeightFollower>();
            if (heightFollower == null) return;
            foreach (float sign in new[] { -1f, 1f })
            {
                Vector3 sample = player.position;
                float reach = sign < 0f ? 4f : viewAhead + 3f;
                for (float distance = 0f; distance <= reach; distance += .5f)
                {
                    if (!heightFollower.TryProjectPosition(sample, forward, out var supported, out var collider)) break;
                    if (collider != null) traversedRoads.Add(collider.transform);
                    sample = supported + forward * (sign * .5f);
                }
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
            foreach(var fade in faded.Values)fade.Restore();faded.Clear();
        }
    }
}
