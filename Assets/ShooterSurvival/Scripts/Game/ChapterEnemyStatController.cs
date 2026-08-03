using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.ShooterSurvival
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ChapterEnemyStatController : MonoBehaviour
    {
        private const string NoryangjinScenePrefix = "Noryangjin_";

        [SerializeField, Min(1)] private int chapter = 1;
        [SerializeField] private Transform routeStart;

        private bool hasCapturedRouteStart;
        private Vector3 capturedRouteStart;
        private Vector3 capturedRouteDirection;

        public int Chapter => chapter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneBootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapActiveScene()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (!scene.IsValid() ||
                !scene.name.StartsWith(NoryangjinScenePrefix, StringComparison.Ordinal))
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<ChapterEnemyStatController>(true) != null)
                    return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                TimeManager timeManager = root.GetComponentInChildren<TimeManager>(true);
                if (timeManager == null)
                    continue;

                timeManager.gameObject.AddComponent<ChapterEnemyStatController>();
                return;
            }

            Debug.LogError(
                $"[ChapterEnemyStats] Scene '{scene.name}' has no TimeManager for runtime setup.");
        }

        private void Awake()
        {
            CaptureRouteStart();
        }

        private void Start()
        {
            ApplyStats();
        }

        public void Configure(int newChapter, Transform newRouteStart = null)
        {
            chapter = Mathf.Max(1, newChapter);
            routeStart = newRouteStart;
            hasCapturedRouteStart = false;
        }

        public int ApplyStats()
        {
            CaptureRouteStart();

            IReadOnlyDictionary<EnemyTier, MonsterGrowthRow> chapterRows =
                LoadChapterRows();
            List<EnemyScript_space> enemies =
                ChapterEnemyProgression.CollectEncounterEnemies(gameObject.scene);
            return ChapterEnemyProgression.ApplyStats(
                enemies,
                chapterRows,
                capturedRouteStart,
                capturedRouteDirection,
                ChapterEnemyProgression.CollectRouteTurns(gameObject.scene));
        }

        private IReadOnlyDictionary<EnemyTier, MonsterGrowthRow> LoadChapterRows()
        {
            var chapterRows = new Dictionary<EnemyTier, MonsterGrowthRow>();
            foreach (MonsterGrowthRow row in MonsterGrowthTables.GetAll())
            {
                if (row.chapter == chapter)
                    chapterRows.Add(row.tier, row);
            }

            if (chapterRows.Count != 3 ||
                !chapterRows.ContainsKey(EnemyTier.Normal) ||
                !chapterRows.ContainsKey(EnemyTier.Elite) ||
                !chapterRows.ContainsKey(EnemyTier.Boss))
            {
                throw new InvalidDataException(
                    $"Sheet '{MonsterGrowthTables.SheetName}' has no complete rows for chapter {chapter}.");
            }

            return chapterRows;
        }

        private void CaptureRouteStart()
        {
            if (hasCapturedRouteStart)
                return;

            Transform start = routeStart;
            if (start == null)
            {
                foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                {
                    PlayerScript player = root.GetComponentInChildren<PlayerScript>(true);
                    if (player == null)
                        continue;

                    start = player.transform;
                    break;
                }
            }

            capturedRouteStart = start != null ? start.position : Vector3.zero;
            capturedRouteDirection = start != null ? start.forward : Vector3.forward;
            hasCapturedRouteStart = true;
        }
    }
}
