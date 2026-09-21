using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class PolishedTalismanAssets
{
    const string Root="Assets/ShooterSurvival/Resources/BonusTalisman";
    const string Art=Root+"/Polished";
    static readonly Dictionary<string,Material> materials=new();
    public static void Build()
    {
        AssetDatabase.Refresh();
        Surface("PaperIvory",new Color(.98f,.90f,.73f),false);
        Surface("NavyPrint",new Color(.026f,.07f,.15f),false);
        Surface("NavyEnamel",new Color(.022f,.07f,.17f),true);
        Surface("CaptionNavy",new Color(.012f,.035f,.08f),false);
        Surface("SilverRivet",new Color(.85f,.9f,.96f),true);
        Surface("GoldFace",new Color(1,.58f,.055f),true);
        Surface("GoldEdge",new Color(.62f,.27f,.015f),true);
        Surface("TealFace",new Color(.01f,.9f,.67f),true);
        Surface("TealEdge",new Color(.005f,.30f,.23f),true);
        GlowMaterial("GoldGlow",new Color(2.6f,1.8f,.55f,1));
        GlowMaterial("TrailCore",new Color(3.3f,2.7f,1.35f,1));
        GlowMaterial("TrailHalo",new Color(1.8f,.92f,.12f,1));
        MakeRing();
        var root=new GameObject("Talisman");
        try
        {
            var visual=root.AddComponent<BonusTalismanVisual>();
            visual.paper=Child(root.transform,"Paper");
            var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/TalismanBody.fbx"),visual.paper);
            model.name="FoldedPaperModel";model.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>materials[m.name]).ToArray();
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            visual.leftFold=model.transform.Find("LeftPaper");visual.rightFold=model.transform.Find("RightPaper");
            if(visual.leftFold==null||visual.rightFold==null)throw new InvalidOperationException("Blender fold pivots missing");
            visual.icon=Child(root.transform,"StatIcon").gameObject.AddComponent<SpriteRenderer>();
            visual.icon.transform.localPosition=new Vector3(0,-.04f,-.15f);
            visual.icon.sortingOrder=3;
            var emblem=Child(visual.icon.transform,"RaisedEmblem");emblem.localRotation=Quaternion.Euler(0,180,0);
            visual.emblemMesh=emblem.gameObject.AddComponent<MeshFilter>();visual.emblemRenderer=emblem.gameObject.AddComponent<MeshRenderer>();
            visual.emblemRenderer.shadowCastingMode=ShadowCastingMode.Off;visual.emblemRenderer.receiveShadows=false;
            var emblems=new List<BonusTalismanVisual.Emblem>();
            foreach(var key in new[]{"WallBonus_Attack","WallBonus_Health","WallBonus_AttackSpeed","WallBonus_MissileDuration","WallBonus_MissileAdd"})
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+key+".fbx");
                var filter=source.GetComponentInChildren<MeshFilter>();
                emblems.Add(new BonusTalismanVisual.Emblem{spriteName=key,mesh=filter.sharedMesh,materials=filter.GetComponent<Renderer>().sharedMaterials.Select(m=>materials[m.name]).ToArray()});
            }
            visual.emblems=emblems.ToArray();
            BonusTalismanRibbon.Apply(visual);
            visual.readyHalo=Child(root.transform,"ReadyHalo").gameObject.AddComponent<SpriteRenderer>();
            visual.readyHalo.sprite=Resources.Load<Sprite>("BonusTalisman/SoftGlow");
            visual.readyHalo.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/GoldGlow.mat");
            visual.readyHalo.transform.localPosition=new Vector3(0,0,.25f);visual.readyHalo.transform.localScale=new Vector3(3,4,1);visual.readyHalo.color=new Color(1,.8f,.4f,.30f);
            visual.idleGlints=new SpriteRenderer[4];
            for(int i=0;i<4;i++)
            {
                var mote=Child(visual.paper,"IdleGlint"+i).gameObject.AddComponent<SpriteRenderer>();
                mote.sprite=Resources.Load<Sprite>("BonusTalisman/SoftGlow");mote.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/GoldGlow.mat");
                mote.transform.localPosition=new Vector3(i%2==0?-.72f:.72f,i<2?.67f:-.64f,-.22f);mote.transform.localScale=Vector3.one*.10f;mote.color=new Color(1,.84f,.45f,.65f);visual.idleGlints[i]=mote;
            }
            visual.labelRoot=Child(root.transform,"Label").gameObject;visual.labelRoot.transform.localPosition=new Vector3(0,-1.5f,-.08f);
            Mesh plate=RoundedPlate();
            var rim=Child(visual.labelRoot.transform,"Rim");rim.localScale=new Vector3(1.02f,1.12f,1);rim.gameObject.AddComponent<MeshFilter>().sharedMesh=plate;rim.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materials["GoldEdge"];
            var badge=Child(visual.labelRoot.transform,"Badge");badge.localPosition=new Vector3(0,0,-.014f);badge.gameObject.AddComponent<MeshFilter>().sharedMesh=plate;badge.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materials["CaptionNavy"];
            visual.label=Child(visual.labelRoot.transform,"Text").gameObject.AddComponent<TextMeshPro>();
            visual.label.transform.localPosition=new Vector3(0,0,-.04f);visual.label.font=HarborRefinementFontBuilder.Font;
            visual.label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root.Replace("BonusTalisman","HarborRefinement")+"/BonusReadable.mat");
            visual.label.rectTransform.sizeDelta=new Vector2(2.48f,.47f);visual.label.fontSize=3.2f;visual.label.enableAutoSizing=true;visual.label.fontSizeMin=1.9f;visual.label.fontSizeMax=3.2f;
            visual.label.alignment=TextAlignmentOptions.MidlineGeoAligned;visual.label.textWrappingMode=TextWrappingModes.NoWrap;
            visual.SetOpen(0);visual.SetContent(Resources.Load<Sprite>("WallBonusIcons/WallBonus_Attack"),"공격력 +14%",new Color(1,.7f,.20f));
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Talisman.prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();
    }
    static Transform Child(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
    static void Surface(string name,Color color,bool glossy)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);GeneratedStylizedSurface.Apply(m);
        m.SetFloat("_Flatness",.25f);m.SetFloat("_ShadowEdgeSize",.28f);m.SetColor("_ColorDim",color*.68f);m.SetFloat("_OutlineWidth",.6f);
        if(glossy){m.SetFloat("_SpecularEnabled",1);m.EnableKeyword("DR_SPECULAR_ON");m.SetColor("_FlatSpecularColor",new Color(1.8f,1.65f,1.35f));m.SetFloat("_FlatSpecularSize",.12f);m.SetFloat("_FlatSpecularEdgeSmoothness",.55f);}
        m.SetFloat("_Cull",2);EditorUtility.SetDirty(m);materials[name]=m;
    }
    static void GlowMaterial(string name,Color tint)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Shooter/TalismanGlow"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_Tint",tint);
        m.SetFloat("_ZTest",name.StartsWith("Trail",StringComparison.Ordinal)?(float)CompareFunction.Always:(float)CompareFunction.LessEqual);
        EditorUtility.SetDirty(m);
    }
    static Mesh RoundedPlate()
    {
        var vertices=new List<Vector3>{Vector3.zero};var triangles=new List<int>();
        for(int corner=0;corner<4;corner++)for(int i=0;i<=8;i++)
        {
            float angle=(corner*90+i*90f/8)*Mathf.Deg2Rad;float x=(corner==0||corner==3)?1.15f:-1.15f;float y=corner<2?.12f:-.12f;
            vertices.Add(new Vector3(x+Mathf.Cos(angle)*.11f,y+Mathf.Sin(angle)*.11f,0));
        }
        for(int i=1;i<vertices.Count;i++)triangles.AddRange(new[]{0,i==vertices.Count-1?1:i+1,i});
        var mesh=new Mesh{name="RoundedCaption"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Art+"/RoundedCaption.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}return mesh;
    }
    static void MakeRing()
    {
        int size=128;var t=new Texture2D(size,size,TextureFormat.RGBA32,false);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f))/63.5f;float a=Mathf.Clamp01(1-Mathf.Abs(d-.74f)/.07f);t.SetPixel(x,y,new Color(1,1,1,a*a));}
        t.Apply();string path=Art+"/ImpactRing.png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=128;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
    }
}
