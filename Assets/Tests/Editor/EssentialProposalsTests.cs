#if UNITY_EDITOR
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Essential proposals 1-15 (2026-10-06): logic contracts that can run without a phone.
public class EssentialProposalsTests
{
    // 1 — AI market voices are disabled at the single playback entry point.
    [Test] public void MarketVoicesAreDisabled() => Assert.That(NoryangjinRevampDirector.VoicesEnabled, Is.False);

    // 3 — the stored sensitivity no longer changes control.
    [Test] public void SensitivityUsesFixedDefault() => Assert.That(SettingsManager.DefaultMoveSensitivity, Is.EqualTo(1f));

    // 5 — leaving the app mid-run pauses once; menus and finished runs are left alone.
    [TestCase(true, false, false, false, true)]
    [TestCase(true, false, true, false, false)]
    [TestCase(true, false, false, true, false)]
    [TestCase(true, true, false, false, false)]
    [TestCase(false, false, false, false, false)]
    public void InterruptionPausesOnlyALiveRun(bool running, bool over, bool settings, bool pause, bool expected)
        => Assert.That(CanvasScript.ShouldPauseForInterruption(running, over, settings, pause), Is.EqualTo(expected));

    // 7 — tips only state enforced rules; the unverified coin claim is excluded.
    [Test] public void LoadingTipsAreVerifiedRules()
    {
        Assert.That(LoadingOverlay.Tips.Length, Is.GreaterThanOrEqualTo(3));
        Assert.That(LoadingOverlay.Tips.Any(t => t.Contains("강한 적")), Is.False);
        Assert.That(LoadingOverlay.NextTip(), Is.Not.Empty);
    }

    // 9 — P' = max(0, P-E), E' = max(0, E-P).
    [TestCase(300f, 120f, 180f, 0f)]
    [TestCase(80f, 200f, 0f, 120f)]
    [TestCase(150f, 150f, 0f, 0f)]
    [TestCase(1f, 0f, 1f, 0f)]
    public void TurretCollisionExchangesRemainingHp(float p, float e, float pAfter, float eAfter)
    {
        var (player, boss) = NoryangjinTurretEvent.ExchangeHp(p, e);
        Assert.That(player, Is.EqualTo(pAfter).Within(1e-4));
        Assert.That(boss, Is.EqualTo(eAfter).Within(1e-4));
    }

    // 10 — effective scale = test base × X1/X2 × section slow-down; highway 1.4 forward speed then reads 2.8.
    [Test] public void GameSpeedComposesMultipliers()
    {
        Assert.That(GameSpeed.Compose(1f, 2f, 1f), Is.EqualTo(2f));
        Assert.That(GameSpeed.Compose(1f, 2f, .2f), Is.EqualTo(.4f).Within(1e-5));
        Assert.That(GameSpeed.Compose(3f, 1f, 1f), Is.EqualTo(3f));
        Assert.That(1.4f * GameSpeed.Compose(1f, GameSpeed.Fast, 1f), Is.EqualTo(2.8f).Within(1e-5));
        Assert.That(GameSpeedToggle.Caption(true), Is.EqualTo("X2"));
        Assert.That(GameSpeedToggle.Caption(false), Is.EqualTo("X1"));
    }

    // 10 — X2 is remembered but only speeds up a live run (lobby/menus stay at 1x).
    [Test] public void GameSpeedOnlyAppliesDuringRuns()
    {
        float before = Time.timeScale;
        try
        {
            GameSpeed.SetUserScale(GameSpeed.Fast);
            GameSpeed.SetRunning(false);
            Assert.That(GameSpeed.Effective, Is.EqualTo(GameSpeed.TestBaseScale));
            GameSpeed.SetRunning(true);
            Assert.That(GameSpeed.Effective, Is.EqualTo(GameSpeed.TestBaseScale * 2f));
        }
        finally { GameSpeed.SetRunning(false); GameSpeed.SetUserScale(GameSpeed.Normal); Time.timeScale = before; }
    }

