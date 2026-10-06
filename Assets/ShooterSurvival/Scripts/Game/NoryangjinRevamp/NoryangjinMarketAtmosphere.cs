using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public sealed class NoryangjinMarketAtmosphere : MonoBehaviour
{
    public StableGameplayCamera gameplayCamera;
    public TMP_Text[] clocks = System.Array.Empty<TMP_Text>();
    public TMP_Text[] televisions = System.Array.Empty<TMP_Text>();
    public NoryangjinRevampEvent auction;
    public Transform train;
    public Material skyTemplate;
    public AudioClip[] broadcasts = System.Array.Empty<AudioClip>();
    private NoryangjinRevampDirector director;
    private Vector3 cameraOffset, trainRest;
    private Quaternion cameraRotation;
    private Material oldSky, sky;
    private Skybox cameraSkybox;
    private Material oldCameraSky;
    private float nextBroadcast = 24;
    private int lastClockMinute=-1;
    private int broadcast;
    private string lastNews;
    // The former fourth line ("활어 경매가 시작됩니다") played at random, e.g. at the shutter; the shutter
    // now announces "문이 곧 닫힙니다. 주의해주세요!" itself (user 2026-09-29).
    private static readonly string[] announcements = {
        "상어가 들어왔습니다. 통로를 비워 주세요", "운반 차량이 지나갑니다. 안전선 밖으로 비켜 주세요",
        "경매장 통로에 물건을 놓지 마세요" };

    private void Start()
    {
        director = GetComponent<NoryangjinRevampDirector>();
        if (gameplayCamera != null) { cameraOffset = gameplayCamera.yawRelativeOffset; cameraRotation = gameplayCamera.yawRelativeRotation; }
        if (train != null) trainRest = train.position;
        if (skyTemplate != null)
        {
            oldSky = RenderSettings.skybox; sky = new Material(skyTemplate); RenderSettings.skybox = sky;
            cameraSkybox = gameplayCamera != null ? gameplayCamera.GetComponent<Skybox>() : null;
            if (cameraSkybox != null) { oldCameraSky = cameraSkybox.material; cameraSkybox.material = sky; }
        }
        // The "새벽 시장 · 4:00" screen clock was replaced by ChapterProgressHud (essential proposal 13).
        // In-world market clocks below still tick for atmosphere.
    }
    private void Update()
    {
        if (director == null || director.Player == null) return;
        float elapsed = director.Elapsed;
        if (!director.Running) return;
        if (elapsed < 1) { nextBroadcast = 24; broadcast = 0; }
        int minutes=240+Mathf.FloorToInt(30*Mathf.Clamp01(elapsed/Mathf.Max(1,director.expectedRunSeconds)));
        if(minutes!=lastClockMinute)
        {
            lastClockMinute=minutes;string clock=$"{minutes/60:D2}:{minutes%60:D2}";
            foreach(var display in clocks)if(display!=null)display.text=clock;
        }
        string latest = director.Hud != null ? director.Hud.CurrentMessage : "";
        if (!string.IsNullOrEmpty(latest) && latest != lastNews)
        {
            lastNews = latest;
            foreach (var tv in televisions) if (tv != null) tv.text = "시장 속보\n" + latest;
        }
        if (sky != null)
        {
            float t = Mathf.Clamp01(elapsed / director.expectedRunSeconds);
            var dawn = new Color(.24f, .37f, .62f); var day = new Color(.4f, .7f, .92f); var dusk = new Color(.55f, .27f, .34f);
            sky.SetColor("_Color2", t < .6f ? Color.Lerp(dawn, day, t / .6f) : Color.Lerp(day, dusk, (t - .6f) / .4f));
            sky.SetColor("_Color1", Color.Lerp(new Color(.32f, .4f, .52f), new Color(.85f, .46f, .28f), t));
        }
        if (train != null) train.position = trainRest + Vector3.right * (Mathf.Repeat(elapsed * 8, 300) - 150);
        // Market speakers stay quiet on the outside pier so wave calls are never covered.
        bool onPier = director.Branch != null && director.Branch.Outside && director.Branch.Driving;
        if (elapsed >= nextBroadcast && director.CanStartHazard && !onPier)
        {
            string message = announcements[broadcast++ % announcements.Length];
            if (broadcasts.Length > 0) director.Speak(broadcasts[(broadcast - 1) % broadcasts.Length], false);
            director.Hud?.Publish(message, 2.4f); nextBroadcast = elapsed + 32;
            foreach (var tv in televisions) if (tv != null) tv.text = "시장 방송\n" + message;
        }
    }
    private void LateUpdate()
    {
        if (gameplayCamera == null || director == null || director.Player == null) return;
        var p = director.Player.transform;
        // Keep the camera beneath the roof until the entire camera rig clears the loading bay.
        var branch = director.Branch;
        bool outsideRoute = branch != null && branch.Selected && branch.Outside;
        bool inside = p.position.z < 43 && p.position.z > -351 && Mathf.Abs(p.position.x - 124.3f) < 9 && Vector3.Dot(p.forward, Vector3.back) > .7f && !outsideRoute;
        bool cold = p.position.z > -342 && p.position.z < -200 && Mathf.Abs(p.position.x + 67) < 9;
        // Optional: drop under decks that cross overhead. Off by default (user: keep the normal early-run view).
        bool under = lowerUnderDecks && !inside && !cold && Underpass(p);
        bool low = inside || cold || under;
        // Preserve room around the shark while aiming below the ceiling. A ten-
        // metre boom made the shark fill the aisle; the old eight-degree pitch
        // looked mostly at sign backs and roof geometry.
        var lowOffset = new Vector3(.12f, 6.8f, -17f); float lowPitch = 18;
        if (inside && branch != null && branch.liftHeight > 0)
        {
            // On the market ramp keep the camera under the roof above its own floor, then aim back at the shark.
            var flat = Vector3.ProjectOnPlane(p.forward, Vector3.up).normalized;
            float rel = branch.HeightAt(p.position + flat * lowOffset.z) - branch.HeightAt(p.position);
            // Do not pitch upward when the camera trails on a lower ramp section.
            lowOffset.y = Mathf.Clamp(lowOffset.y+rel,5.8f,8.2f);
        }
        float t = Mathf.Clamp01(Time.deltaTime * 4);
        gameplayCamera.yawRelativeOffset = Vector3.Lerp(gameplayCamera.yawRelativeOffset, low ? lowOffset : cameraOffset, t);
        gameplayCamera.yawRelativeRotation = Quaternion.Slerp(gameplayCamera.yawRelativeRotation, low ? Quaternion.Euler(lowPitch, 0, 0) : cameraRotation, t);
    }

    [Tooltip("Lower the camera while a walkable deck crosses overhead (S3 underpasses, y=12 decks).")]
    public bool lowerUnderDecks;
    private MeshCollider[] roads;
    private float nextUnderCheck;
    private bool underCached;
    public static float UnderpassLookAhead = 18, UnderpassClearance = 5, UnderpassCeiling = 16;

    private bool Underpass(Transform p)
    {
        if (Time.unscaledTime < nextUnderCheck) return underCached;
        nextUnderCheck = Time.unscaledTime + .1f;
        if (roads == null)
        {
            var follower = p.GetComponent<NoryangjinRoadHeightFollower>();
            roads = follower != null && follower.RoadRoot != null ? follower.RoadRoot.GetComponentsInChildren<MeshCollider>(true) : System.Array.Empty<MeshCollider>();
        }
        underCached = HasDeckAbove(roads, p.position, p.forward, UnderpassLookAhead, UnderpassClearance, UnderpassCeiling);
        return underCached;
    }

    public static bool HasDeckAbove(MeshCollider[] colliders, Vector3 position, Vector3 forward, float lookAhead, float clearance, float ceiling)
    {
        // A deck counts only where the shark's own level also continues underneath it; a ramp the
        // shark is about to climb has nothing at the shark's height below it.
        var flat = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        for (float ahead = -4; ahead <= lookAhead; ahead += 3)
        {
            var origin = position + flat * ahead + Vector3.up * ceiling;
            var ray = new Ray(origin, Vector3.down);
            bool high = false, level = false;
            foreach (var c in colliders)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                var b = c.bounds;
                if (origin.x < b.min.x || origin.x > b.max.x || origin.z < b.min.z || origin.z > b.max.z) continue;
                if (b.min.y > position.y + ceiling || b.max.y < position.y - 1.5f) continue;
                if (!c.Raycast(ray, out var hit, ceiling + 1.5f) || hit.normal.y < .5f) continue;
                if (hit.point.y > position.y + clearance) high = true;
                else if (Mathf.Abs(hit.point.y - position.y) < 1.2f) level = true;
                if (high && level) return true;
            }
        }
        return false;
    }
    private void OnDisable()
    {
        if (gameplayCamera != null) { gameplayCamera.yawRelativeOffset = cameraOffset; gameplayCamera.yawRelativeRotation = cameraRotation; }
        if (sky != null) { RenderSettings.skybox = oldSky; if (cameraSkybox != null) cameraSkybox.material = oldCameraSky; Destroy(sky); }
    }
}
