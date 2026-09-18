#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Sr18PresentationRevisionTests
{
    private sealed class Store : ICosmeticStorage
    {
        public readonly HashSet<string> owned=new();public readonly Dictionary<CosmeticSlot,string> equipped=new();
        public bool IsOwned(string id)=>owned.Contains(id);
        public void SetOwned(string id,bool value){if(value)owned.Add(id);else owned.Remove(id);}
        public string GetEquipped(CosmeticSlot slot)=>equipped.TryGetValue(slot,out var id)?id:"";
        public void SetEquipped(CosmeticSlot slot,string id)=>equipped[slot]=id;
        public void Save(){}
    }
    private sealed class Wallet : ICosmeticWallet
    {
        public int coins=200,jewels=100,calls;
        public int Balance(PriceType type)=>type==PriceType.Jewel?jewels:coins;
        public bool Spend(PriceType type,int amount){if(Balance(type)<amount)return false;calls++;if(type==PriceType.Jewel)jewels-=amount;else coins-=amount;return true;}
    }
    private static CosmeticItem[] Catalog()=>new[]{
        new CosmeticItem{id="skin",name="Skin",slot=CosmeticSlot.Skin,visualKey="skin",isDefault=true},
        new CosmeticItem{id="shoe",name="Shoe",slot=CosmeticSlot.Shoes,visualKey="shoe",isDefault=true},
        new CosmeticItem{id="hat",name="Hat",slot=CosmeticSlot.Hat,visualKey="hat",isDefault=true},
        new CosmeticItem{id="armor",name="Armour",slot=CosmeticSlot.Skin,visualKey="armor",currency=PriceType.Jewel,price=30,effectStat="HP",effectValue=12,effectValueType=ValueType.Percent},
        new CosmeticItem{id="steel",name="Steel",slot=CosmeticSlot.Shoes,visualKey="steel",currency=PriceType.Jewel,price=40,effectStat="ATT",effectValue=8,effectValueType=ValueType.Percent}};

    [Test] public void EquipmentPurchase_UsesJewelsAndNeverChargesForReequip()
    {
        var wallet=new Wallet();var inventory=new CosmeticInventory(Catalog(),new Store(),wallet);
        Assert.That(inventory.PurchaseOrEquip("steel"),Is.EqualTo(CosmeticPurchaseResult.Purchased));
        Assert.That(inventory.PurchaseOrEquip("steel"),Is.EqualTo(CosmeticPurchaseResult.Equipped));
        Assert.That(wallet.coins,Is.EqualTo(200));Assert.That(wallet.jewels,Is.EqualTo(60));Assert.That(wallet.calls,Is.EqualTo(1));
        Assert.That(inventory.PurchaseOrEquip("armor"),Is.EqualTo(CosmeticPurchaseResult.Purchased));
        Assert.That(inventory.Equipped(CosmeticSlot.Shoes).id,Is.EqualTo("steel"));
    }
    [Test] public void EquippedEffects_ReplaceBySlotInsteadOfStackingAcrossRuns()
    {
        var previous=UpgradeStatManager.S;UpgradeStatManager.S=null;
        var go=new GameObject("Equipment effect test");var stats=go.AddComponent<UpgradeStatManager>();
        try
        {
            float initialAttack=stats.GetPercentStat(UpgradeStatManager.UpgradeType.ATT),initialHp=stats.GetPercentStat(UpgradeStatManager.UpgradeType.HP);
            var inventory=new CosmeticInventory(Catalog(),new Store(),new Wallet());
            EquipmentRunEffects.Apply(inventory,stats);
            Assert.That(stats.GetPercentStat(UpgradeStatManager.UpgradeType.ATT),Is.EqualTo(initialAttack));
            inventory.PurchaseOrEquip("steel");inventory.PurchaseOrEquip("armor");
            EquipmentRunEffects.Apply(inventory,stats);EquipmentRunEffects.Apply(inventory,stats);
            Assert.That(stats.GetPercentStat(UpgradeStatManager.UpgradeType.ATT),Is.EqualTo(initialAttack+8));
            Assert.That(stats.GetPercentStat(UpgradeStatManager.UpgradeType.HP),Is.EqualTo(initialHp+12));
            inventory.PurchaseOrEquip("shoe");EquipmentRunEffects.Apply(inventory,stats);
            Assert.That(stats.GetPercentStat(UpgradeStatManager.UpgradeType.ATT),Is.EqualTo(initialAttack));
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UpgradeStatManager.S=previous;}
    }
    [Test] public void WorkbookEquipment_HasDistinctAttachmentsAndBoundedEffects()
    {
        CosmeticTables.Reload();var rows=CosmeticTables.Rows;var visuals=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        Assert.That(rows.Count,Is.EqualTo(24));
        Assert.That(rows.All(r=>r.currency==PriceType.Jewel),Is.True);
        Assert.That(rows.Where(r=>!r.isDefault).All(r=>r.effectValue>0),Is.True);
        Assert.That(rows.Count(r=>visuals.Find(r.visualKey).accessory!=null),Is.GreaterThanOrEqualTo(12));
        foreach(var entry in visuals.entries.Where(e=>e.accessory!=null))Assert.That(entry.accessory.GetComponentsInChildren<Collider>(true),Is.Empty,entry.key);
    }
    [Test] public void ShoePartition_DoesNotTintTailOrRearHip()
    {
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var mesh=catalog.splitSharkMesh;
        var shoe=mesh.GetTriangles(1).ToHashSet();var vertices=mesh.vertices;int tested=0;
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 p=vertices[i]*450f;
            if(p.z<-.9f||(p.y>.41f&&p.z<-.15f&&p.z>-.62f&&Mathf.Abs(p.x)<.45f))
            {Assert.That(shoe.Contains(i),Is.False,"Body landmark vertex "+i);tested++;}
        }
        Assert.That(tested,Is.GreaterThan(100));Assert.That(shoe.Count,Is.GreaterThan(100));
    }
    [Test] public void ReplacingPreviewAttachments_DoesNotAccessDestroyedDescendants()
    {
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var scene=EditorSceneManager.NewPreviewScene();
        var instance=UnityEngine.Object.Instantiate(catalog.previewModel);SceneManager.MoveGameObjectToScene(instance,scene);instance.transform.localScale=Vector3.one*catalog.previewScale;
        try
        {
            foreach(string skin in new[]{"skin_armor","skin_raider","skin_relic","skin_original"})
                Assert.DoesNotThrow(()=>CosmeticAppearance.Apply(instance.transform,catalog,skin,"shoes_steel","hat_goggles"));
            Assert.That(instance.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="__CosmeticHat"),Is.EqualTo(1));
            Assert.That(instance.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="__CosmeticSkin"),Is.Zero);
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    [Test] public void HeldProjectile_IsSilentAndNonCollidingUntilLaunch()
    {
        var go=new GameObject("Arrow2");var collider=go.AddComponent<SphereCollider>();var trail=go.AddComponent<TrailRenderer>();var projectile=go.AddComponent<SimpleProjectile>();
        try
        {
            projectile.SetFlightActive(false);Assert.That(trail.emitting,Is.False);Assert.That(collider.enabled,Is.False);
            projectile.SetFlightActive(true);Assert.That(trail.emitting,Is.True);Assert.That(trail.enabled,Is.True);Assert.That(collider.enabled,Is.True);
            projectile.SetFlightActive(false);Assert.That(trail.positionCount,Is.Zero);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test] public void BulletDamage_IsALaunchSnapshotEvenAfterPoolDeactivation()
    {
        var go=new GameObject("Damage payload");var bullet=go.AddComponent<BulletScript>();
        try
        {
            bullet.SetDirection(Vector3.forward,null,17f);
            typeof(BulletScript).GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(bullet,null);
            Assert.That(bullet.HasDamagePayload,Is.True);Assert.That(bullet.LaunchDamage,Is.EqualTo(17));
            bullet.SetDirection(Vector3.forward,null,9f);Assert.That(bullet.LaunchDamage,Is.EqualTo(9));
            bullet.SetDirection(Vector3.forward,null);Assert.That(bullet.HasDamagePayload,Is.False);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [TestCase(40f,0f,ValueType.Percent,40f)]
    [TestCase(40f,130f,ValueType.Percent,52f)]
    [TestCase(40f,10f,ValueType.Value,50f)]
    public void BoomBarDamage_UsesItsOwnUpgradeMultiplier(float player,float upgrade,ValueType type,float expected)
    {Assert.That(ExtraHelpBuffScript.CalculateProjectileDamage(player,upgrade,type),Is.EqualTo(expected).Within(.001));}
    [Test] public void PermanentHelperPurchase_DoesNotWeakenTheFreeBonusHelper()
    {
        Assert.That(UpgradeTables.Get(8,1).amount,Is.GreaterThanOrEqualTo(100));
        Assert.That(UpgradeTables.Get(9,1).amount,Is.GreaterThan(100));
    }
    [Test] public void DefeatCannotBeBypassedWithThePublicStartHandler()
    {
        bool oldOver=CanvasScript.isGameOver,oldRunning=TimeManager.isGameRunning;
        var go=new GameObject("Start guard");var controller=go.AddComponent<CanvasScript>();
        try{CanvasScript.isGameOver=true;TimeManager.isGameRunning=false;controller.PlayerPressedStartButton();Assert.That(TimeManager.isGameRunning,Is.False);}
        finally{CanvasScript.isGameOver=oldOver;TimeManager.isGameRunning=oldRunning;UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test] public void DisablingOpening_ReleasesPreparedVideoTexture()
    {
        var go=new GameObject("Opening cleanup");var opening=go.AddComponent<OpeningStoryUI>();
        var texture=new RenderTexture(32,32,0);texture.Create();
        try
        {
            typeof(OpeningStoryUI).GetField("videoTexture",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(opening,texture);
            typeof(OpeningStoryUI).GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(opening,null);
            Assert.That(texture==null,Is.True);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);}
    }
    [Test] public void SharedCarryLayer_PreservesLegLocomotion()
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/JH/Model/Animatior/ForwardEnemyShared/ForwardEnemyShared.controller");
        var layer=controller.layers.Single(l=>l.name==EnemyEventController.CarryLayerName);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm),Is.True);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg),Is.False);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root),Is.False);
        Assert.That(layer.stateMachine.defaultState.motion.name,Is.EqualTo("ForwardEnemy_Idle"));
    }
    [Test] public void SharkController_StartsInIdleAndRetainsWalking()
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/JH/Anim/Shark/Original.controller");
        Assert.That(controller.layers[0].stateMachine.defaultState.name,Is.EqualTo("Idle"));
        Assert.That(controller.layers[0].stateMachine.states.Any(s=>s.state.name=="Walk"),Is.True);
    }
    [Test] public void Grounding_LiftsImportedPoseWithoutMovingGameplayRoot()
    {
        var scene=EditorSceneManager.NewPreviewScene();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_FatMan.prefab");
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        try
        {
            var animator=instance.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
            var grounding=instance.AddComponent<EnemyGroundedPose>();grounding.model=animator;
            typeof(EnemyGroundedPose).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(grounding,null);
            Vector3 origin=instance.transform.position;animator.transform.position-=Vector3.up*2;grounding.Settle();
            Assert.That(instance.transform.position,Is.EqualTo(origin));
            float feet=Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
            Assert.That(feet,Is.GreaterThanOrEqualTo(origin.y+grounding.soleHeight-.001f));
            Vector3 settled=animator.transform.position;grounding.Settle();Assert.That(Vector3.Distance(settled,animator.transform.position),Is.LessThan(.001f));
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    [TestCase("Noryangjin_MapTool_Mode")]
    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    public void StoryAndDefeat_HaveRealUserAndAutomationActions(string name)
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas");var opening=canvas.GetComponentInChildren<OpeningStoryUI>(true);
            Assert.That(opening.comic,Is.Not.Null);Assert.That(opening.movie,Is.Not.Null);Assert.That(opening.movie.length,Is.InRange(20d,25d));
            Assert.That(opening.movie.width,Is.GreaterThanOrEqualTo(576));
            Assert.That((double)opening.movie.width/opening.movie.height,Is.EqualTo(9d/16d).Within(.01d));
            var skip=opening.transform.Find("Skip").GetComponent<UnityEngine.UI.Button>();Assert.That(skip.onClick.GetPersistentTarget(0),Is.SameAs(opening));Assert.That(skip.onClick.GetPersistentMethodName(0),Is.EqualTo("Skip"));
            var defeat=canvas.GetComponent<CanvasScript>().gameOverUI.GetComponent<DefeatPresentation>();Assert.That(defeat,Is.Not.Null);
            defeat.ReturnToAltar();Assert.That(defeat.ContinueRequested,Is.True);
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
#endif
