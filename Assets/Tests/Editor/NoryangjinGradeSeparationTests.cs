using NUnit.Framework;
using UnityEngine;

// Grade separation over S3, the new wave patterns and the wet-floor spin (2026-09-28 Claude Code fixes).
public sealed class NoryangjinGradeSeparationTests
{
    [TestCase(0f, 0f)]
    [TestCase(2f, 0f)]
    [TestCase(40f, 9f)]
    [TestCase(52f, 9f)]
    [TestCase(64f, 9f)]
    [TestCase(100f, 0f)]
    [TestCase(150f, 0f)]
    public void RampProfileIsLevelOverS3AndZeroOutside(float d, float expected)
        => Assert.That(NoryangjinMarketBranch.RampProfile(d, 9, 2, 40, 64, 100, 6), Is.EqualTo(expected).Within(1e-3f));

    [Test]
    public void RampProfileIsContinuousAndNeverTooSteep()
    {
        float previous = 0, steepest = 0;
        for (float d = 0; d <= 110; d += .05f)
        {
            float h = NoryangjinMarketBranch.RampProfile(d, 9, 2, 40, 64, 100, 6);
            if (d > 0) steepest = Mathf.Max(steepest, Mathf.Abs(h - previous) / .05f);
            Assert.That(Mathf.Abs(h - previous), Is.LessThan(.03f), $"jump at {d}");
            previous = h;
        }
        Assert.That(Mathf.Atan(steepest) * Mathf.Rad2Deg, Is.LessThan(17f));
    }

    [Test]
    public void BranchPointCarriesTheLiftOnBothRoutes()
    {
        var go = new GameObject("BranchFixture");
        try
        {
            go.transform.position = new Vector3(124.3f, 0, 45); go.transform.forward = Vector3.back;
            var branch = go.AddComponent<NoryangjinMarketBranch>(); branch.liftHeight = 9;
            Assert.That(branch.Point(52, false).y, Is.EqualTo(9).Within(1e-3f));
            Assert.That(branch.Point(52, true).y, Is.EqualTo(9).Within(1e-3f));
            Assert.That(branch.HeightAt(new Vector3(80, 0, -7.25f)), Is.EqualTo(9).Within(1e-3f));
            Assert.That(branch.OnMarketFloor(new Vector3(124.3f, 0, -7.25f)), Is.True);
            Assert.That(branch.OnMarketFloor(new Vector3(99.3f, 0, -7.25f)), Is.False);
            branch.liftHeight = 0;
            Assert.That(branch.Point(52, false).y, Is.EqualTo(0).Within(1e-4f));
        }
        finally { Object.DestroyImmediate(go); }
    }

    [TestCase(NoryangjinWaveEvent.Kind.Side, -1, 0f, 0f, -1.5f, true)]
    [TestCase(NoryangjinWaveEvent.Kind.Side, -1, 0f, 0f, 1.5f, false)]
    [TestCase(NoryangjinWaveEvent.Kind.Side, 1, 0f, 0f, .2f, true)]
    [TestCase(NoryangjinWaveEvent.Kind.Gap, 0, 0f, 0f, 0f, false)]
    [TestCase(NoryangjinWaveEvent.Kind.Gap, 0, 0f, 0f, 1.2f, true)]
    [TestCase(NoryangjinWaveEvent.Kind.Gap, 0, 1f, 0f, 1.25f, false)]
    [TestCase(NoryangjinWaveEvent.Kind.Hunter, -1, 0f, .5f, .5f, true)]
    [TestCase(NoryangjinWaveEvent.Kind.Hunter, -1, 0f, .5f, -1f, false)]
    public void WavePatternsFloodTheRightLanes(NoryangjinWaveEvent.Kind kind, int side, float gap, float center, float lateral, bool flooded)
    {
        var zones = new Vector2[2];
        int count = NoryangjinWaveEvent.Flooded(kind, side, gap, center, 2, .75f, 1.1f, zones);
        Assert.That(NoryangjinWaveEvent.IsFlooded(lateral, zones, count), Is.EqualTo(flooded));
    }

    [Test]
    public void EveryWavePatternLeavesADryLaneOnThePier()
    {
        var zones = new Vector2[2];
        foreach (NoryangjinWaveEvent.Kind kind in System.Enum.GetValues(typeof(NoryangjinWaveEvent.Kind)))
            for (float param = -1; param <= 1.001f; param += .25f)
            {
                int count = NoryangjinWaveEvent.Flooded(kind, param < 0 ? -1 : 1, param, param * 1.4f, 2, .75f, 1.1f, zones);
                bool dry = false;
                for (float lateral = -1.9f; lateral <= 1.9f; lateral += .05f) dry |= !NoryangjinWaveEvent.IsFlooded(lateral, zones, count);
                Assert.That(dry, Is.True, $"{kind} {param}");
            }
    }

    [Test]
    public void SubdividedCubeKeepsTheBoxShapeAndUvRange()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var source = cube.GetComponent<MeshFilter>().sharedMesh;
            var mesh = NoryangjinCrossingLiftBuilder.Subdivide(source, 2, 10);
            Assert.That(mesh.bounds.size, Is.EqualTo(Vector3.one).Using(new Vector3EqualityComparer(1e-4f)));
            Assert.That(mesh.vertexCount, Is.GreaterThan(source.vertexCount));
            foreach (var uv in mesh.uv) { Assert.That(uv.x, Is.InRange(-1e-4f, 1.0001f)); Assert.That(uv.y, Is.InRange(-1e-4f, 1.0001f)); }
            // Every face keeps the source cube's outward winding.
            float Winding(Mesh m, int i)
            {
                var v = m.vertices; var t = m.triangles;
                var n = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                return Mathf.Sign(Vector3.Dot(n, (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3));
            }
            float outward = Winding(source, 0);
            for (int i = 0; i < mesh.triangles.Length; i += 3) Assert.That(Winding(mesh, i), Is.EqualTo(outward), $"triangle {i / 3}");
            for (int i = 0; i < mesh.vertexCount; i++)
                Assert.That(Vector3.Dot(mesh.normals[i], mesh.vertices[i]), Is.GreaterThan(0), $"normal {i}");
        }
        finally { Object.DestroyImmediate(cube); }
    }

    private sealed class Vector3EqualityComparer : System.Collections.Generic.IEqualityComparer<Vector3>
    {
        private readonly float tolerance;
        public Vector3EqualityComparer(float tolerance) => this.tolerance = tolerance;
        public bool Equals(Vector3 a, Vector3 b) => (a - b).sqrMagnitude <= tolerance * tolerance;
        public int GetHashCode(Vector3 v) => 0;
    }
}
