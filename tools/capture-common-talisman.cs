using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class CaptureCommonTalisman
{
    public static object Main(string mode = "inspect")
    {
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var camera=Camera.main;
        var walls=UnityEngine.Object.FindObjectsByType<WallScript>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        if(mode=="inspect")return new{running=TimeManager.isGameRunning,player=player.transform.position.ToString(),scale=player.transform.lossyScale.ToString(),camera=camera.transform.position.ToString(),rotation=camera.transform.eulerAngles.ToString(),
            renderers=player.transform.Find("Original").GetComponentsInChildren<SkinnedMeshRenderer>().Select(r=>new{r.name,center=r.bounds.center.ToString(),size=r.bounds.size.ToString()}).ToArray(),
            walls=walls.Take(2).Select(w=>new{w.name,position=w.transform.position.ToString(),visual=w.GetComponent<BonusTalismanPresentation>()?.Visual?.transform.position.ToString(),scale=w.GetComponent<BonusTalismanPresentation>()?.Visual?.transform.lossyScale.ToString()}).ToArray()};
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();
        canvas.StartCoroutine(Capture(mode,player,camera));
        return "Capture scheduled: "+mode;
    }
    static IEnumerator Capture(string mode,PlayerScript player,Camera camera)
    {
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();
        var story=UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);
        if(story!=null)story.Skip();
        canvas.PlayerPressedStartButton();
        yield return null;
        // Freeze gameplay movement while keeping actual run systems and pickup enabled.
        Time.timeScale=0;
        var tutorial=canvas.GetComponent<CoastalTutorialUI>();
        if(tutorial!=null && tutorial.panel!=null)tutorial.panel.gameObject.SetActive(false);
        foreach(var weapon in player.GetComponentsInChildren<WeaponScript>())weapon.enabled=false;
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
        var holder=player.transform.Find("Original");
        Vector3 forward=player.transform.forward;
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab");
        var root=UnityEngine.Object.Instantiate(prefab,player.transform.position+forward*10,Quaternion.identity);
        var wall=root.GetComponentInChildren<WallScript>(true);
        yield return null;
        wall.SetWallSprite();
        var visual=wall.GetComponent<BonusTalismanPresentation>().Visual;
        var folder="tmp/image-previews/common-talisman-applied-2026-09-20/"+(mode=="final"?"final/":"")+SceneManager.GetActiveScene().name;
        Directory.CreateDirectory(folder);
        yield return Shot(folder+"/01-idle.png");
        root.transform.position=player.transform.position+forward*3.0f;
        wall.SetWallSprite();
        var effect=BonusTalismanPickup.Spawn(visual,player);
        effect.enabled=false;
        root.SetActive(false);
        effect.Sample(.14f);yield return Shot(folder+"/02-unfold.png");
        effect.Sample(.34f);yield return Shot(folder+"/03-absorb.png");
        effect.Sample(.55f);yield return Shot(folder+"/04-body-pulse.png");
        effect.Sample(.7f);UnityEngine.Object.Destroy(effect.gameObject);
        yield return null;yield return Shot(folder+"/05-complete.png");
        UnityEngine.Object.Destroy(root);
        File.WriteAllText(folder+"/capture-complete.txt", "Native visual phases captured; gameplay frozen for deterministic inspection. Actual trigger/reward tests are separate.");
    }
    static IEnumerator Shot(string file)
    {
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(file);
        yield return new WaitForSecondsRealtime(.25f);
    }
}
