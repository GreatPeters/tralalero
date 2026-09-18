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
        sensitivity.onValueChanged.AddListener(SetSensitivity);
    }

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
        sensitivity.SetValueWithoutNotify(settings.moveSensitivity);
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
    private void SetSensitivity(float value)
    {
        SettingsManager.Instance.moveSensitivity=value; SettingsManager.Instance.SaveSettings();
        var player=FindFirstObjectByType<PlayerScript>();if(player!=null)player.moveSensitivity=value;
    }
    private static bool HasWebUrl(string url) => System.Uri.TryCreate(url,System.UriKind.Absolute,out var uri)&&(uri.Scheme==System.Uri.UriSchemeHttps||uri.Scheme==System.Uri.UriSchemeHttp);
    public void OpenPrivacy() { if(HasWebUrl(privacyUrl)) Application.OpenURL(privacyUrl); }
    public void OpenTerms() { if(HasWebUrl(termsUrl)) Application.OpenURL(termsUrl); }
}
