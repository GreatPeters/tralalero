using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Essential proposal 15 (2026-10-06): the same character model should not move and look identical.
    // Variation is seeded from the actor's scene + hierarchy path, so a given actor looks the same on every
    // run (reproducible) while neighbours differ. Attack clips always play at authored speed so the
    // telegraph and the hit frame stay in sync; only idle/walk/run vary. Pooled actors re-apply on reset.
    public sealed class ActorVariation : MonoBehaviour
    {
        public const float MinSpeed = .88f, MaxSpeed = 1.12f, MinScale = .96f, MaxScale = 1.04f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), ColorId = Shader.PropertyToID("_Color");
        private static readonly HashSet<int> NonVaryingStates = new()
        {
            Animator.StringToHash(ForwardEnemyAnimationContract.AttackLoop),
            Animator.StringToHash(ForwardEnemyAnimationContract.AttackOnce),
            Animator.StringToHash(ForwardEnemyAnimationContract.Die),
        };

        private int seed;
        private bool captured, speedApplied, ownsAnimatorSpeed = true;
        private Vector3 baseScale, lastApplied;
        private MaterialPropertyBlock block;

        public int Seed => seed;

        public static int StableSeed(Transform t)
        {
            unchecked
            {
                int hash = t.gameObject.scene.name != null ? Animator.StringToHash(t.gameObject.scene.name) : 17;
                for (var p = t; p != null; p = p.parent) hash = hash * 31 + Animator.StringToHash(p.name) * 7 + p.GetSiblingIndex();
                return hash;
            }
        }

        public static float Hash01(int seed, int salt)
        {
            unchecked
            {
                uint x = (uint)seed * 2654435761u ^ (uint)salt * 2246822519u;
                x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; x *= 3266489917u; x ^= x >> 16;
                return (x & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        public static float SpeedFactor(int seed) => Mathf.Lerp(MinSpeed, MaxSpeed, Hash01(seed, 1));
        public static float PhaseSeconds(int seed) => Hash01(seed, 2) * .6f;
        public static float ScaleFactor(int seed) => Mathf.Lerp(MinScale, MaxScale, Hash01(seed, 3));
        public static Color Tint(int seed)
        {
            float brightness = Mathf.Lerp(.88f, 1.06f, Hash01(seed, 4));
            float warmth = Mathf.Lerp(-.06f, .06f, Hash01(seed, 5));
            return new Color(brightness * (1f + warmth), brightness, brightness * (1f - warmth), 1f);
        }

        public static ActorVariation Ensure(GameObject actor)
        {
            if (actor == null || !Application.isPlaying) return null;
            var variation = actor.GetComponent<ActorVariation>();
            if (variation == null) variation = actor.AddComponent<ActorVariation>();
            return variation;
        }

        // Called on spawn / new run. Restores the authored scale before applying the stable factor.
        public void Apply()
        {
            // Highway throwers retime their own animator to land the release frame; leave their speed alone.
            if (!captured) { seed = StableSeed(transform); ownsAnimatorSpeed = GetComponentInChildren<HighwayEnemyAnimation>(true) == null; }
            // Placement code may assign a new scale between spawns; treat that as the new authored base.
            if (!captured || transform.localScale != lastApplied) baseScale = transform.localScale;
            captured = true;
            transform.localScale = lastApplied = baseScale * ScaleFactor(seed);
            ApplyTint(Tint(seed));
            speedApplied = false;
        }

        private void ApplyTint(Color tint)
        {
            block ??= new MaterialPropertyBlock();
            // Skinned bodies only: weapons, warning rings and text use MeshRenderers whose colours
            // other systems animate, and a property block would freeze them.
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null) continue;
                    int id = material.HasProperty(BaseColorId) ? BaseColorId : material.HasProperty(ColorId) ? ColorId : -1;
                    if (id < 0) continue;
                    renderer.GetPropertyBlock(block, i);
                    block.SetColor(id, material.GetColor(id) * tint);
                    renderer.SetPropertyBlock(block, i);
                }
            }
        }

        // Idle/locomotion only. Leaves attack/death speed (and other retiming owners) untouched.
        public void UpdateAnimatorSpeed(Animator animator)
        {
            if (!ownsAnimatorSpeed || animator == null || !animator.enabled) return;
            bool varying = !NonVaryingStates.Contains(animator.GetCurrentAnimatorStateInfo(0).shortNameHash) && !animator.IsInTransition(0);
            if (varying) { animator.speed = SpeedFactor(seed); speedApplied = true; }
            else if (speedApplied) { animator.speed = 1f; speedApplied = false; }
        }
    }
}
