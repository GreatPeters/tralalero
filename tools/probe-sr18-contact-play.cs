using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Sr18ContactPlayProbe
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static object Pose(string prefix)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
        var scene = SceneManager.GetActiveScene();
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var p = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var targets = map.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true).Where(e => e.name.StartsWith(prefix)).ToArray();
        if (targets.Length == 0) throw new InvalidOperationException("No matching enemy");
        TimeManager.isGameRunning = false; p.enabled = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var w in root.GetComponentsInChildren<WeaponScript>(true)) { w.CancelInvoke(); w.enabled = false; }
            foreach (var e in root.GetComponentsInChildren<EnemyEventController>(true)) e.enabled = false;
            if (root.name == "Canvas") root.SetActive(false);
        }
        foreach (var c in p.GetComponents<Collider>()) c.enabled = false;
        var camera = Camera.main;
        Vector3 offset = Quaternion.Inverse(p.transform.rotation) * (camera.transform.position - p.transform.position);
        Quaternion relativeRotation = Quaternion.Inverse(p.transform.rotation) * camera.transform.rotation;
        Vector3 center = targets.Select(e => e.transform.position).Aggregate(Vector3.zero, (a, b) => a + b) / targets.Length;
        Vector3 forward = targets[0].transform.forward;
        p.transform.SetPositionAndRotation(center - forward * 19, Quaternion.LookRotation(forward));
        if (!camera.transform.IsChildOf(p.transform))
            camera.transform.SetPositionAndRotation(p.transform.position + p.transform.rotation * offset, p.transform.rotation * relativeRotation);
        foreach (var e in targets)
        {
            typeof(EnemyEventController).GetMethod("FacePlayerExactly", Private).Invoke(e, null);
        }
        string folder = SessionState.GetString("SR18.LivePlaytest.20260909.folder", "") + "/diagnostics";
        Directory.CreateDirectory(folder);
        return new { diagnosticOnly = true, output = folder + "/" + prefix.TrimEnd('_') + ".png", objects = targets.Select(e => new { e.name, position = e.transform.position.ToString() }).ToArray() };
    }

    public static object LampContact()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play required");
        var p = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var lamp = GameObject.Find("Noryangjin_MapTool").transform.Find("Props").Cast<Transform>().Single(t => t.name.StartsWith("SR18_L_G02_"))
            .GetComponentsInChildren<ObstacleStats>().Single(o => o.enabled);
        if (lamp.canBeShotDown) throw new InvalidOperationException("Lamp not durable");
        var center = lamp.GetComponent<Collider>().bounds.center;
        p.currentHealth = 100; p.enabled = false;
        p.transform.position = new Vector3(center.x, .12f, center.z);
        foreach (var c in p.GetComponents<Collider>()) c.enabled = true;
        Physics.SyncTransforms();
        return new { diagnosticOnly = true, beforeHealth = 100, lamp = lamp.name, damage = lamp.value };
    }

    public static object BonusCooldown()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play required");
        var p = UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        p.enabled = false; foreach (var c in p.GetComponents<Collider>()) c.enabled = false;
        var pairs = GameObject.Find("Noryangjin_MapTool").GetComponentsInChildren<BonusWallChoicePair>(true).OrderBy(b => b.name).Take(2).ToArray();
        foreach (var pair in pairs)
        {
            pair.PrepareForRun(Rarity.Normal, true);
            pair.Left.Wall.buffType = BuffType.hp_normal;
            typeof(WallScript).GetField("bonusValue", Private).SetValue(pair.Left.Wall, 10f);
        }
        var trigger = typeof(WallScript).GetMethod("OnTriggerEnter", Private);
        p.currentHealth = 100; p.lastWallTouchTime = float.NegativeInfinity; TimeManager.isGameRunning = true;
        trigger.Invoke(pairs[0].Left.Wall, new object[] { p.GetComponent<Collider>() });
        trigger.Invoke(pairs[1].Left.Wall, new object[] { p.GetComponent<Collider>() });
        if (p.currentHealth != 110 || pairs[1].Selected != null) throw new InvalidOperationException("Same-frame double reward");
        float started = Time.time; bool blockedAt15 = false;
        string file = SessionState.GetString("SR18.LivePlaytest.20260909.folder", "") + "/diagnostics/bonus-cooldown.json";
        EditorApplication.CallbackFunction observe = null;
        observe = () =>
        {
            if (!EditorApplication.isPlaying || p == null) { EditorApplication.update -= observe; return; }
            float elapsed = Time.time - started;
            if (!blockedAt15 && elapsed >= 1.5f)
            {
                trigger.Invoke(pairs[1].Left.Wall, new object[] { p.GetComponent<Collider>() });
                blockedAt15 = p.currentHealth == 110 && pairs[1].Selected == null;
            }
            if (elapsed < 2.05f) return;
            trigger.Invoke(pairs[1].Left.Wall, new object[] { p.GetComponent<Collider>() });
            bool passed = blockedAt15 && p.currentHealth == 120 && pairs[1].Selected == pairs[1].Left;
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, "{\"diagnosticOnly\":true,\"blockedAt1_5\":" + blockedAt15.ToString().ToLowerInvariant() + ",\"passed\":" + passed.ToString().ToLowerInvariant() + "}");
            TimeManager.isGameRunning = false; EditorApplication.update -= observe;
        };
        EditorApplication.update += observe;
        return new { diagnosticOnly = true, file, initialHealth = 110 };
    }
}
