using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class VerifyHelperContactPlay
{
 public static string Main(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run());return "Native prefab contact probe started";}
 static IEnumerator Run()
 {
  var results=new List<object>();int i=0;
  foreach(var values in new[]{new[]{100f,250f},new[]{1200f,250f},new[]{2458f,300f},new[]{250f,250f}})
  {
   var position=new Vector3(10000+i++*20,0,10000);
   var enemyGo=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_YllowMan_Net.prefab"),position,Quaternion.identity);
   var helperGo=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Entities/Space/TungTungTung.prefab"),position+Vector3.back*5,Quaternion.identity);
   var enemy=enemyGo.GetComponent<EnemyScript_space>();var helper=helperGo.GetComponent<ExtraHelpBuffScript>();helper.helpType=HelpType.Tungtungtung;
   enemy.ConfigureRewards(false,0);yield return null;
   enemy.ApplyStat(10,values[0],values[0]>2000?EnemyTier.Boss:EnemyTier.Elite);helper.currentHealth=values[1];
   Vector3 from=position+Vector3.back*5,to=position+Vector3.forward*5;helperGo.transform.position=to;
   helper.ResolveContactsAlongMove(from,to);
   results.Add(new{enemyBefore=values[0],helperBefore=values[1],enemyAfter=enemy.CurrentHealth,helperAfter=helper.currentHealth,enemyDead=enemyGo.GetComponent<EnemyEventController>().RuntimeState==EnemyEventRuntimeState.Dead,enemyCollider=enemyGo.GetComponent<Collider>().enabled});
   UnityEngine.Object.Destroy(helperGo);UnityEngine.Object.Destroy(enemyGo);yield return null;
  }
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)results});File.WriteAllText("map-concepts/combat-route-fixes-2026-09-20/helper-contact-play.json",json);
 }
}
