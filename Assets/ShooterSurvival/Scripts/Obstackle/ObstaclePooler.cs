using System.Collections.Generic;
using UnityEngine;

public class ObstaclePooler : MonoBehaviour
{
    public static ObstaclePooler Instance { get; private set; }

    [SerializeField] private ObstaclePrefabs prefabMap;

    private Dictionary<ObstaclePattern, Queue<GameObject>> poolDict = new();

    private void Awake()
    {
        Instance = this;

        foreach (var entry in prefabMap.obstaclePrefabs)
        {
            if (entry.pattern == ObstaclePattern.None) continue;

            Queue<GameObject> queue = new();

            for (int i = 0; i < entry.poolSize; i++)
            {
                GameObject obj = Instantiate(entry.prefab, transform);
                obj.SetActive(false);

                var identifier = obj.GetComponent<ObstacleIdentifier>();
                if (identifier != null)
                    identifier.pattern = entry.pattern;

                queue.Enqueue(obj);
            }

            poolDict[entry.pattern] = queue;
        }
    }

    public GameObject Get(ObstaclePattern pattern, Vector3 position, Transform parent = null)
    {
        if (!poolDict.TryGetValue(pattern, out var queue) || queue.Count == 0)
        {
            Debug.LogWarning($"[Pooler] No object in pool for pattern: {pattern}");
            return null;
        }

        GameObject obj = queue.Dequeue();
        obj.transform.position = position;
        obj.transform.SetParent(parent);
        obj.SetActive(true);
        return obj;
    }

    public void Return(ObstaclePattern pattern, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        poolDict[pattern].Enqueue(obj);
    }
}
