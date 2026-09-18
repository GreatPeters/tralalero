string path="tmp/backups/shop-fidelity-2026-09-17/rank-prefs.tsv";
if(System.IO.File.Exists(path))throw new InvalidOperationException("Do not replace task baseline");
var keys=Enumerable.Range(1,3).SelectMany(i=>new[]{ChapterUpgradeService.LevelKey(i),ChapterUpgradeService.OwnedKey(i)});
System.IO.File.WriteAllLines(path,keys.Select(k=>$"{k}\tint\t{PlayerPrefs.HasKey(k)}\t{PlayerPrefs.GetInt(k)}"));return path;
