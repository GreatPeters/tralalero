var opening = OpeningStoryUI.Instance;
var ads = IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsService.Instance;
return new { paused = UnityEditor.EditorApplication.isPaused, time = UnityEngine.Time.time, frame = UnityEngine.Time.frameCount,
    running = IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning, test = Sr18ProgressionPlaytest.Status(),
    opening = opening != null ? new { active = opening.gameObject.activeInHierarchy, movie = opening.IsMoviePlaying, videoTime = opening.video.time, title = opening.titleText.text } : null,
    ads = ads != null ? new { ads.Ready, ads.Showing, ads.Loading, ads.Status } : null,
    roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(g=>g.name).ToArray() };
