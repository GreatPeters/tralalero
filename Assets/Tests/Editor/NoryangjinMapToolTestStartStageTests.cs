using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public sealed class NoryangjinMapToolTestStartStageTests
{
    private int originalSelection;
    private SceneAsset originalDefault;

    [SetUp]
    public void SaveSession()
    {
        originalSelection = NoryangjinMapToolTestStartStage.SelectedStage;
        NoryangjinMapToolTestStartStage.SelectStage(0);
        originalDefault = EditorSceneManager.playModeStartScene;
    }

    [TearDown]
    public void RestoreSession()
    {
        NoryangjinMapToolTestStartStage.SelectStage(0);
        EditorSceneManager.playModeStartScene = originalDefault;
        NoryangjinMapToolTestStartStage.SelectStage(originalSelection);
    }

    [TestCase(1, "Noryangjin_MapTool_Mode_SR18")]
    [TestCase(2, "HighWay")]
    [TestCase(3, "RestStop")]
    public void SelectionChangesPlayStartWithoutOpeningOrDirtyingTheAuthoringScene(int stage, string expected)
    {
        var active = SceneManager.GetActiveScene(); bool dirty = active.isDirty;
        NoryangjinMapToolTestStartStage.SelectStage(stage);
        Assert.That(EditorSceneManager.playModeStartScene.name, Is.EqualTo(expected));
        Assert.That(NoryangjinMapToolTestStartStage.SelectedStage, Is.EqualTo(stage));
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
        Assert.That(active.isDirty, Is.EqualTo(dirty));
    }

    [Test]
    public void SwitchingStagesThenDataRestoresAnExistingCustomStartScene()
    {
        var configured = AssetDatabase.LoadAssetAtPath<SceneAsset>(NoryangjinMapToolWindow.MapToolScenePath);
        Assert.That(configured, Is.Not.Null);
        EditorSceneManager.playModeStartScene = configured;
        NoryangjinMapToolTestStartStage.SelectStage(2);
        NoryangjinMapToolTestStartStage.SelectStage(3);
        NoryangjinMapToolTestStartStage.SelectStage(0);
        Assert.That(EditorSceneManager.playModeStartScene, Is.EqualTo(configured));
    }

    [Test]
    public void DataRestoresTheOrdinaryOpenSceneDefault()
    {
        EditorSceneManager.playModeStartScene = null;
        NoryangjinMapToolTestStartStage.SelectStage(1);
        NoryangjinMapToolTestStartStage.SelectStage(0);
        Assert.That(EditorSceneManager.playModeStartScene, Is.Null);
    }

    [TestCase(-1)] [TestCase(4)]
    public void InvalidSelectionDoesNotChangeTheStartScene(int stage)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NoryangjinMapToolTestStartStage.SelectStage(stage));
        Assert.That(NoryangjinMapToolTestStartStage.SelectedStage, Is.Zero);
        Assert.That(EditorSceneManager.playModeStartScene, Is.EqualTo(originalDefault));
    }

    [Test]
    public void DataDoesNotOverwriteAnotherToolsSubsequentChoice()
    {
        NoryangjinMapToolTestStartStage.SelectStage(1);
        var external = AssetDatabase.LoadAssetAtPath<SceneAsset>(NoryangjinMapToolWindow.MapToolScene2Path);
        EditorSceneManager.playModeStartScene = external;
        NoryangjinMapToolTestStartStage.SelectStage(0);
        Assert.That(EditorSceneManager.playModeStartScene, Is.EqualTo(external));
    }
}
