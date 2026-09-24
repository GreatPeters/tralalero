using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // ����ü�� '���𰡿� �ε����� ��' ���� �ְ� ������ �ı�
    public class SimpleProjectile : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        private bool isAttacked;
        private bool inFlight;
        [System.NonSerialized] public float damage = 5f;
        public PlayerDamageCause damageCause = PlayerDamageCause.GuardShot;
        private Rigidbody flightBody;
        private Vector3 launchVelocity;
        private float remainingLifetime;
        private bool managedFlight;

        public void Launch(Vector3 direction, float speed, float hitDamage, float lifetime)
        {
            damage = hitDamage;
            launchVelocity = direction.normalized * Mathf.Max(0f, speed);
            remainingLifetime = Mathf.Max(.1f, lifetime);
            managedFlight = true;
            flightBody = GetComponent<Rigidbody>();
            if (flightBody == null) flightBody = gameObject.AddComponent<Rigidbody>();
            flightBody.isKinematic = false;
            flightBody.useGravity = false;
            flightBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            flightBody.interpolation = RigidbodyInterpolation.Interpolate;
            flightBody.angularVelocity = Vector3.zero;
            flightBody.linearVelocity = TimeManager.isGameRunning ? launchVelocity * TimeManager.timeFactor : Vector3.zero;
            SetFlightActive(true);
        }

        private void FixedUpdate()
        {
            if (!managedFlight || !inFlight || flightBody == null) return;
            float factor = TimeManager.isGameRunning ? Mathf.Max(0f, TimeManager.timeFactor) : 0f;
            flightBody.linearVelocity = launchVelocity * factor;
            remainingLifetime -= Time.fixedDeltaTime * factor;
            if (remainingLifetime <= 0f) Destroy(gameObject);
        }

        private void OnEnable()
        {
            isAttacked = false;
            // Legacy standalone hazards already own their launch lifecycle.
            if (GetComponentInParent<EnemyScript_space>() != null) SetFlightActive(false);
            else inFlight = true;
        }

        public void SetFlightActive(bool active)
        {
            inFlight = active;
            if (active) isAttacked = false;
            foreach (var trail in GetComponentsInChildren<TrailRenderer>(true))
            {
                trail.emitting = false;
                trail.Clear();
                trail.enabled = active;
                trail.emitting = active;
            }
            if (TryGetComponent(out Collider projectileCollider)) projectileCollider.enabled = active;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isAttacked || !other.CompareTag(PlayerTag) || (!inFlight && GetComponentInParent<EnemyScript_space>() != null))
                return;

            PlayerScript player = other.GetComponent<PlayerScript>();
            if (player == null) return;
            isAttacked = true;
            player.ApplyDamage(damage,damageCause);

            if (TryGetComponent(out TrailRenderer trail))
                trail.enabled = false;

            if (!managedFlight && transform.name == "Arrow2")
            {
                gameObject.SetActive(false);
                return;
            }

            if (transform.name == "Paddle")
            {
                ObstacleStats obstacle = GetComponentInParent<ObstacleStats>();
                if (obstacle != null)
                {
                    StartCoroutine(obstacle.SpinAndMovePlayer(other.transform, 2f));
                }
                return;
            }

            Destroy(gameObject);
        }

        private void OnDisable()
        {
            inFlight = false;
            foreach (var trail in GetComponentsInChildren<TrailRenderer>(true)) { trail.emitting = false; trail.Clear(); }
        }
    }
}

