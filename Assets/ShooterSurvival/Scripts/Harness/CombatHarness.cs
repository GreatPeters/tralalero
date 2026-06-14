using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndianOceanAssets.ShooterSurvival
{
    public class CombatHarness : MonoBehaviour
    {
        private const string HarnessRootName = "Combat Harness";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureHarnessInstance();
        }

        [Header("State")]
        [SerializeField] private bool hotkeysEnabled = true;
        [SerializeField] private bool overlayVisible = true;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode toggleOverlayKey = KeyCode.BackQuote;
        [SerializeField] private KeyCode startRunKey = KeyCode.F1;
        [SerializeField] private KeyCode skipWaveKey = KeyCode.F2;
        [SerializeField] private KeyCode spawnWalkerKey = KeyCode.F3;
        [SerializeField] private KeyCode spawnRusherKey = KeyCode.F4;
        [SerializeField] private KeyCode spawnTankKey = KeyCode.F5;
        [SerializeField] private KeyCode healPlayerKey = KeyCode.F6;
        [SerializeField] private KeyCode damagePlayerKey = KeyCode.F7;
        [SerializeField] private KeyCode logSnapshotKey = KeyCode.F8;

        [Header("Harness Values")]
        [SerializeField] private float playerHealthStep = 25f;
        [SerializeField] private float spawnForwardOffset = 14f;
        [SerializeField] private float spawnLateralJitter = 4f;
        [SerializeField] private bool verboseStartupLog = true;

        private PlayerScript player;
        private CanvasScript canvasScript;
        private GameManager gameManager;
        private WaveManager waveManager;
        private EnemyPooler enemyPooler;
        private EnemySpawnerScript enemySpawner;

        private void Awake()
        {
            RefreshReferences();
            if (verboseStartupLog)
                LogHarness($"Initialized. hotkeys={hotkeysEnabled}, overlay={overlayVisible}, scene={gameObject.scene.name}");
        }

        private void Update()
        {
            if (!hotkeysEnabled)
                return;

            if (Input.GetKeyDown(toggleOverlayKey))
                overlayVisible = !overlayVisible;

            if (Input.GetKeyDown(startRunKey))
                StartRun();

            if (Input.GetKeyDown(skipWaveKey))
                SkipWave();

            if (Input.GetKeyDown(spawnWalkerKey))
                SpawnEnemy(EnemyType.Walker);

            if (Input.GetKeyDown(spawnRusherKey))
                SpawnEnemy(EnemyType.Rusher);

            if (Input.GetKeyDown(spawnTankKey))
                SpawnEnemy(EnemyType.Tank);

            if (Input.GetKeyDown(healPlayerKey))
                AdjustPlayerHealth(playerHealthStep);

            if (Input.GetKeyDown(damagePlayerKey))
                AdjustPlayerHealth(-playerHealthStep);

            if (Input.GetKeyDown(logSnapshotKey))
                LogSnapshot();
        }

        private void OnGUI()
        {
            if (!overlayVisible)
                return;

            GUI.Box(new Rect(12f, 12f, 360f, 180f),
                "Combat Harness\n" +
                "F1 Start Run\n" +
                "F2 Skip Wave\n" +
                "F3 Spawn Walker\n" +
                "F4 Spawn Rusher\n" +
                "F5 Spawn Tank\n" +
                "F6 Heal Player\n" +
                "F7 Damage Player\n" +
                "F8 Log Snapshot\n" +
                "` Toggle Overlay");
        }

        [ContextMenu("Harness/Start Run")]
        public void StartRun()
        {
            RefreshReferences();

            if (gameManager != null)
            {
                gameManager.OnTapToPlay();
                LogHarness("Run started through GameManager.");
                return;
            }

            if (canvasScript != null)
            {
                canvasScript.PlayerPressedStartButton();
                LogHarness("Run started through Canvas.");
                return;
            }

            TimeManager.timeFactor = 1f;
            TimeManager.isGameRunning = true;
            LogHarness("Run started through TimeManager fallback.");
        }

        [ContextMenu("Harness/Skip Wave")]
        public void SkipWave()
        {
            RefreshReferences();

            if (waveManager == null)
            {
                LogHarness("WaveManager was not found.");
                return;
            }

            bool advanced = waveManager.ForceAdvanceToNextWaveForHarness();
            LogHarness(advanced
                ? $"Forced wave advance. {waveManager.BuildHarnessSnapshot()}"
                : $"No wave left to advance. {waveManager.BuildHarnessSnapshot()}");
        }

        [ContextMenu("Harness/Log Snapshot")]
        public void LogSnapshot()
        {
            RefreshReferences();

            string playerState = player == null
                ? "player=missing"
                : $"playerHp={player.currentHealth:0.##}/{player.MaxHealth:0.##}, score={player.playerScore}, canShoot={player.canShoot}";
            string waveState = waveManager == null ? "wave=missing" : waveManager.BuildHarnessSnapshot();

            LogHarness($"{playerState}, {waveState}");
        }

        private void AdjustPlayerHealth(float delta)
        {
            RefreshReferences();

            if (player == null)
            {
                LogHarness("PlayerScript was not found.");
                return;
            }

            player.ApplyHarnessHealthDelta(delta);
            LogHarness($"Player HP adjusted by {delta:0.##}. hp={player.currentHealth:0.##}/{player.MaxHealth:0.##}");
        }

        private void SpawnEnemy(EnemyType enemyType)
        {
            RefreshReferences();

            if (enemyPooler == null)
            {
                LogHarness("EnemyPooler was not found.");
                return;
            }

            Transform parent = enemySpawner != null ? enemySpawner.transform : enemyPooler.transform;
            GameObject enemy = enemyPooler.GetObjectFromPool_Enemy(enemyType, parent);
            if (enemy == null)
            {
                LogHarness($"Pool did not return an enemy for {enemyType}.");
                return;
            }

            Vector3 spawnOrigin = player != null ? player.transform.position : transform.position;
            Vector3 spawnPosition = spawnOrigin + Vector3.forward * spawnForwardOffset;
            spawnPosition.x += Random.Range(-spawnLateralJitter, spawnLateralJitter);
            spawnPosition.y = 0.05f;
            enemy.transform.position = spawnPosition;

            LogHarness($"Spawned {enemyType} at {spawnPosition}.");
        }

        private void RefreshReferences()
        {
            player ??= FindFirstObjectByType<PlayerScript>();
            canvasScript ??= FindFirstObjectByType<CanvasScript>();
            gameManager ??= GameManager.S != null ? GameManager.S : FindFirstObjectByType<GameManager>();
            waveManager ??= FindFirstObjectByType<WaveManager>();
            enemyPooler ??= EnemyPooler.Instance != null ? EnemyPooler.Instance : FindFirstObjectByType<EnemyPooler>();
            enemySpawner ??= FindFirstObjectByType<EnemySpawnerScript>();
        }

        private static void LogHarness(string message)
        {
            Debug.Log($"[CombatHarness] {message}");
        }

        private static CombatHarness EnsureHarnessInstance()
        {
            var existing = FindFirstObjectByType<CombatHarness>();
            if (existing != null)
                return existing;

            var harnessRoot = GameObject.Find(HarnessRootName) ?? new GameObject(HarnessRootName);
            var harness = harnessRoot.GetComponent<CombatHarness>();
            if (harness == null)
                harness = harnessRoot.AddComponent<CombatHarness>();

            if (Application.isPlaying)
                DontDestroyOnLoad(harnessRoot);

            LogHarness("Bootstrap created runtime instance.");
            return harness;
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        private static void EnsureOnEnterPlayMode(EnterPlayModeOptions _)
        {
            EnsureHarnessInstance();
        }

        [MenuItem("Tools/Combat Harness/Select Runtime Harness")]
        private static void SelectHarness()
        {
            var harness = EnsureHarnessInstance();
            Selection.activeGameObject = harness.gameObject;
        }

        [MenuItem("Tools/Combat Harness/Start Run")]
        private static void MenuStartRun()
        {
            EnsureHarnessInstance().StartRun();
        }

        [MenuItem("Tools/Combat Harness/Skip Wave")]
        private static void MenuSkipWave()
        {
            EnsureHarnessInstance().SkipWave();
        }

        [MenuItem("Tools/Combat Harness/Log Snapshot")]
        private static void MenuLogSnapshot()
        {
            EnsureHarnessInstance().LogSnapshot();
        }
#endif
    }
}
