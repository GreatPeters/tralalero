using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class HighwayAssetImporter
{
    public const string Source = "C:/Users/ljh/Desktop/AI 프로그램/Trellis 자동화/결과물";
    public const string Models = "Assets/ShooterSurvival/Models/Highway/Props";
    public const string Prefabs = "Assets/ShooterSurvival/Prefabs/Highway/Props";
    private static readonly Dictionary<int, string> Labels = new()
    {
        [49]="가드레일 조각", [50]="공사 바리케이드", [51]="교통 드럼통", [52]="고속도로 표지판",
        [53]="과속 카메라", [54]="배송 트럭", [55]="타이어 더미", [56]="커브 화살표",
        [57]="연속 가드레일", [58]="중앙 분리대", [60]="요금소 부스", [61]="하이패스 게이트",
        [64]="방음벽", [65]="고속도로 가로등", [66]="도로 전광판", [67]="승용차", [68]="버스",
        [69]="대형 트럭", [71]="라바콘 줄", [72]="반사 유도봉", [73]="비상 전화",
        [76]="차선 유도봉", [77]="차단기", [78]="요금소 신호등", [79]="요금 단말기",
        [80]="파란 고속버스", [81]="빨간 승용차", [82]="초록 승용차", [83]="노란 탑차"
    };

    public static object ImportLatestProps()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var sourceFolders = Directory.GetDirectories(Source).Where(p => Regex.IsMatch(Path.GetFileName(p), @"^\d+_STAGE02_HWY_")).OrderBy(p => p).ToArray();
        if (sourceFolders.Length != 29) throw new InvalidOperationException("Expected29 reviewed Highway prop sources.");
        Directory.CreateDirectory(Models); Directory.CreateDirectory(Prefabs);
        var report = new List<object>();
        foreach (string source in sourceFolders)
        {
            string name = Path.GetFileName(source);
            int id = int.Parse(name.Substring(0, 3));
            string folder = Models + "/" + id.ToString("D3");
            Directory.CreateDirectory(folder);
            CopyVerified(source + "/model.fbx", folder + "/Model.fbx");
            foreach (string texture in Directory.GetFiles(source + "/textures", "*.png"))
                CopyVerified(texture, folder + "/" + Path.GetFileName(texture));
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string source in sourceFolders)
        {
            int id = int.Parse(Path.GetFileName(source).Substring(0, 3));
            string folder = Models + "/" + id.ToString("D3");
            string fbx = folder + "/Model.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None;
            importer.SaveAndReimport();
            foreach (string path in Directory.GetFiles(folder, "*.png"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                ti.maxTextureSize = 1024; ti.mipmapEnabled = true;
                if (Path.GetFileName(path).Contains("Normal")) ti.textureType = TextureImporterType.NormalMap;
                else if (!Path.GetFileName(path).Contains("BaseColor")) ti.sRGBTexture = false;
                ti.SaveAndReimport();
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "/Surface.mat");
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, folder + "/Surface.mat"); }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/BaseColor.png"));
            material.SetColor("_BaseColor", Color.white);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Normal.png");
            if (normal != null) { material.SetTexture("_BumpMap", normal); material.EnableKeyword("_NORMALMAP"); }
            material.SetFloat("_Smoothness", .28f); GeneratedStylizedSurface.Apply(material);
            var root = new GameObject(Labels[id]);
            try
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), root.transform);
                visual.name = "Visual";
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                var bounds = BoundsOf(root);
                float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                float size = DesiredSize(id);
                visual.transform.localScale *= size / longest;
                bounds = BoundsOf(root);
                visual.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                string prefabPath = Prefabs + "/HWY_" + id.ToString("D3") + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                var meshes = visual.GetComponentsInChildren<MeshFilter>().Select(m => m.sharedMesh).ToArray();
                report.Add(new { id, label = Labels[id], source, prefabPath, triangles = meshes.Sum(m => m.triangles.Length / 3), size = BoundsOf(root).size.ToString(), hashMatches = Hash(source + "/model.fbx") == Hash(fbx) });
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
        string output = "map-concepts/highway-enemies-2026-09-11/props-imported.json";
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return new { imported = report.Count, output };
    }

    public static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    private static float DesiredSize(int id) => id switch
    {
        54 or 69 or 83 => 7f, 68 or 80 => 9f, 67 or 81 or 82 => 4.5f,
        52 or 61 or 66 => 10f, 57 or 64 => 8f, 65 => 8f, 53 => 5f,
        60 => 3.5f, 49 or 50 or 58 or 71 or 77 => 3f,
        51 or 55 or 73 or 78 or 79 => 1.8f, _ => 1.3f
    };

    private static void CopyVerified(string source, string target)
    {
        if (File.Exists(target))
        {
            if (Hash(source) != Hash(target)) throw new InvalidOperationException("Different existing asset; preserve and review: " + target);
            return;
        }
        File.Copy(source, target);
    }
    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(stream));
    }
}
