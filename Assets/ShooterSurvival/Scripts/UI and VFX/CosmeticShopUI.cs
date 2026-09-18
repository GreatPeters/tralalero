using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CosmeticShopUI : MonoBehaviour
{
    public CosmeticVisualCatalog visuals;
    public CosmeticPreview preview;
    public Transform itemsRoot;
    public GameObject itemTemplate;
    public Button skinTab, shoesTab, hatTab, actionButton;
    public TMP_Text detailText, statusText, actionText;
    public TMP_Text selectionName, coinBalance, jewelBalance;
    public Image actionCurrencyIcon;
    public Sprite jewelIcon;
    public bool lightCards;
    public bool referencePresentation;
    public Sprite[] fittedCardIcons = System.Array.Empty<Sprite>();
    public Sprite activeTabSprite, inactiveTabSprite;
    public CoastalMessagePanel messagePanel;
    private readonly Dictionary<string, GameObject> cards = new();
    private CosmeticSlot? builtSlot;
    private CosmeticInventory builtInventory;
    private CosmeticSlot slot;
    private string selected;
    private bool dirty;
    private MoneyScript wallet;
    private float entrance;
    private CanvasGroup fade;

    private void Awake()
    {
        skinTab.onClick.AddListener(() => SelectSlot(CosmeticSlot.Skin));
        shoesTab.onClick.AddListener(() => SelectSlot(CosmeticSlot.Shoes));
        hatTab.onClick.AddListener(() => SelectSlot(CosmeticSlot.Hat));
        actionButton.onClick.AddListener(PurchaseOrEquip);
    }
    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        selected=null;
        if(statusText!=null)statusText.text="";
        var layer = GetComponent<Canvas>();
        if (layer != null) { layer.overrideSorting = true; layer.sortingOrder = 110; }
        CosmeticService.Changed += MarkDirty; wallet = MoneyScript.S;
        if (wallet != null) wallet.onChanged += MarkDirty;
        dirty = true;
        entrance = 0f; fade = GetComponent<CanvasGroup>();
    }
    private void OnDisable()
    {
        CosmeticService.Changed -= MarkDirty;
        if (wallet != null) wallet.onChanged -= MarkDirty;
    }
    private void LateUpdate()
    {
        entrance += Time.unscaledDeltaTime;
        if (fade != null) fade.alpha = Mathf.Clamp01(entrance / .18f);
        if (dirty) { dirty = false; Refresh(); }
    }
    private void MarkDirty() => dirty = true;
    public void SelectSlot(CosmeticSlot value) { if(slot==value)return; GameAudioService.Play(GameSound.Tab); slot = value; selected = null; statusText.text = ""; dirty = true; }
    public void SelectItem(string id) { selected = id; statusText.text = ""; dirty = true; }
    public void Open()
    {
        if (IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning) return;
        gameObject.SetActive(true);
        GameAudioService.Play(GameSound.Open);
    }
    public void Close()
    {
        gameObject.SetActive(false);
        GameAudioService.Play(GameSound.Close);
        var player = FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
        if (player != null) player.ResetStartGesture();
    }
    public void PurchaseOrEquip()
    {
        var inventory = CosmeticService.Current;
        if (inventory == null || selected == null) return;
        var result = inventory.PurchaseOrEquip(selected);
        GameAudioService.Play(result == CosmeticPurchaseResult.Purchased ? GameSound.Upgrade : result == CosmeticPurchaseResult.Equipped ? GameSound.Equip : GameSound.Denied);
        statusText.text = result switch {
            CosmeticPurchaseResult.Purchased => "구매하고 착용했습니다.",
            CosmeticPurchaseResult.Equipped => "착용했습니다.",
            CosmeticPurchaseResult.InsufficientFunds => "보석이 부족합니다.",
            _ => "잠시 후 다시 시도해 주세요." };
        if(result==CosmeticPurchaseResult.InsufficientFunds && messagePanel!=null)
        {
            int price=inventory.Find(selected).price;
            int balance=MoneyScript.S!=null?MoneyScript.S.Jewel:0;
            messagePanel.Show("보석이 부족해요",$"보유 보석  {balance:N0}\n필요 보석  {price:N0}\n부족한 보석  {Mathf.Max(0,price-balance):N0}");
        }
        dirty = true;
    }
    public void Refresh()
    {
        var theme = GameUITheme.Current;
        var inventory = CosmeticService.Current;
        if (inventory == null) { detailText.text = "상점 데이터를 읽을 수 없습니다."; actionButton.interactable = false; return; }
        if (coinBalance != null) coinBalance.text = (MoneyScript.S != null ? MoneyScript.S.Coin : 0).ToString("N0");
        if (jewelBalance != null) jewelBalance.text = (MoneyScript.S != null ? MoneyScript.S.Jewel : 0).ToString("N0");
        if (inventory.Find(selected)?.slot != slot) selected = inventory.Equipped(slot).id;
        bool rebuild = builtSlot != slot || cards.Count == 0 || !ReferenceEquals(builtInventory,inventory);
        if (rebuild)
        {
            foreach (var card in cards.Values) { card.SetActive(false); Destroy(card); }
            cards.Clear(); builtSlot = slot;
            if(builtInventory!=null&&!ReferenceEquals(builtInventory,inventory))preview.Dispose();
            builtInventory=inventory;
        }
        foreach (var row in inventory.Items.Where(r => r.slot == slot).OrderBy(r => r.price).ThenBy(r => r.id))
        {
            if (!cards.TryGetValue(row.id, out var go))
            {
                go = Instantiate(itemTemplate, itemsRoot, false); go.name = row.id; go.SetActive(true); cards.Add(row.id, go);
                string id = row.id; go.GetComponent<Button>().onClick.AddListener(() => SelectItem(id));
            }
            go.transform.Find("Name").GetComponent<TMP_Text>().text = row.name;
            bool equipped = inventory.Equipped(slot).id == row.id;
            go.transform.Find("State").GetComponent<TMP_Text>().text = equipped ? "장착 중" : inventory.Owns(row.id) ? "보유 중" : $"{row.price:N0}";
            var priceIcon = go.transform.Find("CurrencyIcon")?.GetComponent<Image>();
            if (priceIcon != null) { priceIcon.sprite = jewelIcon; priceIcon.gameObject.SetActive(!inventory.Owns(row.id)); }
            var effectLabel = go.transform.Find("Effect")?.GetComponent<TMP_Text>();
            if (effectLabel != null) effectLabel.text = EquipmentRunEffects.Describe(row);
            var image = go.transform.Find("Icon").GetComponent<Image>();
            var entry = visuals.Find(row.visualKey);
            image.sprite = referencePresentation ? fittedCardIcons.FirstOrDefault(icon=>icon!=null&&icon.name=="ShopIcon_"+row.visualKey)??entry?.icon : entry?.icon;
            image.color = entry?.icon != null ? Color.white : entry?.swatch ?? Color.white;
            go.GetComponent<Image>().color = referencePresentation ? Color.white : theme != null
                ? row.id == selected ? Color.Lerp(theme.card,theme.accent,.06f) : theme.card
                : lightCards
                ? row.id == selected ? new Color(.80f,.93f,.92f) : new Color(.94f,.94f,.90f)
                : row.id == selected ? new Color(.14f, .28f, .34f) : new Color(.065f, .13f, .18f);
            var marker = go.transform.Find("Selected");
            if (marker != null) marker.gameObject.SetActive(row.id == selected);
            if(referencePresentation)
            {
                var band=go.transform.Find("PriceBand")?.GetComponent<Image>();
                if(band!=null)band.color=equipped?new Color(.57f,.89f,.90f):new Color(.83f,.79f,.69f,.7f);
                var check=go.transform.Find("EquippedCheck");if(check!=null)check.gameObject.SetActive(equipped);
                var state=go.transform.Find("State").GetComponent<TMP_Text>();
                state.rectTransform.anchorMin=new Vector2(inventory.Owns(row.id)?.08f:.35f,.065f);
                state.rectTransform.anchorMax=new Vector2(inventory.Owns(row.id)?.92f:.79f,.185f);
            }
        }
        if(rebuild)
        {
            itemsRoot.GetComponent<EquipmentCardGrid>()?.RefreshLayout();
            var scroll=itemsRoot.GetComponentInParent<ScrollRect>();
            if(scroll!=null) { scroll.StopMovement(); scroll.verticalNormalizedPosition=1; }
        }
        var item = inventory.Find(selected);
        if (selectionName != null) selectionName.text = referencePresentation
            ? inventory.Equipped(slot).id == selected ? "현재 착용 중" : "미리보기" : item.name;
        string detailColor = theme != null ? ColorUtility.ToHtmlStringRGB(theme.ink) : "9DE8FF";
        detailText.text = item.description + "\n<color=#" + detailColor + ">" + EquipmentRunEffects.Describe(item) + "</color>";
        bool wearing = inventory.Equipped(slot).id == selected;
        bool owned=inventory.Owns(selected);
        bool affordable=MoneyScript.S!=null && MoneyScript.S.Jewel>=item.price;
        actionText.text = wearing ? "장착 중" : owned ? "장착" : $"구매   {item.price:N0}";
        if (actionCurrencyIcon != null) { actionCurrencyIcon.sprite = jewelIcon; actionCurrencyIcon.gameObject.SetActive(!inventory.Owns(selected)); }
        actionButton.interactable = !wearing && (owned || affordable || messagePanel!=null);
        var disabledFill=actionButton.transform.Find("DisabledFill");
        if(disabledFill!=null)disabledFill.gameObject.SetActive(wearing);
        if(lightCards)
        {
            bool available=!wearing;
            if(actionButton.targetGraphic is Image actionImage&&activeTabSprite!=null&&inactiveTabSprite!=null)
                actionImage.sprite=available?activeTabSprite:inactiveTabSprite;
            actionButton.targetGraphic.color = theme != null
                ? Color.white
                : available ? new Color(.87f,.69f,.38f) : new Color(.065f,.13f,.18f);
            actionText.color = theme != null ? available ? theme.buttonInk : theme.muted
                : available ? new Color(.025f,.065f,.10f) : new Color(.72f,.80f,.84f);
            var colors=actionButton.colors;colors.disabledColor=Color.white;actionButton.colors=colors;
        }
        string skin = inventory.Equipped(CosmeticSlot.Skin).visualKey;
        string shoes = inventory.Equipped(CosmeticSlot.Shoes).visualKey;
        string hat = inventory.Equipped(CosmeticSlot.Hat).visualKey;
        if (slot == CosmeticSlot.Skin) skin = item.visualKey;
        if (slot == CosmeticSlot.Shoes) shoes = item.visualKey;
        if (slot == CosmeticSlot.Hat) hat = item.visualKey;
        preview.Show(skin, shoes, hat);
        SetTabColor(skinTab, slot == CosmeticSlot.Skin); SetTabColor(shoesTab, slot == CosmeticSlot.Shoes); SetTabColor(hatTab, slot == CosmeticSlot.Hat);
    }
    private void SetTabColor(Button tab, bool selected)
    {
        if (activeTabSprite != null && inactiveTabSprite != null)
        {
            var image = tab.targetGraphic as Image;
            if (image != null) { image.sprite = selected ? activeTabSprite : inactiveTabSprite; image.color = Color.white; }
            var palette=GameUITheme.Current;
            tab.GetComponentInChildren<TMP_Text>().color = palette!=null ? (selected?palette.buttonInk:palette.ink) : new Color(.16f,.08f,.025f);
            return;
        }
        var theme = GameUITheme.Current;
        if (theme != null)
        {
            tab.targetGraphic.color = selected ? theme.primary : theme.card;
            tab.GetComponentInChildren<TMP_Text>().color = selected ? theme.buttonInk : theme.ink;
            return;
        }
        tab.GetComponent<Image>().color = selected ? new Color(.87f,.69f,.38f) : new Color(.075f,.145f,.20f);
        var label = tab.GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = selected ? new Color(.035f,.08f,.115f) : new Color(.72f,.80f,.84f);
    }
}
