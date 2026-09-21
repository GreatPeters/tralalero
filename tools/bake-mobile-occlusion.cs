using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class BakeMobileOcclusion
{
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||!new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}.Contains(scene.name)||StaticOcclusionCulling.isRunning)throw new InvalidOperationException("Idle chapter Edit Mode required.");
        var map=GameObject.Find("Noryangjin_MapTool").transform;var roads=map.Find("Roads");
        var excluded=new HashSet<Renderer>(roads.GetComponentsInChildren<Renderer>(true));
        var camera=Camera.main;var custom=camera.GetComponent<NoryangjinCameraOcclusion>();
        var groups=new SerializedObject(custom).FindProperty("additionalOccluderGroups");
        for(int i=0;i<groups.arraySize;i++)if(groups.GetArrayElementAtIndex(i).objectReferenceValue is Transform group)
            foreach(var renderer in group.GetComponentsInChildren<Renderer>(true))excluded.Add(renderer);
        int occluders=0,occludees=0;
        foreach(var renderer in map.GetComponentsInChildren<MeshRenderer>(false))
        {
            if(!renderer.enabled||!GameObjectUtility.AreStaticEditorFlagsSet(renderer.gameObject,StaticEditorFlags.BatchingStatic))continue;
            var flags=GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)|StaticEditorFlags.OccludeeStatic;
            bool opaque=renderer.sharedMaterials.All(m=>m!=null&&m.renderQueue<=2500&&(!m.HasProperty("_Surface")||m.GetFloat("_Surface")<.5f));
            if(opaque&&!excluded.Contains(renderer)){flags|=StaticEditorFlags.OccluderStatic;occluders++;}
            else flags&=~StaticEditorFlags.OccluderStatic;
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,flags);occludees++;
            if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.gameObject);
        }
        var roadBounds=roads.GetComponentsInChildren<MeshCollider>(false).Select(c=>c.bounds).ToArray();var bounds=roadBounds[0];foreach(var b in roadBounds)bounds.Encapsulate(b);bounds.Expand(new Vector3(40,40,40));
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="MobileOcclusionViewVolume")??new GameObject("MobileOcclusionViewVolume");
        var area=root.GetComponent<OcclusionArea>();if(area==null)area=root.AddComponent<OcclusionArea>();root.transform.position=bounds.center;area.center=Vector3.zero;area.size=bounds.size;
        camera.useOcclusionCulling=true;StaticOcclusionCulling.smallestOccluder=5;StaticOcclusionCulling.smallestHole=.25f;StaticOcclusionCulling.backfaceThreshold=100;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("tmp/mobile-feedback-2026-09-20");
        File.WriteAllText("tmp/mobile-feedback-2026-09-20/occlusion-setup-"+scene.name+".txt",$"occluders={occluders}, occludees={occludees}, excludedDynamicVisibility={excluded.Count}, bounds={bounds}, previousDataBytes={StaticOcclusionCulling.umbraDataSize}");
        return $"started={StaticOcclusionCulling.GenerateInBackground()}, occluders={occluders}, occludees={occludees}";
    }
}
