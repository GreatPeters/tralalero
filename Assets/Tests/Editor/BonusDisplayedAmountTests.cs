using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class BonusDisplayedAmountTests
{
    [TestCase("Normal","att",8f,12f,18f)]
    [TestCase("Normal","hp",60f,12f,20f)]
    [TestCase("Unique","att",8f,36f,54f)]
    [TestCase("Unique","hp",60f,36f,54f)]
    public void FlatWorkbookRewardsUseTheAdvertisedPoints(string rarity,string stat,float baseline,float low,float high)
    {
        BonusTables.Reload();Assert.That(BonusTables.TryGet(rarity,stat,out var row),Is.True);
        Assert.That(row.valueType,Is.EqualTo(BonusValueType.Value));
        Assert.That(BonusAltarRules.ResolveValue(row,0,baseline),Is.EqualTo(low));
        Assert.That(BonusAltarRules.ResolveValue(row,1,baseline),Is.EqualTo(high));
    }

    [TestCase("att",8f)] [TestCase("hp",60f)]
    public void RatioFallbackDisplaysTheSameResolvedAmountItApplies(string stat,float baseline)
    {
        var root=new GameObject("Bonus amount regression");var target=new GameObject("Current player");
        try
        {
            var player=target.AddComponent<PlayerScript>();player.originalDamage=8;player.originalHealth=60;
            var wall=root.AddComponent<WallScript>();const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            void Set(string field,object value)=>typeof(WallScript).GetField(field,flags).SetValue(wall,value);
            var row=new BonusRow{stat=stat,min=.14f,max=.14f,valueType=BonusValueType.Ratio};
            Set("playerScript",player);Set("selectedBonusRow",row);Set("hasSelectedBonusRow",true);
            wall.SetStats();
            float applied=(float)typeof(WallScript).GetField("bonusValue",flags).GetValue(wall);
            Assert.That(wall.CurrentBonusDisplayValue,Is.EqualTo(applied));
            Assert.That(wall.CurrentBonusDisplayText,Is.EqualTo("+"+Mathf.RoundToInt(applied)));
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(target);}
    }
}
