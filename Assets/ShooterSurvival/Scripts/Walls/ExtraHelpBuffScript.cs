using TMPro;
using UnityEngine;

public enum HelpType
{
    Boombardino,
    Tungtungtung
}

namespace IndianOceanAssets.ShooterSurvival
{
    public class ExtraHelpBuffScript : MonoBehaviour
    {
        private const float TungtungMoveSpeedMultiplier = 1.06f;
        private const int InitialEnemySearchCapacity = 32;
        private int enemyLayerMask;

        [Header("Runtime")]
        [Tooltip("Current health of the Extra Help Buff.")]
        [System.NonSerialized] public float currentHealth;

        [Header("ExtraHelp Buff Params")]
        [Tooltip("Maximum health of the Extra Help Buff.")]
        [SerializeField] private float health = 100f;

        [Tooltip("Speed at which the Extra Help Buff follows the player.")]
        [SerializeField] private float followSpeed;

        [Header("Dependencies")]
        private TextMeshProUGUI healthText;

        [Tooltip("Sound effect to play when the Extra Help Buff dies.")]
        [SerializeField] private AudioClip EH_deathAudioClip;

        private Transform playerTransform;
        private Animator EH_animator;
        private AudioSource audioSource;
        private bool isDead = false;
        private Vector3 previousPosition;
        private PlayerScript playerScript;
        private bool hasDeadParameter;
        private bool hasWalkForwardParameter;
        private bool hasMovingParameter;
        private bool hasMoveDirectionParameter;

        private float lastDisplayedHealth = float.NaN;
        private Collider[] enemySearchBuffer;

        [System.NonSerialized] public int spawnIndex;
        [System.NonSerialized] public HelpType helpType;


        private void Awake()
        {
            enemyLayerMask = LayerMask.GetMask("Enemy");
            PlayerScript owner = GameManager.S != null
                ? GameManager.S.playerScript
                : FindFirstObjectByType<PlayerScript>();
            currentHealth = owner != null ? owner.currentHealth : health;
        }

        private void Start()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                enabled = false;
                return;
            }

            playerTransform = playerObject.transform;
            playerScript = playerObject.GetComponent<PlayerScript>();
            foreach (Animator candidate in GetComponentsInChildren<Animator>(true))
            {
                if (candidate.runtimeAnimatorController == null)
                    continue;

                if (HasAnimatorParameter(candidate, "WalkFwd") ||
                    HasAnimatorParameter(candidate, "IsMoving"))
                {
                    EH_animator = candidate;
                    break;
                }
            }

            CacheAnimatorParameters();
            audioSource = GetComponent<AudioSource>();
            healthText = GetComponentInChildren<TextMeshProUGUI>();

