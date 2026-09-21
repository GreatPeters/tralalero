using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;
using IndianOceanAssets.ShooterSurvival;

public static class ApplyMobileFeedback
{
    static readonly string[] Scenes={"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"};
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        var start=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(start.isDirty)throw new InvalidOperationException("Save current authoring state first.");
        string originalScene=start.path;Directory.CreateDirectory("tmp/mobile-feedback-2026-09-20/before");
        var icons=OriginalUpgradeArtwork.RefreshDatabase();
        foreach(string name in new[]{"Enemy_Woman","Enemy_Guard"})
        {
            string path="Assets/JH/Model/Prefab/"+name+".prefab";Backup(path,name+".prefab");var root=PrefabUtility.LoadPrefabContents(path);
            try{if(name.Contains("Woman"))Woman(root);else Guard(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        string talisman="Assets/ShooterSurvival/Resources/BonusTalisman/Talisman.prefab";Backup(talisman,"Talisman.prefab");var art=PrefabUtility.LoadPrefabContents(talisman);
        try{BonusTalismanRibbon.Apply(art.GetComponent<BonusTalismanVisual>());PrefabUtility.SaveAsPrefabAsset(art,talisman);}
        finally{PrefabUtility.UnloadPrefabContents(art);}
        UpdateBonusPrefabs();
        int cards=0,bonuses=0;
        foreach(string name in Scenes)
        {
            string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";Backup(path,name+".unity");var scene=EditorSceneManager.OpenScene(path);
            var roots=scene.GetRootGameObjects();
            foreach(var enemy in roots.SelectMany(r=>r.GetComponentsInChildren<EnemyScript_space>(true)))
            {
                var animator=enemy.GetComponentInChildren<Animator>(true);
                if(animator!=null)
                {
                    if(animator.name.Contains("Baker_in_Apron"))Woman(enemy.gameObject);
                    if(enemy.GetComponent<EnemyGunAim>()!=null)Guard(enemy.gameObject);
                    animator.cullingMode=AnimatorCullingMode.CullCompletely;Record(animator);
                    foreach(var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true)){skin.updateWhenOffscreen=false;Record(skin);}
                }
            }
            foreach(var visual in roots.SelectMany(r=>r.GetComponentsInChildren<BonusTalismanVisual>(true)))
            {
                BonusTalismanRibbon.Apply(visual);Record(visual);bonuses++;
                var wall=visual.GetComponentInParent<WallScript>();if(wall!=null)BonusTalismanPresentation.Refresh(wall);
            }
            foreach(var card in roots.SelectMany(r=>r.GetComponentsInChildren<UpgradeUI>(true)))
            {
                var data=new SerializedObject(card);var icon=data.FindProperty("iconImage").objectReferenceValue as Image;
                var sprite=OriginalUpgradeArtwork.ForUpgrade(card.UpgradeId);if(icon!=null){icon.sprite=sprite;icon.preserveAspect=true;Record(icon);}
                data.FindProperty("iconOverride").objectReferenceValue=sprite;data.FindProperty("spriteDatabase").objectReferenceValue=icons;data.ApplyModifiedPropertiesWithoutUndo();Record(card);cards++;
            }
            foreach(var tutorial in roots.SelectMany(r=>r.GetComponentsInChildren<CoastalTutorialUI>(true)))Tutorial(tutorial);
            foreach(var canvas in roots.SelectMany(r=>r.GetComponentsInChildren<CanvasScript>(true)))StartShade(canvas);
            Canvas.ForceUpdateCanvases();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(true);EditorSceneManager.OpenScene(originalScene);
        return $"Saved three chapters, {cards} upgrade cards and {bonuses} talismans; Woman grip, Guard shot and readable start UI applied.";
    }
    public static string UpdateBonusPrefabs()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        int count=0;
        foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ShooterSurvival/Prefabs/Walls"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.GetComponentInChildren<BonusTalismanVisual>(true)==null)continue;
            Backup(path,Path.GetFileName(path));var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var visual in root.GetComponentsInChildren<BonusTalismanVisual>(true)){BonusTalismanRibbon.Apply(visual);Record(visual);count++;}
                foreach(var wall in root.GetComponentsInChildren<WallScript>(true))if(wall.wallType==WallType.BuffWall)BonusTalismanPresentation.Refresh(wall);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return $"Updated {count} drop/pool prefab talismans.";
    }
    static void Woman(GameObject root)
    {
        var weapon=root.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m=>m.name.Contains("Chef_s_Steel"));if(weapon==null)return;
        var t=weapon.transform;t.localRotation=Quaternion.Euler(0,90,0);
        // Grip point measured from the wood handle; palm point from the glove's baked skin.
        t.localPosition=new Vector3(-.025f,.11f,.075f)-t.localRotation*Vector3.Scale(new Vector3(-.0065f,-.0015f,0),t.localScale);Record(t);
    }
    static void Guard(GameObject root)
    {
        var enemy=root.GetComponent<EnemyScript_space>();if(enemy==null)return;
        var prop=new SerializedObject(enemy).FindProperty("heldProjectile").objectReferenceValue as Transform;if(prop==null)return;
        var material=ShotMaterial(false);
        foreach(var renderer in prop.GetComponentsInChildren<MeshRenderer>(true)){renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;Record(renderer);}
        var trail=prop.GetComponent<TrailRenderer>();if(trail==null)trail=prop.gameObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial=ShotMaterial(true);trail.time=.22f;trail.minVertexDistance=.03f;trail.startWidth=.15f;trail.endWidth=.015f;trail.alignment=LineAlignment.View;trail.emitting=false;
        trail.startColor=new Color(1,.86f,.40f,1);trail.endColor=new Color(1,.35f,.04f,0);trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;Record(trail);
    }
    static Material ShotMaterial(bool trail)
    {
        string path="Assets/ShooterSurvival/Materials/Generated/"+(trail?"GuardShotTrail":"GuardShotVisible")+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find(trail?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",new Color(1.5f,1.0f,.22f,1));
        if(trail)
        {
            material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);material.SetFloat("_Cull",0);
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
        }
        EditorUtility.SetDirty(material);return material;
    }
    static void Tutorial(CoastalTutorialUI tutorial)
    {
        var panel=(RectTransform)tutorial.title.transform.parent;Rect(panel,.055f,.745f,.945f,.885f);
        Rect(tutorial.title.rectTransform,.18f,.67f,.855f,.94f);Rect(tutorial.body.rectTransform,.18f,.10f,.855f,.66f);
        tutorial.title.fontSize=44;tutorial.title.enableAutoSizing=false;tutorial.title.color=new Color(1,.86f,.30f);tutorial.title.alignment=TextAlignmentOptions.MidlineLeft;
        tutorial.body.fontSize=50;tutorial.body.enableAutoSizing=false;tutorial.body.color=Color.white;tutorial.body.textWrappingMode=TextWrappingModes.Normal;tutorial.body.alignment=TextAlignmentOptions.MidlineLeft;
        tutorial.title.margin=tutorial.body.margin=Vector4.zero;
        Rect(tutorial.icon.rectTransform,.035f,.26f,.155f,.76f);tutorial.icon.preserveAspect=true;
        var close=panel.Find("Close") as RectTransform;if(close!=null)
        {
            Rect(close,.885f,.59f,.985f,.92f);
            foreach(var text in close.GetComponentsInChildren<TMP_Text>(true)){text.fontSize=45;text.enableAutoSizing=false;Record(text);}
        }
        foreach(var component in panel.GetComponentsInChildren<Component>(true))if(component!=null)Record(component);
    }
    static void StartShade(CanvasScript canvas)
    {
        var found=canvas.transform.Find("LobbyStartShade");GameObject root;
        if(found==null){root=new GameObject("LobbyStartShade",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));root.transform.SetParent(canvas.transform,false);}else root=found.gameObject;
        root.transform.SetAsFirstSibling();Rect((RectTransform)root.transform,0,0,1,1);
        var image=root.GetComponent<Image>();image.color=new Color(0,0,0,.9f);image.raycastTarget=false;
        var controller=root.GetComponent<LobbyStartShade>();if(controller==null)controller=root.AddComponent<LobbyStartShade>();controller.owner=canvas;controller.shade=image;Record(controller);Record(image);
    }
    static void Rect(RectTransform rect,float x,float y,float xx,float yy){rect.anchorMin=new Vector2(x,y);rect.anchorMax=new Vector2(xx,yy);rect.offsetMin=rect.offsetMax=Vector2.zero;rect.localScale=Vector3.one;}
    static void Record(UnityEngine.Object target){EditorUtility.SetDirty(target);if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
    static void Backup(string path,string name){var target="tmp/mobile-feedback-2026-09-20/before/"+name;if(!File.Exists(target))File.Copy(path,target);}
}
