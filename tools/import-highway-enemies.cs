using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ImportHighwayEnemies
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        string[] sourceNames={"01-mechanic_4a97f10f4c","02-patrol_cd22a7a411","03-chief_49f79d0151"};
        string[] names={"ConeMechanic","TrafficPatrol","TollgateChief"};
        var reports=new List<object>();
        for(int i=0;i<names.Length;i++)
        {
            string source="outputs/highway-enemies-2026-09-10/"+sourceNames[i];
            string assetName=names[i];
            string folder="Assets/ShooterSurvival/Models/Highway/"+names[i];
            Directory.CreateDirectory(folder);
            string fbx=folder+"/Highway_"+assetName+".fbx";
            if(!File.Exists(fbx))File.Copy(source+"/model.fbx",fbx);
            foreach(string texture in new[]{"BaseColor","Normal","Roughness","Metallic"})
                if(!File.Exists(folder+"/"+texture+".png"))File.Copy(source+"/textures/"+texture+".png",folder+"/"+texture+".png");
            AssetDatabase.Refresh();
            var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;importer.SaveAndReimport();
            var normalImporter=(TextureImporter)AssetImporter.GetAtPath(folder+"/Normal.png");normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();
            string materialPath=folder+"/Highway_"+assetName+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=names[i]};AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/BaseColor.png"));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Normal.png"));material.EnableKeyword("_NORMALMAP");
            material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.3f);material.SetFloat("_Metallic",.05f);EditorUtility.SetDirty(material);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            try
            {
                instance.name="Highway_"+assetName+"_Visual";
                var renderers=instance.GetComponentsInChildren<Renderer>(true);
                foreach(var r in renderers)r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();
                string prefab=folder+"/"+instance.name+".prefab";PrefabUtility.SaveAsPrefabAsset(instance,prefab);
                var meshes=instance.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh).ToArray();
                reports.Add(new{name=assetName,fbx,prefab,vertices=meshes.Sum(m=>m.vertexCount),triangles=meshes.Sum(m=>m.triangles.Length/3),rigged=false});
            }
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        AssetDatabase.SaveAssets();return reports;
    }
}
