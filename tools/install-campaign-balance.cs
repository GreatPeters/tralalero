using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

public static class InstallCampaignBalance
{
    public static object Main(string revision)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        string folder="outputs/campaign-balance-2026-09-23/"+revision;
        string report=File.ReadAllText(folder+"/verification.json");
        string Recorded(string key)=>Regex.Match(report,"\""+key+"\"\\s*:\\s*\"([a-f0-9]{64})\"").Groups[1].Value;
        string Hash(byte[] bytes){using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        string source=GameDataWorkbook.GetEditorSourceAbsolutePath(),archive="Assets/ShooterSurvival/Resources/GameData/Data.bytes";
        var before=File.ReadAllBytes(source);var packaged=File.ReadAllBytes(archive);var candidate=File.ReadAllBytes(folder+"/Data.xlsx");
        string installed="tmp/campaign-balance-2026-09-23/installed-hashes.txt";
        bool known=Hash(before)==Recorded("sourceSha256") || File.Exists(installed)&&Array.IndexOf(File.ReadAllLines(installed),Hash(before))>=0;
        if(!known||Hash(candidate)!=Recorded("candidateSha256"))throw new InvalidOperationException("Concurrent workbook change or unverified candidate");
        GameDataWorkbookSchema.Validate(candidate);
        string backup="tmp/campaign-balance-2026-09-23/workbook/before-install-"+Hash(before).Substring(0,12)+".xlsx";
        if(!File.Exists(backup))File.WriteAllBytes(backup,before);
        try
        {
            File.WriteAllBytes(source,candidate);
            AssetDatabase.ImportAsset("Assets/ShooterSurvival/GameData/Editor/Data.xlsx",ImportAssetOptions.ForceSynchronousImport);
            GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(false);GameDataWorkbookEditor.ValidateRuntimeArchiveOrThrow();
            EncounterPlacementTables.Reload();EnvironmentVariableTables.Reload();UpgradeTables.Reload();
            File.AppendAllText(installed,Hash(candidate)+Environment.NewLine);
        }
        catch{File.WriteAllBytes(source,before);File.WriteAllBytes(archive,packaged);throw;}
        return new{revision,sha256=Hash(candidate),archiveVerified=true};
    }
}
