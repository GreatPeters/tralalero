if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required");
UnityEditor.EditorApplication.isPaused = false;
var opening = OpeningStoryUI.Instance;
opening.Open();
const string path = "tmp/qa-proof/opening-settled.png";
double deadline = UnityEditor.EditorApplication.timeSinceStartup + 15;
UnityEditor.EditorApplication.CallbackFunction capture = null;
capture = () =>
{
    if (opening == null || UnityEditor.EditorApplication.timeSinceStartup > deadline)
    {
        UnityEditor.EditorApplication.update -= capture;
        System.IO.File.WriteAllText(path + ".error.txt", "Opening capture timed out");
        return;
    }
    if (!opening.video.isPrepared || opening.video.time < .4 || opening.pageFade.alpha < .99f) return;
    UnityEditor.EditorApplication.update -= capture;
    opening.video.Pause();
    try
    {
        ChapterVisualProbe.Capture(path);
        System.IO.File.WriteAllLines(path + ".txt", new[] {
            "videoTime=" + opening.video.time, "page=" + opening.CurrentPage, "captionAlpha=" + opening.pageFade.alpha,
            "titleColor=" + opening.titleText.color, "shader=" + opening.titleText.fontSharedMaterial.shader.name,
            "faceColor=" + opening.titleText.fontSharedMaterial.GetColor("_FaceColor") });
    }
    catch (System.Exception error) { System.IO.File.WriteAllText(path + ".error.txt", error.ToString()); }
};
UnityEditor.EditorApplication.update += capture;
return new { queued = true, path };
