using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

// A paddle hit spins only the model; route movement, camera and input keep their heading.
public sealed class CosmeticHitSpin : MonoBehaviour
{
    private Quaternion restingRotation;
    private float elapsed, duration;
    private bool spinning;
    public void Play(float seconds)
    {
        if(!spinning)restingRotation=transform.localRotation;
        duration=Mathf.Max(.1f,seconds);elapsed=0;spinning=true;
    }
    private void LateUpdate()
    {
        if(!spinning)return;
        if(!TimeManager.isGameRunning)return;
        elapsed+=Time.deltaTime;
        transform.localRotation=restingRotation*Quaternion.Euler(0,720f*Mathf.Clamp01(elapsed/duration),0);
        if(elapsed>=duration)ResetPose();
    }
    private void OnDisable()=>ResetPose();
    public void ResetPose(){if(spinning)transform.localRotation=restingRotation;spinning=false;}
}
