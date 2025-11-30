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
        //public GameObject[] extraHelps;
        public GameObject extraHelp_TungTungTung;
        public GameObject extraHelp_BoomBarDino;
        public PlayerScript playerScript;

        public static GameManager S;
        public int currentChapter;
        public int currentStage;

        public int maxStage = 10;
        public int maxChapter = 10;

        [SerializeField] private EnemyTypeInfos[] enemyTypeInfos;

        // �� SO ����
        // [SerializeField] private AllStageEnemyStats enemyStatsSO;

        public List<List<EnemyStat>> normalMonster;
        public List<List<EnemyStat>> eliteMonster;
        public List<List<EnemyStat>> bossMonster;

        [SerializeField] private StageObstacleManager StageObstacleManager;
        public Transform EnemyParent;
        public EnemyScript_space[] enemyScript_spaces;
        public List<float> indexBonus = new List<float> { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 5f };

        void Awake()
        {
            enemyScript_spaces = new EnemyScript_space[EnemyParent.childCount];
            for (int i=0; i< EnemyParent.childCount; i++)
            {
                enemyScript_spaces[i] = EnemyParent.GetChild(i).GetComponent<EnemyScript_space>();
            }

            S = this;
            SettingEnemyTypeInfos();
            SettingMonsterStats();

            ApplyStatsToAllEnemies();
        }

        void Start()
        {
            currentChapter = 0;
            currentStage = 0;
            Debug.Log("오잉1");

            if (StageObstacleManager != null)
            {
                Debug.Log("오잉2");
                StageObstacleManager.SetStage(currentChapter, currentStage);
                StageObstacleManager.LoadStageObstacles();
            }            
        }

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

        // �� ���� ���̺����� ���� �̱�
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

        // �� �ϰ� ����
        public void ApplyStatsToAllEnemies()
        {
            if (enemyScript_spaces == null || enemyTypeInfos == null) return;

            int count = Mathf.Min(enemyScript_spaces.Length, enemyTypeInfos.Length);
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
    }
}
