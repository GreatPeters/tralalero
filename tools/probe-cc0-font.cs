var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Font>("Assets/ShooterSurvival/Fonts/JigmoGameUI/JigmoGameUI.ttf");
var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(GameUIFontBuilder.Path);
var loaded=UnityEngine.TextCore.LowLevel.FontEngine.LoadFontFace(source,72);
return new { source=source!=null, dynamic=source.dynamic, sourceHasA=source.HasCharacter('A'), load=loaded.ToString(),
    sourceHasHangul=source.HasCharacter('가'),
    font=font!=null, sourceAssigned=font!=null&&font.sourceFontFile!=null, material=font!=null&&font.material!=null,
    face=font.faceInfo, internalLoad=typeof(TMPro.TMP_FontAsset).GetMethod("LoadFontFace",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(font,null).ToString(),
    population=font!=null?font.atlasPopulationMode.ToString():"", atlas=font?.atlasTextures?.Select(t=>new {valid=t!=null,w=t!=null?t.width:0,h=t!=null?t.height:0,readable=t!=null&&t.isReadable}).ToArray() };
