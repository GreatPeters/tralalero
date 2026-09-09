using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;

public static class SharkCosmeticAssets
{
    private const string Folder = "Assets/ShooterSurvival/Resources/Cosmetics";
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        string catalogPath = Folder + "/Catalog.asset";
        if (File.Exists(catalogPath)) throw new InvalidOperationException("Catalog already exists; preserve it");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var renderer = player.transform.Find("Original").GetComponentInChildren<SkinnedMeshRenderer>();
        var source = renderer.sharedMesh;
        var mesh = new Mesh { name = "Shark skin and shoes", indexFormat = source.indexFormat,
            vertices = source.vertices, normals = source.normals, tangents = source.tangents, uv = source.uv,
            uv2 = source.uv2, colors32 = source.colors32, boneWeights = source.boneWeights, bindposes = source.bindposes };
        for (int shape = 0; shape < source.blendShapeCount; shape++)
            for (int frame = 0; frame < source.GetBlendShapeFrameCount(shape); frame++)
            {
                var dv = new Vector3[source.vertexCount]; var dn = new Vector3[source.vertexCount]; var dt = new Vector3[source.vertexCount];
                source.GetBlendShapeFrameVertices(shape, frame, dv, dn, dt);
                mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape), source.GetBlendShapeFrameWeight(shape, frame), dv, dn, dt);
            }
        var vertices = mesh.vertices; var triangles = source.triangles;
        float cutoff = vertices.Min(v => v.y) + (vertices.Max(v => v.y) - vertices.Min(v => v.y)) * .33f;
        var body = new List<int>(); var shoes = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            bool shoe = vertices[triangles[i]].y <= cutoff && vertices[triangles[i + 1]].y <= cutoff && vertices[triangles[i + 2]].y <= cutoff;
            (shoe ? shoes : body).AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
        }
        if (shoes.Count < triangles.Length * .05f || shoes.Count > triangles.Length * .55f) throw new InvalidOperationException("Unexpected shoe partition");
        mesh.subMeshCount = 2; mesh.SetTriangles(body, 0); mesh.SetTriangles(shoes, 1); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Folder + "/SharkSkinAndShoes.asset");
        var catalog = ScriptableObject.CreateInstance<CosmeticVisualCatalog>(); catalog.splitSharkMesh = mesh;
        catalog.previewModel = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(source));
        catalog.previewScale = renderer.transform.lossyScale.x;
        var head = renderer.bones.Single(b => b.name == "head");
        var baked = new Mesh(); renderer.BakeMesh(baked);
        var bakedHead = Quaternion.Inverse(renderer.transform.rotation) * (head.position - renderer.transform.position);
        float z = bakedHead.z - .35f;
        var headIndices = renderer.bones.Select((bone, index) => (bone, index)).Where(x => x.bone.name == "head" || x.bone.name == "headend").Select(x => x.index).ToArray();
        var weights = source.boneWeights;
        float top = baked.vertices.Where((v, i) => headIndices.Contains(weights[i].boneIndex0) && Mathf.Abs(v.z - z) < .4f && Mathf.Abs(v.x - bakedHead.x) < .5f).Max(v => v.y);
        // BakeMesh here includes the imported scale; applying TransformPoint would scale twice.
        Vector3 world = renderer.transform.position + renderer.transform.rotation * new Vector3(bakedHead.x, top + .015f, z) - Vector3.up * .12f;
        UnityEngine.Object.DestroyImmediate(baked);
        catalog.hatLocalPosition = head.InverseTransformPoint(world);
        catalog.hatLocalRotation = Quaternion.Inverse(head.rotation) * renderer.transform.rotation;
        catalog.hatLocalScale = Vector3.one * (.85f / head.lossyScale.x);
        var original = renderer.sharedMaterials[0];
        var entries = new List<CosmeticVisualCatalog.Entry>();
        var colors = new Dictionary<string, Color> {
            ["skin_original"] = Color.white, ["skin_coral"] = new Color(1.45f,.72f,.82f),
            ["skin_ice"] = new Color(.72f,1.08f,1.35f), ["skin_sand"] = new Color(1.4f,1.13f,.72f),
            ["shoes_original"] = Color.white, ["shoes_ruby"] = new Color(8f,.5f,.25f),
            ["shoes_mint"] = new Color(.7f,1.7f,.72f), ["shoes_gold"] = new Color(4f,2f,.45f) };
        foreach (var c in colors)
        {
            var material = new Material(original) { name = c.Key };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c.Value);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", new Color(.368f*c.Value.r,.368f*c.Value.g,.368f*c.Value.b));
            AssetDatabase.CreateAsset(material, Folder + "/" + c.Key + ".mat");
            entries.Add(new CosmeticVisualCatalog.Entry { key = c.Key, material = material, swatch = c.Value });
        }
        entries.Add(new CosmeticVisualCatalog.Entry { key = "hat_none", swatch = Color.gray });
        foreach (var key in new[] { "hat_cap", "hat_bucket", "hat_tophat" })
        {
            Color color = key == "hat_cap" ? new Color(.12f,.25f,.43f) : key == "hat_bucket" ? new Color(.74f,.56f,.23f) : new Color(.09f,.10f,.12f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = key };
            foreach (var prop in new[] { "_MainTex", "_BaseMap" }) if (material.HasProperty(prop)) material.SetTexture(prop, Texture2D.whiteTexture);
            foreach (var prop in new[] { "_BumpMap", "_DetailNormalMap", "_OcclusionMap", "_MetallicGlossMap" }) if (material.HasProperty(prop)) material.SetTexture(prop, null);
            foreach (var prop in new[] { "_Color", "_BaseColor" }) if (material.HasProperty(prop)) material.SetColor(prop, color);
            material.DisableKeyword("_NORMALMAP"); AssetDatabase.CreateAsset(material, Folder + "/" + key + ".mat");
            var band = new Material(material) { name = key + " band" };
            foreach (var prop in new[] { "_Color", "_BaseColor" }) if (band.HasProperty(prop)) band.SetColor(prop, new Color(.77f,.52f,.16f));
            AssetDatabase.CreateAsset(band, Folder + "/" + key + "_band.mat");
            var root = new GameObject(key);
            try
            {
                if (key == "hat_cap")
                {
                    Part(root, "Crown", Lathe(new[] { new Vector2(.38f,0), new Vector2(.38f,.08f), new Vector2(.34f,.22f),new Vector2(.25f,.34f),new Vector2(.08f,.4f),new Vector2(0,.41f) }), material, key + "_crown");
                    Part(root, "Visor", Visor(), material, key + "_visor");
                }
                else if (key == "hat_bucket")
                {
                    Part(root, "Crown and brim", Lathe(new[] {new Vector2(0,0),new Vector2(.51f,0),new Vector2(.52f,.025f),new Vector2(.33f,.08f),new Vector2(.28f,.39f),new Vector2(0,.39f)}), material, key);
                    Part(root, "Band", Lathe(new[] {new Vector2(.326f,.10f),new Vector2(.316f,.17f)}), band, key + "_bandmesh");
                }
                else
                {
                    Part(root, "Crown and brim", Lathe(new[] {new Vector2(0,0),new Vector2(.48f,0),new Vector2(.48f,.035f),new Vector2(.28f,.045f),new Vector2(.32f,.62f),new Vector2(0,.62f)}), material, key);
                    Part(root, "Band", Lathe(new[] {new Vector2(.286f,.07f),new Vector2(.292f,.16f)}), band, key + "_bandmesh");
                }
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + key + ".prefab");
                entries.Add(new CosmeticVisualCatalog.Entry { key = key, accessory = prefab, swatch = color });
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        catalog.entries = entries.ToArray(); AssetDatabase.CreateAsset(catalog, catalogPath); AssetDatabase.SaveAssets();
        return new { catalogPath, bodyTriangles = body.Count / 3, shoeTriangles = shoes.Count / 3, entries = entries.Count, cutoff };
    }
    private static void Part(GameObject root, string name, Mesh mesh, Material material, string assetName)
    {
        mesh.name = assetName; AssetDatabase.CreateAsset(mesh, Folder + "/" + assetName + ".asset");
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root.transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
    private static Mesh Lathe(Vector2[] profile)
    {
        const int segments = 32; var vertices = new List<Vector3>(); var tris = new List<int>(); var uv = new List<Vector2>();
        for (int ring = 0; ring < profile.Length; ring++) for (int i = 0; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments;
            vertices.Add(new Vector3(Mathf.Sin(a) * profile[ring].x, profile[ring].y, Mathf.Cos(a) * profile[ring].x));
            uv.Add(new Vector2(i / (float)segments, ring / (float)(profile.Length - 1)));
            if (ring == 0 || i == segments) continue;
            int n = ring * (segments + 1) + i, p = n - segments - 1;
            tris.AddRange(new[] {p,p+1,n,p+1,n+1,n});
        }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(tris,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    private static Mesh Visor()
    {
        var v = new List<Vector3>(); var t = new List<int>();
        for(int i=0;i<=20;i++)
        {
            float a=Mathf.Lerp(-1.3f,1.3f,i/20f);
            v.Add(new Vector3(Mathf.Sin(a)*.37f,0,Mathf.Cos(a)*.29f));
            v.Add(new Vector3(Mathf.Sin(a)*.49f,-.025f,Mathf.Cos(a)*.68f));
            if(i>0){int n=i*2;t.AddRange(new[]{n-2,n-1,n,n,n-1,n+1});}
        }
        var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
    }
}
