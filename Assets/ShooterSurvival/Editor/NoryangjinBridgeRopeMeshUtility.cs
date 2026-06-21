#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NoryangjinBridgeRopeMeshUtility
{
    private const string BridgeMeshName = "SM_Bridge_Rope_Small_Fantasy";
    private const string SourceMeshPath = "Assets/polyperfect/Poly Universal Pack/Meshes/Fantasy/Bridges Fantasy/SM_Bridge_Rope_Small_Fantasy.fbx";
    private const string OutputFolder = "Assets/ShooterSurvival/Models/Generated/Noryangjin";
    private const string OutputMeshPath = OutputFolder + "/SM_Bridge_Rope_Small_Fantasy_NoEndDropRopes.asset";
    private const string TargetScenePath = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";
    private const string TargetBridgeName = "Road_Bridge_X+384_Z+11";
    private const string ReportPath = "Temp/NoryangjinBridgeRopeMeshReport.txt";

    [MenuItem("Tools/MeshyAI/Create Bridge Copy Without End Drop Ropes", false, 2315)]
    public static void CreateBridgeCopyWithoutEndDropRopes()
    {
        Mesh source = AssetDatabase.LoadAllAssetsAtPath(SourceMeshPath)
            .OfType<Mesh>()
            .FirstOrDefault(mesh => mesh.name == BridgeMeshName);

        if (source == null)
        {
            Debug.LogError($"[MeshyAI] Could not load source mesh {BridgeMeshName} from {SourceMeshPath}.");
            return;
        }

        Mesh trimmed = CreateTrimmedMesh(source, out int removedTriangles);
        EnsureFolder(OutputFolder);

        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(OutputMeshPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(trimmed, OutputMeshPath);
        }
        else
        {
            EditorUtility.CopySerialized(trimmed, existing);
            existing.name = trimmed.name;
            EditorUtility.SetDirty(existing);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(OutputMeshPath);

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(
            ReportPath,
            $"Source: {SourceMeshPath}\nRemoved triangles: {removedTriangles}\nOutput: {OutputMeshPath}\n");

        Debug.Log($"[MeshyAI] Created bridge copy without end drop ropes. Removed {removedTriangles} triangles.");
    }

    [MenuItem("Tools/MeshyAI/Replace Noryangjin Bridge With Trimmed Copy", false, 2316)]
    public static void ReplaceNoryangjinBridgeWithTrimmedCopy()
    {
        CreateBridgeCopyWithoutEndDropRopes();

        Mesh trimmed = AssetDatabase.LoadAssetAtPath<Mesh>(OutputMeshPath);
        if (trimmed == null)
        {
            Debug.LogError($"[MeshyAI] Could not load generated mesh at {OutputMeshPath}.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        GameObject target = FindTargetBridge();
        if (target == null)
        {
            Debug.LogError($"[MeshyAI] Could not find {TargetBridgeName} in {TargetScenePath}.");
            return;
        }

        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError($"[MeshyAI] {TargetBridgeName} has no MeshFilter.");
            return;
        }

        meshFilter.sharedMesh = trimmed;
        EditorUtility.SetDirty(meshFilter);

        MeshCollider meshCollider = target.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = trimmed;
            EditorUtility.SetDirty(meshCollider);
        }

        EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[MeshyAI] Replaced {TargetBridgeName} with {trimmed.name}.");
    }

    private static Mesh CreateTrimmedMesh(Mesh source, out int removedTriangles)
    {
        var trimmed = Object.Instantiate(source);
        trimmed.name = BridgeMeshName + "_NoEndDropRopes";

        Vector3[] vertices = source.vertices;
        removedTriangles = 0;

        trimmed.subMeshCount = source.subMeshCount;
        for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
        {
            int[] sourceTriangles = source.GetTriangles(subMesh);
            var keptTriangles = new List<int>(sourceTriangles.Length);

            for (int i = 0; i < sourceTriangles.Length; i += 3)
            {
                int a = sourceTriangles[i];
                int b = sourceTriangles[i + 1];
                int c = sourceTriangles[i + 2];
                if (ShouldRemoveTriangle(vertices[a], vertices[b], vertices[c]))
                {
                    removedTriangles++;
                    continue;
                }

                keptTriangles.Add(a);
                keptTriangles.Add(b);
                keptTriangles.Add(c);
            }

            trimmed.SetTriangles(keptTriangles, subMesh);
        }

        trimmed.RecalculateBounds();
        return trimmed;
    }

    private static bool ShouldRemoveTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float minX = Mathf.Min(a.x, b.x, c.x);
        float maxX = Mathf.Max(a.x, b.x, c.x);
        float minZ = Mathf.Min(a.z, b.z, c.z);
        float maxZ = Mathf.Max(a.z, b.z, c.z);

        bool outsideBridgeEnd = maxX < -34f || minX > 0.8f;
        bool outsideDeckWidth = minZ < -1.45f || maxZ > 1.45f;
        return outsideBridgeEnd && outsideDeckWidth;
    }

    private static GameObject FindTargetBridge()
    {
        GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject.name == TargetBridgeName)
                return sceneObject;
        }

        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
