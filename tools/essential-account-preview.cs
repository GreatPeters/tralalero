using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;
using IndianOceanAssets.ShooterSurvival.Account;

public static class EssentialAccountPreview
{
    const string Snapshot = "outputs/essential-proposals-2026-10-06/ui-preview-preferences-v2.json";
    static Type JsonType=>AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
    static string Json(object value)=>(string)JsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    static Rows ReadRows(string text)=>(Rows)JsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{text,typeof(Rows)});
    [Serializable] public sealed class Row { public string key, kind, value; public bool existed; }
    [Serializable] public sealed class Rows { public Row[] rows; }
    static Dictionary<string,string> Keys()
    {
        var keys = GameProgressStore.Keys();
        foreach(var key in new[]{"play_account_owner","play_account_base","play_account_deletion_pending","analytics_active_round_v1","analytics_pending_events_v1"}) keys[key]="string";
        foreach(var key in new[]{"play_account_auto_login","soundEnabled","vibrationEnabled","analytics_collection_enabled_v1"}) keys[key]="int";
        foreach(var key in new[]{"moveSensitivity","soundVolume"})keys[key]="float";
        return keys;
    }
    public static object Preferences(bool restore)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        if(!restore)
        {
            if(File.Exists(Snapshot))throw new Exception("Preserve original snapshot");
            var rows=Keys().Select(k=>new Row{key=k.Key,kind=k.Value,existed=PlayerPrefs.HasKey(k.Key),value=k.Value=="int"?PlayerPrefs.GetInt(k.Key).ToString(CultureInfo.InvariantCulture):k.Value=="float"?PlayerPrefs.GetFloat(k.Key).ToString("R",CultureInfo.InvariantCulture):PlayerPrefs.GetString(k.Key)}).ToArray();
            string text=Json(new Rows{rows=rows});
            var decoded=ReadRows(text)?.rows;
            if(decoded==null||decoded.Length!=rows.Length||rows.Where((row,i)=>row.key!=decoded[i].key||row.kind!=decoded[i].kind||row.existed!=decoded[i].existed||row.value!=decoded[i].value).Any())throw new Exception("Round-trip snapshot verification failed");
            File.WriteAllText(Snapshot,text);
            return new { captured=rows.Length };
        }
        var before=ReadRows(File.ReadAllText(Snapshot));
        if(before?.rows==null||before.rows.Length<80)throw new Exception("Incomplete snapshot");
        var expected=Keys();
        if(before.rows.Select(r=>r.key).Distinct().Count()!=before.rows.Length||before.rows.Any(r=>r.key==null||!expected.TryGetValue(r.key,out var kind)||kind!=r.kind||r.value==null))throw new Exception("Unexpected snapshot keys or types");
        foreach(var row in before.rows)
        {
            if(!row.existed)PlayerPrefs.DeleteKey(row.key);
            else if(row.kind=="int")PlayerPrefs.SetInt(row.key,int.Parse(row.value,CultureInfo.InvariantCulture));
            else if(row.kind=="float")PlayerPrefs.SetFloat(row.key,float.Parse(row.value,CultureInfo.InvariantCulture));
            else PlayerPrefs.SetString(row.key,row.value);
        }
        PlayerPrefs.Save();
        var mismatches=before.rows.Where(row=>row.existed!=PlayerPrefs.HasKey(row.key)||(row.existed&&(row.kind=="int"?PlayerPrefs.GetInt(row.key).ToString(CultureInfo.InvariantCulture):row.kind=="float"?PlayerPrefs.GetFloat(row.key).ToString("R",CultureInfo.InvariantCulture):PlayerPrefs.GetString(row.key))!=row.value)).Select(row=>row.key).ToArray();
        if(mismatches.Length>0)throw new Exception("Preference restoration failed: "+string.Join(",",mismatches));
        string cache=Path.Combine(Application.persistentDataPath,"PlayAccount",GameProgressSnapshot.Hash("__preview_account__")+".json");
        if(File.Exists(cache))File.Delete(cache);
        return new { restored=before.rows.Length, mismatches, previewOnly=true };
    }
    public static object RecoverPreviewKeys()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        if(PlayerPrefs.GetString("play_account_owner")!="__preview_account__")throw new Exception("Only the known preview account may be removed");
        foreach(var key in new[]{"play_account_owner","play_account_base","play_account_auto_login"})PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        string cache=Path.Combine(Application.persistentDataPath,"PlayAccount",GameProgressSnapshot.Hash("__preview_account__")+".json");
        if(File.Exists(cache))File.Delete(cache);
        return new{previewKeysRemoved=true,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),limitation="The first ephemeral JsonUtility snapshot wrote {} and cannot prove restoration of all original keys. Only keys introduced by this preview are cleared; no older state is guessed."};
    }
    sealed class PreviewBackend:IPlayProgressBackend
    {
        public string PlayerId=>"__preview_account__";
        public string DisplayName=>"테스트 계정";
        public bool IsAvailable=>true;
        public bool IsAuthenticated=>true;
        public event Action<GameProgressSnapshot,GameProgressSnapshot,Action<bool>> Conflict{add{}remove{}}
        public void Authenticate(bool manual,Action<bool,string> done)=>done(true,null);
        public void Read(Action<ProgressResult> done)=>done(new ProgressResult{success=true});
        public void Write(GameProgressSnapshot snapshot,string expected,Action<ProgressResult> done)=>done(new ProgressResult{success=true,snapshot=snapshot});
        public void Delete(Action<bool,string> done)=>throw new Exception("Native UI preview must never delete real data");
    }
    public static object Show(string state)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        var settings=UnityEngine.Object.FindFirstObjectByType<HarborSettingsPanel>(FindObjectsInactive.Include);
        settings.Open(false);
        var account=settings.GetComponent<HarborAccountPanel>();
        if(state=="login")account.loginButton.onClick.Invoke();
        if(state=="deletion")
        {
            PlayAccountService.Instance.Initialize(new PreviewBackend(),_=>{});
            PlayAccountService.Instance.Login();
            account.Refresh();account.deleteButton.onClick.Invoke();
        }
        if(state=="cancel")account.secondButton.onClick.Invoke();
        return new{state,service=PlayAccountService.Instance.State.ToString(),message=PlayAccountService.Instance.Message,dialog=account.dialog.activeSelf,run=TimeManager.isGameRunning,conditions="Actual native Options UI. Preview-only fake backend for deletion confirmation, no deletion is performed."};
    }
    public static object Capture(string output, int width, int height)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        var camera=Camera.main;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var old=canvases.Select(c=>new{canvas=c,c.worldCamera,c.planeDistance}).ToArray();
        var previous=camera.targetTexture;var active=RenderTexture.active;var mask=camera.cullingMask;
        var target=new RenderTexture(width,height,24);
        try
        {
            camera.targetTexture=target;camera.cullingMask=mask|(1<<LayerMask.NameToLayer("UI"));
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.5f;}
            Canvas.ForceUpdateCanvases();
            camera.Render();RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(output,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            foreach(var c in old){c.canvas.renderMode=RenderMode.ScreenSpaceOverlay;c.canvas.worldCamera=c.worldCamera;c.canvas.planeDistance=c.planeDistance;}
            camera.targetTexture=previous;camera.cullingMask=mask;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);Canvas.ForceUpdateCanvases();
        }
        return new{output,conditions="Native Unity camera-rendered current Play UI, overlays temporarily routed to this camera and immediately restored. This avoids a stale minimized GameView backbuffer; it is not a physical-screen capture."};
    }
}
