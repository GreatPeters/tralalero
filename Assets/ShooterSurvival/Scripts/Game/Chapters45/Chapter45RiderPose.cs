using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

// Anchors the generated rider to the generated vehicle. Hazard movement,
// warnings, collision and damage remain on the original Chapter45Hazard.
[DefaultExecutionOrder(250)]
public sealed class Chapter45RiderPose : MonoBehaviour
{
    public Chapter45Director director;
    public Animator animator;
    public Transform rider, hips, seat, leftGrip, rightGrip, leftFootTarget, rightFootTarget;
    public Transform leftArm, leftForearm, leftHand, rightArm, rightForearm, rightHand;
    public Transform leftThigh, leftShin, leftFoot, rightThigh, rightShin, rightFoot;
    private void Update()
    {
        if (animator == null) return;
        animator.enabled = director != null && director.Running && TimeManager.isGameRunning && TimeManager.timeFactor > 0;
        animator.speed = Mathf.Max(0, TimeManager.timeFactor);
    }
    public void ApplyPose()
    {
        if (seat != null) rider.position += seat.position - hips.position;
        Solve(leftArm, leftForearm, leftHand, leftGrip.position);
        Solve(rightArm, rightForearm, rightHand, rightGrip.position);
        leftHand.rotation = leftGrip.rotation; rightHand.rotation = rightGrip.rotation;
        if (leftFootTarget != null) Solve(leftThigh, leftShin, leftFoot, leftFootTarget.position);
        if (rightFootTarget != null) Solve(rightThigh, rightShin, rightFoot, rightFootTarget.position);
        if (leftFootTarget != null) leftFoot.rotation = leftFootTarget.rotation;
        if (rightFootTarget != null) rightFoot.rotation = rightFootTarget.rotation;
    }
    private void LateUpdate() { if (animator != null && animator.enabled) ApplyPose(); }
    private static void Solve(Transform upper, Transform lower, Transform end, Vector3 target)
    {
        float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, end.position);
        Vector3 v = target - upper.position;
        if (v.sqrMagnitude < .00001f || a < .001f || b < .001f) return;
        float distance = Mathf.Clamp(v.magnitude, Mathf.Abs(a-b)+.001f, a+b-.001f);
        Vector3 axis=v.normalized, bend=Vector3.ProjectOnPlane(lower.position-upper.position,axis).normalized;
        if(bend.sqrMagnitude<.01f) bend=Vector3.ProjectOnPlane(-upper.forward,axis).normalized;
        float along=(a*a+distance*distance-b*b)/(2*distance);
        Vector3 elbow=upper.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(end.position-lower.position,target-lower.position)*lower.rotation;
    }
}
