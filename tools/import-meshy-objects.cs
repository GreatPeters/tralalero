using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Imports Meshy static models (vehicles/props) as outlined, ground-pivoted prefabs at game scale.
// Scale rule: characters are 2.3 units (~1.35x a 1.7 m person), so vehicles use real length x 1.35.
// Run: unity command run_script --file tools/import-meshy-objects.cs --entry ImportMeshyObjects.Main
//   --args '["outputs/meshy-reststop-2026-09-25","V01_compact_car,V02_white_sedan"]'
public static class ImportMeshyObjects
{
    const string ModelRoot = ImportMeshyCharactersPaths.ModelRoot, PrefabRoot = ImportMeshyCharactersPaths.PrefabRoot;

    // Longest horizontal side (vehicles, backdrops) or height (props), in game units.
    static readonly Dictionary<string, (float value, bool byLength)> Size = new()
    {
        { "V01_compact_car", (4.9f, true) }, { "V02_white_sedan", (6.6f, true) }, { "V03_black_suv", (6.5f, true) },
        { "V04_1ton_truck", (6.9f, true) }, { "V05_tour_bus", (16.2f, true) }, { "V06_patrol_car", (6.6f, true) },
        { "V07_taxi", (6.6f, true) }, { "V08_box_truck", (10f, true) },
        { "P01_pylon_sign", (16f, false) }, { "P02_walnut_cake_stall", (2.7f, false) }, { "P03_fishcake_stall", (2.7f, false) },
        { "P04_coffee_truck", (6.2f, true) }, { "P05_smoking_booth", (3.2f, false) }, { "P06_recycle_bins", (2f, false) },
        { "P07_walnut_bag", (.55f, false) }, { "P08_suitcase", (1f, false) }, { "P09_mountain_backdrop", (120f, true) },
        { "P10_hand_dryer", (.55f, false) }, { "P11_tray_return", (1.7f, false) }, { "P12_parasol_table", (3.1f, false) },
        { "V09_tow_truck", (8.1f, true) }, { "V10_arrow_truck", (8.8f, true) }, { "V11_tanker", (12f, true) },
        { "P13_apartment_block", (48f, false) }, { "P14_greenhouse", (32f, true) },
        { "A11_water_deer", (1.7f, false) }, { "V12_log_truck", (11f, true) }, { "P15_log", (3.2f, true) }, { "P16_pothole", (3.6f, true) },
        // 2026-09-26 main-building interior, ~1.3x real like the 2.3 m characters.
        { "I01_snack_gondola", (2.1f, false) }, { "I02_drink_fridge", (2.8f, false) }, { "I03_food_counter", (4.6f, false) },
        { "I04_dining_set", (1.25f, false) }, { "I05_order_kiosk", (2.3f, false) },
        // 2026-09-27 bonus pad bases: 3.0 m corner to corner = 2.6 m flat to flat. BonusPad prefab refits exactly.
        { "B01_bonus_pad", (3f, true) }, { "B02_bonus_pad_cracked", (3.1f, true) },
        // 2026-09-27 Noryangjin revamp: market turret truck and LPG cylinder, ~1.3x real like the characters.
        { "N01_turret_truck", (3.8f, true) }, { "N02_lpg_cylinder", (1.35f, false) },
        { "N03_forklift", (4.2f, true) }, { "N04_livefish_truck", (5.8f, true) },
        { "N05_harbor_crane", (17f, false) }, { "N06_red_lighthouse", (18f, false) },
        { "N07_market_cat", (.8f, false) }, { "N08_frozen_tuna", (2.4f, true) },
        { "N09_driven_turret", (4.7f, true) }, { "N10_crab_aquarium", (3.4f, true) },
        { "N11_fish_counter", (3.35f, true) }, { "N12_foam_box", (1.15f, true) },
        { "N15_refrigeration_unit", (3.8f, true) }, { "N16_auction_counter", (3.8f, true) },
        { "N17_tuna_ice_pallet", (3.2f, true) }, { "N18_coldstore_gateway", (15.5f, true) },
        // 2026-09-29 live-fish auction basin (Claude Code feedback pass).
        { "N19_live_fish_tub", (1.05f, true) },
    };
    // Visual review result: yaw (degrees) that turns each model's front toward +Z after length alignment.
    public static readonly Dictionary<string, float> FrontYaw = new();

    public static object Interior0926() => Main("outputs/meshy-interior-2026-09-26", "I01_snack_gondola,I02_drink_fridge,I03_food_counter,I04_dining_set,I05_order_kiosk");

    public static object BonusPad0927() => Main("outputs/meshy-bonuspad-2026-09-27", "B01_bonus_pad,B02_bonus_pad_cracked");

    public static object Noryangjin0927() => Main("outputs/meshy-noryangjin-2026-09-27", "N01_turret_truck,N02_lpg_cylinder");
    public static object Noryangjin0928() => Main("outputs/meshy-noryangjin-fix-2026-09-28", "N03_forklift,N04_livefish_truck,N05_harbor_crane,N06_red_lighthouse,N07_market_cat,N08_frozen_tuna");
    public static object NoryangjinInteriorV2() => Main("outputs/meshy-noryangjin-interior-v2-2026-09-28", "N09_driven_turret,N10_crab_aquarium,N11_fish_counter,N12_foam_box");
    public static object NoryangjinFeedbackV3() => Main("outputs/meshy-noryangjin-feedback-v3-2026-09-28", "N15_refrigeration_unit,N16_auction_counter,N17_tuna_ice_pallet,N18_coldstore_gateway");

    public static object NoryangjinClaude0929() => Main("outputs/meshy-noryangjin-claude-2026-09-29", "N19_live_fish_tub");

