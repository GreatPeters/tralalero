// Run with the official Pipeline eval_file command, outside Play Mode.
if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
AssetDatabase.Refresh();
var paths = System.IO.Directory.GetFiles("Assets/ShooterSurvival/UI/HarborWorkshop", "*.png");
foreach (var file in paths)
{
    string path = file.Replace('\\', '/');
    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
    importer.textureType = TextureImporterType.Sprite;
    importer.spriteImportMode = SpriteImportMode.Single;
    importer.alphaIsTransparency = true;
    importer.mipmapEnabled = false;
    importer.npotScale = TextureImporterNPOTScale.None;
    importer.maxTextureSize = 2048;
    importer.textureCompression = TextureImporterCompression.Uncompressed;
    string name = System.IO.Path.GetFileNameWithoutExtension(path);
    importer.spriteBorder = name switch {
        "ParchmentPanel" => new Vector4(38,38,38,38),
        "WoodSign" => new Vector4(58,52,58,52),
        "GoldButton" => new Vector4(36,36,36,36),
        "WoodButton" => new Vector4(36,36,36,36),
        "NavigationTile" => new Vector4(68,68,68,68),
        "BrassPlaque" => new Vector4(48,48,48,48),
        "WalletPill" => new Vector4(64,40,64,40),
        "WindowFrame" => new Vector4(60,58,60,58),
        _ => Vector4.zero
    };
    importer.SaveAndReimport();
}
return new { imported = paths.Length, folder = "Assets/ShooterSurvival/UI/HarborWorkshop", slicedSurfaces = 8 };
