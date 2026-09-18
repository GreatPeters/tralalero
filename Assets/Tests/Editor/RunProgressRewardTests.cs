using NUnit.Framework;

public sealed class RunProgressRewardTests
{
    [TestCase(14.99f, 0)]
    [TestCase(15f, 30)]
    [TestCase(109.8f, 210)]
    [TestCase(300f, 600)]
    [TestCase(900f, 600)]
    public void ProgressPaysCompletedIntervalsWithinChapterLimit(float seconds, int expected)
        => Assert.That(RunProgressReward.Calculate(seconds, 15, 30, 20), Is.EqualTo(expected));

    [Test]
    public void InvalidInputsDoNotCreateCurrency()
    {
        Assert.That(RunProgressReward.Calculate(float.NaN, 15, 30, 20), Is.Zero);
        Assert.That(RunProgressReward.Calculate(float.PositiveInfinity, 15, 30, 20), Is.Zero);
        Assert.That(RunProgressReward.Calculate(30, 0, 30, 20), Is.Zero);
        Assert.That(RunProgressReward.Calculate(30, 15, -30, 20), Is.Zero);
    }

    [Test]
    public void LargeRewardsDoNotOverflow()
        => Assert.That(RunProgressReward.Calculate(100, 1, int.MaxValue, 100), Is.EqualTo(int.MaxValue));
}
