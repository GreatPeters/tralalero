using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChapterUpgradeCardUI : MonoBehaviour
{
    public int chapter;
    public Button buyButton;
    public TMP_Text title, effects, actionLabel;
    public GameObject lockMarker, coinIcon;
    public TMP_Text chapterLabel, rankLabel, priceLabel;
    public Image[] rankPips;
    public bool referencePresentation;
    public Sprite emptyPip, filledPip;
    public Image chapterArtwork, lockedShade;
    public TMP_Text lockedText;
    private MoneyScript wallet;
    private void Awake() => buyButton.onClick.AddListener(Buy);
    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        wallet = MoneyScript.S;
        if (wallet != null) wallet.onChanged += Refresh;
        ChapterUpgradeService.Changed += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (wallet != null) wallet.onChanged -= Refresh;
        ChapterUpgradeService.Changed -= Refresh;
    }
    public void Buy() { ChapterUpgradeService.Buy(chapter); Refresh(); }
    public void Refresh()
    {
        var catalog = ChapterUpgradeService.Catalog;
        var row = catalog != null ? System.Array.Find(catalog.entries, item => item.chapter == chapter) : null;
        if (row == null) { buyButton.interactable = false; return; }
        int level = ChapterUpgradeService.Level(chapter), cost = row.CostAtLevel(level);
        bool complete = level >= ChapterUpgradeDefinition.MaxLevel, unlocked = ChapterUpgradeService.UnlockedChapter >= chapter;
        if(rankLabel!=null)
        {
            title.text=chapter==1?"노량진 수산시장":row.title;
            chapterLabel.text=$"CHAPTER {chapter:00}";
            rankLabel.text=$"강화 {level} / {ChapterUpgradeDefinition.MaxLevel}";
            effects.text=!unlocked?"챕터 해금 후\n이용 가능":complete?
                $"최대 강화\n공격력 +{row.attackPercent*level:0.#}%\n체력 +{row.healthPercent*level:0.#}%":
                $"다음 강화\n공격력 +{row.attackPercent:0.#}%\n체력 +{row.healthPercent:0.#}%";
            actionLabel.text=complete?"강화 완료":"강화";priceLabel.text=complete?"MAX":cost.ToString("N0");
            buyButton.gameObject.SetActive(unlocked);rankLabel.gameObject.SetActive(unlocked);
            for(int i=0;i<rankPips.Length;i++){
                rankPips[i].gameObject.SetActive(unlocked||referencePresentation);
                rankPips[i].color=referencePresentation?Color.white:i<level?new Color(.03f,.70f,.89f):new Color(.64f,.70f,.77f);
                if(referencePresentation)rankPips[i].sprite=i<level?filledPip:emptyPip;
            }
            if(referencePresentation)
            {
                int multiplier=complete?level:1;
                effects.text=(complete?"최대 강화":"다음 강화")+$"\n공격력 +{row.attackPercent*multiplier:0.#}% / 체력 +{row.healthPercent*multiplier:0.#}%";
                effects.gameObject.SetActive(unlocked);
                if(chapterArtwork!=null)chapterArtwork.color=unlocked?Color.white:new Color(.62f,.65f,.69f);
                if(lockedShade!=null)lockedShade.gameObject.SetActive(!unlocked);
                if(lockedText!=null)lockedText.gameObject.SetActive(!unlocked);
            }
            lockMarker.SetActive(!unlocked);coinIcon.SetActive(unlocked&&!complete);
            buyButton.interactable=!complete&&unlocked&&wallet!=null&&wallet.Coin>=cost;
            return;
        }
        title.text = $"{row.title}   강화 {level} / {ChapterUpgradeDefinition.MaxLevel}";
        effects.text = complete ? $"공격력 +{row.attackPercent * level:0.#}%\n체력 +{row.healthPercent * level:0.#}%" :
            $"공격력 +{row.attackPercent * level:0.#}% > +{row.attackPercent * (level + 1):0.#}%\n체력 +{row.healthPercent * level:0.#}% > +{row.healthPercent * (level + 1):0.#}%";
        actionLabel.text = complete ? "최대 강화 완료" : !unlocked ? $"챕터 {chapter} 해금 후 가능" : $"강화   {cost:N0}";
        lockMarker.SetActive(!unlocked); coinIcon.SetActive(unlocked && !complete);
        buyButton.interactable = !complete && unlocked && wallet != null && wallet.Coin >= cost;
    }
}
