using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

public static class CombatFeedbackProbe
{
    public static object Begin(string label)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
        string folder = "tmp/combat-feedback-2026-09-14/" + label;
        if (Directory.Exists(folder)) throw new InvalidOperationException("Keep earlier evidence");
        Directory.CreateDirectory(folder);
        var player = Object.FindFirstObjectByType<PlayerScript>();
        var canvas = Object.FindFirstObjectByType<CanvasScript>();
        OpeningStoryUI.Instance?.Skip();
        int phase = 0, ticks = 0, lastFrame = -1, startingCoins = 0;
        float originalHealth = 0, originalDamage = 0, originalRate = 0, originalDuration = 0;
        int originalCount = 0;
        var weapon = player.GetComponentInChildren<WeaponScript>();
        var log = new System.Text.StringBuilder();
        CoinPickup farCoin = null;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; return; }
            if (Time.frameCount == lastFrame || EditorApplication.isPaused) return;
            lastFrame = Time.frameCount; ticks++;
            try
            {
                if (phase == 0 && ticks >= 4)
                {
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(folder + "/lobby.png"));
                    phase++; ticks = 0;
                }
                else if (phase == 1 && ticks >= 3)
                {
                    canvas.PlayerPressedStartButton();
                    if (!TimeManager.isGameRunning) throw new InvalidOperationException("Start blocked");
                    player.movement = false; player.canShoot = false;
                    originalHealth = player.MaxHealth; originalDamage = weapon.damage;
                    originalRate = weapon.fireRate; originalCount = weapon.bulletCount;
                    originalDuration = BulletScript.CurrentMissileDuration;
                    player.ApplyRunHealthBonus(100,false); weapon.damage += 80; weapon.fireRate += 3; weapon.bulletCount += 2;
                    BulletScript.AddMissileDurationPercent(100);
                    player.ResetState(); player.movement = false; player.canShoot = false;
                    bool reset = Mathf.Approximately(player.MaxHealth, originalHealth) && Mathf.Approximately(weapon.damage, originalDamage) &&
                        Mathf.Approximately(weapon.fireRate, originalRate) && weapon.bulletCount == originalCount &&
                        Mathf.Approximately(BulletScript.CurrentMissileDuration, originalDuration);
                    log.AppendLine("Retry full stat reset=" + reset + "; HP=" + player.MaxHealth + "; ATT=" + weapon.damage + "; duration=" + BulletScript.CurrentMissileDuration);
                    if (!reset) throw new InvalidOperationException("Run bonus persisted");
                    startingCoins = MoneyScript.S.Coin;
                    CoinPickup.Spawn(player.transform.position + player.transform.right * 2.6f, 7);
                    farCoin = CoinPickup.Spawn(player.transform.position + Vector3.up * 4f, 11);
                    phase++; ticks = 0;
                }
                else if (phase == 2 && ticks >= 5)
                {
                    int gained = MoneyScript.S.Coin - startingCoins;
                    log.AppendLine("Nearby coin delta=" + gained + "; vertically separate retained=" + (farCoin != null));
                    if (gained != 7 || farCoin == null) throw new InvalidOperationException("Coin claim/radius failed");
                    Object.Destroy(farCoin.gameObject);
                    player.ApplyHarnessHealthDelta(-18);
                    phase++; ticks = 0;
                }
                else if (phase == 3 && ticks >= 2)
                {
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(folder + "/damage.png"));
                    log.AppendLine("Damage feedback health=" + player.currentHealth);
                    phase++; ticks = 0;
                }
                else if (phase == 4 && ticks >= 8)
                {
                    var effect = player.GetComponent<OilSteeringEffect>();
                    if (effect == null) effect = player.gameObject.AddComponent<OilSteeringEffect>();
                    effect.Apply(player, 2);
                    phase++; ticks = 0;
                }
                else if (phase == 5 && ticks >= 12)
                {
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(folder + "/oil-spin.png"));
                    log.AppendLine("Oil applied; player route yaw=" + player.transform.eulerAngles.y);
                    phase++; ticks = 0;
                }
                else if (phase == 6 && ticks >= 3)
                {
                    player.GetComponent<OilSteeringEffect>().Clear();
                    player.DieFromHazard(true);
                    phase++; ticks = 0;
                }
                else if (phase == 7 && ticks >= 14)
                {
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(folder + "/hole-death.png"));
                    log.AppendLine("Hole death HP=" + player.currentHealth);
                    File.WriteAllText(folder + "/result.txt", log.ToString());
                    phase++; ticks = 0;
                }
                else if (phase == 8 && ticks >= 3)
                {
                    EditorApplication.update -= tick;
                    EditorApplication.isPaused = true;
                }
            }
            catch (Exception error)
            {
                EditorApplication.update -= tick;
                File.WriteAllText(folder + "/failure.txt",log + error.ToString());
                EditorApplication.isPaused = true;
            }
        };
        EditorApplication.update += tick;
        return "Running frame-bounded probe: " + label;
    }
}
