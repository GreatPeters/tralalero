using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    /// <summary>
    /// Ground half of the bonus presentation: a Meshy hexagonal base, a colour-coded edge light band,
    /// a fading light column and a contact shadow. The floating icon and value text stay on
    /// BonusTalismanVisual (camera-facing). Setting-neutral so Noryangjin, HighWay and RestStop share it.
    /// Never owns colliders or rewards.
    /// </summary>
    public sealed class BonusPadVisual : MonoBehaviour
    {
        public const string ResourcePath = "BonusPad/BonusPad";
        public const float DesignWidth = 2.6f, MinWidth = 1.8f, ColumnHeight = 2.2f;
        public enum Mode { Normal, Negative, Random }

        [SerializeField] private GameObject normalBase, crackedBase;
        [SerializeField] private Material bandMaterial, columnMaterial, shadowMaterial;
        private MeshRenderer band, disc, column, shadow;
        private MaterialPropertyBlock block;
        private Transform owner;
        private Mode mode;
        private Color color = Color.white;
        private float width = DesignWidth, phase;
        private bool visible = true;
        private Camera viewCamera;

        public Mode CurrentMode => mode;
        public Color CurrentColor => color;
        public float Width => width;

        /// <summary>Find or create the pad under a bonus root.</summary>
        public static BonusPadVisual Ensure(Transform scope)
        {
            var existing = scope.Find("BonusPad");
            if (existing != null && existing.TryGetComponent(out BonusPadVisual found)) return found;
            var prefab = Resources.Load<BonusPadVisual>(ResourcePath);
            if (prefab == null) return null;
            var pad = Instantiate(prefab, scope);
            pad.name = "BonusPad";
            return pad;
        }

        /// <summary>Pad width that leaves a readable gap to the nearest other bonus root.</summary>
        public static float WidthForSpacing(float spacing) => Mathf.Clamp(spacing * .68f, MinWidth, DesignWidth);

        public void Configure(Transform follow, Color accent, Mode padMode, float padWidth)
        {
            owner = follow; color = accent; mode = padMode; width = padWidth;
            Build();
            visible = owner == null || owner.gameObject.activeInHierarchy;
            ApplyVisibility();
            Place();
            Tint(0);
        }

        private void Build()
        {
            if (band != null) return;
            block = new MaterialPropertyBlock();
            band = Part("EdgeBand", BandMesh(), bandMaterial);
            disc = Part("CenterRing", RingMesh(.40f, .47f, .205f), bandMaterial);
            column = Part("LightColumn", ColumnMesh(), columnMaterial);
            shadow = Part("ContactShadow", ShadowMesh(), shadowMaterial);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r == band || r == disc || r == column || r == shadow) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private MeshRenderer Part(string partName, Mesh mesh, Material material)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.receiveShadows = false;
            return r;
        }

        private void LateUpdate()
        {
            if (band == null) return;
            bool shown = owner == null || owner.gameObject.activeInHierarchy;
            if (shown != visible) { visible = shown; ApplyVisibility(); }
            if (!visible) return;
            Place();
            phase += Time.deltaTime;
            Tint(phase);
        }

        private void ApplyVisibility()
        {
            bool negative = mode == Mode.Negative;
            normalBase.SetActive(visible && !negative);
            crackedBase.SetActive(visible && negative);
            band.gameObject.SetActive(visible && !negative);
            disc.gameObject.SetActive(visible && !negative);
            column.gameObject.SetActive(visible);
            shadow.gameObject.SetActive(visible);
        }

        private void Place()
        {
            // Wall roots carry non-uniform, rotated scales; keep world size and a flat side toward the player.
            if (viewCamera == null) viewCamera = Camera.main;
            Vector3 forward = viewCamera != null ? Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            if (transform.parent != null) transform.position = transform.parent.position;
            transform.localScale = Vector3.one;
            Vector3 lossy = transform.lossyScale;
            float s = width / DesignWidth;
            transform.localScale = new Vector3(s / Mathf.Max(.001f, Mathf.Abs(lossy.x)), s / Mathf.Max(.001f, Mathf.Abs(lossy.y)), s / Mathf.Max(.001f, Mathf.Abs(lossy.z)));
        }

        private void Tint(float t)
        {
            Color c = mode == Mode.Random ? Color.HSVToRGB(Mathf.Repeat(t * .18f, 1), .75f, 1)
                : mode == Mode.Negative ? new Color(.52f, .28f, .78f) : color;
            float pulse = .85f + .15f * Mathf.Sin(t * 3.1f);
            // Script reloads restore the part renderers but not this non-serializable block.
            block ??= new MaterialPropertyBlock();
            block.Clear(); block.SetColor("_BaseColor", c * (1.25f * pulse)); band.SetPropertyBlock(block); disc.SetPropertyBlock(block);
            block.Clear(); block.SetColor("_BaseColor", new Color(c.r, c.g, c.b, (mode == Mode.Negative ? .32f : .5f) * pulse)); column.SetPropertyBlock(block);
            block.Clear(); block.SetColor("_BaseColor", new Color(0, 0, 0, .42f)); shadow.SetPropertyBlock(block);
        }

        // Geometry in design units (pad 2.6 wide flat-to-flat, 0.22 high); the root scale fits the lane.
        private static Mesh bandMesh, columnMesh, shadowMesh;
        private static readonly float Circum = DesignWidth * .5f / Mathf.Cos(30 * Mathf.Deg2Rad);

        private static Vector3 Corner(int i, float radius, float y)
        {
            float a = 60 * i * Mathf.Deg2Rad; // corners on ±X, so a flat side faces -Z (the player)
            return new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
        }

        public static Mesh BandMesh()
        {
            if (bandMesh != null) return bandMesh;
            var v = new Vector3[24]; var t = new int[36];
            float r = Circum * 1.012f;
            for (int i = 0; i < 6; i++)
            {
                v[i * 4] = Corner(i, r, .07f); v[i * 4 + 1] = Corner(i + 1, r, .07f);
                v[i * 4 + 2] = Corner(i, r, .15f); v[i * 4 + 3] = Corner(i + 1, r, .15f);
                int b = i * 4, k = i * 6;
                t[k] = b; t[k + 1] = b + 2; t[k + 2] = b + 1; t[k + 3] = b + 1; t[k + 4] = b + 2; t[k + 5] = b + 3;
            }
            bandMesh = Finish("BonusPadBand", v, t, null);
            return bandMesh;
        }

        public static Mesh RingMesh(float inner, float outer, float y)
        {
            const int n = 48; var v = new Vector3[n * 2]; var t = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                v[i * 2] = new Vector3(Mathf.Cos(a) * inner, y, Mathf.Sin(a) * inner);
                v[i * 2 + 1] = new Vector3(Mathf.Cos(a) * outer, y, Mathf.Sin(a) * outer);
                int j = (i + 1) % n, k = i * 6;
                t[k] = i * 2; t[k + 1] = j * 2; t[k + 2] = i * 2 + 1; t[k + 3] = i * 2 + 1; t[k + 4] = j * 2; t[k + 5] = j * 2 + 1;
            }
            return Finish("BonusPadRing", v, t, null);
        }

        public static Mesh ColumnMesh()
        {
            if (columnMesh != null) return columnMesh;
            var v = new Vector3[24]; var c = new Color[24]; var t = new int[36];
            float bottom = Circum * .77f, top = Circum * .54f;
            for (int i = 0; i < 6; i++)
            {
                v[i * 4] = Corner(i, bottom, .2f); v[i * 4 + 1] = Corner(i + 1, bottom, .2f);
                v[i * 4 + 2] = Corner(i, top, ColumnHeight); v[i * 4 + 3] = Corner(i + 1, top, ColumnHeight);
                c[i * 4] = c[i * 4 + 1] = Color.white; c[i * 4 + 2] = c[i * 4 + 3] = new Color(1, 1, 1, 0);
                int b = i * 4, k = i * 6;
                t[k] = b; t[k + 1] = b + 2; t[k + 2] = b + 1; t[k + 3] = b + 1; t[k + 4] = b + 2; t[k + 5] = b + 3;
            }
            columnMesh = Finish("BonusPadColumn", v, t, c);
            return columnMesh;
        }

        public static Mesh ShadowMesh()
        {
            if (shadowMesh != null) return shadowMesh;
            const int n = 32; var v = new Vector3[n + 1]; var c = new Color[n + 1]; var t = new int[n * 3];
            v[0] = new Vector3(0, .015f, 0); c[0] = Color.white;
            float r = Circum * 1.35f;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                v[i + 1] = new Vector3(Mathf.Cos(a) * r, .015f, Mathf.Sin(a) * r); c[i + 1] = new Color(1, 1, 1, 0);
                t[i * 3] = 0; t[i * 3 + 1] = (i + 1) % n + 1; t[i * 3 + 2] = i + 1;
            }
            shadowMesh = Finish("BonusPadShadow", v, t, c);
            return shadowMesh;
        }

        private static Mesh Finish(string meshName, Vector3[] v, int[] t, Color[] c)
        {
            var m = new Mesh { name = meshName, vertices = v, triangles = t };
            if (c != null) m.colors = c;
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }
}
