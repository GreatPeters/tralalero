#if UNITY_EDITOR
using System;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class EnemyEventAuthoring
{
    private const string EnemyParentName = "Enemies";
    private const string TargetContainerName = "Enemy Event Targets";

    public static EnemyEventMode DrawSettings(SerializedObject serializedController)
    {
        serializedController.Update();
        SerializedProperty eventMode =
            serializedController.FindProperty("eventMode");
        EditorGUILayout.PropertyField(eventMode, new GUIContent("이벤트"));
        EnemyEventMode mode = (EnemyEventMode)eventMode.intValue;

        EditorGUILayout.Space(3f);
        EditorGUILayout.LabelField("이동 설정", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(
            serializedController.FindProperty("targetPoint"),
            new GUIContent("이동 목표"));
        EditorGUILayout.PropertyField(
            serializedController.FindProperty("moveSpeed"),
            new GUIContent("이동 속도"));
        EditorGUILayout.PropertyField(
            serializedController.FindProperty("moveAnimation"),
            new GUIContent("이동 애니메이션"));
        EditorGUILayout.PropertyField(
            serializedController.FindProperty("arrivalDistance"),
            new GUIContent("도착 판정 거리"));

        if (!EnemyEventController.RequiresTarget(mode))
        {
            EditorGUILayout.HelpBox(
                "현재 이벤트는 이동하지 않습니다. 이동시키려면 이벤트를 '지정 위치 이동 후 공격' 또는 '시작 위치 ↔ 이동 목표 반복'으로 바꾸세요.",
                MessageType.None);
        }

        return mode;
    }

    public static void DrawTargetActions(EnemyEventController controller)
    {
        if (controller == null ||
            !EnemyEventController.RequiresTarget(controller.EventMode))
        {
            return;
        }

        if (controller.TargetPoint == null &&
            !EditorUtility.IsPersistent(controller) &&
            GUILayout.Button("이동 목표 만들기", GUILayout.Height(22f)))
        {
            CreateTarget(controller);
        }

        if (!controller.HasUsableTarget)
        {
            EditorGUILayout.HelpBox(
                "이동 목표를 지정해야 발동합니다. 목표는 적 자신의 자식이 아니어야 합니다.",
                MessageType.Warning);
        }
    }

    public static Transform CreateTarget(EnemyEventController controller)
    {
        if (controller == null ||
            EditorUtility.IsPersistent(controller) ||
            !controller.gameObject.scene.IsValid())
        {
            return null;
        }

        const string undoName = "Create Enemy Event Target";
        var targetObject = new GameObject(
            $"{controller.gameObject.name}_이동목표");
        Undo.RegisterCreatedObjectUndo(targetObject, undoName);
        Transform targetParent = ResolveTargetParent(controller, undoName);
        if (targetParent != null)
            targetObject.transform.SetParent(targetParent, true);
        targetObject.transform.position =
            controller.transform.position + controller.transform.forward * 4f;

        Undo.RecordObject(controller, undoName);
        controller.TargetPoint = targetObject.transform;
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        SceneView.RepaintAll();
        return targetObject.transform;
    }

    private static Transform ResolveTargetParent(
        EnemyEventController controller,
        string undoName)
    {
        Transform targetParent = controller.transform.parent;
        if (targetParent == null ||
            !string.Equals(
                targetParent.name,
                EnemyParentName,
                StringComparison.Ordinal) ||
            targetParent.parent == null)
        {
            return targetParent;
        }

        Transform targetContainer =
            targetParent.parent.Find(TargetContainerName);
        if (targetContainer != null)
            return targetContainer;

        var containerObject = new GameObject(TargetContainerName);
        Undo.RegisterCreatedObjectUndo(containerObject, undoName);
        containerObject.transform.SetParent(targetParent.parent, false);
        return containerObject.transform;
    }
}
#endif
