using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using UnityEditor;

public static class VerifyCampaignFinalState
{
    public static object Main(string Folder="tmp/campaign-balance-2026-09-23")
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        int count=0;
        foreach(var line in File.ReadAllLines(Folder+"/before-prefs.tsv"))
        {
            var p=line.Split('\t');bool exists=bool.Parse(p[2]);
            if(PlayerPrefs.HasKey(p[0])!=exists)throw new Exception("Key presence changed: "+p[0]);
            if(exists)
            {
                string actual=p[1]=="int"?PlayerPrefs.GetInt(p[0]).ToString(CultureInfo.InvariantCulture):p[1]=="float"?PlayerPrefs.GetFloat(p[0]).ToString("R",CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(p[0])));
                if(actual!=p[3])throw new Exception("Key value changed: "+p[0]);
            }
            count++;
        }
        foreach(var line in File.ReadAllLines(Folder+"/extra-prefs.tsv"))
        {
            var p=line.Split('\t');bool exists=bool.Parse(p[1]);
            if(PlayerPrefs.HasKey(p[0])!=exists || exists&&PlayerPrefs.GetInt(p[0])!=int.Parse(p[2]))throw new Exception("Extra key changed: "+p[0]);
            count++;
        }
        var view=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var window=Resources.FindObjectsOfTypeAll(view).Cast<EditorWindow>().First();
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        int index=int.Parse(File.ReadAllText(Folder+"/gameview-before.txt").Split('\t')[0]);
        view.GetProperty("selectedSizeIndex",flags).SetValue(window,index);window.Repaint();
        if(File.Exists(Folder+"/gameview-maximized-before.txt"))window.maximized=bool.Parse(File.ReadAllText(Folder+"/gameview-maximized-before.txt"));
        Time.captureDeltaTime=0;Time.timeScale=1;EditorApplication.isPaused=false;
        if(SessionState.GetBool("NoryangjinMapTool.TestPower9999",false)||SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false)||SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1)!=1)throw new Exception("Test settings not restored");
        var status=GameDataWorkbookEditor.GetRuntimeArchiveStatus(out var detail);
        if(status!=GameDataRuntimeArchiveStatus.Current)throw new Exception(detail);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Dirty scene");
        using var sha=System.Security.Cryptography.SHA256.Create();
        string workbookHash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath()))).Replace("-","").ToLowerInvariant();
        var result=new{preferencesVerified=count,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),gameViewIndex=index,scene=scene.path,sceneDirty=scene.isDirty,timeScale=Time.timeScale,captureDeltaTime=Time.captureDeltaTime,archive=status.ToString(),workbookHash};
        string json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result});
        File.WriteAllText(Folder+"/final-state.json",json);return result;
    }
}
