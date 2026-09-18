using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class HarborGameUIInstaller
{
    private static void BuildUpgrades(CanvasScript canvas, PlayerScript player)
    {
        var window=canvas.transform.Find("UI/Upgrade2");
        var cards=window.GetComponentsInChildren<UpgradeUI>(true).OrderBy(c=>c.UpgradeId).ToArray();
        if(cards.Length!=10)throw new InvalidOperationException("Expected the ten existing permanent upgrades.");
        var content=(RectTransform)cards[0].transform.parent;
        content.SetParent(canvas.transform,false);
        ClearChildren(window);
        Layer(window.gameObject,100);
        var background=GetOrAdd<Image>(window.gameObject);background.sprite=Art("ShopBackdrop");background.color=Color.white;background.type=UnityEngine.UI.Image.Type.Simple;background.raycastTarget=true;
        Image(window,"TitleSign",Art("WoodSign"),.19f,.937f,.715f,.995f);
        Text(window,"Title","신발 개조소",49,Cream,.22f,.939f,.69f,.991f);
        var wallet=Image(window,"Coins",Art("WalletPill"),.735f,.942f,.965f,.991f);
        Image(wallet.transform,"Icon",Art("Coin"),.035f,.12f,.27f,.88f,true);
        var balance=Text(wallet.transform,"Balance","0",35,Ink,.29f,.10f,.94f,.90f);
        balance.enableAutoSizing=true;balance.fontSizeMin=24;balance.fontSizeMax=35;
        wallet.gameObject.AddComponent<MobileWalletUI>().Configure(balance,null);
        var back=Button(window,"Back","",.035f,.941f,.16f,.993f);
        Image(back.transform,"Arrow",Art("BackArrow"),.19f,.15f,.81f,.85f,true);
        CopyButtonCalls(canvas.transform.Find("UI/Top/Back/Image").GetComponent<Button>(),back);
        UnityEventTools.AddBoolPersistentListener(back.onClick,window.gameObject.SetActive,false);
        UnityEventTools.AddPersistentListener(back.onClick,player.ResetStartGesture);
        var permanentTab=Button(window,"PermanentTab","상시 업그레이드",.045f,.876f,.49f,.928f,true,37);
        var chapterTab=Button(window,"ChapterTab","챕터 업그레이드",.51f,.876f,.955f,.928f,false,37);
        Image(window,"Merchant",Art("MerchantBackdrop"),.045f,.727f,.955f,.87f,true);
        var summary=Image(window,"CurrentStats",Art("ParchmentPanel"),.045f,.626f,.955f,.724f);
        Text(summary.transform,"Heading","현재 능력치",32,Ink,.12f,.66f,.88f,.95f);
        Text(summary.transform,"AttackLabel","공격력",32,Ink,.035f,.16f,.26f,.59f);
        var attack=Text(summary.transform,"Attack","0",49,Ink,.26f,.10f,.49f,.62f);
        Text(summary.transform,"HealthLabel","체력",32,Ink,.53f,.16f,.70f,.59f);
        var health=Text(summary.transform,"Health","0",49,Ink,.70f,.10f,.965f,.62f);
        foreach(var value in new[]{attack,health}){value.enableAutoSizing=true;value.fontSizeMin=29;value.fontSizeMax=49;}
        summary.gameObject.AddComponent<UpgradeSummaryUI>().Configure(player,attack,health);
        var viewport=Rect(window,"UpgradeViewport",.035f,.045f,.965f,.615f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;
        content.SetParent(viewport,false);content.name="PermanentItems";SetRect(content,0,1,1,1);content.pivot=new Vector2(.5f,1);scroll.content=content;
        foreach(var fitter in content.GetComponents<ContentSizeFitter>())Object.DestroyImmediate(fitter);
        var grid=GetOrAdd<GridLayoutGroup>(content.gameObject);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=1;
        grid.spacing=new Vector2(0,18);grid.padding=new RectOffset(8,8,8,24);grid.childAlignment=TextAnchor.UpperCenter;
        var layout=GetOrAdd<EquipmentCardGrid>(content.gameObject);layout.grid=grid;layout.scroll=scroll;layout.cardHeight=221;
        for(int i=0;i<cards.Length;i++)BuildPermanentCard(cards[i],i+1);
        layout.RefreshLayout();
        var chapters=BuildChapterCards(window);
        var tabs=GetOrAdd<HarborUpgradeTabs>(window.gameObject);
        tabs.permanent=viewport.gameObject;tabs.chapters=chapters.gameObject;tabs.permanentTab=permanentTab;tabs.chapterTab=chapterTab;
        tabs.activeSprite=Art("GoldButton");tabs.inactiveSprite=Art("WoodButton");tabs.ShowPermanent();
        window.gameObject.SetActive(false);
    }

    private static void BuildPermanentCard(UpgradeUI card,int id)
    {
        string[] names={"강철 앞코","충격 흡수 깔창","스프링 코일","미사일 장식","보스 파쇄기","코인 주머니","회복 패드","퉁퉁퉁 사후르","붐바르 지원","측면 부스터"};
        string[] stats={"공격력","체력","공격 속도","미사일 지속 시간","보스 피해","코인 획득","초당 체력 회복","지원 체력","지원 공격력","좌우 이동 속도"};
        string[] icons={"SteelToe","CushionInsole","SpringCoil","RocketCharm","BossBreaker","CoinPouch","HealingInsert","SahurShield","BomberCharm","LateralSneaker"};
        ClearChildren(card.transform);
        foreach(var old in card.GetComponents<LayoutGroup>())Object.DestroyImmediate(old);
        var background=GetOrAdd<Image>(card.gameObject);background.sprite=Art("ParchmentPanel");background.type=UnityEngine.UI.Image.Type.Sliced;background.color=Color.white;RemoveOutline(card.gameObject);
        var up=Rect(card.transform,"Up",0,0,1,1);
        var icon=Image(up,"Icon",Art(icons[id-1]),.02f,.10f,.22f,.91f,true);
        var title=Text(up,"Name",names[id-1],35,Ink,.235f,.65f,.72f,.94f,TextAlignmentOptions.Left);
        var level=Text(up,"Level","레벨 0",27,Muted,.238f,.44f,.72f,.65f,TextAlignmentOptions.Left);
        Text(up,"Effect",stats[id-1],27,Ink,.238f,.24f,.72f,.43f,TextAlignmentOptions.Left);
        var current=Text(up,"CurrentValue","+0",30,Ink,.235f,.05f,.405f,.26f,TextAlignmentOptions.Left);
        Text(up,"Arrow",">",27,Muted,.415f,.05f,.465f,.26f);
        var next=Text(up,"NextValue","+4",30,new Color(.1f,.30f,.08f),.48f,.05f,.73f,.26f,TextAlignmentOptions.Left);
        var buy=Button(card.transform,"Down","",.745f,.17f,.976f,.79f,true,28);
        var buyLabel=buy.GetComponentInChildren<TextMeshProUGUI>();buyLabel.text="강화";SetRect(buyLabel.rectTransform,.10f,.53f,.90f,.89f);buyLabel.fontSize=27;
        var currency=Image(buy.transform,"Currency",Art("Coin"),.08f,.10f,.34f,.47f,true);
        var price=Text(buy.transform,"Price","0",33,Ink,.35f,.09f,.95f,.47f);price.enableAutoSizing=true;price.fontSizeMin=22;price.fontSizeMax=33;
        var data=new SerializedObject(card);
        data.FindProperty("upgradeId").intValue=id;data.FindProperty("layoutMode").enumValueIndex=2;
        data.FindProperty("displayNameOverride").stringValue=names[id-1];data.FindProperty("iconOverride").objectReferenceValue=icon.sprite;
        data.FindProperty("nameText").objectReferenceValue=title;data.FindProperty("levelText").objectReferenceValue=level;
        data.FindProperty("currentValueText").objectReferenceValue=current;data.FindProperty("nextValueText").objectReferenceValue=next;
        data.FindProperty("valueText").objectReferenceValue=current;data.FindProperty("descriptionText").objectReferenceValue=null;
        data.FindProperty("priceText").objectReferenceValue=price;data.FindProperty("iconImage").objectReferenceValue=icon;
        data.FindProperty("priceCurrencyImage").objectReferenceValue=currency;data.FindProperty("coinPriceSprite").objectReferenceValue=Art("Coin");data.FindProperty("jewelPriceSprite").objectReferenceValue=Art("Jewel");
        data.FindProperty("dimb").objectReferenceValue=null;data.FindProperty("buyButton").objectReferenceValue=buy;data.FindProperty("buyLabel").objectReferenceValue=buyLabel;
        data.FindProperty("priceLockedColor").colorValue=Muted;data.FindProperty("priceNotEnoughColor").colorValue=new Color(.68f,.12f,.07f);
        data.ApplyModifiedPropertiesWithoutUndo();card.gameObject.SetActive(true);
    }

    private static RectTransform BuildChapterCards(Transform window)
    {
        var root=Rect(window,"ChapterUpgrades",.035f,.045f,.965f,.615f);
        Text(root,"Explanation","챕터를 해금하면 열리는 영구 강화",29,Cream,.02f,.943f,.98f,.99f);
        var viewport=Rect(root,"Viewport",0,0,1,.93f);viewport.gameObject.AddComponent<RectMask2D>();
        var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        var content=Rect(viewport,"Items",0,1,1,1);content.pivot=new Vector2(.5f,1);scroll.content=content;
        var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=1;grid.spacing=new Vector2(0,18);grid.padding=new RectOffset(8,8,8,22);
        var layout=content.gameObject.AddComponent<EquipmentCardGrid>();layout.grid=grid;layout.scroll=scroll;layout.cardHeight=368;
        var catalog=AssetDatabase.LoadAssetAtPath<ChapterUpgradeCatalog>("Assets/ShooterSurvival/Resources/Upgrades/ChapterWorkshop.asset");
        foreach(var row in catalog.entries.OrderBy(r=>r.chapter))
        {
            var panel=Image(content,"Chapter"+row.chapter,Art("ParchmentPanel"),0,0,1,1);
            Image(panel.transform,"ChapterArt",Art("Chapter"+row.chapter),.025f,.07f,.275f,.94f,true);
            var title=Text(panel.transform,"Title",$"CHAPTER {row.chapter:00} / {row.title}",38,Ink,.30f,.74f,.965f,.94f,TextAlignmentOptions.Left);
            var effects=Text(panel.transform,"Effects","",42,Ink,.30f,.31f,.965f,.73f,TextAlignmentOptions.Left);
            var button=Button(panel.transform,"Buy","",.30f,.055f,.968f,.28f,true,31);
            var label=button.GetComponentInChildren<TextMeshProUGUI>();SetRect(label.rectTransform,.11f,.05f,.96f,.95f);label.enableAutoSizing=true;label.fontSizeMin=22;label.fontSizeMax=31;
            var coin=Image(button.transform,"Coin",Art("Coin"),.025f,.16f,.12f,.84f,true);
            var marker=Image(panel.transform,"Locked",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/Image/Lobby/Upgrade/img_lobby_upgrade_lock.png"),.09f,.35f,.21f,.67f,true);
            var card=panel.gameObject.AddComponent<ChapterUpgradeCardUI>();card.chapter=row.chapter;card.title=title;card.effects=effects;card.buyButton=button;card.actionLabel=label;card.lockMarker=marker.gameObject;card.coinIcon=coin.gameObject;
            effects.text=$"공격력 +{row.attackPercent:0}%\n체력 +{row.healthPercent:0}%";
            label.text=row.chapter==1?$"강화   {row.coinCost:N0}":$"챕터 {row.chapter} 해금 후 가능";
            marker.gameObject.SetActive(row.chapter>1);coin.gameObject.SetActive(row.chapter==1);
        }
        layout.RefreshLayout();root.gameObject.SetActive(false);return root;
    }
}
