#if UNITY_EDITOR
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyEventController))]
public sealed class EnemyEventControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EnemyEventAuthoring.DrawSettings(serializedObject);
        serializedObject.ApplyModifiedProperties();
        EnemyEventAuthoring.DrawTargetActions((EnemyEventController)target);

        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox(
            "적 발동 스팟은 연결만 담당합니다. 실제 동작은 이 이벤트 설정이 결정합니다.",
            MessageType.Info);
    }

    private void OnSceneGUI()
    {
        var controller = (EnemyEventController)target;
        Transform targetPoint = controller.TargetPoint;
        if (targetPoint == null || EditorUtility.IsPersistent(targetPoint))
            return;

        EditorGUI.BeginChangeCheck();
        Vector3 nextPosition = Handles.PositionHandle(
            targetPoint.position,
            targetPoint.rotation);
        if (!EditorGUI.EndChangeCheck())
            return;

        Undo.RecordObject(targetPoint, "Move Enemy Event Target");
        targetPoint.position = nextPosition;
        EditorUtility.SetDirty(targetPoint);
    }
}
#endif
