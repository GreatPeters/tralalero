using System;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

// Decorative pedestrians only. No colliders, combat, rewards or saved state.
// One driver per street block limits animation to the nearby part of the city.
public sealed class Chapter4StreetLife : MonoBehaviour
{
    [Serializable]
    public sealed class Walker
    {
        public Transform body;
        public Animator animator;
        public Vector3 origin;
        public float phase;
        public float travel = 4;
    }

    public Chapter45Director director;
    public float station;
    public Walker[] walkers = Array.Empty<Walker>();
    private float clock;
    private bool wasRunning;
    private bool initialized;

    public static Vector3 Position(Vector3 origin, float time, float phase, float travel)
        => origin + Vector3.forward * (Mathf.Sin(time * .32f + phase) * Mathf.Max(0, travel));

    private void Update()
    {
        if (director == null || director.chapter != 4) return;
        bool running = director.Running;
        if (running && !wasRunning) { clock = 0; initialized = false; }
        wasRunning = running;
        bool nearby = director.Distance >= station - 95 && director.Distance <= station + 45;
        bool moving = running && TimeManager.isGameRunning && Time.timeScale > 0;
        if (moving) clock += Time.deltaTime;
        foreach (var walker in walkers)
        {
            if (walker.body == null) continue;
            if (walker.body.gameObject.activeSelf != nearby) walker.body.gameObject.SetActive(nearby);
            if (!nearby) continue;
            walker.body.localPosition = Position(walker.origin, clock, walker.phase, walker.travel);
            float direction = Mathf.Cos(clock * .32f + walker.phase);
            walker.body.localRotation = Quaternion.Euler(0, direction >= 0 ? 0 : 180, 0);
            if (walker.animator != null)
            {
                if (!initialized) walker.animator.Play("walk", 0, Mathf.Repeat(walker.phase, 1));
                walker.animator.speed = moving ? Mathf.Abs(direction) * .65f + .15f : 0;
            }
        }
        initialized = nearby;
    }
}
