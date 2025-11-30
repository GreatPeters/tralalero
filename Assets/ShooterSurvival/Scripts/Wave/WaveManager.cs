using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    #region Wave Methods
    // Wave Config
    [Serializable]
    public class Wave
    {
        [Tooltip("The list of enemies to spawn in this wave.")]
        public List<EnemyWaveEntry> enemies = new List<EnemyWaveEntry>();

        [Tooltip("The list of barrels to spawn in this wave.")]
        public List<BarrelWaveEntry> barrels = new List<BarrelWaveEntry>();

        [Tooltip("The list of walls to spawn in this wave.")]
        public List<WallWaveEntry> walls = new List<WallWaveEntry>();

        [Tooltip("The time before the next wave starts.")]
        public float timeForNextWave;
    }

    // Enemy Config
    [Serializable]
    public class EnemyWaveEntry
    {
        [Tooltip("The type of enemy to spawn in the wave.")]
        public EnemyType enemyType;

        [Tooltip("The number of enemies to spawn in the wave.")]
        public int enemyCount;
    }

    // Barrel Config
    [Serializable]
    public class BarrelWaveEntry
    {
        [Tooltip("The type of barrel to spawn in the wave.")]
        public BarrelType barrelType;

        [Tooltip("The number of barrels to spawn in the wave.")]
        public int barrelCount;
    }

    // Wall Config
    [Serializable]
    public class WallWaveEntry
    {
        [Tooltip("The type of wall effect to apply in the wave.")]
        public WallEffectType wallEffectType;

        [Tooltip("The number of walls to spawn in the wave.")]
        public int wallCount;
    }

    // Buff/Nerf Config
    public enum WallEffectType
    {
        HealthIncrease,
        FireRateIncrease,
        ExtraHelp,
        HealthDecrease,
        FireRateDecrease
    }

    #endregion

    // Wave Manager Script
    public class WaveManager : MonoBehaviour
    {
        private EnemySpawnerScript enemySpawner;
        private BarrelSpawnerScript barrelSpawner;
        private WallSpawnerScript wallSpawner;
        private bool isSpawning = false;
        private CanvasScript canvasScript;

        #region Wave Variables
        [Tooltip("The list of waves to be managed and spawned.")]
        public List<Wave> waves = new List<Wave>();

        [Tooltip("The current wave index that is being processed.")]
        public int currentWave = 0;
        #endregion

        private void Awake()
        {
            canvasScript = FindFirstObjectByType<CanvasScript>();
            enemySpawner = GetComponent<EnemySpawnerScript>();
            barrelSpawner = GetComponent<BarrelSpawnerScript>();
            wallSpawner = GetComponent<WallSpawnerScript>();
        }


        private void Update()
        {
            SurvivalModeSpawn();
            GameEnded();
        }

        public void GameEnded()
        {
            if (currentWave >= waves.Count && EnemySpawnerScript.enemyCount == 0 && CanvasScript.isGameOver == false)
            {
                TimeManager.isGameRunning = false;
                canvasScript.YouWin();
            }
        }

        private void SurvivalModeSpawn()
        {
            if (!isSpawning && currentWave < waves.Count && TimeManager.isGameRunning == true)
            {
                StartCoroutine(StartWave(waves[currentWave]));
            }
        }

        // Coroutine to handle the spawning of enemies, barrels, and walls for the current wave
        private IEnumerator StartWave(Wave wave)
        {
            isSpawning = true;

            // Spawn enemies, barrel, and walls for the current wave
            enemySpawner.Spawn(wave);
            barrelSpawner.Spawn(wave);
            wallSpawner.Spawn(wave);

            // Wait for the time before starting the next wave
            yield return new WaitForSeconds(wave.timeForNextWave);

            currentWave++;
            isSpawning = false;
        }
    }
}