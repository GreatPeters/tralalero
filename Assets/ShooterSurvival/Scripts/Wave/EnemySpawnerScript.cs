using System.Collections;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class EnemySpawnerScript : MonoBehaviour
    {
        [Header("Spawn Area Limits")]

        [Tooltip("Minimum and maximum X values where enemies can spawn.")]
        [SerializeField] private Vector2 xSpawnLimits;

        [Tooltip("Minimum and maximum Z values where enemies can spawn.")]
        [SerializeField] private Vector2 zSpawnLimits;

        [Header("Dependencies")]

        [Tooltip("Reference to the Enemy Pooler script.")]
        [SerializeField] private EnemyPooler enemyPooler;

        public static int enemyCount = 0;


        // Spawn objects set inside wave
        public void Spawn(Wave wave)
        {
            StartCoroutine(SpawnEnemies(wave));
        }

        private IEnumerator SpawnEnemies(Wave wave)
        {
            foreach (var enemy in wave.enemies)
            {
                for (int i = 0; i < enemy.enemyCount; i++)
                {
                    if (TimeManager.isGameRunning == false) break;

                    GameObject enemyObject = enemyPooler.GetObjectFromPool_Enemy(enemy.enemyType, transform);
                    if (enemyObject != null)
                    {
                        Vector3 spawnPos = new Vector3(Random.Range(xSpawnLimits.x, xSpawnLimits.y), 0.05f, Random.Range(zSpawnLimits.x, zSpawnLimits.y));
                        enemyObject.transform.position = transform.position + spawnPos;
                    }

                    yield return new WaitForSeconds(0.8f);
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}