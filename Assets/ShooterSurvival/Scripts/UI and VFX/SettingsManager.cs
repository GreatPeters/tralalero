using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance;

        public float moveSensitivity = 1f;
        public float soundVolume = 1f;
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes

            LoadSettings();
        }

        public void LoadSettings()
        {
            moveSensitivity = PlayerPrefs.GetFloat("moveSensitivity", 1f);
            soundVolume = PlayerPrefs.GetFloat("soundVolume", 1f);
            soundEnabled = PlayerPrefs.GetInt("soundEnabled", 1) == 1;
            vibrationEnabled = PlayerPrefs.GetInt("vibrationEnabled", 1) == 1;

            ApplyAudioSettings();
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("moveSensitivity", moveSensitivity);
            PlayerPrefs.SetFloat("soundVolume", soundVolume);
            PlayerPrefs.SetInt("soundEnabled", soundEnabled ? 1 : 0);
            PlayerPrefs.SetInt("vibrationEnabled", vibrationEnabled ? 1 : 0);

            PlayerPrefs.Save();
        }

        public void ResetSettings()
        {
            moveSensitivity = 1f;
            soundVolume = 1f;
            soundEnabled = true;
            vibrationEnabled = true;

            SaveSettings();
            ApplyAudioSettings();
        }

        public void ApplyAudioSettings()
        {
            AudioListener.volume = soundEnabled ? soundVolume : 0f;
        }

        public void SetSoundEnabled(bool enabled)
        {
            soundEnabled = enabled;
            ApplyAudioSettings();
            SaveSettings();
        }

        public void SetVibrationEnabled(bool enabled)
        {
            vibrationEnabled = enabled;
            SaveSettings();
        }
    }
}
