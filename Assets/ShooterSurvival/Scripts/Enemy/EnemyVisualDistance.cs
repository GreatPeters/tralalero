using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Cull distant enemy artwork without disabling movement, triggers or fired shots.
    [DefaultExecutionOrder(-200)]
    public sealed class EnemyVisualDistance : MonoBehaviour
    {
        private Transform player;
        private Renderer[] renderers;
        private bool[] originalHidden;
        private float nextCheck;
        public bool IsRelevant { get; private set; } = true;
        private void Awake()
        {
            renderers=GetComponentsInChildren<Renderer>(true);originalHidden=new bool[renderers.Length];
            for(int i=0;i<renderers.Length;i++)originalHidden[i]=renderers[i].forceRenderingOff;
        }
        private void OnEnable(){nextCheck=0;}
        private void Update()
        {
            if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.15f;
            if(player==null){var target=FindFirstObjectByType<PlayerScript>();if(target!=null)player=target.transform;}
            if(player==null)return;
            float distance=IsRelevant?85f:75f;
            bool relevant=(player.position-transform.position).sqrMagnitude<=distance*distance;
            if(relevant==IsRelevant)return;
            IsRelevant=relevant;
            for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].forceRenderingOff=!relevant||originalHidden[i];
        }
        private void OnDisable()
        {
            if(renderers!=null)for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].forceRenderingOff=originalHidden[i];
            IsRelevant=true;
        }
    }
}
