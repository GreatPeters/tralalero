using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum HelpType
{
    Boombardino,
    Tungtungtung
}

namespace IndianOceanAssets.ShooterSurvival
{
    public class ExtraHelpBuffScript : MonoBehaviour
    {
        [Header("Runtime")]
        [Tooltip("Current health of the Extra Help Buff.")]
        public float currentHealth;

        [Header("ExtraHelp Buff Params")]
        [Tooltip("Maximum health of the Extra Help Buff.")]
        [SerializeField] private float health = 100f;

        [Tooltip("Speed at which the Extra Help Buff follows the player.")]
        [SerializeField] private float followSpeed;

        [Header("Dependencies")]
        [Tooltip("Position of the weapon used by the Extra Help Buff.")]
        [SerializeField] private Transform weaponPos;

        [Tooltip("Health bar UI to display the Extra Help Buff's health.")]
        [SerializeField] private Image healthBar;
        [SerializeField] private TextMeshProUGUI healthText;

        [Tooltip("Sound effect to play when the Extra Help Buff dies.")]
        [SerializeField] private AudioClip EH_deathAudioClip;

        private GameObject weaponPrefab;
        private Transform playerTransform;
        private Animator EH_animator;
        private AudioSource audioSource;
        private bool isDead = false;
        private Vector3 previousPosition;
        private int dir;
        private PlayerScript playerScript;

        public int spawnIndex;
        public HelpType helpType;


        private void Awake()
        {
            // Initialize health
            //currentHealth = health;
            currentHealth = GameManager.S.playerScript.currentHealth;
        }

        private void Start()
        {
            weaponPrefab = FindAnyObjectByType<WeaponScript>().GetComponent<GameObject>();
            playerTransform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
            playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>();
            EH_animator = GetComponentInChildren<Animator>();
            audioSource = GetComponent<AudioSource>();
            healthText = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Update()
        {
            // Update Health bar UI
            //healthBar.fillAmount = currentHealth / health;            
            

            // Check for EH death
            if (isDead == false && currentHealth <= 0)
            {
                isDead = true;
                if (currentHealth <= 0) EH_animator.SetBool("EH_dead", isDead);
                audioSource.PlayOneShot(EH_deathAudioClip);
                Destroy(gameObject, 0.1f);                        // Destroy gameobject
            }

            if (helpType == HelpType.Boombardino)
            {
                FollowPlayer();
                healthText.text = "";
            }
            else if (helpType == HelpType.Tungtungtung)
            {
                MoveAndHitEnemy();
                healthText.text = currentHealth.ToString("F0");
            }
        }

        private void FixedUpdate()
        {
            /*
            if(helpType == HelpType.Boombardino)
            {
                FollowPlayer();
            }
            else if(helpType == HelpType.Tungtungtung)
            {
                //여기다가!!
                MoveAndHitEnemy();
            }
            */

            if(helpType == HelpType.Boombardino)
            {
                HandleAnimation();
            }
            
        }

        private void FollowPlayer()
        {             
            float xOffset = 0f;
            float zOffset = 0f;

            if (spawnIndex == 0)
            {
                xOffset = 0.0f;
                zOffset = -1.5f;
            }
            else if(spawnIndex == 1)
            {
                xOffset = -0.5f;
                zOffset = -1.5f;
            }
            else if(spawnIndex == 2)
            {
                xOffset = 0.5f;
                zOffset = -1.5f;
            }
            else if (spawnIndex == 3)
            {
                xOffset = -1.0f;
                zOffset = -1.5f;
            }
            else if (spawnIndex == 4)
            {
                xOffset = 1.0f;
                zOffset = -1.5f;
            }

            // Follow the player while maintaining the offset distance
            Vector3 targetPosition;

            if (playerScript.currentHealth <= 0)
                Destroy(gameObject);

            if (TimeManager.Instance.isForwardMarchScene == true)
            {
                //float zOffset = Random.Range(-1.25f, -3.25f);
                //float xOffset = Random.Range(0.25f, 2.25f);
                targetPosition = new Vector3(playerTransform.position.x + xOffset, 2.0f, playerTransform.position.z + zOffset);
            }
            else
            {
                targetPosition = new Vector3(playerTransform.position.x, transform.position.y, transform.position.z);
            }
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime * TimeManager.timeFactor);

            if (playerScript.nearestEnemy != null)
            {
                Vector3 enemyDir = playerScript.nearestEnemy.position - transform.GetChild(0).position;
                enemyDir.y = 0;
                //Quaternion targetRot = Quaternion.LookRotation(enemyDir);
                //transform.GetChild(0).rotation = Quaternion.Slerp(transform.GetChild(0).rotation, targetRot, 10f * Time.deltaTime * TimeManager.timeFactor);
            }
            else
            {
                //Quaternion targetRot = Quaternion.LookRotation(Vector3.forward);
                //transform.GetChild(0).rotation = Quaternion.Slerp(transform.GetChild(0).rotation, targetRot, 10f * Time.deltaTime * TimeManager.timeFactor);
            }
        }

        private void HandleAnimation()
        {
            if (TimeManager.isGameRunning == true) EH_animator.enabled = true;
            else EH_animator.enabled = false;

            if (TimeManager.Instance.isForwardMarchScene == true) EH_animator.SetBool("WalkFwd", true);

            bool isMoving = Mathf.Abs(transform.position.x - previousPosition.x) > 0.01f;
            EH_animator.SetBool("IsMoving", isMoving);

            if (isMoving == true)
            {
                if (transform.position.x > previousPosition.x) dir = 1;
                else if (transform.position.x < previousPosition.x) dir = -1;

                EH_animator.SetInteger("MoveDirection", dir);
            }

            previousPosition = transform.position;

        }

        private void MoveAndHitEnemy()
        {
            float tf = TimeManager.timeFactor;
            float step = Mathf.Max(0f, followSpeed) * Time.deltaTime * tf;

            // 타겟: playerScript.nearestEnemy 우선, 없으면 주변에서 탐색
            Transform target = (playerScript != null && playerScript.nearestEnemy != null)
                ? playerScript.nearestEnemy
                : FindNearestEnemy(25f); // 반경은 씬에 맞게 조정

            Vector3 targetPos;
            if (target != null)
            {
                targetPos = target.position;
            }
            else if (playerTransform != null)
            {
                targetPos = transform.position + (playerTransform.forward.normalized * 10f);
            }
            else
            {
                targetPos = transform.position + (Vector3.forward * 10f);
            }

            // 수평면 고정
            targetPos.y = transform.position.y;

            // 이동
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

            // 회전(자식 있으면 자식 회전)
            Vector3 dir = targetPos - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Transform body = transform.childCount > 0 ? transform.GetChild(0) : transform;
                Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                body.rotation = Quaternion.Slerp(body.rotation, rot, 10f * Time.deltaTime * tf);
            }
        }

        // ▼ 이동만을 위한 보조(로컬) 함수
        private Transform FindNearestEnemy(float radius)
        {           
            Transform nearest = null; float minSqr = float.MaxValue;
            //var hits = Physics.OverlapSphere(transform.position, radius);
            int mask = LayerMask.GetMask("Enemy"); // Enemy 레이어만
            var hits = Physics.OverlapSphere(transform.position, radius, mask, QueryTriggerInteraction.Collide);

            foreach (var h in hits)
            {
                if (!h.CompareTag("EnemyTag")) continue; // 적 태그 전제
                float sqr = (h.transform.position - transform.position).sqrMagnitude;
                if (sqr < minSqr) { minSqr = sqr; nearest = h.transform; }
            }
            return nearest;
        }


    }
}