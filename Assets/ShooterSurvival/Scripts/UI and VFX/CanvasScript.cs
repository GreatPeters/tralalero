using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

namespace IndianOceanAssets.ShooterSurvival
{
    public class CanvasScript : MonoBehaviour
    {
        public enum Platform
        {
            Mobile, PC
        }

        public Platform platform;

        [Space]

        public GameObject tapToPlayScreen;
        public GameObject pauseMenuUI;
        public GameObject settingsMenuUI;
        public Slider sensitivitySlider;
        public Slider volumeSlider;
        public GameObject gameOverUI;
        public GameObject youWinUI;
        public GameObject playerScoreUI;

        public GameObject scoreParent;
        public GameObject pauseButton;

        private PlayerScript playerScript;
        public static bool isGameOver = false;
        private TextMeshProUGUI playerScoreText;
        private Animator scorePopAnimator;
        private int previousScore = 0;
        private FTUE_script ftue_Script;


        private void Start()
        {
            // Apply settings to UI sliders and player

            if (platform == Platform.PC)
                GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            else
                GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);

            playerScript = FindFirstObjectByType<PlayerScript>();
            playerScoreText = playerScoreUI.GetComponent<TextMeshProUGUI>();
            playerScoreText.text = "0";
            scorePopAnimator = playerScoreUI.GetComponent<Animator>();

            TimeManager.timeFactor = 0;
            TimeManager.isGameRunning = false;
            isGameOver = false;

            LoadAndApplySettings();

            if (TimeManager.Instance.isForwardMarchScene == false) ftue_Script = FindFirstObjectByType<FTUE_script>();
        }

        private void Update()
        {
            if (playerScript.currentHealth == 0 && !isGameOver)
            {
                StartCoroutine(GameOverDelay(3f));
            }

            UpdateScore();
        }

        public void PlayerPressedStartButton()
        {
            TimeManager.timeFactor = 1;
            TimeManager.isGameRunning = true;

            tapToPlayScreen.SetActive(false);

            if (ftue_Script != null) StartCoroutine(ftue_Script.ShowDisplay(0, 3));
        }

        public void ChangeGameMode()
        {
            if (TimeManager.Instance.isForwardMarchScene)
                SceneManager.LoadScene("Forward March Mode");
            else
                SceneManager.LoadScene("Base Defend Mode");
        }

        public void ResetSettings()
        {
            SettingsManager.Instance.ResetSettings();
            LoadAndApplySettings();
        }

        public void ChangeSensitivity()
        {
            SettingsManager.Instance.moveSensitivity = sensitivitySlider.value;
            SettingsManager.Instance.SaveSettings();

            playerScript.moveSensitivity = SettingsManager.Instance.moveSensitivity;
        }

        public void ChangeVolume()
        {
            SettingsManager.Instance.soundVolume = volumeSlider.value;
            SettingsManager.Instance.SaveSettings();

            AudioListener.volume = SettingsManager.Instance.soundVolume;
        }

        private void LoadAndApplySettings()
        {
            // Update sliders and apply values to game
            sensitivitySlider.value = SettingsManager.Instance.moveSensitivity;
            volumeSlider.value = SettingsManager.Instance.soundVolume;

            playerScript.moveSensitivity = SettingsManager.Instance.moveSensitivity;
            AudioListener.volume = SettingsManager.Instance.soundVolume;
        }

        private IEnumerator GameOverDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameOver();
        }

        private void GameOver()
        {
            scoreParent.SetActive(false);
            pauseButton.SetActive(false);
            isGameOver = true;
            gameOverUI.SetActive(true);
            TimeManager.timeFactor = 0;
            TimeManager.isGameRunning = false;
        }

        public void YouWin()
        {
            if (isGameOver == true) return;

            scoreParent.SetActive(false);
            pauseButton.SetActive(false);

            isGameOver = true;
            TimeManager.isGameRunning = false;
            TimeManager.timeFactor = 0;

            if (playerScript != null) playerScript.PlayWinDance();

            Invoke(nameof(ShowWinScreen), 2);
        }

        private void ShowWinScreen()
        {
            youWinUI.SetActive(true);
        }

        private void UpdateScore()
        {
            int currentScore = playerScript.playerScore;

            if (currentScore != previousScore)
            {
                playerScoreText.text = currentScore.ToString();
                scorePopAnimator.SetTrigger("ScoreInc");
                previousScore = currentScore;
            }
        }

        public void ResumeGame()
        {
            pauseMenuUI.SetActive(false);
            TimeManager.timeFactor = 1;
            TimeManager.isGameRunning = true;

            scoreParent.SetActive(true);
            pauseButton.SetActive(true);
        }

        public void PauseGame()
        {
            if (!isGameOver)
            {
                scoreParent.SetActive(false);
                pauseButton.SetActive(false);
                settingsMenuUI.SetActive(false);
                pauseMenuUI.SetActive(true);
                TimeManager.timeFactor = 0;
                TimeManager.isGameRunning = false;
            }
        }

        public void LoadGame()
        {
            if (TimeManager.Instance.isForwardMarchScene)
                SceneManager.LoadScene("Forward March Mode");
            else
                SceneManager.LoadScene("Base Defend Mode");

            TimeManager.timeFactor = 1;
            TimeManager.isGameRunning = true;
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void SettingsMenu()
        {
            settingsMenuUI.SetActive(true);
            pauseMenuUI.SetActive(false);
        }
    }
}