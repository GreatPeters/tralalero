using System.IO;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;

// Builds Resources/BonusPad/BonusPad.prefab (bonus-wall concept 08: floor pad + hologram).
// Meshy supplies the two bases; band, column and shadow are procedural (BonusPadVisual).
// Run after tools/import-meshy-objects.cs ImportMeshyObjects.BonusPad0927:
//   unity command run_script --file tools/build-bonus-pad.cs --entry BuildBonusPad.Main
public static class BuildBonusPad
{
    const string Root = "Assets/ShooterSurvival/Resources/BonusPad";
    const string Bases = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925"; // ImportMeshyCharactersPaths.PrefabRoot

    public static object Main()
    {
        Directory.CreateDirectory(Root);
        var band = Mat("BonusPadBand", "Universal Render Pipeline/Unlit", false);
        var column = Mat("BonusPadColumn", "Universal Render Pipeline/Particles/Unlit", true);
        var shadow = Mat("BonusPadShadow", "Universal Render Pipeline/Particles/Unlit", false, transparent: true);

        var root = new GameObject("BonusPad");
        var normal = Base(root.transform, "B01_bonus_pad", "NormalBase");
        var cracked = Base(root.transform, "B02_bonus_pad_cracked", "CrackedBase");
        var pad = root.AddComponent<BonusPadVisual>();
        var so = new SerializedObject(pad);
        so.FindProperty("normalBase").objectReferenceValue = normal;
        so.FindProperty("crackedBase").objectReferenceValue = cracked;
        so.FindProperty("bandMaterial").objectReferenceValue = band;
        so.FindProperty("columnMaterial").objectReferenceValue = column;
        so.FindProperty("shadowMaterial").objectReferenceValue = shadow;
        so.ApplyModifiedPropertiesWithoutUndo();
        cracked.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, Root + "/BonusPad.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return new { prefab = Root + "/BonusPad.prefab" };
    }

    // Fit a base to 3.0 m corner to corner (2.6 m flat to flat), 0.22 m tall, sitting on y = 0.
    static GameObject Base(Transform parent, string id, string name)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Bases + "/" + id + ".prefab");
        if (source == null) throw new System.Exception("Missing base " + id);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        go.name = name;
        go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        var b = Bounds(go);
        float horizontal = 3f / Mathf.Max(b.size.x, b.size.z), vertical = .22f / Mathf.Max(.01f, b.size.y);
        go.transform.localScale = new Vector3(horizontal, vertical, horizontal);
        b = Bounds(go);
        go.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static Material Mat(string name, string shader, bool additive, bool transparent = false)
    {
        string path = Root + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find(shader);
        m.SetColor("_BaseColor", Color.white);
        if (additive || transparent)
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", additive ? 2 : 0);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0);
            m.SetFloat("_Cull", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
