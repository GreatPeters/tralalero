using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only grants use the wallet directly, so debug coins are not earned-run analytics.
public static class MapToolCurrencyCheats
{
    private static int coinAmount = 1000, jewelAmount = 100;
    private static string message;

    public static int AddClamped(int balance, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        return (int)Math.Min(int.MaxValue, (long)Math.Max(0, balance) + amount);
    }

    public static void Grant(int coins, int jewels)
    {
        if (coins < 0 || jewels < 0) throw new ArgumentOutOfRangeException("Grant amounts must be nonnegative.");
        var wallet = Application.isPlaying ? MoneyScript.S : null;
        int coin = AddClamped(wallet != null ? wallet.Coin : PlayerPrefs.GetInt("coin"), coins);
        int jewel = AddClamped(wallet != null ? wallet.Jewel : PlayerPrefs.GetInt("jewel"), jewels);
        if (wallet != null) { wallet.Coin = coin; wallet.Jewel = jewel; }
        else { PlayerPrefs.SetInt("coin", coin); PlayerPrefs.SetInt("jewel", jewel); PlayerPrefs.Save(); }
        message = $"지급 완료 · 코인 {coin:N0} / 보석 {jewel:N0}";
    }

    internal static void Draw()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("재화 지급 (에디터 치트)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                coinAmount = Mathf.Max(0, EditorGUILayout.IntField("코인", coinAmount));
                if (GUILayout.Button("코인 지급", GUILayout.Width(85))) Grant(coinAmount, 0);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                jewelAmount = Mathf.Max(0, EditorGUILayout.IntField("보석", jewelAmount));
                if (GUILayout.Button("보석 지급", GUILayout.Width(85))) Grant(0, jewelAmount);
            }
            EditorGUILayout.LabelField("입력한 만큼 더합니다. 현재 저장 재화에 반영됩니다.", EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.LabelField(message, EditorStyles.wordWrappedMiniLabel);
        }
    }
}

public static class ChapterSceneNavigation
{
    [MenuItem("Tools/맵 제작 도구/씬 이동/노량진", false, 2320)]
    public static void OpenNoryangjin() => Open(NoryangjinMapToolWindow.Sr18MapToolScenePath);
    [MenuItem("Tools/맵 제작 도구/씬 이동/고속도로", false, 2321)]
    public static void OpenHighway() => Open(NoryangjinMapToolWindow.HighwayMapToolScenePath);
    [MenuItem("Tools/맵 제작 도구/씬 이동/휴게소", false, 2322)]
    public static void OpenRestStop() => Open(NoryangjinMapToolWindow.RestStopMapToolScenePath);

    public static void Open(string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogWarning("플레이를 종료한 뒤 씬을 이동하세요."); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        { Debug.LogWarning("씬을 찾을 수 없습니다: " + path); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(path);
        NoryangjinMapToolWindow.Open();
    }
}
