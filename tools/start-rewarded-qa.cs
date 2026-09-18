var defeat=UnityEngine.Object.FindFirstObjectByType<DefeatPresentation>();
if(defeat==null)throw new System.InvalidOperationException("Visible defeat offer required");
UnityEditor.EditorApplication.isPaused=false;
System.IO.File.WriteAllText("tmp/qa-proof/chapter-ui-final-v3/ad-before.txt",MoneyScript.S.Coin.ToString());
defeat.WatchRewardedVideo();
return new{coins=MoneyScript.S.Coin,showing=IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsService.Instance.Showing};
