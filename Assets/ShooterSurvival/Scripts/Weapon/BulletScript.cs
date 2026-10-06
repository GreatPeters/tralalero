using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class BulletScript : MonoBehaviour
    {
        BulletPooler bulletPooler;
        Transform projectileRoot;
        PlayerScript routeOwner;
        Vector3 direction;
        float elapsedDuration;
        bool returnedToPool;
        HighwayProjectilePath roadFlight;
        Chapter45Director chapterOwner;
        int launchDeck = -1;
        float launchFloorHeight;
        bool chapterRetired;
        bool checkChapterEnemyOverlap;
        private static Collider[] chapterOverlapColliders = new Collider[16];
        public float LaunchDamage { get; private set; }
        public bool HasDamagePayload { get; private set; }
        public BulletKind ProjectileKind { get; set; }

        private static readonly HashSet<BulletScript> ActiveProjectiles = new();

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
            ActiveProjectiles.Clear();
            System.Array.Clear(chapterOverlapColliders, 0, chapterOverlapColliders.Length);
        }

        private void OnEnable()
        {
            returnedToPool = false;
            ActiveProjectiles.Add(this);
        }

        private void OnDisable()
        {
            // Retain damage until the next launch so the paired physics callback
            // can read it even if this callback returned the object first.
            returnedToPool = true;
            ActiveProjectiles.Remove(this);
            routeOwner = null;
            projectileRoot = null;
            roadFlight = default;
        }

        private void Start()
        {
            bulletPooler = FindFirstObjectByType<BulletPooler>();
        }

        private void FixedUpdate()
        {
            if (returnedToPool) return;
            float remainingDuration = Mathf.Max(0f, CurrentMissileDuration - elapsedDuration);
            float deltaSeconds = Mathf.Min(GetSimulationDeltaTime(), remainingDuration);
            Transform movingTransform = GetProjectileTransform();
            if (deltaSeconds <= 0f) checkChapterEnemyOverlap = true;
            if (deltaSeconds > 0f)
            {
                if (HitOverlappingChapterTarget(movingTransform.position)) return;
                if (checkChapterEnemyOverlap)
                {
                    checkChapterEnemyOverlap = false;
                    if (HitOverlappingChapterEnemy(movingTransform.position)) return;
                }
            }
            if (roadFlight.IsActive)
            {
                movingTransform.position = roadFlight.Advance(baseMissileSpeed * deltaSeconds, out var rotation);
                movingTransform.rotation = rotation * movingTransform.rotation;
            }
            else movingTransform.position += direction * baseMissileSpeed * deltaSeconds;
            AdvanceLifetime(deltaSeconds);
        }

        // A late lane change can place the mouth inside a still-closed panel.
        // Resolve that existing overlap before the first flight step skips its
        // thin trigger. Normal target eligibility, damage and pooling still apply.
        private bool HitOverlappingChapterTarget(Vector3 position)
        {
            if (chapterOwner == null || !TimeManager.isGameRunning || TimeManager.timeFactor <= 0f)
                return false;
            foreach (var target in chapterOwner.targets)
            {
                if (target == null || !target.Alive || !target.Eligible || !CanHitTarget(target.transform)) continue;
                var hit = target.hitCollider;
                if (hit == null || !hit.enabled || !hit.gameObject.activeInHierarchy) continue;
                if ((hit.ClosestPoint(position) - position).sqrMagnitude > .00000001f) continue;
                if (!target.ReceiveProjectile(this)) continue;
                ReturnToPool();
                return true;
            }
            return false;
        }

        // Check only the initial/resumed flight step. Native trigger callbacks
        // can otherwise miss an enemy already containing this fast projectile.
        private bool HitOverlappingChapterEnemy(Vector3 position)
        {
            if (chapterOwner == null || !TimeManager.isGameRunning || TimeManager.timeFactor <= 0f)
                return false;
            int count;
            while ((count = Physics.OverlapSphereNonAlloc(position, .0001f,
                chapterOverlapColliders, ~0, QueryTriggerInteraction.Collide)) == chapterOverlapColliders.Length)
                System.Array.Resize(ref chapterOverlapColliders, chapterOverlapColliders.Length * 2);
            for (int i = 0; i < count; i++)
            {
                var hit = chapterOverlapColliders[i];
                if (hit == null || !hit.enabled || !hit.gameObject.activeInHierarchy ||
                    (hit.ClosestPoint(position) - position).sqrMagnitude > .00000001f) continue;
                var enemy = hit.GetComponentInParent<EnemyScript_space>();
                if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead || !CanHitTarget(enemy.transform)) continue;
                enemy.ReceiveBulletDamage(this);
                ReturnToPool();
                return true;
            }
            return false;
        }

        public static void ConfigureMissileDefaults(float missileSpeed, float missileDuration)
        {
            baseMissileSpeed = Mathf.Max(0.01f, missileSpeed);
            baseMissileDuration = Mathf.Max(MinimumMissileDuration, missileDuration);
        }

        public void SetDirection(Vector3 dir)
        {
            SetDirection(dir, null);
        }

        public void SetDirection(Vector3 dir, PlayerScript owner)
        {
            ProjectileKind = BulletKind.Water;
            HasDamagePayload = owner != null;
            LaunchDamage = owner != null ? owner.ResolvedAttackDamage : 0f;
            direction = dir;
            projectileRoot = transform.root;
            routeOwner = owner;
            chapterOwner = Chapter45Director.For(owner);
            launchDeck = chapterOwner != null ? chapterOwner.CurrentFloor : -1;
            launchFloorHeight = owner != null ? owner.transform.position.y : 0;
            chapterRetired = false;
            checkChapterEnemyOverlap = true;
            var road = owner != null ? owner.ProjectileRoute : null;
            roadFlight = road != null && road.isActiveAndEnabled && road.centers.Length > 1
                ? new HighwayProjectilePath(road, road.Distance, road.OnBypass, projectileRoot.position, dir)
                : default;
            elapsedDuration = 0f;
            returnedToPool = false;
            ActiveProjectiles.Add(this);
        }

        public void SetDirection(Vector3 dir, PlayerScript owner, float damage)
        {
            SetDirection(dir, owner);
            HasDamagePayload = true;
            LaunchDamage = float.IsNaN(damage) || float.IsInfinity(damage) ? 0f : Mathf.Max(0f, damage);
        }

        internal static void ApplyRouteTurn(
            PlayerScript owner,
            Quaternion rotationDelta)
        {
            if (owner == null || Quaternion.Angle(Quaternion.identity, rotationDelta) <= 0.001f)
                return;

            foreach (BulletScript projectile in ActiveProjectiles)
            {
                if (projectile == null ||
                    !projectile.isActiveAndEnabled ||
                    projectile.routeOwner != owner)
                {
                    continue;
                }

                if (projectile.roadFlight.IsActive) continue;

                projectile.direction = rotationDelta * projectile.direction;
                Transform movingTransform = projectile.GetProjectileTransform();
                movingTransform.rotation = rotationDelta * movingTransform.rotation;
            }
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
            if (returnedToPool || !CanHitTarget(other.transform)) return;
            var laterTarget = other.GetComponentInParent<Chapter45Target>();
            if (laterTarget != null && laterTarget.ReceiveProjectile(this))
            {
                ReturnToPool();
                return;
            }
            // Deliver obstacle impact before this pooled projectile is deactivated.
            // Unity does not guarantee which participant receives its callback first.
            var breakable = other.GetComponentInParent<NoryangjinBreakable>();
            if (breakable != null && breakable.Alive)
            {
                breakable.ReceiveProjectile(this);
                ReturnToPool();
                return;
            }
            var obstacle = other.GetComponentInParent<ObstacleStats>();
            obstacle?.ReactToProjectile();
            if (obstacle != null)
            {
                var highway = obstacle.GetComponent<HighwayHazard>();
                if (highway != null) highway.ReactToProjectile(this);
            }
            bool hitLamp = obstacle != null && obstacle.enabled && obstacle.obstaclePattern == ObstaclePattern.Light;
            if (hitLamp || other.CompareTag("EnemyTag") ||
                other.CompareTag("BarrelTag") ||
                other.CompareTag("Obstacle"))
            {
                ReturnToPool();
            }
        }

        // Retained after pooling so both Unity collision participants apply the same deck rule.
        public bool CanHitTarget(Transform target)
        {
            if (chapterRetired) return false;
            if (launchDeck < 0) return true;
            // Physics may still dispatch a contact while the chapter clock is
            // paused. Keep the shot live; its overlap is resolved on resume.
            if (!TimeManager.isGameRunning || TimeManager.timeFactor <= 0f) return false;
            if (chapterOwner == null || !chapterOwner.Running || chapterOwner.IsTransferring || target == null || target.gameObject.scene != chapterOwner.gameObject.scene) return false;
            var deck = target.GetComponentInParent<Chapter45Deck>();
            var switchTarget = target.GetComponentInParent<Chapter45Target>();
            int targetDeck = deck != null ? deck.floor : switchTarget != null ? switchTarget.floor : -1;
            return Chapter45Rules.SameDeck(launchDeck, launchFloorHeight, targetDeck, target.position.y);
        }

        public static void RetireChapterShots(PlayerScript owner)
        {
            var snapshot = new List<BulletScript>(ActiveProjectiles);
            foreach (var projectile in snapshot)
            {
                if (projectile == null || projectile.routeOwner != owner) continue;
                projectile.chapterRetired = true;
                if (projectile.bulletPooler != null) projectile.ReturnToPool();
                else projectile.GetProjectileTransform().gameObject.SetActive(false);
            }
        }

        private void ReturnToPool()
        {
            if (returnedToPool) return;
            returnedToPool = true; // Claim before SetActive(false) invokes OnDisable.
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
