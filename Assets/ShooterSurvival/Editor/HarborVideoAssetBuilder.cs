using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Creates presentation-only video UI prefabs from the reviewed separated artwork.</summary>
public static class HarborVideoAssetBuilder
{
    private const string Art = "Assets/ShooterSurvival/UI/HarborVideo/";
    private const string Work = "map-concepts/harbor-video-ui-2026-09-15/production/";
    [Serializable] private sealed class SpriteList { public SpriteRow[] sprites = Array.Empty<SpriteRow>(); }
    [Serializable] private sealed class SpriteRow { public string file = ""; public float[] border = new float[4]; }
    [Serializable] private sealed class Layout { public int width = 0, height = 0; public Layer[] layers = Array.Empty<Layer>(); }
    [Serializable] private sealed class Layer
    {
        public string name = "", asset = "", kind = "", text = "", color = "", align = "";
        public float[] rect = new float[4];
        public float fontSize = 0, fill = 0;
    }

    public static object Build(string onlyVariant = "")
    {
        if (onlyVariant != "" && onlyVariant != "A" && onlyVariant != "B")
            throw new ArgumentException("Expected A, B or both (empty).", nameof(onlyVariant));
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build video UI assets outside Play Mode.");
        AssetDatabase.Refresh();
        var sprites = JsonUtility.FromJson<SpriteList>(File.ReadAllText(Work + "sprite-manifest.json"));
        foreach (var row in sprites.sprites)
        {
            string path = Art + row.file;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(row.border[0], row.border[1], row.border[2], row.border[3]);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/KERISKEDU_B SDF.asset");
        if (font == null) throw new InvalidOperationException("The existing KERIS font is required.");
        var reports = new System.Collections.Generic.List<string>();
        foreach (string variant in onlyVariant == "" ? new[] { "A", "B" } : new[] { onlyVariant })
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Work + "layout-" + variant + ".json"));
            var root = new GameObject("HarborVideo_" + variant, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder = 200;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(layout.width, layout.height);
                scaler.matchWidthOrHeight = .5f;
                foreach (var layer in layout.layers)
                {
                    var go = new GameObject(layer.name, typeof(RectTransform));
                    go.layer = LayerMask.NameToLayer("UI");
                    var rect = go.GetComponent<RectTransform>(); rect.SetParent(root.transform, false);
                    Place(rect, layer.rect, layout.width, layout.height);
                    if (layer.kind == "text")
                    {
                        var text = go.AddComponent<TextMeshProUGUI>();
                        text.font = font; text.fontSharedMaterial = font.material;
                        text.text = layer.text; text.fontSize = layer.fontSize;
                        text.fontStyle = FontStyles.Normal; text.raycastTarget = false;
                        ColorUtility.TryParseHtmlString(layer.color, out Color color); text.color = color;
                        text.alignment = layer.align == "left" ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
                        text.textWrappingMode = TextWrappingModes.Normal;
                    }
                    else if (layer.kind == "video")
                    {
                        var backing = go.AddComponent<Image>(); backing.color = new Color(.03f,.07f,.10f); backing.raycastTarget = false;
                        go.AddComponent<RectMask2D>();
                        var display = new GameObject("MovieDisplay", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                        display.transform.SetParent(rect, false);
                        var image = display.GetComponent<RawImage>();
                        image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "MovieFramePreview.png");
                        image.raycastTarget = false;
                        var fit = display.GetComponent<AspectRatioFitter>();
                        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 9f / 16f;
                    }
                    else
                    {
                        var image = go.AddComponent<Image>();
                        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + layer.asset + ".png");
                        image.type = image.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                        image.raycastTarget = layer.kind == "button";
                        if (layer.kind == "progress")
                        {
                            image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal;
                            image.fillOrigin = 0; image.fillAmount = layer.fill;
                        }
                        if (layer.kind == "button")
                        {
                            var button = go.AddComponent<Button>(); button.targetGraphic = image;
                            var colors = button.colors; colors.normalColor = Color.white;
                            colors.highlightedColor = new Color(1f,.95f,.83f); colors.pressedColor = new Color(.76f,.65f,.49f);
                            colors.selectedColor = Color.white; colors.disabledColor = new Color(.55f,.52f,.48f);
                            button.colors = colors;
                        }
                    }
                }
                // Labels belong to their buttons, so hiding or moving a button carries its text.
                foreach (string name in new[] { "Skip", "Replay", "Next" })
                {
                    var label = layout.layers.Single(l => l.name == name + "Label");
                    var button = layout.layers.Single(l => l.name == name + "Button");
                    var child = (RectTransform)root.transform.Find(label.name);
                    child.SetParent(root.transform.Find(button.name), false);
                    Place(child, new[] { label.rect[0]-button.rect[0], label.rect[1]-button.rect[1], label.rect[2], label.rect[3] }, button.rect[2], button.rect[3]);
                }
                Canvas.ForceUpdateCanvases();
                foreach (var text in root.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
                string prefabPath = Art + "HarborVideo_" + variant + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null) throw new IOException("Could not save " + prefabPath);
                reports.Add(variant + ": " + prefab.GetComponentsInChildren<Button>(true).Length + " buttons, " + prefab.GetComponentsInChildren<TMP_Text>(true).Length + " editable TMP labels, portrait FitInParent");
            }
            finally { Object.DestroyImmediate(root); }
        }
        File.WriteAllLines(Work + "unity-prefab-verification.txt", reports);
        return new { sprites = sprites.sprites.Length, prefabs = reports, playbackConnected = false };
    }

    private static void Place(RectTransform rect, float[] box, float width, float height)
    {
        rect.anchorMin = new Vector2(box[0]/width, 1f-(box[1]+box[3])/height);
        rect.anchorMax = new Vector2((box[0]+box[2])/width, 1f-box[1]/height);
        rect.pivot = new Vector2(.5f,.5f); rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