    public static object Main(string runDir, string idList)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var ids = idList.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
        Directory.CreateDirectory(ModelRoot); Directory.CreateDirectory(PrefabRoot);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var id in ids)
            {
                // Prefer a lower-poly remesh when one exists (parked vehicles appear by the hundred).
                var folder = Path.Combine(runDir, "models", id);
                var src = Directory.GetFiles(folder, "remesh*.fbx").OrderBy(f => f).LastOrDefault() ?? Directory.GetFiles(folder, "model*.fbx").OrderBy(f => f).Last();
                Directory.CreateDirectory(ModelRoot + "/" + id);
                string dst = ModelRoot + "/" + id + "/" + id + ".fbx";
                // A different source (e.g. a remesh) has new UVs: drop the old extracted texture so it is re-extracted.
                if (!File.Exists(dst) || new FileInfo(dst).Length != new FileInfo(src).Length)
                    foreach (var tex in Directory.GetFiles(ModelRoot + "/" + id).Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg")))
                        File.Delete(tex); // inside StartAssetEditing; the refresh below forgets the stale asset
                File.Copy(src, dst, true);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var reports = new List<object>();
        foreach (var id in ids) reports.Add(ImportOne(id));
        AssetDatabase.SaveAssets();
        return reports;
    }

    static object ImportOne(string id)
    {
        string folder = ModelRoot + "/" + id, fbx = folder + "/" + id + ".fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
        importer.animationType = ModelImporterAnimationType.None; importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
        importer.meshCompression = ModelImporterMeshCompression.Off; importer.isReadable = true;
        importer.SaveAndReimport();
        if (!Directory.GetFiles(folder).Any(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg")))
        {
            importer.ExtractTextures(folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        var texture = Directory.GetFiles(folder).Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg"))
            .Select(f => f.Replace('\\', '/'))
            .Where(f => !Path.GetFileName(f).Contains("normal") && !Path.GetFileName(f).Contains("metallic") && !Path.GetFileName(f).Contains("roughness"))
            .OrderByDescending(f => new FileInfo(f).Length).First(); // base color only
        var ti = (TextureImporter)AssetImporter.GetAtPath(texture);
        ti.maxTextureSize = id.StartsWith("P09") || id.StartsWith("P13") || id.StartsWith("V05") ? 2048 : 1024;
        ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.sRGBTexture = true; ti.SaveAndReimport();
        var material = OutlineMaterial(folder + "/" + id + "_Outline.mat", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        if (id.StartsWith("P09") && material.HasProperty("_OutlineWidth")) { material.SetFloat("_OutlineWidth", 0); EditorUtility.SetDirty(material); }

        var root = new GameObject("MRS_" + id);
        try
        {
            var fit = new GameObject("Fit").transform; fit.SetParent(root.transform, false);
            var orient = new GameObject("Orientation").transform; orient.SetParent(fit, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), orient);
            model.name = "Model";
            int tris = 0;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
                r.shadowCastingMode = id.StartsWith("P09") ? ShadowCastingMode.Off : ShadowCastingMode.On;
                var mf = r.GetComponent<MeshFilter>(); if (mf != null) tris += mf.sharedMesh.triangles.Length / 3;
            }
            var b = Bounds(model.transform, root.transform);
            var spec = Size[id];
            // Vehicles face along their longest axis; align it to +Z, then apply the reviewed front yaw.
            // Vehicles, logs (they roll about their length) and the deer (it runs along +Z) align length to Z.
            bool alignLength = id[0] == 'V' || id.StartsWith("P15") || id.StartsWith("A11");
            float yaw = alignLength && b.size.x > b.size.z ? 90 : 0;
            // Reviewed 2026-09-25: every Meshy vehicle came out nose toward -Z after length alignment.
            if (id[0] == 'V') yaw += 180;
            if (FrontYaw.TryGetValue(id, out var extra)) yaw += extra;
            orient.localRotation = Quaternion.Euler(0, yaw, 0);
            b = Bounds(model.transform, root.transform);
            float scale = spec.byLength ? spec.value / Mathf.Max(b.size.x, b.size.z) : spec.value / b.size.y;
            fit.localScale = Vector3.one * scale;
            b = Bounds(model.transform, root.transform);
            fit.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            b = Bounds(model.transform, root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/" + id + ".prefab");
            return new { id, tris, size = new[] { Math.Round(b.size.x, 2), Math.Round(b.size.y, 2), Math.Round(b.size.z, 2) }, yaw };
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // Same Noryangjin enemy outline treatment as tools/import-meshy-characters.cs.
    static Material OutlineMaterial(string path, Texture2D baseMap)
    {
        var reference = AssetDatabase.LoadAssetAtPath<Material>("Assets/JH/Model/Enemy/Garden_Spear/Material.001.mat");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(reference); AssetDatabase.CreateAsset(mat, path); }
        else mat.CopyPropertiesFromMaterial(reference);
        mat.shader = reference.shader; mat.shaderKeywords = reference.shaderKeywords;
        mat.SetTexture("_BaseMap", baseMap); mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_ColorDim")) mat.SetColor("_ColorDim", new Color(.86f, .86f, .88f, 1));
        mat.enableInstancing = true; EditorUtility.SetDirty(mat);
        return mat;
    }

    static Bounds Bounds(Transform model, Transform space)
    {
        bool first = true; var b = new Bounds();
        foreach (var m in model.GetComponentsInChildren<MeshFilter>(true))
            foreach (var v in m.sharedMesh.vertices)
            {
                var p = space.InverseTransformPoint(m.transform.TransformPoint(v));
                if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
            }
        if (first) throw new Exception("Empty geometry");
        return b;
    }
}

public static class ImportMeshyCharactersPaths
{
    public const string ModelRoot = "Assets/ShooterSurvival/Models/MeshyRestStop20260925";
    public const string PrefabRoot = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925";
}
