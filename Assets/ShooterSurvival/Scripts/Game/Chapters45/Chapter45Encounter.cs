using System;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Chapter45Encounter : MonoBehaviour
{
    public EnemyScript_space[] actors = Array.Empty<EnemyScript_space>();
    public float activationDistance, stopDistance;
    public bool requiredToProceed;
    public int floor, choiceIndex = -1, requiredChoice;
    public float health = 200, damage = 100;
    public string statPrefix = "c45_security";
    public int coinReward = 5;
    public string announcement = "경비대를 돌파하세요";
    public bool Activated { get; private set; }
    public bool Complete { get; private set; }
    private Chapter45Director director;
    private Vector3[] positions;
    private Quaternion[] rotations;

    public void ResetForRun(Chapter45Director owner)
    {
        director = owner; Activated = Complete = false;
        if (positions == null || positions.Length != actors.Length)
        {
            positions = new Vector3[actors.Length]; rotations = new Quaternion[actors.Length];
            for (int i = 0; i < actors.Length; i++) if (actors[i] != null) { positions[i] = actors[i].transform.position; rotations[i] = actors[i].transform.rotation; }
        }
        foreach (var actor in actors)
            if (actor != null)
            {
                actor.gameObject.SetActive(false);
                var deck = actor.GetComponent<Chapter45Deck>();
                if (deck == null) deck = actor.gameObject.AddComponent<Chapter45Deck>();
                deck.floor = floor;
            }
    }
    public bool Eligible => director != null && director.ChoiceMatches(choiceIndex, requiredChoice);
    public void Tick()
    {
        if (Complete || !Eligible || director.CurrentFloor != floor || director.IsTransferring) return;
        if (!Activated)
        {
            if (director.Distance < activationDistance) return;
            Activated = true;
            for (int i = 0; i < actors.Length; i++)
            {
                var actor = actors[i]; if (actor == null) continue;
                var events = actor.GetComponent<EnemyEventController>();
                if (events != null) events.PrepareSpawnAt(positions[i], rotations[i]);
                else actor.transform.SetPositionAndRotation(positions[i], rotations[i]);
                actor.gameObject.SetActive(true);
                actor.ApplyStat(Chapter45Director.Setting(statPrefix + "Damage", damage, 0, 1000000), Chapter45Director.Setting(statPrefix + "Health", health, 1, 10000000), EnemyTier.Normal);
                actor.ConfigureRewards(false, coinReward);
                events?.ActivateFromSpot();
            }
            director.Record("encounter start " + name);
            if (!string.IsNullOrEmpty(announcement)) director.Announce("경비 구역", announcement, 2);
        }
        foreach (var actor in actors) if (actor != null && !actor.IsDead) return;
        Complete = true; director.Record("encounter cleared " + name);
    }
    public void Retire()
    {
        foreach (var actor in actors) if (actor != null) actor.gameObject.SetActive(false);
    }
}
