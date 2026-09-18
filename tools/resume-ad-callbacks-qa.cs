GoogleMobileAds.Common.MobileAdsEventExecutor.Initialize();
OpeningStoryUI.Instance?.Skip();
UnityEditor.EditorApplication.isPaused = false;
return new { pumpActive = GoogleMobileAds.Common.MobileAdsEventExecutor.IsActive() };
