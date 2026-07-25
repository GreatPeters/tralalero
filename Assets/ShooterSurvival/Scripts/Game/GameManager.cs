using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [System.Serializable]
    public struct EnemyTypeInfos
    {
        public EnemyTier tier;                 // Normal / Elite / Boss
        public EnemyCombatType combatType;     // Melee / Ranged
    }

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

        [SerializeField] private EnemyTypeInfos[] enemyTypeInfos;

        public List<List<EnemyStat>> normalMonster;
        public List<List<EnemyStat>> eliteMonster;
        public List<List<EnemyStat>> bossMonster;

        [SerializeField] private StageObstacleManager StageObstacleManager;

        [Header("Scene References")]
        public Transform EnemyParent;

        [Header("Player Reset")]
        [SerializeField] private Transform playerSpawnPoint;     // ???�테?��? ?�작 ?�치

        [Header("UI")]

        public EnemyScript_space[] enemyScript_spaces;
        public List<float> indexBonus = new List<float> { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 5f };

        void Awake()
        {
            S = this;
            canvas = FindFirstObjectByType<CanvasScript>();

            if (playerScript != null)
                weaponManager = playerScript.GetComponent<WeaponManager>();


            enemyScript_spaces = new EnemyScript_space[EnemyParent.childCount];
            for (int i = 0; i < EnemyParent.childCount; i++)
                enemyScript_spaces[i] = EnemyParent.GetChild(i).GetComponent<EnemyScript_space>();


        }

        void Start()
        {
            currentChapter = 1;
            currentStage = 1;

            TimeManager.isGameRunning = false;
            TimeManager.timeFactor = 0f;

            // 처음???��??�면 ?�우�?멈춰?�는 �?추천
            PrepareStageAndShowTapUI();

            SettingEnemyTypeInfos();
            SettingMonsterStats();
            ApplyStatsToAllEnemies();
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

            // (C) ?�들 리셋 (OnEnable 리셋 구조 ?�는 �??�일 ?�정??
            ResetAllEnemiesByReEnable();

            // (D) ???�탯 ?�시 주입
            ApplyStatsToAllEnemies();

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
                if (e == null) continue;

                e.gameObject.SetActive(false);
                e.gameObject.SetActive(true);
            }
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

            if (currentStage >= maxStage)
            {
                currentStage = 0;
                currentChapter++;

                if (currentChapter >= maxChapter)
                    currentChapter = 0; // ?�딩/루프 처리 ?�하�??�기 ?�정
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

        public void SettingEnemyTypeInfos()
        {
            if (enemyTypeInfos == null || enemyTypeInfos.Length == 0)
            {
                enemyTypeInfos = new EnemyTypeInfos[]
                {
                    new() { tier = EnemyTier.Normal, combatType = EnemyCombatType.Melee  },
                    new() { tier = EnemyTier.Normal, combatType = EnemyCombatType.Ranged },
                    new() { tier = EnemyTier.Elite,  combatType = EnemyCombatType.Melee  },
                    new() { tier = EnemyTier.Normal, combatType = EnemyCombatType.Melee  },
                    new() { tier = EnemyTier.Normal, combatType = EnemyCombatType.Ranged },
                    new() { tier = EnemyTier.Normal, combatType = EnemyCombatType.Melee  },
                    new() { tier = EnemyTier.Elite,  combatType = EnemyCombatType.Ranged },
                    new() { tier = EnemyTier.Boss,   combatType = EnemyCombatType.Melee  },
                };
            }
        }

        public void SettingMonsterStats()
        {
            if (TryBuildMonsterStatsFromExcel())
                return;

            normalMonster = new List<List<EnemyStat>>();
            eliteMonster = new List<List<EnemyStat>>();
            bossMonster = new List<List<EnemyStat>>();

            for (int chapter = 0; chapter < maxChapter; chapter++)
            {
                var normalList = new List<EnemyStat>();
                var eliteList = new List<EnemyStat>();
                var bossList = new List<EnemyStat>();

                for (int stage = 0; stage < maxStage; stage++)
                {
                    normalList.Add(new EnemyStat(30 * chapter + stage * 3, 50 * chapter + stage * 5));
                    eliteList.Add(new EnemyStat(50 * chapter + stage * 5, 100 * chapter + stage * 10));
                    bossList.Add(new EnemyStat(150 * chapter + stage * 15, 300 * chapter + stage * 30));
                }

                normalMonster.Add(normalList);
                eliteMonster.Add(eliteList);
                bossMonster.Add(bossList);
            }
        }

        private bool TryBuildMonsterStatsFromExcel()
        {
            List<MonsterRow> rows;
            try
            {
                rows = MonsterTables.GetAll();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] Monster table load failed. fallback to formula. {e.Message}");
                return false;
            }

            if (rows == null || rows.Count == 0) return false;

            InitMonsterListsWithNull();

            foreach (var r in rows)
            {
                if (r.chapter < 0 || r.stage < 0) continue;
                if (r.chapter > maxChapter || r.stage > maxStage) continue;

                var list = GetTierList(r.tier);
                if (list == null) continue;

                list[r.chapter][r.stage] = new EnemyStat(r.damage, r.health);
            }

            for (int chapter = 0; chapter <= maxChapter; chapter++)
            {
                for (int stage = 0; stage <= maxStage; stage++)
                {
                    if (chapter == 0 || stage == 0) continue;

                    EnsureFilled(normalMonster, EnemyTier.Normal, chapter, stage);
                    EnsureFilled(eliteMonster, EnemyTier.Elite, chapter, stage);
                    EnsureFilled(bossMonster, EnemyTier.Boss, chapter, stage);
                }
            }

            Debug.Log("[GameManager] Monster stats loaded from Excel.");
            return true;
        }

        private void InitMonsterListsWithNull()
        {
            normalMonster = new List<List<EnemyStat>>(maxChapter + 1);
            eliteMonster = new List<List<EnemyStat>>(maxChapter + 1);
            bossMonster = new List<List<EnemyStat>>(maxChapter + 1);

            for (int chapter = 0; chapter <= maxChapter; chapter++)
            {
                var normalList = new List<EnemyStat>(maxStage + 1);
                var eliteList = new List<EnemyStat>(maxStage + 1);
                var bossList = new List<EnemyStat>(maxStage + 1);

                for (int stage = 0; stage <= maxStage; stage++)
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

        private void EnsureFilled(List<List<EnemyStat>> list, EnemyTier tier, int chapter, int stage)
        {
            if (list[chapter][stage] != null) return;

            var fallback = BuildFallbackStat(tier, chapter, stage);
            list[chapter][stage] = fallback;
            Debug.LogWarning($"[GameManager] Monster stats missing in Excel. fallback used. tier={tier} chapter={chapter} stage={stage}");
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
            switch (tier)
            {
                case EnemyTier.Normal: return normalMonster[currentChapter][currentStage];
                case EnemyTier.Elite: return eliteMonster[currentChapter][currentStage];
                case EnemyTier.Boss: return bossMonster[currentChapter][currentStage];
                default: return normalMonster[currentChapter][currentStage];
            }
        }

        public void ApplyStatsToAllEnemies()
        {
            if (enemyScript_spaces == null || enemyTypeInfos == null) return;

            int count = Mathf.Min(enemy_script_spaces_len(), enemyTypeInfos.Length);
            for (int i = 0; i < count; i++)
            {
                var info = enemyTypeInfos[i];
                var baseStat = GetLocalStat(info.tier);

                var enemy = enemyScript_spaces[i];
                if (enemy == null) continue;

                float bonus = (indexBonus != null && indexBonus.Count > 0)
                              ? indexBonus[Mathf.Min(i, indexBonus.Count - 1)]
                              : 1f;

                enemy.ApplyStat(baseStat.damage * bonus, baseStat.health * bonus, info.tier, info.combatType);
            }

            if (enemyScript_spaces.Length != enemyTypeInfos.Length)
                Debug.LogWarning($"Count mismatch: enemies={enemyScript_spaces.Length}, infos={enemyTypeInfos.Length}");
        }

        private int enemy_script_spaces_len() => enemyScript_spaces == null ? 0 : enemyScript_spaces.Length;

        private void ClearRuntimeBonusWalls()
        {
            var walls = FindObjectsByType<RuntimeBonusWall>(FindObjectsSortMode.None);
            foreach (var w in walls) Destroy(w.gameObject);
        }
    }


}


