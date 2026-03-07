using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private int upgradeId = 1; // ?‘ì???'?ë³„ ?œë²ˆ'

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private GameObject dimb;   // MAX/Àá±İ Ç¥½Ã
    [SerializeField] private Button buyButton;

    [Header("Visual (optional)")]
    [SerializeField] private Color priceNormalColor = Color.white;
    [SerializeField] private Color priceNotEnoughColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color priceLockedColor = new Color(0.7f, 0.7f, 0.7f);

    private int level;

    private string FormatValue(float value, ValueType valueType)
    {
        string number = value.ToString("0.##");
        return valueType == ValueType.Percent ? $"{number}%" : number;
    }

    void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(Buy);
    }

    void Start()
    {
        Load();
        Refresh();
    }

    void OnEnable()
    {
        if (MoneyScript.S != null)
            MoneyScript.S.onChanged += Refresh;
    }

    void OnDisable()
    {
        if (MoneyScript.S != null)
            MoneyScript.S.onChanged -= Refresh;
    }

    void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(Buy);
    }

    void Refresh()
    {
        if (levelText != null)
            levelText.text = "Lv " + level;

        if (!UpgradeTables.TryGet(upgradeId, level + 1, out var next))
        {
            if (valueText != null) valueText.text = "MAX";
            if (priceText != null)
            {
                priceText.text = "-";
                priceText.color = priceLockedColor;
            }

            SetBuyState(canInteract: false, showDim: true);
            return;
        }

        float currentValue = 0f;
            currentValue = UpgradeStatManager.S.GetStat(next.type);

        // (? íƒ) ? ê¸ˆ ì¡°ê±´???ˆë‹¤ë©??¬ê¸°??ì²´í¬?´ì„œ "?????†ìŒ" ì²˜ë¦¬
        // ?? bool unlocked = PlayerLevel.S.Level >= next.requiredPlayerLevel;
        bool unlocked = true;

        if (!unlocked)
        {
            if (nameText != null) nameText.text = next.item;
            if (valueText != null)
                valueText.text = FormatValue(currentValue, next.valueType);
            if (priceText != null)
            {
                priceText.text = next.price.ToString();
                priceText.color = priceLockedColor;
            }

            SetBuyState(canInteract: false, showDim: true);
            return;
        }

        // ?´ê¸ˆ???????ˆìŒ) ????ë¶€ì¡±ì´?´ë„ ??X, ë²„íŠ¼?€ ?Œë¦¬ê²??
        if (nameText != null) nameText.text = next.item;

        if (valueText != null)
            valueText.text = FormatValue(currentValue, next.valueType);

        if (priceText != null)
            priceText.text = next.price.ToString();

        bool enoughMoney = HasEnoughMoney(next);

        SetBuyState(canInteract: true, showDim: false);

        if (priceText != null)
            priceText.color = enoughMoney ? priceNormalColor : priceNotEnoughColor;
    }

    void SetBuyState(bool canInteract, bool showDim)
    {
        if (buyButton != null)
            buyButton.interactable = canInteract;

        if (dimb != null)
            dimb.SetActive(showDim);
    }

    bool HasEnoughMoney(UpgradeRow row)
    {
        if (MoneyScript.S == null) return true;

        return row.priceType switch
        {
            PriceType.Coin => MoneyScript.S.Coin >= row.price,
            PriceType.Jewel => MoneyScript.S.Jewel >= row.price,
            _ => true
        };
    }

    void Buy()
    {
        // MAXë©?êµ¬ë§¤ ë¶ˆê?
        if (!UpgradeTables.TryGet(upgradeId, level + 1, out var next))
            return;

        // (? íƒ) ? ê¸ˆ ì¡°ê±´???ˆë‹¤ë©??¬ê¸°?œë„ ë§‰ê¸°
        // bool unlocked = PlayerLevel.S.Level >= next.requiredPlayerLevel;
        bool unlocked = true;
        if (!unlocked)
        {
            // TODO: ? ê¸ˆ ?ˆë‚´ ?ì—…/? ìŠ¤??            Debug.Log("?´ê¸ˆ ì¡°ê±´???„ìš”?©ë‹ˆ??");
            return;
        }

        // ??ë¶€ì¡±ì´ë©??ˆë‚´ë§??˜ê³  êµ¬ë§¤??????(ë²„íŠ¼?€ ?Œë¦¬ê²?? ì?)
        if (!HasEnoughMoney(next))
        {
            // TODO: ì½”ì¸ ë¶€ì¡??ì—…/ê´‘ê³  ? ë„/?ì  ?´ë™
            Debug.Log("ì½”ì¸??ë¶€ì¡±í•©?ˆë‹¤.");
            return;
        }

        if (!Pay(next))
            return;

        level++;
        Save();

        if (UpgradeStatManager.S != null)
            UpgradeStatManager.S.ApplyUpgrade(next.type, next.amount, next.valueType);

        var player = FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
        if (player != null) player.RefreshUpgradeStats();

        Refresh();
    }

    bool Pay(UpgradeRow row)
    {
        if (MoneyScript.S == null) return true;

        return row.priceType switch
        {
            PriceType.Coin => MoneyScript.S.SpendCoin(row.price),
            PriceType.Jewel => MoneyScript.S.SpendJewel(row.price),
            _ => true
        };
    }

    void Load()
    {
        level = PlayerPrefs.GetInt("upgrade_lv_" + upgradeId, 0);
    }

    void Save()
    {
        PlayerPrefs.SetInt("upgrade_lv_" + upgradeId, level);
        PlayerPrefs.Save();
    }
}





