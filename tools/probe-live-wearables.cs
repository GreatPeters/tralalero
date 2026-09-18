if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var customizer=player.GetComponent<PlayerCosmeticCustomizer>();var catalog=customizer.visuals;
var phase=SessionState.GetString("Wearables.ProbePhase","before-live");
var folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/wearable-placement-2026-09-15/"+phase;System.IO.Directory.CreateDirectory(folder);
var data=new System.Collections.Generic.List<object>();
System.Collections.IEnumerator Probe()
{
    UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();
    if(phase=="final-live")
    {
        UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>().PlayerPressedStartButton();
        IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;player.canShoot=false;
    }
    var selections=phase=="final-live"?new[]{"hat_bucket","hat_cap","hat_tophat","hat_pirate","hat_relic","hat_goggles","hat_diver"}.Select(hat=>(skin:"skin_original",hat)).ToArray():new[]{(skin:"skin_original",hat:"hat_bucket"),(skin:"skin_original",hat:"hat_cap"),(skin:"skin_diver",hat:"hat_none")};
    foreach(var selection in selections)
    {
        CosmeticAppearance.Apply(customizer.modelRoot,catalog,selection.skin,"shoes_original",selection.hat);yield return new WaitForSecondsRealtime(.3f);
        var live=customizer.modelRoot;var renderer=live.GetComponentInChildren<SkinnedMeshRenderer>();var head=renderer.bones.Single(b=>b.name=="head");
        if(phase=="final-live")
        {
            var mounts=live.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="__CosmeticHat"&&t.gameObject.activeSelf).ToArray();
            if(mounts.Length!=1||mounts[0].parent!=head||Vector3.Distance(mounts[0].localPosition,catalog.hatLocalPosition)>.0000001f)throw new Exception("Head attachment drift: "+selection.hat);
            if(!player.sharkAnim.GetCurrentAnimatorStateInfo(0).IsName("Walk"))throw new Exception("Walking pose missing");
        }
        data.Add(new{selection.skin,selection.hat,modelScale=live.lossyScale.ToString("F4"),headScale=head.lossyScale.ToString("F4"),headRotation=head.localRotation.eulerAngles.ToString("F3")});
        var stage=new GameObject("Live wearable snapshot");stage.transform.position=new Vector3(10000,10000,10000);
        var clone=UnityEngine.Object.Instantiate(live.gameObject,stage.transform,false);clone.transform.localPosition=Vector3.zero;clone.transform.localRotation=Quaternion.identity;clone.transform.localScale=live.lossyScale;
        foreach(var a in clone.GetComponentsInChildren<Animator>(true))a.enabled=false;
        foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        foreach(var r in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true)){r.enabled=true;r.updateWhenOffscreen=true;}
        var cameraObject=new GameObject("Capture Camera");cameraObject.transform.SetParent(stage.transform,false);cameraObject.transform.localPosition=new Vector3(-3.7f,2.65f,6.3f);cameraObject.transform.LookAt(stage.transform.position+new Vector3(0,1.05f,0));
        var camera=cameraObject.AddComponent<Camera>();camera.enabled=true;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.fieldOfView=32;
        var rt=new RenderTexture(1024,768,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
        yield return null;yield return null;
        CosmeticPresentationBuilder.Save(rt,folder+"/"+selection.skin+"-"+selection.hat+".png");camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(stage);
        yield return null;
    }
    System.IO.File.WriteAllLines(folder+"/measurements.txt",data.Select(row=>row.ToString()));
    if(phase=="final-live")
    {
        var music=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(a=>a.name=="Music");
        if(!music.loop||music.clip!=Resources.Load<AudioClip>("Audio/Mobile/music")||!music.isPlaying)throw new Exception("Selected BGM is not playing");
        System.IO.File.WriteAllText(folder+"/verification.txt","PASS: seven headwear items stay attached during Walk\nPASS: no duplicate mounts\nPASS: selected common BGM is playing in a loop\n");
    }
}
player.StartCoroutine(Probe());return "scheduled";
