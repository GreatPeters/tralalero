using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EquipmentIconRenderer
{
    private const string Folder="Assets/ShooterSurvival/Resources/Cosmetics/IconsV2";
    public static object Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        Directory.CreateDirectory(Folder);var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        var previewObject=new GameObject("Equipment icon preview"){hideFlags=HideFlags.HideAndDontSave};var preview=previewObject.AddComponent<CosmeticPreview>();preview.catalog=catalog;
        int count=0;
        try
        {
            foreach(var row in CosmeticTables.Rows)
            {
                string path=Folder+"/"+row.id+".png";var entry=catalog.Find(row.visualKey);
                if(row.slot==CosmeticSlot.Skin||row.slot==CosmeticSlot.Hat&&row.isDefault)
                {
                    preview.Show(row.slot==CosmeticSlot.Skin?row.visualKey:"skin_original","shoes_original","hat_none");preview.ResetView();
                    var ground=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t.parent!=null&&t.parent.name=="__CosmeticPreview"&&t.name=="Cylinder");
                    if(ground!=null)ground.GetComponent<Renderer>().enabled=false;preview.Rotate(0);CosmeticPresentationBuilder.Save(preview.Texture,path);
                }
                else CaptureAccessory(entry,catalog,row.isDefault,path);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=512;importer.SaveAndReimport();
                entry.icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);count++;
            }
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(previewObject);}
        return new{icons=count};
    }
    private static void CaptureAccessory(CosmeticVisualCatalog.Entry entry,CosmeticVisualCatalog catalog,bool originalShoe,string path)
    {
        var stage=new GameObject("Accessory icon stage"){hideFlags=HideFlags.HideAndDontSave};stage.transform.position=new Vector3(12000,12000,12000);
        RenderTexture target=null;Mesh generated=null;
        try
        {
            GameObject model;
            if(originalShoe)
            {
                model=new GameObject("Original sneaker");model.transform.SetParent(stage.transform,false);generated=OriginalShoe(catalog);model.AddComponent<MeshFilter>().sharedMesh=generated;model.AddComponent<MeshRenderer>().sharedMaterial=entry.material;model.transform.localScale=Vector3.one*1000;
            }
            else model=UnityEngine.Object.Instantiate(entry.accessory,stage.transform,false);
            foreach(var item in model.GetComponentsInChildren<Transform>(true))item.gameObject.layer=31;
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            var bounds=HighwayAssetImporter.BoundsOf(model);float extent=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
            var cameraObject=new GameObject("Icon camera");cameraObject.transform.SetParent(stage.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=extent*.69f;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.nearClipPlane=.01f;camera.farClipPlane=20;
            camera.transform.position=bounds.center+new Vector3(-1.5f,1.05f,2.3f)*extent;camera.transform.LookAt(bounds.center);
            var lightObject=new GameObject("Icon light");lightObject.transform.SetParent(stage.transform,false);lightObject.transform.position=bounds.center+new Vector3(-1,2,2)*extent;var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.range=10*extent;light.intensity=3;light.cullingMask=1<<31;light.shadows=LightShadows.None;
            target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;camera.Render();CosmeticPresentationBuilder.Save(target,path);
        }
        finally{stage.SetActive(false);UnityEngine.Object.DestroyImmediate(stage);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(generated!=null)UnityEngine.Object.DestroyImmediate(generated);}
    }
    private static Mesh OriginalShoe(CosmeticVisualCatalog catalog)
    {
        var source=catalog.splitSharkMesh;var positions=source.vertices;var indices=source.GetTriangles(1);var selected=new List<int>();
        for(int i=0;i<indices.Length;i+=3){var center=(positions[indices[i]]+positions[indices[i+1]]+positions[indices[i+2]])/3;if(center.x<0&&center.z>0)selected.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});}
        var used=selected.Distinct().ToArray();var map=used.Select((v,i)=>(v,i)).ToDictionary(p=>p.v,p=>p.i);var mesh=new Mesh{name="Original sneaker icon"};
        var normals=source.normals;var coordinates=source.uv;
        mesh.vertices=used.Select(i=>positions[i]).ToArray();mesh.normals=used.Select(i=>normals[i]).ToArray();mesh.uv=used.Select(i=>coordinates[i]).ToArray();mesh.triangles=selected.Select(i=>map[i]).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
}
