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
        private Collider contactCollider;
        private Collider[] contactOverlaps = new Collider[32];
        private RaycastHit[] contactHits = new RaycastHit[32];

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
        public PlayerScript Owner => playerScript;
        private bool hasDeadParameter;
        private bool hasWalkForwardParameter;
        private bool hasMovingParameter;
        private bool hasMoveDirectionParameter;

        private float lastDisplayedHealth = float.NaN;
        private Collider[] enemySearchBuffer;
        private NoryangjinHelperRouteFollower routeFollower;

        public void ConfigureOwner(PlayerScript owner)
        {
            playerScript = owner;
            playerTransform = owner != null ? owner.transform : null;
            if (helpType == HelpType.Tungtungtung && owner != null && owner.GetComponent<NoryangjinRoadHeightFollower>() != null)
            {
                routeFollower = GetComponent<NoryangjinHelperRouteFollower>();
                if (routeFollower == null) routeFollower = gameObject.AddComponent<NoryangjinHelperRouteFollower>();
                routeFollower.Configure(owner);
            }
        }

        private void OnDestroy()
        {
            if (playerScript != null)
                playerScript.extraHelpWeaponScript?.Remove(GetComponentInChildren<WeaponScript>());
        }

        [System.NonSerialized] public int spawnIndex;
        [System.NonSerialized] public HelpType helpType;


        private void Awake()
        {
            // Authored forward enemies still use Default; pooled enemies use Enemy.
            enemyLayerMask = LayerMask.GetMask("Enemy", "Default");
            PlayerScript owner = GameManager.S != null
                ? GameManager.S.playerScript
                : FindFirstObjectByType<PlayerScript>();
            currentHealth = owner != null ? owner.currentHealth : health;
        }

        private void Start()
        {
            GameObject playerObject = playerScript != null ? playerScript.gameObject : GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                enabled = false;
                return;
            }

            playerTransform = playerObject.transform;
            playerScript = playerObject.GetComponent<PlayerScript>();
            ConfigureOwner(playerScript);
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
                if (value <= 0f) return; // A bonus helper still works before the permanent upgrade is purchased.
                var vt = UpgradeStatManager.S.GetValueType(UpgradeStatManager.UpgradeType.TUNGTUNGTUNG);
                currentHealth = vt == ValueType.Percent
                    ? playerScript.currentHealth * (value / 100f)
                    : playerScript.currentHealth + value;
            }
            else if (helpType == HelpType.Boombardino)
            {
                var weapon = GetComponentInChildren<WeaponScript>();
                if (weapon != null) weapon.damage = ResolveProjectileDamage();
            }
        }

        public float ResolveProjectileDamage()
        {
            if (playerScript == null) playerScript = FindFirstObjectByType<PlayerScript>();
            if (playerScript == null) return 0f;
            float value = UpgradeStatManager.S != null ? UpgradeStatManager.S.GetStat(UpgradeStatManager.UpgradeType.BOOMBAR) : 0f;
            ValueType type = UpgradeStatManager.S != null ? UpgradeStatManager.S.GetValueType(UpgradeStatManager.UpgradeType.BOOMBAR) : ValueType.Percent;
            return CalculateProjectileDamage(playerScript.ResolvedAttackDamage, value, type);
        }

        public static float CalculateProjectileDamage(float playerDamage, float upgradeValue, ValueType type)
        {
            if (upgradeValue <= 0f) return playerDamage;
            return type == ValueType.Percent ? playerDamage * upgradeValue / 100f : playerDamage + upgradeValue;
        }

        private void Update()
        {
            if (playerScript == null || playerScript.currentHealth <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            if (EH_animator != null) EH_animator.enabled = TimeManager.isGameRunning;
            if (!TimeManager.isGameRunning)
                return;
            if (!isDead && currentHealth <= 0f)
            {
                isDead = true;
                if (hasDeadParameter)
                    EH_animator.SetBool("EH_dead", isDead);
                GameAudioService.PlayAt(GameSound.EnemyDeath, transform.position);
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
                targetPosition.y = playerTransform.position.y + 2f;

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
            Vector3 before = transform.position;
            float tf = TimeManager.timeFactor;
            float baseSpeed = playerScript != null
                ? playerScript.ForwardMoveSpeed * TungtungMoveSpeedMultiplier
                : followSpeed;

            float step = Mathf.Max(0f, baseSpeed) * Time.deltaTime * tf;
            if (routeFollower != null)
            {
                routeFollower.Advance(step);
                ResolveContactsAlongMove(before, transform.position);
                return; // Never chase across water, a crossing deck or an unvisited corner.
            }

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
            ResolveContactsAlongMove(before, transform.position);

            // 회전(자식 있으면 자식 회전)
            Vector3 dir = targetPos - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Transform body = transform.childCount > 0 ? transform.GetChild(0) : transform;
                Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                body.rotation = Quaternion.Slerp(body.rotation, rot, 10f * Time.deltaTime * tf);
            }
        }

        public void ResolveContactsAlongMove(Vector3 from, Vector3 to)
        {
            if (helpType != HelpType.Tungtungtung || currentHealth <= 0f) return;
            if (contactCollider == null) contactCollider = GetComponent<Collider>();
            if (contactCollider == null || !contactCollider.enabled) return;
            Physics.SyncTransforms();
            Bounds bounds = contactCollider.bounds;
            float radius = Mathf.Max(.05f, Mathf.Min(bounds.extents.x, bounds.extents.z));
            Vector3 center = bounds.center + from - to;
            Vector3 vertical = Vector3.up * Mathf.Max(0f, bounds.extents.y - radius);
            if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Enemy", "Default");
            int mask = enemyLayerMask;
            int count;
            while ((count = Physics.OverlapCapsuleNonAlloc(center - vertical, center + vertical,
                       radius, contactOverlaps, mask, QueryTriggerInteraction.Collide)) == contactOverlaps.Length)
                System.Array.Resize(ref contactOverlaps, contactOverlaps.Length * 2);
            for (int i = 0; i < count && currentHealth > 0f; i++)
                Contact(contactOverlaps[i]);
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .000001f || currentHealth <= 0f) return;
            while ((count = Physics.CapsuleCastNonAlloc(center - vertical, center + vertical, radius,
                       delta.normalized, contactHits, delta.magnitude, mask,
                       QueryTriggerInteraction.Collide)) == contactHits.Length)
                System.Array.Resize(ref contactHits, contactHits.Length * 2);
            // Resolve the nearest enemy first, so a stronger enemy stops this helper.
            System.Array.Sort(contactHits, 0, count, ContactDistanceComparer.Instance);
            for (int i = 0; i < count && currentHealth > 0f; i++)
                if (Contact(contactHits[i].collider) && currentHealth <= 0f)
                    transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(contactHits[i].distance / delta.magnitude));
        }

        private bool Contact(Collider other) => other != null &&
            other.GetComponentInParent<EnemyScript_space>() is { } enemy && enemy.TryResolveHelperContact(this);

        private sealed class ContactDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly ContactDistanceComparer Instance = new();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
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

