using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IndianOceanAssets.ShooterSurvival.Ads;

public sealed class DefeatPresentation : MonoBehaviour
{
    public TMP_Text resultText;
    public Button rewardedButton, continueButton;
    public Button closeButton;
    public TMP_Text rewardedText, adStatusText;
    public TMP_Text causeText, adviceText, buildText;
    public void SetDamageCause(IndianOceanAssets.ShooterSurvival.PlayerScript player)
    {
        if(player==null)return;
        var cause=player.LastDamageCause;
        if(causeText!=null)causeText.text="마지막 피해  "+IndianOceanAssets.ShooterSurvival.PlayerDamageCauseText.Label(cause)+"\n<color=#B73034>-"+player.LastDamageAmount.ToString("0")+" HP</color>";
        if(adviceText!=null)adviceText.text=cause==IndianOceanAssets.ShooterSurvival.PlayerDamageCause.EnemyContact && player.gameObject.scene.name=="HighWay"
            ? "전투 통로에서는 한쪽 적을 먼저 처치하세요.\n접촉 시 남은 적 체력만큼 피해를 받습니다."
            : IndianOceanAssets.ShooterSurvival.PlayerDamageCauseText.Advice(cause);
        if(buildText!=null)buildText.text=$"이번 판  공격력 {player.ResolvedAttackDamage:0} · 최대 체력 {player.MaxHealth:0}";
    }
    private readonly RewardedCoinOffer offer = new();
    private RewardedAdsService ads;
    private const string LastRewardKey = "ads_last_reward_round";
    public bool ContinueRequested { get; private set; }
    private void OnEnable()
    {
        ContinueRequested = false;
        ads = RewardedAdsService.Instance;
        if (ads != null) { ads.Changed += RefreshAd; ads.Prepare(); }
        RefreshAd();
    }
    private void OnDisable() { if (ads != null) ads.Changed -= RefreshAd; }
    public void SetResult(float seconds, int coins, string roundId = null, int progressCoins = 0)
    {
        if (resultText != null) resultText.text = $"생존 {seconds:0}초\n획득 코인 {coins:N0}" + (progressCoins > 0 ? $"\n진행 보상 {progressCoins:N0} 포함" : "");
        var settings = RewardedAdsService.Instance?.Settings;
        offer.Reset(roundId, coins, settings != null ? settings.minimumBonusCoins : 20,
            settings != null ? settings.maximumBonusCoins : 500);
        RefreshAd();
    }
    public void WatchRewardedVideo()
    {
        if (!isActiveAndEnabled || IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning) return;
        if (ads == null || !ads.Ready || AlreadyRewarded || !offer.TryBegin(out int attempt)) { ads?.Prepare(); RefreshAd(); return; }
        string roundId = offer.RoundId;
        if (!ads.Show(() => {
            // Some providers deliver earned after closed, possibly after Continue reloads the scene.
            if (MoneyScript.S == null || PlayerPrefs.GetString(LastRewardKey, "") == roundId || !offer.TryClaim(attempt, out int bonus)) return;
            PlayerPrefs.SetString(LastRewardKey, roundId);
            MoneyScript.S.Coin = (int)System.Math.Min(int.MaxValue, (long)MoneyScript.S.Coin + bonus);
            if (this != null) RefreshAd();
        }, () => { if (this != null) { offer.Finish(attempt); RefreshAd(); } })) offer.Finish(attempt);
        RefreshAd();
    }
    private bool AlreadyRewarded => !string.IsNullOrEmpty(offer.RoundId) && PlayerPrefs.GetString(LastRewardKey, "") == offer.RoundId;
    private void RefreshAd()
    {
        if (rewardedButton != null) rewardedButton.interactable = offer.Eligible && !AlreadyRewarded && !offer.Showing && ads != null && ads.Ready;
        if (continueButton != null) continueButton.interactable = !offer.Showing;
        if (closeButton != null) closeButton.interactable = !offer.Showing;
        if (rewardedText != null) rewardedText.text = AlreadyRewarded || offer.Claimed ? "보상 받음" : $"광고 보고 +{offer.Coins:N0} 코인";
        if (adStatusText != null) adStatusText.text = offer.Claimed || AlreadyRewarded ? "코인이 추가되었습니다" : ads != null ? ads.Status : "광고 준비 중";
    }
    public void InvalidateOffer() => offer.Invalidate();
    public void ReturnToAltar() { if (!offer.Showing) ContinueRequested = true; }
}
