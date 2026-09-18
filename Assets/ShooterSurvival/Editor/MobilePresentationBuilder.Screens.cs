using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class MobilePresentationBuilder
{
    private static void BuildOtherScreens(CanvasScript canvas)
    {
        StyleStory(canvas.GetComponentInChildren<OpeningStoryUI>(true));
        StyleTransition(canvas.GetComponentInChildren<ChapterTransitionUI>(true));
        StyleResult(canvas.gameOverUI.transform);
        StyleResult(canvas.youWinUI.transform);
        BuildPause(canvas);
        BuildSettings(canvas);
        BuildLobbySettings(canvas);
        StyleLobbyHeader(canvas);
    }

    private static void CopyButtonCalls(Button source, Button destination)
    {
        if (source == null) return;
        var from = new SerializedObject(source);
        var to = new SerializedObject(destination);
        to.CopyFromSerializedProperty(from.FindProperty("m_OnClick"));
        to.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Solid(Image image, Color color, bool rounded = true)
    {
        if (image == null) return;
        image.sprite = rounded ? Round : null; image.color = color;
        image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
        if (rounded) AddOutline(image.gameObject);
    }

    private static void ThemeText(TMP_Text text, float size = 0)
    {
        if (text == null) return;
        text.font = Font; text.fontSharedMaterial = Font.material; text.fontStyle = FontStyles.Normal;
        text.color = palette.ink; text.outlineWidth = 0;
        if (size > 0) { text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = size * .82f; text.fontSizeMax = size; }
    }

    private static void StyleStory(OpeningStoryUI story)
    {
        if (story == null) return;
        var root = story.transform; AddSafeArea(root);
        Solid(root.Find("Background")?.GetComponent<Image>(), palette.page, false);
        if (story.backdrop != null) story.backdrop.gameObject.SetActive(false);
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "Frame" || image.name == "StoryCard") Solid(image, palette.card);
            else if (image.name == "WorkshopHeader") Solid(image, palette.page, false);
            else if (image.name == "Viewport") Solid(image, palette.ink, false);
        }
        Move(root, "MediaSlot", .045f, .265f, .955f, .906f);
        Move(root, "StoryCard", .045f, .119f, .955f, .25f);
        Move(root, "WorkshopHeader", .045f, .938f, .955f, .988f);
        Move(root, "StoryProgressTrack", .045f, .917f, .955f, .924f);
        var track = root.Find("StoryProgressTrack")?.GetComponent<Image>(); Solid(track, Color.Lerp(palette.page, palette.line, .2f), false);
        if (story.pageProgress != null) { story.pageProgress.sprite = Round; story.pageProgress.color = palette.accent; }
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) ThemeText(text);
        ThemeText(story.titleText, 47); ThemeText(story.captionText, 33); ThemeText(story.chapterText, 32);
        foreach (var button in root.GetComponentsInChildren<Button>(true)) StyleButton(button, button.name == "Next");
    }

    private static void StyleTransition(ChapterTransitionUI transition)
    {
        if (transition == null) return;
        var root = transition.transform; AddSafeArea(root);
        Solid(root.Find("Background")?.GetComponent<Image>(), palette.page, false);
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "Frame" || image.name == "Caption") Solid(image, palette.card);
            else if (image.name == "Header") Solid(image, palette.page, false);
            else if (image.name == "Viewport") Solid(image, palette.ink, false);
        }
        Move(root, "MediaSlot", .045f, .265f, .955f, .916f);
        Move(root, "Caption", .045f, .119f, .955f, .25f);
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) ThemeText(text);
        ThemeText(transition.title, 47); ThemeText(transition.caption, 33);
        if (transition.status != null) transition.status.color = palette.muted;
        StyleButton(transition.skipButton, true);
    }

    private static void StyleResult(Transform root)
    {
        GetOrAdd<MobileUILayer>(root.gameObject).Configure(170); GetOrAdd<GraphicRaycaster>(root.gameObject);
        AddSafeArea(root); Solid(root.GetComponent<Image>(), new Color(.06f, .20f, .25f, .84f), false);
        foreach (var image in root.GetComponentsInChildren<Image>(true))
            if (image.name == "AltarCard" || image.name == "VictoryCard") Solid(image, palette.card);
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) ThemeText(label);
        foreach (var button in root.GetComponentsInChildren<Button>(true)) StyleButton(button, button.name.Contains("Watch") || button.name.Contains("Next"));
    }

    private static RectTransform Modal(Transform root, string title, float low = .20f, float high = .80f)
    {
        Solid(GetOrAdd<Image>(root.gameObject), new Color(.06f, .20f, .25f, .84f), false);
        root.GetComponent<Image>().raycastTarget = true; AddSafeArea(root);
        var layer = GetOrAdd<Canvas>(root.gameObject); layer.overrideSorting = true; layer.sortingOrder = 180;
        GetOrAdd<MobileUILayer>(root.gameObject).Configure(180);
        GetOrAdd<GraphicRaycaster>(root.gameObject);
        var panel = MobileUIArt.Panel(root, "MobilePanel", .055f, low, .945f, high, palette);
        MobileUIArt.Text(panel.transform, "Title", title, 58, palette.ink, .07f, .855f, .93f, .97f, TextAlignmentOptions.Center);
        return panel.rectTransform;
    }

    private static void BuildPause(CanvasScript canvas)
    {
        var root = canvas.pauseMenuUI.transform;
        foreach (Transform child in root.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        var panel = Modal(root, "잠깐 쉬어가기", .24f, .77f);
        var resume = MobileUIArt.Button(panel, "Resume", "계속하기", 43, .08f, .61f, .92f, .77f, palette, true);
        UnityEventTools.AddPersistentListener(resume.onClick, canvas.ResumeGame);
        var settings = MobileUIArt.Button(panel, "Settings", "설정", 43, .08f, .39f, .92f, .55f, palette, false);
        UnityEventTools.AddPersistentListener(settings.onClick, canvas.SettingsMenu);
        var retry = MobileUIArt.Button(panel, "Retry", "다시 도전", 43, .08f, .17f, .92f, .33f, palette, false);
        UnityEventTools.AddPersistentListener(retry.onClick, canvas.LoadGame);
        root.gameObject.SetActive(false);
    }

    private static void BuildSettings(CanvasScript canvas)
    {
        var root = canvas.settingsMenuUI.transform;
        // Preserve the original sliders, callbacks and privacy component; only replace their presentation.
        var keep = new[] { canvas.sensitivitySlider.transform, canvas.volumeSlider.transform };
        var privacy = root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "AdPrivacyOptions");
        foreach (var item in keep) item.SetParent(root, false);
        if (privacy != null) privacy.transform.SetParent(root, false);
        foreach (Transform child in root.Cast<Transform>().ToArray())
            if (!keep.Contains(child) && (privacy == null || child != privacy.transform)) Object.DestroyImmediate(child.gameObject);
        var panel = Modal(root, "설정", .15f, .85f);
        ConfigureSlider(canvas.sensitivitySlider, panel, "좌우 조작 감도", .63f, .75f);
        ConfigureSlider(canvas.volumeSlider, panel, "소리 크기", .42f, .54f);
        if (privacy != null)
        {
            ReparentButton(privacy, panel, .08f, .27f, .92f, .36f, "광고 개인정보 설정", false);
            ThemeText(privacy.GetComponentInChildren<TMP_Text>(true), 30);
        }
        var reset = MobileUIArt.Button(panel, "Reset", "초기화", 35, .08f, .10f, .47f, .22f, palette, false);
        UnityEventTools.AddPersistentListener(reset.onClick, canvas.ResetSettings);
        var back = MobileUIArt.Button(panel, "Back", "뒤로", 35, .53f, .10f, .92f, .22f, palette, true);
        UnityEventTools.AddPersistentListener(back.onClick, canvas.PauseGame);
        root.gameObject.SetActive(false);
    }

    private static void ConfigureSlider(Slider slider, Transform panel, string label, float y, float labelY)
    {
        slider.transform.SetParent(panel, false); MobileUIArt.SetRect((RectTransform)slider.transform, .10f, y, .90f, y + .065f);
        foreach (Transform child in slider.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        var track = MobileUIArt.Image(slider.transform, "Track", .04f, .32f, .96f, .68f, Color.Lerp(palette.card, palette.line, .18f), true);
        var fillArea = MobileUIArt.Rect(slider.transform, "FillArea", .04f, .32f, .96f, .68f);
        var fill = MobileUIArt.Image(fillArea, "Fill", 0, 0, 1, 1, palette.accent, true);
        var handleArea = MobileUIArt.Rect(slider.transform, "HandleArea", .04f, 0, .96f, 1);
        var handle = MobileUIArt.Image(handleArea, "Handle", 0, .5f, 0, .5f, palette.primary, true);
        handle.rectTransform.sizeDelta = new Vector2(64, 64); AddOutline(handle.gameObject);
        slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        var colors = slider.colors; colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white; slider.colors = colors;
        MobileUIArt.Text(panel, label, label, 37, palette.ink, .1f, labelY, .9f, labelY + .06f);
    }

    private static void BuildLobbySettings(CanvasScript canvas)
    {
        var root = canvas.transform.Find("UI/Setting"); if (root == null) return;
        var setting = root.GetComponent<SettingScript>(); if (setting == null) return;
        var buttons = root.GetComponentsInChildren<Button>(true);
        var sound = buttons.First(b => b.name == "Sound_Toggle");
        var vibration = buttons.First(b => b.name == "Vibration_Toggle");
        var close = buttons.First(b => b.name == "Close");
        foreach (var button in buttons) button.transform.SetParent(root, false);
        foreach (Transform child in root.Cast<Transform>().ToArray()) if (!buttons.Any(b => b.transform == child)) Object.DestroyImmediate(child.gameObject);
        var panel = Modal(root, "설정", .19f, .81f);
        // UI already owns the safe area, so this nested modal must not apply the inset twice.
        var safe = root.GetComponent<MobileSafeArea>(); if (safe != null) Object.DestroyImmediate(safe);
        var soundLabel = ReparentButton(sound, panel, .08f, .63f, .92f, .77f, "소리 켜짐", true);
        var vibrationLabel = ReparentButton(vibration, panel, .08f, .43f, .92f, .57f, "진동 켜짐", true);
        var legal = buttons.Where(b => b != sound && b != vibration && b != close).ToArray();
        for (int i = 0; i < legal.Length; i++)
        {
            string label = legal[i].GetComponentInChildren<TMP_Text>(true)?.text ?? "안내";
            ReparentButton(legal[i], panel, .08f + i * .44f, .29f, .48f + i * .44f, .37f, label, false);
        }
        ReparentButton(close, panel, .08f, .08f, .92f, .22f, "닫기", false);
        setting.ConfigureMobile(sound, vibration, soundLabel, vibrationLabel);
        EditorUtility.SetDirty(setting); root.gameObject.SetActive(false);
    }

    private static TMP_Text ReparentButton(Button button, Transform parent, float x, float y, float xx, float yy, string label, bool primary)
    {
        button.transform.SetParent(parent, false); MobileUIArt.SetRect((RectTransform)button.transform, x, y, xx, yy);
        var text = button.GetComponent<TMP_Text>();
        if (text != null)
        {
            // Legal buttons put TextMeshPro on the button itself; keep its URL component and text graphic.
            text.text = label; ThemeText(text, 29); text.alignment = TextAlignmentOptions.Center; return text;
        }
        foreach (Transform child in button.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        button.targetGraphic = GetOrAdd<Image>(button.gameObject);
        StyleButton(button, primary);
        return MobileUIArt.Text(button.transform, "Label", label, 37, palette.ink, .04f, .10f, .96f, .90f, TextAlignmentOptions.Center);
    }

    private static void StyleLobbyHeader(CanvasScript canvas)
    {
        var ui = canvas.transform.Find("UI"); var top = ui.Find("Top");
        if (top != null)
        {
            MobileUIArt.SetRect((RectTransform)top, 0, 0, 1, 1);
            foreach (var group in top.GetComponentsInChildren<GridLayoutGroup>(true)) Object.DestroyImmediate(group);
            Move(top, "Resource", .045f, .89f, .80f, .95f);
            Move(top, "Setting", .83f, .89f, .955f, .95f);
            Move(top, "Back", .035f, .931f, .175f, .988f);
            var resource = top.Find("Resource");
            Move(resource, "Coin", 0, 0, .49f, 1); Move(resource, "Jewel", .52f, 0, 1, 1);
            foreach (Transform currency in resource)
            {
                Solid(currency.GetComponent<Image>(), palette.card);
                Move(currency, "Icon", .03f, .12f, .28f, .88f);
                var value = currency.GetComponentInChildren<TMP_Text>(true);
                if (value != null) MobileUIArt.SetRect(value.rectTransform, .29f, .08f, .95f, .92f);
            }
            foreach (var text in top.GetComponentsInChildren<TMP_Text>(true)) ThemeText(text, 37);
            var settings = top.Find("Setting/Image")?.GetComponent<Button>();
            if (settings != null) ReparentButton(settings, settings.transform.parent, 0, 0, 1, 1, "설정", false);
            var back = top.Find("Back/Image")?.GetComponent<Button>();
            if (back != null) ReparentButton(back, back.transform.parent, 0, 0, 1, 1, "뒤로", false);
        }
        var start = ui.Find("Main/StartArea");
        // This legacy image is a flattened +20 ad placeholder with no button or reward callback.
        var adPlaceholder = ui.Find("Main/Ads")?.GetComponent<Image>();
        if (adPlaceholder != null) adPlaceholder.enabled = false;
        if (start != null)
        {
            foreach (var image in start.GetComponentsInChildren<Image>(true)) { Solid(image, Color.clear, false); var outline = image.GetComponent<Outline>(); if (outline != null) Object.DestroyImmediate(outline); }
        }
        var center = ui.Find("Main/Center");
        if (center != null)
        {
            foreach (Transform child in center.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            MobileUIArt.SetRect((RectTransform)center, 0, 0, 1, 1);
            var title = MobileUIArt.Panel(center, "ChapterTitle", .075f, .75f, .925f, .854f, palette);
            var chapter = canvas.GetComponent<ChapterProgression>();
            string chapterName = chapter.chapter == 1 ? "노량진 수산시장" : chapter.chapter == 2 ? "고속도로" : "휴게소";
            MobileUIArt.Text(title.transform, "Chapter", "CHAPTER " + chapter.chapter.ToString("00"), 26, palette.muted, .06f, .60f, .94f, .9f, TextAlignmentOptions.Center);
            MobileUIArt.Text(title.transform, "Name", chapterName, 51, palette.ink, .04f, .08f, .96f, .61f, TextAlignmentOptions.Center);
            var hint = MobileUIArt.Panel(center, "StartHint", .08f, .32f, .92f, .46f, palette);
            MobileUIArt.Text(hint.transform, "Title", "좌우로 움직여 출발!", 41, palette.ink, .04f, .66f, .96f, .96f, TextAlignmentOptions.Center);
            var gesture = new GameObject("Swipe finger and arrows", typeof(RectTransform)).AddComponent<IndianOceanAssets.ShooterSurvival.SwipeStartHint>();
            gesture.transform.SetParent(hint.transform, false); gesture.raycastTarget = false;
            gesture.rectTransform.anchorMin = gesture.rectTransform.anchorMax = new Vector2(.5f, .34f);
            gesture.rectTransform.sizeDelta = new Vector2(310, 90);
        }
        var bottom = ui.Find("Main/Bottom");
        if (bottom != null)
        {
            foreach (var layout in bottom.GetComponents<LayoutGroup>()) Object.DestroyImmediate(layout);
            MobileUIArt.SetRect((RectTransform)bottom, .035f, .045f, .965f, .156f);
            string[] names = { "Skin_Button", "Upgrade_Button", "Story_Button" };
            for (int i = 0; i < names.Length; i++)
            {
                var button = bottom.Find(names[i])?.GetComponent<Button>(); if (button == null) continue;
                MobileUIArt.SetRect((RectTransform)button.transform, i * .34f, 0, i * .34f + .32f, 1);
                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true)) { MobileUIArt.SetRect(label.rectTransform, .03f, .1f, .97f, .9f); ThemeText(label, 40); }
            }
        }
        foreach (var text in canvas.transform.Find("TapToPlay").GetComponentsInChildren<TMP_Text>(true)) { text.text = "터치해서 출발"; ThemeText(text, 48); }
    }
}
