using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

public sealed class NoryangjinBannerEvent : NoryangjinRevampEvent
{
    [Tooltip("Optional PA line spoken with the banner.")]
    public AudioClip voice;
    protected override void OnTriggered() { if (voice != null) Director.Speak(voice, true); }
}
