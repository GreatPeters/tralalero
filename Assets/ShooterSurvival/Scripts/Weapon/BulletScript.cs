using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class BulletScript : MonoBehaviour
    {
        BulletPooler bulletPooler;
        Vector3 spawnPosition;
        Vector3 direction;
        public static float bulletRange;
        public static float originalBulletRange;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            bulletRange = 0f;
            originalBulletRange = 0f;
        }

        private void Start()
        {
            bulletPooler = FindFirstObjectByType<BulletPooler>();

            if (originalBulletRange <= 0f)
            {
                bulletRange = 12f;
                originalBulletRange = bulletRange;
            }
        }

        private void FixedUpdate()
        {
            if (TimeManager.Instance.isForwardMarchScene != true)
            {
                transform.position += direction * 10f * Time.deltaTime;
            }
            else transform.position += direction * 20f * Time.deltaTime * TimeManager.timeFactor;
            OutOfRange();
        }

        public void SetDirection(Vector3 dir)
        {
            direction = dir;
            spawnPosition = transform.position;
        }

        private void OutOfRange()
        {
            // Checks if the bullet is outside the range, if true, return the bullet back to pool
            //if (TimeManager.Instance.isForwardMarchScene == true) bulletRange = 7.5f;
            //if (TimeManager.Instance.isForwardMarchScene == false) bulletRange = 25f;

            if (Vector3.Distance(transform.position, spawnPosition) > bulletRange)
            {
                bulletPooler.ReturnObjectToPool_Bullet(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // returns to pool if contacts enemy
            if (other.CompareTag("EnemyTag") || other.CompareTag("BarrelTag"))
            {
                bulletPooler.ReturnObjectToPool_Bullet(gameObject);
            }
            if(other.CompareTag("Obstacle"))
            {
                bulletPooler.ReturnObjectToPool_Bullet(gameObject);
            }
        }
    }
}