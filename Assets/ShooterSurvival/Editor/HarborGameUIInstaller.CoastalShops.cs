using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class HarborGameUIInstaller
{
    private static void CoastalUpgrades(CanvasScript canvas)
    {
        var window=canvas.transform.Find("UI/Upgrade2");
        var background=window.GetComponent<Image>();background.sprite=FlatFillSprite();background.color=Cream;
        Image(window,"CoastalFrame",Art("Frame"),0,0,1,1);
        Move(window,"TitleSign",.025f,.93f,.975f,.993f);Move(window,"Title",.06f,.938f,.51f,.986f);
        window.Find("Title").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Left;
        Move(window,"Coins",.53f,.945f,.805f,.983f);
        var close=window.Find("Back").GetComponent<Button>();Move(window,"Back",.84f,.938f,.967f,.989f);CoastalClose(close);
        var tabs=window.GetComponent<HarborUpgradeTabs>();tabs.activeSprite=Art("YellowButton");tabs.inactiveSprite=Art("IvoryPanel");
        Move(window,"PermanentTab",.035f,.866f,.495f,.924f);Move(window,"ChapterTab",.505f,.866f,.965f,.924f);
        CoastalButton(tabs.permanentTab,true);CoastalButton(tabs.chapterTab);
        Move(window,"Merchant",.04f,.725f,.96f,.858f);window.Find("Merchant").GetComponent<Image>().preserveAspect=false;
        var summary=window.Find("CurrentStats");Move(window,"CurrentStats",.035f,.615f,.965f,.72f);
        Image(summary,"NavyHeader",Art("NavyPanel"),0,.64f,1,1).transform.SetAsFirstSibling();
        var heading=summary.Find("Heading").GetComponent<TMP_Text>();heading.color=Cream;
        Move(summary,"Heading",.10f,.69f,.90f,.94f);
        Image(summary,"AttackIcon",Art("Attack"),.04f,.13f,.11f,.50f,true);
        Image(summary,"HeartIcon",Art("Heart"),.53f,.14f,.60f,.50f,true);
        Move(summary,"AttackLabel",.13f,.15f,.32f,.49f);Move(summary,"Attack",.32f,.10f,.49f,.56f);
        Move(summary,"HealthLabel",.62f,.15f,.78f,.49f);Move(summary,"Health",.79f,.10f,.96f,.56f);
        var viewport=window.Find("UpgradeViewport");Move(window,"UpgradeViewport",.028f,.026f,.972f,.606f);
        var grid=viewport.GetComponentInChildren<EquipmentCardGrid>(true);grid.cardHeight=208;grid.grid.spacing=new Vector2(0,12);
        foreach(var card in viewport.GetComponentsInChildren<UpgradeUI>(true))
        {
            SetCoastalImage(card.GetComponent<Image>(),Art("RowPanel"));
            var up=card.transform.Find("Up");
            Move(up,"Name",.235f,.72f,.73f,.96f);Move(up,"Level",.235f,.49f,.73f,.71f);
            Move(up,"Effect",.235f,.28f,.73f,.49f);Move(up,"CurrentValue",.235f,.055f,.405f,.285f);Move(up,"Arrow",.415f,.055f,.465f,.285f);Move(up,"NextValue",.48f,.055f,.73f,.285f);
            foreach(var text in up.GetComponentsInChildren<TMP_Text>(true))if(text.fontSize<32)text.fontSize=32;
            up.Find("Level").GetComponent<TMP_Text>().color=Ink;
            CoastalButton(card.transform.Find("Down").GetComponent<Button>(),true);
        }
        grid.RefreshLayout();
        var chapters=window.Find("ChapterUpgrades");Move(window,"ChapterUpgrades",.028f,.026f,.972f,.606f);
        var explanation=chapters.Find("Explanation").GetComponent<TMP_Text>();
        explanation.text="매회 공격력/체력 +5% / 챕터당 최대 5회";explanation.color=Ink;explanation.fontSize=31;
        var layout=chapters.GetComponentInChildren<EquipmentCardGrid>(true);layout.cardHeight=345;layout.grid.spacing=new Vector2(0,16);
        foreach(var card in chapters.GetComponentsInChildren<ChapterUpgradeCardUI>(true))
        {
            SetCoastalImage(card.GetComponent<Image>(),Art("RowPanel"));
            var root=card.transform;Move(root,"ChapterArt",.025f,.13f,.25f,.91f);
            card.chapterLabel=Text(root,"ChapterNumber","CHAPTER "+card.chapter.ToString("00"),28,Ink,.28f,.80f,.965f,.93f,TextAlignmentOptions.Left);
            Move(root,"Title",.28f,.665f,.965f,.84f);card.title.fontSize=39;
            Move(root,"Effects",.28f,.09f,.70f,.63f);card.effects.fontSize=32;card.effects.fontSizeMin=28;card.effects.fontSizeMax=32;
            card.rankLabel=Text(root,"Rank","강화 0 / 5",28,Ink,.70f,.43f,.98f,.56f);
            card.rankPips=new Image[5];
            for(int i=0;i<5;i++)card.rankPips[i]=Image(root,"RankPip"+i,UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),.725f+i*.05f,.57f,.761f+i*.05f,.65f,true);
            Move(root,"Buy",.723f,.07f,.976f,.403f);CoastalButton(card.buyButton,true);
            SetRect(card.actionLabel.rectTransform,.05f,.52f,.95f,.93f);card.actionLabel.fontSize=31;card.actionLabel.color=Ink;
            Move(card.buyButton.transform,"Coin",.06f,.08f,.29f,.47f);
            card.priceLabel=Text(card.buyButton.transform,"Price","0",34,Ink,.31f,.07f,.97f,.48f);
            card.priceLabel.enableAutoSizing=true;card.priceLabel.fontSizeMin=23;card.priceLabel.fontSizeMax=34;
            Move(root,"Locked",.80f,.28f,.90f,.53f);
        }
        layout.RefreshLayout();tabs.ShowPermanent();
    }

    private static void CoastalShop(CanvasScript canvas)
    {
        var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);var root=shop.transform;
        root.Find("Title").GetComponent<TMP_Text>().text="꾸미기";
        Move(root,"HarborTitleSign",.035f,.935f,.965f,.992f);Move(root,"Title",.17f,.94f,.79f,.987f);
        Image(root,"CoastalAnchor",Art("Anchor"),.09f,.946f,.15f,.98f,true);
        CoastalClose(root.Find("Back").GetComponent<Button>());
        Move(root,"CoinWallet",.075f,.879f,.48f,.925f);Move(root,"GemWallet",.52f,.879f,.925f,.925f);
        Move(root,"CoinIcon",.097f,.889f,.16f,.916f);Move(root,"JewelIcon",.546f,.889f,.61f,.916f);
        SetRect(shop.coinBalance.rectTransform,.175f,.885f,.455f,.92f);SetRect(shop.jewelBalance.rectTransform,.625f,.885f,.90f,.92f);
        shop.preview.platformTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(CoastalPath+"PlatformWood.png");
        Move(root,"PreviewArea",.045f,.548f,.955f,.855f);
        SetRect(shop.selectionName.rectTransform,.15f,.837f,.85f,.874f);shop.selectionName.color=Ink;shop.selectionName.fontSize=38;
        var nameplate=Image(root,"CoastalNameplate",Art("Wallet"),.16f,.831f,.84f,.876f);nameplate.transform.SetSiblingIndex(shop.selectionName.transform.GetSiblingIndex());
        Move(root,"RotateHint",.17f,.524f,.79f,.553f);root.Find("RotateHint").GetComponent<TMP_Text>().text="드래그하여 회전";
        Move(root,"ResetPreview",.81f,.522f,.945f,.553f);CoastalButton(root.Find("ResetPreview").GetComponent<Button>());
        var lower=Image(root,"CoastalCatalogPanel",Art("IvoryPanel"),.02f,.017f,.98f,.521f);lower.transform.SetSiblingIndex(1);
        shop.activeTabSprite=Art("YellowButton");shop.inactiveTabSprite=Art("IvoryPanel");
        Button[] tabs={shop.skinTab,shop.shoesTab,shop.hatTab};
        for(int i=0;i<3;i++)
        {
            SetRect((RectTransform)tabs[i].transform,.038f+i*.309f,.458f,.344f+i*.309f,.516f);
            CoastalButton(tabs[i],i==0);var label=tabs[i].GetComponentInChildren<TMP_Text>();SetRect(label.rectTransform,.08f,.12f,.92f,.88f);
            var icon=tabs[i].transform.Find("TabIcon");if(icon!=null)icon.gameObject.SetActive(false);
        }
        Move(root,"EquipmentViewport",.038f,.132f,.95f,.45f);Move(root,"EquipmentScrollbar",.956f,.142f,.968f,.441f);
        var grid=shop.itemsRoot.GetComponent<EquipmentCardGrid>();grid.cardHeight=343;grid.grid.spacing=new Vector2(14,14);
        var card=shop.itemTemplate.transform;
        SetCoastalImage(card.GetComponent<Image>(),Art("RowPanel"));
        Move(card,"Icon",.065f,.46f,.935f,.94f);Move(card,"Name",.055f,.32f,.945f,.465f);
        Move(card,"Effect",.055f,.205f,.945f,.322f);Move(card,"State",.28f,.073f,.93f,.20f);Move(card,"CurrencyIcon",.14f,.092f,.245f,.18f);
        var frame=card.Find("Selected").GetComponent<Image>();SetCoastalImage(frame,Art("SelectionFrame"));
        foreach(var label in card.GetComponentsInChildren<TMP_Text>(true)){label.color=Ink;label.fontSize=label.name=="Name"?34:30;}
        var footer=root.Find("Footer").GetComponent<Image>();SetCoastalImage(footer,Art("IvoryPanel"));SetRect(footer.rectTransform,.025f,.016f,.975f,.123f);
        SetRect(shop.actionButton.GetComponent<RectTransform>(),.065f,.036f,.935f,.108f);CoastalButton(shop.actionButton,true);
        shop.actionText.color=Ink;shop.actionText.fontSize=46;shop.statusText.color=Ink;
        SetRect(shop.statusText.rectTransform,.07f,.111f,.93f,.136f);shop.statusText.fontSize=28;
        shop.messagePanel=BuildCoastalMessage(canvas);
        grid.RefreshLayout();
    }
}
