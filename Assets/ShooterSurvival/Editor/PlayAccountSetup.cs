using System;
using System.Text.RegularExpressions;
using GooglePlayGames;
using GooglePlayGames.Editor;
using UnityEditor;
using UnityEngine;

// Saved Games requires a real Play Console game ID; never manufacture one from Firebase's project number.
public sealed class PlayAccountSetup : EditorWindow
{
    private string gameId = "";
    [MenuItem("Tools/Shooter Survival/Google Play Account Setup")]
    public static void Open() => GetWindow<PlayAccountSetup>("Google Play 계정 연결");
    private void OnEnable() { gameId = PlayGamesSettings.LoadInstance()?.AppId ?? ""; }
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Google Play Games 계정과 저장", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Play Console에서 게임 서비스를 만들고 Saved Games를 켜세요. Android OAuth 연결에는 앱 패키지와 서명 SHA-1을 사용합니다. Firebase 프로젝트 번호는 게임 ID가 아닙니다.", MessageType.Info);
        EditorGUILayout.LabelField("Android 패키지", PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android));
        gameId = EditorGUILayout.TextField("Play Games 게임 ID", gameId);
        using (new EditorGUI.DisabledScope(!Regex.IsMatch(gameId, @"\A[0-9]{5,20}\z")))
            if (GUILayout.Button("게임 ID 적용 및 Android 매니페스트 생성")) Configure(gameId);
        if (GUILayout.Button("공식 설정 안내 열기")) Application.OpenURL("https://developer.android.com/games/pgs/unity/unity-start");
        EditorGUILayout.HelpBox("서비스 ID 적용 후 테스트 계정을 등록하고, 내부 테스트 배포에서 로그인·두 기기 저장·탈퇴를 검증하세요. 클라우드 서버용 비밀키는 이 클라이언트에 넣지 않습니다.", MessageType.None);
    }
    public static object Configure(string appId)
    {
        if (!Regex.IsMatch(appId ?? "", @"\A[0-9]{5,20}\z")) throw new ArgumentException("A real numeric Play Games game ID is required");
#pragma warning disable CS0618
        GPGSProjectSettings.Instance.Set(GPGSUtil.APPIDKEY, appId);
        GPGSProjectSettings.Instance.Save();
        GPGSUtil.UpdateGameInfo();
        GPGSUtil.GenerateAndroidManifest();
        GPGSProjectSettings.Instance.Set(GPGSUtil.ANDROIDSETUPDONEKEY, true);
        GPGSProjectSettings.Instance.Save();
#pragma warning restore CS0618
        AssetDatabase.Refresh();
        return new { configured = true, package = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android), savedGamesMustBeEnabledInPlayConsole = true };
    }
}
