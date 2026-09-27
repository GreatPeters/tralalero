using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Cones dropped by a road-worker enemy: bumping one costs a share of max health once and knocks it over.
// Unlike authored roadblocks (instant death), a cone placed mid-encounter must never be an unreadable kill.
public sealed class RestStopConeContact : MonoBehaviour
{
    public float damageFraction = .12f;
    private bool spent;

    public static float Damage(float maxHealth, float fraction) => Mathf.Max(0, maxHealth) * Mathf.Clamp01(fraction);

    private void OnTriggerEnter(Collider other)
    {
        if (spent || !TimeManager.isGameRunning) return;
        var player = other.GetComponentInParent<PlayerScript>(); if (player == null) return;
        spent = true;
        player.ApplyDamage(Damage(player.MaxHealth, damageFraction), PlayerDamageCause.Roadblock);
        GameAudioService.PlayAt(GameSound.EnemyHit, transform.position);
        transform.rotation *= Quaternion.Euler(80, 0, 0); // knocked over
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }
}
