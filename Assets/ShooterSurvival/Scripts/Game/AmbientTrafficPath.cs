using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Harmless traffic on the opposing carriageway of a curved HighwayRoute. Cars follow route distance with a
// lateral offset (negative = left of the player's lanes) and loop; only cars near the player are shown.
// No colliders or damage: this sells a live Korean expressway without changing encounter rules.
public sealed class AmbientTrafficPath : MonoBehaviour
{
    public HighwayRoute route;
    public Transform[] cars = System.Array.Empty<Transform>();
    public float[] lateral = { -11f, -17f };
    [Tooltip("Route units per second; negative drives toward the route start (oncoming).")]
    public float speed = -26f;
    public float visibleRadius = 170f;
    private float[] distance;
    private PlayerScript player;

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerScript>();
        distance = new float[cars.Length];
        float length = route != null ? route.length : 1;
        for (int i = 0; i < cars.Length; i++) distance[i] = length * (i + .5f) / Mathf.Max(1, cars.Length);
        Place(0);
    }

    private void Update()
    {
        if (!TimeManager.isGameRunning || route == null) return;
        Place(Time.deltaTime * Mathf.Max(0, TimeManager.timeFactor));
    }

    private void Place(float dt)
    {
        if (route == null) return;
        for (int i = 0; i < cars.Length; i++)
        {
            var car = cars[i]; if (car == null) continue;
            distance[i] = Mathf.Repeat(distance[i] + speed * dt, route.length);
            route.Sample(distance[i], false, out var center, out var forward);
            var right = Vector3.Cross(Vector3.up, forward);
            var p = center + right * lateral[i % lateral.Length];
            bool near = player == null || (p - player.transform.position).sqrMagnitude < visibleRadius * visibleRadius;
            if (car.gameObject.activeSelf != near) car.gameObject.SetActive(near);
            if (!near) continue;
            car.SetPositionAndRotation(p, Quaternion.LookRotation(speed < 0 ? -forward : forward, Vector3.up));
        }
    }
}
