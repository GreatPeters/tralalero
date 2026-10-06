using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Chapters45InspectFlaggers
{
    public static object Main()
    {
        var states = new[] { "idle", "walk", "run", "attack_loop", "attack_once", "die", "signal", "scared" };
        var actors = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(a => a.runtimeAnimatorController != null && AssetDatabase.GetAssetPath(a.runtimeAnimatorController).IndexOf("C08", StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(a => new { name = a.name, parent = a.transform.parent != null ? a.transform.parent.name : "", active = a.gameObject.activeInHierarchy,
                controller = AssetDatabase.GetAssetPath(a.runtimeAnimatorController), human = a.isHuman, initialized = a.isInitialized,
                avatar = a.avatar != null ? AssetDatabase.GetAssetPath(a.avatar) : "", avatarHuman = a.avatar != null && a.avatar.isHuman,
                states = states.Select(s => new { name = s, present = a.isInitialized && a.HasState(0, Animator.StringToHash(s)) }).ToArray(),
                clips = a.runtimeAnimatorController.animationClips.Select(c => new { c.name, path = AssetDatabase.GetAssetPath(c), human = c.isHumanMotion, c.length, loop = c.isLooping }).ToArray() }).ToArray();
        string output = "outputs/chapters45-2026-10-02/final-verification-20261001T210533Z/flagger-native-before.json";
        var json = (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert")
            .GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new object[] { new { playing = EditorApplication.isPlaying, actors } });
        File.WriteAllText(output, json);
        int stopped = 0;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("Chapters45Playtest");
            if (type == null) continue;
            var active = type.GetField("active", BindingFlags.Static | BindingFlags.NonPublic);
            var finished = type.GetField("finished", BindingFlags.Static | BindingFlags.NonPublic);
            if (active?.GetValue(null) is bool on && on && finished?.GetValue(null) is bool done && !done)
            {
                type.GetMethod("Finish", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { "paused-for-demonstrated-flagger-animation-fix" });
                stopped++;
            }
        }
        return new { output, found = actors.Length, stopped };
    }
}
