using System;
using System.IO;
using GooglePlayGames;
using UnityEditor;

public static class ConfigurePlayAccount
{
    public static object Main(string gameId)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Edit Mode required");
        string backup = "outputs/play-games-link-2026-10-06/before-game-id";
        Directory.CreateDirectory(backup);
        foreach (string path in new[] {
            "Assets/GooglePlayGames/Resources/PlayGamesSettings.asset",
            "Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml" })
        {
            string destination = Path.Combine(backup, Path.GetFileName(path));
            if (File.Exists(path) && !File.Exists(destination)) File.Copy(path, destination);
        }
        var result = PlayAccountSetup.Configure(gameId);
        var settings = PlayGamesSettings.LoadInstance();
        if (settings == null || settings.AppId != gameId)
            throw new InvalidOperationException("Game ID readback mismatch");
        return result;
    }
}
