#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

public static partial class UpgradeShopReferenceSetup
{
    internal const string ReferenceArtPath = "Assets/JH/UI/Upgrade/Workshop_Reference_20260910.png";
    private static void ApplyReferenceArtwork(Transform root)
    {
        var importer = AssetImporter.GetAtPath(ReferenceArtPath) as TextureImporter;
        if (importer == null) return;
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        var metadata = new System.Collections.Generic.List<SpriteMetaData>();
        metadata.Add(new SpriteMetaData { name = "WorkshopHero", rect = new Rect(0, height * .692f, width, height * .308f), pivot = Vector2.one * .5f });
        float[] xs = { .061f, .356f, .651f };
        float[] ys = { .478f, .265f, .065f };
        float[] hs = { .211f, .208f, .195f };
        for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
            metadata.Add(new SpriteMetaData { name = "WorkshopCard" + (row * 3 + col + 1), rect = new Rect(width * xs[col], height * ys[row], width * .278f, height * hs[row]), pivot = Vector2.one * .5f, border = Vector4.one * 20 });
        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.maxTextureSize = 2048; importer.mipmapEnabled = false;
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var rects = metadata.Select(m => new SpriteRect { name = m.name, rect = m.rect, pivot = m.pivot, border = m.border, spriteID = GUID.Generate() }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }
        var sprites = AssetDatabase.LoadAllAssetsAtPath(ReferenceArtPath).OfType<Sprite>().ToDictionary(s => s.name);
        var hero = GetOrCreateRect(root, "WorkshopMerchant");
        hero.anchorMin = new Vector2(0, 1); hero.anchorMax = Vector2.one; hero.pivot = new Vector2(.5f, 1);
        hero.anchoredPosition = new Vector2(0, -92); hero.sizeDelta = Vector2.zero; hero.SetSiblingIndex(1);
        var heroImage = hero.GetComponent<Image>() ?? Undo.AddComponent<Image>(hero.gameObject);
        heroImage.sprite = sprites["WorkshopHero"]; heroImage.raycastTarget = false;
        var aspect = hero.GetComponent<AspectRatioFitter>() ?? Undo.AddComponent<AspectRatioFitter>(hero.gameObject);
        aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; aspect.aspectRatio = width / (height * .308f);
        var header = (RectTransform)root.Find("Button (1)");
        SetStretch(header, new Vector2(.038f, .842f), new Vector2(.319f, .902f), Vector2.zero, Vector2.zero);
        header.GetComponent<Image>().enabled = false;
        var title = header.GetComponentInChildren<TextMeshProUGUI>(true); title.fontSize = 36; title.enableAutoSizing = true; title.fontSizeMin = 24; title.fontSizeMax = 36;
        var koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/JH/Font/쩡야공유/GmarketSansTTFBold SDF2.asset");
        foreach (var label in header.GetComponentsInChildren<TextMeshProUGUI>(true)) label.font = koreanFont;
        title.text = "신발 개조소";
        var subtitle = header.Find(SubtitleName).GetComponent<TextMeshProUGUI>(); subtitle.text = "한 걸음 더 강하게";
        var container = (RectTransform)root.Find("GameObject");
        SetStretch(container, new Vector2(.035f, .095f), new Vector2(.965f, .675f), Vector2.zero, Vector2.zero);
        var responsive = container.GetComponent<ResponsiveCardGrid>() ?? Undo.AddComponent<ResponsiveCardGrid>(container.gameObject);
        responsive.rows = 3; responsive.cardAspect = 300f / 390f;
        var cards = container.GetComponentsInChildren<UpgradeUI>(true).OrderBy(c => c.transform.GetSiblingIndex()).ToArray();
        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i].transform; card.GetComponent<Image>().sprite = sprites["WorkshopCard" + (i + 1)];
            card.GetComponent<Image>().color = Color.white;
            card.Find("Icon").gameObject.SetActive(false);
            card.Find(PriceBarName).GetComponent<Image>().enabled = false;
        }
        var footer = root.Find(FooterName); if (footer != null) footer.gameObject.SetActive(false);
    }
}
#endif
