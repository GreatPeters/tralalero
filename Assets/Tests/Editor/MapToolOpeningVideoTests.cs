using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MapToolOpeningVideoTests
{
    [TestCase(false, true, false, true)]
    [TestCase(true, true, false, false)]
    [TestCase(false, false, false, false)]
    [TestCase(false, true, true, false)]
    public void OpeningToggleSkipsOnlyAutomaticPlayback(bool enabled, bool playing, bool manual, bool skipped)
    {
        bool original = OpeningStoryUI.EditorAutoPlayEnabled;
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = new GameObject("Opening toggle fixture"); SceneManager.MoveGameObjectToScene(go, scene);
            var story = go.AddComponent<OpeningStoryUI>();
            typeof(OpeningStoryUI).GetField("manualOpen", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(story, manual);
            OpeningStoryUI.EditorAutoPlayEnabled = enabled;
            Assert.That(story.SkipEditorAutoOpeningIfDisabled(playing), Is.EqualTo(skipped));
            Assert.That(go.activeSelf, Is.EqualTo(!skipped));
            Assert.That(story.autoPlayMovie, Is.True, "Do not rewrite the authored setting");
            Assert.That(story.IsMoviePlaying, Is.False);
        }
        finally { OpeningStoryUI.EditorAutoPlayEnabled = original; EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void OffDoesNotHideAnAuthoredNonAutomaticStory()
    {
        bool original = OpeningStoryUI.EditorAutoPlayEnabled;
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = new GameObject("Manual story fixture"); SceneManager.MoveGameObjectToScene(go, scene);
            var story = go.AddComponent<OpeningStoryUI>(); story.autoPlayMovie = false;
            OpeningStoryUI.EditorAutoPlayEnabled = false;
            Assert.That(story.SkipEditorAutoOpeningIfDisabled(true), Is.False);
            Assert.That(go.activeSelf, Is.True);
        }
        finally { OpeningStoryUI.EditorAutoPlayEnabled = original; EditorSceneManager.ClosePreviewScene(scene); }
    }
}
