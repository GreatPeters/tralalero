using UnityEngine;
using UnityEngine.UI;

namespace IndianOceanAssets.ShooterSurvival.Ads
{
    [RequireComponent(typeof(Button),typeof(CanvasGroup))]
    public sealed class RewardedPrivacyOptionsButton : MonoBehaviour
    {
        private Button button;
        private CanvasGroup visibility;
        private RewardedAdsService ads;
        private void Awake()
        {
            button=GetComponent<Button>();visibility=GetComponent<CanvasGroup>();
            button.onClick.AddListener(Show);
        }
        private void OnEnable()
        {
            ads=RewardedAdsService.Instance;
            if(ads!=null)ads.Changed+=Refresh;
            Refresh();
        }
        private void OnDisable() { if(ads!=null)ads.Changed-=Refresh; }
        private void Refresh()
        {
            bool needed=ads!=null&&ads.PrivacyOptionsRequired;
            if(button!=null)button.interactable=needed&&!ads.Showing;
            if(visibility!=null){visibility.alpha=needed?1:0;visibility.blocksRaycasts=needed;}
        }
        public void Show() { if(ads!=null&&!ads.Showing)ads.ShowPrivacyOptions(); }
    }
}
