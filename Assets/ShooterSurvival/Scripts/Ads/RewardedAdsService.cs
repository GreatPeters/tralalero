using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival.Ads
{
    public sealed class RewardedAdsService : MonoBehaviour
    {
        public static RewardedAdsService Instance { get; private set; }
        public event Action Changed;
        public RewardedAdsSettings Settings { get; private set; }
        public bool Ready => initialized && ConsentInformation.CanRequestAds() && ad != null && ad.CanShowAd() && !Showing;
        public bool Showing { get; private set; }
        public bool Loading { get; private set; }
        public string Status { get; private set; } = "광고 준비 중";
        public bool PrivacyOptionsRequired => ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        public bool BlockingConsentForm => waitingForConsentForm;
        private RewardedAd ad, activeAd;
        private bool initialized, initializing, disposed, waitingForConsentForm, waitingForIdle;
        private int loadVersion, initializationVersion;
        private float retryAfter, loadedAt, nextMaintenanceAt, loadStartedAt, initializationStartedAt;
        private static bool GameplayOrStoryActive => OpeningStoryUI.IsBlockingGameplay || TimeManager.isGameRunning || FindFirstObjectByType<ChapterProgression>()?.IsAdvancing == true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var settings = Resources.Load<RewardedAdsSettings>(RewardedAdsSettings.ResourcePath);
            if (settings == null) return;
            var root = new GameObject("Rewarded Ads");
            DontDestroyOnLoad(root);
            var service = root.AddComponent<RewardedAdsService>();
            service.Settings = settings;
        }
        private void Awake()
        {
            Instance = this;
            // Consent callbacks already use this queue before MobileAds.Initialize.
            MobileAdsEventExecutor.Initialize();
        }
        private IEnumerator Start()
        {
            // Keep consent/SDK initialization out of the opening's first frame.
            yield return null;
            while (GameplayOrStoryActive) yield return null;
            Prepare();
        }
        private void Update()
        {
            if (Time.unscaledTime < nextMaintenanceAt) return;
            nextMaintenanceAt = Time.unscaledTime + 2;
            if (initializing && !waitingForConsentForm && !waitingForIdle && Time.unscaledTime - initializationStartedAt > 60)
            {
                ++initializationVersion; initializing = false; retryAfter = Time.unscaledTime + 30;
                SetStatus("광고 연결이 지연됩니다. 잠시 후 다시 시도해 주세요");
            }
            if (Loading && Time.unscaledTime - loadStartedAt > 45)
            {
                ++loadVersion; Loading = false; retryAfter = Time.unscaledTime + 30;
                SetStatus("광고 연결이 지연됩니다. 잠시 후 다시 시도해 주세요");
            }
            if (!GameplayOrStoryActive) Prepare();
        }
        public void Prepare()
        {
            if (disposed || Settings == null || Showing || Loading || initializing || GameplayOrStoryActive || Time.unscaledTime < retryAfter) return;
            if (Ready && Time.unscaledTime - loadedAt < 3500) return;
            if (string.IsNullOrWhiteSpace(Settings.UnitId)) { retryAfter = Time.unscaledTime + 60; SetStatus("광고를 사용할 수 없습니다"); return; }
            if (!initialized)
            {
                initializing = true; initializationStartedAt = Time.unscaledTime;
                int version = ++initializationVersion; SetStatus("광고 준비 중");
                ConsentInformation.Update(new ConsentRequestParameters { TagForUnderAgeOfConsent = Settings.underAgeOfConsent }, error => Main(() =>
                {
                    if (version != initializationVersion) return;
                    if (error != null || ConsentInformation.ConsentStatus != ConsentStatus.Required) { FinishConsent(version); return; }
                    ConsentForm.Load((form, formError) => Main(() =>
                    {
                        if (version != initializationVersion) return;
                        if (formError != null || form == null) { FinishConsent(version); return; }
                        StartCoroutine(ShowConsentWhenIdle(form, version));
                    }));
                }));
                return;
            }
            if (!ConsentInformation.CanRequestAds()) { retryAfter = Time.unscaledTime + 60; SetStatus("광고 동의 설정을 확인해 주세요"); return; }
            Load();
        }
        private IEnumerator ShowConsentWhenIdle(ConsentForm form, int version)
        {
            waitingForIdle = true;
            while (version == initializationVersion && (GameplayOrStoryActive || Showing)) yield return null;
            if (version != initializationVersion || disposed) yield break;
            waitingForIdle = false;
            if (ConsentInformation.ConsentStatus != ConsentStatus.Required) { FinishConsent(version); yield break; }
            // Loading may finish during a round. Only present on an idle screen,
            // and prevent a new run from starting behind the native form.
            waitingForConsentForm = true;
            form.Show(_ => Main(() => FinishConsent(version)));
        }
        private void FinishConsent(int version)
        {
            if (version != initializationVersion) return;
            waitingForConsentForm = false; waitingForIdle = false; initializationStartedAt = Time.unscaledTime;
            if (!ConsentInformation.CanRequestAds())
            {
                initializing = false; retryAfter = Time.unscaledTime + 60;
                SetStatus("지금은 광고를 사용할 수 없습니다"); return;
            }
            MobileAds.Initialize(status => Main(() =>
            {
                if (version != initializationVersion) return;
                initializing = false;
                if (status == null) { retryAfter = Time.unscaledTime + 60; SetStatus("광고 준비에 실패했습니다"); return; }
                initialized = true; Load();
            }));
        }
        private void Load()
        {
            ad?.Destroy(); ad = null; Loading = true; loadStartedAt = Time.unscaledTime;
            int version = ++loadVersion; SetStatus("광고 준비 중");
            RewardedAd.Load(Settings.UnitId, new AdRequest(), (loaded, error) =>
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this == null || disposed || version != loadVersion) { loaded?.Destroy(); return; }
                    Loading = false;
                    if (error != null || loaded == null)
                    {
                        loaded?.Destroy(); retryAfter = Time.unscaledTime + 30;
                        SetStatus("광고가 없습니다. 잠시 후 다시 시도해 주세요"); return;
                    }
                    ad = loaded; loadedAt = Time.unscaledTime; SetStatus("광고 보고 보상 받기");
                }));
        }
        public bool Show(Action earned, Action closed)
        {
            if (!Ready || GameplayOrStoryActive) { Prepare(); return false; }
            var showingAd = ad; activeAd = showingAd; ad = null; Showing = true;
            bool rewarded = false, finished = false, failedToShow = false;
            void Finish()
            {
                if (finished) return;
                finished = true; Showing = false; showingAd.Destroy(); activeAd = null;
                closed?.Invoke(); SetStatus("광고 준비 중"); Prepare();
            }
            showingAd.OnAdFullScreenContentClosed += () => Main(Finish);
            showingAd.OnAdFullScreenContentFailed += _ => Main(() => { failedToShow = true; Finish(); });
            SetStatus("광고 재생 중");
            try
            {
                showingAd.Show(_ => Main(() => { if (!rewarded && !failedToShow) { rewarded = true; earned?.Invoke(); } }));
            }
            catch (Exception error)
            {
                Debug.LogWarning("Rewarded ad could not show: " + error.Message);
                failedToShow = true;
                Finish();
            }
            return true;
        }
        public void ShowPrivacyOptions()
        {
            if (Showing || initializing || GameplayOrStoryActive) return;
            initializing = true; waitingForConsentForm = true; initializationStartedAt = Time.unscaledTime;
            int version = ++initializationVersion;
            loadVersion++; Loading = false; ad?.Destroy(); ad = null;
            SetStatus("광고 동의 설정");
            ConsentForm.ShowPrivacyOptionsForm(_ => Main(() =>
            {
                if (version != initializationVersion) return;
                initializing = false; waitingForConsentForm = false; ad?.Destroy(); ad = null; retryAfter = 0; Prepare();
            }));
        }
        private void Main(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(() => { if (this != null && !disposed) action(); });
        private void SetStatus(string text) { Status = text; Changed?.Invoke(); }
        private void OnDestroy()
        {
            disposed = true; loadVersion++; initializationVersion++; ad?.Destroy(); activeAd?.Destroy(); ad = activeAd = null;
            if (Instance == this) Instance = null;
        }
    }
}
