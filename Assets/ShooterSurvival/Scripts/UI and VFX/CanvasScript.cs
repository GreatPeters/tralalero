using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using IndianOceanAssets.ShooterSurvival.Analytics;

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
        [SerializeField] private TextMeshProUGUI attackDebugText;
        [SerializeField] private PlayerStatusHud playerStatusHud;
        [SerializeField] private TextMeshProUGUI damagePopupPrefab;

        private PlayerScript playerScript;
        public static bool isGameOver = false;
        private TextMeshProUGUI playerScoreText;
        private Animator scorePopAnimator;
        private int previousScore = 0;
        private FTUE_script ftue_Script;
        private float activeRunSeconds;
        private int runStartingCoins;
        private string rewardRoundId;
        private bool progressRewardGranted;
        private int progressRewardCoins;
        private static bool reloadInputSuppressed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetReloadInput() => reloadInputSuppressed = false;

        private IEnumerator ReleaseReloadInput()
        {
            var group = GetComponent<CanvasGroup>();
            bool added = group == null;
            if (added) group = gameObject.AddComponent<CanvasGroup>();
            bool wasInteractable = group.interactable;
            group.interactable = false;
            // A fast second click must not activate a different button in the new lobby.
            yield return new WaitForSecondsRealtime(.3f);
            while (Input.GetMouseButton(0) || Input.touchCount > 0)
                yield return null;
            group.interactable = wasInteractable;
            if (added) Destroy(group);
            playerScript?.ResetStartGesture();
            reloadInputSuppressed = false;
        }



        private void Start()
        {
            // Apply settings to UI sliders and player

            if (platform == Platform.PC)
                GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            else
                GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);

            playerScript = FindFirstObjectByType<PlayerScript>();
            UpgradeStatManager.S?.SyncFromPurchasedLevels();
            playerScript?.ConfigureCanvasScript(this);
            playerScoreText = playerScoreUI.GetComponent<TextMeshProUGUI>();
            playerScoreText.text = "0";
            DamagePopupFX.Prewarm(damagePopupPrefab);
            scorePopAnimator = playerScoreUI.GetComponent<Animator>();
            attackDebugText ??= FindAttackDebugText();
            ResolvePlayerStatusHud();

            if (playerStatusHud != null && playerScript != null)
            {
                playerStatusHud.SetHealth(playerScript.currentHealth, playerScript.MaxHealth);
                playerStatusHud.SetAttack(playerScript.currentDamage);
            }

            TimeManager.timeFactor = 0;
            TimeManager.isGameRunning = false;
            isGameOver = false;
            SetAttackDebugVisible(false);

            LoadAndApplySettings();

            if (TimeManager.Instance.isForwardMarchScene == false) ftue_Script = FindFirstObjectByType<FTUE_script>();
            if (reloadInputSuppressed) StartCoroutine(ReleaseReloadInput());
            speedToggle = GameSpeedToggle.Create(this);
            ChapterProgressHud.Create(this);
            rowGate = EnemyRowGate.Ensure(this);
        }

        private GameSpeedToggle speedToggle;
        private EnemyRowGate rowGate;

        private void Update()
        {
            // X1/X2 only applies to live gameplay; lobby, menus and transitions run at normal speed.
            GameSpeed.SetRunning(TimeManager.isGameRunning && !isGameOver);
            // While the overlay replaces this scene asynchronously, the old scene must not
            // re-detect the dead player and run the defeat sequence (sound/analytics) again.
            if (LoadingOverlay.IsLoading) return;
            if (speedToggle != null)
            {
                bool show = TimeManager.isGameRunning && !isGameOver && pauseButton != null && pauseButton.activeSelf;
                if (speedToggle.gameObject.activeSelf != show) speedToggle.gameObject.SetActive(show);
            }
            if (TimeManager.isGameRunning) activeRunSeconds += Time.deltaTime * Mathf.Max(0f, TimeManager.timeFactor);
            if (playerScript.currentHealth == 0 && !isGameOver)
                StartCoroutine(GameOverSequence(3f));

            UpdateScore();
        }

        public void PlayerPressedStartButton()
        {
            if (reloadInputSuppressed) return;
            if (Account.PlayAccountService.Instance?.BlocksGameplay == true) return;
            if (settingsMenuUI != null && settingsMenuUI.activeInHierarchy || pauseMenuUI != null && pauseMenuUI.activeInHierarchy) return;
            if (OpeningStoryUI.IsBlockingGameplay || Ads.RewardedAdsService.Instance?.BlockingConsentForm == true || FindFirstObjectByType<CosmeticShopUI>() != null || TimeManager.isGameRunning || isGameOver) return;
            activeRunSeconds = 0f;
            rowGate?.ResetGate();
            rewardRoundId = System.Guid.NewGuid().ToString("N");
            progressRewardGranted = false; progressRewardCoins = 0;
            gameOverUI?.GetComponent<DefeatPresentation>()?.InvalidateOffer();
            runStartingCoins = MoneyScript.S != null ? MoneyScript.S.Coin : 0;
            EquipmentRunEffects.Apply(CosmeticService.Current, UpgradeStatManager.S);
            Debug.Log("??뽰삂!?");

            bool usesSceneFallback = GameManager.S == null;
            if (GameManager.S != null)
                GameManager.S.OnTapToPlay();
            else
            {
                NoryangjinTurnSpot.ResetAllForNewRun();
                EnemyEventController.ResetAllForNewRun();
                EnemyEventActivationSpot.ResetAllForNewRun();
                TimeManager.timeFactor = 1;
                TimeManager.isGameRunning = true;
            }

            SetTapPromptVisible(false);
            // Horizontal-start input bypasses the legacy Start button's UI events.
            if (pauseButton != null) pauseButton.SetActive(true);

            if (ftue_Script != null) StartCoroutine(ftue_Script.ShowDisplay(0, 3));

            if (playerScript != null)
            {
                playerScript.ResetState();
                playerScript.LogWeaponDamageDebug("GameStart");
                SetAttackDebugVisible(true);
                UpdateAttackDebugText(playerScript.currentDamage);

                if (usesSceneFallback)
                {
                    NoryangjinUpgradeExtraHelpSpawner extraHelpSpawner =
                        FindFirstObjectByType<NoryangjinUpgradeExtraHelpSpawner>();
                    extraHelpSpawner?.ApplyUpgradeExtraHelps(playerScript);
                }
            }

            GameplayAnalytics.BeginRun(playerScript);
            FindFirstObjectByType<ChapterProgression>()?.BeginRun();
            var activeChapter = FindFirstObjectByType<ChapterProgression>();
            if (activeChapter != null) { PlayerPrefs.SetInt("chapter_last_played", Mathf.Clamp(activeChapter.chapter, 1, 5)); PlayerPrefs.Save(); }
        }

        // Essential proposal 5: leaving the app mid-run opens the settings/pause panel instead of
        // letting the run continue or restart. On return the panel stays open (the player closes it
        // to resume) and touches that were held while switching back are ignored.
        private bool interruptedWhileAway;
        private void OnApplicationPause(bool paused) => HandleInterruption(paused);
        private void OnApplicationFocus(bool focused) => HandleInterruption(!focused);

        private void HandleInterruption(bool away)
        {
            if (Application.isEditor) return; // editor focus changes are not app switches
            if (away)
            {
                if (interruptedWhileAway) return;
                interruptedWhileAway = true;
                if (ShouldPauseForInterruption(TimeManager.isGameRunning, isGameOver,
                        settingsMenuUI != null && settingsMenuUI.activeInHierarchy,
                        pauseMenuUI != null && pauseMenuUI.activeInHierarchy))
                    PauseGame();
                return;
            }
            if (!interruptedWhileAway) return;
            interruptedWhileAway = false;
            if (isActiveAndEnabled) StartCoroutine(ReleaseInputAfterReturn());
        }

        public static bool ShouldPauseForInterruption(bool running, bool gameOver, bool settingsOpen, bool pauseOpen)
            => running && !gameOver && !settingsOpen && !pauseOpen;

        private IEnumerator ReleaseInputAfterReturn()
        {
            var group = GetComponent<CanvasGroup>();
            bool added = group == null;
            if (added) group = gameObject.AddComponent<CanvasGroup>();
            bool wasInteractable = group.interactable;
            group.interactable = false;
            yield return new WaitForSecondsRealtime(.25f);
            while (Input.GetMouseButton(0) || Input.touchCount > 0) yield return null;
            if (group != null)
            {
                group.interactable = wasInteractable;
                if (added) Destroy(group);
            }
            playerScript?.ResetStartGesture();
        }

        public bool IsStartAreaActive()
        {
            if (settingsMenuUI != null && settingsMenuUI.activeInHierarchy || pauseMenuUI != null && pauseMenuUI.activeInHierarchy) return false;
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
            GameplayAnalytics.EndRun(
                GameplayAnalytics.OutcomeAbandoned,
                playerScript);

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

        // Kept for old serialized UI events; sensitivity is no longer user-adjustable.
        public void ChangeSensitivity()
        {
            if (playerScript != null) playerScript.moveSensitivity = SettingsManager.DefaultMoveSensitivity;
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
            if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(SettingsManager.Instance.soundVolume);

            playerScript.moveSensitivity = SettingsManager.DefaultMoveSensitivity;
            SettingsManager.Instance.ApplyAudioSettings();
        }

        private IEnumerator GameOverSequence(float delay)
        {
            GameOver();
            // Let the shark's death/fall read before covering it with results.
            yield return new WaitForSecondsRealtime(.9f);
            if (gameOverUI != null) gameOverUI.SetActive(true);

            var presentation = gameOverUI != null ? gameOverUI.GetComponent<DefeatPresentation>() : null;
            if (presentation != null) yield return new WaitUntil(() => presentation.ContinueRequested);
            else yield return new WaitForSecondsRealtime(delay);

            if (gameOverUI != null) gameOverUI.SetActive(false);
            isGameOver = false;

            if (GameManager.S != null)
                GameManager.S.ResetAfterGameOver();
            else
                LoadingOverlay.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void GameOver()
        {
            GameAudioService.Play(GameSound.Defeat);
            GrantProgressReward();
            GameplayAnalytics.EndRun(
                GameplayAnalytics.OutcomeDeath,
                playerScript);
            scoreParent.SetActive(false);
            pauseButton.SetActive(false);
            SetAttackDebugVisible(false);
            isGameOver = true;
            gameOverUI.GetComponent<DefeatPresentation>()?.SetDamageCause(playerScript);
            gameOverUI.GetComponent<DefeatPresentation>()?.SetResult(activeRunSeconds, Mathf.Max(0, (MoneyScript.S != null ? MoneyScript.S.Coin : 0) - runStartingCoins), rewardRoundId, progressRewardCoins);
            gameOverUI.SetActive(false);
            TimeManager.timeFactor = 0;
            TimeManager.isGameRunning = false;
        }

        public void YouWin()
        {
            if (isGameOver || playerScript != null && playerScript.currentHealth <= 0f) return;
            GameAudioService.Play(GameSound.Victory);
            GrantProgressReward();
            FindFirstObjectByType<ChapterProgression>()?.CompleteChapter();

            GameplayAnalytics.EndRun(
                GameplayAnalytics.OutcomeWin,
                playerScript);
            scoreParent.SetActive(false);
            pauseButton.SetActive(false);
            SetAttackDebugVisible(false);

            isGameOver = true;
            TimeManager.isGameRunning = false;
            TimeManager.timeFactor = 0;

            if (playerScript != null) playerScript.PlayWinDance();

            var progression = FindFirstObjectByType<ChapterProgression>();
            if (progression == null || !progression.TryAdvanceAutomatically())
                StartCoroutine(ShowWinScreenAfterDelay());
        }

        private IEnumerator ShowWinScreenAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2);
            ShowWinScreen();
        }

        private void GrantProgressReward()
        {
            if (progressRewardGranted || string.IsNullOrEmpty(rewardRoundId) || MoneyScript.S == null) return;
            progressRewardGranted = true;
            if (!EnvironmentVariableTables.TryGetFloat("progressCoinPerCheckpoint_" + ChapterSceneKey.Resolve(gameObject.scene.name), out var rate)) return;
            float interval = EnvironmentVariableTables.TryGetFloat("progressRewardInterval", out var configuredInterval) ? configuredInterval : 15;
            int maximum = EnvironmentVariableTables.TryGetFloat("progressRewardMaximum", out var configuredMaximum) ? Mathf.Clamp(Mathf.FloorToInt(configuredMaximum), 0, 1000) : 20;
            int earned = RunProgressReward.Calculate(activeRunSeconds, interval, Mathf.Clamp(Mathf.FloorToInt(rate), 0, 10000), maximum);
            progressRewardCoins = Mathf.Min(earned, int.MaxValue - MoneyScript.S.Coin);
            if (progressRewardCoins > 0) MoneyScript.S.GetCoin(progressRewardCoins);
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
                if (scorePopAnimator != null && scorePopAnimator.runtimeAnimatorController != null && scorePopAnimator.isActiveAndEnabled)
                    scorePopAnimator.SetTrigger("ScoreInc");
                previousScore = currentScore;
            }
        }

        public void ResumeGame()
        {
            if (isGameOver) return;
            GameAudioService.Play(GameSound.Resume);
            pauseMenuUI.SetActive(false);
            settingsMenuUI.SetActive(false);
            TimeManager.timeFactor = 1;
            TimeManager.isGameRunning = true;
            playerScript?.SetSharkLocomotion(!playerScript.IsStationaryCombat);
            SetAttackDebugVisible(true);
            scoreParent.SetActive(!HasPlayerStatusHud);
            pauseButton.SetActive(true);
        }

        public void PauseGame()
        {
            var harbor = settingsMenuUI != null ? settingsMenuUI.GetComponent<HarborSettingsPanel>() : null;
            if (harbor != null && !isGameOver) { harbor.Open(true); return; }
            if (!isGameOver)
            {
                GameAudioService.Play(GameSound.Pause);
                scoreParent.SetActive(false);
                pauseButton.SetActive(false);
                settingsMenuUI.SetActive(false);
                pauseMenuUI.SetActive(true);
                TimeManager.timeFactor = 0;
                TimeManager.isGameRunning = false;
            }
        }

        public void UpdateAttackDebugText(float currentDamage)
        {
            PlayerStatusHud hud = ResolvePlayerStatusHud();
            if (hud != null && hud.IsConfigured)
            {
                hud.SetAttack(currentDamage);
                return;
            }

            attackDebugText ??= FindAttackDebugText();
            if (attackDebugText == null)
                return;

            attackDebugText.text = $"ATT : {Mathf.RoundToInt(currentDamage)}";
        }

        public void UpdatePlayerHealthStatus(float currentHealth, float maxHealth)
        {
            PlayerStatusHud hud = ResolvePlayerStatusHud();
            if (hud != null && hud.IsConfigured)
                hud.SetHealth(currentHealth, maxHealth);
        }

        public void ConfigurePlayerStatusHud(PlayerStatusHud hud, PlayerScript player = null)
        {
            playerStatusHud = hud;
            if (player != null)
            {
                playerScript = player;
                player.ConfigureCanvasScript(this);
            }

            SetAttackDebugVisible(false);
        }

        public bool HasPlayerStatusHud => ResolvePlayerStatusHud()?.IsConfigured == true;

        private void SetAttackDebugVisible(bool visible)
        {
            attackDebugText ??= FindAttackDebugText();
            PlayerStatusHud hud = ResolvePlayerStatusHud();

            if (hud != null && hud.IsConfigured)
            {
                hud.gameObject.SetActive(visible);
                playerScript?.SetPlayerChildCanvasVisible(false);
                if (attackDebugText != null)
                    attackDebugText.gameObject.SetActive(false);
                return;
            }

            if (attackDebugText != null)
                attackDebugText.gameObject.SetActive(visible);
        }

        private PlayerStatusHud ResolvePlayerStatusHud()
        {
            if (playerStatusHud == null)
                playerStatusHud = GetComponentInChildren<PlayerStatusHud>(true);

            return playerStatusHud;
        }

        private TextMeshProUGUI FindAttackDebugText()
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text != null && text.gameObject.name == "ATT")
                    return text;
            }

            return null;
        }

        public TextMeshProUGUI DamagePopupPrefab => damagePopupPrefab;

        public void LoadGame()
        {
            if (reloadInputSuppressed) return;
            reloadInputSuppressed = true;
            GameplayAnalytics.EndRun(
                GameplayAnalytics.OutcomeAbandoned,
                playerScript);

            Scene activeScene = SceneManager.GetActiveScene();
            TimeManager.timeFactor = 0;
            TimeManager.isGameRunning = false;
            isGameOver = false;
            LoadingOverlay.LoadScene(activeScene.name);
        }

        public void QuitGame()
        {
            GameplayAnalytics.EndRun(
                GameplayAnalytics.OutcomeAbandoned,
                playerScript);
            GameplayAnalytics.Flush();
            Application.Quit();
        }

        public void SettingsMenu()
        {
            var harbor = settingsMenuUI != null ? settingsMenuUI.GetComponent<HarborSettingsPanel>() : null;
            if (harbor != null) { harbor.Open(TimeManager.isGameRunning); return; }
            settingsMenuUI.SetActive(true);
            pauseMenuUI.SetActive(false);
        }
    }
}

