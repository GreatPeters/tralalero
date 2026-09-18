using UnityEngine;
using UnityEngine.UI;

namespace IndianOceanAssets.ShooterSurvival
{
    public class SettingScript : MonoBehaviour
    {
        [SerializeField] private Button soundToggleButton;
        [SerializeField] private Button vibrationToggleButton;
        [SerializeField] private Image soundToggleImage;
        [SerializeField] private Image vibrationToggleImage;
        [SerializeField] private Sprite toggleOnSprite;
        [SerializeField] private Sprite toggleOffSprite;
        [SerializeField] private Color toggleColor = Color.white;
        [SerializeField] private TMPro.TMP_Text soundStateText;
        [SerializeField] private TMPro.TMP_Text vibrationStateText;

        public void ConfigureMobile(Button sound, Button vibration, TMPro.TMP_Text soundLabel, TMPro.TMP_Text vibrationLabel)
        {
            soundToggleButton = sound; vibrationToggleButton = vibration;
            soundToggleImage = sound.targetGraphic as Image; vibrationToggleImage = vibration.targetGraphic as Image;
            soundStateText = soundLabel; vibrationStateText = vibrationLabel;
            toggleOnSprite = toggleOffSprite = null;
        }

        private void Awake()
        {
            CacheReferences();
            BindButtons();
            RefreshVisuals();
        }

        private void OnEnable()
        {
            RefreshVisuals();
        }

        public void ToggleSound()
        {
            if (SettingsManager.Instance == null)
                return;

            SettingsManager.Instance.SetSoundEnabled(!SettingsManager.Instance.soundEnabled);
            RefreshVisuals();
        }

        public void ToggleVibration()
        {
            if (SettingsManager.Instance == null)
                return;

            SettingsManager.Instance.SetVibrationEnabled(!SettingsManager.Instance.vibrationEnabled);
            RefreshVisuals();
        }

        private void CacheReferences()
        {
            soundToggleButton ??= GetOrAddButton("Sound_Toggle");
            vibrationToggleButton ??= GetOrAddButton("Vibration_Toggle");

            if (soundToggleImage == null && soundToggleButton != null)
                soundToggleImage = soundToggleButton.GetComponent<Image>();

            if (vibrationToggleImage == null && vibrationToggleButton != null)
                vibrationToggleImage = vibrationToggleButton.GetComponent<Image>();

            if (toggleOnSprite == null && soundStateText == null)
            {
                if (soundToggleImage != null)
                    toggleOnSprite = soundToggleImage.sprite;
                else if (vibrationToggleImage != null)
                    toggleOnSprite = vibrationToggleImage.sprite;
            }
        }

        private void BindButtons()
        {
            if (soundToggleButton != null)
            {
                soundToggleButton.onClick.RemoveListener(ToggleSound);
                soundToggleButton.onClick.AddListener(ToggleSound);
            }

            if (vibrationToggleButton != null)
            {
                vibrationToggleButton.onClick.RemoveListener(ToggleVibration);
                vibrationToggleButton.onClick.AddListener(ToggleVibration);
            }
        }

        private void RefreshVisuals()
        {
            if (SettingsManager.Instance == null)
                return;

            bool soundOn = SettingsManager.Instance.soundEnabled;
            bool vibrationOn = SettingsManager.Instance.vibrationEnabled;

            if (soundStateText != null && vibrationStateText != null)
            {
                soundStateText.text = soundOn ? "소리 켜짐" : "소리 꺼짐";
                vibrationStateText.text = vibrationOn ? "진동 켜짐" : "진동 꺼짐";
                var theme = GameUITheme.Current;
                if (theme != null)
                {
                    if (soundToggleImage != null) soundToggleImage.color = soundOn ? theme.primary : theme.card;
                    if (vibrationToggleImage != null) vibrationToggleImage.color = vibrationOn ? theme.primary : theme.card;
                }
                return;
            }

            if (soundToggleImage != null)
            {
                if (toggleOnSprite != null || toggleOffSprite != null)
                    soundToggleImage.sprite = soundOn ? toggleOnSprite : toggleOffSprite;

                soundToggleImage.color = toggleColor;
            }

            if (vibrationToggleImage != null)
            {
                if (toggleOnSprite != null || toggleOffSprite != null)
                    vibrationToggleImage.sprite = vibrationOn ? toggleOnSprite : toggleOffSprite;

                vibrationToggleImage.color = toggleColor;
            }
        }

        private Button GetOrAddButton(string objectName)
        {
            GameObject target = FindChildGameObject(objectName);
            if (target == null)
                return null;

            Button button = target.GetComponent<Button>();
            if (button == null)
                button = target.AddComponent<Button>();

            return button;
        }

        private GameObject FindChildGameObject(string objectName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == objectName)
                    return child.gameObject;
            }

            return null;
        }
    }
}
