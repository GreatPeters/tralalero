using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Chapter45Choice : MonoBehaviour
{
    public enum ChoiceKind { RouteFork, ShieldOrAttack, FloorRoute }
    public ChoiceKind kind;
    public float distance, endDistance, rewardDistance;
    public float branchOffset = 18, transitionLength = 15, branchHalfWidth = 3;
    public float healFraction = .2f, attackPercent = 20;
    public int coinReward = 80;
    public bool grantShieldOnRisk;
    public int floor = -1;
    public float riskAttackPercent;
    public string riskDescription = "경비 2조";
    public bool OnCurrentFloor => director != null && (floor < 0 || director.CurrentFloor == floor);
    public string coinKey = "", leftName = "공원 길", rightName = "상점 길";
    public TMP_Text previewLabel;
    public GameObject leftRewardVisual, rightRewardVisual;
    public bool Selected { get; private set; }
    public int Selection { get; private set; } = -1;
    public bool Rewarded { get; private set; }
    public int ResolvedCoins { get; private set; }
    private Chapter45Director director;
    private bool previewed;

    public void ResetForRun(Chapter45Director owner)
    {
        director = owner; Selected = Rewarded = previewed = false; Selection = -1;
        ResolvedCoins = Mathf.RoundToInt(Chapter45Director.Setting(coinKey, coinReward, 0, 10000));
        if (leftRewardVisual != null) leftRewardVisual.SetActive(true);
        if (rightRewardVisual != null) rightRewardVisual.SetActive(true);
        if (previewLabel != null) previewLabel.text = Description;
    }
    public string Description => Chapter45PresentationText.Localize(RawDescription);
    private string RawDescription => kind == ChoiceKind.FloorRoute ? "← " + leftName + "     " + rightName + " →" : kind == ChoiceKind.ShieldOrAttack
        ? "← 1회 보호막     공격 +" + attackPercent.ToString("0") + "% →"
        : "← " + leftName + " · 회복 " + (healFraction * 100).ToString("0") + "%\n"
            + rightName + " · " + riskDescription + " · 코인 +" + ResolvedCoins + (riskAttackPercent > 0 ? " · 공격 +" + riskAttackPercent.ToString("0") + "%" : "") + (grantShieldOnRisk ? " · 보호막" : "") + " →";
    private void OnEnable() => UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += RefreshLocale;
    private void OnDisable() => UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= RefreshLocale;
    private void RefreshLocale(UnityEngine.Localization.Locale locale)
    { if (!Selected && previewLabel != null) previewLabel.text = Description; }
    public void Tick()
    {
        if (!OnCurrentFloor || director.IsTransferring) return;
        if (!previewed && director.Distance >= distance - Mathf.Max(18, director.Player.ForwardMoveSpeed * 4))
        { previewed = true; director.Announce("길을 선택하세요", Description, 4); }
        if (!Selected || Rewarded || director.Distance < rewardDistance) return;
        // Mandatory archive/shop fights own stop points before this reward.
        if (!director.ChoiceEncountersComplete(this)) return;
        Rewarded = true;
        if (kind == ChoiceKind.ShieldOrAttack)
        {
            if (Selection == 0) director.GrantShield(); else director.Player.ApplyRunAttackPercent(attackPercent);
        }
        else if (Selection == 0) director.Player.Heal(director.Player.MaxHealth * healFraction);
        else
        {
            if (MoneyScript.S != null && ResolvedCoins > 0) MoneyScript.S.GetCoin(ResolvedCoins);
            if (grantShieldOnRisk) director.GrantShield();
            if (riskAttackPercent > 0) director.Player.ApplyRunAttackPercent(riskAttackPercent);
        }
        if (leftRewardVisual != null) leftRewardVisual.SetActive(false);
        if (rightRewardVisual != null) rightRewardVisual.SetActive(false);
        director.Record("choice reward " + name + "=" + Selection);
        director.AnnounceReward("보상 획득", kind == ChoiceKind.ShieldOrAttack ? (Selection == 0 ? "1회 보호막" : "공격 +" + attackPercent.ToString("0") + "%")
            : Selection == 0 ? "체력 " + (healFraction * 100).ToString("0") + "% 회복" : "코인 +" + ResolvedCoins + (riskAttackPercent > 0 ? " · 공격 +" + riskAttackPercent.ToString("0") + "%" : "") + (grantShieldOnRisk ? " · 1회 보호막" : ""), 3);
        GameAudioService.Play(GameSound.Chapter);
    }
    public void Commit(float lane)
    {
        if (Selected || !OnCurrentFloor) return;
        Selected = true; Selection = lane > 0 ? 1 : 0;
        if (kind == ChoiceKind.FloorRoute) Rewarded = true;
        if (previewLabel != null) previewLabel.text = string.Empty;
        if (leftRewardVisual != null) leftRewardVisual.SetActive(Selection == 0);
        if (rightRewardVisual != null) rightRewardVisual.SetActive(Selection == 1);
        director.Record("choice committed " + name + "=" + Selection);
        director.Announce(Selection == 0 ? leftName : rightName, "선택한 길로 이동합니다", 2);
    }
    public float Offset(float atDistance, int selection)
    {
        if (kind != ChoiceKind.RouteFork || atDistance < distance || atDistance > endDistance) return 0;
        return Chapter45Route.BranchOffset(atDistance, distance, endDistance, selection == 0 ? -branchOffset : branchOffset, transitionLength);
    }
}
