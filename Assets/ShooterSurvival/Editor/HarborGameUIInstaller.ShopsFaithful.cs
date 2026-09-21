using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static partial class HarborGameUIInstaller
{
    private static Image ShopImage(Transform parent,string name,Sprite sprite,float x,float y,float xx,float yy,bool preserve=false)
    {
        var matches=parent.Cast<Transform>().Where(t=>t.name==name).ToArray();
        foreach(var duplicate in matches.Skip(1))UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
        var image=matches.Length>0?matches[0].GetComponent<Image>():Image(parent,name,sprite,x,y,xx,yy,preserve);
        SetRect(image.rectTransform,x,y,xx,yy);image.sprite=sprite;image.preserveAspect=preserve;image.raycastTarget=false;return image;
    }
    private static TMP_Text ShopLabel(Transform parent,string name,string value,float size,float x,float y,float xx,float yy)
    {
        var matches=parent.Cast<Transform>().Where(t=>t.name==name).ToArray();
        foreach(var duplicate in matches.Skip(1))UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
        var text=matches.Length>0?matches[0].GetComponent<TMP_Text>():Text(parent,name,value,size,Ink,x,y,xx,yy);
        SetRect(text.rectTransform,x,y,xx,yy);text.text=value;ShopType(text,size);return text;
    }
    private static void ShopType(TMP_Text text,float size,bool white=false,bool left=false)
    {
        text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RoundedFontPath);
        text.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(PresetFolder+(white?"/Coastal_ShopWhite.mat":"/Coastal_RoundedInk.mat"));
        text.fontStyle=FontStyles.Normal;text.color=white?Color.white:Ink;
        text.fontSize=size;text.enableAutoSizing=false;text.characterSpacing=0;text.wordSpacing=0;
        text.alignment=left?TextAlignmentOptions.MidlineLeft:TextAlignmentOptions.MidlineGeoAligned;
        text.margin=Vector4.zero;text.extraPadding=true;text.UpdateMeshPadding();
        text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;
    }
    private static void ShopSurface(Image image,string art,float ppu=1)
    {
        SetCoastalImage(image,Art(art));image.pixelsPerUnitMultiplier=ppu;
    }
    private static void ShopButton(Button button,bool gold=false)
    {
        var image=button.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+(gold?"ShopGoldButtonTrimmed.asset":"ShopCard.png"));
        image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=4;image.color=Color.white;
        var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=colors.disabledColor=Color.white;
        colors.pressedColor=new Color(.83f,.91f,1);button.colors=colors;
    }
    private static void ImportShopArt()
    {
        foreach(string name in new[]{"ShopCard","ShopHeader","ShopGoldButton"}){
            string artPath=FaithfulPath+name+".png";AssetDatabase.ImportAsset(artPath,ImportAssetOptions.ForceSynchronousImport);
            var artImporter=(TextureImporter)AssetImporter.GetAtPath(artPath);artImporter.textureType=TextureImporterType.Sprite;artImporter.spriteImportMode=SpriteImportMode.Single;
            artImporter.alphaIsTransparency=true;artImporter.mipmapEnabled=false;artImporter.npotScale=TextureImporterNPOTScale.None;
            artImporter.textureCompression=TextureImporterCompression.Uncompressed;artImporter.maxTextureSize=4096;artImporter.isReadable=name!="ShopCard";
            artImporter.spriteBorder=name=="ShopCard"?new Vector4(160,160,160,160):Vector4.zero;artImporter.SaveAndReimport();
            if(name!="ShopCard"&&AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+name+"Trimmed.asset")==null){
                var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(artPath);var pixels=tex.GetPixels32();int x0=tex.width,y0=tex.height,x1=0,y1=0;
                for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(pixels[y*tex.width+x].a>24){x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
                var border=name=="ShopGoldButton"?new Vector4(130,130,130,130):Vector4.zero;
                var sprite=Sprite.Create(tex,new UnityEngine.Rect(x0,y0,x1-x0+1,y1-y0+1),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);sprite.name=name+"Trimmed";AssetDatabase.CreateAsset(sprite,FaithfulPath+name+"Trimmed.asset");
            }
        }
        string path=FaithfulPath+"UpgradeIcons.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        string[] names={"SteelToe","CushionInsole","SpringCoil","RocketCharm","LateralSneaker","BossBreaker","CoinPouch","HealingInsert","SahurShield","BomberCharm"};
        for(int i=0;i<names.Length;i++){
            string spritePath=FaithfulPath+"Shop"+names[i]+".asset";
            if(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath)!=null)continue;
            float w=texture.width/5f,h=texture.height/2f;
            var sprite=Sprite.Create(texture,new UnityEngine.Rect((i%5)*w,i<5?h:0,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprite.name="Shop"+names[i];AssetDatabase.CreateAsset(sprite,spritePath);
        }
        // Vector-like UI rings are deterministic, antialiased native textures, not decorative art.
        foreach(bool filled in new[]{false,true}){
            string ringPath=FaithfulPath+(filled?"ShopPipFilled":"ShopPipEmpty")+".asset";
            if(AssetDatabase.LoadAssetAtPath<Sprite>(ringPath)!=null)continue;
            var tex=new Texture2D(64,64,TextureFormat.RGBA32,false){name=filled?"Filled rank":"Empty rank",filterMode=FilterMode.Bilinear};
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){
                float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));
                Color c=filled?(d>26?Ink:Color.Lerp(new Color(.02f,.45f,.75f),new Color(.10f,.92f,1),y/64f)):(d>26?new Color(.68f,.59f,.45f):Cream);
                c.a=Mathf.Clamp01(30.5f-d);tex.SetPixel(x,y,c);
            }
            tex.Apply();var sprite=Sprite.Create(tex,new UnityEngine.Rect(0,0,64,64),new Vector2(.5f,.5f),100);sprite.name=filled?"ShopPipFilled":"ShopPipEmpty";
            AssetDatabase.CreateAsset(sprite,ringPath);AssetDatabase.AddObjectToAsset(tex,sprite);
        }
        AssetDatabase.SaveAssets();
    }
    private static Sprite[] FittedSkinIcons()
    {
        return Directory.GetFiles(CoastalPath,"skin_*.png").OrderBy(p=>p).Select(path=>{
            string key=Path.GetFileNameWithoutExtension(path),target=FaithfulPath+"ShopIcon_"+key+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(target);if(existing!=null)return existing;
            var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(path));
            try{
                int x0=source.width,y0=source.height,x1=0,y1=0;var pixels=source.GetPixels32();
                for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)if(pixels[y*source.width+x].a>24){x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\','/'));
                float sx=texture.width/(float)source.width,sy=texture.height/(float)source.height;
                // A native sub-sprite removes accidental transparent margins without repainting the icon.
                var sprite=Sprite.Create(texture,new UnityEngine.Rect(x0*sx,y0*sy,(x1-x0+1)*sx,(y1-y0+1)*sy),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                sprite.name="ShopIcon_"+key;AssetDatabase.CreateAsset(sprite,target);return sprite;
            }finally{UnityEngine.Object.DestroyImmediate(source);}
        }).ToArray();
    }
    private static void FaithfulShops(CanvasScript canvas)
    {
        var font=RoundedFont();RoundedMaterial(font,"Coastal_RoundedInk",.014f,false);RoundedMaterial(font,"Coastal_ShopWhite",.055f,true);
        FaithfulCosmeticShop(canvas.GetComponentInChildren<CosmeticShopUI>(true));
        FaithfulUpgradeShop(canvas.transform.Find("UI/Upgrade2"));
        // Warm all authored Korean labels so the first shop open does not show fallback glyphs.
        string labels=string.Concat(canvas.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.font==font).Select(t=>t.text))+"현재 착용 중미리보기장착보유구매다음최대강화챕터해금후이용가능공격력체력레벨";
        font.TryAddCharacters(labels,out _);EditorUtility.SetDirty(font);
        foreach(var component in canvas.GetComponentsInChildren<Component>(true))if(component!=null){
            EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }
    private static void FaithfulCosmeticShop(CosmeticShopUI shop)
    {
        var root=shop.transform;shop.referencePresentation=true;shop.lightCards=true;shop.preview.referencePresentation=true;
        shop.fittedCardIcons=FittedSkinIcons();
        foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))ShopType(t,38);
        Move(root,"HarborTitleSign",.035f,.930f,.965f,.989f);var header=root.Find("HarborTitleSign").GetComponent<Image>();header.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopHeaderTrimmed.asset");header.type=UnityEngine.UI.Image.Type.Simple;header.color=Color.white;
        Move(root,"Title",.25f,.938f,.78f,.982f);ShopType(root.Find("Title").GetComponent<TMP_Text>(),78,true);
        root.Find("CoastalAnchor").gameObject.SetActive(false);
        Move(root,"Back",.854f,.936f,.953f,.984f);ShopSurface(root.Find("Back").GetComponent<Image>(),"SquareButton",.85f);
        root.Find("Back").GetComponent<Image>().type=UnityEngine.UI.Image.Type.Simple;
        foreach(var label in root.Find("Back").GetComponentsInChildren<TMP_Text>(true)){label.text="X";ShopType(label,76,true);}
        Move(root,"CoinWallet",.20f,.884f,.49f,.927f);Move(root,"GemWallet",.505f,.884f,.795f,.927f);
        foreach(string name in new[]{"CoinWallet","GemWallet"})ShopSurface(root.Find(name).GetComponent<Image>(),"Wallet",1.65f);
        Move(root,"CoinIcon",.215f,.891f,.275f,.920f);Move(root,"JewelIcon",.522f,.891f,.587f,.920f);
        SetRect(shop.coinBalance.rectTransform,.28f,.889f,.47f,.921f);SetRect(shop.jewelBalance.rectTransform,.59f,.889f,.775f,.921f);
        ShopType(shop.coinBalance,58);ShopType(shop.jewelBalance,58);
        foreach(var value in new[]{shop.coinBalance,shop.jewelBalance}){value.enableAutoSizing=true;value.fontSizeMin=38;value.fontSizeMax=58;}
        Move(root,"CoastalNameplate",.350f,.827f,.650f,.865f);ShopSurface(root.Find("CoastalNameplate").GetComponent<Image>(),"Wallet",1.9f);
        SetRect(shop.selectionName.rectTransform,.373f,.834f,.627f,.858f);ShopType(shop.selectionName,40);
        Move(root,"PreviewArea",.015f,.548f,.985f,.827f);
        Move(root,"RotateHint",.275f,.552f,.725f,.58f);ShopType(root.Find("RotateHint").GetComponent<TMP_Text>(),40,true);
        ShopImage(root,"RotationLeft",Art("Left"),.23f,.553f,.28f,.58f,true);
        ShopImage(root,"RotationRight",Art("Right"),.72f,.553f,.77f,.58f,true);
        Move(root,"ResetPreview",.845f,.555f,.95f,.581f);ShopButton(root.Find("ResetPreview").GetComponent<Button>());
        foreach(var t in root.Find("ResetPreview").GetComponentsInChildren<TMP_Text>(true))ShopType(t,29);
        Move(root,"CoastalCatalogPanel",.006f,.010f,.994f,.551f);ShopCardSurface(root.Find("CoastalCatalogPanel").GetComponent<Image>(),3.1f);
        shop.activeTabSprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopGoldButtonTrimmed.asset");shop.inactiveTabSprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopCard.png");
        Button[] tabs={shop.skinTab,shop.shoesTab,shop.hatTab};
        for(int i=0;i<3;i++){
            SetRect((RectTransform)tabs[i].transform,.024f+i*.319f,.497f,.337f+i*.319f,.545f);ShopButton(tabs[i],i==0);
            var label=tabs[i].GetComponentInChildren<TMP_Text>();SetRect(label.rectTransform,.04f,.12f,.96f,.88f);ShopType(label,59);
        }
        Move(root,"EquipmentViewport",.024f,.101f,.973f,.493f);Move(root,"EquipmentScrollbar",.977f,.115f,.985f,.481f);
        var grid=shop.itemsRoot.GetComponent<EquipmentCardGrid>();grid.cardHeight=445;grid.grid.spacing=new Vector2(16,16);grid.grid.padding=new RectOffset(4,4,4,10);
        var card=shop.itemTemplate.transform;ShopButton(card.GetComponent<Button>());ShopCardSurface(card.GetComponent<Image>(),4f);
        Move(card,"Icon",.07f,.37f,.93f,.96f);Move(card,"Name",.055f,.275f,.945f,.408f);
        Move(card,"Effect",.055f,.190f,.945f,.285f);Move(card,"State",.35f,.065f,.79f,.185f);Move(card,"CurrencyIcon",.23f,.069f,.355f,.169f);
        ShopType(card.Find("Name").GetComponent<TMP_Text>(),49);ShopType(card.Find("Effect").GetComponent<TMP_Text>(),38);
        ShopType(card.Find("State").GetComponent<TMP_Text>(),49);
        card.Find("Effect").GetComponent<TMP_Text>().color=new Color(.31f,.32f,.34f);
        var band=ShopImage(card,"PriceBand",MobileUIArt.Rounded,.11f,.065f,.89f,.185f);band.type=UnityEngine.UI.Image.Type.Sliced;band.color=new Color(.83f,.79f,.69f,.7f);band.transform.SetSiblingIndex(1);
        var selection=card.Find("Selected").GetComponent<Image>();ShopSurface(selection,"SelectionFrame",.36f);selection.color=new Color(.03f,.74f,.85f);selection.raycastTarget=false;
        var check=ShopImage(card,"EquippedCheck",AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopPipFilled.asset"),.79f,.795f,.945f,.95f,true);
        var tick=ShopImage(check.transform,"Tick",AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),.20f,.20f,.80f,.80f,true);tick.color=Color.white;
        Move(root,"Footer",.022f,.017f,.978f,.100f);root.Find("Footer").GetComponent<Image>().color=Color.clear;
        SetRect(shop.actionButton.GetComponent<RectTransform>(),.046f,.029f,.954f,.090f);ShopButton(shop.actionButton);
        ShopType(shop.actionText,68);SetRect(shop.actionText.rectTransform,.13f,.12f,.87f,.88f);
        var disabled=ShopImage(shop.actionButton.transform,"DisabledFill",MobileUIArt.Rounded,.045f,.16f,.955f,.84f);disabled.color=new Color(.64f,.69f,.76f,.83f);disabled.type=UnityEngine.UI.Image.Type.Sliced;disabled.transform.SetAsFirstSibling();
        SetRect(shop.statusText.rectTransform,.12f,.091f,.88f,.108f);ShopType(shop.statusText,27);
        grid.RefreshLayout();
    }
    private static void FaithfulUpgradeShop(Transform window)
    {
        foreach(var t in window.GetComponentsInChildren<TMP_Text>(true))ShopType(t,38);
        ShopSurface(window.Find("CoastalFrame").GetComponent<Image>(),"Frame",1.4f);
        Move(window,"TitleSign",.020f,.923f,.98f,.990f);ShopCardSurface(window.Find("TitleSign").GetComponent<Image>(),3.5f);
        Move(window,"Title",.07f,.938f,.53f,.978f);ShopType(window.Find("Title").GetComponent<TMP_Text>(),67,false,true);
        Move(window,"Coins",.54f,.938f,.833f,.978f);ShopSurface(window.Find("Coins").GetComponent<Image>(),"Wallet",1.7f);
        var balance=window.Find("Coins/Balance").GetComponent<TMP_Text>();ShopType(balance,53);balance.enableAutoSizing=true;balance.fontSizeMin=32;balance.fontSizeMax=53;
        Move(window,"Back",.858f,.936f,.963f,.983f);ShopSurface(window.Find("Back").GetComponent<Image>(),"SquareButton",.85f);
        window.Find("Back").GetComponent<Image>().type=UnityEngine.UI.Image.Type.Simple;
        foreach(var t in window.Find("Back").GetComponentsInChildren<TMP_Text>(true)){t.text="X";ShopType(t,74,true);}
        var tabs=window.GetComponent<HarborUpgradeTabs>();
        tabs.activeSprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopGoldButtonTrimmed.asset");tabs.inactiveSprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopCard.png");
        Move(window,"PermanentTab",.025f,.861f,.497f,.922f);Move(window,"ChapterTab",.503f,.861f,.975f,.922f);
        foreach(var tab in new[]{tabs.permanentTab,tabs.chapterTab}){ShopButton(tab);foreach(var t in tab.GetComponentsInChildren<TMP_Text>(true))ShopType(t,50);}
        Move(window,"Merchant",.027f,.686f,.973f,.859f);
        var summary=window.Find("CurrentStats");Move(window,"CurrentStats",.023f,.582f,.977f,.691f);ShopCardSurface(summary.GetComponent<Image>(),3.5f);
        ShopSurface(summary.Find("NavyHeader").GetComponent<Image>(),"NavyPanel",1.7f);Move(summary,"NavyHeader",0,.62f,1,1);
        Move(summary,"Heading",.15f,.677f,.85f,.955f);ShopType(summary.Find("Heading").GetComponent<TMP_Text>(),47,true);
        foreach(string name in new[]{"AttackLabel","HealthLabel"})ShopType(summary.Find(name).GetComponent<TMP_Text>(),39);
        foreach(string name in new[]{"Attack","Health"}){var t=summary.Find(name).GetComponent<TMP_Text>();ShopType(t,64);t.enableAutoSizing=true;t.fontSizeMin=44;t.fontSizeMax=64;}
        ShopImage(summary,"Divider",FlatFillSprite(),.5f,.18f,.5015f,.47f).color=new Color(.47f,.46f,.44f);
        Move(window,"UpgradeViewport",.025f,.025f,.975f,.578f);
        var viewport=window.Find("UpgradeViewport");var grid=viewport.GetComponentInChildren<EquipmentCardGrid>(true);grid.cardHeight=240;grid.grid.spacing=new Vector2(0,12);grid.grid.padding=new RectOffset(2,2,4,16);
        string[] icons={"SteelToe","CushionInsole","SpringCoil","RocketCharm","BossBreaker","CoinPouch","HealingInsert","SahurShield","BomberCharm","LateralSneaker"};
        foreach(var card in viewport.GetComponentsInChildren<UpgradeUI>(true)){
            ShopSurface(card.GetComponent<Image>(),"RowPanel",.7f);var up=card.transform.Find("Up");
            Move(up,"Icon",.025f,.13f,.195f,.89f);var icon=up.Find("Icon").GetComponent<Image>();icon.sprite=OriginalUpgradeArtwork.ForUpgrade(card.UpgradeId)??AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"Shop"+icons[card.UpgradeId-1]+".asset");
            var data=new SerializedObject(card);data.FindProperty("iconOverride").objectReferenceValue=icon.sprite;data.ApplyModifiedPropertiesWithoutUndo();
            Move(up,"Name",.217f,.62f,.715f,.92f);ShopType(up.Find("Name").GetComponent<TMP_Text>(),51,false,true);
            Move(up,"Level",.22f,.38f,.71f,.63f);ShopType(up.Find("Level").GetComponent<TMP_Text>(),35,false,true);
            var line=up.Find("EffectLine") as RectTransform;if(line==null)line=Rect(up,"EffectLine",.22f,.10f,.72f,.36f);
            var lineLayout=GetOrAdd<HorizontalLayoutGroup>(line.gameObject);lineLayout.childAlignment=TextAnchor.MiddleLeft;lineLayout.spacing=7;lineLayout.childControlWidth=true;lineLayout.childControlHeight=true;lineLayout.childForceExpandWidth=false;lineLayout.childForceExpandHeight=false;
            foreach(string name in new[]{"Effect","CurrentValue","Arrow","NextValue"}){
                var child=up.Find(name)??line.Find(name);child.SetParent(line,false);var label=child.GetComponent<TMP_Text>();ShopType(label,32,false,true);
                if(name=="Arrow")label.text="→";
            }
            var buy=card.transform.Find("Down").GetComponent<Button>();SetRect((RectTransform)buy.transform,.735f,.16f,.972f,.84f);ShopButton(buy,true);
            foreach(var t in buy.GetComponentsInChildren<TMP_Text>(true))ShopType(t,t.name=="Price"?46:41);
            Move(buy.transform,"Currency",.085f,.13f,.31f,.45f);Move(buy.transform,"Price",.34f,.09f,.92f,.48f);
        }
        grid.RefreshLayout();
        var chapters=window.Find("ChapterUpgrades");Move(window,"ChapterUpgrades",.025f,.025f,.975f,.578f);
        var explanation=chapters.Find("Explanation").GetComponent<TMP_Text>();explanation.text="챕터 해금 후 이용 가능 / 챕터마다 최대 5회\n매회 공격력 / 체력 +5% / 챕터별 최대 +25%";ShopType(explanation,33);SetRect(explanation.rectTransform,.02f,.908f,.98f,.998f);
        Move(chapters,"Viewport",0,0,1,.904f);
        var layout=chapters.GetComponentInChildren<EquipmentCardGrid>(true);layout.cardHeight=362;layout.grid.spacing=new Vector2(0,15);layout.grid.padding=new RectOffset(2,2,4,14);
        foreach(var card in chapters.GetComponentsInChildren<ChapterUpgradeCardUI>(true)){
            var root=card.transform;card.referencePresentation=true;ShopSurface(card.GetComponent<Image>(),"RowPanel",.65f);
            var shade=ShopImage(root,"LockedShade",MobileUIArt.Rounded,.015f,.035f,.985f,.965f);shade.color=new Color(.29f,.33f,.40f,.31f);shade.type=UnityEngine.UI.Image.Type.Sliced;shade.transform.SetSiblingIndex(0);card.lockedShade=shade;
            Move(root,"ChapterArt",.03f,.095f,.255f,.905f);card.chapterArtwork=root.Find("ChapterArt").GetComponent<Image>();
            Move(root,"ChapterNumber",.28f,.765f,.665f,.91f);ShopType(card.chapterLabel,34,false,true);
            Move(root,"Title",.28f,.565f,.69f,.785f);ShopType(card.title,49,false,true);
            Move(root,"Effects",.28f,.11f,.69f,.48f);ShopType(card.effects,35,false,true);
            card.effects.textWrappingMode=TextWrappingModes.Normal;
            Move(root,"Rank",.704f,.49f,.966f,.65f);ShopType(card.rankLabel,35);
            card.emptyPip=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopPipEmpty.asset");card.filledPip=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopPipFilled.asset");
            for(int i=0;i<5;i++){SetRect(card.rankPips[i].rectTransform,.718f+i*.050f,.69f,.760f+i*.050f,.825f);card.rankPips[i].sprite=card.emptyPip;card.rankPips[i].preserveAspect=true;card.rankPips[i].color=Color.white;}
            Move(root,"Buy",.71f,.105f,.966f,.465f);ShopButton(card.buyButton,true);
            SetRect(card.actionLabel.rectTransform,.08f,.52f,.92f,.92f);ShopType(card.actionLabel,40);
            Move(card.buyButton.transform,"Coin",.075f,.10f,.30f,.46f);SetRect(card.priceLabel.rectTransform,.31f,.075f,.96f,.49f);ShopType(card.priceLabel,44);card.priceLabel.enableAutoSizing=true;card.priceLabel.fontSizeMin=31;card.priceLabel.fontSizeMax=44;
            Move(root,"Locked",.638f,.66f,.70f,.84f);
            card.lockedText=ShopLabel(root,"LockedExplanation","챕터 해금 후 이용 가능",36,.30f,.14f,.94f,.40f);
        }
        layout.RefreshLayout();tabs.ShowPermanent();
    }
    private static void ShopCardSurface(Image image,float ppu)
    {
        image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopCard.png");image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=ppu;image.color=Color.white;
    }
    public static object ApplyFaithfulShopsAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        if(!EditorSceneManager.SaveOpenScenes())throw new InvalidOperationException("Save scene state first");
        var setup=EditorSceneManager.GetSceneManagerSetup();ImportShopArt();
        try{foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
            var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<CanvasScript>();
            FaithfulShops(canvas);AlignPresentationText(canvas);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }}finally{AssetDatabase.SaveAssets();EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return "Faithful cosmetic, permanent and chapter shops saved in three chapters";
    }
}
