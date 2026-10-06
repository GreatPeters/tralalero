using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HarborSettingsPanel : MonoBehaviour
{
    public CanvasScript owner;
    public Button soundButton, vibrationButton;
    public TMP_Text soundState, vibrationState;
    public Image soundTrack, vibrationTrack;
    public RectTransform soundKnob, vibrationKnob;
    public Slider volume, sensitivity;
    public GameObject runActions;
    public Button privacyButton, termsButton;
    public string privacyUrl, termsUrl;
    private bool resumeOnClose;

    private void Awake()
    {
        soundButton.onClick.AddListener(ToggleSound);
        vibrationButton.onClick.AddListener(ToggleVibration);
        volume.onValueChanged.AddListener(SetVolume);
        HideRemovedSensitivityRow();
    }

    // The sensitivity option was removed; scenes are re-laid out by the editor installer,
    // and this keeps any older scene copy from exposing the dead control.
    private void HideRemovedSensitivityRow()
    {
        var panel = transform.Find("Panel");
        if (panel == null) return;
        foreach (string name in new[] { "Sensitivity", "SensitivityLabel", "SensitivityIcon" })
        {
            var row = panel.Find(name);
            if (row != null) row.gameObject.SetActive(false);
        }
        // Essential proposal 13: the best record lives in Settings, in the freed slider space.
        if (panel.Find("BestRecord") == null && soundState != null)
        {
            bestRecord = Instantiate(soundState, panel);
            bestRecord.name = "BestRecord";
            var rect = bestRecord.rectTransform;
            rect.anchorMin = new Vector2(BestRecordRect.x, BestRecordRect.y); rect.anchorMax = new Vector2(BestRecordRect.z, BestRecordRect.w);
            rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            bestRecord.alignment = TextAlignmentOptions.Center; bestRecord.enableAutoSizing = true;
            bestRecord.fontSizeMin = 24; bestRecord.fontSizeMax = 40; bestRecord.raycastTarget = false;
            bestRecord.color = new Color(.05f, .12f, .32f);
        }
        else if (bestRecord == null) bestRecord = panel.Find("BestRecord")?.GetComponent<TMP_Text>();
    }

    public static readonly Vector4 BestRecordRect = new Vector4(.065f, .525f, .935f, .560f);
    private TMP_Text bestRecord;

    public void Open(bool duringRun)
    {
        resumeOnClose = duringRun;
        if (duringRun)
        {
            TimeManager.timeFactor = 0;
            TimeManager.isGameRunning = false;
            owner.pauseButton.SetActive(false);
        }
        gameObject.SetActive(true);
        runActions.SetActive(duringRun);
        var fit=transform.Find("Panel")?.GetComponent<AspectRatioFitter>();
        if(fit!=null)fit.aspectRatio=duringRun?.64f:.72f;
        Refresh();
    }

    public void Close()
    {
        if (IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsService.Instance?.BlockingConsentForm == true) return;
        var account = IndianOceanAssets.ShooterSurvival.Account.PlayAccountService.Instance;
        if (account != null && account.State is IndianOceanAssets.ShooterSurvival.Account.PlayAccountState.Conflict or IndianOceanAssets.ShooterSurvival.Account.PlayAccountState.Deleting) return;
        gameObject.SetActive(false);
        FindFirstObjectByType<PlayerScript>()?.ResetStartGesture();
        if (resumeOnClose) owner.ResumeGame();
    }

    private void OnEnable() { if (Application.isPlaying) Refresh(); }
    private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) Close(); }
    public void Refresh()
    {
        var settings = SettingsManager.Instance;
        if (settings == null) return;
        volume.SetValueWithoutNotify(settings.soundVolume);
        if (bestRecord != null) bestRecord.text = ChapterRunProgress.BestRecordText();
        Switch(soundState, soundTrack, soundKnob, settings.soundEnabled);
        Switch(vibrationState, vibrationTrack, vibrationKnob, settings.vibrationEnabled);
        if(privacyButton!=null)privacyButton.interactable=HasWebUrl(privacyUrl);
        if(termsButton!=null)termsButton.interactable=HasWebUrl(termsUrl);
    }
    private static void Switch(TMP_Text label, Image track, RectTransform knob, bool enabled)
    {
        label.text = enabled ? "켜짐" : "꺼짐";
        if(label.transform.parent==track.transform.parent){label.rectTransform.anchorMin=new Vector2(enabled?.06f:.40f,.1f);label.rectTransform.anchorMax=new Vector2(enabled?.59f:.94f,.9f);label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;}
        track.color = enabled ? new Color(1f,.83f,.04f) : new Color(.48f,.53f,.60f);
        knob.anchorMin = new Vector2(enabled ? .60f : .04f, .12f);
        knob.anchorMax = new Vector2(enabled ? .96f : .40f, .88f);
        knob.offsetMin = knob.offsetMax = Vector2.zero;
    }
    private void ToggleSound() { SettingsManager.Instance.SetSoundEnabled(!SettingsManager.Instance.soundEnabled); Refresh(); }
    private void ToggleVibration() { SettingsManager.Instance.SetVibrationEnabled(!SettingsManager.Instance.vibrationEnabled); Refresh(); }
    private void SetVolume(float value) { SettingsManager.Instance.soundVolume=value; SettingsManager.Instance.ApplyAudioSettings(); SettingsManager.Instance.SaveSettings(); }
    private static bool HasWebUrl(string url) => System.Uri.TryCreate(url,System.UriKind.Absolute,out var uri)&&(uri.Scheme==System.Uri.UriSchemeHttps||uri.Scheme==System.Uri.UriSchemeHttp);
    public void OpenPrivacy() { if(HasWebUrl(privacyUrl)) Application.OpenURL(privacyUrl); }
    public void OpenTerms() { if(HasWebUrl(termsUrl)) Application.OpenURL(termsUrl); }
}
