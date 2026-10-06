using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class HarborGameUIInstaller
{
    private const string AccountFontPath = "Assets/ShooterSurvival/Resources/Account/AccountUI SDF.asset";

    public static object ApplyAccountSettingsAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Clean Edit Mode required");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        int installed = 0;
        try
        {
            foreach (string name in new[] { "Noryangjin_MapTool_Mode_SR18_Revamp", "HighWay", "RestStop", "Jamsil", "ShoeTower" })
            {
                string path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
                var scene = EditorSceneManager.OpenScene(path);
                var settings = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HarborSettingsPanel>(true)).Single();
                if (settings.GetComponent<HarborAccountPanel>() != null) continue;
                InstallAccount(settings);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                installed++;
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return new { installed, scenes = 5, gameplayChanged = false };
    }

    private static TMP_FontAsset AccountFont(TMP_FontAsset source)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AccountFontPath);
        if (font != null) return font;
        if (source == null || source.sourceFontFile == null) throw new InvalidOperationException("Account UI requires the existing Korean source font");
        Directory.CreateDirectory(Path.GetDirectoryName(AccountFontPath));
        AssetDatabase.Refresh();
        font = TMP_FontAsset.CreateFontAsset(source.sourceFontFile, 64, 8, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
        font.name = "AccountUI SDF";
        font.TryAddCharacters("Google Play 로그인계정동기화탈퇴 및 데이터 삭제취소게임중이어갈진행선택클라우드저장기정보를확인하는불러오지못했습니다다른에서도진행종료한뒤주세요코인보석강꾸미챕터복구할수없자체는되지않습니다1 2 3 4 5 6 7 8 9 0 · . : , …\n대기준비기기의유지됩니다선택않덮어써집니다");
        AssetDatabase.CreateAsset(font, AccountFontPath);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
        AssetDatabase.SaveAssetIfDirty(font);
        return font;
    }

    private static void InstallAccount(HarborSettingsPanel settings)
    {
        var panel = settings.transform.Find("Panel");
        var template = settings.termsButton;
        var font = AccountFont(template.GetComponentInChildren<TMP_Text>(true).font);
        foreach (Transform child in panel)
        {
            if (new[] { "Header", "Title", "Close", "AnchorCrest", "RunActions" }.Contains(child.name)) continue;
            if (child is RectTransform r && !Mathf.Approximately(r.anchorMin.y, r.anchorMax.y))
            {
                r.anchorMin = new Vector2(r.anchorMin.x, .2f + .78f * r.anchorMin.y);
                r.anchorMax = new Vector2(r.anchorMax.x, .2f + .78f * r.anchorMax.y);
            }
        }
        var account = settings.gameObject.AddComponent<HarborAccountPanel>();
        account.settings = settings;
        Button CopyButton(string name, string text, Transform parent, float x0, float y0, float x1, float y1)
        {
            var button = UnityEngine.Object.Instantiate(template, parent);
            button.name = name;
            button.onClick = new Button.ButtonClickedEvent();
            foreach (var chevron in button.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Chevron")) chevron.gameObject.SetActive(false);
            SetRect(button.GetComponent<RectTransform>(), x0, y0, x1, y1);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.font = font;
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 26;
            label.fontSizeMax = 40;
            SetRect(label.rectTransform, .04f, .1f, .96f, .9f);
            return button;
        }
        TMP_Text Text(string name, Transform parent, string value, float y0, float y1, float size)
        {
            var original = template.GetComponentInChildren<TMP_Text>(true);
            var label = UnityEngine.Object.Instantiate(original, parent);
            label.name = name;
            label.font = font;
            label.text = value;
            label.color = Ink;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 25;
            label.fontSizeMax = size;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            SetRect(label.rectTransform, .07f, y0, .93f, y1);
            return label;
        }
        account.status = Text("AccountStatus", panel, "로그인하면 진행이 계정에 저장됩니다.", .177f, .24f, 32);
        account.loginButton = CopyButton("GooglePlayAccount", "Google Play 로그인", panel, .065f, .095f, .58f, .16f);
        account.deleteButton = CopyButton("DeleteGameAccount", "계정 탈퇴", panel, .605f, .095f, .935f, .16f);
        account.deleteButton.GetComponent<Image>().color = new Color(1f, .85f, .82f);

        var blocker = new GameObject("AccountDialog", typeof(RectTransform), typeof(Image));
        blocker.transform.SetParent(settings.transform, false);
        SetRect(blocker.GetComponent<RectTransform>(), 0, 0, 1, 1);
        blocker.GetComponent<Image>().color = new Color(.02f, .05f, .10f, .93f);
        var surface = UnityEngine.Object.Instantiate(panel.GetComponent<Image>(), blocker.transform);
        surface.name = "AccountDialogPanel";
        foreach (Transform child in surface.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        // Responsive modal stays within the portrait safe-area parent.
        SetRect(surface.rectTransform, .06f, .22f, .94f, .78f);
        var fitter = surface.GetComponent<AspectRatioFitter>();
        if (fitter != null) UnityEngine.Object.DestroyImmediate(fitter);
        account.dialog = blocker;
        account.dialogTitle = Text("AccountDialogTitle", surface.transform, "게임 계정 탈퇴", .80f, .94f, 48);
        account.dialogBody = Text("AccountDialogBody", surface.transform, "", .35f, .78f, 35);
        account.firstButton = CopyButton("AccountFirstChoice", "탈퇴 및 데이터 삭제", surface.transform, .07f, .20f, .93f, .31f);
        account.secondButton = CopyButton("AccountSecondChoice", "취소", surface.transform, .07f, .07f, .93f, .18f);
        account.cancelButton = CopyButton("AccountCancel", "취소", surface.transform, .07f, .01f, .93f, .06f);
        account.cancelButton.gameObject.SetActive(false);
        blocker.SetActive(false);
        foreach (var component in settings.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }
}
