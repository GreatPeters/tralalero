using System;
using System.Collections.Generic;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

[DisallowMultipleComponent]
public sealed class Chapter45Lift : MonoBehaviour
{
    public int afterSegment;
    [Tooltip("-1 keeps the next sequential segment. Other values select an authored route node.")]
    public int destinationSegment = -1;
    public int choiceIndex = -1, requiredChoice;
    public float minimumFloorSeconds;
    public bool requireFloorClear, exactDuration, escalator;
    public RestStopHoldout requiredHoldout;
    public int Destination(int current) => destinationSegment < 0 ? current + 1 : destinationSegment;
    public bool Eligible(Chapter45Director director) => !Used && director.ChoiceMatches(choiceIndex, requiredChoice);
    public bool Ready(Chapter45Director director) => Eligible(director)
        && director.FloorElapsed >= minimumFloorSeconds
        && (!requireFloorClear || director.CurrentFloorEncountersComplete)
        && (requiredHoldout == null || requiredHoldout.Completed);
    public float duration = 6;
    public Transform platform;
    [Tooltip("Aligned mall escalator: flat landings, continuously moving tread belt and ordinary follow camera.")]
    public bool continuousEscalator;
    public Transform escalatorEntry, escalatorExit;
    public Transform[] movingTreads = Array.Empty<Transform>();
    private float treadPhase;
    private void Update()
    {
        if (!continuousEscalator || !escalator || escalatorEntry == null || escalatorExit == null || movingTreads.Length == 0
            || !TimeManager.isGameRunning || TimeManager.timeFactor <= 0) return;
        treadPhase = Mathf.Repeat(treadPhase + Time.deltaTime * TimeManager.timeFactor / Mathf.Max(1, duration), 1);
        for (int i = 0; i < movingTreads.Length; i++)
            if (movingTreads[i] != null)
                movingTreads[i].position = Chapter45Rules.EscalatorPosition(escalatorEntry.position, escalatorExit.position,
                    Mathf.Repeat((float)i / movingTreads.Length + treadPhase, 1)) - Vector3.up * .08f;
    }

    [Tooltip("Optional interior shown only while riding; decorative and collision-free.")]
    public Transform travelCabin;
    public Transform[] doors = Array.Empty<Transform>();
    public Vector3 doorOpenOffset = Vector3.right * 2;
    public string destinationLabel = "상층 도착";
    public bool Used { get; internal set; }
    private Vector3 platformRest;
    private Vector3[] doorRest;
    private bool captured;
    private Renderer[] travelExterior = Array.Empty<Renderer>();
    private bool[] exteriorRenderingOff = Array.Empty<bool>();
    private bool travelCabinActive;
    public void SetTravelCabinActive(bool visible)
    {
        if (travelCabin == null) return;
        if (visible != travelCabinActive)
        {
            if (visible)
            {
                // The authored platform and rear glass keep their original state
                // outside travel. Hide them inside the closed rotating interior.
                var exterior = new List<Renderer>();
                foreach (var renderer in platform.GetComponentsInChildren<Renderer>(true))
                    if (!renderer.transform.IsChildOf(travelCabin)) exterior.Add(renderer);
                travelExterior = exterior.ToArray();
                exteriorRenderingOff = new bool[travelExterior.Length];
                for (int i = 0; i < travelExterior.Length; i++)
                {
                    exteriorRenderingOff[i] = travelExterior[i].forceRenderingOff;
                    travelExterior[i].forceRenderingOff = true;
                }
            }
            else
            {
                for (int i = 0; i < travelExterior.Length; i++)
                    if (travelExterior[i] != null) travelExterior[i].forceRenderingOff = exteriorRenderingOff[i];
            }
            travelCabinActive = visible;
        }
        travelCabin.gameObject.SetActive(visible);
    }
    public void ResetForRun()
    {
        if (!captured)
        {
            if (platform != null) platformRest = platform.position;
            doorRest = new Vector3[doors.Length];
            for (int i = 0; i < doors.Length; i++) if (doors[i] != null) doorRest[i] = doors[i].localPosition;
            captured = true;
        }
        Used = false; treadPhase = 0;
        SetTravelCabinActive(false);
        if (platform != null) platform.position = platformRest;
        SetDoorOpening(1);
    }
    public void MovePlatform(Vector3 displacement) { if (platform != null) platform.position = platformRest + displacement; }
    public void SetDoorOpening(float amount)
    {
        if (doorRest == null) return;
        for (int i = 0; i < doors.Length; i++) if (doors[i] != null) doors[i].localPosition = doorRest[i] + doorOpenOffset * (i % 2 == 0 ? -1 : 1) * Mathf.Clamp01(amount);
    }
}
