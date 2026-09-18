#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WorkshopEquipmentAssets
{
    private const string Folder = "Assets/ShooterSurvival/Resources/Cosmetics";
    private static Material brass, steel, leather, glass, cloth;
    public static object Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var catalog = AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>(Folder + "/Catalog.asset");
        brass = Material("gear_brass", new Color(.61f,.35f,.11f), .72f);
        steel = Material("gear_steel", new Color(.16f,.21f,.24f), .7f);
        leather = Material("gear_leather", new Color(.32f,.065f,.035f), 0);
        glass = Material("gear_crystal", new Color(.05f,.7f,.9f), .25f);
        glass.EnableKeyword("_EMISSION"); glass.SetColor("_EmissionColor", new Color(0,.7f,1.1f));
        cloth = Material("gear_cloth", new Color(.055f,.07f,.095f), 0);
        var entries = catalog.entries.ToList();
        foreach (string key in new[] { "skin_armor", "skin_raider", "skin_relic", "skin_diver", "shoes_steel", "shoes_spring", "shoes_relic", "shoes_salvage", "hat_goggles", "hat_diver", "hat_pirate", "hat_relic" })
        {
            if (entries.Any(e => e.key == key)) continue;
            var root = new GameObject(key);
            try
            {
                switch (key)
                {
                    case "skin_armor":
                        for (int i=0;i<3;i++) Part(root,"Dorsal armour "+i,PrimitiveType.Sphere,steel,new Vector3(0,.48f,-i*.42f),new Vector3(1.05f,.25f,.52f));
                        for (int side=-1;side<=1;side+=2) Part(root,"Riveted shoulder",PrimitiveType.Sphere,brass,new Vector3(side*.46f,.17f,-.1f),new Vector3(.22f,.62f,.65f));
                        break;
                    case "skin_raider":
                        for (int side=-1;side<=1;side+=2) Part(root,"Leather harness",PrimitiveType.Cube,leather,new Vector3(side*.39f,.2f,-.22f),new Vector3(.12f,.75f,1.1f),new Vector3(0,0,side*23));
                        Part(root,"Salvaged medallion",PrimitiveType.Cylinder,brass,new Vector3(0,.57f,-.45f),new Vector3(.33f,.07f,.33f));
                        break;
                    case "skin_relic":
                        for(int i=0;i<3;i++) {Part(root,"Seal band",PrimitiveType.Cube,brass,new Vector3(0,.51f,-i*.42f),new Vector3(.83f,.11f,.13f));Part(root,"Bound crystal",PrimitiveType.Sphere,glass,new Vector3(0,.63f,-i*.42f),new Vector3(.2f,.3f,.2f));}
                        break;
                    case "skin_diver":
                        foreach(int side in new[]{-1,1}){Part(root,"Pressure tank",PrimitiveType.Capsule,steel,new Vector3(side*.4f,.46f,-.5f),new Vector3(.29f,.62f,.29f),new Vector3(90,0,0));Part(root,"Tank band",PrimitiveType.Cube,brass,new Vector3(side*.4f,.49f,-.5f),new Vector3(.36f,.32f,.12f));}
                        break;
                    case "shoes_steel":
                        Part(root,"Bolted toe cap",PrimitiveType.Sphere,steel,new Vector3(0,-.05f,.17f),new Vector3(.53f,.3f,.63f));
                        foreach(int side in new[]{-1,1})Part(root,"Brass rivet",PrimitiveType.Sphere,brass,new Vector3(side*.24f,.04f,.17f),Vector3.one*.075f);
                        break;
                    case "shoes_spring":
                        for(int i=0;i<4;i++)Part(root,"Spring coil",PrimitiveType.Cylinder,brass,new Vector3(0,-.13f+i*.055f,-.12f),new Vector3(.34f,.018f,.34f));
                        Part(root,"Heel housing",PrimitiveType.Cube,steel,new Vector3(0,-.02f,-.29f),new Vector3(.42f,.26f,.13f));
                        break;
                    case "shoes_relic":
                        Part(root,"Crystal setting",PrimitiveType.Cube,brass,new Vector3(0,.02f,.08f),new Vector3(.42f,.12f,.42f));
                        Part(root,"Sealed gemstone",PrimitiveType.Sphere,glass,new Vector3(0,.13f,.08f),new Vector3(.25f,.28f,.3f));
                        break;
                    case "shoes_salvage":
                        foreach(int side in new[]{-1,1}){Part(root,"Magnet arm",PrimitiveType.Cube,steel,new Vector3(side*.23f,0,.03f),new Vector3(.11f,.22f,.52f));Part(root,"Magnet tip",PrimitiveType.Cube,leather,new Vector3(side*.23f,0,.26f),new Vector3(.13f,.24f,.15f));}
                        Part(root,"Magnet bridge",PrimitiveType.Cube,brass,new Vector3(0,0,-.22f),new Vector3(.56f,.22f,.11f));
                        break;
                    case "hat_goggles":
                        foreach(int side in new[]{-1,1}){Part(root,"Lens rim",PrimitiveType.Cylinder,brass,new Vector3(side*.2f,.15f,.2f),new Vector3(.37f,.07f,.37f),new Vector3(90,0,0));Part(root,"Lens",PrimitiveType.Sphere,glass,new Vector3(side*.2f,.15f,.27f),new Vector3(.26f,.26f,.08f));}
                        Part(root,"Bridge",PrimitiveType.Cube,leather,new Vector3(0,.15f,.2f),new Vector3(.18f,.1f,.1f));
                        break;
                    case "hat_diver":
                        Part(root,"Diving helmet",PrimitiveType.Sphere,brass,new Vector3(0,.17f,0),new Vector3(.91f,.78f,.85f));
                        Part(root,"Front window",PrimitiveType.Cylinder,steel,new Vector3(0,.18f,.4f),new Vector3(.51f,.07f,.51f),new Vector3(90,0,0));
                        Part(root,"Window glass",PrimitiveType.Sphere,glass,new Vector3(0,.18f,.47f),new Vector3(.36f,.36f,.07f));
                        Part(root,"Valve",PrimitiveType.Cylinder,steel,new Vector3(.48f,.18f,0),new Vector3(.19f,.08f,.19f),new Vector3(0,0,90));
                        break;
                    case "hat_pirate":
                        Part(root,"Tricorn crown",PrimitiveType.Sphere,cloth,new Vector3(0,.13f,0),new Vector3(.81f,.48f,.67f));
                        foreach(float yaw in new[]{0f,120f,240f}){var go=Part(root,"Folded brim",PrimitiveType.Cube,cloth,new Vector3(0,.07f,0),new Vector3(.92f,.16f,.15f),new Vector3(0,yaw,18));}
                        Part(root,"Contraband crest",PrimitiveType.Sphere,brass,new Vector3(0,.2f,.31f),new Vector3(.18f,.2f,.055f));
                        break;
                    case "hat_relic":
                        Part(root,"Seal crown band",PrimitiveType.Cylinder,brass,new Vector3(0,.075f,0),new Vector3(.78f,.075f,.78f));
                        for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Part(root,"Crown prong",PrimitiveType.Cube,steel,new Vector3(Mathf.Sin(a)*.31f,.28f,Mathf.Cos(a)*.31f),new Vector3(.09f,.35f,.09f),new Vector3(0,a*Mathf.Rad2Deg,0));}
                        Part(root,"Captured crystal",PrimitiveType.Sphere,glass,new Vector3(0,.37f,0),new Vector3(.25f,.39f,.25f));
                        break;
                }
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + key + ".prefab");
                var entry = new CosmeticVisualCatalog.Entry {key=key, accessory=prefab, accessoryAnchor=key.StartsWith("shoes_")?"feet":"chest", accessoryWorldScale=1f};
                if(!key.StartsWith("hat_"))
                {
                    var material = new Material(catalog.Find(key.StartsWith("skin_")?"skin_original":"shoes_original").material){name=key};
                    Color tint=key.Contains("relic")?new Color(.6f,.95f,1.2f):key.Contains("raider")?new Color(.65f,.42f,.36f):key.Contains("diver")?new Color(.22f,.37f,.34f):new Color(.42f,.48f,.57f);
                    material.SetColor("_BaseColor",tint);material.SetColor("_EmissionColor",Color.black);
                    AssetDatabase.CreateAsset(material,Folder+"/"+key+".mat");entry.material=material;entry.swatch=tint;
                }
                entries.Add(entry);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        catalog.entries=entries.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        return new {entries=catalog.entries.Length};
    }
    private static Material Material(string name,Color color,float metallic)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.35f);GeneratedStylizedSurface.Apply(m);return m;
    }
    private static GameObject Part(GameObject root,string name,PrimitiveType type,Material material,Vector3 position,Vector3 scale,Vector3 euler=default)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root.transform,false);go.transform.localPosition=position;go.transform.localScale=scale;go.transform.localEulerAngles=euler;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
}
#endif
