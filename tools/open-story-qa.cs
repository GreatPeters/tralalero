if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required");
UnityEditor.EditorApplication.isPaused = false;
OpeningStoryUI.Instance.Open();
return new { active = OpeningStoryUI.IsBlockingGameplay, moviePrepared = OpeningStoryUI.Instance.video.isPrepared };
