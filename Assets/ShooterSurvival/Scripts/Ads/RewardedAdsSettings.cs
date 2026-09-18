using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival.Ads
{
    [CreateAssetMenu(menuName = "Shooter Survival/Rewarded Ads Settings")]
    public sealed class RewardedAdsSettings : ScriptableObject
    {
        public bool useTestAds = true;
        public string androidRewardedUnitId;
        public string iosRewardedUnitId;
        public bool underAgeOfConsent;
        [Min(0)] public int minimumBonusCoins = 20;
        [Min(1)] public int maximumBonusCoins = 500;
        public const string ResourcePath = "Ads/RewardedAdsSettings";
        // Pinned GoogleMobileAds11.5.0 RewardedAdController sample IDs.
        public string UnitId
        {
            get
            {
#if UNITY_IOS
                return useTestAds ? "ca-app-pub-3940256099942544/1712485313" : iosRewardedUnitId;
#else
                return useTestAds ? "ca-app-pub-3940256099942544/5224354917" : androidRewardedUnitId;
#endif
            }
        }
    }
}
