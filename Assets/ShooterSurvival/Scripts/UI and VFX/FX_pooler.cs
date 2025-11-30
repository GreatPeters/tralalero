using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class FX_pooler : MonoBehaviour
    {
        [System.Serializable]
        public class FXType
        {
            public string fxName;
            public GameObject prefab;
            public int count;
        }

        [SerializeField] private List<FXType> fxTypes;

        private Dictionary<string, Queue<GameObject>> fxPool = new Dictionary<string, Queue<GameObject>>();

        private void Awake()
        {
            // DontDestroyOnLoad(gameObject);

            foreach (FXType fxType in fxTypes)
            {
                if (string.IsNullOrEmpty(fxType.fxName))
                {
                    Debug.LogWarning("One of the FX types has an empty fxName!");
                    continue;
                }

                Queue<GameObject> pool = new Queue<GameObject>();

                for (int i = 0; i < fxType.count; i++)
                {
                    GameObject fx = Instantiate(fxType.prefab, transform);

                    FX_script fxScript = fx.GetComponent<FX_script>();
                    if (fxScript == null)
                    {
                        fxScript = fx.AddComponent<FX_script>();
                    }

                    fxScript.fxName = fxType.fxName;

                    if (string.IsNullOrEmpty(fxScript.fxName))
                    {
                        Debug.LogWarning($"FX prefab '{fx.name}' has an empty fxName.");
                    }

                    fx.SetActive(false);
                    pool.Enqueue(fx);
                }

                fxPool.Add(fxType.fxName, pool);
            }
        }

        public GameObject GetObjectFromPool_FX(string fxName, Transform callerTransform)
        {
            if (callerTransform == null)
            {
                Debug.LogWarning("callerTransform is null");
                return null;
            }

            if (!fxPool.ContainsKey(fxName))
            {
                Debug.LogWarning($"FX Pool does not contain fxName: {fxName}");
                return null;
            }

            Queue<GameObject> pool = fxPool[fxName];
            GameObject fx = null;

            // Clean up any destroyed objects in the queue
            while (pool.Count > 0)
            {
                fx = pool.Dequeue();
                if (fx == null) continue; // Skip destroyed objects
                break;
            }

            if (fx == null)
            {
                Debug.LogWarning($"All pooled FX objects for '{fxName}' are null or destroyed.");
                return null;
            }

            FX_script fxScript = fx.GetComponent<FX_script>();
            if (fxScript != null)
            {
                fxScript.fxName = fxName;
            }
            else
            {
                Debug.LogWarning($"FX object '{fx.name}' is missing FX_script component.");
            }

            fx.transform.SetParent(callerTransform);
            fx.transform.position = callerTransform.position;
            fx.SetActive(true);

            return fx;
        }


        public void ReturnObjectToPool_FX(GameObject fx)
        {
            FX_script fxScript = fx.GetComponent<FX_script>();
            if (fxScript == null)
            {
                Debug.LogWarning($"Returned FX object '{fx.name}' is missing FX_script component.");
                return;
            }

            string fxName = fxScript.fxName;
            if (string.IsNullOrEmpty(fxName) || !fxPool.ContainsKey(fxName))
            {
                Debug.LogWarning($"Invalid fxName '{fxName}' on returned FX object.");
                return;
            }

            fx.SetActive(false);
            fx.transform.SetParent(transform);
            fx.transform.position = transform.position;

            fxPool[fxName].Enqueue(fx);
        }
    }
}