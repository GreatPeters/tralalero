using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyOpeningBest4
{
    public static object Main()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) throw new InvalidOperationException("Unpaused Play Mode required");
        const string folder = "map-concepts/opening-best4-v5-2026-09-23/";
        const string previews = "tmp/image-previews/opening-best4-v5-2026-09-23/";
        var story = UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);
        story.Open();
        var events = new List<string>();
        int phase = 0, captured = 0;
        double deadline = EditorApplication.timeSinceStartup + 100;
        EditorApplication.CallbackFunction tick = null;
        void Require(bool valid, string message) { if (!valid) throw new Exception(message); }
        void Clean()
        {
            Require(!story.gameObject.activeSelf && !story.IsMoviePlaying && story.movieDisplay.texture == null && story.video.targetTexture == null,
                "Playback resources not released");
        }
        bool Ready(int page) => story.IsMoviePlaying && !story.IsSeeking && story.CurrentPage == page &&
            story.video.time >= OpeningStoryUI.GetMoviePageStart(page) + .25 && story.video.time < OpeningStoryUI.GetMoviePageStart(page) + 3;
        tick = () =>
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Opening verification timed out at phase " + phase);
                if (phase == 0)
                {
                    if (story.IsMoviePlaying)
                    {
                        Require(OpeningStoryUI.IsBlockingGameplay && !TimeManager.isGameRunning, "Opening did not block gameplay");
                        var rect = story.movieDisplay.rectTransform.rect;
                        var slot = ((RectTransform)story.movieDisplay.transform.parent).rect;
                        Require(Mathf.Abs(rect.width / rect.height - 720f / 1280f) < .001f,
                            "Movie aspect ratio is distorted");
                        Require(Mathf.Min(1, slot.width / rect.width) * Mathf.Min(1, slot.height / rect.height) >= .849f,
                            "Movie exceeds the existing 15% frame crop limit");
                        if (captured < 4 && story.video.time >= OpeningStoryUI.GetMoviePageStart(captured) + 2)
                        {
                            Require(story.CurrentPage == captured, "Natural scene/caption boundary mismatch");
                            Require(!story.artwork.gameObject.activeSelf, "Static art overlaps movie");
                            ScreenCapture.CaptureScreenshot(previews + "native-0" + (captured + 1) + ".png");
                            events.Add("natural-page-" + captured); captured++;
                        }
                    }
                    if (captured == 4 && !story.gameObject.activeSelf)
                    { Clean(); events.Add("natural-end-cleanup"); story.Open(); phase = 1; }
                }
                else if (phase == 1 && Ready(0))
                { Require(!story.previousButton.interactable, "Previous enabled at first page"); story.Next(); phase = 2; }
                else if (phase == 2 && Ready(1)) { events.Add("next-8s"); story.Next(); phase = 3; }
                else if (phase == 3 && Ready(2)) { events.Add("next-20s"); story.Next(); phase = 4; }
                else if (phase == 4 && Ready(3)) { events.Add("next-30s"); story.Previous(); phase = 5; }
                else if (phase == 5 && Ready(2)) { events.Add("previous-20s"); story.Previous(); phase = 6; }
                else if (phase == 6 && Ready(1)) { events.Add("previous-8s"); story.Previous(); phase = 7; }
                else if (phase == 7 && Ready(0))
                { events.Add("previous-0s"); story.Skip(); Clean(); events.Add("skip-cleanup"); story.Open(); phase = 8; }
                else if (phase == 8 && Ready(0))
                {
                    events.Add("reopen-after-skip"); story.Skip(); Clean();
                    var result = new { passed = true, frames = story.movie.frameCount, events };
                    var json = (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
                        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new object[] { result });
                    File.WriteAllText(folder + "playback.json", json);
                    EditorApplication.update -= tick;
                }
            }
            catch (Exception error)
            {
                EditorApplication.update -= tick; story.Skip();
                File.WriteAllText(folder + "playback-error.txt", error.ToString());
            }
        };
        EditorApplication.update += tick;
        return "Checking four natural scenes, next/previous boundaries, skip, reopen and cleanup";
    }
}
