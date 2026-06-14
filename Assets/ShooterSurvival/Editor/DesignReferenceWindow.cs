#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class DesignReferenceWindow : EditorWindow
{
    private const string WindowTitle = "Design Reference";
    private const string DesignFolderRelativePath = "docs/design";
    private const string MeshyImagesFolderRelativePath = "output/meshy_images";

    [MenuItem("Tools/Design Reference/Open Page", false, 2200)]
    public static void OpenPage()
    {
        DesignReferenceWindow window = GetWindow<DesignReferenceWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(540f, 220f);
        window.Show();
    }

    [MenuItem("Tools/Design Reference/Open Excel List Folder", false, 2201)]
    public static void OpenDesignFolder()
    {
        OpenFolder(DesignFolderRelativePath);
    }

    [MenuItem("Tools/Design Reference/Open Meshy Image Folder", false, 2202)]
    public static void OpenMeshyImagesFolder()
    {
        OpenFolder(MeshyImagesFolderRelativePath);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use tralalero_meshy_asset_plan_kr.xlsx as the current Korean design list. " +
            "The MeshyAI-named Korean workbook is a legacy mirror rebuilt from that file.",
            MessageType.Info);

        EditorGUILayout.Space(4f);
        DrawPathRow("Design Excel list folder", DesignFolderRelativePath, "Open", OpenDesignFolder);
        DrawPathRow("Generated design images folder", MeshyImagesFolderRelativePath, "Open", OpenMeshyImagesFolder);
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
