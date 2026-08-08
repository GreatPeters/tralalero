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
        if (coin < amount) return false;
        Coin -= amount;
        return true;
    }

    public bool SpendJewel(int amount)
    {
        if (jewel < amount) return false;
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

        private static Material s_coinMaterial;
        private static Material s_coinFaceMaterial;
        private static Material s_coinArrowMaterial;
        private bool collected;
        private Vector3 basePosition;
        private Tween bobTween;
        private Transform arrowRootTransform;
        private Camera cachedMainCamera;

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

            CreateVisualPart(
                "Coin Body",
                PrimitiveType.Cylinder,
                pickupObject.transform,
                new Vector3(0f, 0f, 0f),
                new Vector3(0.5f, 0.1f, 0.5f),
                GetCoinMaterial());

            CreateVisualPart(
                "Coin Face Top",
                PrimitiveType.Cylinder,
                pickupObject.transform,
                new Vector3(0f, 0.102f, 0f),
                new Vector3(0.34f, 0.012f, 0.34f),
                GetCoinFaceMaterial());

            CreateVisualPart(
                "Coin Face Bottom",
                PrimitiveType.Cylinder,
                pickupObject.transform,
                new Vector3(0f, -0.102f, 0f),
                new Vector3(0.34f, 0.012f, 0.34f),
                GetCoinFaceMaterial());

            var arrowRoot = new GameObject("Coin Arrow Root");
            arrowRoot.transform.SetParent(pickupObject.transform, false);
            arrowRoot.transform.localPosition = new Vector3(0f, 0.92f, 0f);

            CreateVisualPart(
                "Coin Arrow Stem",
                PrimitiveType.Cube,
                arrowRoot.transform,
                new Vector3(0f, 0.03f, 0f),
                new Vector3(0.08f, 0.28f, 0.08f),
                GetCoinArrowMaterial());

            CreateVisualPart(
                "Coin Arrow Head Left",
                PrimitiveType.Cube,
                arrowRoot.transform,
                new Vector3(-0.08f, -0.12f, 0f),
                new Vector3(0.08f, 0.18f, 0.08f),
                GetCoinArrowMaterial(),
                new Vector3(0f, 0f, 45f));

            CreateVisualPart(
                "Coin Arrow Head Right",
                PrimitiveType.Cube,
                arrowRoot.transform,
                new Vector3(0.08f, -0.12f, 0f),
                new Vector3(0.08f, 0.18f, 0.08f),
                GetCoinArrowMaterial(),
                new Vector3(0f, 0f, -45f));

            var pickup = pickupObject.AddComponent<CoinPickup>();
            pickup.amount = amount;
            return pickup;
        }

        private static void CreateVisualPart(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            CreateVisualPart(name, primitiveType, parent, localPosition, localScale, material, Vector3.zero);
        }

        private static void CreateVisualPart(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, Vector3 localEulerAngles)
        {
            var visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.transform.localEulerAngles = localEulerAngles;

            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = visual.GetComponent<Renderer>();
            if (renderer == null)
                return;

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = material;
        }

        private static Material GetCoinMaterial()
        {
            if (s_coinMaterial != null)
                return s_coinMaterial;

            s_coinMaterial = CreateCoinMaterial(
                new Color(0.92f, 0.67f, 0.12f),
                new Color(0.55f, 0.34f, 0.03f),
                new Color(0.35f, 0.22f, 0.02f));
            return s_coinMaterial;
        }

        private static Material GetCoinFaceMaterial()
        {
            if (s_coinFaceMaterial != null)
                return s_coinFaceMaterial;

            s_coinFaceMaterial = CreateCoinMaterial(
                new Color(1f, 0.84f, 0.22f),
                new Color(0.85f, 0.62f, 0.08f),
                new Color(0.55f, 0.37f, 0.04f));
            return s_coinFaceMaterial;
        }

        private static Material GetCoinArrowMaterial()
        {
            if (s_coinArrowMaterial != null)
                return s_coinArrowMaterial;

            Shader shader = Shader.Find("URP/AlwaysOnTopUnlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            s_coinArrowMaterial = new Material(shader);
            Color arrowColor = new Color(1f, 0.9f, 0.32f, 0.88f);
            if (s_coinArrowMaterial.HasProperty("_BaseColor"))
                s_coinArrowMaterial.SetColor("_BaseColor", arrowColor);
            if (s_coinArrowMaterial.HasProperty("_Color"))
                s_coinArrowMaterial.SetColor("_Color", arrowColor);

            return s_coinArrowMaterial;
        }

        private static Material CreateCoinMaterial(Color baseColor, Color emissionColor, Color specColor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            material.color = baseColor;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", baseColor);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.9f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.8f);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.8f);
            if (material.HasProperty("_SpecColor"))
                material.SetColor("_SpecColor", specColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }

            return material;
        }

        private void Awake()
        {
            arrowRootTransform = transform.Find("Coin Arrow Root");
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
            transform.Rotate(Vector3.up, RotateSpeed * Time.deltaTime, Space.World);

            if (arrowRootTransform != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                arrowRootTransform.localScale = Vector3.one * pulse;
                arrowRootTransform.localPosition = new Vector3(0f, 0.92f + Mathf.Sin(Time.time * 4f) * 0.04f, 0f);
                if (cachedMainCamera == null)
                    cachedMainCamera = Camera.main;
                if (cachedMainCamera != null)
                    arrowRootTransform.forward = cachedMainCamera.transform.forward;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !other.CompareTag("Player"))
                return;

            collected = true;
            MoneyScript.S?.GetCoin(amount);
            DamagePopupFX.ShowCoin(transform.position + Vector3.up * 0.35f, amount);
            Destroy(gameObject);
        }
    }
}
