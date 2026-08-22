#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [CustomEditor(typeof(AuthoredBonusWall))]
    [CanEditMultipleObjects]
    public sealed class AuthoredBonusWallEditor : Editor
    {
        private static readonly string[] GradeLabels =
            BonusAltarRules.CreateGradeLabels();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty rarity = serializedObject.FindProperty("rarity");
            SerializedProperty nearbyDistance = serializedObject.FindProperty("nearbyDistance");

            EditorGUILayout.LabelField("보너스 제단", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "등급에 맞는 능력치와 수치를 Data.xlsx의 <보너스> 시트에서 무작위로 뽑습니다. 가까운 제단끼리는 같은 능력치가 나오지 않습니다.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            int grade = EditorGUILayout.Popup(
                "제단 등급",
                Mathf.Clamp(rarity.enumValueIndex, 0, GradeLabels.Length - 1),
                GradeLabels);
            bool gradeChanged = EditorGUI.EndChangeCheck();
            if (gradeChanged)
            {
                foreach (Object selectedTarget in targets)
                {
                    if (selectedTarget is AuthoredBonusWall altar && altar.Wall != null)
                        Undo.RecordObject(altar.Wall, "Change Bonus Altar Grade");
                }

                rarity.enumValueIndex = grade;
            }

            EditorGUILayout.PropertyField(nearbyDistance, new GUIContent("인접 판정 거리"));
            bool changed = serializedObject.ApplyModifiedProperties();
            if (!changed)
                return;

            foreach (Object selectedTarget in targets)
            {
                if (selectedTarget is not AuthoredBonusWall altar)
                    continue;

                WallScript wall = altar.Wall;
                altar.Configure(altar.Rarity);
                PrefabUtility.RecordPrefabInstancePropertyModifications(altar);
                if (wall != null)
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(wall);
                    EditorUtility.SetDirty(wall);
                }
                EditorUtility.SetDirty(altar);
            }
        }
    }
}
#endif
