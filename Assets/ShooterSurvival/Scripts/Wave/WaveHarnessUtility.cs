using System.Collections.Generic;

namespace IndianOceanAssets.ShooterSurvival
{
    public static class WaveHarnessUtility
    {
        public static int CountTotalEnemies(Wave wave)
        {
            if (wave?.enemies == null)
                return 0;

            int total = 0;
            for (int i = 0; i < wave.enemies.Count; i++)
                total += wave.enemies[i]?.enemyCount ?? 0;

            return total;
        }

        public static int CountTotalBarrels(Wave wave)
        {
            if (wave?.barrels == null)
                return 0;

            int total = 0;
            for (int i = 0; i < wave.barrels.Count; i++)
                total += wave.barrels[i]?.barrelCount ?? 0;

            return total;
        }

        public static int CountTotalWalls(Wave wave)
        {
            if (wave?.walls == null)
                return 0;

            int total = 0;
            for (int i = 0; i < wave.walls.Count; i++)
                total += wave.walls[i]?.wallCount ?? 0;

            return total;
        }

        public static int CountRemainingEnemies(IReadOnlyList<Wave> waves, int currentWave)
        {
            if (waves == null || currentWave >= waves.Count)
                return 0;

            int total = 0;
            int startIndex = currentWave < 0 ? 0 : currentWave;
            for (int i = startIndex; i < waves.Count; i++)
                total += CountTotalEnemies(waves[i]);

            return total;
        }

        public static bool ShouldTriggerVictory(int currentWave, int totalWaves, int aliveEnemyCount, bool isGameOver)
        {
            return currentWave >= totalWaves && aliveEnemyCount <= 0 && !isGameOver;
        }
    }
}
