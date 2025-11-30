using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class FX_script : MonoBehaviour
    {
        [HideInInspector] public string fxName;
        [HideInInspector] public float effectDuration = 3f;

        private FX_pooler fX_Pooler;

        private void Awake()
        {
            fX_Pooler = FindFirstObjectByType<FX_pooler>();
        }

        private void OnEnable()
        {
            CancelInvoke(); // prevent multiple invokes stacking
            Invoke(nameof(ReturnToPool), effectDuration);
        }

        private void ReturnToPool()
        {
            if (fX_Pooler != null)
            {
                fX_Pooler.ReturnObjectToPool_FX(gameObject);
            }
            else
            {
                Debug.LogWarning("FX_pooler reference is missing.");
            }
        }
    }
}