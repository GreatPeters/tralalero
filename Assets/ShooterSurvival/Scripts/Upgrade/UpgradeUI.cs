using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UpgradeUI : MonoBehaviour
{
    private enum LayoutMode
    {
        Auto,
        Legacy,
        CardV2
    }

    [Header("Data")]
    [SerializeField] private int upgradeId = 1;
    [SerializeField] private LayoutMode layoutMode = LayoutMode.Auto;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI currentValueText;
    [SerializeField] private TextMeshProUGUI nextValueText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject dimb;
    [SerializeField] private Button buyButton;

    [Header("Visual (optional)")]
    [SerializeField] private SpriteDatabase spriteDatabase;
    [SerializeField] private string iconKey;
    [SerializeField] private Color priceNotEnoughColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color priceLockedColor = new Color(0.7f, 0.7f, 0.7f);

    private int level;
    private bool hasCachedPriceNormalColor;
    private Color cachedPriceNormalColor;

    private string FormatValue(float value, ValueType valueType)
    {
        string number = value.ToString("0.##");
        return valueType == ValueType.Percent ? $"{number}%" : number;
    }

    private string FormatNameWithLevel(string itemName, int currentLevel)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return string.Empty;

        return currentLevel > 0 ? $"{itemName} ({currentLevel})" : itemName;
    }

    void Awake()
    {
        SyncUpgradeIdFromSiblingOrder();
        AutoBindReferences();

        if (buyButton != null)
            buyButton.onClick.AddListener(Buy);
    }

    void Start()
    {
        SyncUpgradeIdFromSiblingOrder();
        AutoBindReferences();
        Load();
        SyncCurrentLevelStat();
        Refresh();
    }

    void OnValidate()
    {
        SyncUpgradeIdFromSiblingOrder();
        AutoBindReferences();
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
        bool useCardV2Layout = ResolveLayoutMode() == LayoutMode.CardV2;
        UpgradeTables.TryGet(upgradeId, level, out var currentRow);
        CachePriceNormalColor();

        if (!useCardV2Layout && levelText != null)
            levelText.text = "Lv " + level;

        if (!UpgradeTables.TryGet(upgradeId, level + 1, out var next))
        {
            ApplyIcon(currentRow);
            ApplyMaxTexts(useCardV2Layout, currentRow);

            if (descriptionText != null)
                descriptionText.text = string.Empty;

            if (priceText != null)
            {
                priceText.text = "-";
                priceText.color = priceLockedColor;
            }

            SetBuyState(canInteract: false, showDim: true);
            return;
        }

        ApplyIcon(next);

        float currentValue = 0f;
        if (UpgradeStatManager.S != null)
            currentValue = next.valueType == ValueType.Percent
                ? UpgradeStatManager.S.GetPercentStat(next.type)
                : UpgradeStatManager.S.GetFlatStat(next.type);

        bool unlocked = true;
        if (!unlocked)
        {
            ApplyTexts(currentRow, next, currentValue, useCardV2Layout);

            if (priceText != null)
            {
                priceText.text = next.price.ToString();
                priceText.color = priceLockedColor;
            }

            SetBuyState(canInteract: false, showDim: true);
            return;
        }

        ApplyTexts(currentRow, next, currentValue, useCardV2Layout);

        if (priceText != null)
            priceText.text = next.price.ToString();

        bool enoughMoney = HasEnoughMoney(next);

        SetBuyState(canInteract: true, showDim: false);

        if (priceText != null)
            priceText.color = enoughMoney ? cachedPriceNormalColor : priceNotEnoughColor;
    }

    void ApplyMaxTexts(bool useCardV2Layout, UpgradeRow currentRow)
    {
        if (nameText != null)
            nameText.text = FormatNameWithLevel(currentRow.item, level);

        if (!useCardV2Layout)
        {
            if (valueText != null)
                valueText.text = "MAX";
            return;
        }

        float currentValue = currentRow.level > 0 ? currentRow.amount : 0f;
        ValueType currentValueType = currentRow.level > 0 ? currentRow.valueType : ValueType.Value;

        if (currentValueText != null)
            currentValueText.text = FormatValue(currentValue, currentValueType);

        if (nextValueText != null)
            nextValueText.text = "MAX";

        if (valueText != null && currentValueText == null && nextValueText == null)
            valueText.text = "MAX";
    }

    void ApplyTexts(UpgradeRow currentRow, UpgradeRow next, float currentValue, bool useCardV2Layout)
    {
        if (nameText != null)
            nameText.text = FormatNameWithLevel(next.item, level);

        if (useCardV2Layout)
        {
            float displayCurrent = currentRow.level > 0 ? currentRow.amount : 0f;
            ValueType displayValueType = currentRow.level > 0 ? currentRow.valueType : next.valueType;

            if (currentValueText != null)
                currentValueText.text = FormatValue(displayCurrent, displayValueType);

            if (nextValueText != null)
                nextValueText.text = FormatValue(next.amount, next.valueType);

            if (valueText != null && currentValueText == null && nextValueText == null)
                valueText.text = FormatValue(displayCurrent, displayValueType);

            if (descriptionText != null)
                descriptionText.text = string.IsNullOrWhiteSpace(next.note) ? next.item : next.note;

            if (levelText != null)
                levelText.text = string.Empty;

            return;
        }

        if (valueText != null)
            valueText.text = FormatValue(currentValue, next.valueType);
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
        if (MoneyScript.S == null)
            return true;

        return row.priceType switch
        {
            PriceType.Coin => MoneyScript.S.Coin >= row.price,
            PriceType.Jewel => MoneyScript.S.Jewel >= row.price,
            _ => true
        };
    }

    void Buy()
    {
        if (!UpgradeTables.TryGet(upgradeId, level + 1, out var next))
            return;

        bool unlocked = true;
        if (!unlocked)
            return;

        if (!HasEnoughMoney(next))
        {
            Debug.Log("Not enough currency for upgrade.");
            return;
        }

        if (!Pay(next))
            return;

        level++;
        Save();

        if (UpgradeStatManager.S != null)
            UpgradeStatManager.S.ApplyUpgrade(next.type, next.amount, next.valueType);

        var player = FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
        if (player != null)
            player.RefreshUpgradeStats();

        Refresh();
    }

    bool Pay(UpgradeRow row)
    {
        if (MoneyScript.S == null)
            return true;

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

    void SyncCurrentLevelStat()
    {
        if (UpgradeStatManager.S == null)
            return;

        if (UpgradeTables.TryGet(upgradeId, level, out var currentRow) && currentRow.level > 0)
        {
            UpgradeStatManager.S.ApplyUpgrade(currentRow.type, currentRow.amount, currentRow.valueType);
            return;
        }

        if (UpgradeTables.TryGet(upgradeId, 1, out var firstRow))
            UpgradeStatManager.S.ApplyUpgrade(firstRow.type, 0f, firstRow.valueType);
    }

    void Save()
    {
        PlayerPrefs.SetInt("upgrade_lv_" + upgradeId, level);
        PlayerPrefs.Save();
    }

    void SyncUpgradeIdFromSiblingOrder()
    {
        if (transform.parent == null)
            return;

        upgradeId = transform.GetSiblingIndex() + 1;
    }

    LayoutMode ResolveLayoutMode()
    {
        if (layoutMode != LayoutMode.Auto)
            return layoutMode;

        return HasCardStructure() ? LayoutMode.CardV2 : LayoutMode.Legacy;
    }

    bool HasCardStructure()
    {
        return transform.Find("Up") != null || transform.Find("Down") != null;
    }

    void AutoBindReferences()
    {
        if (buyButton == null)
            buyButton = GetComponent<Button>();

        if (spriteDatabase == null)
            spriteDatabase = FindSpriteDatabaseAsset();

        BindTextsByHint();
        BindIconByHint();

        Transform up = transform.Find("Up");
        Transform down = transform.Find("Down");

        if (up != null)
        {
            var upTexts = GetDirectTexts(up);

            if (nameText == null && upTexts.Count > 0)
                nameText = upTexts[0];

            if (currentValueText == null && upTexts.Count > 1)
                currentValueText = upTexts[1];

            if (nextValueText == null && upTexts.Count > 2)
                nextValueText = upTexts[2];

            if (valueText == null && currentValueText != null)
                valueText = currentValueText;
        }

        if (down != null)
        {
            var downTexts = GetDirectTexts(down);

            if (descriptionText == null && downTexts.Count > 0)
                descriptionText = downTexts[0];

            if (priceText == null && downTexts.Count > 1)
                priceText = downTexts[1];

            if (priceText == null && downTexts.Count > 0)
                priceText = downTexts[0];

            if (dimb == null)
                dimb = FindDimObject(down, downTexts);
        }

        if (priceText != null && dimb == priceText.gameObject)
            dimb = null;
    }

    void BindTextsByHint()
    {
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
            TryBindText(text);
    }

    void TryBindText(TextMeshProUGUI text)
    {
        string hint = NormalizeHint(text.gameObject.name + " " + text.text);

        if (nameText == null && MatchesAny(hint, "name", "item", "title"))
        {
            nameText = text;
            return;
        }

        if (levelText == null && MatchesAny(hint, "lv", "level"))
        {
            levelText = text;
            return;
        }

        if (currentValueText == null && MatchesAny(hint, "current", "cur", "before", "prev", "now"))
        {
            currentValueText = text;
            if (valueText == null)
                valueText = text;
            return;
        }

        if (nextValueText == null && MatchesAny(hint, "next", "after", "target"))
        {
            nextValueText = text;
            return;
        }

        if (descriptionText == null && MatchesAny(hint, "note", "desc", "description", "info"))
        {
            descriptionText = text;
            return;
        }

        if (priceText == null && MatchesAny(hint, "price", "cost", "coinvalue", "coinvalue", "coin_value", "costtext"))
        {
            priceText = text;
            return;
        }

        if (valueText == null && MatchesAny(hint, "value", "stat"))
            valueText = text;
    }

    void BindIconByHint()
    {
        if (iconImage != null)
            return;

        var images = GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image == null || image == GetComponent<Image>())
                continue;

            string hint = NormalizeHint(image.gameObject.name);
            if (!MatchesAny(hint, "icon", "thumb", "sprite"))
                continue;

            iconImage = image;
            return;
        }
    }

    static string NormalizeHint(string raw)
    {
        return (raw ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
    }

    static bool MatchesAny(string source, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (source.Contains(key))
                return true;
        }

        return false;
    }

    void ApplyIcon(UpgradeRow currentRow)
    {
        if (iconImage == null || spriteDatabase == null)
            return;

        string[] keys = BuildIconKeyCandidates(currentRow);
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!spriteDatabase.TryGetSprite(key, out var sprite))
                continue;

            iconImage.sprite = sprite;
            return;
        }
    }

    void CachePriceNormalColor()
    {
        if (hasCachedPriceNormalColor || priceText == null)
            return;

        cachedPriceNormalColor = priceText.color;
        hasCachedPriceNormalColor = true;
    }

    string[] BuildIconKeyCandidates(UpgradeRow currentRow)
    {
        string rowItem = currentRow.item ?? string.Empty;
        string typeKey = currentRow.type.ToString();

        return new[]
        {
            iconKey,
            typeKey,
            NormalizeKey(typeKey),
            upgradeId.ToString(),
            "upgrade_" + upgradeId,
            NormalizeKey(rowItem),
            rowItem,
            NormalizeKey(gameObject.name),
            gameObject.name
        };
    }

    static string NormalizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
    }

    static SpriteDatabase FindSpriteDatabaseAsset()
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:SpriteDatabase");
        if (guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<SpriteDatabase>(path);
#else
        return null;
#endif
    }

    static List<TextMeshProUGUI> GetDirectTexts(Transform parent)
    {
        var results = new List<TextMeshProUGUI>();

        for (int i = 0; i < parent.childCount; i++)
        {
            var text = parent.GetChild(i).GetComponent<TextMeshProUGUI>();
            if (text != null)
                results.Add(text);
        }

        return results;
    }

    static GameObject FindDimObject(Transform root, List<TextMeshProUGUI> texts)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.GetComponent<TextMeshProUGUI>() != null)
                continue;

            string lowerName = child.name.ToLowerInvariant();

            if (lowerName.Contains("dim") || lowerName.Contains("lock") || lowerName.Contains("max"))
                return child.gameObject;
        }

        return null;
    }
}
