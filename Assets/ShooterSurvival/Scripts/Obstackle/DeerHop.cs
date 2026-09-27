using UnityEngine;

// Procedural bounding gait for the water deer (Meshy auto-rigging only supports humanoids). One hop per
// stride of parent movement, so the gait follows the deer's speed: squash on landing, stretch in the air,
// nose-up take-off / nose-down landing and a small side sway. When the parent stops, the deer finishes
// its hop and breathes instead of freezing mid-air.
public sealed class DeerHop : MonoBehaviour
{
    public float hopHeight = .55f, strideLength = 1.8f, pitch = 16f, squash = .14f, sway = 4f;
    [Tooltip("World travel direction; zero keeps the authored local rotation.")]
    public Vector3 heading;
    private Vector3 lastParent, basePosition, baseScale;
    private Quaternion baseRotation;
    private float phase, breathe;

    private void Awake() { basePosition = transform.localPosition; baseRotation = transform.localRotation; baseScale = transform.localScale; }

    // Oncoming cars brake for the deer instead of driving through it.
    private void OnEnable()
    {
        OncomingLaneTraffic.Blockers.Add(transform);
        lastParent = transform.parent != null ? transform.parent.position : Vector3.zero;
    }
    private void OnDisable() => OncomingLaneTraffic.Blockers.Remove(transform);

    public static float Height(float phase, float hop) => Mathf.Abs(Mathf.Sin(phase * Mathf.PI)) * hop;

    private void LateUpdate()
    {
        if (transform.parent == null) return;
        var p = transform.parent.position; var step = p - lastParent; step.y = 0; lastParent = p;
        float moved = step.magnitude;
        bool moving = moved > .0005f;
        if (moving) phase += moved / Mathf.Max(.2f, strideLength);
        else phase = Mathf.MoveTowards(phase, Mathf.Ceil(phase), Time.deltaTime * 2.5f); // land, then stand
        breathe += Time.deltaTime;

        float frac = Mathf.Repeat(phase, 1);
        float air = Mathf.Sin(frac * Mathf.PI);        // 0 on the ground, 1 at the top of the hop
        float contact = 1 - Mathf.Clamp01(air * 3f);     // 1 around landing / take-off
        float idle = moving ? 0 : Mathf.Sin(breathe * 3f) * .02f;
        float sy = 1 - squash * contact + squash * .6f * air + idle;
        float sxz = 1 + squash * .5f * contact - squash * .25f * air;

        transform.localPosition = basePosition + Vector3.up * Height(phase, hopHeight);
        transform.localScale = Vector3.Scale(baseScale, new Vector3(sxz, sy, sxz));
        var rest = heading.sqrMagnitude > .01f ? Quaternion.LookRotation(new Vector3(heading.x, 0, heading.z)) : transform.parent.rotation * baseRotation;
        float nose = moving ? -Mathf.Cos(frac * Mathf.PI) * pitch : 0;
        transform.rotation = rest * Quaternion.Euler(nose, 0, moving ? Mathf.Sin(phase * Mathf.PI) * sway : 0);
    }
}
