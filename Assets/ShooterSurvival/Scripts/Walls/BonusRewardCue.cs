using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    /// <summary>Distance-bounded pickup glow and a burst only after a successful claim.</summary>
    [DisallowMultipleComponent]
    public sealed class BonusRewardCue : MonoBehaviour
    {
        public GameObject nearbyGlow, pickupBurst;
        private Transform player;
        private bool claimed;
        private float nextCheck;
        private void OnEnable(){claimed=false;nextCheck=0;}
        private void Update()
        {
            if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.25f;
            if(player==null)player=FindFirstObjectByType<PlayerScript>()?.transform;
            bool visible=!claimed&&player!=null&&(player.position-transform.position).sqrMagnitude<2500;
            if(nearbyGlow!=null&&nearbyGlow.activeSelf!=visible)nearbyGlow.SetActive(visible);
        }
        public void PlayPickup()
        {
            if(claimed)return;claimed=true;
            if(nearbyGlow!=null)nearbyGlow.SetActive(false);
            if(pickupBurst==null)return;
            if(player==null)player=FindFirstObjectByType<PlayerScript>()?.transform;
            var origin=player!=null?player.position:transform.position;
            var burst=Instantiate(pickupBurst,origin+Vector3.up*1.65f,Quaternion.identity);
            Destroy(burst,2.5f);
        }
        private void OnDisable(){if(nearbyGlow!=null)nearbyGlow.SetActive(false);}
    }
}
