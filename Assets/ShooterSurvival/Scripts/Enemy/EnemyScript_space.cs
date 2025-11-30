using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;


namespace IndianOceanAssets.ShooterSurvival
{
    public class EnemyScript_space : MonoBehaviour
    {
        public GameObject bonusWall;
        private bool isDie = false;

        private bool _injected;
        public EnemyTier enemyTier;
        public EnemyCombatType enemyCombatType;

        [Header("Runtime")]
        [Tooltip("Health value that decreases when damaged")]
        [SerializeField] private float _health;

        [Tooltip("Damage dealt to player or other entities")]
        [SerializeField] private float _damage;

        [Tooltip("Score rewarded to player when this enemy dies")]
        [SerializeField] public int _score;

        [Header("Params")]
        [Tooltip("Defines the type of this enemy (e.g., Walker, Rusher, Tank)")]
        [SerializeField] public EnemyType enemyType;

        [Header("Debugging options")]
        [Tooltip("Enable/Disable receiving damage")]
        [SerializeField] private bool recieveDamage = true;

        [Tooltip("Enable/Disable giving damage")]
        [SerializeField] private bool giveDamage = true;

        [Header("Dependancies")]
        [Tooltip("ScriptableObject list for all enemy type data")]
        [SerializeField] private EnemySO[] enemySOArray;

        private Transform hitPos;
        private EffectOverlayScript effectOverlayVignette;
        private EnemySO currentEnemySO;
        private PlayerScript playerScript;
        private Animator enemyAnimator;
        private AudioSource audioSource;
        private bool givePlayerScore = true;

        private TextMeshProUGUI healthText;

        // 🟧 Throw (Once)
        [Header("🟧 Throw (Once)")]
        [SerializeField] private Transform heldProjectile; // 손에 들고 있는 오브젝트(자식)
        [SerializeField] private Transform throwPoint;     // 손끝 기준(없어도 OK)
        [SerializeField] private float throwRange = 7f;    // 던질 거리
        [SerializeField] private float throwSpeed = 12f;   // 던질 속도(=VelocityChange 크기)
        [SerializeField] private float projectileLife = 6f;// 투사체 수명
        private bool hasThrown = false;                    // 한 번만 던지기 플래그

