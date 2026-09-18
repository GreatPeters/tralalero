using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A compact closed brass diving cap sized for the shark rather than a human torso.</summary>
public static class HarborHatBuilder
{
    private const string Folder="Assets/ShooterSurvival/Models/Cosmetics/Harbor";
    public static object Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("Assets/ShooterSurvival/Prefabs/Cosmetics/Harbor");AssetDatabase.Refresh();
        var groups=Enumerable.Range(0,4).Select(_=>new List<CombineInstance>()).ToArray();
        var meshes=new List<Mesh>();
        Mesh Surface(Func<float,float,Vector3> point,int rows,int columns)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=rows;i++)for(int j=0;j<=columns;j++)vertices.Add(point(i/(float)rows,j/(float)columns));
            for(int i=0;i<rows;i++)for(int j=0;j<columns;j++)
            {int a=i*(columns+1)+j,b=a+columns+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
        }
        var sphere=Surface((u,v)=>{float p=(u-.5f)*Mathf.PI,a=v*Mathf.PI*2;return new Vector3(Mathf.Cos(p)*Mathf.Cos(a),Mathf.Sin(p),Mathf.Cos(p)*Mathf.Sin(a));},10,20);
        Mesh Torus(float radius,float tube)=>Surface((u,v)=>{float a=u*Mathf.PI*2,b=v*Mathf.PI*2;return new Vector3((radius+tube*Mathf.Cos(b))*Mathf.Cos(a),-tube*Mathf.Sin(b),(radius+tube*Mathf.Cos(b))*Mathf.Sin(a));},32,8);
        void Part(Mesh mesh,int material,Vector3 position,Vector3 scale,Quaternion rotation)
        {groups[material].Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(position,rotation,scale)});}
        void Ball(int material,Vector3 position,Vector3 scale)=>Part(sphere,material,position,scale,Quaternion.identity);
        var dome=Surface((u,v)=>{float p=u*Mathf.PI*.5f,a=v*Mathf.PI*2;return new Vector3(.88f*Mathf.Cos(p)*Mathf.Cos(a),.64f*Mathf.Sin(p),.81f*Mathf.Cos(p)*Mathf.Sin(a));},12,40);
        Part(dome,0,Vector3.zero,Vector3.one,Quaternion.identity);
        Ball(1,new Vector3(0,-.035f,0),new Vector3(.90f,.065f,.83f));
        Part(Torus(.89f,.055f),2,Vector3.zero,new Vector3(1,1,.93f),Quaternion.identity);
        // Central lamp with a closed teal lens, plus two side pressure housings.
        Part(Torus(.26f,.055f),2,new Vector3(0,.29f,.76f),Vector3.one,Quaternion.Euler(90,0,0));
        Ball(3,new Vector3(0,.29f,.78f),new Vector3(.245f,.245f,.055f));
        Ball(2,new Vector3(-.075f,.375f,.835f),new Vector3(.045f,.025f,.008f));
        foreach(float side in new[]{-1f,1f})
        {
            Ball(2,new Vector3(side*.81f,.24f,0),new Vector3(.095f,.21f,.21f));
            Ball(1,new Vector3(side*.887f,.24f,0),new Vector3(.035f,.13f,.13f));
            Ball(3,new Vector3(side*.911f,.24f,0),new Vector3(.018f,.082f,.082f));
        }
        for(int i=0;i<16;i++)
        {float a=i*Mathf.PI/8;Ball(2,new Vector3(.885f*Mathf.Cos(a),.045f,.823f*Mathf.Sin(a)),Vector3.one*.035f);}
        // Narrow raised strips divide the shell into readable plates.
        foreach(float a in new[]{0f,Mathf.PI*.5f,Mathf.PI,Mathf.PI*1.5f})
        {
            var strip=Surface((u,v)=>{float p=u*Mathf.PI*.49f,angle=a+(v-.5f)*.045f;return new Vector3(.89f*Mathf.Cos(p)*Mathf.Cos(angle),.65f*Mathf.Sin(p),.82f*Mathf.Cos(p)*Mathf.Sin(angle));},12,2);
            Part(strip,2,Vector3.zero,Vector3.one,Quaternion.identity);
        }
        var merged=new List<Mesh>();
        for(int i=0;i<groups.Length;i++){var mesh=new Mesh();mesh.CombineMeshes(groups[i].ToArray(),true,true);merged.Add(mesh);}
        var result=new Mesh{name="Riveted diving cap"};result.CombineMeshes(merged.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,true);result.RecalculateBounds();
        string meshPath=Folder+"/DivingCap.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(existing==null)AssetDatabase.CreateAsset(result,meshPath);else{EditorUtility.CopySerialized(result,existing);UnityEngine.Object.DestroyImmediate(result);result=existing;}
        Color[] colors={new(.48f,.25f,.075f),new(.045f,.075f,.085f),new(.95f,.65f,.22f),new(.045f,.33f,.36f)};
        string[] names={"Copper","NavySeal","Brass","TealGlass"};var materials=new Material[4];
        for(int i=0;i<4;i++)
        {
            string path=Folder+"/"+names[i]+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",colors[i]);material.SetFloat("_Metallic",i==1?0:.45f);material.SetFloat("_Smoothness",i==3?.75f:.4f);
            material.SetFloat("_Cull",0);GeneratedStylizedSurface.Apply(material);materials[i]=material;
        }
        var root=new GameObject("Harbor Diving Cap",typeof(MeshFilter),typeof(MeshRenderer));root.GetComponent<MeshFilter>().sharedMesh=result;
        root.GetComponent<MeshRenderer>().sharedMaterials=materials;root.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        const string prefabPath="Assets/ShooterSurvival/Prefabs/Cosmetics/Harbor/hat_diver.prefab";
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);UnityEngine.Object.DestroyImmediate(root);
        foreach(var mesh in meshes.Concat(merged))UnityEngine.Object.DestroyImmediate(mesh);
        var catalog=AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>("Assets/ShooterSurvival/Resources/Cosmetics/Catalog.asset");
        var entry=catalog.Find("hat_diver");entry.accessory=prefab;entry.hatScale=.78f;entry.hatOffset=new Vector3(0,.14f,-.025f);entry.hatEuler=Vector3.zero;
        entry.icon=RenderIcon(prefab);
        var goggles=catalog.Find("hat_goggles");goggles.hatScale=.94f;goggles.hatOffset=new Vector3(0,-.30f,.27f);goggles.hatEuler=Vector3.zero;
        SharkHeadwearFitter.Apply(catalog);
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();return new{prefabPath,triangles=result.triangles.Length/3};
    }

    private static Sprite RenderIcon(GameObject prefab)
    {
        const string path="Assets/ShooterSurvival/UI/HarborWorkshop/EquipmentIcons/hat_diver_polished.png";
        var stage=new GameObject("Diving cap icon"){hideFlags=HideFlags.HideAndDontSave};stage.transform.position=new Vector3(10000,10000,10000);
        var instance=UnityEngine.Object.Instantiate(prefab,stage.transform,false);
        foreach(var item in instance.GetComponentsInChildren<Transform>(true))item.gameObject.layer=31;
        var view=new GameObject("Icon Camera");view.transform.SetParent(stage.transform,false);
        view.transform.localPosition=new Vector3(-2,1.5f,3);view.transform.LookAt(stage.transform.position+new Vector3(0,.27f,0));
        var camera=view.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.04f;camera.aspect=1;
        camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.nearClipPlane=.1f;camera.farClipPlane=10;
        var texture=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);texture.Create();camera.targetTexture=texture;
        try{camera.Render();CosmeticPresentationBuilder.Save(texture,path);}
        finally{camera.targetTexture=null;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(stage);}
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
