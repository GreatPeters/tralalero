using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// A water deer bounds across the road from the right shoulder to the left without stopping. It stays
// hidden until the shark is close, then runs at a speed chosen to reach the shark's current lane about
// when the shark arrives, so the read is "it is coming from the right: slip in behind it". A hit spins
// the shark's model and costs a share of max health (no instant death). Replaces the HighwayTraffic
// hazard behaviour on deer hazards; the hazard's own component, collider and warning line stay off.
public sealed class WaterDeerCrossing : MonoBehaviour
{
    public Transform deer;
    public float activateAhead = 46f, startLane = 11f, endLane = -11f;
    public float minSpeed = 3.2f, maxSpeed = 5.5f, damageFraction = .2f, hitRadius = 1.5f, spinSeconds = .8f;

    private PlayerScript player;
    private DeerHop hop;
    private float routeD, lane, speed, lastPd, playerSpeed = 7.8f;
    private bool active, done, hit;

    // Speed that brings the deer from its start lane to the target lane in the time the shark needs to arrive.
    public static float CrossingSpeed(float fromLane, float targetLane, float aheadDistance, float sharkSpeed, float min, float max)
    {
        float seconds = Mathf.Max(.5f, aheadDistance / Mathf.Max(1f, sharkSpeed));
        return Mathf.Clamp((fromLane - targetLane) / seconds, min, max);
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerScript>();
        if (deer == null) { var h = GetComponentInChildren<DeerHop>(true); if (h != null) deer = h.transform; }
        if (deer != null) hop = deer.GetComponent<DeerHop>();
        var hazard = GetComponent<HighwayHazard>();
        if (hazard != null) { hazard.enabled = false; if (hazard.warning != null) hazard.warning.SetActive(false); }
        foreach (var c in GetComponents<Collider>()) c.enabled = false;
        routeD = RouteFrame.Available ? RouteFrame.NearestDistance(transform.position) : 0;
        ResetForRun();
    }

    public void ResetForRun()
    {
        active = done = hit = false;
        if (deer != null) deer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (player == null || deer == null || !TimeManager.isGameRunning || !RouteFrame.Available) return;
        float dt = Time.deltaTime * Mathf.Max(0, TimeManager.timeFactor);
        float pd = RouteFrame.PlayerDistance(player);
        if (pd < lastPd - 30) ResetForRun();
        if (dt > 0) playerSpeed = Mathf.Lerp(playerSpeed, Mathf.Clamp((pd - lastPd) / dt, 0, 20), .05f);
        lastPd = pd;

        if (!active && !done && pd >= routeD - activateAhead && pd < routeD)
        {
            if (RouteFrame.PlayerOnBypass) { done = true; return; }
            float target = RouteFrame.Lane(player.transform.position, pd);
            speed = CrossingSpeed(startLane, target, routeD - pd, playerSpeed, minSpeed, maxSpeed);
            lane = startLane; active = true;
            Place();
            deer.gameObject.SetActive(true);
            GameAudioService.Play(GameSound.Warning);
        }
        if (!active) return;

        lane -= speed * dt;
        Place();
        if (!hit)
        {
            var v = player.transform.position - deer.position; v.y = 0;
            if (v.magnitude < hitRadius)
            {
                hit = true; speed *= 1.6f; // startled, it bolts
                player.ApplyDamage(player.MaxHealth * damageFraction, PlayerDamageCause.Wildlife);
                var model = player.GetComponentInChildren<PlayerCosmeticCustomizer>(true)?.modelRoot;
                if (model != null)
                {
                    var spin = model.GetComponent<CosmeticHitSpin>();
                    if (spin == null) spin = model.gameObject.AddComponent<CosmeticHitSpin>();
                    spin.Play(spinSeconds);
                }
            }
        }
        if (lane <= endLane) { active = false; done = true; deer.gameObject.SetActive(false); }
    }

    private void Place()
    {
        RouteFrame.Sample(routeD, out var c, out var f);
        var right = Vector3.Cross(Vector3.up, f);
        transform.position = c + right * lane;
        if (hop != null) hop.heading = -right;
    }
}
