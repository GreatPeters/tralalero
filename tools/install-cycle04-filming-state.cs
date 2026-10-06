using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class InstallCycle04FilmingState {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");
  const string root="Assets/ShooterSurvival/Models/Chapters/Chapters45/DetailedCharacters20261003/",outdir="outputs/chapter45-detailed-design-2026-10-03/phone-readability-cycle04/recovery/";
  return new[]{"CH07","CH08","CH09"}.Select(id=>{
   string path=root+id+"/role-PhoneShopper.controller";var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(root+id+"/idle.anim");
   if(c==null||clip==null)throw new Exception("Existing own-rig idle missing "+id);
   if(c.layers[0].stateMachine.states.Any(s=>s.state.name=="phone_filming"))throw new Exception("Already installed "+id);
   File.Copy(path,outdir+id+"-role-PhoneShopper.controller",false);
   var state=c.layers[0].stateMachine.AddState("phone_filming");state.motion=clip;state.speed=1;state.writeDefaultValues=true;EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);
   return new{id,path,state=state.name,existingClip=AssetDatabase.GetAssetPath(clip),clipLength=clip.length,newAnimationGeneration=false};
  }).ToArray();
 }
}
