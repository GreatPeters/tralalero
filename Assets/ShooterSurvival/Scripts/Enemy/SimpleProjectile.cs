using Unity.VisualScripting;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // ����ü�� '���𰡿� �ε����� ��' ���� �ְ� ������ �ı�
    public class SimpleProjectile : MonoBehaviour
    {
        public bool isAttacked = false;
        public float damage = 5f;
        public string targetTag = "Player";
        public string helperTag = "ExtraHelpTag"; // �ɼ�

        void InIt()
        {
            isAttacked = false;
        }

        void OnEnable()
        {
            InIt();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(targetTag))
            {
                var ps = other.GetComponent<PlayerScript>();
                if (ps != null) ps.currentHealth = Mathf.Max(0f, ps.currentHealth - damage);

                if (transform.GetComponent<TrailRenderer>() != null)
                {
                    transform.GetComponent<TrailRenderer>().enabled = false;
                }

                if (transform.name != "Paddle" && transform.name != "Arrow2")
                {
                    Destroy(gameObject);
                }
                else if (transform.name != "Paddle" && transform.name == "Arrow2")
                {
                    gameObject.SetActive(false);
                }

                else if (transform.name.Contains("Paddle"))
                {
                    var obs = GetComponentInParent<ObstacleStats>();
                    if (obs != null && !isAttacked)
                    {
                        StartCoroutine(obs.SpinAndMovePlayer(other.transform, 2f));
                        isAttacked = true;
                    }
                }

                return;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log(collision.gameObject.name + "!!");
        }
    }
}

