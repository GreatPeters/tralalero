using System;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Decorative opposing traffic. Gameplay wrong-way hazards keep their separate owner.
public sealed class HighwayAmbientTraffic : MonoBehaviour
{
    public HighwayRoute route;
    public Transform[] cars = Array.Empty<Transform>();
    private float[] distances;
    private float previousProgress;

    private void Update()
    {
        if (route == null || !TimeManager.isGameRunning)
        {
            foreach (var car in cars) if (car != null) car.gameObject.SetActive(false);
            distances = null;
            return;
        }
        if (TimeManager.timeFactor <= 0) return;
        if (distances == null || distances.Length != cars.Length || route.Distance < previousProgress)
        {
            distances = new float[cars.Length];
            for (int i = 0; i < cars.Length; i++) distances[i] = route.Distance + 55 + i * 39;
        }
        previousProgress = route.Distance;
        for (int i = 0; i < cars.Length; i++)
        {
            if (cars[i] == null) continue;
            distances[i] -= (11 + i % 3 * 2) * Time.deltaTime * TimeManager.timeFactor;
            if (distances[i] < route.Distance - 30) distances[i] = route.Distance + 235 + i * 12;
            bool visible = distances[i] > 0 && distances[i] < route.length - 12 &&
                           !Array.Exists(route.forks, f => distances[i] >= f.start - 40 && distances[i] <= f.end + 40);
            cars[i].gameObject.SetActive(visible);
            if (!visible) continue;
            route.Sample(distances[i], false, out var center, out var forward);
            float lane = i % 2 == 0 ? -10.5f : -17.5f;
            cars[i].SetPositionAndRotation(center + Vector3.Cross(Vector3.up, forward) * lane + Vector3.up * .04f,
                                          Quaternion.LookRotation(-forward));
        }
    }
}
