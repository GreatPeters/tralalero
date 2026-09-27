using IndianOceanAssets.ShooterSurvival;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Video;

// Scene-authored chapter destinations preserve the same wallet and permanent upgrades.
public sealed class ChapterProgression : MonoBehaviour
{
    public int chapter = 1;
    public string nextScene = "HighWay";
    public VideoClip nextChapterMovie;
    public ChapterTransitionUI transitionUI;
    public TMPro.TMP_Text clearRewardText;
    public string nextChapterTitle = "고속도로";
    [TextArea] public string nextChapterCaption = "빠르게 달리는 차들 사이로, 새로운 길이 이어진다.";
    private bool advancing;
    public bool IsAdvancing => advancing;
    public float Elapsed { get; private set; }
    public bool Completed { get; private set; }
    public int ClearJewels { get; private set; }
    private void Update() { if (TimeManager.isGameRunning) Elapsed += Time.deltaTime; }
    public void BeginRun()
    {
        Elapsed = 0; Completed = false; advancing = false; ClearJewels = 0;
        foreach (var row in FindObjectsByType<HighwayEncounterRow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (row.gameObject.scene == gameObject.scene) row.ResetForRun();
        foreach (var hazard in FindObjectsByType<HighwayHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (hazard.gameObject.scene == gameObject.scene) hazard.ResetForRun();
        foreach (var traffic in FindObjectsByType<HighwayOncomingTraffic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (traffic.gameObject.scene == gameObject.scene) traffic.BeginRun();
        foreach (var holdout in FindObjectsByType<RestStopHoldout>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (holdout.gameObject.scene == gameObject.scene) holdout.BeginRun();
    }
    public void CompleteChapter()
    {
        if (Completed) return;
        Completed = true;
        if (MoneyScript.S != null)
        {
            string rewardKey = "chapter_rewarded_" + chapter;
            bool firstClear = PlayerPrefs.GetInt(rewardKey, 0) == 0;
            string setting = (firstClear ? "firstClearJewels_" : "replayClearJewels_") + gameObject.scene.name;
            if (EnvironmentVariableTables.TryGetFloat(setting, out float configured) && !float.IsNaN(configured) && !float.IsInfinity(configured))
            {
                ClearJewels = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp(configured, 0, 100)), int.MaxValue - MoneyScript.S.Jewel);
                PlayerPrefs.SetInt(rewardKey, 1);
                MoneyScript.S.Jewel += ClearJewels;
            }
        }
        if (clearRewardText != null) clearRewardText.text = ClearJewels > 0 ? $"완주 보상  보석 +{ClearJewels}" : "";
        PlayerPrefs.SetInt("chapter_unlocked", Mathf.Max(PlayerPrefs.GetInt("chapter_unlocked", 1), string.IsNullOrEmpty(nextScene) ? chapter : chapter + 1));
        PlayerPrefs.Save();
    }
    public void LoadNextChapter()
    {
        TryAdvanceAutomatically();
    }
    public bool TryAdvanceAutomatically()
    {
        if (!Completed || advancing || string.IsNullOrEmpty(nextScene) || !Application.CanStreamedLevelBeLoaded(nextScene)) return false;
        advancing = true; StartCoroutine(Advance()); return true;
    }
    private IEnumerator Advance()
    {
        GameAudioService.Play(GameSound.Chapter);
        TimeManager.isGameRunning = false; TimeManager.timeFactor = 0;
        yield return new WaitForSecondsRealtime(.7f);
        if (transitionUI != null)
        {
            yield return transitionUI.Present(nextChapterMovie, nextChapterTitle, nextChapterCaption, ClearJewels);
            transitionUI.ShowLoading();
        }
        // Release movie resources before loading the next scene to bound peak memory.
        TimeManager.timeFactor = 1;
        yield return SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single);
    }
    public void Replay()
    {
        IndianOceanAssets.ShooterSurvival.Analytics.GameplayAnalytics.EndRun(
            IndianOceanAssets.ShooterSurvival.Analytics.GameplayAnalytics.OutcomeAbandoned,FindFirstObjectByType<PlayerScript>());
        Load(gameObject.scene.name);
    }
    private static void Load(string name)
    {
        TimeManager.isGameRunning = false; TimeManager.timeFactor = 1;
        SceneManager.LoadScene(name);
    }
}
