using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Grade separation for the Noryangjin revamp safe copy: the indoor market (and its floor collider)
// climbs over the S3 east pier instead of being crossed at grade. Called by
// tools/install-noryangjin-revamp.cs after every other builder so it follows the finished layout.
// The outside pier deck already follows NoryangjinMarketBranch.Height and is not touched here.
public static class NoryangjinCrossingLiftBuilder
{
    public const string MeshAsset = "Assets/ShooterSurvival/Models/Generated/NoryangjinRevamp/CrossingLiftMeshes.asset";
    public const float S3Z = -7.25f, S3HalfWidth = 4.2f;
    const float HallHalfWidth = 16f, SubdivideMetres = 1f;

    public sealed class Report { public int moved, deformed, subdivided, hiddenRoads, columns; public readonly List<Transform> details = new(); }

    public static Report Apply(Transform root, Transform surfaces, Transform roads, NoryangjinMarketBranch branch, Material concrete)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Install in Edit Mode");
        var report = new Report();
        if (branch == null || branch.liftHeight <= 0) return report;
        AssetDatabase.DeleteAsset(MeshAsset);
        var meshes = new List<Mesh>();
        var hall = root.Find("MarketHall");

        // Underside slab of the raised market floor; it is lifted with everything else below.
        if (hall != null)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(slab.GetComponent<Collider>());
            slab.name = "CrossingUndersideSlab"; slab.transform.SetParent(hall, false);
            float mid = (branch.liftRiseStart + branch.liftFallEnd) * .5f, span = branch.liftFallEnd - branch.liftRiseStart;
            slab.transform.SetPositionAndRotation(branch.transform.position + branch.transform.forward * mid + Vector3.down * .5f, branch.transform.rotation);
            slab.transform.localScale = new Vector3(14.4f, .9f, span);
            slab.GetComponent<Renderer>().sharedMaterial = concrete;
        }

        foreach (Transform child in root.Cast<Transform>().ToArray())
            if (child != branch.transform) Visit(child, branch, meshes, report);
        foreach (Transform child in surfaces.Cast<Transform>().ToArray())
            if (!child.name.StartsWith("OutsidePierDeck")) Visit(child, branch, meshes, report);

        // Old S6 planks and pillars under the raised span would read as a road crossing S3 again.
        foreach (Transform tile in roads)
        {
            if (tile == surfaces) continue;
            var renderers = tile.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) continue;
            var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            Vector3 c = bounds.center;
            if (Mathf.Abs(Vector3.Dot(c - branch.transform.position, branch.transform.right)) > 6) continue;
            if (branch.HeightAt(c) < .6f || Mathf.Abs(c.z - S3Z) < S3HalfWidth + .6f) continue;
            foreach (var r in renderers) r.enabled = false;
            report.hiddenRoads++;
        }

        if (hall != null) report.columns = Columns(hall, branch, concrete);
        var visibility = root.GetComponentInChildren<NoryangjinInteriorDetailVisibility>(true);
        if (visibility != null) visibility.liftedDetails = report.details.ToArray();
        var occlusion=Object.FindFirstObjectByType<NoryangjinCameraOcclusion>();
        if(occlusion!=null&&hall!=null)occlusion.ConfigureWalkingFloor(surfaces.Find("IndoorTileSurface"),
            hall.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="WetFloor"||r.name=="CrossingUndersideSlab").ToArray());

        if (meshes.Count > 0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MeshAsset));
            AssetDatabase.CreateAsset(meshes[0], MeshAsset);
            for (int i = 1; i < meshes.Count; i++) AssetDatabase.AddObjectToAsset(meshes[i], MeshAsset);
            AssetDatabase.SaveAssets();
        }
        return report;
    }

    static bool UnderBay(Transform t)
    {
        for (var p = t; p != null; p = p.parent) if (p.name.StartsWith("HallSegment_")) return true;
        return false;
    }

    public static bool InHall(NoryangjinMarketBranch branch, Vector3 p)
        => Mathf.Abs(Vector3.Dot(p - branch.transform.position, branch.transform.right)) < HallHalfWidth;

    static bool Rigid(Transform t)
        => t.GetComponent<TMP_Text>() || t.GetComponent<SkinnedMeshRenderer>() || t.GetComponent<Animator>() || t.GetComponent<ParticleSystem>()
           || t.GetComponent<Light>() || t.GetComponent<AudioSource>() || t.GetComponent<Canvas>() || t.GetComponent<NoryangjinRevampEvent>()
           || t.GetComponent<NoryangjinBreakable>() || t.GetComponent<LODGroup>() || t.GetComponent<ReflectionProbe>()
           || PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject);

    static bool TryBounds(Transform t, out Bounds bounds)
    {
        bounds = default; bool any = false;
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    static bool Touches(NoryangjinMarketBranch branch, Bounds b)
    {
        // Does any part of these bounds sit over the lifted span of the market footprint?
        var o = branch.transform.position; var f = branch.transform.forward; var r = branch.transform.right;
        float d0 = float.MaxValue, d1 = float.MinValue, l0 = float.MaxValue, l1 = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
            float d = Vector3.Dot(c - o, f), l = Vector3.Dot(c - o, r);
            d0 = Mathf.Min(d0, d); d1 = Mathf.Max(d1, d); l0 = Mathf.Min(l0, l); l1 = Mathf.Max(l1, l);
        }
        return d1 > branch.liftRiseStart && d0 < branch.liftFallEnd && l1 > -HallHalfWidth && l0 < HallHalfWidth;
    }

    static void Visit(Transform t, NoryangjinMarketBranch branch, List<Mesh> meshes, Report report)
    {
        // TMP may not have generated its mesh in Edit Mode. Its renderer bounds can
        // then sit at the origin, outside this hall, while the sign panel is lifted.
        // Author text by its actual anchor instead of that unbuilt render buffer.
        if(t.GetComponent<TMP_Text>()!=null)
        {
            if(InHall(branch,t.position)){float h=branch.HeightAt(t.position);if(h>.001f){t.position+=Vector3.up*h;report.moved++;}}
            return;
        }
        var probe = t.GetComponent<ReflectionProbe>();
        if (probe != null)
        {
            // Stretch the box upward over the raised floor instead of moving the baked capture.
            var c = t.TransformPoint(probe.center); float top = 0;
            for (float s = -.5f; s <= .5f; s += .02f) top = Mathf.Max(top, branch.HeightAt(c + branch.transform.forward * probe.size.z * s));
            if (top > .001f && InHall(branch, c)) { probe.size += Vector3.up * top; probe.center += Vector3.up * top * .5f; report.moved++; }
            return;
        }
        if (TryBounds(t, out var bounds) ? !Touches(branch, bounds) : !(InHall(branch, t.position) && branch.HeightAt(t.position) > .001f))
            return;
        if (Rigid(t))
        {
            Vector3 p = TryBounds(t, out var b) ? b.center : t.position;
            if (!InHall(branch, p)) return;
            float h = branch.HeightAt(p);
            if (h > .001f) { t.position += Vector3.up * h; report.moved++; }
            // Pure scenery outside the bays can be hidden while the camera passes through from S3 below.
            if (h > 3 && t.GetComponentInParent<NoryangjinRevampEvent>(true) == null && t.GetComponentInChildren<NoryangjinRevampEvent>(true) == null
                && t.GetComponentInChildren<NoryangjinBreakable>(true) == null && !UnderBay(t)) report.details.Add(t);
            return;
        }
        var filter = t.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null) Deform(filter, branch, meshes, report);
        else if (filter == null)
        {
            var box = t.GetComponent<BoxCollider>();
            if (box != null) { float h = branch.HeightAt(t.TransformPoint(box.center)); box.center += t.InverseTransformVector(Vector3.up * h); }
        }
        foreach (Transform child in t.Cast<Transform>().ToArray()) Visit(child, branch, meshes, report);
    }

    static void Deform(MeshFilter filter, NoryangjinMarketBranch branch, List<Mesh> meshes, Report report)
    {
        var t = filter.transform; var original = filter.sharedMesh;
        var mesh = original;
        bool primitive = original.name == "Cube" && original.vertexCount == 24 || original.name == "Quad" && original.vertexCount == 4;
        if (primitive)
        {
            // Split along the local axis that runs with the branch so the surface can bend over the ramp.
            var local = t.InverseTransformDirection(branch.transform.forward);
            var scale = t.lossyScale; var extent = new Vector3(Mathf.Abs(local.x) * scale.x, Mathf.Abs(local.y) * scale.y, Mathf.Abs(local.z) * scale.z);
            int axis = extent.x >= extent.y && extent.x >= extent.z ? 0 : extent.y >= extent.z ? 1 : 2;
            float length = axis == 0 ? Mathf.Abs(scale.x) : axis == 1 ? Mathf.Abs(scale.y) : Mathf.Abs(scale.z);
            int segments = Mathf.Clamp(Mathf.CeilToInt(length / SubdivideMetres), 1, 512);
            if (segments > 1) { mesh = Subdivide(original, axis, segments); report.subdivided++; }
        }
        var copy = mesh == original ? Object.Instantiate(original) : mesh;
        copy.name = original.name + "_Lift_" + t.name;
        var vertices = copy.vertices; bool changed = false;
        for (int i = 0; i < vertices.Length; i++)
        {
            var w = t.TransformPoint(vertices[i]);
            if (!InHall(branch, w)) continue;
            float h = branch.HeightAt(w);
            if (h <= .0005f) continue;
            vertices[i] = t.InverseTransformPoint(w + Vector3.up * h); changed = true;
        }
        if (!changed) { if (copy != original) Object.DestroyImmediate(copy); return; }
        copy.vertices = vertices; copy.RecalculateBounds();
        if (primitive) { copy.RecalculateNormals(); copy.RecalculateTangents(); }
        filter.sharedMesh = copy; meshes.Add(copy); report.deformed++;
        var collider = t.GetComponent<MeshCollider>();
        if (collider != null && collider.sharedMesh == original) collider.sharedMesh = copy;
    }

    // Rebuilds Cube/Quad faces as grids split along `axis`, keeping each face's UV mapping.
    public static Mesh Subdivide(Mesh source, int axis, int segments)
    {
        var sv = source.vertices; var suv = source.uv; var sn = source.normals; var tris = source.triangles;
        var faces = new List<int[]>();
        var used = new HashSet<int>();
        for (int i = 0; i < sv.Length; i++)
        {
            if (used.Contains(i)) continue;
            var group = Enumerable.Range(0, sv.Length).Where(j => !used.Contains(j) && Vector3.Dot(sn[j], sn[i]) > .99f).ToArray();
            foreach (var j in group) used.Add(j);
            if (group.Length == 4) faces.Add(group);
        }
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var idx = new List<int>();
        foreach (var face in faces)
        {
            Vector3 n = sn[face[0]];
            var min = face.Select(j => sv[j]).Aggregate(Vector3.Min); var max = face.Select(j => sv[j]).Aggregate(Vector3.Max);
            int[] axes = Enumerable.Range(0, 3).Where(a => max[a] - min[a] > 1e-4f).ToArray();
            if (axes.Length != 2) continue;
            int ua = axes[0], va = axes[1];
            // Affine uv(p) from three corners.
            var c0 = sv[face[0]]; int j1 = face.First(j => Mathf.Abs(sv[j][va] - c0[va]) < 1e-4f && Mathf.Abs(sv[j][ua] - c0[ua]) > 1e-4f);
            int j2 = face.First(j => Mathf.Abs(sv[j][ua] - c0[ua]) < 1e-4f && Mathf.Abs(sv[j][va] - c0[va]) > 1e-4f);
            Vector2 uv0 = suv[face[0]], du = (suv[j1] - uv0) / (sv[j1][ua] - c0[ua]), dv = (suv[j2] - uv0) / (sv[j2][va] - c0[va]);
            int nu = ua == axis ? segments : 1, nv = va == axis ? segments : 1;
            int start = verts.Count;
            for (int b = 0; b <= nv; b++)
                for (int a = 0; a <= nu; a++)
                {
                    var p = c0; p[ua] = Mathf.Lerp(min[ua], max[ua], a / (float)nu); p[va] = Mathf.Lerp(min[va], max[va], b / (float)nv);
                    p[3 - ua - va] = c0[3 - ua - va];
                    verts.Add(p); uvs.Add(uv0 + du * (p[ua] - c0[ua]) + dv * (p[va] - c0[va]));
                }
            for (int b = 0; b < nv; b++)
                for (int a = 0; a < nu; a++)
                {
                    int i0 = start + b * (nu + 1) + a, i1 = i0 + 1, i2 = i0 + nu + 1, i3 = i2 + 1;
                    bool front = Vector3.Dot(Vector3.Cross(verts[i2] - verts[i0], verts[i1] - verts[i0]), n) > 0;
                    if (front) idx.AddRange(new[] { i0, i2, i1, i1, i2, i3 }); else idx.AddRange(new[] { i0, i1, i2, i1, i3, i2 });
                }
        }
        var mesh = new Mesh { name = source.name };
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(idx, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    static int Columns(Transform hall, NoryangjinMarketBranch branch, Material concrete)
    {
        var group = new GameObject("CrossingColumns").transform; group.SetParent(hall, false);
        int count = 0;
        for (float d = branch.liftRiseStart + 8; d < branch.liftFallEnd - 4; d += 8)
        {
            float h = branch.Height(d);
            if (h < 2.4f) continue;
            foreach (float side in new[] { -1f, 1f })
            {
                var p = branch.transform.position + branch.transform.forward * d + branch.transform.right * side * 6.9f;
                if (Mathf.Abs(p.z - S3Z) < S3HalfWidth + 1.2f) continue;
                var column = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(column.GetComponent<Collider>());
                column.name = "CrossingColumn"; column.transform.SetParent(group, false);
                float top = h - .95f, bottom = -2.8f;
                column.transform.SetPositionAndRotation(new Vector3(p.x, (top + bottom) * .5f, p.z), branch.transform.rotation);
                column.transform.localScale = new Vector3(.9f, top - bottom, .9f);
                column.GetComponent<Renderer>().sharedMaterial = concrete;
                count++;
            }
        }
        return count;
    }
}
