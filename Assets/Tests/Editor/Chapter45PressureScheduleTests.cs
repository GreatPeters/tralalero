using NUnit.Framework;

public sealed class Chapter45PressureScheduleTests
{
    [Test] public void HeldFightWarnsAfterReadTimeThenEverySixScaledSeconds()
    {
        var clock = new Chapter45PressureSchedule();
        Assert.That(clock.Step(20, true, 1.8f, 6), Is.False);
        Assert.That(clock.Step(21.79f, true, 1.8f, 6), Is.False);
        Assert.That(clock.Step(21.8f, true, 1.8f, 6), Is.True);
        Assert.That(clock.Step(21.8f, true, 1.8f, 6), Is.False, "Paused chapter time cannot retrigger");
        Assert.That(clock.Step(27.79f, true, 1.8f, 6), Is.False);
        Assert.That(clock.Step(27.8f, true, 1.8f, 6), Is.True);
    }
    [Test] public void LeaveHoldOrRetryRequiresACompleteNewWarningDelay()
    {
        var clock = new Chapter45PressureSchedule();
        clock.Step(20, true, 1.8f, 6);
        Assert.That(clock.Step(21, false, 1.8f, 6), Is.False);
        Assert.That(clock.Step(21.5f, true, 1.8f, 6), Is.False);
        Assert.That(clock.Step(23.3f, true, 1.8f, 6), Is.True);
        Assert.That(clock.Step(0, true, 1.8f, 6), Is.False, "BeginRun clock rewind resets the old schedule");
        Assert.That(clock.Step(1.79f, true, 1.8f, 6), Is.False);
        Assert.That(clock.Step(1.8f, true, 1.8f, 6), Is.True);
    }
    [Test] public void LongFrameDoesNotEmitCatchUpWarnings()
    {
        var clock = new Chapter45PressureSchedule();
        clock.Step(0, true, 1.8f, 6);
        Assert.That(clock.Step(40, true, 1.8f, 6), Is.True);
        Assert.That(clock.Step(40, true, 1.8f, 6), Is.False);
        Assert.That(clock.Step(45.99f, true, 1.8f, 6), Is.False);
        Assert.That(clock.Step(46, true, 1.8f, 6), Is.True);
    }
}
