using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ResizeSeagullHazard
{
    const string Prefab="Assets/ShooterSurvival/Prefabs/Obstacle_Real/Seagull.prefab";
    const string Evidence="map-concepts/seagull-size-followup-2026-09-20";
    public static string Main()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.name!="Noryangjin_MapTool_Mode_SR18" || scene.isDirty)
            throw new InvalidOperationException("Clean SR18 Edit Mode required.");
        Directory.CreateDirectory(Evidence+"/before");
        Backup(Prefab,"Seagull.prefab");Backup(scene.path,"SR18.unity");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try{Configure(root.GetComponent<ObstacleStats>());PrefabUtility.SaveAsPrefabAsset(root,Prefab);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var birds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ObstacleStats>(true))
            .Where(o=>o.obstaclePattern==ObstaclePattern.Seagull).ToArray();
        foreach(var bird in birds)Configure(bird);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save scene.");
        AssetDatabase.SaveAssets();
        return $"Saved prefab and {birds.Length} placed bird: warning 2.808–3.744m, rig scale 4.095, trigger 28m, warning 1 second, descent 0.4 seconds.";
    }
    static void Configure(ObstacleStats stats)
    {
        // Latest request: bird +30%, warning diameter +20% from scale 3.15 / 3.12m.
        stats.balloon.localScale=Vector3.one*4.095f;
        stats.shadowStartScale=.27421875f;
        stats.shadowEndScale=.365625f;
        stats.triggerRadius=28f;
        stats.telegraphTime=1.0f;
        stats.dropTime=.4f;
        stats.dropHeight=6f;
        stats.seagullLandingOffset=.14625f;
        stats.shadowSprite.sharedMaterial=WarningMaterial(stats.shadowSprite.sprite.texture);
        stats.shadowSprite.transform.localScale=Vector3.one*stats.shadowStartScale;
        Record(stats);Record(stats.balloon);Record(stats.shadowSprite);Record(stats.shadowSprite.transform);
    }
    static Material WarningMaterial(Texture texture)
    {
        const string path="Assets/ShooterSurvival/Materials/Generated/Seagull_WarningDark.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null)
        {
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            if(shader==null)throw new InvalidOperationException("URP Unlit shader missing.");
            material=new Material(shader);AssetDatabase.CreateAsset(material,path);
        }
        // The source texture's strongest alpha is only 0.376. Keep its soft edge,
        // but bring the opaque core to approximately 0.83 without changing shared art.
        material.SetTexture("_BaseMap",texture);
        material.SetColor("_BaseColor",new Color(0,0,0,2.2f));
        material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);material.SetFloat("_Cull",0);
        material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
        material.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(material);
        return material;
    }
    static void Record(UnityEngine.Object target)
    {
        EditorUtility.SetDirty(target);
        if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
    static void Backup(string path,string name)
    {
        var destination=Evidence+"/before/"+name;
        if(!File.Exists(destination))File.Copy(path,destination);
    }
}
