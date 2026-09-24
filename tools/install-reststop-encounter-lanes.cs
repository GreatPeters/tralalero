using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

public static class InstallRestStopEncounterLanes
{
    const string Art="Assets/ShooterSurvival/Models/Chapters/CampaignMotion20260923";
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved Edit Mode required");
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        const string path="Assets/ShooterSurvival/Scenes/Tools/RestStop.unity";
        const string backup="tmp/later-chapters-30-2026-09-23/RestStop-before-funnels.unity";
        if(!File.Exists(backup))File.Copy(path,backup);
        var scene=EditorSceneManager.OpenScene(path);
        try
        {
            var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
            var rows=map.Find("EncounterContacts").GetComponentsInChildren<HighwayEncounterRow>(true);
            if(rows.Length!=25||rows.Any(r=>r.left==null||r.right==null))throw new InvalidOperationException("Expected25complete contact pairs");
            var materials=new[]{"BarrierOrange","BarrierCream"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Rebuilt20260923/Encounters/"+n+".mat")).ToArray();
            if(materials.Any(m=>m==null))throw new InvalidOperationException("Shared road-rail materials missing");
            var meshes=new[]{CreateMesh(0),CreateMesh(1)};
            var disabled=EncounterPlacementTables.Rows.Where(r=>r.scene=="RestStop"&&r.kind=="적 배치"&&!r.enabled).Select(r=>r.id).ToHashSet();
            var roads=map.Find("Roads").GetComponentsInChildren<Collider>(true).Where(c=>c.enabled).ToArray();
            Physics.SyncTransforms();
            int correctedDirections=0;
            foreach(var row in rows)
            {
                Vector3 Center(EnemyScript_space actor)
                {var events=actor.GetComponent<EnemyEventController>();return events.EventMode==EnemyEventMode.PatrolBetweenStartAndTarget&&events.HasUsableTarget?(actor.transform.position+events.TargetPoint.position)*.5f:actor.transform.position;}
                var direction=Vector3.ProjectOnPlane(row.left.transform.forward,Vector3.up).normalized;
                var center=RestStopChapterBuilder.ProjectRoadCenter((Center(row.left)+Center(row.right))*.5f,direction,out var roadForward);
                var ray=new Ray(center+Vector3.up*2,Vector3.down);float floor=float.NegativeInfinity;
                foreach(var road in roads)if(road.Raycast(ray,out var hit,4))floor=Mathf.Max(floor,hit.point.y);
                if(!float.IsNegativeInfinity(floor))center.y=floor;
                if(Vector3.Dot(row.transform.forward,roadForward)<.999f)correctedDirections++;
                row.transform.SetPositionAndRotation(center,Quaternion.LookRotation(roadForward));row.halfWidth=1.85f;row.laneCenter=0;
                if(row.barriers!=null)Object.DestroyImmediate(row.barriers.gameObject);
                var visuals=new GameObject("VisibleRestStopPassage").transform;visuals.SetParent(row.transform,false);row.barriers=visuals;
                for(int color=0;color<2;color++)
                {
                    var go=new GameObject(color==0?"OrangeRails":"CreamRails",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(visuals,false);
                    go.GetComponent<MeshFilter>().sharedMesh=meshes[color];var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=materials[color];
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                }
                visuals.gameObject.SetActive(!(disabled.Contains(row.left.name)&&disabled.Contains(row.right.name)));
                EditorUtility.SetDirty(row);
            }
            var manager=map.GetComponent<HighwayEncounterLanes>()??map.gameObject.AddComponent<HighwayEncounterLanes>();manager.rows=rows;EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return new{pairs=rows.Length,activePassages=rows.Count(r=>r.barriers.gameObject.activeSelf),sharedMeshes=2,halfWidth=1.85f,radiiChanged=false,correctedDirections};
        }
        finally{if(!string.IsNullOrEmpty(original)&&original!=path)EditorSceneManager.OpenScene(original);}
    }
    static Mesh CreateMesh(int color)
    {
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
        var combines=new List<CombineInstance>();
        void Box(Vector3 a,Vector3 b,float width,float height)
        {
            var direction=(b-a).normalized;var rotation=Quaternion.LookRotation(direction,Mathf.Abs(Vector3.Dot(direction,Vector3.up))>.9f?Vector3.forward:Vector3.up);
            combines.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS((a+b)*.5f,rotation,new Vector3(width,height,Vector3.Distance(a,b)))});
        }
        foreach(int sign in new[]{-1,1})
        {
            Vector3 Point(float z,float y)=>new Vector3(sign*(HighwayEncounterRow.PassageHalfWidth(-z,4.4f,1.85f)+.84f),y,z);
            for(int i=0;i<14;i++)if(i%2==color){float z=-22+i*2;Box(Point(z,.72f),Point(z+2,.72f),.27f,.26f);}
            if(color==0)foreach(float z in new[]{-22f,-16,-12,-6,0,6})Box(Point(z,.02f),Point(z,.9f),.2f,.2f);
        }
        var mesh=new Mesh{name="RestStopSharedPassage_"+color};mesh.CombineMeshes(combines.ToArray(),true,true);
        string path=Art+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;
    }
}
