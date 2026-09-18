using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MobileUIArt
{
    public const string FontPath = "Assets/ShooterSurvival/Resources/UI/KERISKEDU_B SDF.asset";
    public const string Root = "Assets/ShooterSurvival/UI/Mobile";
    public struct Palette
    {
        public Color page, card, ink, muted, primary, accent, line;
        public string name;
    }
    public static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var color); return color; }
    public static Palette GetPalette(int index)
    {
        if (index == 1) return new Palette { name = "Boardwalk Ticket", page = Hex("#F1DCB5"), card = Hex("#FFF2D6"),
            ink = Hex("#48362E"), muted = Hex("#82705B"), primary = Hex("#F18A70"), accent = Hex("#438D87"), line = Hex("#725A43") };
        if (index == 2) return new Palette { name = "Night Arcade", page = Hex("#142339"), card = Hex("#263C57"),
            ink = Hex("#F3F8FC"), muted = Hex("#A7BED0"), primary = Hex("#62DDD0"), accent = Hex("#B99AEC"), line = Hex("#557190") };
        return new Palette { name = "Ocean Pop", page = Hex("#E9F5F0"), card = Hex("#FFFAEF"),
            ink = Hex("#173E4D"), muted = Hex("#66818A"), primary = Hex("#FFD268"), accent = Hex("#31B9B4"), line = Hex("#285567") };
    }

    public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
    public static Sprite Rounded => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Rounded.png");
    public static Sprite Coin => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/Image/Lobby/Upgrade/img_nav_coin.png");
    public static Sprite Jewel => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/Image/Lobby/Upgrade/img_nav_diamond.png");

    public static void EnsureAssets()
    {
        Directory.CreateDirectory(Root);
        if (Font == null && !AssetDatabase.CopyAsset("Assets/JH/Font/KERISKEDU_B SDF.asset", FontPath))
            throw new InvalidOperationException("Could not preserve/copy the requested KERIS font.");
        if (Rounded == null)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - 64f) - 36f, 0f);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - 64f) - 36f, 0f);
                float alpha = Mathf.Clamp01(28f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels); texture.Apply();
            string path = Root + "/Rounded.png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(30, 30, 30, 30);
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    public static RectTransform Rect(Transform parent, string name, float x, float y, float xx, float yy)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); SetRect(rect, x, y, xx, yy); return rect;
    }
    public static void SetRect(RectTransform rect, float x, float y, float xx, float yy)
    {
        rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(xx, yy);
        rect.pivot = Vector2.one * .5f; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    public static Image Image(Transform parent, string name, float x, float y, float xx, float yy, Color color, bool rounded = false)
    {
        var image = Rect(parent, name, x, y, xx, yy).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false;
        if (rounded) { image.sprite = Rounded; image.type = UnityEngine.UI.Image.Type.Sliced; }
        return image;
    }
    public static Image Panel(Transform parent, string name, float x, float y, float xx, float yy, Palette palette, float inset = 5)
    {
        var border = Image(parent, name, x, y, xx, yy, palette.line, true);
        var fill = Image(border.transform, "Fill", 0, 0, 1, 1, palette.card, true);
        fill.rectTransform.offsetMin = Vector2.one * inset; fill.rectTransform.offsetMax = Vector2.one * -inset;
        return fill;
    }
    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
        float x, float y, float xx, float yy, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var label = Rect(parent, name, x, y, xx, yy).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font; label.fontSharedMaterial = Font.material; label.text = text;
        label.fontStyle = FontStyles.Normal; label.fontSize = size; label.color = color;
        label.alignment = alignment; label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }
    public static Button Button(Transform parent, string name, string text, float size,
        float x, float y, float xx, float yy, Palette palette, bool primary = false)
    {
        var fill = Panel(parent, name, x, y, xx, yy, palette);
        fill.color = primary ? palette.primary : palette.card;
        var button = fill.transform.parent.gameObject.AddComponent<Button>();
        button.targetGraphic = fill; fill.raycastTarget = true;
        var colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(.96f, .99f, 1f);
        colors.pressedColor = new Color(.82f, .90f, .91f); colors.selectedColor = Color.white;
        colors.disabledColor = new Color(.68f, .73f, .73f, .85f); colors.fadeDuration = .08f;
        button.colors = colors;
        Text(fill.transform, "Label", text, size, primary ? Hex("#173E4D") : palette.ink,
            .05f, .08f, .95f, .92f, TextAlignmentOptions.Center);
        return button;
    }

    public static object RenderCandidates()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        EnsureAssets();
        string folder = "map-concepts/mobile-presentation-2026-09-14/ui-candidates/v2";
        Directory.CreateDirectory(folder);
        for (int style = 0; style < 3; style++)
        {
            RenderCandidate(style, false, folder + "/style-" + style + "-upgrades.png");
            RenderCandidate(style, true, folder + "/style-" + style + "-cosmetics.png");
        }
        return new { folder, candidates = 3, images = 6, font = Font.name };
    }

    public static object RenderOne(int style, bool cosmetics, string label)
    {
        EnsureAssets();
        if (Path.GetFileName(label) != label) throw new ArgumentException("A simple evidence label is required.");
        string path = "map-concepts/mobile-presentation-2026-09-14/ui-candidates/" + label + ".png";
        RenderCandidate(style, cosmetics, path);
        return new { path };
    }

    private static void RenderCandidate(int style, bool cosmetics, string path)
    {
        if (File.Exists(path)) throw new InvalidOperationException("Preserve previous candidate: " + path);
        const int width = 720, height = 1560;
        const float scale = width / 1080f;
        Palette palette = GetPalette(style);
        var preview = new PreviewRenderUtility();
        Texture2D rendered = null;
        try
        {
            var root = new GameObject("Mobile UI candidate", typeof(RectTransform), typeof(Canvas));
            preview.AddSingleGO(root);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = preview.camera;
            ((RectTransform)root.transform).sizeDelta = new Vector2(width, height);
            Image(root.transform, "Page", 0, 0, 1, 1, palette.page);
            if (style == 1)
                Image(root.transform, "TicketStripe", .035f, .89f, .965f, .905f, palette.primary, true);
            Button(root.transform, "Back", "<", 48 * scale, .04f, .93f, .17f, .988f, palette);
            Text(root.transform, "Heading", cosmetics ? "나만의 상어" : "강화 상점", 66 * scale, palette.ink, .21f, .925f, .96f, .985f);
            var money = Panel(root.transform, "Wallet", .57f, .876f, .96f, .916f, palette, 3);
            var coin = Image(money.transform, "Coin", .04f, .08f, .20f, .92f, Color.white); coin.sprite = Coin; coin.preserveAspect = true;
            Text(money.transform, "Value", "3,007", 39 * scale, palette.ink, .23f, .05f, .95f, .95f, TextAlignmentOptions.Right);
            if (cosmetics) BuildCosmeticCandidate(root.transform, palette, scale);
            else BuildUpgradeCandidate(root.transform, palette, scale);
            preview.camera.orthographic = true; preview.camera.orthographicSize = height * .5f;
            preview.camera.cameraType = CameraType.Game;
            preview.camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
            preview.camera.nearClipPlane = .1f; preview.camera.farClipPlane = 30;
            preview.camera.clearFlags = CameraClearFlags.SolidColor; preview.camera.backgroundColor = palette.page;
            preview.BeginStaticPreview(new Rect(0, 0, width, height));
            preview.camera.aspect = (float)width / height;
            File.WriteAllText(path + ".diag.txt", "cameraType=" + preview.camera.cameraType +
                "; mask=" + preview.camera.cullingMask + "; layer=" + root.layer +
                "; rootPosition=" + root.transform.position + "; scale=" + root.transform.lossyScale +
                "; rect=" + ((RectTransform)root.transform).rect + "; scene=" + root.scene.name);
            Canvas.ForceUpdateCanvases(); preview.Render(true, false);
            rendered = preview.EndStaticPreview(); File.WriteAllBytes(path, rendered.EncodeToPNG());
        }
        finally { if (rendered != null) Object.DestroyImmediate(rendered); preview.Cleanup(); }
    }
    private static void BuildUpgradeCandidate(Transform root, Palette palette, float scale)
    {
        Text(root, "Hint", "지금보다 한 걸음 더", 32 * scale, palette.muted, .055f, .873f, .55f, .912f);
        var stats = Panel(root, "CurrentStats", .04f, .77f, .96f, .862f, palette);
        Text(stats.transform, "AttackLabel", "현재 공격력", 31 * scale, palette.muted, .045f, .57f, .49f, .89f);
        Text(stats.transform, "Attack", "8", 62 * scale, palette.ink, .045f, .08f, .48f, .60f);
        Text(stats.transform, "HealthLabel", "최대 체력", 31 * scale, palette.muted, .54f, .57f, .96f, .89f);
        Text(stats.transform, "Health", "60", 62 * scale, palette.ink, .54f, .08f, .96f, .60f);
        var names = new[] { "공격력", "체력", "공격 속도", "미사일 지속 시간", "코인 보너스", "좌우 이동 속도" };
        for (int i = 0; i < names.Length; i++)
        {
            float x = .04f + (i % 2) * .47f, top = .738f - (i / 2) * .222f;
            var card = Panel(root, "Card" + i, x, top - .205f, x + .45f, top, palette);
            Text(card.transform, "Name", names[i], 38 * scale, palette.ink, .07f, .72f, .94f, .93f);
            Text(card.transform, "Level", i == 5 ? "레벨 0 / 10" : "레벨 0", 27 * scale, palette.muted, .07f, .57f, .93f, .73f);
            Text(card.transform, "Value", i == 5 ? "+0%  >  +5%" : i == 0 ? "8  >  12" : i == 1 ? "60  >  140" : "+0%  >  +5%",
                42 * scale, palette.ink, .07f, .32f, .94f, .57f, TextAlignmentOptions.Center);
            Button(card.transform, "Buy", i == 5 ? "70 코인" : "강화하기", 34 * scale, .04f, .04f, .96f, .29f, palette, true);
        }
        Text(root, "Footer", "강화는 다음 도전에도 이어집니다", 31 * scale, palette.muted, .05f, .032f, .95f, .07f, TextAlignmentOptions.Center);
    }
    private static void BuildCosmeticCandidate(Transform root, Palette palette, float scale)
    {
        Text(root, "Hint", "모습도 능력도 새롭게", 32 * scale, palette.muted, .055f, .873f, .56f, .912f);
        var stage = Panel(root, "HeroStage", .04f, .48f, .96f, .861f, palette);
        Text(stage.transform, "Name", "트랄랄레오", 46 * scale, palette.ink, .05f, .84f, .95f, .96f, TextAlignmentOptions.Center);
        var hero = Image(stage.transform, "Hero", .14f, .11f, .86f, .84f, Color.white);
        hero.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ShooterSurvival/Resources/Cosmetics/Icons/skin_original.png");
        hero.preserveAspect = true;
        Text(stage.transform, "Rotate", "드래그해서 둘러보기", 28 * scale, palette.muted, .1f, .02f, .9f, .10f, TextAlignmentOptions.Center);
        var tabs = new[] { "피부", "신발", "모자" };
        for (int i = 0; i < 3; i++) Button(root, "Tab" + i, tabs[i], 38 * scale, .04f + i * .313f, .408f, .335f + i * .313f, .468f, palette, i == 0);
        for (int i = 0; i < 4; i++)
        {
            float x = .04f + i % 2 * .47f, top = .388f - i / 2 * .126f;
            var card = Panel(root, "Equipment" + i, x, top - .114f, x + .45f, top, palette);
            var icon = Image(card.transform, "Icon", .035f, .13f, .35f, .92f, Color.white);
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ShooterSurvival/Resources/Cosmetics/Icons/" + new[] { "skin_original", "skin_coral", "skin_ice", "skin_sand" }[i] + ".png");
            icon.preserveAspect = true;
            Text(card.transform, "Name", new[] { "오리지널", "코랄", "아이스", "샌드" }[i], 34 * scale, palette.ink, .39f, .51f, .96f, .9f);
            Text(card.transform, "State", i == 0 ? "착용 중" : "보석 10", 28 * scale, palette.muted, .39f, .11f, .96f, .46f);
        }
        Button(root, "Action", "착용하기", 45 * scale, .05f, .033f, .95f, .108f, palette, true);
    }
}
