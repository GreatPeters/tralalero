using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StagePatternData))]
public class StagePatternDataEditor : Editor
{
    private int _chapterIndex;
    private int _stageIndex;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var chaptersProp = serializedObject.FindProperty("chapters");
        if (chaptersProp == null)
        {
            EditorGUILayout.HelpBox("chapters property not found.", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        DrawToolbar();
        EditorGUILayout.Space();

        if (chaptersProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No chapter data. Import from Data.xlsx first.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        _chapterIndex = Mathf.Clamp(_chapterIndex, 0, chaptersProp.arraySize - 1);

        string[] chapterLabels = BuildLabels("Chapter", chaptersProp.arraySize);
        _chapterIndex = EditorGUILayout.Popup("Chapter", _chapterIndex, chapterLabels);

        var chapterProp = chaptersProp.GetArrayElementAtIndex(_chapterIndex);
        var stagesProp = chapterProp.FindPropertyRelative("stages");
        if (stagesProp == null || stagesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No stage data in this chapter.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        _stageIndex = Mathf.Clamp(_stageIndex, 0, stagesProp.arraySize - 1);

        string[] stageLabels = BuildLabels("Stage", stagesProp.arraySize);
        _stageIndex = EditorGUILayout.Popup("Stage", _stageIndex, stageLabels);

        var stageProp = stagesProp.GetArrayElementAtIndex(_stageIndex);
        var stepsProp = stageProp.FindPropertyRelative("steps");
        if (stepsProp == null)
        {
            EditorGUILayout.HelpBox("steps property not found.", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Chapter {_chapterIndex + 1} / Stage {_stageIndex + 1}", EditorStyles.boldLabel);

        for (int i = 0; i < stepsProp.arraySize; i++)
        {
            DrawStep(stepsProp.GetArrayElementAtIndex(i), i);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Import From Excel"))
            {
                StagePatternDataImporter.ImportSelectedOrFirst();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Open Excel Asset"))
            {
                GameDataWorkbookEditor.OpenSourceWorkbook();
            }
        }
    }

    private static void DrawStep(SerializedProperty stepProp, int index)
    {
        var patternProp = stepProp.FindPropertyRelative("pattern");
        var difficultyProp = stepProp.FindPropertyRelative("obstacleDifficulty");

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField($"Step {index + 1}", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(patternProp, new GUIContent("Pattern"));
            EditorGUILayout.PropertyField(difficultyProp, new GUIContent("Difficulty"));
        }
    }

    private static string[] BuildLabels(string prefix, int count)
    {
        var labels = new string[count];
        for (int i = 0; i < count; i++)
            labels[i] = $"{prefix} {i + 1}";
        return labels;
    }
}
