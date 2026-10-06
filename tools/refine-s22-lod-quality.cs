using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class S22LodQuality
{
 public static object Main(){if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var setup=EditorSceneManager.GetSceneManagerSetup();int revised=0;try{foreach(var name in new[]{"Noryangjin_MapTool_Mode_SR18_Revamp","HighWay"}){var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");foreach(var group in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true))){var levels=group.GetLODs();if(levels.Length!=3||!levels[1].renderers.Any(r=>r!=null&&r.name=="S22_LOD1"))continue;group.SetLODs(new[]{new LOD(.14f,levels[0].renderers),new LOD(0,levels[1].renderers)});foreach(var r in levels[2].renderers)if(r!=null&&r.name=="S22_LOD2")Object.DestroyImmediate(r.gameObject);group.RecalculateBounds();revised++;}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}File.WriteAllText("outputs/s22-polish-2026-10-01/lod-quality-correction.txt",$"{revised} groups now original + reviewed LOD1 only. Aggressive welded LOD2 rejected after native UV/silhouette review; no far cull.");return new{revised};}
}
