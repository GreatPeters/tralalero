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
        private WeaponManager weaponManager; // 있으면

        public GameObject extraHelp_TungTungTung;
        public GameObject extraHelp_BoomBarDino;
        public PlayerScript playerScript;

        public static GameManager S;
        public int currentChapter;
        public int currentStage;

        public int maxStage = 10;
        public int maxChapter = 10;

        [SerializeField] private EnemyTypeInfos[] enemyTypeInfos;

        public List<List<EnemyStat>> normalMonster;
        public List<List<EnemyStat>> eliteMonster;
        public List<List<EnemyStat>> bossMonster;

        [SerializeField] private StageObstacleManager StageObstacleManager;

        [Header("Scene References")]
        public Transform EnemyParent;

        [Header("Player Reset")]
        [SerializeField] private Transform playerSpawnPoint;     // ✅ 스테이지 시작 위치

        [Header("UI")]
        [SerializeField] private GameObject tapToPlayUI;         // ✅ "TAP TO PLAY" 패널(캔버스)

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

            // 처음엔 대기 화면 띄우고 멈춰두는 걸 추천
            PrepareStageAndShowTapUI();

            SettingEnemyTypeInfos();
            SettingMonsterStats();
            ApplyStatsToAllEnemies();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnStageClear();
            }
        }

        // =========================
        // ✅ 스테이지 클리어 → 다음 스테이지로 이동 (대기 상태)
        // =========================
        public void OnStageClear()
        {
            // 1) 일단 멈추기(이동/발사 포함)
            SetGameRunning(false);

            // 2) 다음 스테이지 인덱스 계산
            GoNextStageIndex();

            // 3) 다음 스테이지 준비(리셋+로드)
            PrepareStageObjectsForNextRun();

            // 4) "Tap to Play" 다시 띄우기 (CanvasScript가 갖고있음)
            if (canvas != null) canvas.tapToPlayScreen.SetActive(true);
        }

        // =========================
        // ✅ "TAP TO PLAY" 버튼에서 호출
        // =========================
        public void OnTapToPlay()
        {
            // 탭 UI 내리고 게임 시작
            ShowTapUI(false);
            SetGameRunning(true);
        }

        // =========================
        // ✅ 다음 스테이지 준비(초기화 + 로드)
        // =========================
        private void PrepareStageAndShowTapUI()
        {
            SetGameRunning(false);
            ShowTapUI(true);

            PrepareStageObjectsForNextRun();
        }

        private void PrepareStageObjectsForNextRun()
        {
            //보너스로 생성한 Wall 모두 제거
            ClearRuntimeBonusWalls();

            // (A) 플레이어 위치 리셋
            ResetPlayerToSpawn();

            // (B) 장애물 풀 반환 + 새 스테이지 로드
            if (StageObstacleManager != null)
            {
                StageObstacleManager.SetStage(currentChapter-1, currentStage-1);
                StageObstacleManager.LoadStageObstacles(); // 내부에서 ClearObstacles() 호출됨
            }

            // (C) 적들 리셋 (OnEnable 리셋 구조 쓰는 게 제일 안정적)
            ResetAllEnemiesByReEnable();

            // (D) 적 스탯 다시 주입
            ApplyStatsToAllEnemies();

            // (E) ExtraHelp 남아있으면 정리
            ClearAllExtraHelps();

            // (F) Wall 다시 랜덤 세팅(재생성 X, Init이 정답)
            if (WallManager.S != null)
                WallManager.S.InIt();
        }

        private void ResetPlayerToSpawn()
        {
            if (playerScript == null) return;
            if (playerSpawnPoint == null) return;

            var t = playerScript.transform;
            t.position = playerSpawnPoint.position;
            t.rotation = playerSpawnPoint.rotation;

            // Rigidbody 있으면 속도 0
            var rb = playerScript.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // (선택) 플레이어 내부 상태까지 리셋하고 싶으면
            // playerScript.ResetState();  // 이런 함수가 있다면 여기서 호출
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
            // ExtraHelpTag로 통일되어 있으면 이게 제일 깔끔
            // (EnemyScript_space에서도 ExtraHelpTag로 충돌 체크 중)
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
                    currentChapter = 0; // 엔딩/루프 처리 원하면 여기 수정
            }
        }

        private void ShowTapUI(bool show)
        {
            if (tapToPlayUI != null)
                tapToPlayUI.SetActive(show);
        }

        private void SetGameRunning(bool running)
        {
            TimeManager.isGameRunning = running;
            TimeManager.timeFactor = running ? 1f : 0f;

            // // ✅ 입력/발사 확실히 멈추고 싶으면 컴포넌트도 꺼버리기(강력)
            // if (playerScript != null) playerScript.enabled = running;
            // if (weaponManager != null) weaponManager.enabled = running;
        }

        // =========================
        // 기존 코드 (유지)
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
