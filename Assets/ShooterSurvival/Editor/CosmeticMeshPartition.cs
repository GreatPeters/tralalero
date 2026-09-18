#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CosmeticMeshPartition
{
    public static void Apply(Mesh source, Mesh destination, Transform[] bones)
    {
        var vertices = source.vertices;
        var weights = source.boneWeights;
        // Shoes end below the knee. The old 33% cut included the rear leg/hip
        // beneath the tail even after excluding tail-bone vertices.
        float cutoff = vertices.Min(v => v.y) + (vertices.Max(v => v.y) - vertices.Min(v => v.y)) * .23f;
        bool[] legBones = bones.Select(b => b != null && b.name.EndsWith("leg2", StringComparison.OrdinalIgnoreCase)).ToArray();
        bool IsShoeVertex(int index)
        {
            BoneWeight w = weights[index];
            float legWeight = (legBones[w.boneIndex0] ? w.weight0 : 0f)
                + (legBones[w.boneIndex1] ? w.weight1 : 0f)
                + (legBones[w.boneIndex2] ? w.weight2 : 0f)
                + (legBones[w.boneIndex3] ? w.weight3 : 0f);
            return vertices[index].y <= cutoff && legWeight >= .35f;
        }
        var body = new List<int>();
        var shoes = new List<int>();
        int[] triangles = source.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var target = IsShoeVertex(triangles[i]) && IsShoeVertex(triangles[i + 1]) && IsShoeVertex(triangles[i + 2]) ? shoes : body;
            target.Add(triangles[i]); target.Add(triangles[i + 1]); target.Add(triangles[i + 2]);
        }
        if (shoes.Count == 0 || body.Count == 0) throw new InvalidOperationException("Shark shoe partition requires both body and leg geometry.");
        destination.subMeshCount = 2;
        destination.SetTriangles(body, 0);
        destination.SetTriangles(shoes, 1);
        destination.RecalculateBounds();
    }
}
#endif
