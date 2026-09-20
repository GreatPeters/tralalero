using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class InspectTalismanBonuses
{
    public static object Main()
    {
        var walls = UnityEngine.Object.FindObjectsByType<WallScript>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        string Describe(Transform t) => string.Join("/", Ancestors(t)) + " pos=" + t.localPosition + " scale=" + t.localScale + " rot=" + t.localEulerAngles + " active=" + t.gameObject.activeSelf;
        var first = walls.FirstOrDefault(w => w.GetComponentInParent<AuthoredBonusWall>() != null);
        var root = first != null ? first.GetComponentInParent<AuthoredBonusWall>().transform : null;
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/ShooterSurvival/Prefabs/Walls"}).Select(AssetDatabase.GUIDToAssetPath)
            .Select(p => new { path=p, asset=AssetDatabase.LoadAssetAtPath<GameObject>(p) })
            .Where(p=>p.asset.GetComponentInChildren<WallScript>(true)!=null)
            .Select(p=>new{p.path,walls=p.asset.GetComponentsInChildren<WallScript>(true).Select(w=>new{w.name,type=w.wallType.ToString(),buff=w.buffType.ToString(),w.isRandom,rarity=w.rarity.ToString()}).ToArray()}).ToArray();
        return new {playing=EditorApplication.isPlaying,wallCount=walls.Length,root=root!=null?Describe(root):null,
            hierarchy=root!=null?root.GetComponentsInChildren<Transform>(true).Select(Describe).ToArray():null,
            canvases=root!=null?root.GetComponentsInChildren<Canvas>(true).Select(c=>new {c.name,mode=c.renderMode.ToString(),rect=c.GetComponent<RectTransform>().sizeDelta.ToString()}).ToArray():null,
            prefabs,icons=AssetDatabase.FindAssets("t:Sprite",new[]{"Assets/ShooterSurvival/Resources/WallBonusIcons"}).Select(AssetDatabase.GUIDToAssetPath).ToArray()};
    }
    static string[] Ancestors(Transform t) { return t.parent==null?new[]{t.name}:Ancestors(t.parent).Concat(new[]{t.name}).ToArray(); }
}
