using System;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class NoryangjinMapToolTestSpeedTests
{
    [TestCase(false, "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity", false)]
    [TestCase(true, "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity", true)]
    [TestCase(true, "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity", true)]
    [TestCase(true, "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity", true)]
    [TestCase(true, "Assets/Scenes/Other.unity", false)]
    public void TestSpeed_IsLimitedToPlayingMapToolScenes(bool playing, string path, bool expected)
    {
        Assert.That(NoryangjinMapToolTestSpeed.CanApply(playing, path), Is.EqualTo(expected));
    }

    [TestCase(2f)]
    [TestCase(3f)]
    public void SelectingTestSpeedInEditMode_PreservesClockPhysicsAndGameTimeFactor(float value)
    {
        float selected = NoryangjinMapToolTestSpeed.SelectedTimeScale;
        float scale = Time.timeScale;
        float physicsStep = Time.fixedDeltaTime;
        float gameFactor = TimeManager.timeFactor;
        try
        {
            NoryangjinMapToolTestSpeed.SelectTimeScale(value);
            Assert.That(NoryangjinMapToolTestSpeed.SelectedTimeScale, Is.EqualTo(value));
            Assert.That(Time.timeScale, Is.EqualTo(scale));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(physicsStep));
            Assert.That(TimeManager.timeFactor, Is.EqualTo(gameFactor));
            NoryangjinMapToolTestSpeed.SelectTimeScale(1f);
            Assert.That(NoryangjinMapToolTestSpeed.SelectedTimeScale, Is.EqualTo(1f));
        }
        finally
        {
            NoryangjinMapToolTestSpeed.SelectTimeScale(selected);
        }
    }

    [Test]
    public void UnsupportedSpeed_IsRejectedWithoutChangingTheSelection()
    {
        float selected = NoryangjinMapToolTestSpeed.SelectedTimeScale;
        Assert.Throws<ArgumentOutOfRangeException>(() => NoryangjinMapToolTestSpeed.SelectTimeScale(4f));
        Assert.That(NoryangjinMapToolTestSpeed.SelectedTimeScale, Is.EqualTo(selected));
    }
}
