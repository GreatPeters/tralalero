using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Essential proposal 11 balance probe (2026-10-07). Drives a real Noryangjin run on the indoor route with
// the game's own PlayerMove (steering logic from tools/claude-noryangjin-run.cs), pins HP so the run reaches
// the shutter, sets the weapon attack to a given value and records whether the 7 box walls fall inside the
// 20 s countdown. Directed fixture: HP pin and attack override are NOT natural play.
//   unity command --project-path . run_script --file tools/essential15-shutter-balance.cs --entry EssentialShutterBalance.Attack28
public static class EssentialShutterBalance
{
    const string Output = "tmp/essential15-shutter-balance";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    public static object Attack8() => Begin(8);
    public static object Attack20() => Begin(20);
    public static object Attack24() => Begin(24);
    public static object Attack28() => Begin(28);
    public static object Attack36() => Begin(36);

    public static object Begin(float attack)
    {
        if (!EditorApplication.isPlaying || TimeManager.isGameRunning) throw new Exception("Play start screen required");
        Directory.CreateDirectory(Output);
        string resultFile = $"{Output}/attack{attack:F0}.txt";
        if (File.Exists(resultFile)) File.Delete(resultFile);
        var player = Object.FindFirstObjectByType<PlayerScript>();
        var director = Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        var shutter = Object.FindFirstObjectByType<NoryangjinShutterEvent>();
        var move = player.GetType().GetMethod("PlayerMove", Flags);
        var laneOrigin = player.GetType().GetField("routeLaneOrigin", Flags);
        var routeRight = player.GetType().GetField("routeRight", Flags);
        var weaponField = player.GetType().GetField("currentWeaponScript", Flags);
        var waves = director.GetComponentsInChildren<NoryangjinWaveEvent>();
        var incidents = director.GetComponentsInChildren<NoryangjinMarketIncident>();
        var breaks = Enumerable.Repeat(-1f, shutter.walls.Length).ToArray(); float lastWallHp = -1; int frame = -1; bool done = false; float countdownAt = -1, nextTargetScan = 0;
        var targets = Array.Empty<EnemyScript_space>();
        EditorApplication.CallbackFunction tick = null;
        void Finish(string text)
        {
            done = true; EditorApplication.update -= tick;
            File.WriteAllText(resultFile, text + "\n" + string.Join("\n", director.Timeline.Where(l => l.Contains("shutter"))));
            GameSpeed.TestBaseScale = 1f; EditorApplication.isPaused = true;
        }
        tick = () =>
        {
            try
            {
                if (!EditorApplication.isPlaying || player == null) { EditorApplication.update -= tick; return; }
                if (done || frame == Time.frameCount || EditorApplication.isPaused) return; frame = Time.frameCount;
                if (director.Branch.choiceUI.Pending && director.Branch.choiceUI.Remaining < 4) director.Branch.choiceUI.Select(0); // indoor
                if (player.currentHealth > 0) player.currentHealth = 99999f;
                var weapon = weaponField.GetValue(player) as WeaponScript;
                if (weapon != null && !Mathf.Approximately(weapon.damage, attack)) weapon.damage = attack;
                if (shutter.Triggered && countdownAt < 0) countdownAt = director.Elapsed;
                if (countdownAt >= 0)
                    for (int w = 0; w < shutter.walls.Length; w++)
                        if (shutter.walls[w] != null && !shutter.walls[w].Alive && breaks[w] < 0) breaks[w] = director.Elapsed - countdownAt;
                if (countdownAt >= 0 && director.Elapsed - countdownAt > 19.5f && lastWallHp < 0)
                    lastWallHp = shutter.walls.Where(x => x != null && x.Alive).Select(x => x.Health).DefaultIfEmpty(0).Sum();
                string last = director.Timeline.LastOrDefault() ?? "";
                if (last.Contains("shutter passed") || last.Contains("shutter closing") || director.Elapsed > 420 || player.currentHealth <= 0)
                {
                    var walls = shutter.walls.Where(w => w != null).ToArray();
                    Finish($"attack={attack} wallHP={shutter.wallHealth} countdownAt={countdownAt:F1} end={director.Elapsed:F1} used={(countdownAt >= 0 ? director.Elapsed - countdownAt : -1):F1} result={(last.Contains("passed") ? "PASSED" : last.Contains("closing") ? "CLOSED" : "OTHER")} wallsLeft={walls.Count(w => w.Alive)} fire={director.PlayerFireRate():F2} hp={player.currentHealth:F0} cause={player.LastDamageCause} last='{last}' wallBreaks=[{string.Join(",", breaks.Select(b => b.ToString("F1")))}] hpLeftAt19.5={lastWallHp:F0}");
                    return;
                }
                if (!TimeManager.isGameRunning || player.IsWorldYawTurnActive) return;
                var at = player.transform.position;
                var origin = (Vector3)laneOrigin.GetValue(player); var right = (Vector3)routeRight.GetValue(player);
                float current = Vector3.Dot(at - origin, right), desired = 0; bool urgent = false; float soonest = float.PositiveInfinity;
                foreach (var wave in waves)
                    foreach (var band in wave.waves)
                    {
                        float safe = wave.SafeLateral(band);
                        if (float.IsNaN(safe) || Mathf.Abs(wave.Lateral(at)) > 5 || Mathf.Abs(-wave.Ahead(at) - band.along) > band.length) continue;
                        float key = wave.IsCrashing(band) ? -1 : wave.SecondsToCrash(band);
                        if (key < 0 && !wave.IsCrashing(band) || key >= soonest) continue;
                        soonest = key; desired = current + (safe - wave.Lateral(at)); urgent = true;
                    }
                if (!urgent)
                {
                    if (director.Elapsed >= nextTargetScan) { targets = Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None); nextTargetScan = director.Elapsed + .2f; }
                    float best = 30; var aim = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
                    foreach (var enemy in targets)
                    {
                        if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;
                        var delta = enemy.transform.position - at; float ahead = Vector3.Dot(delta, aim), lane = Vector3.Dot(enemy.transform.position - origin, right);
                        if (ahead < 1 || ahead > 26 || Mathf.Abs(lane) > 2.2f || Mathf.Abs(delta.y) > 4) continue;
                        float priority = ahead + Mathf.Abs(lane - current) * 2;
                        if (priority < best) { best = priority; desired = Mathf.Clamp(lane, -1.8f, 1.8f); }
                    }
                }
                foreach (var incident in incidents)
                    if (incident.Triggered && incident.Ahead(at) > -8 && incident.Ahead(at) < 24 && Mathf.Abs(incident.Lateral(at)) < 5)
                        desired = incident.lane < 0 ? 1.6f : -1.6f;
                desired = Mathf.Clamp(desired, -1.9f, 1.9f);
                float error = desired - current;
                if (Mathf.Abs(error) > .05f) move.Invoke(player, new object[] { Mathf.Sign(error) * Mathf.Min(15, Mathf.Abs(error) * 12) });
            }
            catch (Exception error) { Finish("harness error " + error); }
        };
        EditorApplication.update += tick;
        foreach (var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None)) harness.enabled = false;
        Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();
        Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        GameSpeed.TestBaseScale = 3f; // whole simulation 3x; countdown is game time so the result is unchanged
        return new { resultFile, attack };
    }
}