            ApplyUpgradeStats();
        }


        private void ApplyUpgradeStats()
        {
            if (playerScript == null || UpgradeStatManager.S == null) return;

            if (helpType == HelpType.Tungtungtung)
            {
                float value = UpgradeStatManager.S.GetStat(UpgradeStatManager.UpgradeType.TUNGTUNGTUNG);
                var vt = UpgradeStatManager.S.GetValueType(UpgradeStatManager.UpgradeType.TUNGTUNGTUNG);
                currentHealth = vt == ValueType.Percent
                    ? playerScript.currentHealth * (value / 100f)
                    : playerScript.currentHealth + value;
            }
            else if (helpType == HelpType.Boombardino)
            {
                float value = UpgradeStatManager.S.GetStat(UpgradeStatManager.UpgradeType.BOOMBAR);
                var vt = UpgradeStatManager.S.GetValueType(UpgradeStatManager.UpgradeType.BOOMBAR);

                var weapon = GetComponentInChildren<WeaponScript>();
                if (weapon != null)
                {
                    float baseDamage = playerScript.currentDamage;
                    weapon.damage = vt == ValueType.Percent
                        ? baseDamage * (value / 100f)
                        : baseDamage + value;
                }
            }
        }

        private void Update()
        {
            if (!isDead && currentHealth <= 0f)
            {
                isDead = true;
                if (hasDeadParameter)
                    EH_animator.SetBool("EH_dead", isDead);
                if (audioSource != null && EH_deathAudioClip != null)
                    audioSource.PlayOneShot(EH_deathAudioClip);
                Destroy(gameObject, 0.1f);
                return;
            }

            if (helpType == HelpType.Boombardino)
            {
                FollowPlayer();
                HandleAnimation();
                if (healthText != null && healthText.text.Length > 0)
                    healthText.text = string.Empty;
            }
            else if (helpType == HelpType.Tungtungtung)
            {
                MoveAndHitEnemy();
                if (healthText != null &&
                    !Mathf.Approximately(lastDisplayedHealth, currentHealth))
                {
                    healthText.text = currentHealth.ToString("F0");
                    lastDisplayedHealth = currentHealth;
                }
            }
        }

        private void FollowPlayer()
        {
            Vector3 targetPosition;

            if (playerScript.currentHealth <= 0)
            {
                Destroy(gameObject);
                return;
            }

            if (TimeManager.Instance.isForwardMarchScene == true)
            {
                Vector3 offset = spawnIndex switch
                {
                    0 => new Vector3(0f, 0f, -1.5f),
                    1 => new Vector3(-0.5f, 0f, -1.5f),
                    2 => new Vector3(0.5f, 0f, -1.5f),
                    3 => new Vector3(-1f, 0f, -1.5f),
                    4 => new Vector3(1f, 0f, -1.5f),
                    _ => Vector3.zero
                };
                Vector3 routeOffset =
                    playerTransform.right * offset.x +
                    playerTransform.forward * offset.z;
                targetPosition = playerTransform.position + routeOffset;
                targetPosition.y = 2f;

                Vector3 routeForward = Vector3.ProjectOnPlane(
                    playerTransform.forward,
                    Vector3.up);
                if (routeForward.sqrMagnitude > 0.0001f)
                {
                    Quaternion routeRotation = Quaternion.LookRotation(
                        routeForward.normalized,
                        Vector3.up);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        routeRotation,
                        10f * Time.deltaTime * TimeManager.timeFactor);
                }
            }
            else
            {
                targetPosition = new Vector3(playerTransform.position.x, transform.position.y, transform.position.z);
            }
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime * TimeManager.timeFactor);
        }

        private void HandleAnimation()
        {
            if (EH_animator == null || EH_animator.runtimeAnimatorController == null)
                return;

            EH_animator.enabled = TimeManager.isGameRunning;

            if (TimeManager.Instance.isForwardMarchScene == true && hasWalkForwardParameter)
                EH_animator.SetBool("WalkFwd", true);

            Vector3 positionDelta = transform.position - previousPosition;
            float lateralDelta = playerTransform != null
                ? Vector3.Dot(positionDelta, playerTransform.right)
                : positionDelta.x;
            bool isMoving = Mathf.Abs(lateralDelta) > 0.01f;
            if (hasMovingParameter)
                EH_animator.SetBool("IsMoving", isMoving);

            if (isMoving == true)
            {
                if (hasMoveDirectionParameter)
                {
                    EH_animator.SetInteger(
                        "MoveDirection",
                        lateralDelta > 0f ? 1 : -1);
                }
            }

            previousPosition = transform.position;

        }

        private void CacheAnimatorParameters()
        {
            hasDeadParameter = HasAnimatorParameter(EH_animator, "EH_dead");
            hasWalkForwardParameter = HasAnimatorParameter(EH_animator, "WalkFwd");
            hasMovingParameter = HasAnimatorParameter(EH_animator, "IsMoving");
            hasMoveDirectionParameter = HasAnimatorParameter(EH_animator, "MoveDirection");
        }

        private static bool HasAnimatorParameter(Animator animator, string parameterName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == parameterName)
                    return true;
            }

            return false;
        }

        private void MoveAndHitEnemy()
        {
            float tf = TimeManager.timeFactor;
            float baseSpeed = playerScript != null
                ? playerScript.ForwardMoveSpeed * TungtungMoveSpeedMultiplier
                : followSpeed;

            float step = Mathf.Max(0f, baseSpeed) * Time.deltaTime * tf;

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
            enemySearchBuffer ??= new Collider[InitialEnemySearchCapacity];

            int hitCount;
            while (true)
            {
                hitCount = Physics.OverlapSphereNonAlloc(
                    transform.position,
                    radius,
                    enemySearchBuffer,
                    enemyLayerMask,
                    QueryTriggerInteraction.Collide);

                if (hitCount < enemySearchBuffer.Length)
                    break;

                enemySearchBuffer = new Collider[enemySearchBuffer.Length * 2];
            }

            Transform nearest = null;
            float minSqr = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = enemySearchBuffer[i];
                if (hit == null || !hit.CompareTag("EnemyTag"))
                    continue;

                float sqr = (hit.transform.position - transform.position).sqrMagnitude;
                if (sqr >= minSqr)
                    continue;

                minSqr = sqr;
                nearest = hit.transform;
            }

            return nearest;
        }


    }
}

