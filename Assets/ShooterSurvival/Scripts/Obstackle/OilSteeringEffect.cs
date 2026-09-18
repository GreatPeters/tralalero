using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Refresh steering and spin the visible model; retain the route/camera frame.
public sealed class OilSteeringEffect : MonoBehaviour
{
    private PlayerScript player;
    private float originalDivider, remaining;
    private bool applied;
    private CosmeticHitSpin spin;
    public void Apply(PlayerScript target, float seconds)
    {
        if (target == null || seconds <= 0) return;
        if (!applied) { player=target; originalDivider=target.moveSensitivity_Devision; applied=true; }
        remaining=Mathf.Max(remaining,Mathf.Max(0,seconds));
        target.moveSensitivity_Devision=originalDivider/.55f;
        var model = target.GetComponentInChildren<PlayerCosmeticCustomizer>(true)?.modelRoot;
        if (model != null)
        {
            spin = model.GetComponent<CosmeticHitSpin>() ?? model.gameObject.AddComponent<CosmeticHitSpin>();
            spin.Play(remaining);
        }
    }
    private void Update()
    {
        if (!applied) return;
        if (player==null || player.currentHealth<=0) { Clear(); return; }
        if (!TimeManager.isGameRunning) return;
        remaining-=Time.deltaTime;
        if (remaining<=0) Clear();
    }
    public void Clear()
    {
        if (applied && player!=null) player.moveSensitivity_Devision=originalDivider;
        if (spin != null) spin.ResetPose();
        applied=false;remaining=0;
    }
    private void OnDisable() => Clear();
}
