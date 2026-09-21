using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BonusTalismanTests
{
    private readonly List<GameObject> owned = new();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject Make(string name) { var go=new GameObject(name);owned.Add(go);return go; }
    [TearDown] public void Cleanup()
    {
        foreach(var fx in UnityEngine.Object.FindObjectsByType<BonusTalismanPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(fx.gameObject);
        foreach(var go in owned)if(go!=null)UnityEngine.Object.DestroyImmediate(go);
        owned.Clear();
    }

    [Test]
    public void PolishedResourcesHaveRaisedEmblemsAndNonReflectiveCaption()
    {
        var visual=Resources.Load<BonusTalismanVisual>(BonusTalismanPresentation.ResourcePath);
        Assert.That(visual.emblems.Length,Is.EqualTo(5));
        Assert.That(visual.emblems.All(e=>e.mesh!=null&&e.materials.All(m=>m!=null)),Is.True);
        Assert.That(visual.idleGlints.Length,Is.EqualTo(4));
        var caption=Resources.Load<Material>("BonusTalisman/Polished/CaptionNavy");
        Assert.That(caption.GetFloat("_SpecularEnabled"),Is.Zero);
        Assert.That(caption.IsKeywordEnabled("DR_SPECULAR_ON"),Is.False);
        Assert.That(Resources.Load<Material>("BonusTalisman/Polished/TrailCore").GetFloat("_ZTest"),Is.EqualTo(8));
    }

    [Test]
    public void LegacyBonusWithoutTextReferenceStillShowsItsRolledValue()
    {
        var wall=Make("Legacy label").AddComponent<WallScript>();wall.buffType=BuffType.attPer_normal;
        typeof(WallScript).GetField("displayBonusValue",Private).SetValue(wall,14f);
        typeof(WallScript).GetField("bonusValueType",Private).SetValue(wall,BonusValueType.Percent);
        Assert.That(BonusTalismanPresentation.LabelFor(wall),Does.Contain("+14%"));
    }

    [Test]
    public void TorsoGlowIsLoadableAsASprite()
    {
        Assert.That(Resources.Load<Sprite>("BonusTalisman/SoftGlow"),Is.Not.Null);
    }

    [Test]
    public void BonusRerollsUseTheOriginalEnhancementArtworkForEveryMappedType()
    {
        var go=UnityEngine.Object.Instantiate(Resources.Load<BonusTalismanVisual>(BonusTalismanPresentation.ResourcePath)).gameObject;owned.Add(go);
        var visual=go.GetComponent<BonusTalismanVisual>();Assert.That(visual.enhancementIcons,Is.Not.Null);
        foreach(BuffType type in Enum.GetValues(typeof(BuffType)))
        {
            string key=BonusAltarRules.ResolveIconResourceName(type);if(key==null)continue;
            var expected=visual.enhancementIcons.GetSpriteOrDefault(key);Assert.That(expected,Is.Not.Null,key);
            visual.SetContent(Resources.Load<Sprite>("WallBonusIcons/"+key),"보너스 +1",Color.white);
            Assert.That(visual.icon.sprite==expected,Is.True,type.ToString());Assert.That(visual.icon.enabled,Is.True);
            Assert.That(visual.emblemRenderer.enabled,Is.False,"The earlier replacement emblem must not cover the restored original icon.");
        }
    }

    [Test]
    public void EveryPositiveBonusTypeHasAnIconAndUsesTheSameVisual()
    {
        var go=Make("All bonus types");go.AddComponent<BoxCollider>();var wall=go.AddComponent<WallScript>();
        foreach(BuffType type in Enum.GetValues(typeof(BuffType)))
        {
            wall.buffType=type;
            var presentation=BonusTalismanPresentation.Refresh(wall);
            Assert.That(presentation.Visual,Is.Not.Null,type.ToString());
            Assert.That(presentation.Visual.icon.sprite,Is.Not.Null,type.ToString());
            Assert.That(presentation.Visual.label.text,Is.Not.Empty,type.ToString());
        }
        Assert.That(go.GetComponentsInChildren<BonusTalismanVisual>(true).Length,Is.EqualTo(1),"Rerolls must reuse the visual.");
    }

    [Test]
    public void NonuniformLegacyGfxDoesNotStretchTheTalisman()
    {
        var root=Make("Legacy scaled root");root.transform.localScale=new Vector3(2.25f,2.9f,2.9f);root.AddComponent<BonusWallLifetimeRoot>();
        var child=Make("GFX");child.transform.SetParent(root.transform,false);child.transform.localScale=Vector3.one*25;child.transform.localRotation=Quaternion.Euler(270,0,0);
        var wall=child.AddComponent<WallScript>();var view=BonusTalismanPresentation.Refresh(wall).Visual;
        Assert.That(view.transform.parent,Is.EqualTo(root.transform));
        Assert.That(Vector3.Distance(view.transform.lossyScale,Vector3.one),Is.LessThan(.01f));
        Assert.That(view.transform.position.y,Is.EqualTo(root.transform.position.y+2.2f).Within(.01f));
    }

    [Test]
    public void NerfWallsRetainTheirExistingPresentation()
    {
        var go=Make("Nerf");var wall=go.AddComponent<WallScript>();wall.wallType=WallType.NerfWall;
        Assert.That(BonusTalismanPresentation.Refresh(wall),Is.Null);
        Assert.That(go.GetComponent<BonusTalismanPresentation>(),Is.Null);
    }

    [Test]
    public void RealPickupAppliesRewardOnceAndEffectSurvivesOriginalDeactivation()
    {
        var playerGo=Make("Player");playerGo.tag="Player";var collider=playerGo.AddComponent<BoxCollider>();
        var player=playerGo.AddComponent<PlayerScript>();player.currentHealth=100;player.lastWallTouchTime=Time.time-3;
        var go=Make("Runtime bonus");go.AddComponent<BoxCollider>();go.AddComponent<RuntimeBonusWall>();
        var wall=go.AddComponent<WallScript>();wall.buffType=BuffType.hp_normal;
        typeof(WallScript).GetField("playerScript",Private).SetValue(wall,player);
        typeof(WallScript).GetField("bonusValue",Private).SetValue(wall,10f);
        var presentation=BonusTalismanPresentation.Refresh(wall);
        typeof(WallScript).GetMethod("OnTriggerEnter",Private).Invoke(wall,new object[]{collider});
        Assert.That(player.currentHealth,Is.EqualTo(110));
        Assert.That(go.activeSelf,Is.False);
        var effects=UnityEngine.Object.FindObjectsByType<BonusTalismanPickup>(FindObjectsSortMode.None);
        Assert.That(effects.Length,Is.EqualTo(1));
        Assert.That(effects[0].transform.parent,Is.Null);
        Assert.That(presentation.PlayPickup(player),Is.False,"One transient effect per claimed bonus.");
        Assert.That(player.currentHealth,Is.EqualTo(110));
        effects[0].Sample(.3f);effects[0].Sample(BonusTalismanPickup.Duration+.02f);
        var art=effects[0].GetComponent<BonusTalismanVisual>();
        Assert.That(art.paper.localScale,Is.EqualTo(Vector3.zero));
        Assert.That(art.icon.color.a,Is.Zero);
        Assert.That(art.GetComponentInChildren<LineRenderer>().enabled,Is.False);
        Assert.That(art.transform.Find("TorsoPulse").GetComponent<SpriteRenderer>().color.a,Is.EqualTo(0).Within(.0001));
        go.SetActive(true);
        // EditMode objects do not dispatch ordinary MonoBehaviour lifecycle callbacks.
        typeof(BonusTalismanPresentation).GetMethod("OnEnable",Private).Invoke(presentation,null);
        Assert.That(presentation.PlayPickup(player),Is.True,"Re-enabled pickup resets its claimed guard.");
    }

    [Test]
    public void InvalidAuthoredRollHidesTalismanAndDisablesTrigger()
    {
        var go=Make("Invalid authored bonus");var trigger=go.AddComponent<BoxCollider>();
        var wall=go.AddComponent<WallScript>();go.AddComponent<AuthoredBonusWall>();
        var presentation=BonusTalismanPresentation.Refresh(wall);
        wall.SetWallSprite();
        Assert.That(presentation.Visual.gameObject.activeSelf,Is.False);
        Assert.That(trigger.enabled,Is.False);
    }

    [Test]
    public void EverySavedBonusPrefabIsCoveredAndVisualHasNoPhysicsOrRewardCode()
    {
        int count=0;
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ShooterSurvival/Prefabs/Walls"}).Select(AssetDatabase.GUIDToAssetPath))
            foreach(var wall in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<WallScript>(true))
            {
                if(wall.wallType!=WallType.BuffWall)continue;
                var presentation=wall.GetComponent<BonusTalismanPresentation>();
                Assert.That(presentation,Is.Not.Null,path);
                Assert.That(presentation.Visual,Is.Not.Null,path);
                count++;
            }
        Assert.That(count,Is.GreaterThanOrEqualTo(21));
        var art=Resources.Load<BonusTalismanVisual>(BonusTalismanPresentation.ResourcePath);
        Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(art.GetComponentsInChildren<WallScript>(true),Is.Empty);
    }
}
