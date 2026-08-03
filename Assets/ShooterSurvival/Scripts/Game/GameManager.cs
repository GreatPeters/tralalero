using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class EnemyStat
    {
        public float damage;
        public float health;
        public EnemyStat(float damage, float health)
        {
            this.damage = damage;
            this.health = health;
        }
    }

    public class GameManager : MonoBehaviour
    {
        private CanvasScript canvas;
        private WeaponManager weaponManager; // ?�으�?

        public GameObject extraHelp_TungTungTung;
        public GameObject extraHelp_BoomBarDino;
        public PlayerScript playerScript;

        public static GameManager S;
        public int currentChapter;
        public int currentStage;

        public int maxStage = 10;
        public int maxChapter = 10;

        private readonly List<GameObject> destroyTargetList = new();

        public List<List<EnemyStat>> normalMonster;
        public List<List<EnemyStat>> eliteMonster;
        public List<List<EnemyStat>> bossMonster;

        private Dictionary<int, Dictionary<EnemyTier, MonsterGrowthRow>> chapterGrowthRows;

        [SerializeField] private StageObstacleManager StageObstacleManager;

        [Header("Scene References")]
        public Transform EnemyParent;

        [Header("Player Reset")]
        [SerializeField] private Transform playerSpawnPoint;     // ???�테?��? ?�작 ?�치

        [Header("UI")]

        public EnemyScript_space[] enemyScript_spaces;

        void Awake()
        {
            S = this;
            canvas = FindFirstObjectByType<CanvasScript>();

            if (playerScript != null)
                weaponManager = playerScript.GetComponent<WeaponManager>();
        }

        void Start()
        {
            currentChapter = 1;
            currentStage = 1;

            TimeManager.isGameRunning = false;
            TimeManager.timeFactor = 0f;

            // 처음???��??�면 ?�우�?멈춰?�는 �?추천
            PrepareStageAndShowTapUI();

            SettingMonsterStats();
            ApplyStatsToEnemyReferences();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                //DestroyAllRegisteredTarget();
                OnStageClear();
            }
        }

        public void RegisterDestroyTarget(GameObject go)
        {
            if (!go) return;
            destroyTargetList.Add(go);
        }

        public void DestroyAllRegisteredTarget()
        {
            // 1) ?�록???�시 ?�브?�트 ?��? ?�괴
            for (int i = destroyTargetList.Count - 1; i >= 0; i--)
            {
                var go = destroyTargetList[i];
                if (go) Destroy(go);
            }

            // 2) 리스??비우�?(remove 반복 ?�??Clear)
            destroyTargetList.Clear();
        }


        // =========================
        // ???�테?��? ?�리?????�음 ?�테?��?�??�동 (?��??�태)
        // =========================
        public void OnStageClear()
        {
            DestroyAllRegisteredTarget();

            // 1) ?�단 멈추�??�동/발사 ?�함)
            SetGameRunning(false);

            // 2) ?�음 ?�테?��? ?�덱??계산
            GoNextStageIndex();

            // 3) ?�음 ?�테?��? 준�?리셋+로드)
            PrepareStageObjectsForNextRun();

            // 4) "Tap to Play" ?�시 ?�우�?(CanvasScript가 갖고?�음)
            if (canvas != null) canvas.buttons.SetActive(true);

            playerScript.ResetStatBonus();
            BulletScript.ResetStatBonus();
            weaponManager.currentWeapon.GetComponentInChildren<WeaponScript>().ResetStatBonus();

        }

        public void ResetAfterGameOver()
        {
            DestroyAllRegisteredTarget();
            SetGameRunning(false);

            PrepareStageObjectsForNextRun();
            ShowTapUI(true);

            playerScript.ResetStatBonus();
            BulletScript.ResetStatBonus();
            weaponManager.currentWeapon.GetComponentInChildren<WeaponScript>().ResetStatBonus();
        }
        private void ApplyUpgradeExtraHelps()
        {
            if (playerScript == null || UpgradeStatManager.S == null) return;

            int tungCount = Mathf.Max(0, Mathf.RoundToInt(UpgradeStatManager.S.GetStat(UpgradeStatManager.UpgradeType.TUNGTUNGTUNG)));
            int boomCount = Mathf.Max(0, Mathf.RoundToInt(UpgradeStatManager.S.GetStat(UpgradeStatManager.UpgradeType.BOOMBAR)));

            for (int i = 0; i < tungCount; i++)
                SpawnExtraHelp(extraHelp_TungTungTung, HelpType.Tungtungtung);

            for (int i = 0; i < boomCount; i++)
                SpawnExtraHelp(extraHelp_BoomBarDino, HelpType.Boombardino);
        }

        private void SpawnExtraHelp(GameObject prefab, HelpType helpType)
        {
            if (prefab == null || playerScript == null) return;

            Vector3 spawnOffset = new Vector3(1.5f, 0f, -0.75f);
            Vector3 spawnPosition = playerScript.transform.position + spawnOffset;

            GameObject go = Instantiate(prefab, spawnPosition, Quaternion.identity);
            var eh = go.GetComponent<ExtraHelpBuffScript>();
            if (eh != null)
            {
                playerScript.extraHelpCount++;
                eh.spawnIndex = playerScript.extraHelpCount - 1;
                eh.helpType = helpType;
            }

            var ws = go.GetComponentInChildren<WeaponScript>();
            if (ws != null && playerScript.extraHelpWeaponScript != null)
                playerScript.extraHelpWeaponScript.Add(ws);
        }
        // =========================
        // ??"TAP TO PLAY" 버튼?�서 ?�출
        // =========================
        public void OnTapToPlay()
        {
            // ??UI ?�리�?게임 ?�작
            ShowTapUI(false);
            NoryangjinTurnSpot.ResetAllForNewRun();
            EnemyMovementController.ResetAllForNewRun();
            EnemyMovementActivationTrigger.ResetAllForNewRun();
            SetGameRunning(true);
            ApplyUpgradeExtraHelps();
        }

        // =========================
        // ???�음 ?�테?��? 준�?초기??+ 로드)
        // =========================
        private void PrepareStageAndShowTapUI()
        {
            SetGameRunning(false);
            ShowTapUI(true);

            PrepareStageObjectsForNextRun();
        }

        private void PrepareStageObjectsForNextRun()
        {
            Debug.Log("..?�잉!!?");
            //보너?�로 ?�성??Wall 모두 ?�거
            ClearRuntimeBonusWalls();

            // // (A) ?�레?�어 ?�치 리셋
            // ResetPlayerToSpawn();

            // (B) ?�애�??� 반환 + ???�테?��? 로드
            if (StageObstacleManager != null)
            {
                StageObstacleManager.SetStage(currentChapter - 1, currentStage - 1);
                StageObstacleManager.LoadStageObstacles(); // ?��??�서 ClearObstacles() ?�출??
            }

            RefreshEnemyReferences();

            // (C) ?�들 리셋 (OnEnable 리셋 구조 ?�는 �??�일 ?�정??
            ResetAllEnemiesByReEnable();

            // (D) ???�탯 ?�시 주입
            ApplyStatsToEnemyReferences();

            // (E) ExtraHelp ?�아?�으�??�리
            ClearAllExtraHelps();

            // (F) Wall ?�시 ?�덤 ?�팅(?�생??X, Init???�답)
            if (WallManager.S != null)
                WallManager.S.InIt();

            // CanvasScript.isGameOver = false;
            // TimeManager.timeFactor = 1;
            // TimeManager.isGameRunning = true;

            //(A) ?�레?�어 ?�치 리셋
            ResetPlayerToSpawn();
        }

        private void ResetPlayerToSpawn()
        {

            if (playerScript == null) return;
            if (playerSpawnPoint == null) return;

            var t = playerScript.transform;
            t.position = playerSpawnPoint.position;
            t.rotation = playerSpawnPoint.rotation;

            // Rigidbody ?�으�??�도 0
            var rb = playerScript.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (playerScript != null)
                playerScript.ResetState();
        }

        private void ResetAllEnemiesByReEnable()
        {
            if (enemyScript_spaces == null) return;

            for (int i = 0; i < enemyScript_spaces.Length; i++)
            {
                var e = enemyScript_spaces[i];
                if (e == null || !ShouldResetEnemyByReEnable(e.gameObject)) continue;

                e.gameObject.SetActive(false);
                e.gameObject.SetActive(true);
            }
        }

        public static bool ShouldResetEnemyByReEnable(GameObject enemyObject)
        {
            if (enemyObject == null)
                return false;

            bool isInactiveQueuedPoolObject =
                !enemyObject.activeSelf &&
                enemyObject.GetComponentInParent<EnemyPooler>(includeInactive: true) != null;
            return !isInactiveQueuedPoolObject;
        }

        private void ClearAllExtraHelps()
        {
            // ExtraHelpTag�??�일?�어 ?�으�??�게 ?�일 깔끔
            // (EnemyScript_space?�서??ExtraHelpTag�?충돌 체크 �?
            var helps = GameObject.FindGameObjectsWithTag("ExtraHelpTag");
            for (int i = 0; i < helps.Length; i++)
                Destroy(helps[i]);
        }

        private void GoNextStageIndex()
        {
            currentStage++;

            if (currentStage > Mathf.Max(1, maxStage))
            {
                currentStage = 1;
                currentChapter++;

                if (currentChapter > Mathf.Max(1, maxChapter))
                    currentChapter = 1; // 마지막 챕터 다음에는 캠페인 처음으로 순환한다.
            }
        }

        private void ShowTapUI(bool show)
        {
            if (canvas != null && canvas.buttons != null)
                canvas.buttons.SetActive(show);
        }

        private void SetGameRunning(bool running)
        {
            TimeManager.isGameRunning = running;
            TimeManager.timeFactor = running ? 1f : 0f;

            // // ???�력/발사 ?�실??멈추�??�으�?컴포?�트??꺼버리기(강력)
            // if (playerScript != null) playerScript.enabled = running;
            // if (weaponManager != null) weaponManager.enabled = running;
        }

        // =========================
        // 기존 코드 (?��?)
        // =========================

        public void SettingMonsterStats()
        {
            chapterGrowthRows = null;
            if (TryBuildMonsterStatsFromGrowthExcel())
                return;

            if (TryBuildMonsterStatsFromLegacyExcelEndpoints())
                return;

            BuildFormulaFallbackMonsterStats();
            Debug.LogWarning("[GameManager] Monster growth data was unavailable. Formula stats were used.");
        }

        private bool TryBuildMonsterStatsFromGrowthExcel()
        {
            if (!MonsterGrowthTables.TryGetAll(out List<MonsterGrowthRow> rows))
            {
                Debug.Log(
                    $"[GameManager] Optional '{MonsterGrowthTables.SheetName}' sheet is absent. " +
                    "Using legacy monster endpoints.");
                return false;
            }

            if (!TryCollectGrowthRowsByChapter(
                    rows,
                    out Dictionary<int, Dictionary<EnemyTier, MonsterGrowthRow>> growthByChapter))
                return false;

            chapterGrowthRows = growthByChapter;
            BuildInterpolatedMonsterStats(growthByChapter);
            Debug.Log("[GameManager] Monster stats loaded from chapter enemy growth rows.");
            return true;
        }

        private bool TryBuildMonsterStatsFromLegacyExcelEndpoints()
        {
            List<MonsterRow> rows;
            try
            {
                rows = MonsterTables.GetAll();
            }
            catch (GameDataIntegrityException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] Monster table load failed. fallback to formula. {e.Message}");
                return false;
            }

            InitMonsterListsWithNull();
            foreach (MonsterRow row in rows)
            {
                if (row.chapter <= 0 || row.chapter > Mathf.Max(1, maxChapter) ||
                    row.stage <= 0 || row.stage > Mathf.Max(1, maxStage) ||
                    !System.Enum.IsDefined(typeof(EnemyTier), row.tier))
                {
                    continue;
                }

                GetTierList(row.tier)[row.chapter][row.stage] =
                    new EnemyStat(row.damage, row.health);
            }

            for (int chapter = 1; chapter <= Mathf.Max(1, maxChapter); chapter++)
            {
                for (int stage = 1; stage <= Mathf.Max(1, maxStage); stage++)
                {
                    foreach (EnemyTier tier in System.Enum.GetValues(typeof(EnemyTier)))
                    {
                        if (GetTierList(tier)[chapter][stage] == null)
                            return false;
                    }
                }
            }

            Debug.LogWarning(
                "[GameManager] The legacy '몬스터' sheet is being used directly because " +
                "the optional chapter growth sheet is absent.");
            return true;
        }

        public static bool TryCollectGrowthRowsByChapter(
            IEnumerable<MonsterGrowthRow> rows,
            out Dictionary<int, Dictionary<EnemyTier, MonsterGrowthRow>> growthByChapter)
        {
            growthByChapter = new Dictionary<int, Dictionary<EnemyTier, MonsterGrowthRow>>();
            if (rows != null)
            {
                foreach (MonsterGrowthRow row in rows)
                {
                    if (row.chapter <= 0 || !System.Enum.IsDefined(typeof(EnemyTier), row.tier))
                    {
                        growthByChapter.Clear();
                        return false;
                    }

                    if (!growthByChapter.TryGetValue(
                            row.chapter,
                            out Dictionary<EnemyTier, MonsterGrowthRow> chapterRows))
                    {
                        chapterRows = new Dictionary<EnemyTier, MonsterGrowthRow>();
                        growthByChapter.Add(row.chapter, chapterRows);
                    }

                    if (chapterRows.ContainsKey(row.tier))
                    {
                        growthByChapter.Clear();
                        return false;
                    }

                    chapterRows.Add(row.tier, row);
                }
            }

            if (growthByChapter.Count == 0)
                return false;

            foreach (Dictionary<EnemyTier, MonsterGrowthRow> chapterRows in growthByChapter.Values)
            {
                if (chapterRows.Count != 3 ||
                    !chapterRows.ContainsKey(EnemyTier.Normal) ||
                    !chapterRows.ContainsKey(EnemyTier.Elite) ||
                    !chapterRows.ContainsKey(EnemyTier.Boss))
                {
                    growthByChapter.Clear();
                    return false;
                }
            }

            return true;
        }

        private void BuildFormulaFallbackMonsterStats()
        {
            InitMonsterListsWithNull();
            for (int chapter = 1; chapter <= Mathf.Max(1, maxChapter); chapter++)
            {
                for (int stage = 1; stage <= Mathf.Max(1, maxStage); stage++)
                {
                    foreach (EnemyTier tier in System.Enum.GetValues(typeof(EnemyTier)))
                        GetTierList(tier)[chapter][stage] = BuildFallbackStat(tier, chapter, stage);
                }
            }
        }

        private void BuildInterpolatedMonsterStats(
            IReadOnlyDictionary<int, Dictionary<EnemyTier, MonsterGrowthRow>> growthByChapter)
        {
            InitMonsterListsWithNull();
            for (int chapter = 1; chapter <= Mathf.Max(1, maxChapter); chapter++)
            {
                if (!growthByChapter.TryGetValue(
                        chapter,
                        out Dictionary<EnemyTier, MonsterGrowthRow> chapterRows))
                {
                    continue;
                }

                for (int stage = 1; stage <= Mathf.Max(1, maxStage); stage++)
                {
                    float progress = MonsterStatInterpolator.CalculateProgress(
                        stage - 1,
                        Mathf.Max(1, maxStage));
                    FillInterpolatedTier(EnemyTier.Normal, chapter, stage, progress, chapterRows);
                    FillInterpolatedTier(EnemyTier.Elite, chapter, stage, progress, chapterRows);
                    FillInterpolatedTier(EnemyTier.Boss, chapter, stage, progress, chapterRows);
                }
            }
        }

        private void FillInterpolatedTier(
            EnemyTier tier,
            int chapter,
            int stage,
            float progress,
            IReadOnlyDictionary<EnemyTier, MonsterGrowthRow> chapterRows)
        {
            MonsterStatInterpolator.Evaluate(
                chapterRows[tier],
                progress,
                out float damage,
                out float health);
            GetTierList(tier)[chapter][stage] = new EnemyStat(damage, health);
        }

        private void InitMonsterListsWithNull()
        {
            int chapterCount = Mathf.Max(1, maxChapter);
            int stageCount = Mathf.Max(1, maxStage);
            normalMonster = new List<List<EnemyStat>>(chapterCount + 1);
            eliteMonster = new List<List<EnemyStat>>(chapterCount + 1);
            bossMonster = new List<List<EnemyStat>>(chapterCount + 1);

            for (int chapter = 0; chapter <= chapterCount; chapter++)
            {
                var normalList = new List<EnemyStat>(stageCount + 1);
                var eliteList = new List<EnemyStat>(stageCount + 1);
                var bossList = new List<EnemyStat>(stageCount + 1);

                for (int stage = 0; stage <= stageCount; stage++)
                {
                    normalList.Add(null);
                    eliteList.Add(null);
                    bossList.Add(null);
                }

                normalMonster.Add(normalList);
                eliteMonster.Add(eliteList);
                bossMonster.Add(bossList);
            }
        }

        private List<List<EnemyStat>> GetTierList(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Normal: return normalMonster;
                case EnemyTier.Elite: return eliteMonster;
                case EnemyTier.Boss: return bossMonster;
                default: return normalMonster;
            }
        }

        private EnemyStat BuildFallbackStat(EnemyTier tier, int chapter, int stage)
        {
            return tier switch
            {
                EnemyTier.Normal => new EnemyStat(30 * chapter + stage * 3, 50 * chapter + stage * 5),
                EnemyTier.Elite => new EnemyStat(50 * chapter + stage * 5, 100 * chapter + stage * 10),
                EnemyTier.Boss => new EnemyStat(150 * chapter + stage * 15, 300 * chapter + stage * 30),
                _ => new EnemyStat(30 * chapter + stage * 3, 50 * chapter + stage * 5)
            };
        }

        private EnemyStat GetLocalStat(EnemyTier tier)
        {
            int chapter = Mathf.Clamp(currentChapter, 1, Mathf.Max(1, maxChapter));
            int stage = Mathf.Clamp(currentStage, 1, Mathf.Max(1, maxStage));
            EnemyStat stat = tier switch
            {
                EnemyTier.Normal => normalMonster[chapter][stage],
                EnemyTier.Elite => eliteMonster[chapter][stage],
                EnemyTier.Boss => bossMonster[chapter][stage],
                _ => normalMonster[chapter][stage]
            };

            return stat ?? BuildFallbackStat(tier, chapter, stage);
        }

        public void ApplyStatsToAllEnemies()
        {
            RefreshEnemyReferences();
            ApplyStatsToEnemyReferences();
        }

        private void ApplyStatsToEnemyReferences()
        {
            if (enemyScript_spaces == null)
                return;

            if (chapterGrowthRows != null)
            {
                ApplyChapterGrowthStatsToEnemyReferences();
                return;
            }

            if (normalMonster == null)
                return;

            for (int i = 0; i < enemyScript_spaces.Length; i++)
            {
                var enemy = enemyScript_spaces[i];
                if (enemy == null)
                    continue;

                ApplyLegacyStat(enemy);
            }
        }

        private void ApplyChapterGrowthStatsToEnemyReferences()
        {
            if (!chapterGrowthRows.TryGetValue(
                    currentChapter,
                    out Dictionary<EnemyTier, MonsterGrowthRow> chapterRows))
            {
                throw new System.IO.InvalidDataException(
                    $"Sheet '{MonsterGrowthTables.SheetName}' has no rows for chapter {currentChapter}.");
            }

            var encounterEnemies = new List<EnemyScript_space>();
            foreach (EnemyScript_space enemy in enemyScript_spaces)
            {
                if (enemy == null)
                    continue;

                if (IsPooledEnemy(enemy.gameObject))
                    ApplyLegacyStat(enemy);
                else
                    encounterEnemies.Add(enemy);
            }

            if (encounterEnemies.Count == 0)
                return;

            Vector3 routeStart = playerSpawnPoint != null
                ? playerSpawnPoint.position
                : playerScript != null
                    ? playerScript.transform.position
                    : Vector3.zero;
            Vector3 routeDirection = playerSpawnPoint != null
                ? playerSpawnPoint.forward
                : playerScript != null
                    ? playerScript.transform.forward
                    : Vector3.forward;
            ChapterEnemyProgression.ApplyStats(
                encounterEnemies,
                chapterRows,
                routeStart,
                routeDirection,
                ChapterEnemyProgression.CollectRouteTurns(gameObject.scene));
        }

        private void ApplyLegacyStat(EnemyScript_space enemy)
        {
            EnemyTier fixedTier = ForwardEnemyTierResolver.ResolveOrFallback(
                enemy.gameObject.name,
                enemy.enemyTier);
            EnemyStat baseStat = GetLocalStat(fixedTier);
            enemy.ApplyStat(
                baseStat.damage,
                baseStat.health,
                fixedTier,
                enemy.enemyCombatType);
        }

        public static bool IsPooledEnemy(GameObject enemyObject)
            => ChapterEnemyProgression.IsPooledEnemy(enemyObject);

        private void RefreshEnemyReferences()
        {
            EnemyScript_space[] sceneEnemies = FindObjectsByType<EnemyScript_space>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var activeSceneEnemies = new List<EnemyScript_space>(sceneEnemies.Length);
            foreach (EnemyScript_space enemy in sceneEnemies)
            {
                if (enemy != null && enemy.gameObject.scene == gameObject.scene)
                    activeSceneEnemies.Add(enemy);
            }

            enemyScript_spaces = activeSceneEnemies.ToArray();
        }

        private void ClearRuntimeBonusWalls()
        {
            var walls = FindObjectsByType<RuntimeBonusWall>(FindObjectsSortMode.None);
            foreach (var w in walls) Destroy(w.gameObject);
        }
    }


}


