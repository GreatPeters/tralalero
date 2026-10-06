using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Cinema-only contact uses authored crowd damage, not remaining enemy health.
// Ch3 keeps its original contact rules. Pool activation resets the one-hit latch.
[DisallowMultipleComponent]
public sealed class Chapter45AudienceContact : MonoBehaviour
{
    public RestStopHoldout holdout;
    public float damage = 2;
    public bool RetiredOnContact { get; private set; }
    private bool resolved;
    private void OnEnable() { resolved = false; RetiredOnContact = false; }
    public bool Contact(PlayerScript player)
    {
        if (resolved || holdout == null || !holdout.Active || !holdout.OnChapterFloor
            || player == null || player.HoldoutAim != holdout || !Chapter45Director.CanActorContact(player, transform)) return true;
        resolved = true; RetiredOnContact = true;
        player.ApplyDamage(Mathf.Max(0, damage), PlayerDamageCause.EnemyContact);
        var combat = GetComponent<EnemyScript_space>();
        if (combat != null) combat.RetireAfterUnrewardedContact();
        return true;
    }
}
