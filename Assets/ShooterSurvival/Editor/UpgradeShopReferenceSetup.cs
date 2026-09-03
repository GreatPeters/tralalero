#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UpgradeShopReferenceSetup
{
    public const string MenuPath =
        "Tools/Shooter Survival/UI/Rebuild Upgrade Shoe Workshop";

    internal const string BackgroundPath =
        "Assets/JH/UI/Upgrade/Workshop_Background.png";
    internal const string CardSpritePath =
        "Assets/JH/UI/Upgrade/카드.png";
    internal const string SignSpritePath =
        "Assets/JH/UI/Upgrade/메뉴이름.png";
    internal const string OverlayName = "WorkshopTint";
    internal const string SubtitleName = "WorkshopSubtitle";
    internal const string FooterName = "WorkshopFooter";
    internal const string PriceBarName = "PriceBar";

    private static readonly Color Ink = new(0.12f, 0.055f, 0.02f, 1f);
    private static readonly Color PriceGold = new(1f, 0.76f, 0.2f, 1f);
    private static readonly Color PriceBarColor = new(0.16f, 0.075f, 0.025f, 0.94f);

    [MenuItem(MenuPath, false, 2405)]
    public static void ConfigureOpenScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rebuild Upgrade Shoe Workshop");
        try
        {
            EnsureSpriteImport(BackgroundPath, 2048);
            Transform upgradeRoot = ResolveReachableUpgradeRoot(scene);
            if (upgradeRoot == null)
                throw new InvalidOperationException(
                    "The main Upgrade button does not reference an upgrade panel.");

            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            Sprite cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
            Sprite signSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SignSpritePath);
            if (background == null || cardSprite == null || signSprite == null)
                throw new InvalidOperationException("Upgrade workshop sprites are not imported.");

            PrepareReachableHierarchy(upgradeRoot);
            ConfigureRoot(upgradeRoot, background);
            ConfigureHeader(upgradeRoot, signSprite);
            ConfigureCards(upgradeRoot, cardSprite);
            ConfigureFooter(upgradeRoot, signSprite);

            EditorUtility.SetDirty(upgradeRoot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[Upgrade Shop] Rebuilt the reachable 3x3 shoe-workshop upgrade UI.");
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
    }

    internal static Transform ResolveReachableUpgradeRoot(Scene scene)
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button button in buttons)
        {
            if (button == null ||
                button.gameObject.scene != scene ||
                button.name != "Upgrade_Button")
            {
                continue;
            }

            for (int index = 0;
                 index < button.onClick.GetPersistentEventCount();
                 index++)
            {
                if (button.onClick.GetPersistentMethodName(index) != "SetActive")
                    continue;

                GameObject target =
                    button.onClick.GetPersistentTarget(index) as GameObject;
                if (target != null &&
                    target.GetComponentsInChildren<UpgradeUI>(true).Length == 9)
                {
                    return target.transform;
                }
            }
        }

        return null;
    }

    private static void PrepareReachableHierarchy(Transform root)
    {
        GridLayoutGroup legacyRootGrid = root.GetComponent<GridLayoutGroup>();
        if (legacyRootGrid != null)
            Undo.DestroyObjectImmediate(legacyRootGrid);

        Transform top = root.parent?.Find("Top");
        if (top != null && root.GetSiblingIndex() > top.GetSiblingIndex())
        {
            Undo.RecordObject(root, "Order Upgrade Workshop Below Global Navigation");
            root.SetSiblingIndex(top.GetSiblingIndex());
        }

        if (root is RectTransform rootRect)
        {
            SetStretch(
                rootRect,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
        }

        RectTransform header = GetOrCreateRect(root, "Button (1)");
        if (header.GetComponent<Image>() == null)
            Undo.AddComponent<Image>(header.gameObject);
        if (header.GetComponentInChildren<TextMeshProUGUI>(true) == null)
            GetOrCreateText(header, "Text (TMP)", null, "업그레이드");

        UpgradeUI[] cards = root.GetComponentsInChildren<UpgradeUI>(true)
            .OrderBy(card => card.transform.GetSiblingIndex())
            .ToArray();
        if (cards.Length != 9)
            throw new InvalidOperationException($"Expected 9 upgrade cards, found {cards.Length}.");

        RectTransform container = GetOrCreateRect(root, "GameObject");
        foreach (UpgradeUI card in cards)
        {
            FlattenCardStructure(card);
            if (card.transform.parent != container)
                Undo.SetTransformParent(card.transform, container, "Move Upgrade Card");
        }
    }

    private static void FlattenCardStructure(UpgradeUI card)
    {
        Transform root = card.transform;
        var serialized = new SerializedObject(card);
        MoveReferencedChild(serialized, "iconImage", root, "Icon");
        MoveReferencedChild(serialized, "nameText", root, "Name");
        MoveReferencedChild(serialized, "currentValueText", root, "Value");
        MoveReferencedChild(serialized, "nextValueText", root, "NextValue");
        MoveReferencedChild(serialized, "descriptionText", root, "Description");
        MoveReferencedChild(serialized, "priceText", root, "Coin_Value");

        Transform currency = root.Find("Down/Coin");
        if (currency != null)
            MoveChild(currency, root, "CoinIcon");

        TextMeshProUGUI name = root.Find("Name")
            ?.GetComponent<TextMeshProUGUI>();
        GetOrCreateText(root, "Lv", name, string.Empty);

        Transform up = root.Find("Up");
        Transform down = root.Find("Down");
        if (up != null)
            Undo.DestroyObjectImmediate(up.gameObject);
        if (down != null)
            Undo.DestroyObjectImmediate(down.gameObject);
    }

    private static void MoveReferencedChild(
        SerializedObject serialized,
        string propertyName,
        Transform parent,
        string targetName)
    {
        Component component = serialized.FindProperty(propertyName)
            ?.objectReferenceValue as Component;
        if (component != null)
            MoveChild(component.transform, parent, targetName);
    }

    private static void MoveChild(
        Transform child,
        Transform parent,
        string targetName)
    {
        Undo.SetTransformParent(child, parent, "Flatten Upgrade Card");
        Undo.RecordObject(child.gameObject, "Rename Upgrade Card Child");
        child.gameObject.name = targetName;
    }

    private static void ConfigureRoot(Transform root, Sprite background)
    {
        Image rootImage = root.GetComponent<Image>();
        if (rootImage == null)
            rootImage = Undo.AddComponent<Image>(root.gameObject);
        Undo.RecordObject(rootImage, "Style Upgrade Workshop");
        rootImage.sprite = background;
        rootImage.color = Color.white;
        rootImage.preserveAspect = false;
        rootImage.raycastTarget = false;

        RectTransform overlay = GetOrCreateRect(root, OverlayName);
        SetStretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.SetAsFirstSibling();
        Image overlayImage = overlay.GetComponent<Image>();
        if (overlayImage == null)
            overlayImage = Undo.AddComponent<Image>(overlay.gameObject);
        Undo.RecordObject(overlayImage, "Style Upgrade Workshop");
        overlayImage.sprite = null;
        overlayImage.color = new Color(0.055f, 0.025f, 0.012f, 0.22f);
        overlayImage.raycastTarget = false;
    }

    private static void ConfigureHeader(Transform root, Sprite signSprite)
    {
        Transform header = root.Find("Button (1)");
        if (!(header is RectTransform headerRect))
            throw new InvalidOperationException("Upgrade title sign was not found.");

        Undo.RecordObject(headerRect, "Layout Upgrade Workshop Header");
        SetStretch(
            headerRect,
            new Vector2(0.17f, 0.875f),
            new Vector2(0.83f, 0.975f),
            Vector2.zero,
            Vector2.zero);
        Image headerImage = header.GetComponent<Image>();
        if (headerImage != null)
        {
            Undo.RecordObject(headerImage, "Style Upgrade Workshop Header");
            headerImage.sprite = signSprite;
            headerImage.color = new Color(0.88f, 0.69f, 0.43f, 1f);
            headerImage.raycastTarget = false;
        }

        TextMeshProUGUI title = header.GetComponentInChildren<TextMeshProUGUI>(true);
        if (title == null)
            throw new InvalidOperationException("Upgrade title text was not found.");
        StyleText(title, 46f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        title.text = "그지 신발 개조소";
        RectTransform titleRect = title.rectTransform;
        SetStretch(titleRect, new Vector2(0.05f, 0.37f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI subtitle = GetOrCreateText(
            header,
            SubtitleName,
            title,
            "불운한 신발도, 돈만 내면 괜찮아져!");
        StyleText(subtitle, 18f, new Color(0.25f, 0.105f, 0.035f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);
        SetStretch(
            subtitle.rectTransform,
            new Vector2(0.08f, 0.08f),
            new Vector2(0.92f, 0.40f),
            Vector2.zero,
            Vector2.zero);
    }

    private static void ConfigureCards(Transform root, Sprite cardSprite)
    {
        Transform container = root.Find("GameObject");
        if (!(container is RectTransform containerRect))
            throw new InvalidOperationException("Upgrade card container was not found.");

        Undo.RecordObject(containerRect, "Layout Upgrade Workshop Cards");
        SetStretch(
            containerRect,
            new Vector2(0.035f, 0.105f),
            new Vector2(0.965f, 0.855f),
            Vector2.zero,
            Vector2.zero);

        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = Undo.AddComponent<GridLayoutGroup>(container.gameObject);
        Undo.RecordObject(grid, "Layout Upgrade Workshop Cards");
        grid.padding = new RectOffset(15, 15, 10, 10);
        grid.cellSize = new Vector2(300f, 390f);
        grid.spacing = new Vector2(18f, 18f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        UpgradeUI[] upgrades = container
            .GetComponentsInChildren<UpgradeUI>(true)
            .OrderBy(upgrade => upgrade.transform.GetSiblingIndex())
            .ToArray();
        if (upgrades.Length != 9)
            throw new InvalidOperationException($"Expected 9 upgrade cards, found {upgrades.Length}.");

        for (int index = 0; index < upgrades.Length; index++)
            ConfigureCard(upgrades[index], index, cardSprite);
    }

    private static void ConfigureCard(UpgradeUI upgrade, int index, Sprite cardSprite)
    {
        Transform card = upgrade.transform;
        Undo.RecordObject(card.gameObject, "Rename Upgrade Card");
        card.name = $"UpgradeCard_{index + 1:00}";
        Image cardImage = card.GetComponent<Image>();
        if (cardImage == null)
            cardImage = Undo.AddComponent<Image>(card.gameObject);
        Undo.RecordObject(cardImage, "Style Upgrade Card");
        cardImage.sprite = cardSprite;
        cardImage.color = new Color(0.94f, 0.86f, 0.70f, 1f);
        cardImage.type = Image.Type.Sliced;

        Button button = card.GetComponent<Button>();
        if (button == null)
            button = Undo.AddComponent<Button>(card.gameObject);
        Undo.RecordObject(button, "Style Upgrade Card");
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.91f, 0.68f, 1f);
        colors.pressedColor = new Color(0.77f, 0.58f, 0.32f, 1f);
        colors.disabledColor = new Color(0.36f, 0.32f, 0.27f, 0.82f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI name = RequireText(card, "Name");
        TextMeshProUGUI level = RequireText(card, "Lv");
        TextMeshProUGUI current = RequireText(card, "CurrentValue", "Value");
        TextMeshProUGUI price = RequireText(card, "Price", "Coin_Value");
        Image icon = RequireImage(card, "Icon");
        Image currency = RequireImage(card, "CoinIcon");
        Transform dim = card.Find("Dimb");

        Undo.RecordObject(level.gameObject, "Hide Legacy Upgrade Level");
        level.gameObject.SetActive(false);
        Undo.RecordObject(name.gameObject, "Rename Upgrade Card Text");
        name.gameObject.SetActive(true);
        name.gameObject.name = "Name";
        StyleText(name, 34f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        name.enableAutoSizing = true;
        name.fontSizeMin = 22f;
        name.fontSizeMax = 34f;
        SetStretch(name.rectTransform, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero);

        Undo.RecordObject(icon, "Style Upgrade Card Icon");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetStretch(icon.rectTransform, new Vector2(0.20f, 0.43f), new Vector2(0.80f, 0.80f), Vector2.zero, Vector2.zero);

        current.gameObject.name = "CurrentValue";
        StyleText(current, 30f, Ink, TextAlignmentOptions.Right, FontStyles.Bold);
        SetStretch(current.rectTransform, new Vector2(0.08f, 0.29f), new Vector2(0.40f, 0.42f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI arrow = GetOrCreateText(card, "ValueArrow", current, ">");
        StyleText(arrow, 30f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        SetStretch(arrow.rectTransform, new Vector2(0.42f, 0.29f), new Vector2(0.58f, 0.42f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI next = GetOrCreateText(card, "NextValue", current, "0");
        StyleText(next, 30f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        SetStretch(next.rectTransform, new Vector2(0.60f, 0.29f), new Vector2(0.92f, 0.42f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI description = GetOrCreateText(card, "Description", current, "업그레이드 효과");
        StyleText(description, 21f, new Color(0.20f, 0.09f, 0.025f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        description.enableAutoSizing = true;
        description.fontSizeMin = 16f;
        description.fontSizeMax = 21f;
        SetStretch(description.rectTransform, new Vector2(0.07f, 0.17f), new Vector2(0.93f, 0.29f), Vector2.zero, Vector2.zero);

        RectTransform priceBar = GetOrCreateRect(card, PriceBarName);
        SetStretch(priceBar, new Vector2(0.08f, 0.025f), new Vector2(0.92f, 0.15f), Vector2.zero, Vector2.zero);
        Undo.RecordObject(priceBar, "Order Upgrade Price Bar");
        priceBar.SetAsFirstSibling();
        Image priceBarImage = priceBar.GetComponent<Image>();
        if (priceBarImage == null)
            priceBarImage = Undo.AddComponent<Image>(priceBar.gameObject);
        Undo.RecordObject(priceBarImage, "Style Upgrade Price Bar");
        priceBarImage.sprite = null;
        priceBarImage.color = PriceBarColor;
        priceBarImage.raycastTarget = false;

        Undo.RecordObject(currency.transform, "Order Upgrade Currency Icon");
        Undo.RecordObject(currency, "Style Upgrade Currency Icon");
        currency.transform.SetAsLastSibling();
        currency.preserveAspect = true;
        currency.raycastTarget = false;
        SetStretch(currency.rectTransform, new Vector2(0.18f, 0.045f), new Vector2(0.34f, 0.13f), Vector2.zero, Vector2.zero);
        Sprite coinSprite = FindSpriteByName("src_coin");
        Sprite jewelSprite = FindSpriteByName("src_diamond");

        Undo.RecordObject(price.transform, "Order Upgrade Price Text");
        price.transform.SetAsLastSibling();
        price.gameObject.name = "Price";
        StyleText(price, 31f, PriceGold, TextAlignmentOptions.Center, FontStyles.Bold);
        SetStretch(price.rectTransform, new Vector2(0.35f, 0.035f), new Vector2(0.86f, 0.145f), Vector2.zero, Vector2.zero);

        if (dim != null)
        {
            Undo.RecordObject(dim, "Order Upgrade Dim");
            dim.SetAsLastSibling();
            Image dimImage = dim.GetComponent<Image>();
            if (dimImage != null)
            {
                Undo.RecordObject(dimImage, "Style Upgrade Dim");
                dimImage.color = new Color(0.04f, 0.025f, 0.02f, 0.80f);
            }
        }

        Undo.RecordObject(upgrade, "Bind Upgrade Workshop Card");
        var serialized = new SerializedObject(upgrade);
        serialized.FindProperty("layoutMode").enumValueIndex = 2;
        serialized.FindProperty("nameText").objectReferenceValue = name;
        serialized.FindProperty("levelText").objectReferenceValue = level;
        serialized.FindProperty("valueText").objectReferenceValue = current;
        serialized.FindProperty("currentValueText").objectReferenceValue = current;
        serialized.FindProperty("nextValueText").objectReferenceValue = next;
        serialized.FindProperty("descriptionText").objectReferenceValue = description;
        serialized.FindProperty("priceText").objectReferenceValue = price;
        serialized.FindProperty("iconImage").objectReferenceValue = icon;
        serialized.FindProperty("priceCurrencyImage").objectReferenceValue = currency;
        serialized.FindProperty("coinPriceSprite").objectReferenceValue = coinSprite;
        serialized.FindProperty("jewelPriceSprite").objectReferenceValue = jewelSprite;
        serialized.FindProperty("dimb").objectReferenceValue = dim != null ? dim.gameObject : null;
        serialized.FindProperty("buyButton").objectReferenceValue = button;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(upgrade);
    }

    private static void ConfigureFooter(Transform root, Sprite signSprite)
    {
        RectTransform footer = GetOrCreateRect(root, FooterName);
        SetStretch(footer, new Vector2(0.17f, 0.02f), new Vector2(0.88f, 0.085f), Vector2.zero, Vector2.zero);
        Image image = footer.GetComponent<Image>();
        if (image == null)
            image = Undo.AddComponent<Image>(footer.gameObject);
        Undo.RecordObject(image, "Style Upgrade Workshop Footer");
        image.sprite = signSprite;
        image.color = new Color(0.58f, 0.39f, 0.22f, 0.96f);
        image.raycastTarget = false;

        TextMeshProUGUI template = root.GetComponentInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI text = GetOrCreateText(
            footer,
            "FooterText",
            template,
            "신발은 장비다!  ·  업그레이드는 영구 적용");
        StyleText(text, 22f, new Color(0.95f, 0.79f, 0.46f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetStretch(text.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
    }

    private static void EnsureSpriteImport(string assetPath, int maxTextureSize)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Missing TextureImporter: {assetPath}");

        bool changed =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.mipmapEnabled ||
            importer.maxTextureSize != maxTextureSize;
        if (!changed)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = maxTextureSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static RectTransform GetOrCreateRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing is RectTransform existingRect)
            return existingRect;

        var gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, "Build Upgrade Workshop UI");
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static TextMeshProUGUI GetOrCreateText(
        Transform parent,
        string name,
        TextMeshProUGUI template,
        string value)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text = existing != null
            ? existing.GetComponent<TextMeshProUGUI>()
            : null;
        if (text == null)
        {
            RectTransform rect = GetOrCreateRect(parent, name);
            text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
        }

        Undo.RecordObject(text, "Update Upgrade Workshop Text");
        if (template != null)
        {
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
        }
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static TextMeshProUGUI RequireText(
        Transform parent,
        params string[] candidateNames)
    {
        foreach (string candidateName in candidateNames)
        {
            Transform child = parent.Find(candidateName);
            TextMeshProUGUI text =
                child != null ? child.GetComponent<TextMeshProUGUI>() : null;
            if (text != null)
                return text;
        }

        throw new InvalidOperationException(
            $"Missing TMP child '{string.Join("' or '", candidateNames)}' " +
            $"under {parent.name}.");
    }

    private static Image RequireImage(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        Image image = child != null ? child.GetComponent<Image>() : null;
        if (image == null)
            throw new InvalidOperationException($"Missing Image child '{name}' under {parent.name}.");
        return image;
    }

    private static void StyleText(
        TextMeshProUGUI text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        FontStyles style)
    {
        Undo.RecordObject(text, "Style Upgrade Workshop Text");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }

    private static void SetStretch(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        Undo.RecordObject(rect, "Layout Upgrade Workshop UI");
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static Sprite FindSpriteByName(string spriteName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{spriteName} t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && string.Equals(sprite.name, spriteName, StringComparison.Ordinal))
                return sprite;
        }
        return null;
    }
}
#endif
