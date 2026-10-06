using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

// A visual arm link for an authored adjacent pair. RoleAction continues to own
// each person's movement and contact; this component only adjusts one arm.
[DefaultExecutionOrder(245)]
public sealed class Chapter45CouplePose : MonoBehaviour
{
    public Chapter45RoleAction follower, leader;
    public Animator animator;
    public Transform upper, lower, hand, partnerElbow;
    private void LateUpdate()
    {
        if (follower == null || leader == null || !follower.CanAct || !leader.CanAct || animator == null || !animator.enabled) return;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("die")) return;
        Vector3 target = partnerElbow.position + leader.transform.forward * .06f;
        float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);
        Vector3 v=target-upper.position;
        if(v.sqrMagnitude<.00001f||a<.001f||b<.001f)return;
        float d=Mathf.Clamp(v.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);
        Vector3 axis=v.normalized,bend=Vector3.ProjectOnPlane(lower.position-upper.position,axis).normalized;
        if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(-upper.forward,axis).normalized;
        float along=(a*a+d*d-b*b)/(2*d);
        Vector3 elbow=upper.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
    }
}
