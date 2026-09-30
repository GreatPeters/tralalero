using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

// Opt-in only on newly spawned market merchants; ordinary SR18 combat is unchanged.
public sealed class NoryangjinMerchant : MonoBehaviour
{
    // Kept serialized for historical test scripts; coin theft is no longer a gameplay rule.
    [HideInInspector] public bool coinContact;
    private EnemyScript_space enemy;
    private Animator animator;
    private float recoverAt;
    public string locomotion = ForwardEnemyAnimationContract.Run;
    public float DeathSeconds { get; private set; } = .5f;
    private void Awake()
    {
        enemy = GetComponent<EnemyScript_space>(); animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == "die") DeathSeconds = Mathf.Clamp(clip.length, .5f, 3f);
    }
    public void ReactToHit()
    {
        if (animator == null || !animator.HasState(0, Animator.StringToHash("hit")) || recoverAt > Time.time) return;
        animator.CrossFadeInFixedTime("hit", .04f); recoverAt = Time.time + .18f;
    }
    public void SpawnDeathCoins(int amount)
    {
        if(amount<=0)return;
        var pickup=CoinPickup.Spawn(transform.position+Vector3.up*.15f,amount);
        var visual=pickup.GetComponentInChildren<CoinTokenVisual>();
        if(visual!=null)visual.transform.localScale*=.5f;
        NoryangjinRevampDirector.Active?.Timeline.Add($"merchant death coins {amount}");
    }
    public static int CoinLoss(int available, int maximum) => Mathf.Min(Mathf.Max(0, available), Mathf.Max(0, maximum));
    private void Update()
    {
        if (recoverAt > 0 && Time.time >= recoverAt && enemy != null && !enemy.IsDead)
        { recoverAt = 0; if (animator != null) animator.CrossFadeInFixedTime(locomotion, .08f); }
    }
}
