using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class ExplodeScript_space : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [Tooltip("Original object that will be deactivated when exploded.")]
        public GameObject originalObject;

        [Tooltip("Prefab for the fractured pieces after explosion.")]
        public GameObject fracturedPrefab;

        [Tooltip("Force applied to fractured pieces.")]
        public float explosionForce = 20f;

        [Tooltip("Radius within which the explosion force is applied.")]
        public float explosionRadius = 5f;

        [Tooltip("Time before the object is destroyed after the explosion.")]
        // public float destroyDelay = 3f;

        [Header("Dependancies")]
        public GameObject barrelExplodeFX;

        private GameObject fracturedInstance;
        private bool hasExploded = false;
        private CameraShake cameraShake;

        private void Start()
        {
            cameraShake = FindFirstObjectByType<CameraShake>();
        }

        public void Explode()
        {
            if (hasExploded == true) return;
            hasExploded = true;

            cameraShake.Shake(1f, 1f, 1.75f, 5f);

            if (originalObject != null && fracturedPrefab != null)
            {
                originalObject.SetActive(false);

                Transform spawnPos = transform.GetChild(1);

                fracturedInstance = Instantiate(fracturedPrefab, spawnPos);
                fracturedInstance.transform.localPosition = Vector3.zero;

                foreach (Transform piece in fracturedPrefab.transform)
                {
                    Rigidbody rb = piece.GetComponent<Rigidbody>();
                    rb.AddExplosionForce(explosionForce, fracturedInstance.transform.position, explosionRadius);
                }
            }

            GameObject deathFX = Instantiate(barrelExplodeFX, transform);
            Destroy(gameObject, deathFX.GetComponent<ParticleSystem>().main.duration + 2);

        }
    }
}