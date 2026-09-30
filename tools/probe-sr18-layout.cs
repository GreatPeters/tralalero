using System.IO;
using System.Linq;
using System.Text;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Read-only dump of the SR18 scene layout for Noryangjin revamp planning.
//   unity command run_script --file tools/probe-sr18-layout.cs --entry ProbeSr18Layout.Main
public static class ProbeSr18Layout
{
    public static object Main()
    {
        var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects())
        {
            sb.AppendLine($"ROOT {root.name} children={root.transform.childCount} comps={string.Join(",", root.GetComponents<Component>().Select(c => c.GetType().Name))}");
            var groups = root.transform.Cast<Transform>().GroupBy(t => Prefix(t.name)).OrderByDescending(g => g.Count()).Take(40);
            foreach (var g in groups) sb.AppendLine($"  {g.Key} x{g.Count()} e.g. {g.First().name}");
        }
        sb.AppendLine("TURNSPOTS");
        foreach (var t in Object.FindObjectsByType<NoryangjinTurnSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(t => t.name))
            sb.AppendLine($"  {t.name} {V(t.transform.position)} yaw={t.transform.eulerAngles.y:F0}");
        sb.AppendLine("ENEMIES");
        foreach (var e in Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(e => e.name))
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(e.gameObject);
            sb.AppendLine($"  {e.name} {V(e.transform.position)} active={e.gameObject.activeSelf} parent={e.transform.parent?.name} prefab={(src != null ? AssetDatabase.GetAssetPath(src) : "-")} scale={e.transform.lossyScale.y:F2}");
        }
        var player = Object.FindFirstObjectByType<PlayerScript>();
        sb.AppendLine($"PLAYER {V(player.transform.position)} yaw={player.transform.eulerAngles.y:F0} speed");
        foreach (var cp in Object.FindObjectsByType<ChapterProgression>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sb.AppendLine($"PROGRESSION {cp.name} next={cp.nextScene}");
        Directory.CreateDirectory("tmp/noryangjin-revamp");
        File.WriteAllText("tmp/noryangjin-revamp/sr18-layout.txt", sb.ToString());
        return new { path = "tmp/noryangjin-revamp/sr18-layout.txt", lines = sb.ToString().Split('\n').Length };
    }

    static string Prefix(string n)
    {
        int cut = n.IndexOfAny("0123456789(".ToCharArray());
        return cut > 0 ? n.Substring(0, cut) : n;
    }
    static string V(Vector3 v) => $"({v.x:F1},{v.y:F1},{v.z:F1})";
}
