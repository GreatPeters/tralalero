using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance;

        public float moveSensitivity = 1f;
        public float soundVolume = 1f;

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

            AudioListener.volume = soundVolume;
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("moveSensitivity", moveSensitivity);
            PlayerPrefs.SetFloat("soundVolume", soundVolume);

            PlayerPrefs.Save();
        }

        public void ResetSettings()
        {
            moveSensitivity = 1f;
            soundVolume = 1f;

            SaveSettings();
        }
    }
}