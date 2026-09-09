using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class NoryangjinRunBalanceTests
{
    [Test]
    public void HealthBonuses_UpdateCapacityAndCurrentHealthAndReset()
    {
        var go = new GameObject("Health bonus player");
        try
        {
            var p = go.AddComponent<PlayerScript>(); p.RefreshUpgradeStats();
            float baseline = p.MaxHealth;
            p.ApplyRunHealthBonus(20, false);
            Assert.That(p.MaxHealth, Is.EqualTo(baseline + 20));
            Assert.That(p.currentHealth, Is.EqualTo(p.MaxHealth));
            p.currentHealth = 50;
            p.ApplyRunHealthBonus(10, true);
            Assert.That(p.MaxHealth, Is.EqualTo((baseline + 20) * 1.1f).Within(.001));
            Assert.That(p.currentHealth, Is.EqualTo(50 + (baseline + 20) * .1f).Within(.001));
            p.ResetState();
            Assert.That(p.MaxHealth, Is.EqualTo(baseline));
            Assert.That(p.currentHealth, Is.EqualTo(baseline));
        }
        finally { Object.DestroyImmediate(go); }
    }
    [TestCase(0, 0)] [TestCase(20, 1)] [TestCase(100, 1)] [TestCase(390, 1)] [TestCase(-1, 0)]
    public void PercentageUpgrade_UnlocksOneHelper(float amount, int count)
        => Assert.That(NoryangjinUpgradeExtraHelpSpawner.ResolvePermanentCount(amount), Is.EqualTo(count));
}
