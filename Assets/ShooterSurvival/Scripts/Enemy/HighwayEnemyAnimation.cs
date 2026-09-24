using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // A short additive reaction does not restart an in-flight throw wind-up.
    [DefaultExecutionOrder(190)]
    public sealed class HighwayEnemyAnimation : MonoBehaviour
    {
        public Animator animator;
        public Transform chest, head;
        public float deathSeconds=1.05f;
        public float releaseNormalizedTime=.46f;
        float recoil, cooldown;
        float normalSpeed=1f;
        bool retimed;
        EnemyEventController events;
        EnemyScript_space combat;
        void Awake(){events=GetComponent<EnemyEventController>();combat=GetComponent<EnemyScript_space>();}
        void OnEnable(){RestoreSpeed();recoil=0;cooldown=0;}
        void OnDisable()=>RestoreSpeed();
        public void PrepareThrow(float delay)
        {
            if(animator==null||animator.runtimeAnimatorController==null)return;
            RestoreSpeed();
            foreach(var clip in animator.runtimeAnimatorController.animationClips)
            {
                if(clip.name!="attack_once"&&!clip.name.EndsWith("|attack_once"))continue;
                normalSpeed=animator.speed;retimed=true;
                if(delay>0)animator.speed=clip.length*Mathf.Clamp01(releaseNormalizedTime)/delay;
                else{animator.Play(ForwardEnemyAnimationContract.AttackOnce,0,releaseNormalizedTime);animator.Update(0);}
                break;
            }
        }
        void RestoreSpeed(){if(retimed&&animator!=null)animator.speed=normalSpeed;retimed=false;}
        public void ReactToHit()
        {
            if(Time.time<cooldown)return;
            cooldown=Time.time+.12f;recoil=.2f;
        }
        void LateUpdate()
        {
            if(events!=null&&events.RuntimeState==EnemyEventRuntimeState.Dead){RestoreSpeed();return;}
            if(!TimeManager.isGameRunning||animator==null||!animator.enabled||events!=null&&events.RuntimeState==EnemyEventRuntimeState.Dead)return;
            if(recoil>0)
            {
                recoil=Mathf.Max(0,recoil-Time.deltaTime*TimeManager.timeFactor);
                float weight=Mathf.Sin(recoil/.2f*Mathf.PI);
                if(chest!=null)chest.localRotation*=Quaternion.Euler(-9*weight,0,3*weight);
                if(head!=null)head.localRotation*=Quaternion.Euler(-5*weight,0,0);
            }
            if(combat!=null&&combat.ReleaseQueuedHighwayProjectile())RestoreSpeed();
        }
    }
}
