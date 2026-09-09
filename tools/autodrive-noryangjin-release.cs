// Run through Pipeline eval after begin-sr18-live-playtest, Play, and the observer.
// Only supplies PlayerMove input; normal stats, collisions, time and game systems remain active.
if (!UnityEditor.EditorApplication.isPlaying || IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)
    throw new InvalidOperationException("Play start screen required");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.name != "Noryangjin_MapTool_Mode_SR18") throw new InvalidOperationException("SR18 required");
var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
var p = UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var move = p.GetType().GetMethod("PlayerMove", flags);
var origin = p.GetType().GetField("routeLaneOrigin", flags);
var right = p.GetType().GetField("routeRight", flags);
var pairs = map.Find("Bonuses").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.BonusWallChoicePair>(true);
var hazards = map.Find("Props").Cast<Transform>().Where(t => t.name.StartsWith("SR18_L_G")).ToArray();
var enemies = map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>(true);
var formations = enemies.Where(e => e.name.EndsWith("_Right"))
    .Select(e => new[] { enemies.Single(left => left.name == e.name.Substring(0, e.name.Length - 6)), e }).ToArray();
string folder = UnityEditor.SessionState.GetString("SR18.LivePlaytest.20260909.folder", "") + "/release-run";
System.IO.Directory.CreateDirectory(folder);
float started = Time.time;
double nextCapture = 0;
int shots = 0;
bool capturedEnd = false;
UnityEditor.EditorApplication.CallbackFunction steer = null;
steer = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying || p == null) { UnityEditor.EditorApplication.update -= steer; return; }
    if (!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning || p.currentHealth <= 0)
    {
        if (!capturedEnd) { capturedEnd = true; ScreenCapture.CaptureScreenshot(folder + "/end.png"); }
        return;
    }
    if (Time.time - started >= 400)
    {
        ScreenCapture.CaptureScreenshot(folder + "/400-seconds.png");
        UnityEditor.EditorApplication.isPaused = true;
        UnityEditor.EditorApplication.update -= steer;
        return;
    }
    if (UnityEditor.EditorApplication.timeSinceStartup > nextCapture)
    {
        nextCapture = UnityEditor.EditorApplication.timeSinceStartup + 4;
        ScreenCapture.CaptureScreenshot(folder + "/frame-" + (shots++).ToString("D3") + ".png");
    }
    if (p.IsWorldYawTurnActive) return;
    Vector3 forward = Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized;
    Vector3 lateral = (Vector3)right.GetValue(p);
    float desired = 0, nearest = 26;
    foreach (var pair in pairs)
    {
        if (pair == null || pair.Selected != null || !pair.Left.gameObject.activeInHierarchy) continue;
        Vector3 center = (pair.Left.transform.position + pair.Right.transform.position) * .5f;
        Vector3 delta = center - p.transform.position;
        float ahead = Vector3.Dot(delta, forward);
        if (ahead < -1 || ahead > nearest || Mathf.Abs(delta.y) > 2 || Mathf.Abs(Vector3.Dot(delta, lateral)) > 5 ||
            Vector3.Dot(Vector3.ProjectOnPlane(pair.Left.transform.forward, Vector3.up).normalized, forward) > -.9f) continue;
        nearest = ahead;
        bool helperRight = (pair.Right.RolledStat ?? "").Contains("tung") || (pair.Right.RolledStat ?? "").Contains("boom");
        desired = helperRight ? 2 : -2;
    }
    foreach (var formation in formations)
    {
        if (formation.Any(e => e == null)) continue;
        Vector3 center = (formation[0].transform.position + formation[1].transform.position) * .5f;
        Vector3 delta = center - p.transform.position;
        float ahead = Vector3.Dot(delta, forward);
        if (ahead < -2.2f || ahead > nearest || Mathf.Abs(delta.y) > 2 ||
            Mathf.Abs(Vector3.Dot(delta, lateral)) > 5 || Vector3.Dot(formation[0].transform.forward, forward) < .9f) continue;
        nearest = ahead;
        bool rightDead = formation[1].RuntimeState == IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Dead;
        var aim = rightDead ? formation[1] : formation[0];
        desired = Mathf.Clamp(Vector3.Dot(aim.transform.position - (Vector3)origin.GetValue(p), lateral), -2, 2);
    }
    foreach (var hazard in hazards)
    {
        if (hazard == null || !hazard.gameObject.activeInHierarchy || Mathf.Abs(hazard.position.y - p.transform.position.y) > 2 ||
            Vector3.Dot(hazard.forward, forward) < .9f) continue;
        foreach (var part in hazard.GetComponentsInChildren<ObstacleStats>())
        {
            var collider = part.GetComponent<Collider>();
            if (!part.enabled || collider == null || !collider.enabled) continue;
            Vector3 delta = collider.bounds.center - p.transform.position;
            float ahead = Vector3.Dot(delta, forward);
            if (ahead < -2.2f || ahead > nearest || Mathf.Abs(Vector3.Dot(delta, lateral)) > 5) continue;
            nearest = ahead;
            float offset = Vector3.Dot(collider.bounds.center - (Vector3)origin.GetValue(p), lateral);
            desired = part.obstaclePattern == ObstaclePattern.Light && part.canBeShotDown && ahead > 8
                ? Mathf.Clamp(offset, -2, 2) : offset >= 0 ? -2 : 2;
        }
    }
    if (nearest == 26 && enemies.Any(e => e != null && e.RuntimeState == IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Attacking &&
        Mathf.Abs(e.transform.position.y - p.transform.position.y) < 2 && Vector3.Dot(e.transform.position - p.transform.position, forward) > 0 &&
        Vector3.Dot(e.transform.position - p.transform.position, forward) < 30)) desired = ((int)(Time.time / 1.4f) % 2 == 0) ? -2 : 2;
    float lane = Vector3.Dot(p.transform.position - (Vector3)origin.GetValue(p), lateral);
    float error = desired - lane;
    if (Mathf.Abs(error) > .04f) move.Invoke(p, new object[] { Mathf.Sign(error) * Mathf.Min(15, Mathf.Abs(error) * 12) });
};
UnityEditor.EditorApplication.update += steer;
UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>().PlayerPressedStartButton();
return new { started = true, folder, health = p.currentHealth, attack = p.currentDamage, timeScale = Time.timeScale, stopAfterGameSeconds = 400 };
