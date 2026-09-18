using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using IndianOceanAssets.ShooterSurvival.Ads;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class ChapterPresentationInstaller
{
    public static object ApplyOpenScene(TMP_FontAsset fontOverride = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var scene = SceneManager.GetActiveScene();
        if (!NoryangjinMapToolWindow.IsMapToolScenePath(scene.path)) throw new InvalidOperationException("Chapter scene required.");
        var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas");
        // This legacy image promised gems but had no button or ad implementation.
        var legacyAdArt = canvas.transform.Find("UI/Main/Ads");
        if (legacyAdArt != null) legacyAdArt.gameObject.SetActive(false);
        fontOverride ??= GameUIFont.Load();
        if (fontOverride == null) throw new InvalidOperationException("CC0 Game UI font missing.");
        RefinedGameUI.BuildShop(canvas);
        OpeningWorkshopPresentation.ApplyOpening(canvas, fontOverride);
        WorkshopPresentationUI.BuildDefeat(canvas, fontOverride);
        var chapter = canvas.GetComponent<ChapterProgression>() ?? canvas.AddComponent<ChapterProgression>();
        chapter.chapter = scene.name == "RestStop" ? 3 : scene.name == "HighWay" ? 2 : 1;
        chapter.nextScene = chapter.chapter == 1 ? "HighWay" : chapter.chapter == 2 ? "RestStop" : "";
        chapter.nextChapterTitle = chapter.chapter == 1 ? "고속도로" : "휴게소";
        chapter.nextChapterCaption = chapter.chapter == 1 ? "눈앞을 스치는 차들. 빈틈을 찾아 앞으로 나아가자." : "잠깐 쉬려 했는데, 모두가 나를 보고 놀랐다.";
        string clipPath = chapter.chapter == 1 ? "Assets/JH/UI/ChapterTransitions/Highway_Entry.mp4" : "Assets/JH/UI/ChapterTransitions/RestStop_Entry.mp4";
        chapter.nextChapterMovie = chapter.chapter < 3 ? AssetDatabase.LoadAssetAtPath<VideoClip>(clipPath) : null;
        if (chapter.chapter < 3 && chapter.nextChapterMovie == null) throw new InvalidOperationException("Transition movie has not imported: " + clipPath);
        chapter.transitionUI = OpeningWorkshopPresentation.BuildTransition(canvas, fontOverride);
        chapter.clearRewardText = WorkshopPresentationUI.BuildVictory(canvas, fontOverride);
        BuildPrivacyOptions(canvas.GetComponent<CanvasScript>(), fontOverride ?? canvas.GetComponentInChildren<OpeningStoryUI>(true).captionText.font);
        foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true)) label.font = fontOverride;
        foreach (var root in scene.GetRootGameObjects()) foreach (var component in root.GetComponentsInChildren<Component>(true))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorUtility.SetDirty(chapter);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Chapter presentation save failed.");
        return new { scene = scene.name, chapter = chapter.chapter, next = chapter.nextScene, transitionSeconds = chapter.nextChapterMovie != null ? chapter.nextChapterMovie.length : 0, font = fontOverride != null ? fontOverride.name : "existing font; CC0 decision pending" };
    }

    public static object ConfigureTestAds()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        if ((int)PlayerSettings.Android.minSdkVersion < 24) PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings")).FirstOrDefault(t => t != null);
        if (type == null) throw new InvalidOperationException("Pinned Google Mobile Ads package has not imported.");
        var settings = (UnityEngine.Object)type.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        var data = new SerializedObject(settings);
        var android = data.FindProperty("adMobAndroidAppId"); var ios = data.FindProperty("adMobIOSAppId");
        if (string.IsNullOrEmpty(android.stringValue)) android.stringValue = "ca-app-pub-3940256099942544~3347511713";
        if (string.IsNullOrEmpty(ios.stringValue)) ios.stringValue = "ca-app-pub-3940256099942544~1458002511";
        data.FindProperty("overrideDefaultGmaAndroidSdk").boolValue = true; data.FindProperty("selectedGmaAndroidSdk").intValue = 0;
        data.ApplyModifiedPropertiesWithoutUndo();
        const string path = "Assets/ShooterSurvival/Resources/Ads/RewardedAdsSettings.asset";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var gameSettings = AssetDatabase.LoadAssetAtPath<RewardedAdsSettings>(path);
        if (gameSettings == null) { gameSettings = ScriptableObject.CreateInstance<RewardedAdsSettings>(); AssetDatabase.CreateAsset(gameSettings, path); }
        EditorUtility.SetDirty(settings); EditorUtility.SetDirty(gameSettings); AssetDatabase.SaveAssets();
        return new { configured = true, testAds = gameSettings.useTestAds, productionIdsRequired = string.IsNullOrEmpty(gameSettings.androidRewardedUnitId) };
    }

    private static void BuildPrivacyOptions(CanvasScript canvas, TMP_FontAsset font)
    {
        if (canvas.settingsMenuUI == null) throw new InvalidOperationException("Settings panel missing.");
        var parent = canvas.settingsMenuUI.transform;
        var previous = parent.Find("AdPrivacyOptions"); if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var root = new GameObject("AdPrivacyOptions", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        var rect = (RectTransform)root.transform; rect.anchorMin = new Vector2(.16f,.11f); rect.anchorMax = new Vector2(.84f,.17f); rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = root.GetComponent<Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/메뉴이름.png"); image.type = Image.Type.Sliced; image.raycastTarget = true;
        var button = root.GetComponent<Button>(); button.targetGraphic = image;
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(root.transform, false);
        var text = label.GetComponent<TextMeshProUGUI>(); text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero; text.font = font; text.text = "광고 개인정보 설정";
        text.fontSize = 27; text.alignment = TextAlignmentOptions.Center; text.color = new Color(.16f,.075f,.035f); text.raycastTarget = false;
        root.AddComponent<RewardedPrivacyOptionsButton>();
    }
}
