using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.ShooterSurvival
{
    /// <summary>Detached feedback survives disabling the collectible. Contains no reward logic.</summary>
    public sealed class BonusTalismanPickup : MonoBehaviour
    {
        public const float Duration = .68f;
        private BonusTalismanVisual visual;
        private PlayerScript player;
        private Transform body;
        private Vector3 bodyLocalPoint;
        private float surfaceOffset;
        private Vector3 start, iconScale;
        private SpriteRenderer pulse;
        private LineRenderer trail;
        private float elapsed;
        private float claimStamp;
        private Camera viewCamera;
        public float Elapsed => elapsed;
        public Vector3 TargetPosition
        {
            get
            {
                if (body == null) return Vector3.zero;
                Vector3 point = body.TransformPoint(bodyLocalPoint);
                return viewCamera != null ? point + (viewCamera.transform.position - point).normalized * surfaceOffset : point;
            }
        }

        public static BonusTalismanPickup Spawn(BonusTalismanVisual source, PlayerScript target)
        {
            var copy = Instantiate(source, source.transform.position, source.transform.rotation);
            copy.name = "BonusTalismanPickup";
            SceneManager.MoveGameObjectToScene(copy.gameObject, target.gameObject.scene);
            copy.transform.localScale = Vector3.one;
            copy.IsPickup = true;
            copy.labelRoot.SetActive(false);
            var effect = copy.gameObject.AddComponent<BonusTalismanPickup>();
            effect.visual = copy;
            effect.player = target;
            effect.claimStamp = target.lastWallTouchTime;
            effect.viewCamera = Camera.main;
            effect.start = copy.icon.transform.position;
            effect.iconScale = copy.icon.transform.localScale;
            effect.body = target.transform.Find("Original") ?? target.transform;
            // Use the visible body's largest skinned mesh, excluding shoes/accessories.
            SkinnedMeshRenderer main = null;
            float largest = 0;
            foreach (var renderer in effect.body.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                float volume = renderer.bounds.size.sqrMagnitude;
                if (volume > largest) { largest = volume; main = renderer; }
            }
            Vector3 center = main != null ? main.bounds.center : target.transform.position + Vector3.up * 1.6f;
            // The source mesh includes feet/tail. Aim above its center at the torso.
            // Project the glow in front of the body along the camera ray so opaque skin
            // cannot hide it. This preserves its screen position over the torso.
            if (main != null) center += Vector3.up * main.bounds.extents.y * .35f;
            effect.surfaceOffset = main != null ? main.bounds.extents.magnitude + .05f : .5f;
            effect.bodyLocalPoint = effect.body.InverseTransformPoint(center);
            var glowObject = new GameObject("TorsoPulse");
            glowObject.transform.SetParent(copy.transform);
            effect.pulse = glowObject.AddComponent<SpriteRenderer>();
            effect.pulse.sprite = Resources.Load<Sprite>("BonusTalisman/SoftGlow");
            effect.pulse.color = Color.clear;
            effect.pulse.sortingOrder = 4;
            var lineObject = new GameObject("AbsorptionTrail");
            lineObject.transform.SetParent(copy.transform);
            effect.trail = lineObject.AddComponent<LineRenderer>();
            effect.trail.sharedMaterial = Resources.Load<Material>("BonusTalisman/Trail");
            effect.trail.useWorldSpace = true;
            effect.trail.positionCount = 12;
            effect.trail.numCapVertices = 3;
            effect.trail.widthMultiplier = .045f;
            effect.trail.enabled = false;
            return effect;
        }

        private void Update()
        {
            if (player == null || !player.gameObject.activeInHierarchy || !TimeManager.isGameRunning || player.currentHealth <= 0 || player.lastWallTouchTime < claimStamp)
            { Destroy(gameObject); return; }
            elapsed += Time.deltaTime;
            Sample(elapsed);
            if (elapsed >= Duration) Destroy(gameObject);
        }

        // Also used by the native capture harness to inspect exact animation phases.
        public void Sample(float time)
        {
            if (visual == null || body == null) return;
            float open = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / .18f));
            visual.SetOpen(open);
            float travel = Mathf.SmoothStep(0, 1, Mathf.Clamp01((time - .15f) / .36f));
            Vector3 end = TargetPosition;
            Vector3 control = (start + end) * .5f + Vector3.up * .4f;
            Vector3 position = Curve(start, control, end, travel);
            visual.icon.transform.position = position;
            visual.icon.transform.localScale = iconScale * Mathf.Lerp(1, .12f, travel);
            visual.icon.color = new Color(1, 1, 1, 1 - Mathf.Clamp01((time - .44f) / .1f));
            visual.paper.localScale = Vector3.one * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((time - .2f) / .2f)));
            if (viewCamera != null)
            {
                visual.icon.transform.rotation = viewCamera.transform.rotation;
                pulse.transform.rotation = viewCamera.transform.rotation;
            }
            trail.enabled = travel > 0 && time < .55f;
            Color gold = new Color(1, .74f, .27f, 1 - Mathf.Clamp01((time - .43f) / .12f));
            trail.startColor = new Color(gold.r, gold.g, gold.b, 0);
            trail.endColor = gold;
            for (int i = 0; i < 12; i++)
                trail.SetPosition(i, Curve(start, control, end, Mathf.Lerp(Mathf.Max(0, travel - .45f), travel, i / 11f)));
            float pulseTime = Mathf.Clamp01((time - .43f) / .23f);
            float alpha = Mathf.Sin(pulseTime * Mathf.PI) * .65f;
            pulse.color = new Color(1, .76f, .34f, alpha);
            pulse.transform.position = end;
            pulse.transform.localScale = Vector3.one * Mathf.Lerp(.25f, 1.1f, pulseTime);
        }

        private static Vector3 Curve(Vector3 a, Vector3 b, Vector3 c, float t) =>
            (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;
    }
}
