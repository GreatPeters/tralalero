using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;

public class WaveHarnessUtilityTests
{
    [Test]
    public void CountTotals_AggregatesAllEntries()
    {
        var wave = new Wave
        {
            enemies = new List<EnemyWaveEntry>
            {
                new EnemyWaveEntry { enemyType = EnemyType.Walker, enemyCount = 3 },
                new EnemyWaveEntry { enemyType = EnemyType.Tank, enemyCount = 2 }
            },
            barrels = new List<BarrelWaveEntry>
            {
                new BarrelWaveEntry { barrelCount = 4 }
            },
            walls = new List<WallWaveEntry>
            {
                new WallWaveEntry { wallCount = 1 },
                new WallWaveEntry { wallCount = 2 }
            }
        };

        Assert.That(WaveHarnessUtility.CountTotalEnemies(wave), Is.EqualTo(5));
        Assert.That(WaveHarnessUtility.CountTotalBarrels(wave), Is.EqualTo(4));
        Assert.That(WaveHarnessUtility.CountTotalWalls(wave), Is.EqualTo(3));
    }

    [Test]
    public void CountRemainingEnemies_StartsFromCurrentWave()
    {
        var waves = new List<Wave>
        {
            new Wave
            {
                enemies = new List<EnemyWaveEntry>
                {
                    new EnemyWaveEntry { enemyCount = 2 }
                }
            },
            new Wave
            {
                enemies = new List<EnemyWaveEntry>
                {
                    new EnemyWaveEntry { enemyCount = 5 }
                }
            },
            new Wave
            {
                enemies = new List<EnemyWaveEntry>
                {
                    new EnemyWaveEntry { enemyCount = 7 }
                }
            }
        };

        Assert.That(WaveHarnessUtility.CountRemainingEnemies(waves, 1), Is.EqualTo(12));
        Assert.That(WaveHarnessUtility.CountRemainingEnemies(waves, 3), Is.EqualTo(0));
        Assert.That(WaveHarnessUtility.CountRemainingEnemies(waves, -1), Is.EqualTo(14));
    }

    [Test]
    public void ShouldTriggerVictory_OnlyWhenRunIsComplete()
    {
        Assert.That(WaveHarnessUtility.ShouldTriggerVictory(3, 3, 0, false), Is.True);
        Assert.That(WaveHarnessUtility.ShouldTriggerVictory(2, 3, 0, false), Is.False);
        Assert.That(WaveHarnessUtility.ShouldTriggerVictory(3, 3, 1, false), Is.False);
        Assert.That(WaveHarnessUtility.ShouldTriggerVictory(3, 3, 0, true), Is.False);
    }
}
