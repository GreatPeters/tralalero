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

        [Header("Throw (Optional)")]
        [SerializeField] private Transform heldProjectile;
        [SerializeField] private Transform throwPoint;
        [SerializeField, Min(0f)] private float throwRange = 7f;
        [SerializeField, Min(0f)] private float throwSpeed = 12f;
        [SerializeField, Min(0f)] private float throwReleaseDelay = 2f;

        private float _health;
        private float _damage;
        private bool isDead;
        private bool hasThrown;
        private bool rewardPlayerScore;
        private EnemyTier enemyTier;

        private Transform hitPosition;
        private PlayerScript playerScript;
        private Animator enemyAnimator;
        private EnemyEventController eventController;
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
            healthText = GetComponentInChildren<TextMeshProUGUI>(true);

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
        }

        private void Update()
        {
            if (!TimeManager.isGameRunning ||
                isDead)
                return;

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
                ReceiveBulletDamage();
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
            ExtraHelpBuffScript extraHelp = other.GetComponent<ExtraHelpBuffScript>();
            rewardPlayerScore = false;
            if (extraHelp.currentHealth > _health)
            {
                extraHelp.currentHealth -= _health;
                _health = 0f;
                RefreshHealthText();
                EnemyDeath();
                return;
            }

            float extraHelpHealth = extraHelp.currentHealth;
            _health -= extraHelpHealth;
            extraHelp.currentHealth = 0f;
            RefreshHealthText();
        }

        private void ReceiveBulletDamage()
        {
            GameObject hitEffect = Instantiate(enemyData.enemyHitVFX, hitPosition);
            ParticleSystem particles = hitEffect.GetComponent<ParticleSystem>();
            Destroy(hitEffect, particles.main.duration);

            float damage = playerScript.currentDamage;
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
            audioSource.PlayOneShot(enemyData.enemyDeathSound);
            EnemyDeath();
        }

        public void EnemyDeath()
        {
            if (isDead)
                return;

            isDead = true;
            SpawnBonusAltar();

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

            int coinAmount = CoinDropUtility.ApplyCoinBonus(
                CoinDropUtility.GetCoinAmount(enemyTier));
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

            Collider projectileCollider = heldProjectile.GetComponent<Collider>();
            if (projectileCollider != null)
                projectileCollider.isTrigger = false;

            TrailRenderer trail = heldProjectile.GetComponentInChildren<TrailRenderer>(true);
            if (trail == null)
                return;

            trail.emitting = false;
            trail.Clear();
            trail.emitting = true;
        }

        private void BeginThrow()
        {
            hasThrown = true;
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
            if (!CanBeginThrow())
                return false;

            BeginThrow();
            return true;
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

            Transform releaseTransform = throwPoint != null ? throwPoint : transform;
            Vector3 releasePosition = releaseTransform.position;
            Vector3 throwDirection = CalculateThrowDirection(
                releasePosition,
                GetPlayerAimPoint(),
                -releaseTransform.forward);

            heldProjectile.position = releasePosition;
            heldProjectile.SetParent(null, true);
            heldProjectile.rotation = BuildThrownProjectileRotation(throwDirection);

            Collider projectileCollider = heldProjectile.GetComponent<Collider>();
            if (projectileCollider == null)
                projectileCollider = heldProjectile.gameObject.AddComponent<SphereCollider>();
            projectileCollider.isTrigger = true;
            heldProjectile.GetComponent<SimpleProjectile>().damage = _damage;

            Rigidbody rigidbody = heldProjectile.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = heldProjectile.gameObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = false;
            rigidbody.useGravity = false;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            foreach (Collider enemyCollider in GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(projectileCollider, enemyCollider, true);

            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.linearVelocity = throwDirection * throwSpeed;
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
