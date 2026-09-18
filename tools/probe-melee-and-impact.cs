using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

public static class MeleeImpactProbe
{
    public static object Begin(string label = "melee-final")
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
        OpeningStoryUI.Instance?.Skip();
        Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        if (!TimeManager.isGameRunning) throw new InvalidOperationException("Run must be active");
        var player = Object.FindFirstObjectByType<PlayerScript>();
        player.movement = false; player.canShoot = false;
        typeof(PlayerScript).GetField("currentForwardMoveSpeed",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(player,0f);
        string folder = "tmp/combat-feedback-2026-09-14/" + label;
        if (Directory.Exists(folder)) throw new InvalidOperationException("Preserve evidence");
        Directory.CreateDirectory(folder);
        var stage = new GameObject("Melee inspection stage");
        var controllers = new EnemyEventController[2];
        for (int i = 0; i < 2; i++)
        {
            string name = i == 0 ? "Enemy_Woman" : "Enemy_YllowMan_Sword";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/"+name+".prefab");
            var go = Object.Instantiate(prefab,player.transform.position + player.transform.forward*12 + player.transform.right*(i==0?-2.5f:2.5f),player.transform.rotation,stage.transform);
            go.name=name;
            go.SetActive(true);
            foreach (Transform child in go.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=31;
            foreach (var canvas in go.GetComponentsInChildren<Canvas>(true)) canvas.enabled=false;
            var target = new GameObject(name+" target");target.transform.SetParent(stage.transform);
            target.transform.position=go.transform.position+player.transform.forward*2.2f;
            controllers[i]=go.GetComponent<EnemyEventController>();
            controllers[i].TargetPoint=target.transform;controllers[i].MoveSpeed=1.8f;
            controllers[i].EventMode=EnemyEventMode.PatrolBetweenStartAndTarget;
        }
        var cameraObject = new GameObject("Melee inspection camera"); cameraObject.transform.SetParent(stage.transform);
        var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.2f,.25f);
        var center=player.transform.position+player.transform.forward*13+Vector3.up*1.5f;
        camera.transform.position=center-player.transform.forward*10+Vector3.up*4;
        camera.transform.LookAt(center);camera.fieldOfView=42;
        float elapsed=0;int sample=0,last=-1;
        float[] times={.1f,.5f,1.1f,1.6f,2.1f,2.7f,3.4f,4.3f};
        var report=new System.Text.StringBuilder();
        EditorApplication.CallbackFunction tick=null;
        tick=()=>{
            if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}
            if(EditorApplication.isPaused||Time.frameCount==last)return;
            last=Time.frameCount;elapsed+=Time.deltaTime;
            if(elapsed<times[sample])return;
            if(sample==0){ foreach(var controller in controllers) if(!controller.ActivateFromSpot()) throw new InvalidOperationException("Sample activation failed"); }
            var texture=RenderTexture.GetTemporary(1100,650,24);
            var old=RenderTexture.active;camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
            var image=new Texture2D(1100,650,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1100,650),0,0);image.Apply();
            File.WriteAllBytes(folder+"/phase-"+sample+".png",image.EncodeToPNG());
            Object.Destroy(image);camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(texture);
            foreach(var controller in controllers){var animator=controller.GetComponentInChildren<Animator>();report.AppendLine(sample+" "+controller.name+" "+controller.RuntimeState+" "+animator.GetCurrentAnimatorStateInfo(0).normalizedTime+" position="+controller.transform.position);}
            sample++;
            if(sample==times.Length){EditorApplication.update-=tick;File.WriteAllText(folder+"/result.txt",report.ToString());}
        };
        EditorApplication.update+=tick;
        return folder;
    }
}
