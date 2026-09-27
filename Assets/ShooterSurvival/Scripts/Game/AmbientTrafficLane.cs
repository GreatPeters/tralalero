using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Harmless mainline traffic beside the player's lane: cars loop along the local +Z axis faster than
// the shark to sell highway speed. No colliders; visibility only.
public sealed class AmbientTrafficLane : MonoBehaviour
{
    public Transform[] cars = System.Array.Empty<Transform>();
    public float length = 420f, speed = 24f;
    private float[] offsets;

    private void Awake()
    {
        offsets = new float[cars.Length];
        for (int i = 0; i < cars.Length; i++) offsets[i] = cars.Length == 0 ? 0 : length * i / cars.Length;
        Place(0);
    }

    private void Update()
    {
        if (!TimeManager.isGameRunning) return;
        Place(Time.deltaTime * speed * Mathf.Max(0, TimeManager.timeFactor));
    }

    private void Place(float advance)
    {
        for (int i = 0; i < cars.Length; i++)
        {
            if (cars[i] == null) continue;
            offsets[i] = Mathf.Repeat(offsets[i] + advance, length);
            cars[i].localPosition = new Vector3(cars[i].localPosition.x, 0, offsets[i]);
        }
    }
}
