using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Ambient rest-stop people: idle until the (bigger) shark comes close, then panic and run away
// from the route. Purely visual; no colliders or rewards, matching the chapter-entry story beat.
public sealed class RestStopCivilian : MonoBehaviour
{
    public float noticeDistance = 11f, fleeSpeed = 4.5f, fleeDistance = 9f;
    [Tooltip("Looping state played before the shark arrives (e.g. \"signal\" for a road-work flagger).")]
    public string idleState = "idle";
    private Animator animator;
    private PlayerScript player;
    private Vector3 home;
    private Quaternion homeRotation;
    private float fled = -1;
    private bool settled;
    private Vector3 fleeDirection;
    private static readonly int Scared = Animator.StringToHash("scared"), Run = Animator.StringToHash("run"), Idle = Animator.StringToHash("idle");

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        player = FindFirstObjectByType<PlayerScript>();
        home = transform.position; homeRotation = transform.rotation;
    }

    private void OnEnable() => ResetCivilian();

    public void ResetCivilian()
    {
        fled = -1; settled = false; transform.SetPositionAndRotation(home, homeRotation);
        int rest = Animator.StringToHash(string.IsNullOrEmpty(idleState) ? "idle" : idleState);
        if (animator != null && animator.isActiveAndEnabled)
            animator.Play(animator.HasState(0, rest) ? rest : Idle, 0, Random.value);
    }

    private void Update()
    {
        if (player == null || !TimeManager.isGameRunning) return;
        if (player.currentHealth > 0 && player.movement && fled > 0 && (player.transform.position - home).sqrMagnitude > 60f * 60f && Vector3.Dot(player.transform.forward, home - player.transform.position) > 0)
            ResetCivilian(); // the run restarted behind us
        Vector3 away = transform.position - player.transform.position; away.y = 0;
        if (fled < 0)
        {
            if (away.sqrMagnitude > noticeDistance * noticeDistance) return;
            fled = 0;
            var side = Vector3.Cross(Vector3.up, player.transform.forward);
            fleeDirection = (Vector3.Dot(away, side) >= 0 ? side : -side).normalized;
            if (animator != null) animator.Play(animator.HasState(0, Scared) ? Scared : Run, 0, 0);
            transform.rotation = Quaternion.LookRotation(-away.normalized, Vector3.up); // gasp at the shark first
            return;
        }
        fled += Time.deltaTime;
        if (fled < .55f) return;
        if (fled - Time.deltaTime < .55f && animator != null && animator.HasState(0, Run)) animator.Play(Run, 0, 0);
        if (fled < .55f + fleeDistance / fleeSpeed)
        {
            transform.rotation = Quaternion.LookRotation(fleeDirection, Vector3.up);
            transform.position += fleeDirection * fleeSpeed * Time.deltaTime;
        }
        else if (!settled)
        {
            settled = true;
            if (animator != null && animator.HasState(0, Scared)) animator.Play(Scared, 0, 0);
        }
    }
}
