using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-only fresh-install reset: wipes this project's PlayerPrefs and the local Play account backups.
// Map tool settings live in EditorPrefs/SessionState, so they survive the reset.
public static class MapToolAccountReset
{
    private static string message;

    public static string LocalAccountBackupFolder => Path.Combine(Application.persistentDataPath, "PlayAccount");

    public static void ResetLocalAccount()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        string backups = LocalAccountBackupFolder;
        if (Directory.Exists(backups)) Directory.Delete(backups, true);
        CosmeticService.ReloadCatalog();
        message = "초기화 완료 · 다음 Play는 첫 실행 상태로 시작합니다.";
    }

    internal static void Draw()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("계정 초기화 (에디터 테스트)", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("초기화하기", GUILayout.Height(30f)) && EditorUtility.DisplayDialog(
                        "계정 초기화",
                        "재화, 챕터 진행, 업그레이드, 코스메틱, 튜토리얼, 설정 등 이 프로젝트의 PlayerPrefs와 로컬 계정 백업을 모두 삭제합니다.\n되돌릴 수 없습니다.",
                        "초기화",
                        "취소"))
                {
                    try { ResetLocalAccount(); }
                    catch (IOException e) { message = "로컬 백업 삭제 실패: " + e.Message; }
                }
            }
            EditorGUILayout.LabelField(
                EditorApplication.isPlayingOrWillChangePlaymode
                    ? "플레이를 종료하면 초기화할 수 있습니다."
                    : "첫 설치 상태로 되돌립니다. 클라우드(Google Play) 저장은 건드리지 않습니다.",
                EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.LabelField(message, EditorStyles.wordWrappedMiniLabel);
        }
        EditorGUILayout.Space(6f);
    }
}
