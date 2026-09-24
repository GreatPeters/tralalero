using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class InstallHighwayEncounterLanes
{
    const string Folder="Assets/ShooterSurvival/Models/Highway/Rebuilt20260923/Encounters";
    public static string Main()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        const string path="Assets/ShooterSurvival/Scenes/Tools/HighWay.unity";
        const string backup="tmp/highway-enemy-rebuild-2026-09-23/before/HighWay.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene(path);var route=UnityEngine.Object.FindFirstObjectByType<HighwayRoute>();
        var manager=route.GetComponent<HighwayEncounterLanes>();if(manager==null)manager=route.gameObject.AddComponent<HighwayEncounterLanes>();
        var parent=route.transform.Find("EncounterLanes");if(parent==null){parent=new GameObject("EncounterLanes").transform;parent.SetParent(route.transform,false);}
        var actors=route.transform.Find("Enemies").GetComponentsInChildren<EnemyScript_space>(true);
        // Sideways patrols were opening a third passage between the pair.
        // Keep walking animation, but patrol forward/back inside each lane.
        foreach(var actor in actors)
        {
            var events=actor.GetComponent<EnemyEventController>();
            if(events==null||events.EventMode!=EnemyEventMode.PatrolBetweenStartAndTarget||!events.PatrolAcrossRoad||!events.HasUsableTarget)continue;
            var center=(actor.transform.position+events.TargetPoint.position)*.5f;
            float distance=Vector3.Distance(actor.transform.position,events.TargetPoint.position);
            var forward=Vector3.ProjectOnPlane(actor.transform.forward,Vector3.up).normalized;
            actor.transform.position=center-forward*distance*.5f;events.TargetPoint.position=center+forward*distance*.5f;events.PatrolAcrossRoad=false;
            Record(actor.transform);Record(events.TargetPoint);Record(events);
        }
        var rows=new List<HighwayEncounterRow>();
        var orange=Material("BarrierOrange",new Color(.96f,.36f,.035f));var cream=Material("BarrierCream",new Color(.96f,.91f,.74f));
        foreach(var left in actors.Where(e=>!e.name.EndsWith("_Right",StringComparison.Ordinal)))
        {
            var right=actors.FirstOrDefault(e=>e.name==left.name+"_Right");if(right==null)continue;
            string name=left.name+"_Passage";var transform=parent.Find(name);if(transform==null){transform=new GameObject(name).transform;transform.SetParent(parent,false);}
            var row=transform.GetComponent<HighwayEncounterRow>();if(row==null)row=transform.gameObject.AddComponent<HighwayEncounterRow>();
            row.left=left;row.right=right;var center=(left.transform.position+right.transform.position)*.5f;row.routeDistance=route.NearestDistance(center);row.halfWidth=1.85f;
            route.Sample(row.routeDistance,false,out var sampled,out var forward);row.laneCenter=Vector3.Dot(center-sampled,Vector3.Cross(Vector3.up,forward));
            transform.SetPositionAndRotation(sampled,Quaternion.LookRotation(forward));
            foreach(var actor in new[]{left,right})
            {var member=actor.GetComponent<HighwayEncounterMember>();if(member==null)member=actor.gameObject.AddComponent<HighwayEncounterMember>();member.row=row;Record(member);}
            if(row.barriers!=null)UnityEngine.Object.DestroyImmediate(row.barriers.gameObject);
            {
                var visuals=new GameObject("VisibleConstructionFunnel").transform;visuals.SetParent(transform,false);row.barriers=visuals;
                for(int color=0;color<2;color++)
                {
                    var combines=new List<CombineInstance>();var primitives=new List<GameObject>();
                    for(int sign=-1;sign<=1;sign+=2)
                    {
                        Vector3 Point(float delta,float y)
                        {
                            route.Sample(row.routeDistance+delta,false,out var p,out var f);
                            float width=HighwayEncounterRow.PassageHalfWidth(-delta,4.4f,row.halfWidth);
                            return p+Vector3.Cross(Vector3.up,f)*(row.laneCenter+sign*(width+.84f))+Vector3.up*y;
                        }
                        for(int part=0;part<14;part++)
                        {
                            if(part%2!=color)continue;float d=-22+part*2;
                            AddBox(Point(d,.72f),Point(d+2,.72f),.27f,.26f,visuals,combines,primitives);
                        }
                        if(color==0)foreach(float d in new[]{-22f,-16,-12,-6,0,6})
                            AddBox(Point(d,.02f),Point(d,.9f),.20f,.20f,visuals,combines,primitives);
                    }
                    var mesh=new Mesh(){name=name+"_"+color};mesh.CombineMeshes(combines.ToArray(),true,true);
                    string meshPath=Folder+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(saved==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
                    foreach(var primitive in primitives)UnityEngine.Object.DestroyImmediate(primitive);
                    var go=new GameObject(color==0?"OrangeRails":"CreamRails",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(visuals,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=color==0?orange:cream;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                }
            }
            Record(row);rows.Add(row);
        }
        manager.rows=rows.OrderBy(r=>r.routeDistance).ToArray();Record(manager);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(original);
        return $"Installed {rows.Count} visible encounter funnels. Original enemy capsules and stats retained.";
    }
    static void AddBox(Vector3 a,Vector3 b,float width,float height,Transform parent,List<CombineInstance> combines,List<GameObject> primitives)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.position=(a+b)*.5f;go.transform.rotation=Quaternion.LookRotation((b-a).normalized,Mathf.Abs(Vector3.Dot((b-a).normalized,Vector3.up))>.9f?Vector3.forward:Vector3.up);go.transform.localScale=new Vector3(width,height,Vector3.Distance(a,b));
        combines.Add(new CombineInstance{mesh=go.GetComponent<MeshFilter>().sharedMesh,transform=parent.worldToLocalMatrix*go.transform.localToWorldMatrix});primitives.Add(go);
    }
    static Material Material(string name,Color color)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("FlatKit/Stylized Surface"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetColor("_ColorDim",color*.7f);m.EnableKeyword("_CELPRIMARYMODE_SINGLE");m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static void Record(UnityEngine.Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
}
