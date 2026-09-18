if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required");
var opening = OpeningStoryUI.Instance;
var capture = ChapterVisualProbe.Capture("tmp/qa-proof/opening.png");
return new { capture, opening = opening != null, playingMovie = opening != null && opening.IsMoviePlaying,
    page = opening != null ? opening.CurrentPage : -1, time = opening != null && opening.video != null ? opening.video.time : -1 };
