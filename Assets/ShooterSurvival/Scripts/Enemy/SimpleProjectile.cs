using Unity.VisualScripting;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // 투사체가 '무언가에 부딪혔을 때' 피해 주고 스스로 파괴
    public class SimpleProjectile : MonoBehaviour
    {
        public float damage = 5f;
        public string targetTag = "Player";
        public string helperTag = "ExtraHelpTag"; // 옵션

        void OnTriggerEnter(Collider other)
        {
            // 플레이어 피격
            if (other.CompareTag(targetTag))
            {                
                var ps = other.GetComponent<PlayerScript>();
                if (ps != null) ps.currentHealth = Mathf.Max(0f, ps.currentHealth - damage);

                if(transform.GetComponent<TrailRenderer>() != null)
                {
                    transform.GetComponent<TrailRenderer>().enabled = false; // 트레일 렌더러 끄기
                }
                
                if(transform.name != "Paddle")
                {
                    Destroy(gameObject);
                }
                else
                {
                    var obs = GetComponentInParent<ObstacleStats>();
                    if (obs != null)
                        StartCoroutine(obs.SpinAndMovePlayer(other.transform, 2f));
                }

                    return;
            }

            /*
            // 보조 대상(옵션)
            if (!string.IsNullOrEmpty(helperTag) && other.CompareTag(helperTag))
            {
                var hs = other.GetComponent<ExtraHelpBuffScript>();
                if (hs != null) hs.currentHealth = Mathf.Max(0f, hs.currentHealth - damage);
                Destroy(gameObject);
                return;
            }
            */
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log(collision.gameObject.name + "!!");
        }
    }
}

