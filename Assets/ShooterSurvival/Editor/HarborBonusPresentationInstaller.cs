using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class HarborBonusPresentationInstaller
{
    private const string Root="Assets/ShooterSurvival/Resources/HarborRefinement";
    public static object ApplyAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        Directory.CreateDirectory(Root);AssetDatabase.Refresh();
        var loop=BuildRewardEffect("BonusGlow",true);
        var burst=BuildRewardEffect("BonusPickupBurst",false);
        string matPath=Root+"/BonusReadable.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(material==null){material=new Material(HarborRefinementFontBuilder.Font.material);AssetDatabase.CreateAsset(material,matPath);}
        material.CopyPropertiesFromMaterial(HarborRefinementFontBuilder.Font.material);material.SetFloat("_FaceDilate",0);material.SetFloat("_OutlineWidth",.12f);material.SetColor("_OutlineColor",new Color(.015f,.045f,.10f,1));
        material.EnableKeyword("OUTLINE_ON");material.EnableKeyword("UNDERLAY_ON");material.SetColor("_UnderlayColor",new Color(0,0,0,.65f));material.SetFloat("_UnderlayOffsetY",-.1f);material.SetFloat("_UnderlaySoftness",.08f);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
        void Apply(AuthoredBonusWall altar)
        {
            var visual=altar.transform.Find("ChoiceAltarVisual");if(visual==null)return;
            var pedestal=visual.Find("PickupPedestal");if(pedestal==null){pedestal=new GameObject("PickupPedestal").transform;pedestal.SetParent(visual,false);}
            foreach(Transform part in visual.Cast<Transform>().ToArray())
            {
                if(part.name.StartsWith("Pedestal")||part.name.StartsWith("Front")||part.name.StartsWith("Wear")||part.name.StartsWith("Edge"))
                    part.SetParent(pedestal,false);
                if(part.name=="GlowOrbit"||part.name=="IconEnergyBillboard"||part.name=="ChoiceParticles"||part.name=="GroundAura")part.gameObject.SetActive(false);
            }
            pedestal.localScale=new Vector3(1,.28f,1);
            var cue=altar.GetComponent<BonusRewardCue>();if(cue==null)cue=altar.gameObject.AddComponent<BonusRewardCue>();cue.pickupBurst=burst;
            var oldGlow=altar.transform.Find("CollectibleGlow");
            var glow=oldGlow!=null?oldGlow.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(loop,altar.gameObject.scene);glow.name="CollectibleGlow";glow.transform.SetParent(altar.transform,false);glow.transform.localPosition=new Vector3(0,.2f,0);glow.transform.localScale=Vector3.one*.22f;glow.SetActive(false);cue.nearbyGlow=glow;
            foreach(var text in altar.GetComponentsInChildren<TMP_Text>(true)){text.font=HarborRefinementFontBuilder.Font;text.fontSharedMaterial=material;text.fontStyle=FontStyles.Normal;text.extraPadding=true;text.UpdateMeshPadding();}
            var value=altar.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t=>t.name=="Value_Text");
            if(value!=null&&value.transform.parent.Find("CollectHint")==null){
                var label=Object.Instantiate(value,value.transform.parent);label.name="CollectHint";label.text="보너스";label.fontSize=value.fontSize*.56f;label.enableAutoSizing=false;label.color=new Color(1,.91f,.28f);label.raycastTarget=false;
                label.transform.localPosition=value.transform.localPosition+Vector3.up*.25f;label.rectTransform.sizeDelta=new Vector2(Mathf.Max(value.rectTransform.rect.width,1.08f),.2f);
            }
            foreach(var collider in altar.GetComponentsInChildren<Collider>(true))if(collider.GetComponent<WallScript>()!=null)collider.isTrigger=true;
            foreach(var component in altar.GetComponentsInChildren<Component>(true)){if(component==null)continue;EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();int count=0;
        try{
            foreach(string path in new[]{"Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab","Assets/ShooterSurvival/Prefabs/Walls/New/Box_right.prefab"}){
                if(!File.Exists(path))continue;
                var backup="tmp/backups/harbor-opening-refinement-2026-09-16/"+Path.GetFileName(path);if(!File.Exists(backup))File.Copy(path,backup);
                var contents=PrefabUtility.LoadPrefabContents(path);try{foreach(var altar in contents.GetComponentsInChildren<AuthoredBonusWall>(true))Apply(altar);PrefabUtility.SaveAsPrefabAsset(contents,path);}finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            // Reopen saved scenes after prefab authoring, so inherited additions
            // exist before adding scene-specific overrides and cannot duplicate.
            foreach(string sceneName in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
                var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity");
                foreach(var altar in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AuthoredBonusWall>(true))){Apply(altar);count++;}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
        }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return new{altars=count,cartoonFx=true};
    }
    public static object RebuildRewardEffects()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        BuildRewardEffect("BonusGlow",true);BuildRewardEffect("BonusPickupBurst",false);AssetDatabase.SaveAssets();
        return "Gold stars and champagne glints; no smoke, gas or green glow";
    }
    private static GameObject BuildRewardEffect(string name,bool loop)
    {
        const string graphics="Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/";
        var root=new GameObject(name);
        try {
            ParticleSystem Part(GameObject go,string material,float size,float lifetime,int count) {
                var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.duration=loop?2:.9f;main.loop=loop;main.prewarm=loop;main.playOnAwake=true;main.maxParticles=32;
                main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
                main.startLifetime=new ParticleSystem.MinMaxCurve(lifetime*.7f,lifetime);main.startSize=new ParticleSystem.MinMaxCurve(size*.65f,size);
                main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.66f,.12f),new Color(1,.96f,.65f));
                main.startSpeed=loop?.25f:3.2f;main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.gravityModifier=loop?0:.10f;
                var emission=ps.emission;emission.rateOverTime=loop?count:0;if(!loop)emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
                var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=loop?5:55;shape.radius=loop?.9f:.15f;shape.rotation=new Vector3(-90,0,0);
                var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(1,.60f),new GradientAlphaKey(0,1)});fade.color=gradient;
                var scale=ps.sizeOverLifetime;scale.enabled=true;scale.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.2f,1),new Keyframe(1,0)));
                var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(graphics+material);renderer.renderMode=ParticleSystemRenderMode.Billboard;
                return ps;
            }
            Part(root,"cfxr star ab.mat",loop?.40f:.55f,loop?1.5f:.95f,loop?8:24);
            var glints=new GameObject("ChampagneGlints");glints.transform.SetParent(root.transform,false);glints.transform.localPosition=Vector3.up*.25f;
            Part(glints,"cfxr proc glow soft add.mat",loop?.16f:.18f,loop?1.1f:.5f,loop?7:12);
            return PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+name+".prefab");
        } finally {Object.DestroyImmediate(root);}
    }
}
