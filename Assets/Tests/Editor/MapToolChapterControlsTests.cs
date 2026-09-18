using NUnit.Framework;

public sealed class MapToolChapterControlsTests
{
    [TestCase(10, 20, 30)]
    [TestCase(-1, 20, 20)]
    [TestCase(int.MaxValue, 10, int.MaxValue)]
    public void CurrencyGrant_AddsWithoutOverflow(int balance, int amount, int expected) =>
        Assert.That(MapToolCurrencyCheats.AddClamped(balance, amount), Is.EqualTo(expected));

    [Test] public void CurrencyGrant_RejectsNegativeAmounts() =>
        Assert.Throws<System.ArgumentOutOfRangeException>(() => MapToolCurrencyCheats.AddClamped(100, -1));

    [Test] public void Highway_UsesMapToolControlsAndEnemyClassification()
    {
        Assert.That(NoryangjinMapToolWindow.IsMapToolScenePath("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity"), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsMapToolScenePath("Assets/ShooterSurvival/Scenes/Tools/RestStop.unity"), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsEnemyPalettePrefabPath("Assets/ShooterSurvival/Prefabs/RestStop/Enemies/CoffeeVendor.prefab"), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsEnemyPalettePrefabPath("Assets/ShooterSurvival/Prefabs/Highway/Enemies/TrafficPatrol.prefab"), Is.True);
        Assert.That(NoryangjinMapToolWindow.IsEnemyPalettePrefabPath("Assets/ShooterSurvival/Prefabs/Highway/Props/TrafficBarrel.prefab"), Is.False);
    }
}
