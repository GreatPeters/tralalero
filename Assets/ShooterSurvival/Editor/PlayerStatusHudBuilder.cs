#if UNITY_EDITOR
using System;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayerStatusHudBuilder
{
    public const string HudRootName = "PlayerStatusHUD";
    public const string MenuPath = "Tools/Shooter Survival/UI/Apply Player Status HUD";

    private const string PanelSpritePath = "Assets/JH/UI/Upgrade/메뉴이름.png";
    private const string HeartSpritePath =
        "Assets/ShooterSurvival/UI/PlayerStatus/PlayerStatus_Heart.png";
    private const string AttackSpritePath = "Assets/JH/UI/Upgrade/공격력.png";
    private const string FontPath = "Assets/JH/Font/쩡야공유/GmarketSansTTFBold SDF2.asset";

    private static readonly Color32 TextColor = new(43, 28, 20, 255);
    private static readonly Color32 TrackColor = new(64, 47, 31, 255);
    private static readonly Color32 FillColor = new(74, 190, 20, 255);
    private static readonly Color32 RivetColor = new(202, 145, 54, 255);

    [MenuItem(MenuPath, false, 2311)]
    public static void ApplyToOpenNoryangjinScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Player Status HUD] Exit Play Mode before applying the HUD.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() ||
            !string.Equals(
                scene.path,
                NoryangjinForwardGameplayInstaller.TargetScenePath,
                StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError(
                $"[Player Status HUD] Open {NoryangjinForwardGameplayInstaller.TargetScenePath} first.");
            return;
        }

        CanvasScript canvas = FindInScene<CanvasScript>(scene);
        PlayerScript player = FindInScene<PlayerScript>(scene);
        if (canvas == null || player == null)
        {
            Debug.LogError("[Player Status HUD] CanvasScript or PlayerScript is missing.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Player Status HUD");

        Build(canvas, player);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Failed to save the Player Status HUD scene changes.");

        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("[Player Status HUD] Applied the slim parchment status HUD.");
    }

    public static PlayerStatusHud Build(CanvasScript canvas, PlayerScript player)
    {
        if (canvas == null)
            throw new ArgumentNullException(nameof(canvas));

        ConfigureHeartSpriteImporter();
        Sprite panelSprite = LoadRequiredAsset<Sprite>(PanelSpritePath);
        Sprite heartSprite = LoadRequiredAsset<Sprite>(HeartSpritePath);
        Sprite attackSprite = LoadRequiredAsset<Sprite>(AttackSpritePath);
        TMP_FontAsset font = LoadRequiredAsset<TMP_FontAsset>(FontPath);

        Transform existing = canvas.transform.Find(HudRootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject rootObject = CreateUiObject(HudRootName, canvas.transform);
        RectTransform root = rootObject.GetComponent<RectTransform>();
        SetTopLeftRect(root, Vector2.zero, new Vector2(520f, 224f));

        PlayerStatusHud hud = Undo.AddComponent<PlayerStatusHud>(rootObject);

        RectTransform healthCard = CreatePanel(
            "HealthCard",
            root,
            panelSprite,
            Vector2.zero,
            new Vector2(500f, 122f));
        CreateRivets(healthCard);

        CreateImage(
            "HeartIcon",
            healthCard,
            heartSprite,
            new Vector2(22f, -16f),
            new Vector2(84f, 84f),
            true);
        CreateText(
            "HealthLabel",
            healthCard,
            font,
            "체력",
            new Vector2(122f, -14f),
            new Vector2(92f, 44f),
            34f);
        TextMeshProUGUI healthValue = CreateText(
            "HealthValue",
            healthCard,
            font,
            "100 / 100",
            new Vector2(218f, -14f),
            new Vector2(252f, 44f),
            34f);
        Image healthFill = CreateHealthBar(
            healthCard,
            new Vector2(122f, -68f),
            new Vector2(340f, 28f));

        RectTransform attackCard = CreatePanel(
            "AttackCard",
            root,
            panelSprite,
            new Vector2(40f, -132f),
            new Vector2(390f, 88f));
        CreateRivets(attackCard);
        CreateImage(
            "AttackIcon",
            attackCard,
            attackSprite,
            new Vector2(14f, -10f),
            new Vector2(68f, 68f),
            true);
        CreateText(
            "AttackLabel",
            attackCard,
            font,
            "공격력",
            new Vector2(96f, -18f),
            new Vector2(142f, 48f),
            32f);
        TextMeshProUGUI attackValue = CreateText(
            "AttackValue",
            attackCard,
            font,
            "50",
            new Vector2(248f, -18f),
            new Vector2(105f, 48f),
            34f);

        hud.Configure(healthValue, healthFill, attackValue);
        hud.SetHealth(100f, 100f);
        hud.SetAttack(50f);

        Undo.RecordObject(canvas, "Configure Player Status HUD");
        if (player != null)
            Undo.RecordObject(player, "Configure Player Status HUD Canvas");
        canvas.ConfigurePlayerStatusHud(hud, player);
        EditorUtility.SetDirty(canvas);

        if (player != null)
        {
            EditorUtility.SetDirty(player);
        }
        EditorUtility.SetDirty(hud);
        return hud;
    }

    private static RectTransform CreatePanel(
        string name,
        RectTransform parent,
        Sprite sprite,
        Vector2 position,
        Vector2 size)
    {
        Image image = CreateImage(name, parent, sprite, position, size, false);
        image.raycastTarget = false;
        return image.rectTransform;
    }

    private static Image CreateHealthBar(
        RectTransform parent,
        Vector2 position,
        Vector2 size)
    {
        Sprite backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/Background.psd");
        Sprite fillSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd");

        Image track = CreateImage(
            "HealthBarTrack",
            parent,
            backgroundSprite,
            position,
            size,
            false);
        track.type = Image.Type.Sliced;
        track.color = TrackColor;

        Image fill = CreateImage(
            "HealthFill",
            track.rectTransform,
            fillSprite,
            new Vector2(4f, -4f),
            size - new Vector2(8f, 8f),
            false);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        fill.color = FillColor;
        return fill;
    }

    private static void CreateRivets(RectTransform panel)
    {
        Sprite rivetSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Vector2 panelSize = panel.sizeDelta;
        Vector2[] positions =
        {
            new(10f, -10f),
            new(panelSize.x - 26f, -10f),
            new(10f, -(panelSize.y - 26f)),
            new(panelSize.x - 26f, -(panelSize.y - 26f))
        };

        for (int i = 0; i < positions.Length; i++)
        {
            Image rivet = CreateImage(
                $"Rivet_{i + 1}",
                panel,
                rivetSprite,
                positions[i],
                new Vector2(16f, 16f),
                true);
            rivet.color = RivetColor;
            Shadow shadow = Undo.AddComponent<Shadow>(rivet.gameObject);
            shadow.effectColor = new Color32(70, 43, 22, 190);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = true;
        }
    }

    private static Image CreateImage(
        string name,
        RectTransform parent,
        Sprite sprite,
        Vector2 position,
        Vector2 size,
        bool preserveAspect)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        Image image = Undo.AddComponent<Image>(gameObject);
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        SetTopLeftRect(image.rectTransform, position, size);
        return image;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        RectTransform parent,
        TMP_FontAsset font,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = Undo.AddComponent<TextMeshProUGUI>(gameObject);
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Normal;
        text.color = TextColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = value;
        SetTopLeftRect(text.rectTransform, position, size);
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void SetTopLeftRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"Required HUD asset is missing: {path}");

        return asset;
    }

    internal static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static void ConfigureHeartSpriteImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(HeartSpritePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Required HUD texture is missing: {HeartSpritePath}");

        bool needsReimport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.mipmapEnabled ||
            !importer.alphaIsTransparency ||
            importer.maxTextureSize != 512;
        if (!needsReimport)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 512;
        importer.SaveAndReimport();
    }
}
#endif
