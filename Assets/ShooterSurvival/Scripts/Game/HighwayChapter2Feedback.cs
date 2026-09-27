using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Scene-owned CFXR rentals. No effects are created during scene destruction.
public sealed class HighwayChapter2Feedback : MonoBehaviour
{
    public GameObject explosionPrefab, dustPrefab;
    public Material woodMaterial, missileMaterial;
    private sealed class Effect { public GameObject root; public ParticleSystem[] particles; public float until; }
    private sealed class Wreck { public GameObject root; public string key; public Vector3 origin, side, forward, scale; public Quaternion rotation; public float started; }
    private readonly List<Effect> explosions = new(), dust = new();
    private readonly List<Wreck> flying = new();
    private readonly Dictionary<string, Stack<GameObject>> wreckPool = new();
    private PlayerScript player;
    private float elapsed;
    private sealed class Chip { public Transform t; public Vector3 velocity; public float started; }
    private sealed class Link { public Transform t; public Vector3 from, to; public float started; }
    private readonly List<Chip> chips = new();
    private readonly List<Link> links = new();
    public int ActiveExplosions { get; private set; }
    public int PeakExplosions { get; private set; }

    public void Prepare(PlayerScript target)
    {
        player = target;
        if (explosions.Count == 0) Warm(explosionPrefab, explosions, HighwayChapter2Data.Count("fxMaximum"));
        if (dust.Count == 0) Warm(dustPrefab, dust, HighwayChapter2Data.Count("fxMaximum"));
        if (chips.Count == 0)
            for (int i = 0; i < 24; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Pooled wood chip";
                go.transform.SetParent(transform, false); Destroy(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial = woodMaterial; go.SetActive(false); chips.Add(new Chip { t = go.transform });
            }
        if (links.Count == 0)
            for (int i = 0; i < 6; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = "Pooled chain missile";
                go.transform.SetParent(transform, false); Destroy(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial = missileMaterial; go.SetActive(false); links.Add(new Link { t = go.transform });
            }
        Clear();
    }

    private void Warm(GameObject prefab, List<Effect> pool, int count)
    {
        if (prefab == null) return;
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, transform);
            go.name = "Pooled " + prefab.name;
            foreach (var script in go.GetComponentsInChildren<MonoBehaviour>(true))
                if (script.GetType().Name.StartsWith("CFXR_Effect")) script.enabled = false;
            var particles = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var p in particles)
            {
                var main = p.main; main.loop = false; main.stopAction = ParticleSystemStopAction.None;
                main.startLifetime = Mathf.Min(.6f, main.startLifetime.constantMax);
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            go.SetActive(false);
            pool.Add(new Effect { root = go, particles = particles });
        }
    }

    private void Play(List<Effect> pool, Vector3 position, float scale)
    {
        var available = pool.Find(e => !e.root.activeSelf);
        if (available == null) return; // Cosmetic saturation cannot delay combat/rewards.
        available.root.transform.SetPositionAndRotation(position, Quaternion.identity);
        available.root.transform.localScale = Vector3.one * scale;
        available.until = elapsed + HighwayChapter2Data.Value("smokeSeconds");
        available.root.SetActive(true);
        foreach (var p in available.particles)
        {
            bool smoke = p.name.ToLowerInvariant().Contains("smoke");
            bool inFront = player != null && Vector3.Dot(position - player.transform.position, player.transform.forward) < HighwayChapter2Data.Value("clearViewAhead");
            var emission = p.emission; emission.enabled = !(smoke && inFront);
            p.Clear(true); p.Play(true);
        }
        ActiveExplosions = explosions.FindAll(e => e.root.activeSelf).Count;
        PeakExplosions = Mathf.Max(PeakExplosions, ActiveExplosions);
    }

    public void Dust(Vector3 position) => Play(dust, position, .5f);
    public void Shatter(Vector3 position) => Play(explosions, position, 2f);
    public void WoodChips(Vector3 position)
    {
        int index = 0;
        foreach (var chip in chips)
        {
            if (chip.t.gameObject.activeSelf) continue;
            float angle = index * 2.39f + elapsed;
            chip.t.position = position; chip.t.localScale = new Vector3(.15f, .06f, .24f);
            chip.velocity = new Vector3(Mathf.Cos(angle) * 4, 3 + Mathf.Sin(angle) * 2, Mathf.Sin(angle) * 4);
            chip.started = elapsed; chip.t.gameObject.SetActive(true); if (++index == 8) break;
        }
    }
    public void ChainMissile(Vector3 from, Vector3 to)
    {
        var link = links.Find(l => !l.t.gameObject.activeSelf); if (link == null) return;
        link.from = from; link.to = to; link.started = elapsed; link.t.position = from;
        link.t.localScale = new Vector3(.18f, .4f, .18f); link.t.gameObject.SetActive(true);
    }

