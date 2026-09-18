using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ChapterPolishIntegrationTests
{
    [TestCase("Noryangjin_MapTool_Mode_SR18", 1, "HighWay")]
    [TestCase("HighWay", 2, "RestStop")]
    [TestCase("RestStop", 3, "")]
    public void ChapterPresentationHasCompleteReferences(string name, int number, string next)
    {
        string path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
        var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas");
            var chapter = canvas.GetComponent<ChapterProgression>();
            Assert.That(chapter.chapter, Is.EqualTo(number)); Assert.That(chapter.nextScene, Is.EqualTo(next));
            Assert.That(chapter.transitionUI, Is.Not.Null); Assert.That(chapter.transitionUI.player, Is.Not.Null);
            Assert.That(chapter.transitionUI.display, Is.Not.Null); Assert.That(chapter.transitionUI.skipButton, Is.Not.Null);
            Assert.That(chapter.clearRewardText, Is.Not.Null);
            if (next.Length > 0) Assert.That(chapter.nextChapterMovie.length, Is.InRange(4.9, 5.2));
            var opening = canvas.GetComponentInChildren<OpeningStoryUI>(true);
            Assert.That(opening.nextText, Is.Not.Null); Assert.That(opening.captionText, Is.Not.Null);
            Assert.That(opening.pageProgress, Is.Not.Null);
            var uiFont = GameUIFont.Load();
            Assert.That(uiFont, Is.Not.Null);
            Assert.That(canvas.GetComponentsInChildren<TMPro.TMP_Text>(true).All(t => t.font == uiFont), Is.True, "Every chapter canvas label uses the verified CC0 font");
            Assert.That(uiFont.sourceFontFile, Is.Not.Null, "Dynamic Korean glyphs need the shipped source font");
            Assert.That(opening.gameObject.activeSelf, Is.EqualTo(number == 1));
            var result = canvas.GetComponent<CanvasScript>().gameOverUI.GetComponent<DefeatPresentation>();
            Assert.That(result.rewardedButton, Is.Not.Null); Assert.That(result.rewardedText, Is.Not.Null);
            Assert.That(result.continueButton, Is.Not.Null); Assert.That(result.adStatusText, Is.Not.Null);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    [Test]
    public void RestStopRoadSupportsEveryRouteSectionAndRegistersForTravel()
    {
        var scene = SceneManager.GetSceneByPath(RestStopChapterBuilder.ScenePath); bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(RestStopChapterBuilder.ScenePath, OpenSceneMode.Additive);
        try
        {
            var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
            var roads = map.Find("Roads"); var colliders = roads.GetComponentsInChildren<MeshCollider>(true);
            var points = new[] { new Vector3(0,0,-420), Vector3.zero, new Vector3(240,0,0), new Vector3(240,0,440), new Vector3(-100,0,440), new Vector3(-100,0,780), new Vector3(220,0,780) };
            for (int segment = 0; segment < points.Length - 1; segment++)
            {
                var delta = points[segment + 1] - points[segment];
                for (float distance = 0; distance <= delta.magnitude; distance += 4)
                {
                    var p = points[segment] + delta.normalized * distance;
                    Assert.That(colliders.Any(c => c.Raycast(new Ray(p + Vector3.up, Vector3.down), out _, 2)), Is.True, "Road gap at " + p);
                }
            }
            var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerScript>(true)).Single();
            Assert.That(player.GetComponent<NoryangjinRoadHeightFollower>().RoadRoot, Is.SameAs(roads));
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == RestStopChapterBuilder.ScenePath && s.enabled), Is.True);
            Assert.That(map.Find("Enemies").GetComponentsInChildren<EnemyScript_space>(true).Count(e => e.HasConfiguredProjectile), Is.GreaterThan(0));
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
