using UnityEngine;

// Plays one looping state on a decorative crowd member (auction bidders, hose workers) with a
// random start offset so a crowd does not move in lockstep.
public sealed class NoryangjinCrowdPose : MonoBehaviour
{
    public string state = "idle";
    public float speed = 1;
    private void OnEnable()
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator == null) return;
        animator.speed = speed;
        int hash = Animator.StringToHash(state);
        if (animator.HasState(0, hash)) animator.Play(hash, 0, Random.value);
    }
}
