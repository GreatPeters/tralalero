#if UNITY_EDITOR
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class BonusWallChoicePairTests
{
    private readonly List<GameObject> objects = new();
    private AuthoredBonusWall Altar(string name)
    {
        var go = new GameObject(name); objects.Add(go);
        go.AddComponent<BoxCollider>(); go.AddComponent<WallScript>();
        return go.AddComponent<AuthoredBonusWall>();
    }
    [TearDown] public void Cleanup() { foreach (var go in objects) if (go != null) Object.DestroyImmediate(go); objects.Clear(); }

    [TestCase(true)]
    [TestCase(false)]
    public void ChoosingEitherSide_ClosesTheOtherAndRejectsSecondReward(bool chooseLeft)
    {
        var left = Altar("Left"); var right = Altar("Right");
        var pair = left.gameObject.AddComponent<BonusWallChoicePair>(); pair.Configure(left, right);
        var chosen = chooseLeft ? left : right; var other = chooseLeft ? right : left;
        Assert.That(pair.TryChoose(chosen), Is.True);
        Assert.That(pair.Selected, Is.EqualTo(chosen));
        Assert.That(other.gameObject.activeSelf, Is.False);
        Assert.That(other.GetComponent<BoxCollider>().enabled, Is.False);
        Assert.That(pair.TryChoose(other), Is.False);
        Assert.That(pair.TryChoose(chosen), Is.False);
    }

    [Test]
    public void ForeignOrInactiveAltar_CannotClaimThePair()
    {
        var left = Altar("Left"); var right = Altar("Right"); var foreign = Altar("Foreign");
        var pair = left.gameObject.AddComponent<BonusWallChoicePair>(); pair.Configure(left, right);
        Assert.That(pair.TryChoose(foreign), Is.False);
        left.gameObject.SetActive(false);
        Assert.That(pair.TryChoose(left), Is.False);
        Assert.That(pair.Selected, Is.Null);
    }

    [Test]
    public void NewRun_RestoresBothChoicesAndRollsDistinctRareEffects()
    {
        var left = Altar("Left"); var right = Altar("Right");
        var pair = left.gameObject.AddComponent<BonusWallChoicePair>(); pair.Configure(left, right);
        pair.TryChoose(right);
        pair.PrepareForRun(Rarity.Rare, true);
        Assert.That(pair.Selected, Is.Null);
        Assert.That(left.gameObject.activeSelf && right.gameObject.activeSelf, Is.True);
        Assert.That(left.GetComponent<BoxCollider>().enabled && right.GetComponent<BoxCollider>().enabled, Is.True);
        Assert.That(left.RolledStat, Is.Not.Empty);
        Assert.That(right.RolledStat, Is.Not.Empty.And.Not.EqualTo(left.RolledStat));
        Assert.That(pair.TryChoose(left), Is.True);
    }

    [Test]
    public void CanonicalDropPrefab_RemainsASingleUnpairedAltar()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab");
        Assert.That(prefab.GetComponent<BonusWallChoicePair>(), Is.Null);
        Assert.That(prefab.GetComponent<AuthoredBonusWall>().ChoicePair, Is.Null);
    }

    [Test]
    public void DisabledPair_ClosesBothSides()
    {
        var left = Altar("Left"); var right = Altar("Right");
        var pair = left.gameObject.AddComponent<BonusWallChoicePair>(); pair.Configure(left, right);
        pair.PrepareForRun(Rarity.Normal, false);
        Assert.That(left.gameObject.activeSelf || right.gameObject.activeSelf, Is.False);
    }
}
#endif
