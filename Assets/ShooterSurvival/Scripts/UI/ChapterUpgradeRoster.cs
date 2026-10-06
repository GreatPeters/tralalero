using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Extends the scene-authored scroll list while keeping its existing card styling and bindings.
public static class ChapterUpgradeRoster
{
    public static int Ensure(Transform root) => Ensure(root, ChapterUpgradeService.Catalog);

    public static int Ensure(Transform root, ChapterUpgradeCatalog catalog)
    {
        if (root == null || catalog == null) return 0;
        var cards = root.GetComponentsInChildren<ChapterUpgradeCardUI>(true);
        if (cards.Length == 0) return 0;
        var template = cards.OrderBy(card => card.chapter).Last();
        var present = new HashSet<int>(cards.Select(card => card.chapter));
        int added = 0;
        foreach (var definition in catalog.entries.OrderBy(row => row.chapter))
        {
            if (!present.Add(definition.chapter)) continue;
            var clone = Object.Instantiate(template.gameObject, template.transform.parent, false);
            clone.name = "Chapter" + definition.chapter;
            var card = clone.GetComponent<ChapterUpgradeCardUI>();
            card.chapter = definition.chapter;
            card.title.text = definition.title;
            if (card.chapterLabel != null) card.chapterLabel.text = $"CHAPTER {definition.chapter:00}";
            ConfigureArtwork(card, definition);
            clone.SetActive(true);
            if (Application.isPlaying) card.Refresh();
            added++;
        }
        if (added > 0)
        {
            var grid = template.transform.parent.GetComponent<EquipmentCardGrid>();
            grid?.RefreshLayout();
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)template.transform.parent);
        }
        return added;
    }

    private static void ConfigureArtwork(ChapterUpgradeCardUI card, ChapterUpgradeDefinition definition)
    {
        var artwork = card.chapterArtwork != null ? card.chapterArtwork : card.transform.Find("ChapterArt")?.GetComponent<Image>();
        if (artwork == null) return;
        card.chapterArtwork = artwork;
        var existingNumber = card.transform.Find("ChapterNumberArtwork");
        if (definition.artwork != null)
        {
            artwork.sprite = definition.artwork;
            artwork.gameObject.SetActive(true);
            if (existingNumber != null) existingNumber.gameObject.SetActive(false);
            return;
        }
        // A chapter numeral is deliberate neutral art until a verified chapter image is assigned.
        artwork.gameObject.SetActive(false);
        TMP_Text number = existingNumber != null ? existingNumber.GetComponent<TMP_Text>() : null;
        if (number == null)
        {
            number = Object.Instantiate(card.chapterLabel != null ? card.chapterLabel : card.title, card.transform, false);
            number.name = "ChapterNumberArtwork";
        }
        var source = artwork.rectTransform;
        var rect = number.rectTransform;
        rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot; rect.sizeDelta = source.sizeDelta; rect.anchoredPosition = source.anchoredPosition;
        number.text = definition.chapter.ToString("00");
        number.alignment = TextAlignmentOptions.Center;
        number.enableAutoSizing = true; number.fontSizeMin = 32; number.fontSizeMax = 112;
        number.raycastTarget = false;
        number.gameObject.SetActive(true);
    }
}
