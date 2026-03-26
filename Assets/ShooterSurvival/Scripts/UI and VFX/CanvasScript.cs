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

        public GameObject buttons;
        public GameObject pauseMenuUI;
        public GameObject settingsMenuUI;
        public Slider sensitivitySlider;
        public Slider volumeSlider;
        public GameObject gameOverUI;
        public GameObject youWinUI;
        public GameObject playerScoreUI;

        public GameObject scoreParent;
        public GameObject pauseButton;
        public GameObject bottom;
        public RectTransform startAreaImg;

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
                StartCoroutine(GameOverSequence(3f));

            UpdateScore();
        }

        public void PlayerPressedStartButton()
        {
            Debug.Log("??뽰삂!?");

            if (GameManager.S != null)
                GameManager.S.OnTapToPlay();
            else
            {
                TimeManager.timeFactor = 1;
                TimeManager.isGameRunning = true;
            }

            //tapToPlayScreen.SetActive(false);

            if (ftue_Script != null) StartCoroutine(ftue_Script.ShowDisplay(0, 3));

            if (playerScript != null)
                playerScript.ResetState();

        }

        public bool IsStartAreaActive()
        {
            return startAreaImg != null && startAreaImg.gameObject.activeInHierarchy;
        }

        public bool IsPointerOverStartArea(Vector2 screenPosition)
        {
            if (!IsStartAreaActive())
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(startAreaImg, screenPosition, null);
        }

        public void SetTapPromptVisible(bool visible)
        {
            if (startAreaImg != null)
                startAreaImg.gameObject.SetActive(visible);

            if (buttons == null)
                return;

            buttons.SetActive(visible);
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
            SettingsManager.Instance.ApplyAudioSettings();
        }

        private void LoadAndApplySettings()
        {
            // Update sliders and apply values to game
            sensitivitySlider.value = SettingsManager.Instance.moveSensitivity;
            volumeSlider.value = SettingsManager.Instance.soundVolume;

            playerScript.moveSensitivity = SettingsManager.Instance.moveSensitivity;
            SettingsManager.Instance.ApplyAudioSettings();
        }

        private IEnumerator GameOverSequence(float delay)
        {
            GameOver();

            yield return new WaitForSecondsRealtime(delay);

            if (gameOverUI != null) gameOverUI.SetActive(false);
            isGameOver = false;

            if (GameManager.S != null)
                GameManager.S.ResetAfterGameOver();
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

