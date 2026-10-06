using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

// A shootable object with health: styrofoam box walls, the overturned truck, LPG cylinders, the
// exit shutter and the turret truck. BulletScript hands projectiles to it. Health scales with the
// shark's current attack so a wall always takes about hitsToBreak shots.
[DisallowMultipleComponent]
public sealed class NoryangjinBreakable : MonoBehaviour
{
    public float hitsToBreak = 6;
    public float minimumHealth = 60;
    [Tooltip("Blocks forward movement while intact (the shark stops and shoots).")]
    public bool blocksPath = true;
    public float halfWidth = 1.5f, halfDepth = .6f;
    [Tooltip("Share of max HP drained per second while the shark is pressed against it (closed shutter).")]
    public float drainWhileBlocked;
    [Tooltip("LPG cylinder: breaking it destroys every breakable within this radius.")]
    public float explodeRadius;
    public int coins = 3;
    public bool armOnRunStart = true;
    public bool showHealth = true;
    public float healthLabelHeight = 2.4f;
    public float healthLabelFrontOffset;
    public float healthFontSize = 7;
    public Transform healthBarRoot, healthBarFill;
    public TMP_Text attachedDamageLabel;
    public Vector3 damageLabelOffset = new Vector3(1.3f, 1.8f, 0);
    public float healthReadoutDistance = 1000;
    public GameObject fishDropPrefab;
    public bool rewardsPerPiece;
    [Tooltip("Pieces knocked off one by one as health drops (turret cargo boxes).")]
    public Transform[] shedPieces = Array.Empty<Transform>();

    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool Armed { get; private set; }
    public bool Alive => Armed && Health > 0;
    public bool BlocksNow => blocksPath && Alive && gameObject.activeInHierarchy;
    public event Action<NoryangjinBreakable> Broke;

    private NoryangjinRevampDirector director;
    private TextMeshPro label;
    private Renderer[] renderers = Array.Empty<Renderer>();
    private Collider[] colliders = Array.Empty<Collider>();
    private readonly List<(Transform t, Vector3 p, Quaternion r, bool on)> shedRest = new();
    private readonly List<Rigidbody> flying = new();
    private Vector3 restLocal; private Quaternion restRotation;
    private float shake;
    private float damageLabelUntil, damageWindowEnd, pendingDisplayDamage;
    private Vector3 fullBarScale;

    public void Bind(NoryangjinRevampDirector owner)
    {
        director = owner;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        restLocal = transform.localPosition; restRotation = transform.localRotation;
        if (healthBarFill != null) fullBarScale = healthBarFill.localScale;
        shedRest.Clear();
        foreach (var piece in shedPieces) if (piece != null) shedRest.Add((piece, piece.localPosition, piece.localRotation, piece.gameObject.activeSelf));
        shedOrder = TopFirstOrder(shedPieces, transform);
        if (showHealth && label == null)
        {
            label = new GameObject("HealthLabel").AddComponent<TextMeshPro>();
            label.transform.SetParent(transform, false);
            label.font = Resources.Load<TMP_FontAsset>("UI/GmarketHarbor SDF");
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = healthFontSize; label.fontStyle = FontStyles.Bold;
            label.outlineWidth = .28f; label.outlineColor = new Color32(30, 20, 20, 255);
            label.rectTransform.sizeDelta = new Vector2(6, 2);
        }
    }

    public void ResetForRun()
    {
        transform.localPosition = restLocal; transform.localRotation = restRotation; restLocalWithMotion = restLocal;
        StopAllCoroutines();
        foreach (var rb in flying) if (rb != null) Destroy(rb);
        flying.Clear();
        foreach (var s in shedRest) { s.t.localPosition = s.p; s.t.localRotation = s.r; s.t.gameObject.SetActive(s.on); }
        SetShown(true);
        Armed = false; Health = MaxHealth = 0;
        damageLabelUntil = damageWindowEnd = pendingDisplayDamage = 0;
        if (attachedDamageLabel != null) attachedDamageLabel.gameObject.SetActive(false);
        if (armOnRunStart) Arm();
        else RefreshLabel();
    }

