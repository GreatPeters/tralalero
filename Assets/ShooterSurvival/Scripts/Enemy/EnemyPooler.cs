using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class EnemyPooler : MonoBehaviour
    {
        public static EnemyPooler Instance { get; private set; }

        [SerializeField] private EnemySO[] enemySOArray;

        private Dictionary<EnemyType, Queue<GameObject>> poolDict = new Dictionary<EnemyType, Queue<GameObject>>();

        private void Awake()
        {
            foreach (var enemySO in enemySOArray)
            {
                Queue<GameObject> enemyQueue = new Queue<GameObject>();

                for (int i = 0; i < enemySO.poolSize; i++)
                {
                    GameObject enemy = Instantiate(enemySO.enemyPrefab, transform.position, Quaternion.identity);       // Instantiate
                    enemy.transform.SetParent(transform);                                                               // Set parent
                    enemy.SetActive(false);                                                                             // Hide gameobject
                    enemyQueue.Enqueue(enemy);                                                                          // Add to pool
                }

                // Add the queue to the dictionary using enemy type as key
                poolDict[enemySO.enemyType] = enemyQueue;
            }
        }

        public GameObject GetObjectFromPool_Enemy(EnemyType enemyType, Transform callerTransform)
        {
            // Re-enable the enemy gameobject and child it to the caller
            if (poolDict.ContainsKey(enemyType) && poolDict[enemyType].Count > 0)
            {
                GameObject enemy = poolDict[enemyType].Dequeue();                       // Remove the gameobject from pool
                enemy.SetActive(true);                                                  // Un-hide the enemy prefab
                enemy.transform.SetParent(callerTransform);

                EnemySpawnerScript.enemyCount += 1;
                return enemy;
            }

            Debug.Log("Enemy Type/Count not found");
            return null;
        }

        public void ReturnObjectToPool_Enemy(EnemyType enemyType, GameObject enemy)
        {
            enemy.SetActive(false);
            enemy.transform.rotation = Quaternion.LookRotation(Vector3.back);           // Reset rotation
            enemy.transform.GetChild(0).gameObject.SetActive(true);                     // Hide the enemy prefab
            enemy.transform.SetParent(transform);                                       // Reset the parent to the pooler
            poolDict[enemyType].Enqueue(enemy);                                         // Add back to pool

            EnemySpawnerScript.enemyCount -= 1;
        }
    }
}