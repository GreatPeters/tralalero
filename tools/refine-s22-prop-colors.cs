using System;
using UnityEngine;
using UnityEditor;
public static class S22PropColors
{
 public static object Main(){if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");const string dir="Assets/ShooterSurvival/Models/Generated/S22Polish/Props/";AssetDatabase.ImportAsset(dir+"MobilePropVertex.shader",ImportAssetOptions.ForceSynchronousImport);var shader=AssetDatabase.LoadAssetAtPath<Shader>(dir+"MobilePropVertex.shader");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Invalid vertex shader");var mat=AssetDatabase.LoadAssetAtPath<Material>(dir+"MobilePropColors.mat");mat.shader=shader;mat.SetColor("_BaseColor",Color.white);mat.shaderKeywords=Array.Empty<string>();EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();return "Multiply directional shade by vertex regions; preserve dark recess and beverage colors.";}
}
