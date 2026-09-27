using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ProbeHighwayVisualFixes
{
    const string Output="outputs/highway-visual-fixes-2026-09-27";
    static string stage="before";
    public static object RoadMaterials()
    {
        var route=Object.FindFirstObjectByType<HighwayRoute>();
        return route.transform.Find("Roads/Chapter2Roads").GetComponentsInChildren<MeshRenderer>().Select(r=>r.sharedMaterial).Distinct().Select(m=>new{m.name,shader=m.shader.name,path=AssetDatabase.GetAssetPath(m),properties=Enumerable.Range(0,m.shader.GetPropertyCount()).Where(i=>m.shader.GetPropertyType(i)==UnityEngine.Rendering.ShaderPropertyType.Float||m.shader.GetPropertyType(i)==UnityEngine.Rendering.ShaderPropertyType.Range).Select(i=>m.shader.GetPropertyName(i)).Where(n=>n.ToLower().Contains("outline")).Select(n=>new{name=n,value=m.GetFloat(n)}).ToArray()}).ToArray();
    }
    public static object CurveProbe()
    {
        var route=Object.FindFirstObjectByType<HighwayRoute>();float old=route.forks[0].transitionLength;var result=new List<object>();
        try
        {
            foreach(float ramp in new[]{180,200,220,240,260,280,300,320,340})
            {
                route.forks[0].transitionLength=ramp;float worst=0,at=0,maxStep=0;
                for(float d=0;d<route.length;d+=4)
                {route.Sample(d,true,out var p,out var forward);route.Sample(d+1,true,out var next,out var tangent);float angle=Vector3.Angle(forward,tangent);if(angle>worst){worst=angle;at=d;}maxStep=Mathf.Max(maxStep,Vector3.Distance(p,next));}
                result.Add(new{ramp,worst,at,maxStep});
            }
        }
        finally{route.forks[0].transitionLength=old;}
        return result;
    }
    public static object RuntimeVisibility()
    {
        var chapter=HighwayChapter2Controller.Active;var camera=Camera.main;
        return chapter.Vehicles.Where(v=>v.Active&&!v.Dead).Select(v=>new{v.name,v.Distance,delta=v.Distance-chapter.Route.Distance,pos=v.transform.position.ToString(),body=v.body.gameObject.activeSelf,renderers=v.body.GetComponentsInChildren<MeshRenderer>(true).Select(r=>new{r.name,r.enabled,r.forceRenderingOff,active=r.gameObject.activeInHierarchy,bounds=r.bounds.ToString(),r.gameObject.layer,layerVisible=(camera.cullingMask&(1<<r.gameObject.layer))!=0}).ToArray()}).ToArray();
    }
    public static object InspectAfter(){stage="after";return Inspect();}
    public static object SweepAfter(){stage="after";return Sweep();}
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    public static object Preview()
    {
        string[] ids={"V02_white_sedan","V11_tanker","V01_compact_car","V03_black_suv"};
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var sheet=new Texture2D(1600,700,TextureFormat.RGB24,false);
        try
        {
            var light=new GameObject("Preview light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,150,0);SceneManager.MoveGameObjectToScene(light.gameObject,scene);
            for(int i=0;i<ids.Length;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/"+ids[i]+".prefab"),scene);
                var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                for(int j=0;j<2;j++)
                {
                    var camera=new GameObject("Preview camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;camera.fieldOfView=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.68f,.72f,.75f);camera.aspect=400f/350;
                    var dir=Quaternion.Euler(j==0?18:3,j==0?215:270,0)*Vector3.forward;camera.transform.position=b.center-dir*b.extents.magnitude*3.6f;camera.transform.LookAt(b.center);
                    var rt=new RenderTexture(400,350,24);camera.targetTexture=rt;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,400,350),i*400,(1-j)*350);RenderTexture.active=prior;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);
                }
                Object.DestroyImmediate(go);
            }
            sheet.Apply();Directory.CreateDirectory(Output);File.WriteAllBytes(Output+"/vehicle-library.png",sheet.EncodeToPNG());return new{file=Output+"/vehicle-library.png",columns=ids};
        }
        finally{Object.DestroyImmediate(sheet);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
    }
    public static object Inspect()
    {
        var route=Object.FindFirstObjectByType<HighwayRoute>();
        var roads=route.transform.Find("Roads/Chapter2Roads").GetComponentsInChildren<MeshCollider>();
        Physics.SyncTransforms();
        var rows=new List<object>();
        foreach(var car in route.GetComponentsInChildren<HighwayVehicleEnemy>(true))
        {
            float minGap=float.PositiveInfinity,maxGap=float.NegativeInfinity;int below=0,samples=0;
            var nearby=roads.Where(r=>Mathf.Abs(r.bounds.center.x-car.transform.position.x)<r.bounds.extents.x+20&&Mathf.Abs(r.bounds.center.z-car.transform.position.z)<r.bounds.extents.z+20).ToArray();
            var shape=car.GetComponent<BoxCollider>();float bottom=shape.center.y-shape.size.y*.5f;
            foreach(var filter in car.body.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.sharedMesh==null)continue;
                var vertices=filter.sharedMesh.vertices;var bounds=filter.sharedMesh.bounds;
                foreach(var vertex in vertices)
                {
                    var world=filter.transform.TransformPoint(vertex);if(car.transform.InverseTransformPoint(world).y>bottom+.3f)continue;
                    var ray=new Ray(world+Vector3.up*30,Vector3.down);float roadY=float.NegativeInfinity;
                    foreach(var road in nearby)if(road.Raycast(ray,out var hit,60))roadY=Mathf.Max(roadY,hit.point.y);
                    if(float.IsNegativeInfinity(roadY))continue;
                    float gap=world.y-roadY;minGap=Mathf.Min(minGap,gap);maxGap=Mathf.Max(maxGap,gap);if(gap<-.08f)below++;samples++;
                }
            }
            rows.Add(new{car.name,kind=car.kind.ToString(),car.station,minGap,maxGap,below,samples,rotation=car.transform.eulerAngles.ToString(),models=car.body.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh!=null).Select(m=>AssetDatabase.GetAssetPath(m.sharedMesh)).Distinct().ToArray()});
        }
        var result=new{playing=EditorApplication.isPlaying,clean=!SceneManager.GetActiveScene().isDirty,vehicles=rows};Directory.CreateDirectory(Output);File.WriteAllText(Output+"/support-"+stage+".json",Json(result));
        return new{playing=EditorApplication.isPlaying,clean=!SceneManager.GetActiveScene().isDirty,vehicles=rows.Count,file=Output+"/support-"+stage+".json"};
    }
    public static object Sweep()
    {
        var route=Object.FindFirstObjectByType<HighwayRoute>();var roads=route.transform.Find("Roads/Chapter2Roads").GetComponentsInChildren<MeshCollider>();Physics.SyncTransforms();
        var results=new List<object>();
        foreach(var car in route.GetComponentsInChildren<HighwayVehicleEnemy>(true).GroupBy(c=>c.kind+":"+AssetDatabase.GetAssetPath(c.body.GetComponentsInChildren<MeshFilter>(true).First().sharedMesh)).Select(g=>g.First()))
        {
            var collider=car.GetComponent<BoxCollider>();float low=collider.center.y-collider.size.y*.5f;var points=new List<Vector3>();
            foreach(var filter in car.body.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null)foreach(var v in filter.sharedMesh.vertices)
                {var p=car.transform.InverseTransformPoint(filter.transform.TransformPoint(v));if(p.y<low+.035f)points.Add(p);}
            var support=points.Where((v,i)=>i%Mathf.Max(1,points.Count/12)==0).ToArray();
            float worst=0,worstStation=0;bool worstBranch=false;int penetrations=0;
            foreach(bool branch in new[]{false,true})for(float d=10;d<route.length-10;d+=10)
            {
                route.Sample(d,branch,out var p,out var f);var slope=route.Point(d+.5f,branch)-route.Point(d-.5f,branch);
                var matrix=Matrix4x4.TRS(p+Vector3.Cross(Vector3.up,f)*HighwayChapter2Data.Lane(car.laneIndex)+Vector3.up*HighwayChapter2Data.Value("vehicleGroundClearance"),Quaternion.LookRotation(-slope.normalized),Vector3.one);
                foreach(var local in support)
                {
                    var world=matrix.MultiplyPoint3x4(local);float roadY=float.NegativeInfinity;
                    foreach(var road in roads)
                    {
                        var b=road.bounds;if(world.x<b.min.x||world.x>b.max.x||world.z<b.min.z||world.z>b.max.z)continue;
                        if(road.Raycast(new Ray(world+Vector3.up*30,Vector3.down),out var hit,60))roadY=Mathf.Max(roadY,hit.point.y);
                    }
                    if(float.IsNegativeInfinity(roadY))continue;float gap=world.y-roadY;
                    if(gap<-.08f)penetrations++;
                    if(gap<worst){worst=gap;worstStation=d;worstBranch=branch;}
                }
            }
            results.Add(new{kind=car.kind.ToString(),model=car.body.GetComponentsInChildren<MeshFilter>(true).First().sharedMesh.name,worst,worstStation,worstBranch,penetrations,support=support.Length});
        }
        File.WriteAllText(Output+"/support-sweep-"+stage+".json",Json(results));return results;
    }
}
