using NUnit.Framework;

public class OpeningMovieTimingTests
{
    [TestCase(0, 0)]
    [TestCase(7.99, 0)]
    [TestCase(8, 1)]
    [TestCase(15.99, 1)]
    [TestCase(16, 2)]
    [TestCase(21.04, 2)]
    [TestCase(21.05, 3)]
    [TestCase(26.08, 3)]
    public void CaptionsFollowUnequalShotBoundaries(double seconds, int page)
        => Assert.That(OpeningStoryUI.GetMoviePageAtTime(seconds), Is.EqualTo(page));

    [TestCase(0, 0)]
    [TestCase(1, 192)]
    [TestCase(2, 384)]
    [TestCase(3, 505)]
    public void NextSeeksToTheActualFirstFrame(int page, int frame)
    {
        double seconds = OpeningStoryUI.GetMoviePageStart(page);
        Assert.That(seconds * 24, Is.EqualTo(frame).Within(0.000001));
        Assert.That(OpeningStoryUI.GetMoviePageAtTime(seconds), Is.EqualTo(page));
        if (page > 0)
            Assert.That(OpeningStoryUI.GetMoviePageAtTime(seconds - 1d / 24d), Is.EqualTo(page - 1));
    }
}
