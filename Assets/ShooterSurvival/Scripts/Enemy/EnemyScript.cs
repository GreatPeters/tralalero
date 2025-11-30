using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public enum EnemyType
    {
        Walker,
        Rusher,
        Tank
    }

    public class EnemyScript : MonoBehaviour
    {
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

        [Header("Debugging Options")]
        [Tooltip("Enable/Disable enemy movement")]
        [SerializeField] private bool movement = true;

        [Tooltip("Enable/Disable receiving damage")]
        [SerializeField] private bool recieveDamage = true;

        [Tooltip("Enable/Disable giving damage")]
        [SerializeField] private bool giveDamage = true;

        [Header("Dependencies")]
        [Tooltip("ScriptableObject list for all enemy type data")]
        [SerializeField] private EnemySO[] enemySOArray;


        private Transform hitPos;
        private EffectOverlayScript effectOverlayVignette;
        private EnemySO currentEnemySO;
        private float _speed;
        private float _turnSpeed;
        private float playerDetectDistance;
        private PlayerScript playerScript;
        private Transform playerTransform;
        private EnemyPooler enemyPooler;
        private FX_pooler fX_Pooler;
        private Animator enemyAnimator;
        private AudioSource audioSource;
        private bool givePlayerScore = true;

        private void Awake()
        {
            hitPos = transform.GetChild(1).GetComponent<Transform>();
            playerScript = FindFirstObjectByType<PlayerScript>().GetComponent<PlayerScript>();
            enemyPooler = FindFirstObjectByType<EnemyPooler>().GetComponent<EnemyPooler>();
            fX_Pooler = FindFirstObjectByType<FX_pooler>().GetComponent<FX_pooler>();
            effectOverlayVignette = GameObject.FindGameObjectWithTag("VolumeTag").GetComponent<EffectOverlayScript>();
            audioSource = GetComponent<AudioSource>();
            playerTransform = playerScript.transform;
            playerDetectDistance = playerScript.enemyDetectRadius;
            enemyAnimator = GetComponentInChildren<Animator>();

            transform.Rotate(new Vector3(0, 180, 0));
        }

        private void OnEnable()
        {

            currentEnemySO = enemySOArray[(int)enemyType];

            // Initializing local variables
            _health = currentEnemySO.enemyHealth;
            _damage = currentEnemySO.enemyDamage;
            _score = currentEnemySO.scoreUponDeath;
            _speed = currentEnemySO.enemySpeed;
            _turnSpeed = currentEnemySO.turnSpeed;
        }

        private void Update()
        {
            if (TimeManager.isGameRunning == false) enemyAnimator.enabled = false;
            else enemyAnimator.enabled = true;
        }

        private void FixedUpdate()
        {
            // Enemy movement
            if (TimeManager.isGameRunning == false) return;
            if (movement)
                EnemyMove();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Damage player on contact
            if (other.CompareTag("Player"))
            {
                effectOverlayVignette.NerfOverlay();
                if (giveDamage)
                {
                    if (playerScript.currentHealth > _health)
                    {
                        playerScript.currentHealth -= _health;
                        _health = 0f;

                        givePlayerScore = false;
                        EnemyDeath();
                    }
                    else if(playerScript.currentHealth <= _health)
                    {
                        float playerHP = playerScript.currentHealth;

                        _health -= playerHP;
                        playerScript.currentHealth = 0f;
                        
                        givePlayerScore = false;
                    }             
                }
            }

            // Damage ExtraHelpBuff entity
            if (other.CompareTag("ExtraHelpTag"))
            {
                if (giveDamage)
                {
                    ExtraHelpBuffScript extraHelpBuffScript = other.GetComponent<ExtraHelpBuffScript>();
                    extraHelpBuffScript.currentHealth -= _damage;
                }
            }

            // Take damage from player's bullet
            if (other.CompareTag("BulletTag"))
            {
                if (recieveDamage)
                {

                    // effects
                    fX_Pooler.GetObjectFromPool_FX(enemyType.ToString() + "_HitEffect", hitPos.transform);
                    audioSource.PlayOneShot(currentEnemySO.enemyHitSound);

                    _health -= playerScript.currentDamage;

                    if (_health <= 0f)
                    {
                        audioSource.PlayOneShot(currentEnemySO.enemyDeathSound);
                        EnemyDeath();
                    }
                }
            }

            // Return enemy to pool if it touches the destroyer area
            if (other.CompareTag("DestroyerTag"))
            {
                ReturnToPool();
            }
        }

        private void EnemyMove()
        {
            if (TimeManager.isGameRunning == false) return;

            Enemy_SurvivalMode();
        }

        private void Enemy_SurvivalMode()
        {
            if (playerTransform == null)
            {
                var player = FindFirstObjectByType<PlayerScript>();
                if (player == null) return;  // No player present, skip

                playerScript = player.GetComponent<PlayerScript>();
                playerTransform = playerScript.transform;
            }

            if (Vector3.Distance(transform.position, playerTransform.position) >= playerDetectDistance)
            {
                transform.Translate(Vector3.forward * _speed * Time.deltaTime * TimeManager.timeFactor);
            }
            else DetectPlayer();
        }

        private void DetectPlayer()
        {
            Vector3 moveDir = (playerTransform.position - transform.position).normalized;
            transform.position += moveDir * _speed * Time.deltaTime * TimeManager.timeFactor;

            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _turnSpeed * Time.deltaTime * TimeManager.timeFactor);
        }

        private void EnemyDeath()
        {
            movement = false;
            gameObject.GetComponent<Collider>().enabled = false;

            GameObject fx = fX_Pooler.GetObjectFromPool_FX(enemyType.ToString() + "_DeathEffect", hitPos.transform);  // special FX for tank
            if (fx == null) print("fx not found");

            GameObject enemyVisual = transform.GetChild(0).gameObject;
            enemyVisual.SetActive(false);
            Invoke(nameof(ReturnToPool), 2);             // Delay Return to Pool

            if (givePlayerScore == true) playerScript.playerScore += _score;
            givePlayerScore = true;
        }

        private void ReturnToPool()
        {
            enemyPooler.ReturnObjectToPool_Enemy(enemyType, gameObject);
            gameObject.GetComponent<Collider>().enabled = true;
            movement = true;

        }

     

    }
}