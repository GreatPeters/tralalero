if (!UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isPaused) throw new System.InvalidOperationException("Unpaused Play Mode required");
var opening = UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(UnityEngine.FindObjectsInactive.Include);
opening.Open();
const string folder = "map-concepts/flow-opening-2026-09-12/installed/";
const string previews = "tmp/image-previews/opening-flow-installed-fit/";
System.IO.Directory.CreateDirectory(previews);
var events = new System.Collections.Generic.List<string>();
int phase = 0; double deadline = UnityEditor.EditorApplication.timeSinceStartup + 45;
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () => {
    try {
        if (UnityEditor.EditorApplication.timeSinceStartup > deadline) throw new System.TimeoutException("Opening playback verification");
        if (phase == 0 && opening.IsMoviePlaying && opening.video.time >= 2 && opening.video.time < 8) {
            if (opening.CurrentPage != 0) throw new System.Exception("First caption changed early");
            if (opening.movieDisplay.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode != UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent || opening.artwork.gameObject.activeSelf) throw new System.Exception("Movie full-frame fit or background failed");
            UnityEngine.ScreenCapture.CaptureScreenshot(previews + "01-in-game.png"); events.Add("first-shot-playing-page0"); phase++; return;
        }
        if (phase == 1 && opening.IsMoviePlaying && opening.video.time >= 9) {
            if (opening.CurrentPage != 1) throw new System.Exception("Natural8s transition mismatch");
            UnityEngine.ScreenCapture.CaptureScreenshot(previews + "02-in-game.png"); events.Add("natural8s-transition-page1"); phase++; return;
        }
        if (phase == 2 && System.IO.File.Exists(previews + "02-in-game.png")) { opening.Next(); phase++; return; }
        if (phase == 3 && opening.IsMoviePlaying && opening.video.frame >= 385 && opening.video.time < 20) {
            if (opening.CurrentPage != 2) throw new System.Exception("Next did not reach16s/page2");
            events.Add("next-seek16s-page2"); opening.Next(); phase++; return;
        }
        if (phase == 4 && opening.IsMoviePlaying && opening.video.frame >= 506) {
            if (opening.CurrentPage != 3) throw new System.Exception("Next did not reach21.04s/page3");
            events.Add("next-seek21.04s-page3"); phase++; return;
        }
        if (phase == 5 && !opening.gameObject.activeSelf) {
            if (opening.IsMoviePlaying || opening.movieDisplay.texture != null || opening.video.targetTexture != null) throw new System.Exception("Movie cleanup failed");
            events.Add("natural-end-closed-and-released");
            System.IO.File.WriteAllText(folder + "playback-fit.json", "{\"passed\":true,\"frames\":626,\"events\":[\"" + string.Join("\",\"",events) + "\"]}");
            UnityEditor.EditorApplication.update -= tick;
        }
    } catch (System.Exception error) {
        UnityEditor.EditorApplication.update -= tick; opening.Skip();
        System.IO.File.WriteAllText(folder + "playback-fit-error.txt", error.ToString());
    }
};
UnityEditor.EditorApplication.update += tick;
return "Verifying playback, natural caption transition, both Next seeks and end cleanup";
