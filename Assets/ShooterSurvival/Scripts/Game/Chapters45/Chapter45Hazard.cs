using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// One taught warning/body sweep. The authored hitbox moves with the visible body.
[DisallowMultipleComponent]
public sealed class Chapter45Hazard : MonoBehaviour
{
    public float distance, warningDistance = 18, warningSeconds = 1.5f, operationSeconds = 1.2f;
    public float safeLane = -3, damageFraction = .06f, safeHalfWidth = 1.6f;
    public int floor, choiceIndex = -1, requiredChoice;
    public bool manualOnly;
    public float lingerSeconds;
    [Tooltip("Explicit fatal contacts (open manholes and falling rebar) bypass shields.")]
    public bool fatalContact;
    public bool fallIntoHole;
    public PlayerDamageCause damageCause = PlayerDamageCause.Traffic;
    public Transform body;
    public GameObject footprint;
    [Tooltip("Show the full swept body path for this verified moving hazard.")]
    public bool showSweepPath;
    public Vector3 sweepFrom = new(7, 0, 0), sweepTo = new(0, 0, 0);
    public string warning = "교차 차량 · 표시된 위험 구역을 피하세요";
    public bool Triggered { get; private set; }
    public bool Active { get; private set; }
    public bool Finished { get; private set; }
    public int Contacts { get; private set; }
    private Chapter45Director director;
    private Vector3 bodyRest;
    private bool captured, hit;
    private Mesh sweepWarningMesh, authoredFootprintMesh;
    private float elapsed, resolvedWarning, resolvedOperation;

