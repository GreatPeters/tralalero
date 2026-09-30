using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Base for revamp events. The event's transform sits on the road centre with +Z pointing in the
// direction the shark travels on that stretch; triggers use the along-road distance to the shark.
public abstract class NoryangjinRevampEvent : MonoBehaviour
{
    [Tooltip("Starts when the shark is this many metres before the anchor.")]
    public float triggerAhead = 40;
    [TextArea] public string banner;
    public float bannerSeconds = 2.5f;
    public enum RouteScope { Any, Indoor, Outdoor }
    public RouteScope routeScope;
    [Tooltip("Large hazards reserve a quiet window. Zero is a non-hazard announcement/reward.")]
    public float hazardSeconds;

    protected NoryangjinRevampDirector Director { get; private set; }
    protected PlayerScript Player => Director != null ? Director.Player : null;
    public bool Triggered { get; private set; }

    public void Bind(NoryangjinRevampDirector director) => Director = director;

    public virtual void ResetForRun() => Triggered = false;

    public void Tick(float dt)
    {
        if (Player == null) return;
        if (routeScope != RouteScope.Any && (Director.Branch == null || !Director.Branch.Selected
            || Director.Branch.Outside != (routeScope == RouteScope.Outdoor))) return;
        if (!Triggered)
        {
            float ahead = Ahead(Player.transform.position);
            if (ahead <= triggerAhead && ahead > -2 && OnStretch(Player.transform.position)
                && (hazardSeconds <= 0 || Director.CanStartHazard))
            {
                Triggered = true;
                if (!string.IsNullOrEmpty(banner)) Director.Hud?.Publish(banner, bannerSeconds);
                Director.Timeline.Add($"{name} triggered at {Director.Elapsed:F1}");
                if (hazardSeconds > 0) Director.QuietFor(hazardSeconds);
                OnTriggered();
            }
        }
        if (Triggered) OnTick(dt);
    }

    protected abstract void OnTriggered();
    protected virtual void OnTick(float dt) { }

    public float Ahead(Vector3 p) => Vector3.Dot(transform.position - p, transform.forward);
    public float Lateral(Vector3 p) => Vector3.Dot(p - transform.position, transform.right);
    protected bool OnStretch(Vector3 p)
        => Mathf.Abs(Lateral(p)) < 7 && Mathf.Abs(p.y - transform.position.y) < 3
           && Vector3.Dot(Vector3.ProjectOnPlane(Player.transform.forward, Vector3.up).normalized, transform.forward) > .7f;

    // Along-road position of a point offset from the anchor.
    protected Vector3 At(float along, float lateral = 0, float up = 0)
        => transform.position + transform.forward * along + transform.right * lateral + Vector3.up * up;
}
