if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity")throw new System.InvalidOperationException("SR18 required");
string folder="tmp/backups/sr18-data-work-2026-09-07/layout-"+System.DateTime.Now.ToString("HHmmss");System.IO.Directory.CreateDirectory(folder);
string before=folder+"/before.unity";UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,before,true);
string disk=System.IO.File.ReadAllText(scene.path).Replace("\r\n","\n"),live=System.IO.File.ReadAllText(before).Replace("\r\n","\n");
string pattern=@"(?m)^  (m_AnchorMin|m_AnchorMax|m_AnchoredPosition|m_SizeDelta): .*\n";
if(System.Text.RegularExpressions.Regex.Replace(disk,pattern,"")!=System.Text.RegularExpressions.Regex.Replace(live,pattern,""))throw new System.InvalidOperationException("Additional live changes; preserve and inspect before proceeding");
var rects=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RectTransform>(true)).GroupBy(t=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(t).targetObjectId.ToString()).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>g.Single());
int restored=0;var changedRects=new System.Collections.Generic.List<RectTransform>();
foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(disk,@"(?ms)^--- !u!224 &(\d+)\n.*?(?=^---|\z)")){
 string id=match.Groups[1].Value;string saved=match.Value;
 string actual=System.Text.RegularExpressions.Regex.Match(live,@"(?ms)^--- !u!224 &"+id+@"\n.*?(?=^---|\z)").Value;
 if(actual==saved)continue;
 if(!rects.TryGetValue(id,out var rect))throw new System.InvalidOperationException("Missing RectTransform "+id);
 foreach(System.Text.RegularExpressions.Match field in System.Text.RegularExpressions.Regex.Matches(actual,pattern))if(!field.Value.Contains("{x: 0, y: 0}"))throw new System.InvalidOperationException("Not the observed zero-layout issue: "+id);
 Vector2 Read(string field){var v=System.Text.RegularExpressions.Regex.Match(saved,field+@": \{x: ([^,]+), y: ([^}]+)\}");return new Vector2(float.Parse(v.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture),float.Parse(v.Groups[2].Value,System.Globalization.CultureInfo.InvariantCulture));}
 UnityEditor.Undo.RecordObject(rect,"Verify driven layout");rect.anchorMin=Read("m_AnchorMin");rect.anchorMax=Read("m_AnchorMax");rect.anchoredPosition=Read("m_AnchoredPosition");rect.sizeDelta=Read("m_SizeDelta");restored++;changedRects.Add(rect);
}
string verified=folder+"/restored.unity";UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,verified,true);
string checkedText=System.IO.File.ReadAllText(verified).Replace("\r\n","\n");
if(checkedText!=disk){
 if(changedRects.Count!=6||changedRects.Any(r=>r.drivenByObject is not UnityEngine.UI.GridLayoutGroup)||System.Text.RegularExpressions.Regex.Replace(checkedText,pattern,"")!=System.Text.RegularExpressions.Regex.Replace(disk,pattern,""))throw new System.InvalidOperationException("Additional changes; leaving live scene dirty");
 // Unity deliberately serializes driven RectTransform fields as zero, while live GridLayoutGroup values remain correct.
 UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 return new{restored,canonicalizedDrivenFields=true,backup=before};
}
typeof(UnityEditor.SceneManagement.EditorSceneManager).GetMethod("ClearSceneDirtiness",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{scene});
return new{restored,identicalToSaved=true,backup=before};
