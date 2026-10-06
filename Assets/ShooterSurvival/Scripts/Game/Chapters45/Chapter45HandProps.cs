using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

// Visual attachment for the newly authored Generic rigs. It never moves the
// actor root, changes colliders, or applies damage. The role owns those systems.
[DefaultExecutionOrder(240)]
public sealed class Chapter45HandProps : MonoBehaviour
{
    public enum Style { Palm, Basketball, TwoHandTool, Phone, ShoppingBag, Leaflets }
    public Style style;
    public Animator animator;
    public Transform actor, rightHand, rightForearm, leftUpperArm, leftForearm, leftHand, prop;
    public Quaternion gripRotationOffset = Quaternion.identity;
    public Vector3 gripOffset;
    public float secondaryGripSpacing = .09f;
    public bool useLeftHand;
    private Chapter45RoleAction phoneRole;
    private Transform phoneRightUpperArm, phoneHead;
    private Quaternion phoneHeadRest;
    private bool phoneRigCached;

    private void Awake() { if (style == Style.Phone) CachePhoneRig(); }
    private void CachePhoneRig()
    {
        if (phoneRigCached || actor == null || animator == null) return;
        phoneRole = actor.GetComponent<Chapter45RoleAction>();
        phoneRightUpperArm = rightForearm != null ? rightForearm.parent : null;
        foreach (var bone in animator.GetComponentsInChildren<Transform>(true))
            if (bone.name == "Head") { phoneHead = bone; break; }
        if (phoneHead != null) phoneHeadRest = Quaternion.Inverse(actor.rotation) * phoneHead.rotation;
        // A colored case separates the small handset from dark hair and hands.
        // Per-renderer override leaves shared materials and other props intact.
        foreach (var renderer in prop.GetComponentsInChildren<Renderer>())
            if (renderer.name == "Phone casing")
            {
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                var caseColor = new Color(.035f, .62f, .90f, 1);
                block.SetColor("_Color", caseColor); block.SetColor("_BaseColor", caseColor);
                renderer.SetPropertyBlock(block);
            }
        // A19.4cm landscape case remains hand-sized; character/camera scale is unchanged.
        prop.localScale = Vector3.Scale(prop.localScale, new Vector3(1.15f, 1.25f, 1));
        phoneRigCached = true;
    }

    public void ApplyPose()
    {
        if (animator == null || prop == null || rightHand == null) return;
        var hand = useLeftHand && leftHand != null ? leftHand : rightHand;
        var forearm = useLeftHand && leftForearm != null ? leftForearm : rightForearm;
        Vector3 palm = hand.position + (hand.position - forearm.position).normalized * .055f;
        prop.SetPositionAndRotation(palm + hand.TransformVector(gripOffset), hand.rotation * gripRotationOffset);
        var state = Application.isPlaying && animator.isInitialized ? animator.GetCurrentAnimatorStateInfo(0) : default;
        if (state.IsName("die")) return;
        if (style == Style.TwoHandTool && leftUpperArm != null && (state.IsName("attack_once") || state.IsName("attack_loop") || state.IsName("mop")))
        {
            Vector3 target = prop.position + prop.up * secondaryGripSpacing;
            SolveArm(leftUpperArm, leftForearm, leftHand, target);
        }
        else if (style == Style.Basketball)
        {
            bool dribbling = state.IsName("walk") || state.IsName("dribble");
            float phase = Mathf.Repeat(state.normalizedTime * 4, 1);
            float bounce = dribbling ? Mathf.Sin(phase * Mathf.PI) : 0;
            Vector3 p = palm + actor.forward * .12f;
            p.y = Mathf.Lerp(palm.y - .16f, actor.position.y + .215f, bounce * bounce);
            prop.position = p;
        }
        else if (style == Style.Phone)
        {
            CachePhoneRig();
            float raised = phoneRole != null ? phoneRole.PhoneFilmingWeight : 0;
            Vector3 restingPhone = palm + actor.up * .035f + actor.forward * .025f;
            if (phoneHead != null && phoneRightUpperArm != null && leftUpperArm != null
                && leftHand != null && leftForearm != null && raised > 0)
            {
                // Both hands support an eye-level landscape handset. Its screen
                // faces the actor; the rear camera faces the approaching player.
                Vector3 filmingPhone = phoneHead.position + actor.forward * .20f + actor.up * .25f;
                Vector3 leftOffset = -actor.right * .075f - actor.up * .025f;
                Vector3 rightOffset = actor.right * .075f - actor.up * .025f;
                // Fit the shared handset to both existing arm lengths. In
                // particular the shorter rig must not reach for a floating prop.
                for (int i = 0; i < 3; i++)
                {
                    filmingPhone = ReachablePhoneCenter(filmingPhone, leftOffset, leftUpperArm, leftForearm, leftHand);
                    filmingPhone = ReachablePhoneCenter(filmingPhone, rightOffset, phoneRightUpperArm, rightForearm, rightHand);
                }
                Vector3 leftGrip = filmingPhone + leftOffset;
                Vector3 rightGrip = filmingPhone + rightOffset;
                SolveArm(leftUpperArm, leftForearm, leftHand, Vector3.Lerp(leftHand.position, leftGrip, raised));
                SolveArm(phoneRightUpperArm, rightForearm, rightHand, Vector3.Lerp(rightHand.position, rightGrip, raised));
                phoneHead.rotation = Quaternion.Slerp(phoneHead.rotation, actor.rotation * phoneHeadRest, raised * .8f);
                prop.position = Vector3.Lerp(restingPhone, filmingPhone, raised);
            }
            else prop.position = restingPhone;
            prop.rotation = actor.rotation * Quaternion.Euler(25 * (1 - raised), 0, 90 * raised);
        }
        else if (style == Style.Leaflets)
        {
            // The lower edge sits in the open hand; keep the fan readable while
            // the opposite hand performs the authored throwing gesture.
            prop.position = palm + actor.up * .075f + actor.forward * .02f;
            prop.rotation = actor.rotation;
        }
        else if (style == Style.ShoppingBag)
        {
            // A bag hangs vertically below the wrist instead of swinging through
            // the body when an animation twists the hand.
            prop.rotation = Quaternion.LookRotation(actor.forward, Vector3.up);
        }
    }

    private void LateUpdate()
    {
        if (animator == null || !animator.enabled || !TimeManager.isGameRunning || TimeManager.timeFactor <= 0) return;
        ApplyPose();
    }

    private static Vector3 ReachablePhoneCenter(Vector3 center, Vector3 gripOffset, Transform upper, Transform lower, Transform hand)
    {
        float reach = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position) - .02f;
        Vector3 grip = center + gripOffset;
        return upper.position + Vector3.ClampMagnitude(grip - upper.position, Mathf.Max(.001f, reach)) - gripOffset;
    }

    private static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target)
    {
        float a = Vector3.Distance(upper.position, lower.position);
        float b = Vector3.Distance(lower.position, hand.position);
        var direction = target - upper.position;
        float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
        if (direction.sqrMagnitude < .00001f || a < .001f || b < .001f) return;
        Vector3 axis = direction.normalized;
        Vector3 bend = Vector3.ProjectOnPlane(lower.position - upper.position, axis).normalized;
        if (bend.sqrMagnitude < .01f) bend = Vector3.ProjectOnPlane(-upper.forward, axis).normalized;
        float along = (a * a + distance * distance - b * b) / (2 * distance);
        Vector3 elbow = upper.position + axis * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
        upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
    }
}
