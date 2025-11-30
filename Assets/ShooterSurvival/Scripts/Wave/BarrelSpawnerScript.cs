using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public class BarrelSpawnerScript : MonoBehaviour
    {
        [Header("Params")]
        [SerializeField] private Vector2 xSpawnLimits;
        [SerializeField] private Vector2 zSpawnLimits;

        [Header("Dependencies")]
        [Tooltip("Index of the prefab in this array must match BarrelType enum")]
        [SerializeField] private GameObject[] barrelPrefabs;

        private void Awake()
        {
            if (barrelPrefabs == null || barrelPrefabs.Length == 0)
                Debug.LogError("Barrel prefabs not assigned in BarrelSpawnerScript.");
        }

        public void Spawn(Wave wave)
        {
            // Spawns barrels based on the wave data. Each barrel type is spawned according to its defined count in the wave.
            if (wave.barrels == null) return;

            foreach (var barrelEntry in wave.barrels)
            {                                // Loop through each barrel entry in the wave
                for (int i = 0; i < barrelEntry.barrelCount; i++)
                {                    // Spawn each barrel based on its count
                    int index = (int)barrelEntry.barrelType;                          // Index for the prefab based on barrel type

                    if (index >= 0 && index < barrelPrefabs.Length)
                    {
                        Vector3 offset = new Vector3(
                            Random.Range(xSpawnLimits.x, xSpawnLimits.y),
                            0.05f,
                            Random.Range(zSpawnLimits.x, zSpawnLimits.y));

                        if (TimeManager.isGameRunning == false) return;
                        Instantiate(barrelPrefabs[index], transform.position + offset, Quaternion.identity); // Instantiate the barrel at the calculated position
                    }
                }
            }
        }
    }
}