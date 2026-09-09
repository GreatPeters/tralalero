using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CosmeticShopInstaller
{
    private static readonly Color Ink = new Color(.12f,.08f,.04f);
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var catalog = AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>("Assets/ShooterSurvival/Resources/Cosmetics/Catalog.asset");
        if (catalog == null || CosmeticTables.Rows.Count != 12) throw new InvalidOperationException("Catalog/data missing");
        var reports = new System.Collections.Generic.List<object>();
        foreach (string name in new[] { "Noryangjin_MapTool_Mode", "Noryangjin_MapTool_Mode_SR18" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
            var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas").transform;
            var ui = canvas.Find("UI"); if (ui.Find("CosmeticShop") != null) throw new InvalidOperationException("Shop already installed");
            string backup = "tmp/backups/sr18-release-20260910/before-cosmetics-" + name + ".unity";
            EditorSceneManager.SaveScene(scene, backup, true);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/JH/Font/쩡야공유/GmarketSansTTFBold SDF2.asset");
            var paper = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/카드.png");
            var root = Rect(ui, "CosmeticShop", Vector2.zero, Vector2.one);
            root.gameObject.SetActive(false);
            var bg = root.gameObject.AddComponent<Image>(); bg.color = new Color(.055f,.095f,.12f);
            root.SetSiblingIndex(ui.Find("Top").GetSiblingIndex());
            var shop = root.gameObject.AddComponent<CosmeticShopUI>(); shop.visuals = catalog;
            Text(root,"Title",font,"상어 꾸미기",46,Color.white,new Vector2(.08f,.84f),new Vector2(.92f,.88f));
            Text(root,"Subtitle",font,"피부 · 신발 · 모자를 나만의 조합으로",23,new Color(.76f,.80f,.78f),new Vector2(.08f,.805f),new Vector2(.92f,.835f));
            var previewArea = Rect(root,"PreviewArea",new Vector2(.05f,.54f),new Vector2(.95f,.80f));
            var previewRect = Rect(previewArea,"Preview",Vector2.zero,Vector2.one);
            var raw = previewRect.gameObject.AddComponent<RawImage>(); raw.raycastTarget = false;
            var fit = previewRect.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 1;
            var preview = root.gameObject.AddComponent<CosmeticPreview>(); preview.catalog = catalog; preview.display = raw; shop.preview = preview;
            shop.skinTab = Button(root,"SkinTab",font,"피부",new Vector2(.07f,.475f),new Vector2(.34f,.53f),null);
            shop.shoesTab = Button(root,"ShoesTab",font,"신발",new Vector2(.365f,.475f),new Vector2(.635f,.53f),null);
            shop.hatTab = Button(root,"HatTab",font,"모자",new Vector2(.66f,.475f),new Vector2(.93f,.53f),null);
            var items = Rect(root,"Items",new Vector2(.07f,.18f),new Vector2(.93f,.455f));
            var grid = items.gameObject.AddComponent<GridLayoutGroup>(); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            grid.spacing = new Vector2(20,20); grid.cellSize = new Vector2(400,280); grid.childAlignment = TextAnchor.UpperCenter;
            var responsive = items.gameObject.AddComponent<ResponsiveCardGrid>(); responsive.rows = 2; responsive.cardAspect = 1.45f;
            var template = Rect(items,"ItemTemplate",Vector2.zero,Vector2.one);
            var cardImage = template.gameObject.AddComponent<Image>(); cardImage.sprite = paper; cardImage.type = Image.Type.Sliced;
            template.gameObject.AddComponent<Button>();
            Text(template,"Name",font,"",30,Ink,new Vector2(.05f,.78f),new Vector2(.95f,.96f));
            var icon = Rect(template,"Icon",new Vector2(.04f,.05f),new Vector2(.64f,.79f)).gameObject.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
            Text(template,"State",font,"",24,Ink,new Vector2(.64f,.22f),new Vector2(.96f,.67f));
            template.gameObject.SetActive(false); shop.itemTemplate = template.gameObject; shop.itemsRoot = items;
            shop.detailText = Text(root,"Detail",font,"",23,new Color(.86f,.85f,.74f),new Vector2(.07f,.135f),new Vector2(.93f,.175f));
            shop.actionButton = Button(root,"PurchaseEquip",font,"착용하기",new Vector2(.20f,.074f),new Vector2(.90f,.126f),paper);
            shop.actionText = shop.actionButton.GetComponentInChildren<TMP_Text>(); shop.actionText.color = Ink;
            shop.statusText = Text(root,"Status",font,"선택한 상품을 미리 보고 구매하세요.",21,new Color(.74f,.8f,.77f),new Vector2(.2f,.035f),new Vector2(.93f,.069f));
            var entry = ui.Find("Main/Bottom/Skin_Button"); var entryImage = entry.GetComponent<Image>(); entryImage.raycastTarget = true;
            var open = entry.GetComponent<Button>() ?? entry.gameObject.AddComponent<Button>(); open.targetGraphic = entryImage;
            entry.GetComponentInChildren<TMP_Text>(true).text = "꾸미기";
            UnityEventTools.AddBoolPersistentListener(open.onClick, root.gameObject.SetActive, true);
            foreach (string item in new[] { "Main/Bottom", "Main/Center", "Main/StartArea", "Main/Ads" })
                UnityEventTools.AddBoolPersistentListener(open.onClick, ui.Find(item).gameObject.SetActive, false);
            var back = ui.Find("Top/Back"); UnityEventTools.AddBoolPersistentListener(open.onClick, back.gameObject.SetActive, true);
            var backButton = back.GetComponentInChildren<Button>(true);
            UnityEventTools.AddBoolPersistentListener(backButton.onClick, root.gameObject.SetActive, false);
            var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.PlayerScript>(true)).Single();
            var customizer = player.GetComponent<PlayerCosmeticCustomizer>() ?? player.gameObject.AddComponent<PlayerCosmeticCustomizer>();
            customizer.modelRoot = player.transform.Find("Original"); customizer.visuals = catalog;
            if (customizer.modelRoot == null) throw new InvalidOperationException("Visible Original shark missing");
            EditorUtility.SetDirty(customizer); EditorUtility.SetDirty(open); EditorUtility.SetDirty(backButton);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            reports.Add(new { scene = name, backup });
        }
        return reports;
    }
    private static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
    {
        var go = new GameObject(name,typeof(RectTransform));var rect = go.GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,TMP_FontAsset font,string value,float size,Color color,Vector2 min,Vector2 max)
    {
        var rect=Rect(parent,name,min,max);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;
        text.color=color;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.enableAutoSizing=true;text.fontSizeMin=size*.75f;text.fontSizeMax=size;return text;
    }
    private static Button Button(Transform parent,string name,TMP_FontAsset font,string label,Vector2 min,Vector2 max,Sprite sprite)
    {
        var rect=Rect(parent,name,min,max);var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=Image.Type.Sliced;image.color=new Color(.19f,.25f,.29f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;Text(rect,"Text",font,label,28,Color.white,new Vector2(.05f,.08f),new Vector2(.95f,.92f));return button;
    }
}
