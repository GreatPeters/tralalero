using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class BulletScript : MonoBehaviour
    {
        BulletPooler bulletPooler;
        Transform projectileRoot;
        Vector3 direction;
        float elapsedDuration;

        private const float FallbackMissileSpeed = 16f;
        private const float FallbackMissileDuration = 1f;
        private const float MinimumMissileDuration = 0.01f;
        private static float baseMissileSpeed = FallbackMissileSpeed;
        private static float baseMissileDuration = FallbackMissileDuration;
        private static float upgradeDurationFlatBonus;
        private static float upgradeDurationPercentBonus;
        private static float runDurationPercentBonus;

        public static float BaseMissileSpeed => baseMissileSpeed;
        public static float BaseMissileDuration => baseMissileDuration;
        public static float CurrentMissileDuration => Mathf.Max(
            MinimumMissileDuration,
            baseMissileDuration *
            (1f + (upgradeDurationPercentBonus + runDurationPercentBonus) / 100f) +
            upgradeDurationFlatBonus);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            baseMissileSpeed = FallbackMissileSpeed;
            baseMissileDuration = FallbackMissileDuration;
            upgradeDurationFlatBonus = 0f;
            upgradeDurationPercentBonus = 0f;
            runDurationPercentBonus = 0f;
        }

        private void Start()
        {
            bulletPooler = FindFirstObjectByType<BulletPooler>();
        }

        private void FixedUpdate()
        {
            float remainingDuration = Mathf.Max(0f, CurrentMissileDuration - elapsedDuration);
            float deltaSeconds = Mathf.Min(GetSimulationDeltaTime(), remainingDuration);
            Transform movingTransform = GetProjectileTransform();
            movingTransform.position += direction * baseMissileSpeed * deltaSeconds;
            AdvanceLifetime(deltaSeconds);
        }

        public static void ConfigureMissileDefaults(float missileSpeed, float missileDuration)
        {
            baseMissileSpeed = Mathf.Max(0.01f, missileSpeed);
            baseMissileDuration = Mathf.Max(MinimumMissileDuration, missileDuration);
        }

        public void SetDirection(Vector3 dir)
        {
            direction = dir;
            projectileRoot = transform.root;
            elapsedDuration = 0f;
        }

        private static float GetSimulationDeltaTime()
        {
            bool isForwardMarch =
                TimeManager.Instance != null &&
                TimeManager.Instance.isForwardMarchScene;
            float timeScale = isForwardMarch
                ? Mathf.Max(0f, TimeManager.timeFactor)
                : 1f;
            return Time.fixedDeltaTime * timeScale;
        }

        private void AdvanceLifetime(float deltaSeconds)
        {
            elapsedDuration += Mathf.Max(0f, deltaSeconds);
            if (elapsedDuration < CurrentMissileDuration)
                return;

            ReturnToPool();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("EnemyTag") ||
                other.CompareTag("BarrelTag") ||
                other.CompareTag("Obstacle"))
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            bulletPooler.ReturnObjectToPool_Bullet(GetProjectileTransform().gameObject);
        }

        private Transform GetProjectileTransform()
        {
            return projectileRoot != null ? projectileRoot : transform;
        }

        // Reset only temporary in-run duration bonuses. Permanent upgrades remain applied.
        public static void ResetStatBonus()
        {
            runDurationPercentBonus = 0f;
        }

        public static void AddMissileDurationPercent(float percentValue)
        {
            runDurationPercentBonus += percentValue;
        }

        public static void ApplyMissileDurationUpgrade(float flatValue, float percentValue)
        {
            upgradeDurationFlatBonus = flatValue;
            upgradeDurationPercentBonus = percentValue;
        }
    }
}