    public void ResetForRun(Chapter45Director owner)
    {
        director = owner;
        if (body != null && !captured) { bodyRest = body.position; captured = true; }
        if (body != null)
            foreach (var collider in body.GetComponentsInChildren<Collider>(true))
            {
                var contact = collider.GetComponent<Chapter45HazardHitbox>();
                if (contact == null) contact = collider.gameObject.AddComponent<Chapter45HazardHitbox>();
                contact.owner = this;
            }
        resolvedWarning = Chapter45Director.Setting("c45_warningSeconds", warningSeconds, 1.2f, 4);
        resolvedOperation = Chapter45Director.Setting("c45_sweepSeconds", operationSeconds, .5f, 4);
        RestoreSweepFootprint();
        Contacts = 0; Triggered = Active = Finished = hit = false; elapsed = 0;
        if (body != null) { body.position = bodyRest; body.gameObject.SetActive(false); }
        if (footprint != null) footprint.SetActive(false);
    }
    public void Trigger()
    {
        if (director == null || Active || director.CurrentFloor != floor || director.IsTransferring || !director.ChoiceMatches(choiceIndex, requiredChoice)) return;
        Triggered = true; Finished = hit = false; elapsed = 0;
        ConfigureSweepFootprint();
        if (footprint != null) footprint.SetActive(true);
        director.AnnounceWarning("위험 예고", warning, resolvedWarning);
        director.Record("hazard warning " + name);
    }
    public void Tick(float dt)
    {
        if (!manualOnly && !Triggered && director.Distance >= distance - warningDistance && director.ChoiceMatches(choiceIndex, requiredChoice) && director.CurrentFloor == floor) Trigger();
        if (!Triggered || Finished) return;
        if (director.CurrentFloor != floor || director.IsTransferring) { Cancel(); return; }
        elapsed += dt;
        if (elapsed < resolvedWarning) return;
        if (!Active) { Active = true; director.Record("hazard active " + name); }
        float t = Mathf.Clamp01((elapsed - resolvedWarning) / resolvedOperation);
        if (body != null)
        {
            body.gameObject.SetActive(true);
            body.position = bodyRest + transform.TransformDirection(Vector3.Lerp(sweepFrom, sweepTo, t));
        }
        if (t >= 1 && elapsed >= resolvedWarning + resolvedOperation + Mathf.Max(0, lingerSeconds)) Cancel();
    }
    public void Hit(PlayerScript player)
    {
        if (!Active || hit || !TimeManager.isGameRunning || TimeManager.timeFactor <= 0 || player != director.Player || director.IsTransferring || director.CurrentFloor != floor) return;
        // The body's authored sweep bounds leave the advertised corridor clear;
        // any actual physical overlap remains authoritative.
        hit = true; Contacts++;
        if (fatalContact) player.DieFromFatalHazard(fallIntoHole, damageCause);
        else player.ApplyDamage(player.MaxHealth * Chapter45Director.Setting("c45_hazardDamageFraction", damageFraction, .01f, .2f), damageCause);
        director.Record("hazard contact " + name);
    }
    // Visual only: project both ends of the existing collider sweep onto the
    // ground. The player capsule margin marks unsafe player-center positions;
    // it does not alter the body, collision, warning clock or damage.
    private void ConfigureSweepFootprint()
    {
        if (!showSweepPath || footprint == null || body == null) return;
        var filter = footprint.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return;
        var boxes = body.GetComponentsInChildren<BoxCollider>(true);
        if (boxes.Length == 0) return;
        if (authoredFootprintMesh == null) authoredFootprintMesh = filter.sharedMesh;
        float padding = .85f;
        var player = director != null ? director.Player : null;
        var capsule = player != null ? player.GetComponent<CapsuleCollider>() : null;
        if (capsule != null)
        {
            var scale = capsule.transform.lossyScale;
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float offset = Vector3.ProjectOnPlane(capsule.transform.TransformPoint(capsule.center) - player.transform.position, Vector3.up).magnitude;
            padding = radius + offset + .1f;
        }
        Vector3 axisX = transform.right, axisZ = transform.forward;
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
        foreach (var box in boxes)
        {
            if (!box.enabled) continue;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = box.center + Vector3.Scale(box.size * .5f,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 restCorner = box.transform.TransformPoint(corner) + bodyRest - body.position;
                for (int end = 0; end < 2; end++)
                {
                    Vector3 point = restCorner + transform.TransformDirection(end == 0 ? sweepFrom : sweepTo) - transform.position;
                    float x = Vector3.Dot(point, axisX), z = Vector3.Dot(point, axisZ);
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
                }
            }
        }
        if (float.IsInfinity(minX)) return;
        minX -= padding; maxX += padding; minZ -= padding; maxZ += padding;
        var points = new[] { new Vector2(minX, minZ), new Vector2(minX, maxZ), new Vector2(maxX, maxZ), new Vector2(maxX, minZ) };
        var vertices = new Vector3[4];
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 world = transform.position + axisX * points[i].x + axisZ * points[i].y;
            world.y = footprint.transform.position.y;
            vertices[i] = footprint.transform.InverseTransformPoint(world);
        }
        if (sweepWarningMesh == null)
        {
            sweepWarningMesh = new Mesh { name = "Runtime hazard sweep warning" };
            sweepWarningMesh.vertices = new Vector3[4];
            sweepWarningMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        }
        sweepWarningMesh.vertices = vertices;
        sweepWarningMesh.RecalculateNormals(); sweepWarningMesh.RecalculateBounds();
        filter.sharedMesh = sweepWarningMesh;
    }
    private void RestoreSweepFootprint()
    {
        if (authoredFootprintMesh == null || footprint == null) return;
        var filter = footprint.GetComponent<MeshFilter>();
        if (filter != null) filter.sharedMesh = authoredFootprintMesh;
    }
    private void OnDisable() { RestoreSweepFootprint(); }
    private void OnDestroy()
    {
        RestoreSweepFootprint();
        if (sweepWarningMesh != null) Destroy(sweepWarningMesh);
    }
    public void Cancel()
    {
        RestoreSweepFootprint();
        Active = false; Finished = true;
        if (body != null) body.gameObject.SetActive(false);
        if (footprint != null) footprint.SetActive(false);
    }
}
