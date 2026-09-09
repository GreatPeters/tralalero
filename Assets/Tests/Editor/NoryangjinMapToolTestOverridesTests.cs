using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class NoryangjinMapToolTestOverridesTests
{
    [Test]
    public void PowerAndMovement_RestoreWithoutChangingAuthoredHealth()
    {
        var go = new GameObject("Test player");
        try
        {
            var p = go.AddComponent<PlayerScript>(); p.moveSensitivity_Devision = 125;
            float authoredHealth = p.originalHealth;
            NoryangjinMapToolTestOverrides.Apply(p, true, true);
            Assert.That(p.currentHealth, Is.EqualTo(9999));
            Assert.That(p.MaxHealth, Is.EqualTo(9999));
            Assert.That(p.currentDamage, Is.EqualTo(9999));
            Assert.That(p.originalHealth, Is.EqualTo(authoredHealth));
            Assert.That(p.moveSensitivity_Devision, Is.EqualTo(31.25f));
            NoryangjinMapToolTestOverrides.Apply(p, false, false);
            Assert.That(p.MaxHealth, Is.LessThan(9999));
            Assert.That(p.currentDamage, Is.LessThan(9999));
            Assert.That(p.moveSensitivity_Devision, Is.EqualTo(125));
        }
        finally { NoryangjinMapToolTestOverrides.Restore(); Object.DestroyImmediate(go); }
    }

    [Test]
    public void FastMovement_DoesNotCompoundOnRepeatedRunStarts()
    {
        var go = new GameObject("Test player");
        try
        {
            var p = go.AddComponent<PlayerScript>(); p.moveSensitivity_Devision = 125;
            NoryangjinMapToolTestOverrides.Apply(p, true, true);
            p.currentHealth = 4000;
            NoryangjinMapToolTestOverrides.Apply(p, true, true);
            Assert.That(p.currentHealth, Is.EqualTo(9999));
            Assert.That(p.moveSensitivity_Devision, Is.EqualTo(31.25f));
        }
        finally { NoryangjinMapToolTestOverrides.Restore(); Object.DestroyImmediate(go); }
    }
}
