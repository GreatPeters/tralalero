using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

public sealed class NoryangjinCatEvent : NoryangjinRevampEvent
{
    public Transform cat;
    public NoryangjinBreakable protectedStall;
    public int coins = 15;
    private bool following, resolved;
    private Vector3 rest;
    private bool captured;
    public override void ResetForRun()
    {
        base.ResetForRun(); following = resolved = false;
        if (cat == null) return;
        if (!captured) { rest = cat.localPosition; captured = true; }
        cat.localPosition = rest; cat.gameObject.SetActive(true);
    }
    protected override void OnTriggered() { }
    protected override void OnTick(float dt)
    {
        if (resolved || cat == null) return;
        if (!following && Ahead(Player.transform.position) < -7)
        {
            if (protectedStall == null || protectedStall.Health < protectedStall.MaxHealth)
            { resolved = true; return; }
            following = true;
            Director.Hud?.Publish("가게를 지켜줬다냥!", 2);
        }
        if (!following) return;
        Vector3 goal = Player.transform.position - Player.transform.forward * 1.2f;
        Vector3 delta = goal - cat.position; delta.y = 0;
        if (delta.sqrMagnitude > .01f) cat.rotation = Quaternion.LookRotation(delta);
        cat.position = Vector3.MoveTowards(cat.position, goal, 9 * dt);
        if ((cat.position - goal).sqrMagnitude < 1)
        {
            resolved = true;
            CoinPickup.Spawn(cat.position + Vector3.up * .5f, CoinDropUtility.ApplyCoinBonus(coins));
            Director.Timeline.Add($"cat reward at {Director.Elapsed:F1}");
        }
    }
}
