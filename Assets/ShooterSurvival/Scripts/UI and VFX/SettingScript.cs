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

            if (toggleOnSprite == null)
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
