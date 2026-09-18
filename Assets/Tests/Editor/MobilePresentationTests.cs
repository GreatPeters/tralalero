#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class MobilePresentationTests
{
    [Test]
    public void EquipmentGridRepairsHeightAfterAnInactiveLayoutReset()
    {
        var root=new GameObject("Isolated grid",typeof(RectTransform),typeof(GridLayoutGroup),typeof(EquipmentCardGrid));
        root.SetActive(false);
        try
        {
            var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1000,0);
            var grid=root.GetComponent<GridLayoutGroup>();grid.constraintCount=2;grid.spacing=new Vector2(22,22);grid.padding=new RectOffset(8,8,8,22);
            for(int i=0;i<10;i++){var card=new GameObject("Card",typeof(RectTransform));card.transform.SetParent(rect,false);}
            var layout=root.GetComponent<EquipmentCardGrid>();layout.grid=grid;layout.cardHeight=360;layout.RefreshLayout();
            float expected=5*360+4*22+30;
            Assert.That(rect.rect.height,Is.EqualTo(expected).Within(.01f));
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,0);
            layout.RefreshLayout();
            Assert.That(rect.rect.height,Is.EqualTo(expected).Within(.01f));
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void InactiveModalRestoresItsOwnCanvasOrderOnEnable()
    {
        var root=new GameObject("Isolated canvas",typeof(RectTransform),typeof(Canvas));
        try
        {
            var child=new GameObject("Modal",typeof(RectTransform),typeof(Canvas),typeof(MobileUILayer));child.transform.SetParent(root.transform,false);
            child.GetComponent<MobileUILayer>().Configure(180);child.SetActive(false);
            child.GetComponent<Canvas>().overrideSorting=false;child.SetActive(true);
            Assert.That(child.GetComponent<Canvas>().overrideSorting,Is.True);
            Assert.That(child.GetComponent<Canvas>().sortingOrder,Is.EqualTo(180));
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void EverySoundEventHasACompactMonoClipAndImportantCuesOutrankFire()
    {
        Assert.That(GameAudioService.ClipNames.Length,Is.EqualTo(Enum.GetValues(typeof(GameSound)).Length));
        foreach(string name in GameAudioService.ClipNames)
        {
            var clip=Resources.Load<AudioClip>("Audio/Mobile/"+name);
            Assert.That(clip,Is.Not.Null,name);Assert.That(clip.samples,Is.GreaterThan(0),name);
            Assert.That(clip.channels,Is.EqualTo(1),name);Assert.That(clip.length,Is.LessThan(3),name);
        }
        Assert.That(GameAudioService.VoiceLimit,Is.LessThanOrEqualTo(16));
        Assert.That(GameAudioService.Cooldown(GameSound.Shot),Is.GreaterThanOrEqualTo(.06f));
        Assert.That(GameAudioService.Priority(GameSound.Warning),Is.GreaterThan(GameAudioService.Priority(GameSound.Shot)));
        foreach(string name in new[]{"music","harbor","traffic"})Assert.That(Resources.Load<AudioClip>("Audio/Mobile/"+name),Is.Not.Null,name);
    }

    [Test]
    public void NewCoinHasAReadableFaceWithoutTextureOrTransparentOverdraw()
    {
        var mesh=Resources.Load<Mesh>("VFX/CoinTokenMesh");var material=Resources.Load<Material>("VFX/CoinToken");
        Assert.That(mesh,Is.Not.Null);Assert.That(material,Is.Not.Null);
        Assert.That(mesh.bounds.size.x,Is.GreaterThan(1));Assert.That(mesh.bounds.size.y,Is.GreaterThan(1));
        Assert.That(mesh.triangles.Length/3,Is.LessThanOrEqualTo(700));Assert.That(mesh.subMeshCount,Is.EqualTo(1));
        Assert.That(material.GetTexturePropertyNames(),Is.Empty);Assert.That(material.renderQueue,Is.LessThan(2500));
    }

    [Test]
    public void ChosenFontCoversCoreKoreanCurrencyAndUpgradeLabels()
    {
        var font=GameUIFont.Load();Assert.That(font,Is.Not.Null);Assert.That(font.name,Is.EqualTo("KERISKEDU_B SDF"));
        foreach(char character in "현재공격력최대체력좌우이동속도늘리기강화상점꾸미기이야기설정레벨뒤로0123456789+%/,.".Distinct())
            Assert.That(font.HasCharacter(character),Is.True,character.ToString());
    }
}
#endif
