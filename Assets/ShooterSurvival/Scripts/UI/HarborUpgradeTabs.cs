using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HarborUpgradeTabs : MonoBehaviour
{
    public GameObject permanent, chapters;
    public Button permanentTab, chapterTab;
    public Sprite activeSprite, inactiveSprite;
    private void Awake()
    {
        permanentTab.onClick.AddListener(ShowPermanent);
        chapterTab.onClick.AddListener(ShowChapters);
    }
    private void OnEnable() { if (Application.isPlaying) ShowPermanent(); }
    public void ShowPermanent() => Show(false);
    public void ShowChapters() { Show(true); GameAudioService.Play(GameSound.Tab); }
    private void Show(bool chapter)
    {
        permanent.SetActive(!chapter); chapters.SetActive(chapter);
        Style(permanentTab, !chapter); Style(chapterTab, chapter);
    }
    private void Style(Button tab, bool active)
    {
        ((Image)tab.targetGraphic).sprite = active ? activeSprite : inactiveSprite;
        var theme=GameUITheme.Current;
        tab.GetComponentInChildren<TMP_Text>().color = theme!=null ? (active?theme.buttonInk:theme.ink) : new Color(.16f,.08f,.025f);
    }
}
