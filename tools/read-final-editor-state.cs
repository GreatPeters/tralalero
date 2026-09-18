if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Expected Edit Mode");
foreach(var window in UnityEngine.Resources.FindObjectsOfTypeAll<NoryangjinMapToolWindow>())window.SetPaletteConcept(0);
int checkedKeys=0;
foreach(var line in System.IO.File.ReadAllLines("tmp/backups/skins-progression-2026-09-12/playerprefs.tsv")){
 var f=line.Split('\t');bool exists=bool.Parse(f[2]);if(UnityEngine.PlayerPrefs.HasKey(f[0])!=exists)throw new System.InvalidOperationException("Presence drift "+f[0]);
 if(exists){if(f[1]=="int"&&UnityEngine.PlayerPrefs.GetInt(f[0])!=int.Parse(f[3],System.Globalization.CultureInfo.InvariantCulture))throw new System.InvalidOperationException(f[0]);if(f[1]=="float"&&UnityEngine.PlayerPrefs.GetFloat(f[0])!=float.Parse(f[3],System.Globalization.CultureInfo.InvariantCulture))throw new System.InvalidOperationException(f[0]);if(f[1]=="string"&&UnityEngine.PlayerPrefs.GetString(f[0])!=System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(f[3])))throw new System.InvalidOperationException(f[0]);}checkedKeys++;
}
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();return new{scene=scene.name,scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying,timeScale=UnityEngine.Time.timeScale,verifiedKeys=checkedKeys,coin=UnityEngine.PlayerPrefs.GetInt("coin"),jewel=UnityEngine.PlayerPrefs.GetInt("jewel")};
