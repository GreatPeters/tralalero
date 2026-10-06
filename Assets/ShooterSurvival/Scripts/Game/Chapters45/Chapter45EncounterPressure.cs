using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Scaled chapter time drives warnings. There is no timer damage: only the bound
// Chapter45Hazard bodies can make physical contact with the player.
[DefaultExecutionOrder(180)]
[DisallowMultipleComponent]
public sealed class Chapter45EncounterPressure : MonoBehaviour
{
    public Chapter45Director director;
    public Chapter45Encounter encounter;
    public Chapter45Target target;
    public Chapter45Hazard[] alternatingSweeps = System.Array.Empty<Chapter45Hazard>();
    public float stopDistance, firstWarningDelay = 1.8f, warningCadence = 6f;
    public int floor;
    private Chapter45PressureSchedule schedule;
    private int nextSide;
    private float lastElapsed = -1;
    private bool wasHeld;

    private void Update()
    {
        if (director == null || !director.Running || director.Player == null || director.Player.currentHealth <= 0
            || director.gameObject.scene != gameObject.scene || director.CurrentFloor != floor || director.IsTransferring)
        { ResetPressure(); return; }

        // BeginRun can occur in-place. A rewind of the owner clock resets the cadence.
        if (director.Elapsed < lastElapsed) ResetPressure();
        lastElapsed = director.Elapsed;
        if (!TimeManager.isGameRunning || TimeManager.timeFactor <= 0) return;

        bool boundFight = encounter != null
            ? encounter.requiredToProceed && encounter.Activated && encounter.Eligible && !encounter.Complete
            : target != null && target.Alive && target.Eligible && target.Blocks(director.Distance + .1f, director.Lane);
        bool held = boundFight && Mathf.Abs(director.Distance - stopDistance) <= .3f;
        if (!held) { ResetPressure(); return; }
        wasHeld = true;
        // Never overlap warnings or bodies, including when workbook timing grows.
        foreach (var hazard in alternatingSweeps)
            if (hazard != null && hazard.Triggered && !hazard.Finished) return;
        if (!schedule.Step(director.Elapsed, true, firstWarningDelay, warningCadence) || alternatingSweeps.Length == 0) return;
        var next = alternatingSweeps[nextSide % alternatingSweeps.Length];
        nextSide++;
        next?.Trigger();
    }

    private void ResetPressure()
    {
        if (wasHeld)
            foreach (var hazard in alternatingSweeps) if (hazard != null) hazard.Cancel();
        schedule.Reset(); nextSide = 0; wasHeld = false; lastElapsed = -1;
    }
    private void OnDisable() => ResetPressure();
}

// The schedule uses the director's paused/scaled clock, never wall time. A long
// frame produces at most one warning rather than a catch-up burst.
public struct Chapter45PressureSchedule
{
    private bool armed;
    private float next, last;
    public void Reset() { armed = false; next = last = 0; }
    public bool Step(float elapsed, bool held, float firstDelay, float cadence)
    {
        if (!held || armed && elapsed < last) Reset();
        last = elapsed;
        if (!held) return false;
        if (!armed) { armed = true; next = elapsed + Mathf.Max(.5f, firstDelay); }
        if (elapsed + .0001f < next) return false;
        next = elapsed + Mathf.Max(3, cadence);
        return true;
    }
}
