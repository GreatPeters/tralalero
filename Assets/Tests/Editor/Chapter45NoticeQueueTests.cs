using NUnit.Framework;

public sealed class Chapter45NoticeQueueTests
{
    static string Title(Chapter45NoticeQueue queue, float now)
        => queue.TryRead(now, out var notice) ? notice.Title : null;

    [TestCase(true)] [TestCase(false)]
    public void SameFrameWarningIsImmediateAndEarnedRewardGetsThreeSecondsAfterward(bool rewardFirst)
    {
        var q = new Chapter45NoticeQueue();
        if (rewardFirst) q.Reward("reward", "+20%", 3, 100);
        q.Warning("warning", "avoid", 1.6f, 100);
        if (!rewardFirst) q.Reward("reward", "+20%", 3, 100);
        Assert.That(Title(q, 100), Is.EqualTo("warning"));
        Assert.That(Title(q, 101.59f), Is.EqualTo("warning"));
        Assert.That(Title(q, 101.6f), Is.EqualTo("reward"));
        Assert.That(Title(q, 104.59f), Is.EqualTo("reward"));
        Assert.That(Title(q, 104.61f), Is.Null);
    }
    [Test] public void CaptainRechargeCannotOverwriteItsOwnTriggeredWarning()
    {
        var q = new Chapter45NoticeQueue();
        q.Warning("warning", "avoid", 1.6f, 100);
        q.Information("recharge", "aim", 2, 100);
        Assert.That(Title(q, 100), Is.EqualTo("warning"));
        Assert.That(Title(q, 101.61f), Is.Null, "Do not show a stale queued recharge notice.");
    }
    [Test] public void ShorterOverlappingWarningCannotReleaseAStillProtectedRewardEarly()
    {
        var q = new Chapter45NoticeQueue();
        q.Warning("long warning", "avoid", 4, 100);
        q.Reward("reward", "+coins", 3, 100);
        q.Warning("new warning", "avoid", 1, 101);
        Assert.That(Title(q, 102.5f), Is.EqualTo("new warning"));
        Assert.That(Title(q, 103.99f), Is.EqualTo("new warning"));
        Assert.That(Title(q, 104), Is.EqualTo("reward"));
    }
    [Test] public void InterruptedRewardRestartsFullReadingTimeAndRetainsRewardOrder()
    {
        var q = new Chapter45NoticeQueue();
        q.Reward("first", "+coins", 3, 100);
        q.Reward("second", "+shield", 3, 101);
        q.Warning("warning", "avoid", 1.6f, 102.8f);
        Assert.That(Title(q, 104.41f), Is.EqualTo("first"));
        Assert.That(Title(q, 107.4f), Is.EqualTo("first"));
        Assert.That(Title(q, 107.42f), Is.EqualTo("second"));
    }
    [Test] public void OrdinaryNoticeCannotInterruptProtectedReward()
    {
        var q = new Chapter45NoticeQueue();
        q.Reward("reward", "+coins", 3, 100);
        q.Information("door", "open", 2, 101);
        Assert.That(Title(q, 102.9f), Is.EqualTo("reward"));
    }
    [Test] public void PausedScaledClockKeepsNoticeAliveForResumeRendering()
    {
        var q = new Chapter45NoticeQueue();
        q.Warning("warning", "avoid", 1.6f, 100);
        for (int i = 0; i < 100; i++) Assert.That(Title(q, 100.4f), Is.EqualTo("warning"));
        Assert.That(Title(q, 101.59f), Is.EqualTo("warning"));
        Assert.That(Title(q, 101.61f), Is.Null);
    }
    [Test] public void LiftSuppressesAndRetainsUnshownRewardsUntilLanding()
    {
        var q = new Chapter45NoticeQueue();
        q.Reward("first", "+coins", 3, 100);
        q.Suspend(); q.Suspend();
        q.Reward("second", "+shield", 3, 103);
        q.Information("door", "open", 2, 105);
        Assert.That(Title(q, 106), Is.Null);
        q.Resume(106);
        Assert.That(Title(q, 106), Is.EqualTo("first"));
        Assert.That(Title(q, 108.99f), Is.EqualTo("first"));
        Assert.That(Title(q, 109), Is.EqualTo("second"));
    }
    [Test] public void ClearRemovesActivePendingAndSuspendedStateBeforeRetry()
    {
        var q = new Chapter45NoticeQueue();
        q.Warning("warning", "avoid", 2, 100);
        q.Reward("reward", "+coins", 3, 100); q.Suspend(); q.Clear();
        q.Resume(0); Assert.That(Title(q, 0), Is.Null);
        q.Information("new run", "start", 4, 0);
        Assert.That(Title(q, 0), Is.EqualTo("new run"));
    }

    [Test] public void OrderCallDoesNotEraseAnActivePlateWarningAndEachLineExpiresIndividually()
    {
        var q = new Chapter45NoticeQueue();
        q.Warning("행동 예고", "접시 투척 · 옆으로", 1.1f, 10);
        q.Warning("주문 034번", "대기줄 이동", 1.2f, 10.1f);
        Assert.That(q.TryRead(10.5f, out var overlapping), Is.True);
        StringAssert.Contains("접시 투척", overlapping.Detail);
        StringAssert.Contains("주문 034번", overlapping.Detail);
        q.TryRead(11.11f, out var remaining);
        StringAssert.DoesNotContain("접시 투척", remaining.Detail);
        StringAssert.Contains("대기줄 이동", remaining.Detail);
        Assert.That(q.TryRead(11.31f, out _), Is.False);
    }
    [Test] public void RepeatedActorWarningDoesNotDuplicateTextAndSuspendDropsStaleDanger()
    {
        var q = new Chapter45NoticeQueue();
        q.Warning("행동 예고", "학생 잡기", 1.1f, 10);
        q.Warning("행동 예고", "학생 잡기", 1.1f, 10.2f);
        q.TryRead(10.3f, out var current);
        Assert.That(current.Detail, Is.EqualTo("학생 잡기"));
        q.Suspend(); q.Resume(11); q.Warning("새 경고", "골프", 1, 11);
        q.TryRead(11.1f, out current);
        Assert.That(current.Detail, Is.EqualTo("골프"));
    }
}
