using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NoryangjinReleaseSetup
{
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var active = SceneManager.GetActiveScene();
        if (active.isDirty) throw new InvalidOperationException("Preserve the dirty scene before applying UI");
        var reports = new System.Collections.Generic.List<object>();
        foreach (string name in new[] { "Noryangjin_MapTool_Mode", "Noryangjin_MapTool_Mode_SR18" })
        {
            string path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
            var scene = EditorSceneManager.OpenScene(path);
            string backup = "tmp/backups/sr18-release-20260910/before-ui-" + name + ".unity";
            if (!File.Exists(backup)) EditorSceneManager.SaveScene(scene, backup, true);
            UpgradeShopReferenceSetup.ConfigureOpenScene();
            if (name.EndsWith("SR18"))
            {
                var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
                foreach (int index in new[] { 10, 19 })
                {
                    var e = map.Find("Enemies").GetComponentsInChildren<EnemyEventController>().Single(e => e.name.StartsWith($"SR18_L_E{index:00}_"));
                    Vector3 center = (e.transform.position + e.TargetPoint.position) * .5f;
                    Undo.RecordObjects(new UnityEngine.Object[] { e, e.transform, e.TargetPoint }, "Cross-lane patrol");
                    var before = e.transform.position;
                    e.PatrolAcrossRoad = true; e.MoveSpeed = 1.4f; e.MoveAnimation = EnemyMoveAnimation.Walk;
                    e.transform.position = center - e.transform.right; e.TargetPoint.position = center + e.transform.right;
                    e.RefreshPlacementAfterAuthoringChange(before, false);
                    foreach (var o in new UnityEngine.Object[] { e, e.transform, e.TargetPoint }) { EditorUtility.SetDirty(o); PrefabUtility.RecordPrefabInstancePropertyModifications(o); }
                }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            reports.Add(new { scene = name, backup });
        }
        return reports;
    }
}
