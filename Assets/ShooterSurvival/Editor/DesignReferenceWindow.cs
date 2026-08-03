#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class DesignReferenceWindow : EditorWindow
{
    private const string WindowTitle = "MeshyAI 자료 위치";
    private const string DesignFolderRelativePath = "docs/design";
    private const string NoryangjinMapPlanFolderRelativePath =
        "outputs/chapter_campaign_reference_orthogonal_20min";
    private const string MeshyImagesFolderRelativePath = "output/meshy_images";

    [MenuItem("Tools/맵 제작 도구/자료/자료 위치 안내", false, 2200)]
    public static void OpenPage()
    {
        DesignReferenceWindow window = GetWindow<DesignReferenceWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(540f, 300f);
        window.Show();
    }

    [MenuItem("Tools/맵 제작 도구/자료/에셋 설계도 폴더", false, 2201)]
    public static void OpenDesignFolder()
    {
        OpenFolder(DesignFolderRelativePath);
    }

    [MenuItem("Tools/맵 제작 도구/자료/노량진 맵 설계도 및 미리보기 폴더", false, 2202)]
    public static void OpenNoryangjinMapPlanFolder()
    {
        OpenFolder(NoryangjinMapPlanFolderRelativePath);
    }

    [MenuItem("Tools/맵 제작 도구/자료/Meshy 이미지 폴더", false, 2203)]
    public static void OpenMeshyImagesFolder()
    {
        OpenFolder(MeshyImagesFolderRelativePath);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "에셋 설계도, 노량진 맵 설계도와 미리보기, 생성된 Meshy 이미지의 위치입니다.",
            MessageType.Info);

        EditorGUILayout.Space(4f);
        DrawPathRow("에셋 설계도 폴더", DesignFolderRelativePath, "열기", OpenDesignFolder);
        DrawPathRow(
            "노량진 맵 설계도 및 미리보기 폴더",
            NoryangjinMapPlanFolderRelativePath,
            "열기",
            OpenNoryangjinMapPlanFolder);
        DrawPathRow("Meshy 이미지 폴더", MeshyImagesFolderRelativePath, "열기", OpenMeshyImagesFolder);
    }

    private static void DrawPathRow(string label, string relativePath, string buttonLabel, Action action)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
                EditorGUILayout.SelectableLabel(
                    relativePath,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            if (GUILayout.Button(buttonLabel, GUILayout.Width(92f), GUILayout.Height(38f)))
                action();
        }

        EditorGUILayout.Space(4f);
    }

    private static void OpenFolder(string relativePath)
    {
        string absolutePath = GetProjectPath(relativePath);
        if (!Directory.Exists(absolutePath))
        {
            ShowMissingPath(absolutePath);
            return;
        }

        OpenShellPath(absolutePath);
    }

    private static string GetProjectPath(string relativePath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string platformPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(projectRoot, platformPath));
    }

    private static void OpenShellPath(string absolutePath)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = absolutePath,
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning(
                $"[DesignReference] Could not open path through shell. Falling back to reveal. path={absolutePath}\n{ex.Message}");
            EditorUtility.RevealInFinder(absolutePath);
        }
    }

    private static void ShowMissingPath(string absolutePath)
    {
        string message = $"Path does not exist:\n{absolutePath}";
        EditorUtility.DisplayDialog(WindowTitle, message, "OK");
        UnityEngine.Debug.LogWarning($"[DesignReference] {message}");
    }
}
#endif
