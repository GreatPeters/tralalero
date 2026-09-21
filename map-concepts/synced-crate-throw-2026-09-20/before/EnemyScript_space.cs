using System.Collections;
using TMPro;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DisallowMultipleComponent]
    public sealed class EnemyScript_space : MonoBehaviour
    {
        private const float DirectionEpsilonSqr = 0.000001f;
        private static readonly Vector3 DroppedBonusAltarScale = Vector3.one * 3f;
        private static readonly Quaternion DroppedBonusAltarRotation = Quaternion.Euler(0f, 180f, 0f);

        [Header("Noryangjin Enemy")]
        [SerializeField] private EnemySO enemyData;
        [SerializeField] private GameObject bonusWall;
        [SerializeField] private bool dropBonusAltar = true;
        [SerializeField] private int placementCoinReward = -1;
        public void ConfigureRewards(bool dropAltar, int coins = -1)
        {
            dropBonusAltar=dropAltar;placementCoinReward=coins;
        }

        [Header("Throw (Optional)")]
        [SerializeField] private Transform heldProjectile;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private bool hideHeldProjectile;
        [SerializeField, Min(0f)] private float throwRange = 7f;
        [SerializeField, Min(0f)] private float throwSpeed = 12f;
        [SerializeField, Min(0f)] private float throwReleaseDelay = 2f;
        [SerializeField] private bool stationaryThrow;
        [SerializeField, Min(.1f)] private float throwApproachSeconds = 5f;
        [SerializeField, Min(.1f)] private float throwCycleSeconds = 2.8f;
        private float throwCooldown;
        private readonly System.Collections.Generic.List<GameObject> launchedProjectiles = new();
        public bool StationaryThrow => stationaryThrow;
        public Vector3 AuthoredThrowDirection => Vector3.ProjectOnPlane(-transform.forward, Vector3.up).normalized;

        private float _health;
        public float CurrentHealth => _health;
        private float _damage;
        private bool isDead;
        private bool hasThrown;
        private bool rewardPlayerScore;
        private EnemyTier enemyTier;

        private Transform hitPosition;
        private PlayerScript playerScript;
        private Animator enemyAnimator;
        private EnemyEventController eventController;
        private FatManCratePose cratePose;
        private AudioSource audioSource;
        private TextMeshProUGUI healthText;

        private Transform projectileParent;
        private Vector3 projectileLocalPosition;
        private Quaternion projectileLocalRotation;
        private Vector3 projectileLocalScale;

        private void Awake()
        {
            hitPosition = transform.Find("Walker-HitPos");
            if (hitPosition == null && transform.childCount > 1)
                hitPosition = transform.GetChild(1);
            playerScript = FindFirstObjectByType<PlayerScript>();

            audioSource = GetComponent<AudioSource>();
            enemyAnimator = GetComponentInChildren<Animator>();
            eventController = GetComponent<EnemyEventController>();
            cratePose = GetComponent<FatManCratePose>();
            healthText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (Application.isPlaying && enemyData != null)
                EnemyHitEffectPool.Prewarm(enemyData.enemyHitVFX);

            if (heldProjectile == null)
                return;

            projectileParent = heldProjectile.parent;
            projectileLocalPosition = heldProjectile.localPosition;
            projectileLocalRotation = heldProjectile.localRotation;
            projectileLocalScale = heldProjectile.localScale;
        }

        private void OnEnable()
        {
            isDead = false;
            hasThrown = false;
            rewardPlayerScore = true;

            if (enemyAnimator != null)
            {
                enemyAnimator.Rebind();
                enemyAnimator.Update(0f);
                eventController?.SnapToRouteDirection();
            }

            Collider enemyCollider = GetComponent<Collider>();
            if (enemyCollider != null)
                enemyCollider.enabled = true;

            if (healthText != null)
            {
                healthText.enabled = true;
                RefreshHealthText();
            }

            ResetHeldProjectile();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ClearLaunchedProjectiles();
        }

        private void Update()
        {
            if (!TimeManager.isGameRunning ||
                isDead)
                return;

            if (stationaryThrow)
            {
                throwCooldown = Mathf.Max(0f, throwCooldown - Time.deltaTime * Mathf.Max(0f, TimeManager.timeFactor));
                if (throwCooldown <= 0f && hasThrown)
                {
                    hasThrown = false;
                    ResetHeldProjectile();
                }
                if (CanBeginThrow() && IsInApproachWindow())
                {
                    if (eventController != null && eventController.RuntimeState == EnemyEventRuntimeState.Waiting)
                        eventController.ActivateFromSpot();
                    else BeginThrow();
                }
                return;
            }
            if (eventController != null)
                return;

            if (hasThrown || heldProjectile == null || playerScript == null)
                return;

            Vector3 releasePosition = throwPoint != null
                ? throwPoint.position
                : transform.position;
            float throwRangeSqr = throwRange * throwRange;
            if ((releasePosition - playerScript.transform.position).sqrMagnitude <= throwRangeSqr)
                BeginThrow();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDead)
                return;

            if (other.CompareTag("Player"))
            {
                ResolvePlayerContact();
                return;
            }

            if (other.CompareTag("ExtraHelpTag"))
            {
                ResolveExtraHelpContact(other);
                return;
            }

            if (other.CompareTag("BulletTag"))
            {
                ReceiveBulletDamage(other.GetComponentInParent<BulletScript>() ?? other.GetComponentInChildren<BulletScript>());
                return;
            }

            if (other.CompareTag("DestroyerTag"))
                EnemyDeath();
        }

        private void ResolvePlayerContact()
        {
            rewardPlayerScore = false;

            if (playerScript.currentHealth > _health)
            {
                playerScript.currentHealth -= _health;
                _health = 0f;
                RefreshHealthText();
                EnemyDeath();
                return;
            }

            float playerHealth = playerScript.currentHealth;
            _health -= playerHealth;
            playerScript.currentHealth = 0f;
            RefreshHealthText();
        }

        private void ResolveExtraHelpContact(Collider other)
        {
            ExtraHelpBuffScript extraHelp = other.GetComponentInParent<ExtraHelpBuffScript>();
            TryResolveHelperContact(extraHelp);
        }

        // Both physics callbacks and the helper's swept movement use this one transaction.
        public bool TryResolveHelperContact(ExtraHelpBuffScript extraHelp)
        {
            if (isDead || _health <= 0f || extraHelp == null ||
                extraHelp.helpType != HelpType.Tungtungtung || extraHelp.currentHealth <= 0f)
                return false;
            rewardPlayerScore = false;
            ExchangeContactHealth(ref _health, ref extraHelp.currentHealth);
            RefreshHealthText();
            if (_health <= 0f) EnemyDeath();
            return true;
        }

        public static void ExchangeContactHealth(ref float enemyHealth, ref float helperHealth)
        {
            float enemyBefore = Mathf.Max(0f, enemyHealth);
            float helperBefore = Mathf.Max(0f, helperHealth);
            enemyHealth = Mathf.Max(0f, enemyBefore - helperBefore);
            helperHealth = Mathf.Max(0f, helperBefore - enemyBefore);
        }

        private void ReceiveBulletDamage(BulletScript projectile)
        {
            GameAudioService.PlayAt(GameSound.EnemyHit, transform.position);
            EnemyHitEffectPool.Play(enemyData.enemyHitVFX, hitPosition);

            float damage = projectile != null && projectile.HasDamagePayload ? projectile.LaunchDamage : playerScript.currentDamage;
            if (enemyTier == EnemyTier.Boss && UpgradeStatManager.S != null)
            {
                damage = UpgradeStatManager.S.ApplyToBase(
                    UpgradeStatManager.UpgradeType.BOSS_DAMAGE,
                    damage);
            }

            DamagePopupFX.Show(hitPosition.position + Vector3.up * 1.5f, damage);
            _health -= damage;
            if (_health > 0f)
            {
                RefreshHealthText();
                return;
            }

            _health = 0f;
            RefreshHealthText();
            EnemyDeath();
        }

        public void EnemyDeath()
        {
            if (isDead)
                return;

            isDead = true;
            GameAudioService.PlayAt(GameSound.EnemyDeath, transform.position);
            if(dropBonusAltar)SpawnBonusAltar();

            Collider enemyCollider = GetComponent<Collider>();
            if (enemyCollider != null)
                enemyCollider.enabled = false;

            if (eventController != null)
                eventController.PlayDie();
            else
                enemyAnimator.Play(ForwardEnemyAnimationContract.Die, 0, 0f);
            StartCoroutine(DeathFlow());

            if (rewardPlayerScore)
                playerScript.playerScore += enemyData.scoreUponDeath;
            rewardPlayerScore = false;

            int baseCoin=placementCoinReward>=0?placementCoinReward:CoinDropUtility.GetCoinAmount(enemyTier);
            int coinAmount = baseCoin>0?CoinDropUtility.ApplyCoinBonus(baseCoin):0;
            CoinDropUtility.SpawnWorldCoinDrop(transform.position, coinAmount);
        }

        private GameObject SpawnBonusAltar()
        {
            GameObject altar = Instantiate(
                bonusWall,
                transform.position,
                DroppedBonusAltarRotation);
            altar.transform.localScale = DroppedBonusAltarScale;
            if (altar.GetComponent<RuntimeBonusWall>() == null)
                altar.AddComponent<RuntimeBonusWall>();

            return altar;
        }

        private IEnumerator DeathFlow()
        {
            yield return new WaitForSeconds(0.25f);

            if (healthText != null)
                healthText.enabled = false;

            yield return new WaitForSeconds(0.25f);
            gameObject.SetActive(false);
        }

        public void ApplyStat(float damage, float health, EnemyTier tier)
        {
            _damage = damage;
            _health = health;
            enemyTier = tier;

            if (healthText == null)
                healthText = GetComponentInChildren<TextMeshProUGUI>(true);

            RefreshHealthText();
        }

        private void RefreshHealthText()
        {
            if (healthText != null)
                healthText.text = _health.ToString("F0");
        }

        private void ResetHeldProjectile()
        {
            if (heldProjectile == null)
                return;

            heldProjectile.gameObject.SetActive(true);
            heldProjectile.SetParent(projectileParent, false);
            heldProjectile.localPosition = projectileLocalPosition;
            heldProjectile.localRotation = projectileLocalRotation;
            heldProjectile.localScale = projectileLocalScale;

            Rigidbody rigidbody = heldProjectile.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }

            // A carried prop must not collide with its owner, the road or the player.
            foreach (Collider collider in heldProjectile.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            bool showHeldProp = !hideHeldProjectile && GetComponent<EnemyGunAim>() == null;
            foreach (Renderer renderer in heldProjectile.GetComponentsInChildren<Renderer>(true))
                if (!(renderer is TrailRenderer)) renderer.enabled = showHeldProp;
            heldProjectile.GetComponent<SimpleProjectile>()?.SetFlightActive(false);
            cratePose?.ResetCarry();
        }

        private void BeginThrow()
        {
            hasThrown = true;
            cratePose?.BeginWindup(throwReleaseDelay);
            throwCooldown = Mathf.Max(throwCycleSeconds, throwReleaseDelay + .5f);
            if (eventController != null)
                eventController.PlayAttackOnce();
            else
                enemyAnimator.Play(
                    ForwardEnemyAnimationContract.AttackOnce,
                    0,
                    0f);
            if (Application.isPlaying)
                StartCoroutine(ReleaseProjectileAfterDelay());
        }

        public bool TryBeginTriggeredFire()
        {
            if (!CanBeginThrow() || (stationaryThrow && !IsInApproachWindow()))
                return false;

            BeginThrow();
            return true;
        }

        public bool CanBeginTriggeredFire => CanBeginThrow();

        public bool HasConfiguredProjectile => heldProjectile != null;

        public void ConfigureTriggeredFire(float releaseDelay, float speed)
        {
            throwReleaseDelay = Mathf.Max(0f, releaseDelay);
            throwSpeed = Mathf.Max(0f, speed);
        }

        internal void ResetTriggeredFireForNewRun()
        {
            StopAllCoroutines();
            ClearLaunchedProjectiles();
            hasThrown = false;
            throwCooldown = 0f;
            ResetHeldProjectile();
        }

        public bool IsInApproachWindow()
        {
            if (playerScript == null) return false;
            return IsWithinApproachWindow(playerScript.transform.position, playerScript.transform.forward,
                transform.position, playerScript.ForwardMoveSpeed, throwApproachSeconds, 6f);
        }

        public static bool IsWithinApproachWindow(Vector3 playerPosition, Vector3 playerForward,
            Vector3 position, float moveSpeed, float seconds, float lateralLimit)
        {
            Vector3 forward = Vector3.ProjectOnPlane(playerForward, Vector3.up).normalized;
            Vector3 delta = position - playerPosition;
            float ahead = Vector3.Dot(delta, forward);
            float lateral = Mathf.Abs(Vector3.Dot(delta, Vector3.Cross(Vector3.up, forward)));
            return moveSpeed > 0f && ahead >= 0f && ahead <= moveSpeed * seconds &&
                lateral <= lateralLimit && Mathf.Abs(delta.y) < 3f;
        }

        private void ClearLaunchedProjectiles()
        {
            foreach (var projectile in launchedProjectiles) if (projectile != null) Destroy(projectile);
            launchedProjectiles.Clear();
        }

        private bool CanBeginThrow()
        {
            return !isDead &&
                   !hasThrown &&
                   heldProjectile != null &&
                   playerScript != null &&
                   enemyAnimator != null;
        }

        private IEnumerator ReleaseProjectileAfterDelay()
        {
            float remainingDelay = Mathf.Max(0f, throwReleaseDelay);
            while (remainingDelay > 0f)
            {
                yield return null;
                if (isDead || heldProjectile == null)
                    yield break;
                if (!TimeManager.isGameRunning)
                    continue;

                remainingDelay -=
                    Time.deltaTime * Mathf.Max(0f, TimeManager.timeFactor);
            }

            if (isDead || heldProjectile == null || !TimeManager.isGameRunning)
                yield break;

            GetComponent<EnemyGroundedPose>()?.Settle();
            GetComponent<EnemyGunAim>()?.AimAtPlayer();
            cratePose?.PrepareRelease();
            Transform releaseTransform = throwPoint != null ? throwPoint : heldProjectile;
            Vector3 releasePosition = releaseTransform.position;
            // Detached throw props keep the hand's real release height. A low keyframe
            // must not launch a large crate through the road surface.
            if (throwPoint == heldProjectile)
                releasePosition.y = Mathf.Max(releasePosition.y, transform.position.y + .9f);
            Vector3 throwDirection = stationaryThrow ? AuthoredThrowDirection : CalculateThrowDirection(
                releasePosition,
                GetPlayerAimPoint(),
                -releaseTransform.forward);

            GameAudioService.PlayAt(GameSound.Throw, releasePosition);
            Quaternion launchRotation = cratePose != null ? heldProjectile.rotation : BuildThrownProjectileRotation(throwDirection);
            var projectile = Instantiate(heldProjectile.gameObject, releasePosition, launchRotation);
            projectile.name = heldProjectile.name + "_Shot";
            projectile.transform.localScale = heldProjectile.lossyScale;
            launchedProjectiles.RemoveAll(p => p == null);
            launchedProjectiles.Add(projectile);
            foreach (Renderer renderer in heldProjectile.GetComponentsInChildren<Renderer>(true))
                if (!(renderer is TrailRenderer)) renderer.enabled = false;
            cratePose?.Release();
            foreach (Renderer renderer in projectile.GetComponentsInChildren<Renderer>(true))
                if (!(renderer is TrailRenderer)) renderer.enabled = true;

            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider == null)
                projectileCollider = projectile.AddComponent<SphereCollider>();
            projectileCollider.isTrigger = true;

            foreach (Collider enemyCollider in GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(projectileCollider, enemyCollider, true);

            var flight = projectile.GetComponent<SimpleProjectile>() ?? projectile.AddComponent<SimpleProjectile>();
            flight.Launch(throwDirection, throwSpeed, _damage, 8f);
        }

        private Vector3 GetPlayerAimPoint()
        {
            if (playerScript == null)
            {
                Transform releaseTransform = throwPoint != null ? throwPoint : transform;
                return releaseTransform.position - releaseTransform.forward;
            }

            Collider playerCollider = playerScript.GetComponent<Collider>();

            return playerCollider != null
                ? playerCollider.bounds.center
                : playerScript.transform.position;
        }

        private static Vector3 CalculateThrowDirection(
            Vector3 releasePosition,
            Vector3 targetPosition,
            Vector3 fallbackDirection)
        {
            Vector3 direction = targetPosition - releasePosition;
            if (direction.sqrMagnitude > DirectionEpsilonSqr)
                return direction.normalized;

            if (fallbackDirection.sqrMagnitude > DirectionEpsilonSqr)
                return fallbackDirection.normalized;

            return Vector3.forward;
        }

        private static Quaternion BuildThrownProjectileRotation(Vector3 direction)
        {
            Vector3 normalizedDirection = direction.sqrMagnitude > DirectionEpsilonSqr
                ? direction.normalized
                : Vector3.forward;
            Vector3 up = Mathf.Abs(Vector3.Dot(normalizedDirection, Vector3.up)) > 0.999f
                ? Vector3.forward
                : Vector3.up;

            return Quaternion.LookRotation(normalizedDirection, up)
                * Quaternion.Euler(0f, -90f, 0f);
        }
    }
}
