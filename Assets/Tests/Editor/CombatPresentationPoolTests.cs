#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class CombatPresentationPoolTests
{
    private readonly List<GameObject> objects = new();
    private GameObject Make(string name) { var root = new GameObject(name); objects.Add(root); return root; }
    private static void Tick(object pool, float seconds) =>
        pool.GetType().GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(pool, new object[] { seconds });

    [TearDown]
    public void Cleanup()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null) Object.DestroyImmediate(objects[i]);
        objects.Clear();
    }

    [Test]
    public void PopupBurst_ReusesBoundedObjects_AndExpiresWithoutDestroyingThem()
    {
        var pool = Make("Test popup pool").AddComponent<DamagePopupPool>();
        pool.Initialize(null);
        var original = pool.GetComponentsInChildren<Canvas>(true);
        Assert.That(original.Length, Is.EqualTo(64));
        for (int i = 0; i < 200; i++) pool.Show(Vector3.zero, i, false);
        CollectionAssert.AreEquivalent(original, pool.GetComponentsInChildren<Canvas>(true));
        Assert.That(original.Count(c => c.gameObject.activeSelf), Is.EqualTo(63), "One slot is reserved for player damage.");
        Tick(pool, 1f);
        Assert.That(original.All(c => !c.gameObject.activeSelf), Is.True);
        Assert.That(pool.transform.childCount, Is.EqualTo(64));
        Assert.That(pool.GetComponentsInChildren<GraphicRaycaster>(true), Is.Empty);
        Assert.That(pool.GetComponentsInChildren<TextMeshProUGUI>(true).All(t => !t.raycastTarget), Is.True);
    }

    [Test]
    public void PopupReuse_ResetsCoinFormattingColorScaleAndFade_AndHonorsPausedTime()
    {
        var pool = Make("Test popup pool").AddComponent<DamagePopupPool>();
        pool.Initialize(null);
        pool.Show(Vector3.one, 42, true);
        var text = pool.GetComponentsInChildren<TextMeshProUGUI>().Single();
        Assert.That(text.text, Is.EqualTo("+42"));
        Assert.That(text.canvas.transform.localScale.x, Is.EqualTo(.007f).Within(.0001f));
        var origin = text.canvas.transform.position;
        Tick(pool, 0f);
        Assert.That(text.canvas.transform.position, Is.EqualTo(origin));
        Tick(pool, .375f);
        Assert.That(text.color.a, Is.EqualTo(.75f).Within(.001f));
        Assert.That(text.canvas.transform.position.y, Is.GreaterThan(origin.y));
        Tick(pool, 1f);
        for (int i = 0; i < 64; i++) pool.Show(Vector3.zero, 7, false);
        Assert.That(text.text, Is.EqualTo("7"));
        Assert.That(text.color, Is.EqualTo(new Color(1f, .25f, .25f, 1f)));
        Assert.That(text.canvas.transform.localScale.x, Is.EqualTo(.01f).Within(.0001f));
    }

    [Test]
    public void PlayerLossSurvivesEnemyPopupSaturationAndCombinesExactAmounts()
    {
        var pool=Make("Player popup reservation").AddComponent<DamagePopupPool>();pool.Initialize(null);
        pool.ShowPlayerDamage(Vector3.zero,100.4f);
        for(int i=0;i<200;i++)pool.Show(Vector3.one,10,false);
        pool.ShowPlayerDamage(Vector3.zero,25.4f);
        var player=pool.GetComponentsInChildren<TextMeshProUGUI>().Single(t=>t.text.StartsWith("-"));
        Assert.That(player.text,Is.EqualTo("-126"));
        Assert.That(pool.GetComponentsInChildren<Canvas>(true).Length,Is.EqualTo(64));
        Tick(pool,1.2f);Assert.That(player.gameObject.activeInHierarchy,Is.False);
        pool.ShowPlayerDamage(Vector3.zero,5);Assert.That(player.text,Is.EqualTo("-5"));
    }

    private EnemyHitEffectPool PrepareEffect(out GameObject prefab, out Transform attachment)
    {
        prefab = Make("Test hit prefab");
        var particles = prefab.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        prefab.SetActive(false);
        attachment = Make("Test hit anchor").transform;
        EnemyHitEffectPool.Prewarm(prefab);
        var pool = Object.FindFirstObjectByType<EnemyHitEffectPool>();
        objects.Add(pool.gameObject);
        return pool;
    }

    [Test]
    public void HitBurst_RecyclesVisuals_AndReturnsThemWhenEnemyDisables()
    {
        var pool = PrepareEffect(out var prefab, out var attachment);
        var original = pool.GetComponentsInChildren<ParticleSystem>(true);
        Assert.That(original.Length, Is.EqualTo(32));
        for (int i = 0; i < 100; i++) EnemyHitEffectPool.Play(prefab, attachment);
        CollectionAssert.AreEquivalent(original, attachment.GetComponentsInChildren<ParticleSystem>(true));
        Tick(pool, 0f);
        Assert.That(attachment.childCount, Is.EqualTo(32), "Paused time must not expire active effects.");
        attachment.gameObject.SetActive(false);
        Tick(pool, .01f);
        Assert.That(attachment.childCount, Is.Zero);
        Assert.That(pool.transform.childCount, Is.EqualTo(32));
        Assert.That(original.All(p => !p.gameObject.activeSelf), Is.True);
        attachment.gameObject.SetActive(true);
        EnemyHitEffectPool.Play(prefab, attachment);
        Assert.That(attachment.childCount, Is.EqualTo(1));
        Tick(pool, 10f);
        Assert.That(attachment.childCount, Is.Zero);
    }

    [Test]
    public void HitPoolTeardown_RemovesActiveChildren_AndAllowsFreshScenePool()
    {
        var pool = PrepareEffect(out var prefab, out var attachment);
        EnemyHitEffectPool.Play(prefab, attachment);
        // EditMode-created behaviours do not receive the normal play-mode teardown callback.
        typeof(EnemyHitEffectPool).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(pool, null);
        Object.DestroyImmediate(pool.gameObject);
        Assert.That(attachment.childCount, Is.Zero);
        EnemyHitEffectPool.Prewarm(prefab);
        var replacement = Object.FindFirstObjectByType<EnemyHitEffectPool>();
        objects.Add(replacement.gameObject);
        Assert.That(replacement.transform.childCount, Is.EqualTo(32));
    }
}
#endif