    // 10 — highway open road: 1.4x at X1, 2.8x at X2, back to the player's choice when the section ends.
    [Test] public void HighwayOpenRoadSectionIsOnePointFour()
    {
        float before = Time.timeScale;
        try
        {
            GameSpeed.SetRunning(true);
            GameSpeed.SetSectionScale(HighwayChapter2Controller.RushTimeScale);
            Assert.That(GameSpeed.Effective / GameSpeed.TestBaseScale, Is.EqualTo(1.4f).Within(1e-4));
            GameSpeed.SetUserScale(GameSpeed.Fast);
            Assert.That(GameSpeed.Effective / GameSpeed.TestBaseScale, Is.EqualTo(2.8f).Within(1e-4));
            GameSpeed.SetSectionScale(1f);
            Assert.That(GameSpeed.Effective / GameSpeed.TestBaseScale, Is.EqualTo(2f).Within(1e-4));
            GameSpeed.SetRunning(false);
            Assert.That(GameSpeed.Effective / GameSpeed.TestBaseScale, Is.EqualTo(1f).Within(1e-4));
        }
        finally { GameSpeed.SetSectionScale(1f); GameSpeed.SetRunning(false); GameSpeed.SetUserScale(GameSpeed.Normal); Time.timeScale = before; }
    }

    // 11 — fixed 20 s countdown and top-first shedding.
    [Test] public void ShutterCountdownIsTwentySeconds()
    {
        Assert.That(NoryangjinShutterEvent.CountdownFor(20f), Is.EqualTo(20f));
        Assert.That(NoryangjinShutterEvent.CountdownFor(0f), Is.EqualTo(20f));
    }

    [Test] public void BoxesShedFromTheTop()
    {
        var root = new GameObject("stack");
        try
        {
            var pieces = new[] { 0.4f, 1.6f, 0.4f, 1.0f, 1.6f }.Select((y, i) =>
            {
                var t = new GameObject("box" + i).transform; t.SetParent(root.transform, false); t.localPosition = new Vector3(i, y, 0); return t;
            }).ToArray();
            var order = NoryangjinBreakable.TopFirstOrder(pieces, root.transform);
            // Same visiting order as NoryangjinBreakable.ShedPieces: i goes Length-1 down to 0 as HP drops.
            var shed = Enumerable.Range(0, pieces.Length).Reverse().Select(i => pieces[order[i]].localPosition.y).ToArray();
            Assert.That(shed, Is.Ordered.Descending);
        }
        finally { Object.DestroyImmediate(root); }
    }

    // 13 — progress never moves back and only reaches 100% on completion.
    [Test] public void ChapterProgressIsMonotonicAndCapped()
    {
        Assert.That(ChapterRunProgress.Advance(.5f, .3f, false), Is.EqualTo(.5f));
        Assert.That(ChapterRunProgress.Advance(.5f, 1.2f, false), Is.EqualTo(ChapterRunProgress.BeforeFinishCap));
        Assert.That(ChapterRunProgress.Advance(.2f, .1f, true), Is.EqualTo(1f));
        Assert.That(ChapterRunProgress.Checkpoints(3, 3), Is.LessThan(1f));
        Assert.That(ChapterRunProgress.Checkpoints(0, 0), Is.EqualTo(0f));
        // RestStop: route starts 375 m behind the shark; the finish trigger is at 1430 m, not the 2100 m polyline end.
        Assert.That(ChapterRunProgress.Span(375f, 1430f, 375f), Is.EqualTo(0f));
        Assert.That(ChapterRunProgress.Span(375f, 1430f, 902.5f), Is.EqualTo(.5f).Within(1e-4));
        Assert.That(ChapterRunProgress.Span(375f, 1430f, 2100f), Is.EqualTo(1f));
        var run = new ChapterRunProgress();
        run.Update(new ChapterRunProgress.Reading { value = 375f, goal = 1430f }, false);
        Assert.That(run.Value, Is.EqualTo(0f));
        run.Update(new ChapterRunProgress.Reading { value = 1430f, goal = 1430f }, false);
        Assert.That(run.Value, Is.EqualTo(ChapterRunProgress.BeforeFinishCap));
        run.Update(new ChapterRunProgress.Reading { value = 600f, goal = 1430f }, false); // never moves back
        Assert.That(run.Value, Is.EqualTo(ChapterRunProgress.BeforeFinishCap));
        Assert.That(run.Complete(), Is.EqualTo(1f));
    }

