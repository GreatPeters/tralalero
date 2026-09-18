using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival.Analytics;

public class MoneyScript : MonoBehaviour
{
    public static MoneyScript S;

    public Action onChanged;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI jewelText;

    [Header("Money")]
    [SerializeField] private int coin;
    [SerializeField] private int jewel;

    const string COIN_KEY = "coin";
    const string JEWEL_KEY = "jewel";

    void Awake()
    {
        if (S != null && S != this)
        {
            Destroy(gameObject);
            return;
        }
        S = this;

        Load();
    }

    public int Coin
    {
        get => coin;
        set { coin = Mathf.Max(0, value); RefreshUI(); }
    }

    public RectTransform CoinTarget => coinText != null ? coinText.rectTransform : null;

    public int Jewel
    {
        get => jewel;
        set { jewel = Mathf.Max(0, value); RefreshUI(); }
    }

    void Start() => RefreshUI();

    void RefreshUI()
    {
        if (coinText != null) coinText.text = coin.ToString();
        if (jewelText != null) jewelText.text = jewel.ToString();

        Save();
        onChanged?.Invoke();
    }

    public void GetCoin(int amount)
    {
        int earnedAmount = Mathf.Max(0, amount);
        Coin += earnedAmount;
        GameplayAnalytics.RecordCoinEarned(earnedAmount);
    }
    public void GetJewel(int amount) => Jewel += Mathf.Max(0, amount);

    public bool SpendCoin(int amount)
    {
        if (amount < 0 || coin < amount) return false;
        Coin -= amount;
        return true;
    }

    public bool SpendJewel(int amount)
    {
        if (amount < 0 || jewel < amount) return false;
        Jewel -= amount;
        return true;
    }

    void Save()
    {
        PlayerPrefs.SetInt(COIN_KEY, coin);
        PlayerPrefs.SetInt(JEWEL_KEY, jewel);
        PlayerPrefs.Save();
    }

    void Load()
    {
        coin = PlayerPrefs.GetInt(COIN_KEY, coin);
        jewel = PlayerPrefs.GetInt(JEWEL_KEY, jewel);
    }

}

namespace IndianOceanAssets.ShooterSurvival
{
    public static class CoinDropUtility
    {
        public static int GetCoinAmount(EnemyType enemyType)
        {
            return enemyType switch
            {
                EnemyType.Walker => 10,
                EnemyType.Rusher => 20,
                EnemyType.Tank => 50,
                _ => 10
            };
        }

        public static int GetCoinAmount(EnemyTier enemyTier)
        {
            return enemyTier switch
            {
                EnemyTier.Normal => 10,
                EnemyTier.Elite => 20,
                EnemyTier.Boss => 50,
                _ => 10
            };
        }

        public static int ApplyCoinBonus(int baseCoin)
        {
            float bonus = UpgradeStatManager.S != null
                ? UpgradeStatManager.S.GetPercentStat(UpgradeStatManager.UpgradeType.COIN_BONUS)
                : 0f;

            return Mathf.Max(1, Mathf.RoundToInt(baseCoin * (1f + bonus / 100f)));
        }

        public static void SpawnWorldCoinDrop(Vector3 worldPosition, int amount)
        {
            if (amount <= 0)
                return;

            Vector3 spawnPosition = worldPosition;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 toPlayer = player.transform.position - worldPosition;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                    spawnPosition -= toPlayer.normalized * 1.5f;
            }

            CoinPickup.Spawn(spawnPosition, amount);
        }
    }

    public class CoinPickup : MonoBehaviour
    {
        private int amount;
        private const float BobHeight = 0.28f;
        private const float BobDuration = 0.75f;
        private const float RotateSpeed = 120f;

        private bool collected;
        private Vector3 basePosition;
        private Tween bobTween;
        private Transform collector;
        private float collectionRadius;

        public static CoinPickup Spawn(Vector3 worldPosition, int amount)
        {
            GameObject pickupObject = new GameObject($"Coin Pickup ({amount})");
            pickupObject.transform.position = worldPosition + Vector3.up * 1.1f;

            var trigger = pickupObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;

            var rb = pickupObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            CoinTokenVisual.Attach(pickupObject.transform);

            var pickup = pickupObject.AddComponent<CoinPickup>();
            pickup.amount = amount;
            return pickup;
        }

        private void Awake()
        {
            collectionRadius = 3.5f;
            if (EnvironmentVariableTables.TryGetFloat("coinPickupRadius_" + gameObject.scene.name, out float configured) && !float.IsNaN(configured) && !float.IsInfinity(configured))
            {
                collectionRadius = Mathf.Clamp(configured, 0f, 6f);
            }
            collector = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void OnEnable()
        {
            basePosition = transform.position;
            bobTween?.Kill();
            bobTween = transform.DOMoveY(basePosition.y + BobHeight, BobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDisable()
        {
            bobTween?.Kill();
            bobTween = null;
        }

        private void Update()
        {
            if (!collected && collector != null && collectionRadius > 0 && TimeManager.isGameRunning)
            {
                Vector3 delta = collector.position - transform.position;
                if (Mathf.Abs(delta.y) < 2.5f && delta.x * delta.x + delta.z * delta.z <= collectionRadius * collectionRadius) Collect();
            }
            transform.Rotate(Vector3.up, RotateSpeed * Time.deltaTime, Space.World);

        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !TimeManager.isGameRunning || other.GetComponentInParent<PlayerScript>() == null)
                return;
            Collect();
        }
        private void Collect()
        {
            if (collected || MoneyScript.S == null) return;
            collected = true;
            MoneyScript.S.GetCoin(amount);
            GameAudioService.Play(GameSound.Coin);
            DamagePopupFX.ShowCoin(transform.position + Vector3.up * 0.35f, amount);
            Destroy(gameObject);
        }
    }
}
