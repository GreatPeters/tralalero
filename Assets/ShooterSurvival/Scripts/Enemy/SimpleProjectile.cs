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

            isAttacked = true;

            PlayerScript player = other.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.currentHealth = Mathf.Max(0f, player.currentHealth - damage);
                player.UpdateHealth();
            }

            if (TryGetComponent(out TrailRenderer trail))
                trail.enabled = false;

            if (transform.name == "Arrow2")
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

