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

    private const string HeartSpritePath =
        "Assets/ShooterSurvival/UI/PlayerStatus/PlayerStatus_Heart.png";
    private const string AttackSpritePath = "Assets/JH/UI/Upgrade/공격력.png";
    private const string FontPath = "Assets/JH/Font/쩡야공유/GmarketSansTTFBold SDF2.asset";

    private static readonly Color32 PanelColor = new(27, 34, 41, 220);
    private static readonly Color32 PanelShadowColor = new(5, 12, 18, 145);
    private static readonly Color32 TextColor = new(241, 246, 250, 255);
    private static readonly Color32 TrackColor = new(52, 61, 69, 255);
    private static readonly Color32 FillColor = new(255, 91, 85, 255);

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
        Debug.Log("[Player Status HUD] Applied the modern dark status HUD.");
    }

    public static PlayerStatusHud Build(CanvasScript canvas, PlayerScript player)
    {
        if (canvas == null)
            throw new ArgumentNullException(nameof(canvas));

        ConfigureHeartSpriteImporter();
        Sprite heartSprite = LoadRequiredAsset<Sprite>(HeartSpritePath);
        Sprite attackSprite = LoadRequiredAsset<Sprite>(AttackSpritePath);
        TMP_FontAsset font = LoadRequiredAsset<TMP_FontAsset>(FontPath);

        Transform existing = canvas.transform.Find(HudRootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject rootObject = CreateUiObject(HudRootName, canvas.transform);
        RectTransform root = rootObject.GetComponent<RectTransform>();
        SetTopLeftRect(root, Vector2.zero, new Vector2(488f, 216f));

        PlayerStatusHud hud = Undo.AddComponent<PlayerStatusHud>(rootObject);

        RectTransform healthCard = CreatePanel(
            "HealthCard",
            root,
            new Vector2(18f, -18f),
            new Vector2(452f, 112f));

        CreateImage(
            "HeartIcon",
            healthCard,
            heartSprite,
            new Vector2(20f, -20f),
            new Vector2(64f, 64f),
            true);
        CreateText(
            "HealthLabel",
            healthCard,
            font,
            "체력",
            new Vector2(104f, -14f),
            new Vector2(92f, 40f),
            30f);
        TextMeshProUGUI healthValue = CreateText(
            "HealthValue",
            healthCard,
            font,
            "100 / 100",
            new Vector2(206f, -14f),
            new Vector2(220f, 40f),
            32f);
        Image healthFill = CreateHealthBar(
            healthCard,
            new Vector2(104f, -68f),
            new Vector2(314f, 24f));

        RectTransform attackCard = CreatePanel(
            "AttackCard",
            root,
            new Vector2(46f, -140f),
            new Vector2(332f, 70f));
        CreateImage(
            "AttackIcon",
            attackCard,
            attackSprite,
            new Vector2(16f, -11f),
            new Vector2(48f, 48f),
            true);
        CreateText(
            "AttackLabel",
            attackCard,
            font,
            "공격력",
            new Vector2(82f, -10f),
            new Vector2(118f, 50f),
            28f);
        TextMeshProUGUI attackValue = CreateText(
            "AttackValue",
            attackCard,
            font,
            "50",
            new Vector2(224f, -10f),
            new Vector2(78f, 50f),
            30f);

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
        Vector2 position,
        Vector2 size)
    {
        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd");
        Image image = CreateImage(name, parent, panelSprite, position, size, false);
        image.type = Image.Type.Sliced;
        image.color = PanelColor;
        image.raycastTarget = false;

        Shadow shadow = Undo.AddComponent<Shadow>(image.gameObject);
        shadow.effectColor = PanelShadowColor;
        shadow.effectDistance = new Vector2(0f, -4f);
        shadow.useGraphicAlpha = true;
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
