using System.Collections;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class WallSpawnerScript : MonoBehaviour
    {
        [Header("Spawn Limits")]
        [Tooltip("The x-axis limits within which walls will spawn.")]
        [SerializeField] private Vector2 xSpawnLimits;
        [Tooltip("The z-axis limits within which walls will spawn.")]
        [SerializeField] private Vector2 zSpawnLimits;

        [Header("Wall Prefab")]
        [Tooltip("The wall prefab to be instantiated during wave spawning.")]
        [SerializeField] private GameObject wallPrefab;

        // Spawns the walls based on the provided wave configuration
        public void Spawn(Wave wave)
        {
            StartCoroutine(SpawnWalls(wave));
        }

        // Coroutine for spawning walls in waves
        private IEnumerator SpawnWalls(Wave wave)
        {
            // Loop through each wall in the wave configuration
            foreach (var wall in wave.walls)
            {
                // Wait for a random period before spawning the next wall
                yield return new WaitForSeconds(Random.Range(3f, 5f));

                // Spawn the specified number of walls for this particular wall entry
                for (int i = 0; i < wall.wallCount; i++)
                {
                    if (TimeManager.isGameRunning == false) break;

                    // Instantiate the wall prefab
                    GameObject wallInstance = Instantiate(wallPrefab);
                    wallInstance.transform.position = transform.position + new Vector3(
                        Random.Range(xSpawnLimits.x, xSpawnLimits.y),
                        0.05f,
                        Random.Range(zSpawnLimits.x, zSpawnLimits.y));

                    WallScript wallScript = wallInstance.GetComponent<WallScript>();
                    if (wallScript != null)
                    {
                        // Assign wall type based on the wall's WallEffectType
                        wallScript.wallType = (wall.wallEffectType == WallEffectType.HealthIncrease ||
                                                wall.wallEffectType == WallEffectType.FireRateIncrease ||
                                                wall.wallEffectType == WallEffectType.ExtraHelp)
                                                ? WallType.BuffWall
                                                : WallType.NerfWall;

                        // Assign BuffType or NerfType based on the effect
                        switch (wall.wallEffectType)
                        {
                            case WallEffectType.HealthIncrease:
                                wallScript.buffType = BuffType.HealthBoost;
                                break;
                            case WallEffectType.FireRateIncrease:
                                wallScript.buffType = BuffType.FireRateIncrease;
                                break;
                            case WallEffectType.ExtraHelp:
                                wallScript.buffType = BuffType.ExtraHelp;
                                break;
                            case WallEffectType.HealthDecrease:
                                wallScript.nerfType = NerfType.HealthReduce;
                                break;
                            case WallEffectType.FireRateDecrease:
                                wallScript.nerfType = NerfType.FireRateReduce;
                                break;
                        }
                    }

                    yield return new WaitForSeconds(5f);
                }

                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}