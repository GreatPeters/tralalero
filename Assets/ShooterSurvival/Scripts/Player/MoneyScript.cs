using System;
using TMPro;
using UnityEngine;

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

    public void GetCoin(int amount) => Coin += Mathf.Max(0, amount);
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

    public void ResetSave()
    {
        PlayerPrefs.DeleteKey(COIN_KEY);
        PlayerPrefs.DeleteKey(JEWEL_KEY);
        PlayerPrefs.Save();
        RefreshUI();
    }
}