    public void Arm(float multiplier = 1)
    {
        float attack = director != null && director.Player != null ? director.Player.ResolvedAttackDamage : 50;
        MaxHealth = Health = Mathf.Max(minimumHealth, attack * hitsToBreak) * multiplier;
        Armed = true; SetShown(true); RefreshLabel();
    }

    // Absolute HP that does not follow the shark's attack, so attack upgrades shorten the break.
    public void ArmFixed(float health)
    {
        MaxHealth = Health = Mathf.Max(1f, health);
        Armed = true; SetShown(true); RefreshLabel();
    }

    public void ReceiveProjectile(BulletScript projectile)
    {
        if (!Alive) return;
        float damage = projectile != null && projectile.HasDamagePayload ? projectile.LaunchDamage
            : director != null && director.Player != null ? director.Player.ResolvedAttackDamage : 50;
        GameAudioService.PlayAt(GameSound.EnemyHit, transform.position);
        Damage(damage);
    }

    public void Damage(float amount)
    {
        if (!Alive || amount <= 0 || float.IsNaN(amount)) return;
        float before = Health;
        Health = Mathf.Max(0, Health - amount);
        if (attachedDamageLabel == null) DamagePopupFX.Show(transform.TransformPoint(damageLabelOffset), amount);
        else
        {
            if (Time.time >= damageWindowEnd) { pendingDisplayDamage = 0; damageWindowEnd = Time.time + .16f; }
            pendingDisplayDamage += amount;
            attachedDamageLabel.text = pendingDisplayDamage.ToString("F0");
            attachedDamageLabel.gameObject.SetActive(true); damageLabelUntil = Time.time + .5f;
        }
        shake = .18f;
        ShedPieces(before, Health);
        RefreshLabel();
        if (Health <= 0) Break();
    }
    public Vector3 CoinDropPosition(Vector3 scatter) => new Vector3(scatter.x,transform.position.y+.25f,scatter.z);
    private void DropCoin(Vector3 scatter,int amount)
    {
        var pickup=CoinPickup.Spawn(CoinDropPosition(scatter),CoinDropUtility.ApplyCoinBonus(amount));
        var visual=pickup.GetComponentInChildren<CoinTokenVisual>();
        if(visual!=null)visual.transform.localScale*=.45f;
    }

