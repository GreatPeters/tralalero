using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static partial class HarborGameUIInstaller
{
    public const string ChapterThemePath="Assets/ShooterSurvival/UI/ChapterThemes/";

    public static string ChapterPlaqueAssetPath(int chapter) => chapter switch
    {
        1 => FaithfulPath+"LobbyPlaque.png",
        2 => ChapterThemePath+"HighwayPlaque.png",
        3 => ChapterThemePath+"RestStopPlaque.png",
        _ => throw new ArgumentOutOfRangeException(nameof(chapter),chapter,"Author a chapter theme before applying its lobby.")
    };

    private static void ImportChapterPlaques()
    {
        foreach(string name in new[]{"HighwayPlaque","RestStopPlaque"}){
            string path=ChapterThemePath+name+".png";if(!File.Exists(path))throw new FileNotFoundException("Missing chapter UI artwork",path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=Vector4.zero;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
    }

    private static void ApplyChapterPlaque(CanvasScript canvas)
    {
        int chapter=canvas.GetComponent<ChapterProgression>()?.chapter??1;
        string path=ChapterPlaqueAssetPath(chapter);var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(sprite==null)throw new InvalidOperationException("Import chapter plaque first: "+path);
        var title=canvas.transform.Find("UI/Main/Center/ChapterTitle");var image=title.GetComponent<Image>();
        image.sprite=sprite;image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=false;image.color=Color.white;
        var fit=GetOrAdd<AspectRatioFitter>(title.gameObject);fit.aspectMode=AspectRatioFitter.AspectMode.WidthControlsHeight;fit.aspectRatio=sprite.rect.width/sprite.rect.height;
        EditorUtility.SetDirty(image);EditorUtility.SetDirty(fit);
        if(PrefabUtility.IsPartOfPrefabInstance(image))PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        if(PrefabUtility.IsPartOfPrefabInstance(fit))PrefabUtility.RecordPrefabInstancePropertyModifications(fit);
    }

    public static object ApplyRoadChapterPlaques()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        if(!EditorSceneManager.SaveOpenScenes())throw new InvalidOperationException("Preserve pending scene edits first");
        ImportChapterPlaques();var setup=EditorSceneManager.GetSceneManagerSetup();
        try{foreach(string name in new[]{"HighWay","RestStop"}){
            var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
            ApplyChapterPlaque(scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<CanvasScript>());
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return "Highway motorway plaque and RestStop service-area plaque saved";
    }
}
