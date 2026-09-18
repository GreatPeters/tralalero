using System.Linq;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    // Humanoid look-at keeps authored throws/carrying on the body and arms while
    // the head continues acknowledging the player through the wind-up.
    public sealed class EnemyLookAtTarget : MonoBehaviour
    {
        private Animator animator;
        private EnemyEventController owner;
        private EnemyGroundedPose grounding;
        private PlayerScript player;
        private Transform playerHead;
        private void Awake()
        {
            animator=GetComponent<Animator>();owner=GetComponentInParent<EnemyEventController>();
            grounding=owner!=null?owner.GetComponent<EnemyGroundedPose>():null;
            player=FindFirstObjectByType<PlayerScript>();
        }
        private void OnAnimatorIK(int layerIndex)
        {
            if(animator==null||!animator.isHuman||player==null||owner==null)return;
            if(owner.RuntimeState==EnemyEventRuntimeState.Dead){animator.SetLookAtWeight(0);return;}
            if(playerHead==null&&player.sharkAnim!=null)
                playerHead=player.sharkAnim.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="head");
            Vector3 target=playerHead!=null?playerHead.position:player.transform.position+Vector3.up;
            // The visual grounding pass follows animation; compensate its previous
            // frame lift so a crouching throw also aims down to the real player.
            if(grounding!=null)target-=Vector3.up*grounding.AppliedLift;
            animator.SetLookAtWeight(1f,0f,1f,1f,.4f);
            animator.SetLookAtPosition(target);
        }
    }
}
