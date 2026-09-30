using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

public sealed class NoryangjinRushEvent : NoryangjinRevampEvent
{
    public GameObject[] templates = Array.Empty<GameObject>();
    public int rows = 4, perRow = 4;
    public float rowGap = 2.2f, spread = 4.6f, startAlong = 0;
    public float runSpeed = 4.2f;
    [Tooltip("Sideways speed toward the shark's lane (m/s); 0 keeps a straight line.")]
    public float homing = 1.1f;
    public float healthHits = 1.5f, damageShare = .06f;
    public int coinsEach = 2;
    public string animation = ForwardEnemyAnimationContract.Run;
    public float scale = 1.69f;
    public AudioClip[] shouts = Array.Empty<AudioClip>();
    private float nextShout;
    private int shoutIndex;
    private readonly List<GameObject> spawned = new();
    private readonly Dictionary<GameObject, float> footOffset = new();

    // Merchants run up and down the market ramp that climbs over the S3 east pier.
    private void FollowMarketFloor(GameObject go)
    {
        var branch = Director != null ? Director.Branch : null;
        if (branch == null || branch.liftHeight <= 0 || !branch.OnMarketFloor(go.transform.position)) return;
        var p = go.transform.position;
        footOffset.TryGetValue(go, out float offset);
        p.y = branch.transform.position.y + branch.HeightAt(p) + offset;
        go.transform.position = p;
    }

    public override void ResetForRun()
    {
        base.ResetForRun();
        nextShout = 0; shoutIndex = 0;
        foreach (var go in spawned) if (go != null) Destroy(go);
        spawned.Clear(); footOffset.Clear();
    }

    protected override void OnTriggered()
    {
        if (templates.Length == 0) return;
        if (shouts.Length > 0) { Director.Speak(shouts[0], true, 1.08f); shoutIndex = 1; nextShout = Director.Elapsed + 1.2f; }
        float attack = Player.ResolvedAttackDamage;
        int k = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < perRow; c++, k++)
            {
                var template = templates[k % templates.Length];
                if (template == null) continue;
                float lateral = perRow == 1 ? 0 : Mathf.Lerp(-spread * .5f, spread * .5f, c / (float)(perRow - 1)) + (r % 2 == 1 ? spread / perRow * .5f : 0);
                lateral = Mathf.Clamp(lateral, -spread * .5f, spread * .5f);
                // Noryangjin humanoid prefabs face local -Z.
                var go = Instantiate(template, At(startAlong + r * rowGap, lateral), Quaternion.LookRotation(transform.forward), transform);
                go.name = name + "_Merchant" + k;
                go.transform.localScale = Vector3.one * scale;
                StripToCombat(go);
                var merchant = go.AddComponent<NoryangjinMerchant>();
                merchant.locomotion = animation;
                go.SetActive(true);
                var enemy = go.GetComponent<EnemyScript_space>();
                if (enemy != null)
                {
                    enemy.ConfigureRewards(false, coinsEach);
                    enemy.ApplyStat(Player.MaxHealth * damageShare, Mathf.Max(30, attack * healthHits), EnemyTier.Normal);
                }
                var animator = go.GetComponentInChildren<Animator>();
                if (animator != null && animator.HasState(0, Animator.StringToHash(animation))) animator.Play(animation, 0, UnityEngine.Random.value);
                var branch = Director.Branch;
                // Rows spawn at the anchor's floor height; keep only their offset from that floor.
                footOffset[go] = branch != null && branch.OnMarketFloor(transform.position)
                    ? go.transform.position.y - branch.transform.position.y - branch.HeightAt(transform.position) : 0;
                FollowMarketFloor(go);
                spawned.Add(go);
            }
    }

    // Keep only what a free-running enemy needs; route/event controllers would fight the rush.
    public static void StripToCombat(GameObject go)
    {
        foreach (var c in go.GetComponents<Component>())
        {
            if (c is Transform || c is Animator || c is EnemyScript_space || c is Collider || c is AudioSource || c is Rigidbody) continue;
            DestroyImmediate(c);
        }
    }

    protected override void OnTick(float dt)
    {
        if (shoutIndex < shouts.Length && Director.Elapsed >= nextShout)
        { Director.Speak(shouts[shoutIndex++], true, .94f); nextShout = Director.Elapsed + 1.5f; }
        if (runSpeed <= 0) return;
        foreach (var go in spawned)
        {
            if (go == null || !go.activeInHierarchy) continue;
            var enemy = go.GetComponent<EnemyScript_space>();
            if (enemy != null && enemy.IsDead) continue;
            go.transform.position -= transform.forward * runSpeed * dt;
            if (homing > 0)
            {
                float gap = Lateral(Player.transform.position) - Lateral(go.transform.position);
                go.transform.position += transform.right * Mathf.Clamp(gap, -homing * dt, homing * dt);
            }
            FollowMarketFloor(go);
            if (-Ahead(go.transform.position) < -Ahead(Player.transform.position) - 30) go.SetActive(false);
        }
    }
}
