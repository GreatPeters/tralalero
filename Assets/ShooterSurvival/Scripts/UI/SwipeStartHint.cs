using UnityEngine;
using UnityEngine.UI;

namespace IndianOceanAssets.ShooterSurvival
{
    // A small vector gesture, independent of font glyph support or sprite imports.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SwipeStartHint : MaskableGraphic
    {
        private float offset;
        private void Update()
        {
            offset = Mathf.Sin(Time.unscaledTime * 3f) * 28f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Color ink = new Color(.025f, .16f, .23f);
            for (int pass = 0; pass < 2; pass++)
            {
                float grow = pass == 0 ? 3 : 0;
                Color tint = pass == 0 ? ink : Color.white;
                // Raised index finger, curled middle/ring fingers, palm and thumb.
                Rect(mesh, offset - 9 - grow, -8 - grow, 18 + grow * 2, 47 + grow * 2, tint);
                Rect(mesh, offset + 8 - grow, -7 - grow, 14 + grow * 2, 25 + grow * 2, tint);
                Rect(mesh, offset + 21 - grow, -11 - grow, 13 + grow * 2, 22 + grow * 2, tint);
                Rect(mesh, offset - 12 - grow, -37 - grow, 48 + grow * 2, 34 + grow * 2, tint);
                Quad(mesh, new Vector2(offset - 12 - grow, -31), new Vector2(offset - 31 - grow, -6), new Vector2(offset - 24, 3 + grow), new Vector2(offset + 4, -20), tint);
            }
            Arrow(mesh, -96, -1, ink); Arrow(mesh, 96, 1, ink);
        }

        private static void Arrow(VertexHelper mesh, float x, float sign, Color tint)
        {
            Rect(mesh, x - 21, -3, 42, 9, tint);
            int i = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x + sign * 36, 1), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x + sign * 12, 21), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x + sign * 12, -19), tint, Vector2.zero);
            mesh.AddTriangle(i, i + 1, i + 2);
        }
        private static void Rect(VertexHelper mesh, float x, float y, float w, float h, Color tint)
            => Quad(mesh, new Vector2(x,y), new Vector2(x,y+h), new Vector2(x+w,y+h), new Vector2(x+w,y), tint);
        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int i = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero); mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero); mesh.AddVert(d, tint, Vector2.zero);
            mesh.AddTriangle(i, i+1, i+2); mesh.AddTriangle(i, i+2, i+3);
        }
    }
}
