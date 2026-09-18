using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

public static class InspectCombatActors
{
    public static object Main()
    {
        return Object.FindObjectsByType<EnemyEventController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(e => { var a = e.GetComponentInChildren<Animator>(true); var c = a?.runtimeAnimatorController as AnimatorOverrideController;
                return new { e.name, mode = e.EventMode.ToString(), animator = a?.name,
                    controller = c != null ? AssetDatabase.GetAssetPath(c) : "",
                    clips = c != null ? c.animationClips.Select(x => x.name + ":" + x.length).ToArray() : new string[0] }; }).ToArray();
    }
}
