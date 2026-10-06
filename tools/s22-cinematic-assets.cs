using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class S22CinematicAssets
{
 public static object Main(){AssetDatabase.ImportAsset("Assets/ShooterSurvival/Audio/NoryangjinRevamp/S22Polish/auction-sold-live.wav",ImportAssetOptions.ForceSynchronousImport);var c=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var shark=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/polyperfect/Low Poly Animated Animals/- Prefabs/Animals/Shark_White.prefab");return new{shoe=AssetDatabase.GetAssetPath(c.Find("shoes_ruby").accessory),normalShark=AssetDatabase.GetAssetPath(shark),normalAnimator=shark.GetComponentInChildren<Animator>()!=null,normalClips=shark.GetComponentInChildren<Animator>()?.runtimeAnimatorController?.animationClips.Select(a=>a.name).ToArray()};}
}
