using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

public sealed class NoryangjinWallEvent : NoryangjinRevampEvent
{
    public NoryangjinBreakable[] pieces = Array.Empty<NoryangjinBreakable>();
    protected override void OnTriggered() { foreach (var p in pieces) if (p != null) p.Arm(); }
}
