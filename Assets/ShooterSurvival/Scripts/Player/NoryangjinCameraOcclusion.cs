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
        [SerializeField] private Transform[] clearViewGroups = System.Array.Empty<Transform>();
        [SerializeField] private bool inspectCombinedRoads;
        [SerializeField] private bool feedbackTransparency;
        [SerializeField,Range(.05f,.5f)] private float feedbackOpacity=.18f;
        [SerializeField, Min(0f)] private float viewAhead = 18f;
        [SerializeField, Min(0f)] private float clearance = 2f;
        private readonly List<(Renderer[] renderers, bool explicitScenery, bool fullyHide)> candidateGroups = new();
        private readonly Dictionary<Renderer, bool> hidden = new();
        private readonly List<Renderer> restored = new();
        private readonly HashSet<Transform> traversedRoads = new();
        private readonly Dictionary<Renderer, TemporarySceneryFade> faded = new();
        private NoryangjinRoadHeightFollower heightFollower;
        private static readonly float[] FeedbackLookAhead={0,8,18,32,45};
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
        public void ConfigureClearViewOccluders(Transform[] groups,bool combinedRoads)
        {
            RestoreAll();clearViewGroups=groups??System.Array.Empty<Transform>();inspectCombinedRoads=combinedRoads;CacheRenderers();
        }
        public void ConfigureFeedbackTransparency(Transform[] groups)
        {
            feedbackTransparency=true;feedbackOpacity=.25f;ConfigureClearViewOccluders(groups,true);
        }
        private void OnEnable() => CacheRenderers();
        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();
        private void CacheRenderers()
        {
            candidateGroups.Clear();var seen = new HashSet<Renderer>();
            foreach(var group in clearViewGroups)
            {
                if(group==null)continue;var renderers=new List<Renderer>();
                foreach(var renderer in group.GetComponentsInChildren<Renderer>(true))if(seen.Add(renderer))renderers.Add(renderer);
                if(renderers.Count>0)candidateGroups.Add((renderers.ToArray(),true,true));
            }
            if (additionalOccluderGroups != null) foreach (var group in additionalOccluderGroups)
            {
                if (group == null) continue;
                var renderers = new List<Renderer>();
                foreach (var renderer in group.GetComponentsInChildren<Renderer>(true)) if (seen.Add(renderer)) renderers.Add(renderer);
                if (renderers.Count > 0) candidateGroups.Add((renderers.ToArray(),true,false));
            }
            if (roadRoot != null) foreach (var renderer in roadRoot.GetComponentsInChildren<Renderer>(true))
                if (seen.Add(renderer)) candidateGroups.Add((new[]{renderer},false,false));
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
                        blocks=feedbackTransparency?FeedbackBlocksView(bounds,head,forward,side):SeverelyBlocks(bounds,transform.position,head,forward,side);
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
                    else if(inspectCombinedRoads&&bounds.max.y>player.position.y+clearance&&renderer.TryGetComponent<Collider>(out var collider))
                    {
                        for(int i=0;i<3&&!blocks;i++)
                        {
                            var target=head+forward*(i==0?0:viewAhead)+side*(i==1?-2:i==2?2:0);
                            var delta=target-transform.position;
                            blocks=delta.sqrMagnitude>.001f&&IntersectsView(bounds,transform.position,target)&&collider.Raycast(new Ray(transform.position,delta.normalized),out _,delta.magnitude);
                        }
                    }
                    if (blocks) break;
                }
                if(candidate.fullyHide)
                {
                    bool any=false;var combined=new Bounds();
                    foreach(var renderer in group)
                    {
                        if(renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                        if(!any){combined=renderer.bounds;any=true;}else combined.Encapsulate(renderer.bounds);
                    }
                    blocks=any&&(feedbackTransparency?FeedbackBlocksView(combined,head,forward,side):SeverelyBlocks(combined,transform.position,head,forward,side));
                    // A portal is hollow. Its collision mesh, unlike its AABB, preserves the opening.
                    bool hasMesh=false,meshBlocks=false;
                    if(feedbackTransparency&&blocks)foreach(var renderer in group)
                    {
                        if(renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy||!renderer.TryGetComponent<MeshCollider>(out var geometry))continue;
                        hasMesh=true;
                        for(int i=0;i<3&&!meshBlocks;i++)
                        {
                            var target=head+forward*(i==0?0:viewAhead)+side*(i==1?-1.5f:i==2?1.5f:0);
                            var delta=target-transform.position;
                            meshBlocks=geometry.Raycast(new Ray(transform.position,delta.normalized),out _,delta.magnitude);
                        }
                    }
                    if(hasMesh)blocks=meshBlocks;
                }
                // Treat an authored sign/arch as one visual: its lettering and beam
                // must not remain floating when a panel hides.
                foreach (var renderer in group)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    if(feedbackTransparency||candidate.explicitScenery&&!candidate.fullyHide)
                    {
                        if(blocks&&!renderer.forceRenderingOff&&!faded.ContainsKey(renderer))faded.Add(renderer,new TemporarySceneryFade(renderer,feedbackTransparency?feedbackOpacity:.4f));
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
            int body=0,road=0,ahead=0;
            for(int i=-1;i<=1;i++)
            {
                if(IntersectsView(bounds,camera,torso+side*(i*.65f)))body++;
                if(IntersectsView(bounds,camera,torso+forward*6+side*(i*1.2f)))road++;
                if(IntersectsView(bounds,camera,torso+forward*12+side*(i*1.2f)))ahead++;
            }
            // Covering the shark, or hanging across the lanes just ahead where enemies and bonuses are read.
            return body>=2&&road>=2||road>=2&&ahead>=2;
        }
        private bool FeedbackBlocksView(Bounds bounds,Vector3 head,Vector3 forward,Vector3 side)
        {
            if(bounds.SqrDistance(transform.position)>70*70)return false;
            // A small roof tile can cover the shark without also covering the road six metres ahead.
            // Sample the body and upcoming aisle separately; side walls parallel to travel stay solid.
            foreach(float ahead in FeedbackLookAhead)
                for(int lateral=-1;lateral<=1;lateral++)
                    if(IntersectsView(bounds,transform.position,head+forward*ahead+side*lateral*.8f))return true;
            return false;
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
