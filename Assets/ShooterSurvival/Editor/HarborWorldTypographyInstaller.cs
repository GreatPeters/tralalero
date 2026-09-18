using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using TMPro;using IndianOceanAssets.ShooterSurvival;

public static class HarborWorldTypographyInstaller
{
    public static object ApplyAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var font=HarborRefinementFontBuilder.Font;
        var bonus=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Resources/HarborRefinement/BonusReadable.mat");bonus.EnableKeyword("OUTLINE_ON");EditorUtility.SetDirty(bonus);AssetDatabase.SaveAssetIfDirty(bonus);
        string path="Assets/ShooterSurvival/Resources/HarborRefinement/EnemyHealthReadable.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(bonus);AssetDatabase.CreateAsset(material,path);}material.CopyPropertiesFromMaterial(bonus);material.SetFloat("_OutlineWidth",.16f);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
        void Style(GameObject root)
        {
            foreach(var enemy in root.GetComponentsInChildren<EnemyScript_space>(true))foreach(var text in enemy.GetComponentsInChildren<TMP_Text>(true))
            {
                if(text.font!=font){text.fontSize*=.8f;text.fontSizeMax=text.fontSize;text.fontSizeMin=text.fontSize*.65f;}
                text.font=font;text.fontSharedMaterial=material;text.fontStyle=FontStyles.Normal;text.color=Color.white;text.extraPadding=true;text.UpdateMeshPadding();
                EditorUtility.SetDirty(text);if(PrefabUtility.IsPartOfPrefabInstance(text))PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
        }
        void Scenery(GameObject root)
        {
            var white=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Resources/UI/HarborMaterials/Coastal_White.mat");
            var ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Resources/UI/HarborMaterials/Coastal_Ink.mat");
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){
                if(text.font==font)continue;
                text.font=font;text.fontSharedMaterial=text.color.grayscale>.6f?white:ink;text.fontStyle=FontStyles.Normal;text.fontSize*=.8f;
                text.fontSizeMax=text.fontSize;text.fontSizeMin=text.fontSize*.65f;text.enableAutoSizing=true;text.extraPadding=true;text.UpdateMeshPadding();
                EditorUtility.SetDirty(text);if(PrefabUtility.IsPartOfPrefabInstance(text))PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
            foreach(var sign in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Korean_Direction_Sign")){
                var labels=sign.GetComponentsInChildren<TMP_Text>(true).OrderByDescending(t=>t.transform.localPosition.y).ToArray();
                for(int i=0;i<labels.Length;i++){
                    var text=labels[i];var pos=text.transform.localPosition;pos.y=i==0?7.48f:6.58f;text.transform.localPosition=pos;
                    text.rectTransform.sizeDelta=new Vector2(13.1f,.72f);text.fontSize=text.fontSizeMax=i==0?7.4f:5.1f;text.fontSizeMin=i==0?4.5f:3.8f;text.enableAutoSizing=true;
                    text.textWrappingMode=TextWrappingModes.NoWrap;EditorUtility.SetDirty(text);EditorUtility.SetDirty(text.transform);
                }
            }
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var prefabs=new HashSet<string>();int labels=0;
        try{
            foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
                var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
                foreach(var root in scene.GetRootGameObjects()){
                    foreach(var enemy in root.GetComponentsInChildren<EnemyScript_space>(true)){string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemy);if(!string.IsNullOrEmpty(source))prefabs.Add(source);labels+=enemy.GetComponentsInChildren<TMP_Text>(true).Length;}
                    Style(root);Scenery(root);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            foreach(string prefab in prefabs){
                string backup="tmp/backups/harbor-opening-refinement-2026-09-16/world-typography/"+prefab;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(prefab,backup);
                var root=PrefabUtility.LoadPrefabContents(prefab);try{Style(root);PrefabUtility.SaveAsPrefabAsset(root,prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return new{labels,prefabs=prefabs.Count};
    }
}
