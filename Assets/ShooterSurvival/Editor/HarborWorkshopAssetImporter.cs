using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class HarborWorkshopAssetImporter
{
    public const string Root="Assets/ShooterSurvival/Resources/HarborRefinement";
    public static Material ImportMaterial(string folder,string name)
    {
        foreach(string texture in Directory.GetFiles(folder,"*.png")){
            AssetDatabase.ImportAsset(texture,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texture);importer.maxTextureSize=2048;importer.mipmapEnabled=true;
            if(Path.GetFileName(texture)=="Normal.png")importer.textureType=TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
        string path=folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/BaseColor.png"));
        material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Normal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_Smoothness",.22f);
        GeneratedStylizedSurface.Apply(material);AssetDatabase.SaveAssetIfDirty(material);return material;
    }
    public static object InstallWorkshop()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        AssetDatabase.Refresh();string folder=Root+"/Workshop";string fbx=folder+"/Workshop.fbx";
        AssetDatabase.ImportAsset(fbx,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        var material=ImportMaterial(folder,"Workshop");
        var scene=SceneManager.GetActiveScene();if(scene.name!="Noryangjin_MapTool_Mode_SR18")throw new InvalidOperationException("SR18 required");
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        var departure=map.Find("Props/HarborDeparture_20260916");if(departure==null)throw new InvalidOperationException("Install pier first");
        var old=departure.Find("ShoeWorkshop");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("ShoeWorkshop").transform;root.SetParent(departure,false);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx),scene);model.transform.SetParent(root,false);
        foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=material;
        Bounds Bounds(){var renderers=model.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
        var bounds=Bounds();model.transform.localScale*=5.1f/bounds.size.y;bounds=Bounds();model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        root.position=new Vector3(49,.07f,-121.5f);root.rotation=Quaternion.Euler(0,65,0);root.localScale=Vector3.one*1.1f;
        var deck=map.Find("Roads/HarborWorkshopSideDeck");deck.position=new Vector3(44.07f,0,-118.8f);
        var sign=new GameObject("WorkshopSign",typeof(TextMeshPro)).GetComponent<TextMeshPro>();sign.transform.SetParent(root,false);
        sign.transform.localPosition=new Vector3(0,3.55f,1.81f);sign.transform.localRotation=Quaternion.Euler(0,180,0);
        sign.font=HarborRefinementFontBuilder.Font;sign.fontSharedMaterial=HarborRefinementFontBuilder.Font.material;sign.text="신발 개조소";sign.fontSize=5;sign.color=new Color(.04f,.12f,.25f);sign.alignment=TextAlignmentOptions.Center;sign.rectTransform.sizeDelta=new Vector2(4.2f,.62f);sign.enableAutoSizing=true;sign.fontSizeMin=2;sign.fontSizeMax=5;
        foreach(var c in root.GetComponentsInChildren<Component>(true)){if(c!=null)EditorUtility.SetDirty(c);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{workshop=root.position.ToString(),height=Bounds().size.y};
    }
    public static object InstallMerchant()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        string folder=Root+"/Merchant",path=folder+"/Merchant.fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.SaveAndReimport();
        var clips=importer.defaultClipAnimations;foreach(var clip in clips)clip.loopTime=clip.name.Contains("SeatedIdle");importer.clipAnimations=clips;importer.SaveAndReimport();
        var animations=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        var idle=animations.First(c=>c.name.Contains("SeatedIdle"));var greet=animations.First(c=>c.name.Contains("StandGreet"));
        string controllerPath=folder+"/Merchant.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null){
            controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Greet",AnimatorControllerParameterType.Trigger);
            var machine=controller.layers[0].stateMachine;var seated=machine.AddState("SeatedIdle");seated.motion=idle;machine.defaultState=seated;
            var waving=machine.AddState("StandGreet");waving.motion=greet;
            var start=seated.AddTransition(waving);start.hasExitTime=false;start.duration=.12f;start.AddCondition(AnimatorConditionMode.If,0,"Greet");
            var finish=waving.AddTransition(seated);finish.hasExitTime=true;finish.exitTime=1;finish.duration=.12f;
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        var material=ImportMaterial(folder,"Merchant");var scene=SceneManager.GetActiveScene();
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var departure=map.Find("Props/HarborDeparture_20260916");
        var old=departure.Find("MerchantStation");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var station=new GameObject("MerchantStation").transform;station.SetParent(departure,false);station.position=new Vector3(52.7f,.07f,-118.9f);station.localScale=Vector3.one*1.8f;station.rotation=Quaternion.Euler(0,70,0);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);model.transform.SetParent(station,false);
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){renderer.sharedMaterial=material;if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;}
        var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        station.gameObject.AddComponent<HarborMerchantGreeting>().animator=animator;
        string seatMatPath=Root+"/Stool.mat";var wood=AssetDatabase.LoadAssetAtPath<Material>(seatMatPath);if(wood==null){wood=new Material(Shader.Find("Universal Render Pipeline/Lit"));wood.SetColor("_BaseColor",new Color(.22f,.095f,.035f));wood.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(wood,seatMatPath);}
        GeneratedStylizedSurface.Apply(wood);AssetDatabase.SaveAssetIfDirty(wood);
        void Cylinder(string name,Vector3 pos,Vector3 scale){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(station,false);go.transform.localPosition=pos;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=wood;}
        Cylinder("Seat",new Vector3(0,.53f,0),new Vector3(.72f,.055f,.72f));
        foreach(float x in new[]{-.24f,.24f})foreach(float z in new[]{-.24f,.24f})Cylinder("StoolLeg",new Vector3(x,.26f,z),new Vector3(.07f,.26f,.07f));
        foreach(var component in station.GetComponentsInChildren<Component>(true)){if(component==null)continue;EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{animations=animations.Select(c=>c.name).ToArray(),position=station.position.ToString()};
    }
}
