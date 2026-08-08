using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // ����ü�� '���𰡿� �ε����� ��' ���� �ְ� ������ �ı�
    public class SimpleProjectile : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        private bool isAttacked;
        [System.NonSerialized] public float damage = 5f;

        private void OnEnable()
        {
            isAttacked = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PlayerTag))
                return;

            PlayerScript player = other.GetComponent<PlayerScript>();
            if (player != null)
                player.currentHealth = Mathf.Max(0f, player.currentHealth - damage);

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
                if (obstacle != null && !isAttacked)
                {
                    StartCoroutine(obstacle.SpinAndMovePlayer(other.transform, 2f));
                    isAttacked = true;
                }
                return;
            }

            Destroy(gameObject);
        }
    }
}

