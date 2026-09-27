using UnityEngine;
using UnityEngine.UI;

// Tapered manga rays around all four edges, leaving the aiming corridor open.
public sealed class HighwayRushLines : MaskableGraphic
{
    private void Update()
    {
        if (isActiveAndEnabled) SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        var focus = rect.center + Vector2.up * rect.height * .08f;
        int count = HighwayChapter2Data.Count("speedLineCount");
        float perimeter = 2 * (rect.width + rect.height);
        for (int i = 0; i < count; i++)
        {
            float seed = Mathf.Repeat(i * .618034f, 1);
            float along = perimeter * (i + seed * .65f) / count;
            Vector2 edge;
            if (along < rect.width) edge = new Vector2(rect.xMin + along, rect.yMax);
            else if ((along -= rect.width) < rect.height) edge = new Vector2(rect.xMax, rect.yMax - along);
            else if ((along -= rect.height) < rect.width) edge = new Vector2(rect.xMax - along, rect.yMin);
            else edge = new Vector2(rect.xMin, rect.yMin + along - rect.width);

            var direction = (edge - focus).normalized;
            var normal = new Vector2(-direction.y, direction.x);
            float pulse = .5f + .5f * Mathf.Sin(Time.time * 14 + seed * Mathf.PI * 2);
            var tip = Vector2.Lerp(focus, edge, .60f + seed * .22f + pulse * .05f);
            var outside = edge + direction * 12;
            float width = (i % 5 == 0 ? 6.5f : 1.6f) + seed * 2;
            bool fadeAtHud = edge.y >= rect.yMax - .1f;
            // Thin light companions keep the black ink visible over dark asphalt.
            Ray(mesh, tip + normal * 3, outside + normal * 3, normal, width * .6f,
                new Color(1, 1, 1, .40f + pulse * .12f), fadeAtHud);
            Ray(mesh, tip, outside, normal, width, new Color(.015f, .02f, .03f, .8f + pulse * .18f), fadeAtHud);
        }
    }

    private static void Ray(VertexHelper mesh, Vector2 tip, Vector2 edge, Vector2 normal, float width, Color color, bool fadeAtHud)
    {
        int start = mesh.currentVertCount;
        var faint = color; faint.a *= .45f;
        var outer = color; if (fadeAtHud) outer.a = 0;
        var middle = Vector2.Lerp(tip, edge, .78f);
        mesh.AddVert(tip, faint, Vector2.zero);
        mesh.AddVert(middle - normal * width * .78f, color, Vector2.zero);
        mesh.AddVert(middle + normal * width * .78f, color, Vector2.zero);
        mesh.AddVert(edge - normal * width, outer, Vector2.zero);
        mesh.AddVert(edge + normal * width, outer, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start + 1, start + 3, start + 2);
        mesh.AddTriangle(start + 2, start + 3, start + 4);
    }
}
