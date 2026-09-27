using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Makes pooled player projectiles readable from the rest-stop holdout's raised quarter-view camera:
    // larger body plus a bright trail. Pooled objects are restored when disabled, so ordinary shots elsewhere
    // keep their authored size.
    public sealed class ProjectileEmphasis : MonoBehaviour
    {
        public const float HoldoutScale = 2.4f;
        private Vector3 baseScale;
        private bool captured, active;
        private TrailRenderer trail;
        private static Material trailMaterial;

        public static void Apply(GameObject projectile, bool emphasize)
        {
            if (projectile == null) return;
            var e = projectile.GetComponent<ProjectileEmphasis>();
            if (e == null) { if (!emphasize) return; e = projectile.AddComponent<ProjectileEmphasis>(); }
            e.Set(emphasize);
        }

        private void Set(bool on)
        {
            if (!captured) { baseScale = transform.localScale; captured = true; }
            active = on;
            transform.localScale = on ? baseScale * HoldoutScale : baseScale;
            if (on && trail == null) trail = CreateTrail();
            if (trail != null) { trail.Clear(); trail.emitting = on; trail.enabled = on; }
        }

        private TrailRenderer CreateTrail()
        {
            var t = gameObject.AddComponent<TrailRenderer>();
            if (trailMaterial == null)
            {
                trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                trailMaterial.SetColor("_BaseColor", new Color(.1f, .45f, 1f, 1f));
            }
            t.sharedMaterial = trailMaterial; t.time = .32f; t.minVertexDistance = .2f;
            t.widthCurve = new AnimationCurve(new Keyframe(0, .85f), new Keyframe(1, .1f));
            t.startColor = new Color(.15f, .5f, 1f, 1f); t.endColor = new Color(.05f, .25f, .9f, 0f);
            t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }

        private void OnDisable()
        {
            if (!active) return;
            Set(false);
        }
    }
}
