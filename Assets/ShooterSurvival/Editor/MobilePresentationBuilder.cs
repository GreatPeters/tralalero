using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class MobilePresentationBuilder
{
    private static MobileUIArt.Palette palette;
    private static TMP_FontAsset Font => MobileUIArt.Font;
    private static Sprite Round => MobileUIArt.Rounded;

    public static object ApplyOpenScene(int style = 0)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var scene = SceneManager.GetActiveScene();
        if (!new[] { "Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop" }.Contains(scene.name))
            throw new InvalidOperationException("A playable chapter is required.");
        string backup = "tmp/backups/mobile-presentation-2026-09-14/scenes/" + scene.name + ".before-ui.unity";
        if (!File.Exists(backup))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Scene snapshot failed.");
        }
        MobileUIArt.EnsureAssets(); palette = MobileUIArt.GetPalette(style); SaveTheme();
        var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas").GetComponent<CanvasScript>();
        var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerScript>(true)).Single();
        BuildUpgrades(canvas, player);
        RefinedGameUI.BuildShop(canvas.gameObject);
        StyleShop(canvas.GetComponentInChildren<CosmeticShopUI>(true));
        StyleLobby(canvas);
        BuildHud(canvas);
        BuildOtherScreens(canvas);
        foreach (var button in canvas.GetComponentsInChildren<Button>(true)) GetOrAdd<GameUIButtonSound>(button.gameObject);
        ApplyFonts(scene);
        AddSafeArea(canvas.transform.Find("UI"));
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Mobile presentation save failed.");
        return new { scene = scene.name, style = palette.name, upgrades = canvas.GetComponentsInChildren<UpgradeUI>(true).Length, font = Font.name };
    }

    private static void SaveTheme()
    {
        const string path = "Assets/ShooterSurvival/Resources/UI/MobileTheme.asset";
        var theme = AssetDatabase.LoadAssetAtPath<GameUITheme>(path);
        if (theme == null) { theme = ScriptableObject.CreateInstance<GameUITheme>(); AssetDatabase.CreateAsset(theme, path); }
        theme.paletteName = palette.name; theme.page = palette.page; theme.card = palette.card;
        theme.ink = palette.ink; theme.muted = palette.muted; theme.primary = palette.primary;
        theme.accent = palette.accent; theme.line = palette.line;
        EditorUtility.SetDirty(theme); AssetDatabase.SaveAssetIfDirty(theme);
    }

    private static void BuildUpgrades(CanvasScript canvas, PlayerScript player)
    {
        var groups = canvas.GetComponentsInChildren<UpgradeUI>(true).GroupBy(u => u.transform.parent).ToArray();
        foreach (var group in groups)
        {
            var content = (RectTransform)group.Key;
            var window = content.parent;
            if (window.name == "UpgradeViewport") window = window.parent;
            var cards = group.OrderBy(u => u.UpgradeId).ToList();
            var layer = GetOrAdd<Canvas>(window.gameObject);
            layer.overrideSorting = true; layer.sortingOrder = 100;
            GetOrAdd<MobileUILayer>(window.gameObject).Configure(100);
            if (window.GetComponent<GraphicRaycaster>() == null) window.gameObject.AddComponent<GraphicRaycaster>();
            if (cards.Count == 9)
            {
                var extra = Object.Instantiate(cards.Last().gameObject, content, false);
                extra.name = "UpgradeCard_10"; cards.Add(extra.GetComponent<UpgradeUI>());
            }
            if (cards.Count != 10) throw new InvalidOperationException("Expected ten stable upgrade cards.");
            var responsive = content.GetComponent<ResponsiveCardGrid>();
            if (responsive != null) Object.DestroyImmediate(responsive);
            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter != null) Object.DestroyImmediate(fitter);
            var viewport = window.Find("UpgradeViewport") as RectTransform;
            if (viewport == null)
            {
                viewport = MobileUIArt.Rect(window, "UpgradeViewport", .035f, .065f, .965f, .724f);
                viewport.gameObject.AddComponent<RectMask2D>();
                var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            }
            content.SetParent(viewport, false); MobileUIArt.SetRect(content, 0, 1, 1, 1); content.pivot = new Vector2(.5f, 1);
            var scroll = GetOrAdd<ScrollRect>(viewport.gameObject);
            scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false;
            scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 45;
            var grid = GetOrAdd<GridLayoutGroup>(content.gameObject);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            grid.spacing = new Vector2(22, 22); grid.padding = new RectOffset(8, 8, 8, 22);
            grid.childAlignment = TextAnchor.UpperCenter;
            var layout = GetOrAdd<EquipmentCardGrid>(content.gameObject);
            layout.grid = grid; layout.scroll = scroll; layout.cardHeight = 360;
            foreach (var card in cards) BuildUpgradeCard(card);
            RemoveNamed(window, "MobileUpgradeHeader");
            var header = MobileUIArt.Rect(window, "MobileUpgradeHeader", 0, 0, 1, 1);
            var back = MobileUIArt.Button(header, "Back", "뒤로", 31, .035f, .931f, .175f, .988f, palette, false);
            CopyButtonCalls(canvas.transform.Find("UI/Top/Back/Image").GetComponent<Button>(), back);
            UnityEventTools.AddBoolPersistentListener(back.onClick, window.gameObject.SetActive, false);
            var stats = MobileUIArt.Panel(header, "CurrentStats", .045f, .745f, .955f, .852f, palette);
            MobileUIArt.Text(stats.transform, "AttackLabel", "현재 공격력", 29, palette.muted, .05f, .61f, .48f, .92f);
            var attack = MobileUIArt.Text(stats.transform, "AttackValue", "8", 58, palette.ink, .05f, .05f, .48f, .64f);
            MobileUIArt.Text(stats.transform, "HealthLabel", "최대 체력", 29, palette.muted, .55f, .61f, .96f, .92f);
            var health = MobileUIArt.Text(stats.transform, "HealthValue", "60", 58, palette.ink, .55f, .05f, .96f, .64f);
            stats.gameObject.AddComponent<UpgradeSummaryUI>().Configure(player, attack, health);
            MobileUIArt.Text(header, "Title", "강화 상점", 60, palette.ink, .205f, .931f, .955f, .987f);
            var coinPanel = MobileUIArt.Panel(header, "Coins", .045f, .868f, .49f, .920f, palette, 4);
            var coinIcon = MobileUIArt.Image(coinPanel.transform, "Icon", .05f, .08f, .23f, .92f, Color.white);
            coinIcon.sprite = MobileUIArt.Coin; coinIcon.preserveAspect = true;
            var coins = MobileUIArt.Text(coinPanel.transform, "Balance", "0", 38, palette.ink, .25f, .05f, .95f, .95f, TextAlignmentOptions.Right);
            var gemPanel = MobileUIArt.Panel(header, "Jewels", .51f, .868f, .955f, .920f, palette, 4);
            var gemIcon = MobileUIArt.Image(gemPanel.transform, "Icon", .05f, .08f, .23f, .92f, Color.white);
            gemIcon.sprite = MobileUIArt.Jewel; gemIcon.preserveAspect = true;
            var gems = MobileUIArt.Text(gemPanel.transform, "Balance", "0", 38, palette.ink, .25f, .05f, .95f, .95f, TextAlignmentOptions.Right);
            header.gameObject.AddComponent<MobileWalletUI>().Configure(coins, gems);
            foreach (string name in new[] { "WorkshopTint", "WorkshopMerchant", "WorkshopFooter", "Button (1)" })
            {
                RemoveNamed(window, name);
            }
            var background = GetOrAdd<Image>(window.gameObject);
            background.sprite = null; background.color = palette.page;
            foreach (var button in window.GetComponentsInChildren<Button>(true))
                if (!button.transform.IsChildOf(content))
                {
                    StyleButton(button, false);
                }
            layout.RefreshLayout();
        }
    }

    private static void BuildUpgradeCard(UpgradeUI upgrade)
    {
        var data = new SerializedObject(upgrade);
        int id = upgrade.transform.GetSiblingIndex() + 1;
        var oldIcon = data.FindProperty("iconImage").objectReferenceValue as Image;
        Sprite iconSprite = oldIcon != null ? oldIcon.sprite : null;
        foreach (var oldLayout in upgrade.GetComponents<LayoutGroup>()) Object.DestroyImmediate(oldLayout);
        foreach (Transform child in upgrade.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        var background = GetOrAdd<Image>(upgrade.gameObject);
        background.sprite = Round; background.type = UnityEngine.UI.Image.Type.Sliced; background.color = palette.card;
        AddOutline(background.gameObject);
        var up = MobileUIArt.Rect(upgrade.transform, "Up", .025f, .34f, .975f, .98f);
        var icon = MobileUIArt.Image(up, "Icon", .04f, .58f, .22f, .94f, Color.white);
        icon.sprite = iconSprite; icon.preserveAspect = true;
        if (id == 10)
        {
            icon.gameObject.SetActive(false);
            MobileUIArt.Text(up, "LateralIcon", "< >", 39, palette.accent, .035f, .61f, .22f, .92f, TextAlignmentOptions.Center);
        }
        var title = MobileUIArt.Text(up, "Name", id == 10 ? "좌우 이동 속도 늘리기" : "강화", 34, palette.ink, .25f, .56f, .97f, .97f);
        title.enableAutoSizing = true; title.fontSizeMin = 28; title.fontSizeMax = 34;
        var level = MobileUIArt.Text(up, "Level", "레벨 0", 25, palette.muted, .05f, .40f, .95f, .60f);
        var current = MobileUIArt.Text(up, "CurrentValue", "0", 39, palette.ink, .04f, .06f, .43f, .37f, TextAlignmentOptions.Center);
        MobileUIArt.Text(up, "Arrow", ">", 30, palette.muted, .445f, .06f, .55f, .37f, TextAlignmentOptions.Center);
        var next = MobileUIArt.Text(up, "NextValue", "5%", 39, palette.ink, .57f, .06f, .96f, .37f, TextAlignmentOptions.Center);
        current.enableAutoSizing = next.enableAutoSizing = true;
        current.fontSizeMin = next.fontSizeMin = 25; current.fontSizeMax = next.fontSizeMax = 39;
        var buy = MobileUIArt.Button(upgrade.transform, "Down", "", 32, .035f, .035f, .965f, .31f, palette, true);
        var fill = buy.targetGraphic.transform;
        Object.DestroyImmediate(fill.Find("Label").gameObject);
        var buyLabel = MobileUIArt.Text(fill, "BuyLabel", "강화", 30, MobileUIArt.Hex("#173E4D"), .055f, .15f, .43f, .85f, TextAlignmentOptions.Center);
        buyLabel.enableAutoSizing = true; buyLabel.fontSizeMin = 22; buyLabel.fontSizeMax = 30;
        var currency = MobileUIArt.Image(fill, "CurrencyIcon", .45f, .22f, .60f, .78f, Color.white); currency.sprite = MobileUIArt.Coin; currency.preserveAspect = true;
        var price = MobileUIArt.Text(fill, "Price", "70", 34, MobileUIArt.Hex("#173E4D"), .62f, .13f, .94f, .87f, TextAlignmentOptions.Center);
        data.Update();
        data.FindProperty("upgradeId").intValue = id; data.FindProperty("layoutMode").enumValueIndex = 2;
        data.FindProperty("nameText").objectReferenceValue = title;
        data.FindProperty("levelText").objectReferenceValue = level;
        data.FindProperty("currentValueText").objectReferenceValue = current;
        data.FindProperty("nextValueText").objectReferenceValue = next;
        data.FindProperty("valueText").objectReferenceValue = current;
        data.FindProperty("descriptionText").objectReferenceValue = null;
        data.FindProperty("priceText").objectReferenceValue = price;
        data.FindProperty("iconImage").objectReferenceValue = icon;
        data.FindProperty("priceCurrencyImage").objectReferenceValue = currency;
        data.FindProperty("coinPriceSprite").objectReferenceValue = MobileUIArt.Coin;
        data.FindProperty("jewelPriceSprite").objectReferenceValue = MobileUIArt.Jewel;
        data.FindProperty("dimb").objectReferenceValue = null;
        data.FindProperty("buyButton").objectReferenceValue = buy;
        data.FindProperty("buyLabel").objectReferenceValue = buyLabel;
        data.FindProperty("priceNotEnoughColor").colorValue = MobileUIArt.Hex("#A72D37");
        data.FindProperty("priceLockedColor").colorValue = palette.muted;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StyleShop(CosmeticShopUI shop)
    {
        var root = shop.transform;
        root.GetComponent<Image>().color = palette.page;
        var stage = MobileUIArt.Panel(root, "HeroStage", .045f, .50f, .955f, .867f, palette);
        var preview = root.Find("PreviewArea") as RectTransform;
        preview.SetParent(stage.transform, false); MobileUIArt.SetRect(preview, .035f, .10f, .965f, .85f);
        shop.selectionName.transform.SetParent(stage.transform, false);
        MobileUIArt.SetRect(shop.selectionName.rectTransform, .05f, .845f, .95f, .985f);
        shop.selectionName.fontSize = 45;
        var rotate = root.Find("RotateHint") as RectTransform;
        rotate.SetParent(stage.transform, false); MobileUIArt.SetRect(rotate, .15f, .015f, .85f, .11f);
        Move(root, "EquipmentViewport", .045f, .173f, .945f, .404f);
        Move(root, "EquipmentScrollbar", .955f, .18f, .971f, .397f);
        Move(root, "SkinTab", .045f, .421f, .341f, .487f);
        Move(root, "ShoesTab", .352f, .421f, .648f, .487f);
        Move(root, "HatTab", .659f, .421f, .955f, .487f);
        Move(root, "ResetPreview", .805f, .516f, .94f, .55f);
        var reset = root.Find("ResetPreview"); if (reset != null) reset.SetAsLastSibling();
        var footer = root.Find("Footer").GetComponent<Image>(); footer.color = palette.page;
        var line = root.Find("HeaderLine"); if (line != null) line.gameObject.SetActive(false);
        var template = shop.itemTemplate.GetComponent<Image>(); template.sprite = Round; template.type = UnityEngine.UI.Image.Type.Sliced; template.color = palette.card;
        AddOutline(template.gameObject);
        shop.itemsRoot.GetComponent<EquipmentCardGrid>().cardHeight = 286;
        foreach (var image in shop.itemTemplate.GetComponentsInChildren<Image>(true))
            if (image.name == "Selected") image.color = palette.accent;
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) label.color = palette.ink;
        foreach (string name in new[] { "Subtitle", "RotateHint", "Status", "Detail" })
        {
            var label = root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == name);
            if (label != null) label.color = palette.muted;
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true)) StyleButton(button, button == shop.actionButton);
        shop.lightCards = true; EditorUtility.SetDirty(shop);
    }

    private static void StyleLobby(CanvasScript canvas)
    {
        var ui = canvas.transform.Find("UI");
        var background = ui.Find("Background")?.GetComponent<Image>();
        if (background != null) { background.sprite = null; background.color = Color.clear; background.raycastTarget = false; }
        var backdrop = ui.Find("Background/Background")?.GetComponent<Image>();
        if (backdrop != null) { backdrop.sprite = null; backdrop.color = Color.clear; backdrop.raycastTarget = false; }
        var bottom = ui.Find("Main/Bottom");
        if (bottom != null)
            foreach (var button in bottom.GetComponentsInChildren<Button>(true))
            {
                StyleButton(button, button.name.Contains("Upgrade"));
                string label = button.name.Contains("Upgrade") ? "강화" : button.name.Contains("Skin") ? "꾸미기" : button.name.Contains("Story") ? "이야기" : null;
                if (label != null)
                {
                    var text = button.GetComponentInChildren<TMP_Text>(true);
                    if (text == null) text = MobileUIArt.Text(button.transform, "MobileLabel", label, 39, palette.ink, .04f, .1f, .96f, .9f, TextAlignmentOptions.Center);
                    else text.text = label;
                }
            }
        foreach (var label in ui.GetComponentsInChildren<TMP_Text>(true)) { label.font = Font; label.fontSharedMaterial = Font.material; }
    }

    private static void BuildHud(CanvasScript canvas)
    {
        var hud = canvas.GetComponentInChildren<PlayerStatusHud>(true);
        if (hud == null) return;
        foreach (Transform child in hud.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        MobileUIArt.SetRect((RectTransform)hud.transform, 0, 0, 1, 1); AddSafeArea(hud.transform);
        var health = MobileUIArt.Panel(hud.transform, "HealthCard", .035f, .90f, .65f, .981f, palette);
        MobileUIArt.Text(health.transform, "Label", "체력", 30, palette.muted, .04f, .50f, .26f, .89f);
        var healthText = MobileUIArt.Text(health.transform, "Value", "60 / 60", 43, palette.ink, .28f, .47f, .95f, .94f, TextAlignmentOptions.Right);
        var track = MobileUIArt.Image(health.transform, "HealthTrack", .04f, .13f, .96f, .37f, Color.Lerp(palette.card, palette.line, .18f), true);
        var fill = MobileUIArt.Image(track.transform, "Fill", 0, 0, 1, 1, MobileUIArt.Hex("#F47772"), true);
        fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        var attack = MobileUIArt.Panel(hud.transform, "AttackCard", .675f, .90f, .965f, .981f, palette);
        MobileUIArt.Text(attack.transform, "Label", "공격력", 28, palette.muted, .08f, .55f, .94f, .93f);
        var attackText = MobileUIArt.Text(attack.transform, "Value", "8", 48, palette.ink, .08f, .08f, .94f, .60f, TextAlignmentOptions.Right);
        hud.Configure(healthText, fill, attackText);
        if (canvas.pauseButton != null)
        {
            var rect = canvas.pauseButton.GetComponent<RectTransform>();
            MobileUIArt.SetRect(rect, .83f, .817f, .96f, .884f);
            var button = canvas.pauseButton.GetComponent<Button>(); if (button != null) StyleButton(button, false);
        }
        EditorUtility.SetDirty(hud);
    }

    private static void ApplyFonts(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects()) MobileFontMigration.ApplyRoot(root);
        foreach (var root in scene.GetRootGameObjects())
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = Font; text.fontSharedMaterial = Font.material; text.fontStyle = FontStyles.Normal;
            EditorUtility.SetDirty(text);
            if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
    }
    private static void StyleButton(Button button, bool primary)
    {
        var image = button.targetGraphic as Image;
        if (image == null) image = button.GetComponent<Image>();
        if (image != null) { image.sprite = Round; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = primary ? palette.primary : palette.card; AddOutline(image.gameObject); }
        var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(.78f, .88f, .88f); colors.selectedColor = Color.white;
        colors.disabledColor = new Color(.72f, .76f, .76f, 1); button.colors = colors;
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true)) { text.font = Font; text.fontSharedMaterial = Font.material; text.color = primary ? MobileUIArt.Hex("#173E4D") : palette.ink; }
    }
    private static void AddOutline(GameObject root)
    {
        var outline = GetOrAdd<Outline>(root);
        outline.effectColor = palette.line; outline.effectDistance = new Vector2(3, -3); outline.useGraphicAlpha = true;
    }
    private static void AddSafeArea(Transform target)
    {
        if (target != null && target.GetComponent<MobileSafeArea>() == null) target.gameObject.AddComponent<MobileSafeArea>();
    }
    private static void Move(Transform root, string name, float x, float y, float xx, float yy)
    {
        var rect = root.Find(name) as RectTransform; if (rect != null) MobileUIArt.SetRect(rect, x, y, xx, yy);
    }
    private static void RemoveNamed(Transform root, string name)
    {
        var child = root.Find(name); if (child != null) Object.DestroyImmediate(child.gameObject);
    }
    private static T GetOrAdd<T>(GameObject root) where T : Component
    {
        var component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }
}
