using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class WeaponScript : MonoBehaviour
    {
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

        private void Awake()
        {
            damage = weaponSO.weaponDamage;                                 // Set damage from the weapon SO
            fireRate = weaponSO.weaponFireRate;                             // Set fire rate from the weapon SO
            playerScript = GetComponentInParent<PlayerScript>();
            extraHelpBuffScript = GetComponentInParent<ExtraHelpBuffScript>();
            audioSource = GetComponent<AudioSource>();

            weaponRB = GetComponent<Rigidbody>();
            weaponRB.useGravity = false;

            originalFireRate = fireRate;
        }

        void OnEnable()
        {
            if(bulletKind == BulletKind.Water)
            {
                RestartShooting();
            }
            else if(bulletKind == BulletKind.Bomb)
            {
                Invoke("RestartShooting", 1f);
            }

                parentAnimator = GetComponentInParent<Animator>();
        }

        private void OnDisable()
        {
            CancelInvoke("ShootBullet");
        }


        private void Start()
        {
            bulletPooler = FindFirstObjectByType<BulletPooler>().GetComponent<BulletPooler>();
            previousFireRate = fireRate;                            // Store the initial fire rate
        }


        private void FixedUpdate()
        {
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

                    weaponRB.useGravity = true;
                    weaponRB.isKinematic = false;
                }
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
            if (isShooting && TimeManager.isGameRunning && gameObject.activeInHierarchy)
            {
                if (playerScript != null && !playerScript.canShoot) return;

                int count = Mathf.Min(bulletCount, bulletPositions.Length);
                for (int i = 0; i < count; i++)
                {
                    Transform bulletPos = bulletPositions[i];
                    parentAnimator.SetTrigger("WeaponShoot");
                    audioSource.PlayOneShot(weaponSO.weaponSound);

                    // ⬇️ 종류 지정 꺼내기 (새 API)
                    GameObject bullet = bulletPooler.Get(bulletKind, transform);
                    if (bullet != null)
                    {
                        bullet.transform.position = bulletPos.position;
                        bullet.transform.parent = null;
                        // 기존처럼 방향 지정
                        bullet.GetComponentInChildren<BulletScript>().SetDirection(bulletPos.up); //:contentReference[oaicite:2]{index=2}:contentReference[oaicite:3]{index=3}
                    }
                }
            }
        }

        // Destory weapon if on ground with delay
        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("GroundTag"))
            {
                Destroy(weaponRB, 2f);
            }
        }

    }
}