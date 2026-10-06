using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

// Optional visual double-take on selected authored adults. Combat owns all
// timing, movement and damage. This changes only the head bone after animation.
[DefaultExecutionOrder(250)]
public sealed class Chapter45ComicReaction : MonoBehaviour
{
    public Chapter45RoleAction role;
    public Animator animator;
    public Transform head;
    public float personality = 1;
    public float VisibleReaction { get; private set; }
    private Chapter45RoleAction.ActionPhase previousPhase;
    private float age = 10;
    private Quaternion basePose, appliedPose;
    private bool applied;

    public static Vector3 DoubleTake(float progress, float amount)
    {
        float t = Mathf.Clamp01(progress);
        float envelope = Mathf.Sin(t * Mathf.PI);
        return new Vector3(-8 * Mathf.Sin(t * Mathf.PI * 2),
            27 * Mathf.Sin(t * Mathf.PI * 3), 13 * Mathf.Sin(t * Mathf.PI * 2))
            * envelope * amount;
    }

    private void OnEnable()
    {
        age = 10;
        previousPhase = role != null ? role.Phase : Chapter45RoleAction.ActionPhase.Moving;
        applied = false;
        VisibleReaction = 0;
    }

    private void Restore()
    {
        // Do not overwrite a fresh animator pose or another component's work.
        if (applied && head != null && Quaternion.Angle(head.localRotation, appliedPose) < .01f)
            head.localRotation = basePose;
        applied = false;
        VisibleReaction = 0;
    }

    private void LateUpdate()
    {
        if (head == null || role == null || animator == null) { Restore(); return; }
        // Preserve the exact visible pose during a pause or transfer.
        if (!TimeManager.isGameRunning || TimeManager.timeFactor <= 0 || !animator.enabled) return;
        if (!role.CanAct) { Restore(); return; }
        if (role.Phase == Chapter45RoleAction.ActionPhase.Recovering && previousPhase != role.Phase)
            age = 0;
        previousPhase = role.Phase;
        age += Time.deltaTime * Mathf.Max(0, TimeManager.timeFactor);
        Restore();
        if (role.Phase != Chapter45RoleAction.ActionPhase.Recovering || age >= 1.15f) return;
        basePose = head.localRotation;
        float amount = Mathf.Clamp(personality, .5f, 1.15f);
        Vector3 offset = DoubleTake(age / 1.15f, amount);
        head.localRotation = basePose * Quaternion.Euler(offset);
        appliedPose = head.localRotation;
        applied = true;
        VisibleReaction = offset.magnitude;
    }

    private void OnDisable() { Restore(); }
}
