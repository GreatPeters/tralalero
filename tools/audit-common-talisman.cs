using System;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class AuditCommonTalisman
{
    public static object Main()
    {
        if(UnityEditor.EditorApplication.isPlaying)throw new Exception("Edit Mode only");
        var lines=new System.Collections.Generic.List<string>();
        foreach(var name in BonusTalismanInstaller.SceneNames)
        {
            string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var walls=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WallScript>(true)).Where(w=>w.wallType==WallType.BuffWall).ToArray();
                foreach(var wall in walls)
                {
                    var p=wall.GetComponent<BonusTalismanPresentation>();
                    if(p==null||p.Visual==null)throw new Exception("Missing talisman "+name+"/"+wall.name);
                    var altar=wall.GetComponentInParent<AuthoredBonusWall>();
                    if(altar!=null && altar.transform.Find("ChoiceAltarVisual").gameObject.activeSelf)throw new Exception("Legacy altar visible");
                }
                lines.Add(name+": "+walls.Length+" positive bonuses with saved visuals; legacy altars hidden; dirty="+scene.isDirty);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        File.WriteAllLines("map-concepts/common-talisman-applied-2026-09-20/scene-audit.txt",lines);
        return lines;
    }
}
