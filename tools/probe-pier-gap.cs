using System;using System.Linq;using UnityEngine;using UnityEditor;
public static class ProbePierGap {public static object Main(){
 var roads=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Roads");var colliders=roads.GetComponentsInChildren<MeshCollider>(true);Physics.SyncTransforms();var lines=new System.Collections.Generic.List<string>();
 for(float x=38.6f;x<=39.4f;x+=.1f)foreach(float z in new[]{-116.5f,-115.11f,-113.7f}){
 bool found=false;foreach(var c in colliders)if(c.Raycast(new Ray(new Vector3(x,2,z),Vector3.down),out var hit,4)){lines.Add(x.ToString("F1")+","+z+"="+hit.point.y+" "+c.name);found=true;break;}if(!found)lines.Add(x.ToString("F1")+","+z+"=gap");}
 return string.Join("\n",lines);
}}
