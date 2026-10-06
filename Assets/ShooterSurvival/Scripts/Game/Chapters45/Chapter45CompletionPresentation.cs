using System.Linq;
using TMPro;
using UnityEngine;

// Bound only to the ShoeTower result panel. Rewards and chapter progression
// remain owned by the physical goal and the existing CanvasScript transaction.
public sealed class Chapter45CompletionPresentation : MonoBehaviour
{
    public TMP_Text title, message;
    private string originalTitle, originalMessage, originalReturn;
    private TMP_Text returnLabel;
    private bool captured;

    private void OnEnable()
    {
        if (!captured)
        {
            originalTitle = title != null ? title.text : "";
            originalMessage = message != null ? message.text : "";
            returnLabel = GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.text == "돌아가기");
            originalReturn = returnLabel != null ? returnLabel.text : "";
            captured = true;
        }
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += RefreshLocale;
        Apply();
    }
    private void RefreshLocale(UnityEngine.Localization.Locale locale) => Apply();
    private void Apply()
    {
        Restore();
        var director = Chapter45Director.Active;
        if (gameObject.scene.name != "ShoeTower" || director == null
            || director.gameObject.scene != gameObject.scene || director.chapter != 5
            || !director.GoalClaimed || !director.goals.Any(g => g != null && g.offering && g.Claimed)) return;
        if (title != null) title.text = Chapter45PresentationText.Localize("공물을 찾았다!");
        if (message != null) message.text = Chapter45PresentationText.Localize("더 좋은 신발을 손에 넣었습니다");
        if (returnLabel != null) returnLabel.text = Chapter45PresentationText.Localize(originalReturn);
    }
    private void OnDisable()
    {
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= RefreshLocale;
        Restore();
    }
    private void Restore()
    {
        if (!captured) return;
        if (title != null) title.text = originalTitle;
        if (message != null) message.text = originalMessage;
        if (returnLabel != null) returnLabel.text = originalReturn;
    }
}