    // 15 — stable, bounded variation; attack timing is never varied.
    [Test] public void ActorVariationIsStableAndBounded()
    {
        for (int seed = -500; seed < 500; seed += 37)
        {
            Assert.That(ActorVariation.SpeedFactor(seed), Is.InRange(ActorVariation.MinSpeed, ActorVariation.MaxSpeed));
            Assert.That(ActorVariation.ScaleFactor(seed), Is.InRange(ActorVariation.MinScale, ActorVariation.MaxScale));
            Assert.That(ActorVariation.SpeedFactor(seed), Is.EqualTo(ActorVariation.SpeedFactor(seed)));
        }
        Assert.That(Enumerable.Range(0, 20).Select(ActorVariation.SpeedFactor).Distinct().Count(), Is.GreaterThan(10));
    }

    // 2 — a row opens only after the shark crosses it untouched; the nearest lane member collides.
    [Test] public void EnemyRowCrossingRules()
    {
        Assert.That(EnemyRowGate.Crossed(2f, -.1f), Is.True);
        Assert.That(EnemyRowGate.Crossed(float.NaN, -.1f), Is.False);
        Assert.That(EnemyRowGate.Crossed(20f, -.1f), Is.False); // a turn re-projected a far row, not a crossing
        Assert.That(EnemyRowGate.Crossed(-1f, -2f), Is.False);
        Assert.That(EnemyRowGate.NearestLane(new[] { -2.5f, .4f, 3f }), Is.EqualTo(1));
        Assert.That(EnemyRowGate.IsRow(2, 4f, 5f), Is.True);
        Assert.That(EnemyRowGate.IsRow(2, 2f, 9f), Is.False); // a column, not a row
        Assert.That(EnemyRowGate.IsRow(1, 4f, 4f), Is.False);
        Assert.That(EnemyRowGate.IsCloseCombat(EnemyEventMode.AttackLoop), Is.True);
        Assert.That(EnemyRowGate.IsCloseCombat(EnemyEventMode.Shoot), Is.False);
        Assert.That(EnemyRowGate.IsCloseCombat(EnemyEventMode.AmbushMoveThenShoot), Is.False);
    }

    // 3, 4, 14 — saved chapter scenes: no sensitivity row, separated bottom buttons, every character atlas matches its mesh.
    [TestCase("Noryangjin_MapTool_Mode_SR18_Revamp")]
    [TestCase("HighWay")]
    [TestCase("RestStop")]
    [TestCase("Jamsil")]
    [TestCase("ShoeTower")]
    public void ChapterSceneSettingsAndCharacterAtlases(string name)
    {
        string path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
        var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var settings = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HarborSettingsPanel>(true)).Single();
            var panel = settings.transform.Find("Panel");
            foreach (var row in new[] { "Sensitivity", "SensitivityLabel", "SensitivityIcon" })
                Assert.That(panel.Find(row).gameObject.activeSelf, Is.False, row);
            var rects = new[] { "PrivacyPolicy", "Terms", "AdPrivacyOptions" }.Select(n => (RectTransform)panel.Find(n)).ToArray();
            for (int i = 0; i + 1 < rects.Length; i++)
                Assert.That(rects[i].anchorMin.y - rects[i + 1].anchorMax.y, Is.GreaterThanOrEqualTo(.015f), rects[i].name);
            var login = (RectTransform)panel.Find("GooglePlayAccount"); var delete = (RectTransform)panel.Find("DeleteGameAccount");
            Assert.That(delete.anchorMin.x - login.anchorMax.x, Is.GreaterThanOrEqualTo(.05f));
            Assert.That(rects.Last().anchorMin.y, Is.GreaterThan(((RectTransform)panel.Find("AccountStatus")).anchorMax.y));
            Assert.That(login.anchorMin.y, Is.GreaterThan(((RectTransform)panel.Find("RunActions")).anchorMax.y));
            Assert.That(CharacterTextureMismatchRepair.Scan(scene), Is.Empty);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
#endif
