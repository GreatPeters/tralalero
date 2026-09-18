var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(GameUIFontBuilder.Path);
bool beforeA=font.HasCharacter('A'), beforeHangul=font.HasCharacter('가');
int before=font.characterTable.Count;
bool added=font.TryAddCharacters("A가",out string missing);
return new {before,beforeA,beforeHangul,added,missing,after=font.characterTable.Count,afterA=font.HasCharacter('A'),afterHangul=font.HasCharacter('가'),glyphs=font.glyphTable.Count};
