using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// A road-work sinkhole in one lane. Falling in is death (the existing "fell into a hole" feedback);
// the construction sign and cones ahead make it readable, and the other lanes stay open.
[RequireComponent(typeof(Collider))]
public sealed class RoadPotholeHazard : MonoBehaviour
{
    private bool consumed;

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || consumed || !TimeManager.isGameRunning) return;
        var player = other.GetComponentInParent<PlayerScript>(); if (player == null || player.IsStationaryCombat) return;
        consumed = true;
        player.DieFromHazard(true);
    }

    private void OnEnable() => consumed = false;
}
