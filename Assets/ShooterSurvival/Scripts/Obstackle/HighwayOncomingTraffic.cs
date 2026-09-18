using System;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

public sealed class HighwayOncomingTraffic : MonoBehaviour
{
    [Serializable] public sealed class Beat
    {
        public float distance;
        public string title;
        public int[] lanes = { 0 };
        public float[] delays = { 0 };
    }
    public HighwayRoute route;
    public ChapterPatternHUD hud;
    public Transform[] cars;
    public LineRenderer[] warnings;
    public Beat[] beats;
    public float WarningSeconds { get; private set; } = 2.2f;
    public int Launched { get; private set; }
    public int Contacts { get; private set; }
    private PlayerScript player;
    private int beatIndex;
    private float clock = -1, speed = 16;
    private float launchDistance;
    private bool[] launched, hit;
    private float[] positions;
    private Vector3 previousPlayer;

    public void BeginRun()
    {
        player = FindFirstObjectByType<PlayerScript>();
        beatIndex = 0; clock = -1; Launched = Contacts = 0;
        WarningSeconds = HighwayRoute.Setting("highwayOncomingWarning", 2.2f, 1.5f, 5);
        speed = HighwayRoute.Setting("highwayOncomingSpeed", 16, 8, 24);
        launched = new bool[cars.Length]; hit = new bool[cars.Length]; positions = new float[cars.Length];
        Hide();
    }
    private void OnDisable() => Hide();
    private void Hide()
    {
        if (cars != null) foreach (var car in cars) if (car != null) car.gameObject.SetActive(false);
        if (warnings != null) foreach (var warning in warnings) if (warning != null) warning.gameObject.SetActive(false);
        hud?.Clear(this);
    }
    private void Update()
    {
        if (!TimeManager.isGameRunning || TimeManager.timeFactor <= 0 || player == null || player.currentHealth <= 0 || beatIndex >= beats.Length) return;
        var beat = beats[beatIndex];
        if (clock < 0)
        {
            if (route.Distance < beat.distance) return;
            GameAudioService.Play(GameSound.Warning);
            clock = 0; launchDistance = route.Distance + 68; previousPlayer = player.transform.position;
            Array.Clear(launched, 0, launched.Length); Array.Clear(hit, 0, hit.Length);
            for (int i = 0; i < beat.lanes.Length; i++) DrawWarning(i, beat.lanes[i]);
        }
        float delta = Time.deltaTime * TimeManager.timeFactor;
        clock += delta;
        bool finished = true;
        for (int i = 0; i < beat.lanes.Length; i++)
        {
            float launchAt = WarningSeconds + beat.delays[i];
            if (!launched[i] && clock >= launchAt)
            {
                launched[i] = true; positions[i] = launchDistance; Launched++;
                Place(cars[i], positions[i], beat.lanes[i]); cars[i].gameObject.SetActive(true);
            }
            if (warnings[i] != null) warnings[i].gameObject.SetActive(!launched[i]);
            if (!launched[i]) { finished = false; continue; }
            if (!cars[i].gameObject.activeSelf) continue;
            var previous = cars[i].position;
            positions[i] -= speed * delta;
            Place(cars[i], positions[i], beat.lanes[i]);
            if (!hit[i] && SweptContact(previous - previousPlayer, cars[i].position - player.transform.position, 1.65f))
            {
                hit[i] = true; Contacts++;
                player.DieFromHazard(false);
            }
            if (positions[i] < route.Distance - 22 || positions[i] <= 0) cars[i].gameObject.SetActive(false);
            else finished = false;
        }
        previousPlayer = player.transform.position;
        hud?.Show(this, beat.title, clock < WarningSeconds ? "경고 차로를 비우세요  차량 접근 중" : "빈 차로로 피하세요", 2);
        if (finished) { Hide(); beatIndex++; clock = -1; }
    }
    private void Place(Transform car, float distance, int lane)
    {
        route.Sample(distance, false, out var point, out var forward);
        car.SetPositionAndRotation(point + Vector3.Cross(Vector3.up, forward) * Lane(lane) + Vector3.up * .05f, Quaternion.LookRotation(-forward));
    }
    private void DrawWarning(int index, int lane)
    {
        var line = warnings[index]; line.gameObject.SetActive(true); line.positionCount = 17;
        for (int i = 0; i < 17; i++)
        {
            route.Sample(route.Distance + 8 + i * 4, false, out var p, out var f);
            line.SetPosition(i, p + Vector3.Cross(Vector3.up, f) * Lane(lane) + Vector3.up * .065f);
        }
    }
    public static float Lane(int lane) => (Mathf.Clamp(lane, 0, 2) - 1) * 3.3f;
    public static bool SweptContact(Vector3 from, Vector3 to, float radius)
    {
        from.y = to.y = 0;
        var delta = to - from;
        float t = Mathf.Clamp01(-Vector3.Dot(from, delta) / Mathf.Max(.00001f, delta.sqrMagnitude));
        return (from + delta * t).sqrMagnitude <= radius * radius;
    }
}
