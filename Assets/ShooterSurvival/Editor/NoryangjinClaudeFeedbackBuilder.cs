using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// 2026-09-29 user screenshot feedback for the Noryangjin revamp safe copy (Claude Code). Runs inside
// tools/install-noryangjin-revamp.cs after the V2/V3 builders and before the crossing lift:
//   - seagulls (the user's SR18 gimmick) dive onto black shadows instead of the container drop, earlier;
//   - connector corners are capped so old turn planks no longer poke out;
//   - the cold auction becomes a live-fish auction with the crowd in the aisle, scattering from the shark;
//   - every Meshy character gets the project's FlatKit outline material back;
//   - the emptied end stretch gets a merchant charge;
//   - the final boss uses the new Meshy N20 model at a uniform scale when it has been imported.
public static class NoryangjinClaudeFeedbackBuilder
{
    const string Dir = "Assets/ShooterSurvival/Models/Generated/NoryangjinClaude";
    const string Prefabs = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    const string OutlineReference = "Assets/JH/Model/Enemy/Garden_Spear/Material.001.mat";
    const string Stage01 = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/";
    // A little taller than the 3.4-unit shark so she reads as the boss; uniform scale only.
    public const float BossHeight = 3.8f;
    static readonly Dictionary<string, Material> mats = new();
    static TMP_FontAsset font;