        private void Awake()
        {
            hitPos = transform.GetChild(1).GetComponent<Transform>();
            playerScript = FindFirstObjectByType<PlayerScript>().GetComponent<PlayerScript>();
            effectOverlayVignette = GameObject.FindGameObjectWithTag("VolumeTag").GetComponent<EffectOverlayScript>();
            audioSource = GetComponent<AudioSource>();
            enemyAnimator = GetComponentInChildren<Animator>();

            if (enemyType == EnemyType.Walker)
            {
                healthText = transform.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        private void Start()
        {
            currentEnemySO = enemySOArray[(int)enemyType];

            if (_injected) return;

            _health = currentEnemySO.enemyHealth;
            _damage = currentEnemySO.enemyDamage;
            _score = currentEnemySO.scoreUponDeath;
        }

        private void Update()
        {
            enemyAnimator.enabled = TimeManager.isGameRunning;
            if (!TimeManager.isGameRunning) return;

            // 가까워지면 한 번만 던지기
            if (!hasThrown && playerScript != null && heldProjectile != null)
            {
                Vector3 from = (throwPoint ? throwPoint.position : transform.position);
                if (Vector3.Distance(from, playerScript.transform.position) <= throwRange)
                {
                    ThrowOnceSimple();
                }
            }

            if (enemyType == EnemyType.Walker && healthText != null)
            {
                healthText.text = _health.ToString("F0");
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (!giveDamage) return;

                effectOverlayVignette.NerfOverlay();

                if (playerScript.currentHealth > _health)
                {
                    playerScript.currentHealth -= _health;
                    _health = 0f;

                    givePlayerScore = false;
                    EnemyDeath();
                    isDie = true;
                }
                else
                {
                    float playerHP = playerScript.currentHealth;
                    _health -= playerHP;
                    playerScript.currentHealth = 0f;
                    givePlayerScore = false;
                }
            }

            if (other.CompareTag("ExtraHelpTag"))
            {
                if (!giveDamage) return;

                ExtraHelpBuffScript extraHelpBuffScript = other.GetComponent<ExtraHelpBuffScript>();

                if (extraHelpBuffScript.currentHealth > _health)
                {
                    extraHelpBuffScript.currentHealth -= _health;
                    _health = 0f;

                    givePlayerScore = false;
                    EnemyDeath();
                }
                else
                {
                    float extraHP = extraHelpBuffScript.currentHealth;
                    _health -= extraHP;
                    extraHelpBuffScript.currentHealth = 0f;
                    givePlayerScore = false;
                }
            }

            if (other.CompareTag("BulletTag"))
            {
                if (!recieveDamage) return;

                GameObject hitfx = Instantiate(currentEnemySO.enemyHitVFX, hitPos);
                Destroy(hitfx, hitfx.GetComponent<ParticleSystem>().main.duration);

                _health -= playerScript.currentDamage;

                if (_health <= 0f)
                {
                    _health = 0f;
                    audioSource.PlayOneShot(currentEnemySO.enemyDeathSound);
                    EnemyDeath();
                }
            }

            if (other.CompareTag("DestroyerTag"))
            {
                EnemyDeath();
            }
        }

        public void EnemyDeath()
        {
            Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y+0.95f, transform.position.z);
            GameObject.Instantiate(bonusWall, spawnPos, Quaternion.identity);

            recieveDamage = false;
            giveDamage = false;
            
            enemyAnimator.SetTrigger("die");
            StartCoroutine(DeathFlow());

            //GameObject deathfx = Instantiate(currentEnemySO.enemyDeathVFX, hitPos);
            //float destruction_timer = deathfx.GetComponent<ParticleSystem>().main.duration;
            //Destroy(deathfx, destruction_timer);

            GetComponent<Collider>().enabled = false;

            if (givePlayerScore) playerScript.playerScore += _score;
            givePlayerScore = false;
        }

        IEnumerator DeathFlow()
        {
            yield return new WaitForSeconds(0.5f);
            healthText.gameObject.SetActive(false);

            yield return new WaitForSeconds(1f);
            GameObject enemyVisual = transform.GetChild(0).gameObject;
            enemyVisual.SetActive(false);
        }


        public void ApplyStat(float damage, float health, EnemyTier tier, EnemyCombatType combatType)
        {
            _injected = true;
            Debug.Log("적용!");

            enemyTier = tier;
            enemyCombatType = combatType;

            _damage = damage;
            _health = health;

            if (enemyType == EnemyType.Walker)
            {
                if (healthText == null)
                    healthText = GetComponentInChildren<TextMeshProUGUI>();

                if (healthText != null)
                    healthText.text = _health.ToString("F0");
            }
        }

        // 🟧 한 번 던지기 (심플 + 안정화)
        private void ThrowOnceSimple()
        {
            enemyAnimator.SetTrigger("act");          

            if (hasThrown || heldProjectile == null) return;
            hasThrown = true;
            float sec = 2f;

            if(transform.name.Contains("FatMan"))
            {
                sec = 1.6f;
            }
            else if(transform.name.Contains("Guard"))
            {
                sec = 0.8f;
            }
            else
            {
                sec = 2f;
            }

                DOVirtual.DelayedCall(sec, () =>
                {
                    if(isDie == true)
                    {
                        return;
                    }

                    if (throwPoint)
                    {
                        heldProjectile.position = throwPoint.position;
                        heldProjectile.rotation = throwPoint.rotation;
                    }

                    heldProjectile.SetParent(null, true);
                    heldProjectile.rotation = Quaternion.Euler(0f, 90f, 0f);

                    var col = heldProjectile.GetComponent<Collider>();
                    if (col == null) col = heldProjectile.gameObject.AddComponent<SphereCollider>();
                    col.isTrigger = true;
                    col.GetComponent<SimpleProjectile>().damage = _damage;

                    var rb = heldProjectile.GetComponent<Rigidbody>();
                    if (rb == null) rb = heldProjectile.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.useGravity = false; // 포물선 원하면 true
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.interpolation = RigidbodyInterpolation.Interpolate; 

                    // 발사자 본체와 충돌 무시
                    foreach (var ec in GetComponentsInChildren<Collider>(true))
                        if (ec && col) Physics.IgnoreCollision(col, ec, true);

                    // 발사
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.AddForce(-throwPoint.forward * throwSpeed, ForceMode.VelocityChange);

                    //Destroy(heldProjectile.gameObject, projectileLife);
                });
        }        
    }
}
