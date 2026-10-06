using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

public sealed class Chapter45HazardHitbox : MonoBehaviour
{
    public Chapter45Hazard owner;
    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerScript>();
        if (player != null) owner?.Hit(player);
    }
}
