using IndianOceanAssets.ShooterSurvival.Ads;
using NUnit.Framework;

public class RewardedCoinOfferTests
{
    [Test]
    public void RewardRequiresAnAttemptAndIsPaidOnceEvenAfterClose()
    {
        var offer = new RewardedCoinOffer(); offer.Reset("round1", 45, 20, 500);
        Assert.That(offer.TryClaim(0, out _), Is.False);
        Assert.That(offer.TryBegin(out int token), Is.True);
        Assert.That(offer.TryBegin(out _), Is.False);
        offer.Finish(token);
        Assert.That(offer.TryClaim(token, out int coins), Is.True);
        Assert.That(coins, Is.EqualTo(45));
        Assert.That(offer.TryClaim(token, out _), Is.False);
        offer.Reset("round1", 45, 20, 500);
        Assert.That(offer.Eligible, Is.False);
    }
    [Test]
    public void OldAttemptCannotRewardAnotherRound()
    {
        var offer = new RewardedCoinOffer(); offer.Reset("round1", 10, 20, 500);
        offer.TryBegin(out int old); offer.Finish(old); offer.Reset("round2", 80, 20, 500);
        Assert.That(offer.TryClaim(old, out _), Is.False);
        offer.TryBegin(out int current); offer.Invalidate();
        Assert.That(offer.TryClaim(current, out _), Is.False);
    }
    [TestCase(-5, 20)]
    [TestCase(120, 120)]
    [TestCase(9999, 500)]
    public void BonusUsesConfiguredBounds(int earned, int expected)
    {
        var offer = new RewardedCoinOffer(); offer.Reset("round", earned, 20, 500);
        Assert.That(offer.Coins, Is.EqualTo(expected));
    }
}
