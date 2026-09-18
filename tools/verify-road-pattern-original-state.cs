if(UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("Original project required");
var differences=new System.Collections.Generic.List<string>();
var lines=System.IO.File.ReadAllLines("tmp/backups/road-patterns-2026-09-13/original-playerprefs.tsv");
foreach(var line in lines){var c=line.Split('\t');string current=c[1]=="string"?System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(UnityEngine.PlayerPrefs.GetString(c[0]))):c[1]=="int"?UnityEngine.PlayerPrefs.GetInt(c[0]).ToString(System.Globalization.CultureInfo.InvariantCulture):UnityEngine.PlayerPrefs.GetFloat(c[0]).ToString("R",System.Globalization.CultureInfo.InvariantCulture);if(bool.Parse(c[2])!=UnityEngine.PlayerPrefs.HasKey(c[0])||current!=c[3])differences.Add(c[0]);}
GameDataWorkbookEditor.ValidateRuntimeArchiveOrThrow();
return new{keys=lines.Length,unchanged=differences.Count==0,differences,coins=UnityEngine.PlayerPrefs.GetInt("coin"),jewels=UnityEngine.PlayerPrefs.GetInt("jewel"),archiveVerified=true,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name};
