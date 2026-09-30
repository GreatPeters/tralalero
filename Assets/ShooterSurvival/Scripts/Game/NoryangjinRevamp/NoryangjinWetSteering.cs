using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// A wet patch briefly carries the shark sideways; input still counters the slide inside route bounds.
public sealed class NoryangjinWetSteering : MonoBehaviour
{
    private PlayerScript player;
    private float original, remaining;
    private float sideways;
    public float Remaining=>remaining;
    public float SidewaysSpeed=>sideways;
    public void Apply(PlayerScript target, float seconds,float sidewaysSpeed=.7f)
    {
        if (remaining <= 0) { player = target; original = target.moveSensitivity_Devision; sideways=sidewaysSpeed; }
        remaining = Mathf.Max(remaining, seconds);
        target.moveSensitivity_Devision = original / .55f;
    }
    public void Clear()
    {
        if (player != null && remaining > 0) player.moveSensitivity_Devision = original;
        remaining = 0;sideways=0;
    }
    private void Update()
    {
        if (remaining <= 0) return;
        if (player == null || player.currentHealth <= 0) { Clear(); return; }
        if (!TimeManager.isGameRunning) return;
        if(NoryangjinRevampDirector.Active!=null&&NoryangjinRevampDirector.Active.Spinning)return;
        player.ApplySurfaceSlip(sideways*Time.deltaTime*TimeManager.timeFactor);
        sideways=Mathf.MoveTowards(sideways,0,Time.deltaTime*.35f);
        if (remaining <= Time.deltaTime) Clear(); else remaining -= Time.deltaTime;
    }
    private void OnDisable() => Clear();
}
