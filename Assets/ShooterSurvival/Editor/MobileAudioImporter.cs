using System;
using UnityEditor;
using UnityEngine;

public static class MobileAudioImporter
{
    public static object Apply()
    {
        int count=0;
        foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/ShooterSurvival/Resources/Audio/Mobile"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(AudioImporter)AssetImporter.GetAtPath(path);
            bool music=path.EndsWith("/music.ogg",StringComparison.Ordinal);
            bool ambience=path.EndsWith("/harbor.ogg",StringComparison.Ordinal)||path.EndsWith("/traffic.ogg",StringComparison.Ordinal);
            importer.forceToMono=!music&&!ambience;importer.loadInBackground=music;
            var settings=importer.defaultSampleSettings;
            settings.loadType=music?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=music||ambience?AudioCompressionFormat.Vorbis:AudioCompressionFormat.ADPCM;
            settings.quality=.45f;settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=music?44100u:22050u;
            importer.defaultSampleSettings=settings;importer.SetOverrideSampleSettings("Android",settings);importer.SaveAndReimport();count++;
        }
        return new { clips=count,voices=GameAudioService.VoiceLimit };
    }
}
