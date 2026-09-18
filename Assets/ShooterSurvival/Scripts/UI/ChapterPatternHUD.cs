using TMPro;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public sealed class ChapterPatternHUD : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text title, description;
    private Object owner;
    private int priority;
    public void Show(Object source, string heading, string detail, int importance)
    {
        if (owner != null && owner != source && importance < priority) return;
        owner = source; priority = importance;
        if (title != null) title.text = heading;
        if (description != null) description.text = detail;
        if (panel != null) panel.SetActive(true);
    }
    public void Clear(Object source)
    {
        if (source != owner) return;
        owner = null; priority = 0;
        if (panel != null) panel.SetActive(false);
    }
    private void Update()
    {
        if (!TimeManager.isGameRunning && panel != null) panel.SetActive(false);
    }
}
