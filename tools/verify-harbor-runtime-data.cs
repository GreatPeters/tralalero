if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var rows=EncounterPlacementTables.Rows.Where(r=>r.kind=="적 배치"&&r.scene==scene.name).ToArray();
var ids=rows.Select(r=>r.id).ToHashSet();var enemies=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyScript_space>(true)).Where(e=>ids.Contains(e.name)).ToDictionary(e=>e.name);
var lines=new System.Collections.Generic.List<string>();
foreach(var row in rows.Where(r=>r.id.EndsWith("_Right"))){
 var left=rows.Single(r=>r.id==row.id.Substring(0,row.id.Length-6));var actualRight=enemies[row.id].CurrentHealth;var actualLeft=enemies[left.id].CurrentHealth;
 if(Mathf.Abs(actualRight-row.health)>.01f||Mathf.Abs(actualLeft-left.health)>.01f)throw new Exception("Runtime health differs from workbook: "+row.id);
 lines.Add(row.id+"\t"+actualLeft+"\t"+actualRight+"\t"+(actualRight/actualLeft));
}
string path="map-concepts/harbor-opening-refinement-2026-09-16/runtime-health-"+scene.name+".tsv";System.IO.File.WriteAllLines(path,lines);return new{scene=scene.name,pairs=lines.Count};
