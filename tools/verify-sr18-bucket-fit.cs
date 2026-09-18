// Focused contact/animation probe, not a progression or normal-traversal test.
if (!EditorApplication.isPlaying || EditorApplication.isPaused)
    throw new InvalidOperationException("Unpaused Play Mode required.");
if (IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning ||
    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Noryangjin_MapTool_Mode_SR18")
    throw new InvalidOperationException("SR18 lobby required; do not interrupt a running round.");
string folder = "tmp/image-previews/sr18-bucket-fit-2026-09-11/runtime-" + DateTime.Now.ToString("HHmmss");
if (System.IO.Directory.Exists(folder)) throw new InvalidOperationException("Evidence folder already exists.");
System.IO.Directory.CreateDirectory(folder);
var player = UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var obstacle = UnityEngine.Object.FindObjectsByType<ObstacleStats>(FindObjectsSortMode.None)
    .First(o => o.obstaclePattern == ObstaclePattern.Bucket && o.bucket != null);
var playerCollider = player.GetComponentsInChildren<Collider>().First(c => c.enabled && c.CompareTag("Player"));
var body = player.GetComponent<Rigidbody>();
var constraints = body.constraints;
bool enabled = player.enabled;
player.enabled = false;
body.constraints = RigidbodyConstraints.FreezeAll;
player.sharkAnim.Play("Walk", 0, 0f);
obstacle.bucket.position = playerCollider.bounds.center;
Physics.SyncTransforms();
double start = EditorApplication.timeSinceStartup, attachedAt = -1;
bool first = false, walking = false, detached = false;
var samples = new System.Collections.Generic.List<object>();
var serialize = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert"))
    .First(t => t != null).GetMethod("SerializeObject", new[] { typeof(object) });
EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!EditorApplication.isPlaying || player == null) { EditorApplication.update -= tick; return; }
    double elapsed = EditorApplication.timeSinceStartup - start;
    bool attached = obstacle.bucket.IsChildOf(player.transform);
    if (attached && !first)
    {
        first = true; attachedAt = EditorApplication.timeSinceStartup;
        Capture(player, folder + "/attached-side.png", true);
        Capture(player, folder + "/attached-game.png", false);
    }
    if (attached && !walking && EditorApplication.timeSinceStartup - attachedAt > .8)
    {
        walking = true;
        Capture(player, folder + "/walking-side.png", true);
        Capture(player, folder + "/walking-game.png", false);
    }
    if (first && !attached) detached = true;
    samples.Add(new { elapsed, attached, player.canShoot,
        openingDown = Vector3.Dot(obstacle.bucket.forward, -player.transform.up),
        scale = obstacle.bucket.lossyScale.x });
    if (elapsed < 8 && !(detached && player.canShoot)) return;
    EditorApplication.update -= tick;
    System.IO.File.WriteAllText(folder + "/result.json", (string)serialize.Invoke(null, new object[] {
        new { focusedProbe = true, physicalContact = first, walking, detached,
            restoredShooting = player.canShoot, attachSeconds = obstacle.bucketAttachSeconds, samples } }));
    body.constraints = constraints;
    player.sharkAnim.Play("Idle", 0, 0f);
    player.enabled = enabled;
};
EditorApplication.update += tick;
return new { folder, collider = playerCollider.name, attachSeconds = obstacle.bucketAttachSeconds };

void Capture(IndianOceanAssets.ShooterSurvival.PlayerScript player, string path, bool side)
{
    var go = new GameObject("Bucket fit capture") { hideFlags = HideFlags.HideAndDontSave };
    var camera = go.AddComponent<Camera>();
    camera.CopyFrom(Camera.main);
    camera.enabled = false;
    int width = side ? 640 : 540, height = side ? 640 : 1170;
    if (side)
    {
        camera.orthographic = true; camera.orthographicSize = 2.8f;
        var target = player.transform.position + player.transform.up * 1.5f + player.transform.forward * .6f;
        camera.transform.position = target + player.transform.right * 8f + player.transform.up * 2f;
        camera.transform.LookAt(target, player.transform.up);
    }
    else camera.transform.SetPositionAndRotation(Camera.main.transform.position, Camera.main.transform.rotation);
    var rt = new RenderTexture(width, height, 24);
    var previous = RenderTexture.active;
    var image = new Texture2D(width, height, TextureFormat.RGB24, false);
    try
    {
        camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
        System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
    }
    finally
    {
        RenderTexture.active = previous; camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
