using System;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class PlayerDamageNotificationTests
{
    [TestCase(2f,true,0f)]
    [TestCase(2f,false,.1f)]
    [TestCase(.02f,false,.02f)]
    [TestCase(-1f,false,0f)]
    public void ANewDamageNoticeSurvivesItsHitFrameAndLargeHitches(float elapsed,bool newHit,float expected)
        => Assert.That(PlayerDamageFeedback.VisibleFrameDelta(elapsed,newHit),Is.EqualTo(expected));

    [Test]
    public void EveryCauseReportsActualLossOnceEvenAfterRepeatedHudRefresh()
    {
        foreach(PlayerDamageCause cause in Enum.GetValues(typeof(PlayerDamageCause)))
        {
            var go=new GameObject("Damage contract");
            try
            {
                var player=go.AddComponent<PlayerScript>();
                player.RefreshUpgradeStats();
                int count=0;float received=0;PlayerDamageCause source=PlayerDamageCause.None;
                player.DamageTaken+=(amount,why)=>{count++;received=amount;source=why;};
                float damage=player.currentHealth*.2f;
                player.ApplyDamage(damage,cause);player.UpdateHealth();player.UpdateHealth();
                Assert.That(count,Is.EqualTo(1),cause.ToString());
                Assert.That(received,Is.EqualTo(damage).Within(.001));Assert.That(source,Is.EqualTo(cause));
                player.ApplyDamage(float.MaxValue,cause);
                Assert.That(count,Is.EqualTo(2));Assert.That(player.LastDamageWasFatal,Is.True);
                player.ApplyDamage(10,PlayerDamageCause.Other);player.UpdateHealth();Assert.That(count,Is.EqualTo(2));
            }
            finally{Object.DestroyImmediate(go);}
        }
    }

    [Test]
    public void HealingAndInvalidDamageDoNotEmitDamageAndLegacyLossStillDoes()
    {
        var go=new GameObject("Damage fallback");
        try
        {
            var p=go.AddComponent<PlayerScript>();p.RefreshUpgradeStats();int count=0;p.DamageTaken+=(_,_)=>count++;
            p.ApplyDamage(float.NaN,PlayerDamageCause.Other);p.ApplyDamage(float.PositiveInfinity,PlayerDamageCause.Other);p.ApplyDamage(-10,PlayerDamageCause.Other);
            p.ApplyRunHealthBonus(20,false);p.UpdateHealth();Assert.That(count,Is.Zero);
            p.currentHealth-=5;p.UpdateHealth();p.UpdateHealth();Assert.That(count,Is.EqualTo(1));
            p.ApplyHarnessHealthDelta(5);p.UpdateHealth();Assert.That(count,Is.EqualTo(1));
        }
        finally{Object.DestroyImmediate(go);}
    }
}
