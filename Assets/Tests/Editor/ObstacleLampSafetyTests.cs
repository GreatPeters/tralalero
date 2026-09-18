using System.Reflection;
using DG.Tweening;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class ObstacleLampSafetyTests
{
    private GameObject lampObject, playerObject, bulletObject;
    private ObstacleStats lamp;
    private PlayerScript player;
    private Collider lampCollider, playerCollider, bulletCollider;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void Setup()
    {
        lampObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lampObject.name = "Lamp safety test";
        lampObject.transform.position = new Vector3(2, 3, 0);
        lampCollider = lampObject.GetComponent<Collider>();
        lampCollider.isTrigger = true;
        lamp = lampObject.AddComponent<ObstacleStats>();
        lamp.obstaclePattern = ObstaclePattern.Light;
        lamp.value = 50;
        Call("ResetState");
        playerObject = new GameObject("Player", typeof(BoxCollider));
        playerObject.tag = "Player";
        player = playerObject.AddComponent<PlayerScript>();
        player.currentHealth = 100;
        playerCollider = playerObject.GetComponent<Collider>();
        bulletObject = new GameObject("Bullet", typeof(BoxCollider));
        bulletObject.tag = "BulletTag";
        bulletCollider = bulletObject.GetComponent<Collider>();
    }

    [TearDown]
    public void Cleanup()
    {
        DOTween.Kill(lampObject.transform);
        Object.DestroyImmediate(lampObject);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(bulletObject);
    }

    private void Call(string method, params object[] args) =>
        typeof(ObstacleStats).GetMethod(method, Private).Invoke(lamp, args);

    [Test]
    public void StandingLamp_IsLethal()
    {
        Call("OnTriggerEnter", playerCollider);
        Assert.That(player.currentHealth, Is.Zero);
    }

    [Test]
    public void DurableLamp_StaysDangerousAfterAutoFire()
    {
        lamp.canBeShotDown = false;
        Call("OnTriggerEnter", bulletCollider);
        Assert.That(lampCollider.enabled, Is.True);
        Assert.That(lampObject.transform.rotation, Is.EqualTo(Quaternion.identity));
        Call("OnTriggerEnter", playerCollider);
        Assert.That(player.currentHealth, Is.Zero);
    }

    [Test]
    public void ShotLamp_DisarmsBeforeToppleAndIgnoresQueuedPlayerContact()
    {
        Call("OnTriggerEnter", bulletCollider);
        Assert.That(lampCollider.enabled, Is.False, "Toppling must not sweep damage into the safe lane");
        Call("OnTriggerEnter", playerCollider);
        Assert.That(player.currentHealth, Is.EqualTo(100), "Already queued trigger callbacks must be harmless too");
        Call("OnTriggerEnter", bulletCollider);
        Assert.That(lampCollider.enabled, Is.False);
    }

    [Test]
    public void RestartDuringTopple_RestoresOriginalColliderStatesAndPose()
    {
        var disabledCollider = lampObject.AddComponent<SphereCollider>();
        disabledCollider.enabled = false;
        Vector3 initialPosition = lampObject.transform.position;
        Quaternion initialRotation = lampObject.transform.rotation;
        Call("OnTriggerEnter", bulletCollider);
        Call("ResetState");
        DOTween.Complete(lampObject.transform);
        Assert.That(lampCollider.enabled, Is.True);
        Assert.That(disabledCollider.enabled, Is.False);
        Assert.That(lampObject.transform.position, Is.EqualTo(initialPosition));
        Assert.That(lampObject.transform.rotation, Is.EqualTo(initialRotation));
        Call("OnTriggerEnter", playerCollider);
        Assert.That(player.currentHealth, Is.Zero);
        Call("OnTriggerEnter", bulletCollider);
        Assert.That(lampCollider.enabled, Is.False, "A restarted lamp must be shootable again");
    }

    [Test]
    public void SettledLamp_RestoresContactAndUsesPlayerWideCooldown()
    {
        player.currentHealth = player.MaxHealth;
        float maximum = player.MaxHealth;
        Call("OnTriggerEnter", bulletCollider);
        DOTween.Complete(lampObject.transform);
        Assert.That(lampCollider.enabled, Is.True);
        Call("OnTriggerEnter", playerCollider);
        Call("OnTriggerStay", playerCollider);
        Assert.That(player.currentHealth, Is.EqualTo(maximum * .7f).Within(.001f));
        Assert.That(player.TryTakeFallenPoleDamage(Time.time + .99f), Is.False);
        Assert.That(player.TryTakeFallenPoleDamage(Time.time + 1f), Is.True);
        Assert.That(player.currentHealth, Is.EqualTo(maximum * .4f).Within(.001f));
    }
}
