using System;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
#endif
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival.Analytics
{
    public sealed class FirebaseAnalyticsRuntime : MonoBehaviour
    {
        public const string CollectionEnabledPlayerPrefsKey =
            "analytics_collection_enabled_v1";

        private const string RuntimeObjectName = "Gameplay Analytics";
        private const float CheckpointIntervalSeconds = 15f;
        // Product policy: collect on first install unless the player opts out.
        // Opt-in-first releases must change this default and wire the public
        // SetCollectionEnabled API to their consent UI.
        private const int CollectionEnabledByDefault = 1;

        private static FirebaseAnalyticsRuntime instance;
        private FirebaseAnalyticsSink analyticsSink;
        private float checkpointTimer;
        private bool collectionEnabled;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private bool firebaseInitialized;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static FirebaseAnalyticsRuntime EnsureInstance()
        {
            if (instance != null)
                return instance;

            instance = FindFirstObjectByType<FirebaseAnalyticsRuntime>();
            if (instance != null)
                return instance;

            var runtimeObject = new GameObject(RuntimeObjectName);
            return runtimeObject.AddComponent<FirebaseAnalyticsRuntime>();
        }

        public static bool IsCollectionEnabled =>
            instance != null
                ? instance.collectionEnabled
                : ReadStoredCollectionPreference();

        public static void SetCollectionEnabled(bool enabled)
        {
            EnsureInstance().ApplyCollectionPreference(enabled);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            collectionEnabled = ReadStoredCollectionPreference();
            analyticsSink = new FirebaseAnalyticsSink(
                collectionEnabled && SupportsFirebaseDelivery);
            GameplayAnalytics.Initialize(analyticsSink);
            if (analyticsSink.IsReady)
                GameplayAnalytics.RecoverUnfinishedRun();
            else
                GameplayAnalytics.DiscardLocalState();

            InitializeFirebase();
        }

        private void Update()
        {
            bool activelyPlaying =
                TimeManager.isGameRunning &&
                !CanvasScript.isGameOver;
            GameplayAnalytics.Tick(Time.unscaledDeltaTime, activelyPlaying);

            if (!GameplayAnalytics.IsRunActive)
            {
                checkpointTimer = 0f;
                return;
            }

            checkpointTimer += Time.unscaledDeltaTime;
            if (checkpointTimer < CheckpointIntervalSeconds)
                return;

            checkpointTimer = 0f;
            GameplayAnalytics.SaveCheckpoint();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                return;

            GameplayAnalytics.SaveCheckpoint();
            GameplayAnalytics.Flush();
        }

        private void OnApplicationQuit()
        {
            GameplayAnalytics.SaveCheckpoint();
            GameplayAnalytics.Flush();
        }

        private void InitializeFirebase()
        {
#if !((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            Debug.Log(
                "[Analytics] Firebase Analytics delivery is unavailable on this " +
                "Editor or desktop build. Validate events on an Android device.");
            return;
#else
            try
            {
                FirebaseApp.CheckAndFixDependenciesAsync()
                    .ContinueWithOnMainThread(HandleDependencyResult);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[Analytics] Firebase dependency check could not start: " +
                    exception.Message);
            }
#endif
        }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private void HandleDependencyResult(Task<DependencyStatus> task)
        {
            if (task.IsCanceled)
            {
                Debug.LogWarning("[Analytics] Firebase dependency check was canceled.");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogWarning(
                    $"[Analytics] Firebase dependency check failed: " +
                    task.Exception?.GetBaseException().Message);
                return;
            }

            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogWarning(
                    $"[Analytics] Firebase dependencies are unavailable: {task.Result}");
                return;
            }

            try
            {
                _ = FirebaseApp.DefaultInstance;
                firebaseInitialized = true;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(collectionEnabled);
                analyticsSink.MarkFirebaseReady();

                Debug.Log(
                    collectionEnabled
                        ? "[Analytics] Firebase Analytics is ready."
                        : "[Analytics] Firebase Analytics collection is disabled.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[Analytics] Firebase Analytics initialization failed: " +
                    exception.Message);
            }
        }
#endif

        private void ApplyCollectionPreference(bool enabled)
        {
            collectionEnabled = enabled;
            PlayerPrefs.SetInt(
                CollectionEnabledPlayerPrefsKey,
                enabled ? 1 : 0);
            PlayerPrefs.Save();

            analyticsSink?.SetCollectionEnabled(
                enabled && SupportsFirebaseDelivery);
            if (!enabled)
            {
                checkpointTimer = 0f;
                GameplayAnalytics.DiscardLocalState();
            }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (firebaseInitialized)
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled);
#endif
        }

        private static bool ReadStoredCollectionPreference()
        {
            return PlayerPrefs.GetInt(
                CollectionEnabledPlayerPrefsKey,
                CollectionEnabledByDefault) == 1;
        }

        private static bool SupportsFirebaseDelivery
        {
            get
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
