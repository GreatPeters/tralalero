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
    private readonly List<GameObject> cards = new();
    private CosmeticSlot slot;
    private string selected;
    private bool dirty;
    private MoneyScript wallet;

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
        CosmeticService.Changed += MarkDirty; wallet = MoneyScript.S;
        if (wallet != null) wallet.onChanged += MarkDirty;
        dirty = true;
    }
    private void OnDisable()
    {
        CosmeticService.Changed -= MarkDirty;
        if (wallet != null) wallet.onChanged -= MarkDirty;
    }
    private void LateUpdate() { if (dirty) { dirty = false; Refresh(); } }
    private void MarkDirty() => dirty = true;
    public void SelectSlot(CosmeticSlot value) { slot = value; selected = null; statusText.text = ""; dirty = true; }
    public void SelectItem(string id) { selected = id; dirty = true; }
    public void PurchaseOrEquip()
    {
        var inventory = CosmeticService.Current;
        if (inventory == null || selected == null) return;
        var result = inventory.PurchaseOrEquip(selected);
        statusText.text = result switch {
            CosmeticPurchaseResult.Purchased => "구매하고 착용했습니다.",
            CosmeticPurchaseResult.Equipped => "착용했습니다.",
            CosmeticPurchaseResult.InsufficientFunds => "코인이 부족합니다.",
            _ => "잠시 후 다시 시도해 주세요." };
        dirty = true;
    }
    public void Refresh()
    {
        var inventory = CosmeticService.Current;
        if (inventory == null) { detailText.text = "상점 데이터를 읽을 수 없습니다."; actionButton.interactable = false; return; }
        if (inventory.Find(selected)?.slot != slot) selected = inventory.Equipped(slot).id;
        foreach (var card in cards) { card.SetActive(false); Destroy(card); } cards.Clear();
        foreach (var row in inventory.Items.Where(r => r.slot == slot).OrderBy(r => r.price).ThenBy(r => r.id))
        {
            var go = Instantiate(itemTemplate, itemsRoot, false); go.name = row.id; go.SetActive(true); cards.Add(go);
            go.transform.Find("Name").GetComponent<TMP_Text>().text = row.name;
            bool equipped = inventory.Equipped(slot).id == row.id;
            go.transform.Find("State").GetComponent<TMP_Text>().text = equipped ? "착용 중" : inventory.Owns(row.id) ? "보유" : $"{row.price:N0} 코인";
            var image = go.transform.Find("Icon").GetComponent<Image>();
            var entry = visuals.Find(row.visualKey); image.sprite = entry?.icon;
            image.color = entry?.icon != null ? Color.white : entry?.swatch ?? Color.white;
            go.GetComponent<Image>().color = row.id == selected ? new Color(1f, .85f, .55f) : new Color(.94f, .90f, .79f);
            string id = row.id; go.GetComponent<Button>().onClick.AddListener(() => SelectItem(id));
        }
        var item = inventory.Find(selected);
        detailText.text = item.name + "  ·  " + item.description;
        bool wearing = inventory.Equipped(slot).id == selected;
        actionText.text = wearing ? "착용 중" : inventory.Owns(selected) ? "착용하기" : $"구매하고 착용  {item.price:N0} 코인";
        actionButton.interactable = !wearing;
        string skin = inventory.Equipped(CosmeticSlot.Skin).visualKey;
        string shoes = inventory.Equipped(CosmeticSlot.Shoes).visualKey;
        string hat = inventory.Equipped(CosmeticSlot.Hat).visualKey;
        if (slot == CosmeticSlot.Skin) skin = item.visualKey;
        if (slot == CosmeticSlot.Shoes) shoes = item.visualKey;
        if (slot == CosmeticSlot.Hat) hat = item.visualKey;
        preview.Show(skin, shoes, hat);
        SetTabColor(skinTab, slot == CosmeticSlot.Skin); SetTabColor(shoesTab, slot == CosmeticSlot.Shoes); SetTabColor(hatTab, slot == CosmeticSlot.Hat);
    }
    private static void SetTabColor(Button tab, bool selected) => tab.GetComponent<Image>().color = selected ? new Color(.85f,.57f,.22f) : new Color(.19f,.25f,.29f);
}
