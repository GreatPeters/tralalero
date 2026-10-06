using NUnit.Framework;

// Essential proposal 12: four captions (theft, the god's anger, the curse, better shoes)
// follow the shots of Curse_Opening_Animated.mp4 (24 fps, 39 s).
public class OpeningMovieTimingTests
{
    [TestCase(0, 0)]
    [TestCase(10.99, 0)]
    [TestCase(11, 1)]
    [TestCase(20.49, 1)]
    [TestCase(20.5, 2)]
    [TestCase(26.99, 2)]
    [TestCase(27, 3)]
    [TestCase(38.99, 3)]
    public void CaptionsFollowUnequalShotBoundaries(double seconds, int page)
        => Assert.That(OpeningStoryUI.GetMoviePageAtTime(seconds), Is.EqualTo(page));

    [TestCase(0, 0)]
    [TestCase(1, 264)]
    [TestCase(2, 492)]
    [TestCase(3, 648)]
    public void NextSeeksToTheActualFirstFrame(int page, int frame)
    {
        double seconds = OpeningStoryUI.GetMoviePageStart(page);
        Assert.That(seconds * 24, Is.EqualTo(frame).Within(0.000001));
        Assert.That(OpeningStoryUI.GetMoviePageAtTime(seconds), Is.EqualTo(page));
        if (page > 0)
            Assert.That(OpeningStoryUI.GetMoviePageAtTime(seconds - 1d / 24d), Is.EqualTo(page - 1));
    }

    [Test]
    public void CaptionsTellTheStoryInTheRequestedOrder()
    {
        Assert.That(OpeningStoryUI.Titles, Is.EqualTo(new[] { "신성한 신발을 훔쳤다", "신이 분노했다", "저주를 받았다", "더 좋은 신발을 얻어야 한다" }));
        Assert.That(OpeningStoryUI.Captions.Length, Is.EqualTo(4));
    }
}
