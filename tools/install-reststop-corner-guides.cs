using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

public static class InstallRestStopCornerGuides
{
    const string ScenePath="Assets/ShooterSurvival/Scenes/Tools/RestStop.unity";
    const string Art="Assets/ShooterSurvival/Models/Chapters/CampaignMotion20260923";
    const string RootName="Campaign_CornerGuides";
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        string previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        try
        {
            string backup="tmp/campaign-balance-2026-09-23/RestStop-before-corner-guides.unity";
            if(!File.Exists(backup))File.Copy(ScenePath,backup);
            var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
            var props=map.Find("Props");var existing=props.Find(RootName);if(existing!=null)Object.DestroyImmediate(existing.gameObject);
            var root=new GameObject(RootName).transform;root.SetParent(props,false);
            var navy=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/RoadPatterns/FoodHallNavy.mat");
            var amber=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/RoadPatterns/WarningAmber.mat");
            var treePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ithappy/Megacity/Prefabs/Props/tree_012.prefab");
            if(navy==null||amber==null||treePrefab==null)throw new InvalidOperationException("Missing authored material or tree");
            var turns=props.GetComponentsInChildren<NoryangjinTurnSpot>(true).OrderBy(t=>t.name).ToArray();
            var rightMesh=Board(-1);var leftMesh=Board(1);
            foreach(var turn in turns)
            {
                Vector3 incoming=turn.transform.forward,outgoing=Quaternion.Euler(0,turn.TargetYawDegrees,0)*Vector3.forward;
                Vector3 right=Vector3.Cross(Vector3.up,incoming);
                var board=new GameObject(turn.name+"_Direction",typeof(MeshFilter),typeof(MeshRenderer));board.transform.SetParent(root,false);
                board.transform.SetPositionAndRotation(turn.transform.position+incoming*12,Quaternion.LookRotation(-incoming));
                board.GetComponent<MeshFilter>().sharedMesh=Vector3.Dot(outgoing,right)>0?rightMesh:leftMesh;
                board.GetComponent<Renderer>().sharedMaterials=new[]{navy,amber};
                foreach(int side in new[]{-1,1})
                {
                    var tree=(GameObject)PrefabUtility.InstantiatePrefab(treePrefab,root);tree.name=turn.name+"_OuterTree_"+side;
                    var bounds=BoundsOf(tree);tree.transform.localScale*=6/Mathf.Max(.01f,bounds.size.y);
                    tree.transform.rotation=Quaternion.Euler(0,37*side,0);bounds=BoundsOf(tree);
                    var target=turn.transform.position+incoming*22+right*(side*15);
                    tree.transform.position+=target-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                    foreach(var collider in tree.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                    foreach(var component in tree.GetComponentsInChildren<Component>(true))if(component!=null)PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return new{corners=turns.Length,boards=turns.Length,trees=turns.Length*2,activeColliders=root.GetComponentsInChildren<Collider>(true).Count(c=>c.enabled),boardDrawCalls=turns.Length*2};
        }
        finally{if(previous!=ScenePath&&!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);}
    }
    static Bounds BoundsOf(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;
    }
    static Mesh Board(int direction)
    {
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(primitive);
        var dark=new List<CombineInstance>();var bright=new List<CombineInstance>();
        void Box(List<CombineInstance> list,Vector3 p,Vector3 s,float z=0)=>list.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(p,Quaternion.Euler(0,0,z),s)});
        Box(dark,new Vector3(0,2.8f,0),new Vector3(11,2.8f,.18f));
        foreach(int side in new[]{-1,1})Box(dark,new Vector3(side*4,1,0),new Vector3(.17f,2,.17f));
        foreach(float x in new[]{-3f,0,3f})foreach(int y in new[]{-1,1})
            Box(bright,new Vector3(x,2.8f+y*.43f,.13f),new Vector3(1.45f,.3f,.06f),-direction*y*45);
        var darkMesh=new Mesh();darkMesh.CombineMeshes(dark.ToArray(),true,true);
        var brightMesh=new Mesh();brightMesh.CombineMeshes(bright.ToArray(),true,true);
        var result=new Mesh{name=direction>0?"CornerChevronLocalRight":"CornerChevronLocalLeft"};
        result.CombineMeshes(new[]{new CombineInstance{mesh=darkMesh,transform=Matrix4x4.identity},new CombineInstance{mesh=brightMesh,transform=Matrix4x4.identity}},false,true);
        Object.DestroyImmediate(darkMesh);Object.DestroyImmediate(brightMesh);
        string path=Art+"/"+result.name+".asset";var stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(stored==null){AssetDatabase.CreateAsset(result,path);return result;}
        EditorUtility.CopySerialized(result,stored);Object.DestroyImmediate(result);return stored;
    }
}