    public static List<string> Apply(Transform root, Transform roads, Transform surfaces, float pathHalf, TMP_FontAsset labelFont, Func<string, AudioClip> voice)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Install in Edit Mode");
        Directory.CreateDirectory(Dir); mats.Clear(); font = labelFont;
        var report = new List<string>();
        report.Add(SeagullDrops(root, voice));
        report.Add(CornerCaps(roads, surfaces));
        report.Add(LiveAuction(root, voice));
        report.Add(EndStretchCharge(root, pathHalf, voice));
        report.Add(Boss(pathHalf));
        report.Add(OutlineCharacters(root));
        return report;
    }

    // ---------------------------------------------------------------- 7. seagull drops
    static string SeagullDrops(Transform root, Func<string, AudioClip> voice)
    {
        var container = root.GetComponentsInChildren<NoryangjinMarketIncident>(true).FirstOrDefault(e => e.kind == NoryangjinMarketIncident.Kind.ContainerDrop);
        Vector3 at = container != null ? container.transform.position : new Vector3(225.5f, 0, -25);
        if (container != null) Object.DestroyImmediate(container.gameObject);
        var template = Object.FindObjectsByType<ObstacleStats>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(o => o.obstaclePattern == ObstaclePattern.Seagull && o.name.StartsWith("SR18_"));
        if (template == null) return "seagull template missing";
        var group = new GameObject("L_SeagullDrops").transform; group.SetParent(root, false);
        var banner = new GameObject("L_SeagullWarning").transform; banner.SetParent(group, false);
        banner.SetPositionAndRotation(at + Vector3.back * 26, Quaternion.LookRotation(Vector3.forward));
        var ev = banner.gameObject.AddComponent<NoryangjinBannerEvent>();
        ev.triggerAhead = 30; ev.banner = "그림자 주의! 갈매기가 내리꽂힌다"; ev.bannerSeconds = 2.4f; ev.voice = voice("gull-warning");
        // Three dives down the pier, alternating lanes; each lands well ahead and waits as an obstacle.
        var drops = new[] { (lane: -1.1f, along: -14f), (lane: 1.1f, along: -2f), (lane: -.2f, along: 10f) };
        for (int i = 0; i < drops.Length; i++)
        {
            var gull = Object.Instantiate(template.gameObject, group);
            gull.name = "N7_SeagullDrop_" + (i + 1);
            gull.transform.SetPositionAndRotation(new Vector3(at.x + drops[i].lane, template.transform.position.y, at.z + drops[i].along), Quaternion.LookRotation(Vector3.forward));
            var stats = gull.GetComponent<ObstacleStats>();
            stats.triggerRadius = 44; stats.telegraphTime = 1.4f; stats.dropHeight = 14; stats.dropTime = .55f;
            gull.SetActive(true);
        }
        return $"seagull drops: {drops.Length} from {template.name}, radius 44, 1.4 s shadow, 14 m dive (container drop removed)";
    }

    // ---------------------------------------------------------------- 5. connector corners
    static string CornerCaps(Transform roads, Transform surfaces)
    {
        var connector = surfaces.Find("ContinuousConnector0")?.GetComponent<Renderer>();
        if (connector == null) return "connector missing";
        int hidden = 0;
        foreach (var corner in new[] { new Vector3(124.3f, 0, -348f), new Vector3(-67f, 0, -348f) })
        {
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.name = "ConnectorCornerCap"; cap.transform.SetParent(surfaces, false);
            // Slightly below the connector top so overlapping faces never fight; it only shows in the gap.
            cap.transform.position = corner + Vector3.up * .01f; cap.transform.localScale = new Vector3(7.6f, .17f, 7.6f);
            cap.GetComponent<Renderer>().sharedMaterial = connector.sharedMaterial;
            foreach (Transform tile in roads)
            {
                if (tile == surfaces || !tile.name.Contains("RightTurn")) continue;
                var rs = tile.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue;
                var c = rs[0].bounds.center; if (Mathf.Abs(c.x - corner.x) > 8 || Mathf.Abs(c.z - corner.z) > 8) continue;
                foreach (var r in rs) r.enabled = false; hidden++;
            }
        }
        return $"connector corners capped, {hidden} old turn tiles hidden";
    }

    // ---------------------------------------------------------------- 6. live-fish auction
    static string LiveAuction(Transform root, Func<string, AudioClip> voice)
    {
        var hall = root.Find("ColdAuctionHall"); var live = hall != null ? hall.Find("LiveAuction") : null;
        if (live == null) return "live auction missing";
        foreach (Transform child in live.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        // Clear the frozen-tuna pallets from the auction bays so the basins own the room.
        foreach (Transform bay in hall)
            if (bay.name.StartsWith("InsulatedBay") && Mathf.Abs(bay.localPosition.z - live.localPosition.z) < 16)
                foreach (Transform t in bay.Cast<Transform>().ToArray())
                    if (t.name.StartsWith("N17_") || t.name == "PalletBayStripe") Object.DestroyImmediate(t.gameObject);
        var water = Mat("AuctionFloorWater", new Color(.02f, .05f, .08f, .38f), transparent: true);
        var tubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "N19_live_fish_tub.prefab");
        int tubs = 0;
        // Two columns of live-fish basins either side of the aisle.
        foreach (float x in new[] { -5.8f, -4.35f, 4.35f, 5.8f })
            for (float z = -13; z <= 13.1f; z += 2.9f, tubs++)
            {
                var p = new Vector3(x, .12f, z + (Mathf.Abs(x) > 5 ? 1.45f : 0));
                if (tubPrefab != null) Place(tubPrefab, live, p, Quaternion.Euler(0, (tubs * 47) % 360, 0), 1);
                else Tub(live, p);
                if (tubs % 3 == 0) Quad(live, "BasinSpill", p + new Vector3(Mathf.Sign(x) * -.7f, -.1f, .4f), new Vector2(1.1f, 1.6f), water);
            }
        // Auctioneer on a crate at the far end of the lots, bell and price board on a proper stand.
        var crate = Box(live, "AuctioneerCrate", new Vector3(3.4f, .35f, 15.6f), new Vector3(1.5f, .7f, 1.3f), Mat("AuctionCrate", new Color(.36f, .45f, .56f)));
        var auctioneer = Person(live, "Auctioneer", new Vector3(3.4f, .72f, 15.6f), new Vector3(-.6f, 0, -1), "N13_merchant_male");
        Cap(auctioneer, new Color(.95f, .78f, .15f), "경매");
        foreach (float x in new[] { 5.0f, 6.9f }) Box(live, "BoardLeg", new Vector3(x, 1.45f, 16.3f), new Vector3(.09f, 2.9f, .09f), Mat("BoardSteel", new Color(.42f, .47f, .5f)));
        var board = Box(live, "AuctionPriceBoard", new Vector3(5.95f, 3.25f, 16.3f), new Vector3(2.6f, 1.15f, .12f), Mat("BoardNavy", new Color(.05f, .16f, .42f)));
        var activity = live.GetComponent<NoryangjinAuctionActivity>() ?? live.gameObject.AddComponent<NoryangjinAuctionActivity>();
        activity.priceBoard = Label(board, "LivePrice", "활어 경매 진행 중\n30,000원", new Vector3(0, 0, -.08f), new Vector2(2.4f, 1.0f), 2.5f, Color.white);
        activity.auctioneer = auctioneer; activity.soldLot = "광어 한 대야";
        activity.callClip = voice("auction-call-live"); activity.soldClip = voice("auction-sold-live");
        if (activity.voice == null) { activity.voice = live.gameObject.AddComponent<AudioSource>(); activity.voice.playOnAwake = false; activity.voice.spatialBlend = 1; activity.voice.minDistance = 5; activity.voice.maxDistance = 34; activity.voice.volume = .45f; }
        // Hanging sign on wires that reach the ceiling.
        var sign = Box(live, "LiveAuctionSign", new Vector3(0, 5.9f, -9), new Vector3(5.6f, 1.0f, .12f), Mat("SignWhite", new Color(.95f, .96f, .93f)));
        Label(sign, "Title", "활어 경매장", new Vector3(0, 0, -.08f), new Vector2(5.4f, .9f), 4.4f, new Color(.05f, .16f, .42f));
        foreach (float x in new[] { -2.4f, 2.4f }) Box(live, "SignWire", new Vector3(x, 6.95f, -9), new Vector3(.03f, 1.1f, .03f), Mat("BoardSteel", Color.gray));
        // The crowd stands in the aisle facing the auctioneer and scatters as the shark arrives.
        var scatter = live.GetComponent<NoryangjinAuctionScatter>() ?? live.gameObject.AddComponent<NoryangjinAuctionScatter>();
        scatter.shout = voice("auction-scatter"); scatter.triggerAhead = 16; scatter.runSeconds = .75f;
        var members = new List<NoryangjinAuctionScatter.Member>(); var bidders = new List<Animator>();
        var colors = new[] { new Color(.2f, .55f, .95f), new Color(.95f, .35f, .3f), new Color(.3f, .8f, .4f), new Color(.95f, .6f, .15f) };
        int n = 0;
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 3; col++, n++)
            {
                float x = (col - 1) * 1.45f + (row % 2 == 0 ? .3f : -.3f), z = -5 + row * 3.4f + (col == 1 ? .8f : 0);
                var home = new Vector3(x, .12f, z);
                var who = Person(live, "Bidder" + n, home, new Vector3(3.4f, 0, 15.6f) - home, n % 3 == 1 ? "N14_merchant_female" : "N13_merchant_male");
                Cap(who, colors[n % colors.Length], (101 + n * 7).ToString());
                bidders.Add(who);
                float side = x > .15f ? 1 : x < -.15f ? -1 : (n % 2 == 0 ? 1 : -1);
                var world = live.TransformPoint(home);
                members.Add(new NoryangjinAuctionScatter.Member { body = who.transform.parent.parent, animator = who, home = who.transform.parent.parent.position,
                    homeRotation = who.transform.parent.parent.rotation, flee = world + live.right * side * (3.0f + (n % 3) * .25f - Mathf.Abs(x)) + live.forward * ((n % 2) * .6f - .3f), nerve = (n % 4) * 1.2f });
            }
        // Spectators behind the basins stay put.
        for (int i = 0; i < 6; i++)
        {
            float side = i % 2 == 0 ? -1 : 1; var p = new Vector3(side * 7.3f, .12f, -10 + i * 4.2f);
            var who = Person(live, "Spectator" + i, p, new Vector3(0, 0, p.z + 2) - p, i % 3 == 0 ? "N14_merchant_female" : "N13_merchant_male");
            Cap(who, colors[(i + 2) % colors.Length], (220 + i * 11).ToString());
            bidders.Add(who);
        }
        scatter.members = members.ToArray(); activity.bidders = bidders.ToArray();
        var bannerEvent = hall.GetComponentsInChildren<NoryangjinBannerEvent>(true).FirstOrDefault(e => e.name == "D_ColdAuction");
        if (bannerEvent != null) bannerEvent.banner = "활어 경매 중! 상어 난입!";
        return $"live auction: {tubs} basins ({(tubPrefab != null ? "Meshy N19" : "built")}), {members.Count} scattering bidders, {bidders.Count - members.Count} spectators";
    }

    static void Tub(Transform parent, Vector3 p)
    {
        var body = Cylinder(parent, "LiveFishBasin", p + Vector3.up * .2f, new Vector3(1.05f, .2f, 1.05f), Mat("BasinBlue", new Color(.07f, .3f, .85f)));
        Cylinder(body, "BasinWater", p + Vector3.up * .36f, new Vector3(.9f, .02f, .9f), Mat("BasinWater", new Color(.25f, .7f, .9f, .75f), transparent: true), world: true);
    }

    // Numbered bidder cap on the head bone (Noryangjin buyers wear numbered caps).
    static void Cap(Animator who, Color color, string number)
    {
        if (who == null) return;
        var head = who.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Head");
        if (head == null) return;
        var skin = who.GetComponentInChildren<SkinnedMeshRenderer>(true);
        float top = skin != null ? skin.bounds.max.y : head.position.y + .6f;
        var cap = new GameObject("BidderCap").transform; cap.SetParent(head, false);
        cap.position = new Vector3(head.position.x, top - .09f, head.position.z); cap.rotation = who.transform.rotation;
        var lossy = cap.parent.lossyScale; cap.localScale = new Vector3(1 / Mathf.Max(.001f, lossy.x), 1 / Mathf.Max(.001f, lossy.y), 1 / Mathf.Max(.001f, lossy.z));
        var cloth = Mat("Cap_" + ColorUtility.ToHtmlStringRGB(color), color);
        Cylinder(cap, "Crown", Vector3.zero, new Vector3(.44f, .08f, .44f), cloth, local: true);
        Box(cap, "Brim", new Vector3(0, -.06f, .25f), new Vector3(.36f, .025f, .2f), cloth, local: true);
        var tag = Box(cap, "NumberBadge", new Vector3(0, .01f, .215f), new Vector3(.2f, .1f, .012f), Mat("CapBadge", Color.white), local: true);
        // Readable from in front of the wearer.
        Label(tag, "Number", number, new Vector3(0, 0, .012f), new Vector2(.2f, .1f), .7f, new Color(.05f, .1f, .25f), facing: Vector3.back);
    }

    // ---------------------------------------------------------------- 8. end stretch
    static string EndStretchCharge(Transform root, float pathHalf, Func<string, AudioClip> voice)
    {
        var e1 = root.GetComponentsInChildren<NoryangjinRushEvent>(true).FirstOrDefault(r => r.name == "E1_MerchantRush");
        if (e1 == null) return "E1 templates missing";
        var a = new GameObject("K_MerchantUnionCharge").transform; a.SetParent(root, false);
        // After the SR18 encounters that are live again in this stretch, before the corner at x~439.
        a.SetPositionAndRotation(new Vector3(425f, 0, 203.5f), Quaternion.LookRotation(Vector3.right));
        var ev = a.gameObject.AddComponent<NoryangjinRushEvent>();
        ev.templates = e1.templates; ev.triggerAhead = 30; ev.banner = "노량진 상인 연합 출동!";
        ev.rows = 2; ev.perRow = 3; ev.rowGap = 4.5f; ev.spread = pathHalf * 1.6f; ev.runSpeed = 2.8f; ev.homing = .5f;
        ev.healthHits = .8f; ev.damageShare = .05f; ev.coinsEach = 3; ev.hazardSeconds = 10; ev.scale = e1.scale;
        ev.shouts = new[] { voice("merchant-union") }.Concat(e1.shouts.Skip(1)).ToArray();
        return "end stretch: merchant union charge at x425 (6 merchants)";
    }

    // ---------------------------------------------------------------- 9. final boss
    static string Boss(float pathHalf)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "N20_ajumma_boss.prefab");
        var woman = Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e.name == "SR18_L_E25_T293_Enemy_Woman");
        if (woman == null) return "boss enemy missing";
        if (prefab == null) return "N20 boss prefab not imported yet; kept the stretched SR18 woman";
        woman.transform.localScale = Vector3.one;
        // Reinstalls replace the body added by a previous install.
        foreach (Transform child in woman.transform.Cast<Transform>().ToArray())
            if (child.name == "N20_AjummaBossBody") Object.DestroyImmediate(child.gameObject);
        // Hide the old visual, keep the enemy (health, contact, rewards) and give it the new body.
        foreach (Transform child in woman.transform.Cast<Transform>().ToArray())
            if (child.GetComponentInChildren<Animator>(true) != null || child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) child.gameObject.SetActive(false);
        var body = (GameObject)PrefabUtility.InstantiatePrefab(prefab, woman.transform);
        body.name = "N20_AjummaBossBody"; body.transform.localPosition = Vector3.zero;
        // Meshy bodies face +Z; face the shark, which arrives heading north (+Z) on this last pier.
        body.transform.rotation = Quaternion.LookRotation(Vector3.back);
        // The prefab is fitted to the character height; scale uniformly to the boss height (no sideways stretch).
        body.transform.localScale = Vector3.one * (BossHeight / ImportedHeight);
        var animator = body.GetComponentInChildren<Animator>(); if (animator != null) animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // Still blocks the whole deck: the collider (not the body) spans the lane.
        var col = woman.GetComponent<Collider>();
        if (col is CapsuleCollider cap) { cap.radius = Mathf.Max(cap.radius, pathHalf + .6f); cap.height = Mathf.Max(cap.height, BossHeight); cap.center = new Vector3(0, BossHeight * .5f, 0); }
        else if (col is BoxCollider box) { box.size = new Vector3(pathHalf * 2 + 1.2f, BossHeight, Mathf.Max(box.size.z, 1.6f)); box.center = new Vector3(0, BossHeight * .5f, 0); }
        return $"final boss: Meshy N20 at uniform height {BossHeight} m";
    }
    public const float ImportedHeight = 2.3f; // ImportMeshyCharacters.CharacterHeight

    // ---------------------------------------------------------------- 6. outline
    static string OutlineCharacters(Transform root)
    {
        var reference = AssetDatabase.LoadAssetAtPath<Material>(OutlineReference);
        if (reference == null) return "outline reference missing";
        int swapped = 0;
        var targets = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList();
        var boss = Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e.name == "SR18_L_E25_T293_Enemy_Woman");
        if (boss != null) targets.AddRange(boss.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        foreach (var r in targets.Distinct())
        {
            var output = r.sharedMaterials.ToArray(); bool changed = false;
            for (int i = 0; i < output.Length; i++)
            {
                var source = output[i]; if (source == null || source.shader == reference.shader) continue;
                var texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
                if (texture == null) continue;
                string path = $"{Dir}/Outline_{texture.name}.mat";
                if (!mats.TryGetValue(path, out var m))
                {
                    m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m == null) { m = new Material(reference); AssetDatabase.CreateAsset(m, path); } else m.CopyPropertiesFromMaterial(reference);
                    m.shader = reference.shader; m.shaderKeywords = reference.shaderKeywords;
                    m.SetTexture("_BaseMap", texture); m.SetColor("_BaseColor", new Color(.9f, .9f, .9f, 1));
                    if (m.HasProperty("_ColorDim")) m.SetColor("_ColorDim", new Color(.8f, .8f, .83f, 1));
                    m.enableInstancing = true; EditorUtility.SetDirty(m); mats[path] = m;
                }
                output[i] = m; changed = true;
            }
            if (changed) { r.sharedMaterials = output; if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r); swapped++; }
        }
        return $"outline: {swapped} character renderers now use the FlatKit outline material";
    }

    // ---------------------------------------------------------------- helpers
    static Animator Person(Transform parent, string name, Vector3 local, Vector3 facing, string id)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + id + ".prefab");
        facing.y = 0; if (facing.sqrMagnitude < .001f) facing = Vector3.back;
        var go = Place(prefab, parent, local, Quaternion.LookRotation(parent.TransformDirection(facing.normalized)), 1, world: false);
        go.name = name;
        return go.GetComponentInChildren<Animator>();
    }

    static GameObject Place(GameObject prefab, Transform parent, Vector3 local, Quaternion rotation, float scale, bool world = false)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = local;
        if (world) go.transform.rotation = rotation; else go.transform.rotation = rotation;
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static Transform Box(Transform parent, string name, Vector3 p, Vector3 size, Material m, bool local = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m; return go.transform;
    }

    static Transform Cylinder(Transform parent, string name, Vector3 p, Vector3 size, Material m, bool local = true, bool world = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name; go.transform.SetParent(parent, false);
        if (world) go.transform.position = p; else go.transform.localPosition = p;
        go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = m; return go.transform;
    }

    static void Quad(Transform parent, string name, Vector3 p, Vector2 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.localRotation = Quaternion.Euler(90, 0, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1); var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static TMP_Text Label(Transform parent, string name, string text, Vector3 p, Vector2 size, float fontSize, Color color, Vector3? facing = null)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshPro>(); t.font = font; t.text = text; t.fontSize = fontSize; t.color = color;
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.rectTransform.sizeDelta = size;
        // Undo the parent box scale so the text keeps its authored size.
        var lossy = parent.lossyScale; go.transform.localScale = new Vector3(1 / Mathf.Max(.001f, lossy.x), 1 / Mathf.Max(.001f, lossy.y), 1 / Mathf.Max(.001f, lossy.z));
        go.transform.localPosition = new Vector3(p.x / Mathf.Max(.001f, lossy.x), p.y / Mathf.Max(.001f, lossy.y), p.z / Mathf.Max(.001f, lossy.z));
        go.transform.rotation = Quaternion.LookRotation(parent.TransformDirection(facing ?? Vector3.forward));
        return t;
    }

    static Material Mat(string name, Color color, bool transparent = false)
    {
        string path = $"{Dir}/{name}.mat";
        if (mats.TryGetValue(path, out var cached)) return cached;
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", .35f);
        if (transparent)
        {
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent;
        }
        m.enableInstancing = true; EditorUtility.SetDirty(m); mats[path] = m; return m;
    }
}
