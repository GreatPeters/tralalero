using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class WeaponScript : MonoBehaviour
    {
        private const string PlayerDefaultAttVariableKey = "playerDefaultAtt";
        private const string VisiblePlayerVisualName = "Original";
        private const string VisiblePlayerMouthName = "headend";
        private const string VisiblePlayerMuzzleName = "ProjectileMuzzle";
        private const float VisiblePlayerMuzzleForwardOffset = 0.35f;
        public BulletKind bulletKind;

        [Header("Runtime")]
        [SerializeField] public float damage;                   // Weapon damage value
        [SerializeField] public float fireRate;                 // Weapon fire rate
        public int bulletCount = 1;
        public float originalFireRate;            // Original fire rate value, used for buffs
        //public float origi

        [Header("Dependancies")]
        [SerializeField] private WeaponSO weaponSO;             //Weapon's ScriptableObject for weapon stats

        [Tooltip("Assign only one transform if you intend to fire a single bullet at a time")]
        [SerializeField] private Transform[] bulletPositions;           // Position from where the bullet will be shot

        private AudioSource audioSource;
        private Rigidbody weaponRB;
        private PlayerScript playerScript;
        private BulletPooler bulletPooler;
        private float previousFireRate;                         // To track if the fire rate has changed
        private Animator parentAnimator;
        private bool isShooting = true;
        private ExtraHelpBuffScript extraHelpBuffScript;
        private bool subscribedToStats;
        private Transform playerProjectileMuzzle;
        private Transform visiblePlayerMouth;

        public int TotalProjectilesSpawned { get; private set; }
        public Vector3 LastProjectileSpawnPosition { get; private set; }
        public Vector3 LastVisibleMouthPositionAtSpawn { get; private set; }


        private void Awake()
        {
            playerScript = GetComponentInParent<PlayerScript>();
            extraHelpBuffScript = GetComponentInParent<ExtraHelpBuffScript>();
            CacheVisiblePlayerMuzzle();
            audioSource = GetComponent<AudioSource>();
            damage = GetBaseDamage();                                        // Set damage from config or weapon SO
            fireRate = GetBaseFireRate();
            bulletCount = GetBaseBulletCount();

            weaponRB = GetComponent<Rigidbody>();
            weaponRB.useGravity = false;

            originalFireRate = fireRate;
            ApplyUpgradeStats();
        }

        void OnEnable()
        {
            SubscribeToStatChanges();

            if (bulletKind == BulletKind.Water)
            {
                RestartShooting();
            }
            else if (bulletKind == BulletKind.Bomb)
            {
                Invoke("RestartShooting", 1f);
            }

            parentAnimator = GetComponentInParent<Animator>();
            ApplyUpgradeStats();
        }

        private void OnDisable()
        {
            CancelInvoke("ShootBullet");

            UnsubscribeFromStatChanges();
        }


        private void Start()
        {
            SubscribeToStatChanges();
            bulletPooler = FindFirstObjectByType<BulletPooler>().GetComponent<BulletPooler>();
            previousFireRate = fireRate;                            // Store the initial fire rate
        }


        private void FixedUpdate()
        {
            SubscribeToStatChanges();

            if (fireRate != previousFireRate)
            {
                RestartShooting();                      // Restart shooting with the new fire rate
                previousFireRate = fireRate;
            }

            if (weaponRB != null)
            {
                bool isDead = false;

                if (playerScript != null)
                {
                    isDead = playerScript.currentHealth == 0;                   // Check if the player is dead
                }
                else if (extraHelpBuffScript != null)
                {
                    isDead = extraHelpBuffScript.currentHealth <= 0;            // Check if the Extra Help Buff is dead
                }

                if (isDead)
                {
                    isShooting = false;                                         // Stop shooting if dead
                    CancelInvoke("ShootBullet");

                    // weaponRB.useGravity = true;
                    // weaponRB.isKinematic = false;
                }

                //Debug.Log(isDead);
            }
        }


        // Restart shooting by canceling previous invocations and setting up a new interval.
        private void RestartShooting()
        {
            CancelInvoke("ShootBullet");

            if (fireRate > 0)
            {
                float shootInterval = 1f / fireRate;                  // Calculate the time interval between shots
                InvokeRepeating("ShootBullet", 0f, shootInterval);
            }
        }

        // Hnadle Bullet shooting
        private void ShootBullet()
        {
            //Debug.Log(isShooting + " isShooting");
            //Debug.Log(TimeManager.isGameRunning + " TimeManager.isGameRunning");

            if (isShooting && TimeManager.isGameRunning && gameObject.activeInHierarchy)
            {
                Debug.Log("슛!!!!?");
                if (playerScript != null && !playerScript.canShoot) return;

                int count = Mathf.Min(bulletCount, bulletPositions.Length);
                for (int i = 0; i < count; i++)
                {
                    Transform bulletPos = bulletPositions[i];
                    Vector3 direction = bulletPos.up.normalized;
                    Vector3 spawnPosition = ResolveProjectileSpawnPosition(bulletPos);
                    parentAnimator.SetTrigger("WeaponShoot");
                    audioSource.PlayOneShot(weaponSO.weaponSound);

                    // 종류 지정 꺼내기 (새 API)
                    GameObject bullet = bulletPooler.Get(bulletKind, transform);
                    if (bullet != null)
                    {
                        TotalProjectilesSpawned++;
                        LastProjectileSpawnPosition = spawnPosition;
                        LastVisibleMouthPositionAtSpawn = visiblePlayerMouth != null
                            ? visiblePlayerMouth.position
                            : Vector3.positiveInfinity;
                        bullet.transform.parent = null;
                        bullet.transform.position = spawnPosition;
                        bullet.transform.rotation = BuildProjectileRotation(direction);
                        bullet.GetComponentInChildren<BulletScript>().SetDirection(direction); //:contentReference[oaicite:2]{index=2}:contentReference[oaicite:3]{index=3}
                    }
                }
            }
        }

        private Vector3 ResolveProjectileSpawnPosition(Transform authoredMuzzle)
        {
            if (authoredMuzzle == null)
                return transform.position;

            if (playerScript == null || extraHelpBuffScript != null)
                return authoredMuzzle.position;

            if (playerProjectileMuzzle == null)
                CacheVisiblePlayerMuzzle();

            if (playerProjectileMuzzle != null)
                return playerProjectileMuzzle.position;

            if (visiblePlayerMouth != null)
            {
                return visiblePlayerMouth.position +
                    playerScript.transform.forward.normalized *
                    VisiblePlayerMuzzleForwardOffset;
            }

            return authoredMuzzle.position;
        }

        private void CacheVisiblePlayerMuzzle()
        {
            if (playerScript == null || extraHelpBuffScript != null)
                return;

            Transform visibleVisual = null;
            foreach (Transform child in playerScript.transform)
            {
                if (child.name != VisiblePlayerVisualName)
                    continue;

                visibleVisual = child;
                break;
            }

            if (visibleVisual == null)
                return;

            foreach (Transform candidate in visibleVisual.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == VisiblePlayerMouthName)
                    visiblePlayerMouth = candidate;
                else if (candidate.name == VisiblePlayerMuzzleName)
                    playerProjectileMuzzle = candidate;
            }
        }

        private static Quaternion BuildProjectileRotation(Vector3 direction)
        {
            return Quaternion.LookRotation(direction, Vector3.up);
        }

        // 발사 로직 초기화
        public void ResetShooting()
        {
            isShooting = true;
            CancelInvoke(nameof(ShootBullet));
            RestartShooting();
        }

        // Destory weapon if on ground with delay
        private void OnCollisionEnter(Collision other)
        {
            // if (other.gameObject.CompareTag("GroundTag"))
            // {
            //     Destroy(weaponRB, 2f);
            // }
        }

        //스탯 초기화
        public void ResetStatBonus()
        {
            // 스탯 원복
            damage = GetBaseDamage();
            fireRate = GetBaseFireRate();
            bulletCount = GetBaseBulletCount();
            ApplyUpgradeStats();
        }

        private void ApplyUpgradeStats()
        {
            if (UpgradeStatManager.S == null) return;

            damage = UpgradeStatManager.S.ApplyToBase(UpgradeStatManager.UpgradeType.ATT, GetBaseDamage());
            fireRate = UpgradeStatManager.S.ApplyToBase(
                UpgradeStatManager.UpgradeType.ATT_SPEED,
                GetBaseFireRate());
        }

        public void LogDamageBreakdown(string context = "GameStart")
        {
            float weaponSoBaseDamage = weaponSO != null ? weaponSO.weaponDamage : 0f;
            bool usesEnvironmentOverride = playerScript != null
                && extraHelpBuffScript == null
                && playerScript.UseExcelCharacterDefaults
                && EnvironmentVariableTables.TryGetFloat(PlayerDefaultAttVariableKey, out var attackValue)
                && attackValue > 0f;

            float originalDamage = GetBaseDamage();
            float flatBonus = UpgradeStatManager.S != null
                ? UpgradeStatManager.S.GetFlatStat(UpgradeStatManager.UpgradeType.ATT)
                : 0f;
            float percentBonus = UpgradeStatManager.S != null
                ? UpgradeStatManager.S.GetPercentStat(UpgradeStatManager.UpgradeType.ATT)
                : 0f;
            float finalDamage = originalDamage * (1f + percentBonus / 100f) + flatBonus;

            Debug.Log(
                $"[DamageDebug:{context}] weapon={name}, source={(usesEnvironmentOverride ? "EnvOverride" : "WeaponSO")}, " +
                $"weaponSOBase={weaponSoBaseDamage}, originalDamage={originalDamage}, additionalDamage={flatBonus}, " +
                $"bonusPercent={percentBonus}%, finalDamage={finalDamage}, runtimeDamageField={damage}");
        }

        private float GetBaseDamage()
        {
            if (playerScript != null && extraHelpBuffScript == null)
                return playerScript.DefaultAttackDamage;

            return weaponSO != null ? weaponSO.weaponDamage : 0f;
        }

        private float GetBaseFireRate()
        {
            if (playerScript != null && extraHelpBuffScript == null)
                return playerScript.DefaultFireRate;

            return weaponSO != null ? weaponSO.weaponFireRate : 0f;
        }

        private int GetBaseBulletCount()
        {
            return playerScript != null && extraHelpBuffScript == null
                ? Mathf.Max(1, playerScript.DefaultProjectileCount)
                : 1;
        }

        private void SubscribeToStatChanges()
        {
            if (subscribedToStats || UpgradeStatManager.S == null)
                return;

            UpgradeStatManager.S.StatsChanged += ApplyUpgradeStats;
            subscribedToStats = true;
        }

        private void UnsubscribeFromStatChanges()
        {
            if (!subscribedToStats || UpgradeStatManager.S == null)
                return;

            UpgradeStatManager.S.StatsChanged -= ApplyUpgradeStats;
            subscribedToStats = false;
        }


    }
}