    public void DestroyVehicle(HighwayVehicleEnemy vehicle, Vector3 routeForward)
    {
        Play(explosions, vehicle.transform.position + Vector3.up, 1.3f);
        if (vehicle.body == null) return;
        // Sedan combat stats can have several silhouettes; never rent another model's wreck.
        string key = vehicle.kind + ":" + (vehicle.body.childCount > 0 ? vehicle.body.GetChild(0).name : vehicle.body.name);
        if (!wreckPool.TryGetValue(key, out var pool)) wreckPool[key] = pool = new Stack<GameObject>();
        var root = pool.Count > 0 ? pool.Pop() : Instantiate(vehicle.body.gameObject, transform);
        root.name = "Pooled wreck " + key;
        foreach (var contact in root.GetComponentsInChildren<Collider>(true)) contact.enabled = false;
        foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
        foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true)) { particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); particle.gameObject.SetActive(false); }
        foreach (var trail in root.GetComponentsInChildren<TrailRenderer>(true)) { trail.emitting = false; trail.Clear(); }
        root.transform.SetPositionAndRotation(vehicle.body.position, vehicle.body.rotation);
        root.transform.localScale = vehicle.body.lossyScale;
        root.SetActive(true);
        flying.Add(new Wreck { root = root, key = key, origin = root.transform.position,
            rotation = root.transform.rotation, side = Vector3.Cross(Vector3.up, routeForward) * (vehicle.Lane < 0 ? -1 : 1),
            forward = routeForward, started = elapsed, scale = root.transform.localScale });
        GameAudioService.PlayAt(GameSound.Explosion, vehicle.transform.position);
    }

    private void Update()
    {
        if (!TimeManager.isGameRunning) { Clear(); return; }
        elapsed += Time.deltaTime * Mathf.Max(0, TimeManager.timeFactor);
        foreach (var chip in chips)
        {
            if (!chip.t.gameObject.activeSelf) continue;
            float age = elapsed - chip.started;
            if (age >= .5f) { chip.t.gameObject.SetActive(false); continue; }
            chip.t.position += chip.velocity * Time.deltaTime; chip.velocity += Vector3.down * (12 * Time.deltaTime);
            chip.t.Rotate(new Vector3(160, 220, 90) * Time.deltaTime);
        }
        foreach (var link in links)
        {
            if (!link.t.gameObject.activeSelf) continue;
            float u = (elapsed - link.started) / .22f;
            if (u >= 1) { link.t.gameObject.SetActive(false); continue; }
            link.t.position = Vector3.Lerp(link.from, link.to, u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 1.4f);
            link.t.rotation = Quaternion.FromToRotation(Vector3.up, link.to - link.from);
        }
        foreach (var pool in new[] { explosions, dust })
            foreach (var effect in pool)
                if (effect.root.activeSelf && elapsed >= effect.until)
                {
                    foreach (var p in effect.particles) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    effect.root.SetActive(false);
                }
        ActiveExplosions = explosions.FindAll(e => e.root.activeSelf).Count;
        float life = HighwayChapter2Data.Value("debrisSeconds");
        for (int i = flying.Count - 1; i >= 0; i--)
        {
            var wreck = flying[i]; float t = elapsed - wreck.started;
            if (t >= life) { Return(wreck); flying.RemoveAt(i); continue; }
            // Sideways/up/north only. Debris can never launch toward the approaching player.
            wreck.root.transform.position = wreck.origin + wreck.side * (HighwayChapter2Data.Value("debrisSideSpeed") * t)
                + Vector3.up * (HighwayChapter2Data.Value("debrisUpSpeed") * t) + wreck.forward * t;
            wreck.root.transform.rotation = wreck.rotation * Quaternion.Euler(t * 220, t * 150, t * 280);
            wreck.root.transform.localScale = Vector3.Lerp(wreck.scale, wreck.scale * .1f, Mathf.Clamp01((t / life - .65f) / .35f));
        }
    }

    private void Return(Wreck wreck)
    {
        if (wreck.root == null) return;
        wreck.root.SetActive(false);
        wreckPool[wreck.key].Push(wreck.root);
    }

    public void Clear()
    {
        foreach (var wreck in flying) Return(wreck);
        flying.Clear();
        foreach (var chip in chips) if (chip.t != null) chip.t.gameObject.SetActive(false);
        foreach (var link in links) if (link.t != null) link.t.gameObject.SetActive(false);
        foreach (var pool in new[] { explosions, dust })
            foreach (var effect in pool) if (effect.root != null) effect.root.SetActive(false);
        ActiveExplosions = 0;
    }
    private void OnDisable() => Clear();
}