    private void ShedPieces(float before, float after)
    {
        if (shedPieces.Length == 0 || MaxHealth <= 0) return;
        int had = Mathf.CeilToInt(before / MaxHealth * shedPieces.Length), has = Mathf.CeilToInt(after / MaxHealth * shedPieces.Length);
        if (shedOrder == null || shedOrder.Length != shedPieces.Length) shedOrder = TopFirstOrder(shedPieces, transform);
        for (int i = has; i < had && i < shedPieces.Length; i++)
        {
            // Essential proposal 11: the highest box always goes first, so no box is left floating.
            // i counts down from Length-1 as health drops, so shedOrder[i] walks from the top piece down.
            var piece = shedPieces[shedOrder[i]];
            if (piece == null || !piece.gameObject.activeSelf) continue;
            if (!piece.TryGetComponent<Rigidbody>(out var rb)) rb = piece.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false; rb.useGravity = true;
            rb.AddForce((transform.right * UnityEngine.Random.Range(-1f, 1f) + Vector3.up * 1.6f - transform.forward * .6f) * 4, ForceMode.VelocityChange);
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 6, ForceMode.VelocityChange);
            flying.Add(rb);
            if (rewardsPerPiece)
            {
                DropCoin(piece.position,1);
                SpawnFish(piece.position);
            }
            StartCoroutine(HideLater(piece.gameObject, 1.6f));
        }
    }

    private int[] shedOrder;

    // Ascending by height. ShedPieces visits i = Length-1, Length-2, ... 0 over a wall's life,
    // so the highest piece is shed first. Equal heights fall back to array order.
    public static int[] TopFirstOrder(Transform[] pieces, Transform space)
    {
        var order = new int[pieces.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        float Height(int i) => pieces[i] == null ? float.MinValue
            : space != null ? space.InverseTransformPoint(pieces[i].position).y : pieces[i].position.y;
        Array.Sort(order, (a, b) =>
        {
            int byHeight = Height(a).CompareTo(Height(b));
            return byHeight != 0 ? byHeight : a.CompareTo(b);
        });
        return order;
    }

    private System.Collections.IEnumerator HideLater(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (go != null) go.SetActive(false);
    }

    private void Break()
    {
        GameAudioService.PlayAt(GameSound.EnemyDeath, transform.position);
        director?.Burst(explodeRadius > 0 ? director.bigBreakEffect : director.breakEffect, transform.position + Vector3.up, explodeRadius > 0 ? 1.6f : 1);
        if (coins > 0) DropCoin(transform.position,coins);
        SetShown(false);
        if (!rewardsPerPiece) SpawnFish(transform.position);
        Broke?.Invoke(this);
        if (explodeRadius > 0) director?.ExplodeAround(this, explodeRadius);
    }

    private void SpawnFish(Vector3 position)
    {
        if (fishDropPrefab != null)
        {
            var fish = Instantiate(fishDropPrefab, position + Vector3.up * .8f, Quaternion.Euler(0, 45, 75));
            fish.transform.localScale *= .28f;
            fish.SetActive(true);
            var body = fish.AddComponent<Rigidbody>();
            body.AddForce((Vector3.up + transform.right * .5f) * 2, ForceMode.VelocityChange);
            Destroy(fish, 2);
        }
    }

    private void SetShown(bool shown)
    {
        foreach (var r in renderers) if (r != null && r != label?.GetComponent<Renderer>()) r.enabled = shown;
        foreach (var c in colliders) if (c != null) c.enabled = shown;
        if (label != null) label.gameObject.SetActive(shown && showHealth);
    }

    private void RefreshLabel()
    {
        if (healthBarFill != null && MaxHealth > 0)
            healthBarFill.localScale = new Vector3(fullBarScale.x * Mathf.Clamp01(Health / MaxHealth), fullBarScale.y, fullBarScale.z);
        if (label == null) return;
        label.gameObject.SetActive(showHealth && Alive);
        label.text = Health.ToString("F0");
        label.color = Health < MaxHealth * .35f ? new Color(1, .45f, .35f) : Color.white;
    }

    private void LateUpdate()
    {
        bool visible=showHealth&&Alive;
        if(visible&&director!=null&&director.Player!=null)
        {
            var delta=transform.position-director.Player.transform.position;
            visible=delta.sqrMagnitude<=healthReadoutDistance*healthReadoutDistance&&Vector3.Dot(delta,director.Player.transform.forward)>-.5f;
        }
        if(label!=null)label.gameObject.SetActive(visible);
        if(healthBarRoot!=null)healthBarRoot.gameObject.SetActive(visible);
        if (healthBarRoot != null && Camera.main != null) healthBarRoot.rotation = Camera.main.transform.rotation;
        if (attachedDamageLabel != null && Time.time >= damageLabelUntil) attachedDamageLabel.gameObject.SetActive(false);
        if (label != null && label.gameObject.activeSelf)
        {
            label.transform.position = transform.position + Vector3.up * healthLabelHeight - transform.forward * healthLabelFrontOffset;
            var cam = Camera.main;
            if (cam != null) label.transform.rotation = cam.transform.rotation;
        }
        if (shake > 0)
        {
            shake -= Time.deltaTime;
            transform.localPosition = restLocalWithMotion + UnityEngine.Random.insideUnitSphere * .06f * Mathf.Clamp01(shake / .18f);
            if (shake <= 0) transform.localPosition = restLocalWithMotion;
        }
    }

    // Moving breakables (turret) update this; static ones keep their authored rest.
    [NonSerialized] public Vector3 restLocalWithMotion;
    private void OnEnable() { restLocalWithMotion = transform.localPosition; }
}
