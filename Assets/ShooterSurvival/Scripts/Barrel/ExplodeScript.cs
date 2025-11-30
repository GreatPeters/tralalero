using System.Collections;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class ExplodeScript : MonoBehaviour
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
        public float destroyDelay = 3f;

        private GameObject fracturedInstance;
        private FX_pooler fX_Pooler;
        private bool hasExploded = false;
        private CameraShake cameraShake;
        private Vector3 lastPos;

        private void Start()
        {
            fX_Pooler = FindFirstObjectByType<FX_pooler>().GetComponent<FX_pooler>();
            cameraShake = FindFirstObjectByType<CameraShake>().GetComponent<CameraShake>();
        }

        private void FixedUpdate()
        {
            // Movement for the barrel
            if (hasExploded == false)
            {
                lastPos = transform.position;
                transform.Translate(Vector3.back * Time.deltaTime * TimeManager.timeFactor);
            }
            else transform.position = lastPos;        // Prevents the barrel from moving after explosion
        }

        public void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            cameraShake.Shake(1f, 1f, 1.75f, 5f);

            if (originalObject != null) originalObject.SetActive(false);        // Disable the original object

            // Instantiate and apply explosion force to fractured pieces
            if (fracturedPrefab != null)
            {
                fracturedInstance = Instantiate(fracturedPrefab, lastPos, transform.rotation);
                fracturedInstance.transform.parent = transform;
                fracturedInstance.transform.localPosition = Vector3.zero;

                foreach (Transform piece in fracturedInstance.transform)
                {
                    Rigidbody rb = piece.GetComponent<Rigidbody>();
                    piece.transform.localPosition = Vector3.zero;        // Reset position to zero
                    if (rb != null)
                    {
                        rb.AddExplosionForce(explosionForce, lastPos, explosionRadius);
                    }
                }
            }

            // Instantiate explosion visual effect
            fX_Pooler.GetObjectFromPool_FX("Barrel_ExplodeFire", transform);                                              // Destroy the VFX after 7 seconds

            StartCoroutine(ShrinkAndDestroy());
        }

        private IEnumerator ShrinkAndDestroy()
        {
            // Shrink and Destory each piece
            yield return new WaitForSeconds(destroyDelay);

            float shrinkDuration = 1f;
            float elapsedTime = 0f;
            Vector3 initialScale = transform.localScale;

            while (elapsedTime < shrinkDuration)
            {
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsedTime / shrinkDuration);
                elapsedTime += Time.deltaTime * TimeManager.timeFactor;
                yield return null;
            }

            transform.localScale = Vector3.zero;                // Ensure it is fully shrunk
            originalObject.SetActive(true);
            Destroy(gameObject);                                // Destroy the object
        }
    }
}