using System;
using UnityEditor;
using UnityEngine;

/// <summary>Fit the crown opening against the canonical forehead, not the outer brim bounds.</summary>
public static class SharkHeadwearFitter
{
    public const float ForeheadZ=.00215f;
    public const float SeatingDepth=.00009f;
    public const float HatUnitScale=.0016f;

    public static float SurfaceHeight(Mesh mesh,float z)
    {
        var vertices=mesh.vertices;var triangles=mesh.GetTriangles(0);float top=float.NegativeInfinity;
        for(int i=0;i<triangles.Length;i+=3)
        {
            var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
            float denominator=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
            if(Mathf.Abs(denominator)<1e-15f)continue;
            float u=((b.z-c.z)*(-c.x)+(c.x-b.x)*(z-c.z))/denominator;
            float v=((c.z-a.z)*(-c.x)+(a.x-c.x)*(z-c.z))/denominator;
            if(u>=0&&v>=0&&u+v<=1)top=Mathf.Max(top,u*a.y+v*b.y+(1-u-v)*c.y);
        }
        if(float.IsInfinity(top))throw new InvalidOperationException("No forehead surface at the fitting point");
        return top;
    }

    public static void Apply(CosmeticVisualCatalog catalog)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var renderer=catalog.previewModel.GetComponentInChildren<SkinnedMeshRenderer>(true);
        int head=Array.FindIndex(renderer.bones,b=>b.name=="head");
        if(head<0)throw new InvalidOperationException("Head bone missing");
        var mesh=catalog.splitSharkMesh;
        float height=SurfaceHeight(mesh,ForeheadZ);
        float slope=Mathf.Atan2(SurfaceHeight(mesh,ForeheadZ-.0002f)-SurfaceHeight(mesh,ForeheadZ+.0002f),.0004f)*Mathf.Rad2Deg;
        slope=Mathf.Clamp(slope,8,28);
        var bind=mesh.bindposes[head];
        catalog.hatLocalPosition=bind.MultiplyPoint3x4(new Vector3(0,height-SeatingDepth,ForeheadZ));
        catalog.hatLocalRotation=bind.rotation*Quaternion.Euler(slope,0,0);
        catalog.hatLocalScale=bind.lossyScale*HatUnitScale;
        void Fit(string key,float scale,Vector3 offset,Vector3 rotation)
        {var item=catalog.Find(key);if(item==null)return;item.hatScale=scale;item.hatOffset=offset;item.hatEuler=rotation;}
        Fit("hat_cap",1f,new Vector3(0,-.035f,.045f),Vector3.zero);
        Fit("hat_bucket",1.05f,new Vector3(0,-.07f,-.015f),Vector3.zero);
        Fit("hat_tophat",.94f,new Vector3(0,-.045f,-.04f),new Vector3(0,0,-3));
        Fit("hat_pirate",1.16f,new Vector3(0,-.06f,-.03f),new Vector3(0,0,-3));
        Fit("hat_relic",1.02f,new Vector3(0,-.035f,.12f),Vector3.zero);
        Fit("hat_goggles",.98f,new Vector3(0,-.10f,.10f),new Vector3(-45,0,0));
        Fit("hat_diver",.63f,new Vector3(0,-.02f,-.025f),new Vector3(-5,0,0));
        AddPirateLining(catalog);
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
    }

    private static void AddPirateLining(CosmeticVisualCatalog catalog)
    {
        const string prefabPath="Assets/ShooterSurvival/Prefabs/Cosmetics/Harbor/hat_pirate_fitted.prefab";
        const string materialPath="Assets/ShooterSurvival/Models/Cosmetics/Harbor/PirateLining.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null)
        {
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Pirate crown lining"};
            material.SetColor("_BaseColor",new Color(.045f,.033f,.032f));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.15f);
            AssetDatabase.CreateAsset(material,materialPath);
        }
        GeneratedStylizedSurface.Apply(material);AssetDatabase.SaveAssetIfDirty(material);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if(prefab==null)
        {
            var root=new GameObject("Fitted pirate hat");
            try
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Cosmetics/Trellis/hat_pirate.prefab");
                PrefabUtility.InstantiatePrefab(source,root.transform);
                var lining=GameObject.CreatePrimitive(PrimitiveType.Sphere);lining.name="Inner crown lining";lining.transform.SetParent(root.transform,false);
                UnityEngine.Object.DestroyImmediate(lining.GetComponent<Collider>());
                lining.transform.localPosition=new Vector3(0,.24f,-.04f);lining.transform.localScale=new Vector3(.62f,.40f,.52f);
                var renderer=lining.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        catalog.Find("hat_pirate").accessory=prefab;
    }
}
