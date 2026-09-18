using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class HighwayRefinement
{
    private const string Prefix="Highway_Polish_";
    public static object Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=HighwaySceneBuilder.ScenePath)throw new InvalidOperationException("HighWay Edit Mode required.");
        var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var props=map.Find("Props");
        if(props.Cast<Transform>().Any(t=>t.name.StartsWith(Prefix)))throw new InvalidOperationException("Preserve the already refined Highway.");
        EnsureStartApron();
        string texturePath="Assets/ShooterSurvival/Models/Highway/Route/AsphaltDetailed.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.SaveAndReimport();
        var asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");asphalt.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));asphalt.SetTextureScale("_BaseMap",Vector2.one*2.5f);asphalt.color=new Color(.72f,.74f,.77f);EditorUtility.SetDirty(asphalt);
        RenderSettings.fogColor=new Color(.60f,.73f,.85f);RenderSettings.fogStartDistance=150;RenderSettings.fogEndDistance=360;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.68f,.77f);RenderSettings.ambientEquatorColor=new Color(.40f,.45f,.53f);RenderSettings.ambientGroundColor=new Color(.20f,.22f,.20f);
        var roads=map.Find("Roads");var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Concrete.mat");
        int supports=0;
        for(float d=20;d<HighwaySceneBuilder.Length;d+=40)
        {var p=HighwaySceneBuilder.Sample(d,out var f);var root=new GameObject(Prefix+"Support_"+(++supports)).transform;root.SetParent(props,false);root.SetPositionAndRotation(p,Quaternion.LookRotation(f));float height=p.y+6.5f;Cube(root,"Pillar",new Vector3(0,-height*.5f,0),new Vector3(1.6f,height,1.6f),concrete);Cube(root,"Bearing",new Vector3(0,-.5f,0),new Vector3(12,.65f,2),concrete);}
        int walls=0;
        foreach(var span in new[]{new Vector2(140,360),new Vector2(1450,1530),new Vector2(1680,1970)})
            for(float d=span.x;d<span.y;d+=12)foreach(int side in new[]{-1,1})
            {var g=Prop(props,64,d,side*10.5f,0,Prefix+"NoiseWall_"+(++walls));g.transform.localScale*=.55f;AlignLongAxis(g,d);}
        foreach(float d in new[]{35f,540f,1880f})Prop(props,53,d,9,180,Prefix+"SpeedCamera");
        foreach(float d in new[]{390f,650f,1190f,1550f,2050f})Prop(props,56,d,-7.8f,180,Prefix+"CurveMarker");
        foreach(int id in new[]{49,57,58,66,71})Prop(props,id,490+(id%5)*11,10+(id%2)*3,0,Prefix+"Roadworks_"+id);
        Prop(props,61,1470,21,0,Prefix+"ServiceTollGate");
        foreach(float d in new[]{1490f,1770f,1990f}){Prop(props,77,d,9.8f,0,Prefix+"ServiceBarrier");Prop(props,78,d,-8.5f,180,Prefix+"TollSignal");}
        for(float d=1420;d<1520;d+=10)Prop(props,76,d,6.1f,90,Prefix+"LaneBollard");
        MoveTraffic(props,"HWY_G15_Traffic",45);MoveTraffic(props,"HWY_G16_Traffic",245);
        int arrows=0;
        var white=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/RoadPaint.mat");
        var arrowMesh=new Mesh{name="Highway Direction Arrow"};
        arrowMesh.vertices=new[]{new Vector3(-.16f,-.55f,0),new Vector3(.16f,-.55f,0),new Vector3(-.16f,.14f,0),new Vector3(.16f,.14f,0),new Vector3(-.5f,.12f,0),new Vector3(.5f,.12f,0),new Vector3(0,.72f,0)};
        arrowMesh.triangles=new[]{0,2,1,1,2,3,4,6,5};arrowMesh.RecalculateNormals();
        AssetDatabase.CreateAsset(arrowMesh,"Assets/ShooterSurvival/Models/Highway/Route/DirectionArrow.asset");
        foreach(var gantry in props.Cast<Transform>().Where(t=>t.name=="Highway_SignGantry"))
            foreach(var sign in gantry.Cast<Transform>().Where(t=>t.name=="GreenSign"))
            {var old=sign.Find("Arrow");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);var arrow=new GameObject("Arrow");arrow.transform.SetParent(sign,false);arrow.transform.localPosition=new Vector3(0,0,-.75f);arrow.transform.localScale=new Vector3(1/3.25f,1/1.7f,1);arrow.AddComponent<MeshFilter>().sharedMesh=arrowMesh;arrow.AddComponent<MeshRenderer>().sharedMaterial=white;arrows++;}
        foreach(var renderer in map.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
            if(material!=null && AssetDatabase.GetAssetPath(material).StartsWith("Assets/ShooterSurvival/Models/Highway",StringComparison.Ordinal)){material.enableInstancing=true;EditorUtility.SetDirty(material);}
        foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true))
            if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("map-concepts/highway-chapter-2026-09-11/refinement.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{supports,walls,arrows,trafficDistances=new[]{45,245}},Newtonsoft.Json.Formatting.Indented));
        return new{supports,walls,arrows};
    }
    public static void EnsureStartApron()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=HighwaySceneBuilder.ScenePath)throw new InvalidOperationException("HighWay Edit Mode required.");
        var roads=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Roads");
        for(int i=0;i<2;i++)
        {
            string name="Highway_StartApron_"+i;if(roads.Find(name)!=null)continue;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Highway/Roads/HighwayStraight.prefab");
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,roads);g.name=name;g.transform.position=HighwaySceneBuilder.Points[0]-Vector3.forward*(10+i*20);
            PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    private static void MoveTraffic(Transform props,string id,float distance)
    {var t=props.Find(id);if(t==null)throw new InvalidOperationException(id);var p=HighwaySceneBuilder.Sample(distance,out var f);t.SetPositionAndRotation(p-Vector3.Cross(Vector3.up,f)*4.5f,Quaternion.LookRotation(f));}
    private static GameObject Prop(Transform parent,int id,float distance,float lane,float yaw,string name)
    {var path=HighwayAssetImporter.Prefabs+"/HWY_"+id.ToString("D3")+".prefab";var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);g.name=name;var p=HighwaySceneBuilder.Sample(distance,out var f);g.transform.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*lane,Quaternion.LookRotation(f)*Quaternion.Euler(0,yaw,0));return g;}
    private static void AlignLongAxis(GameObject root,float distance)
    {
        root.transform.rotation=Quaternion.identity;
        var samples=new List<Vector3>();
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {var vertices=filter.sharedMesh.vertices;int step=Mathf.Max(1,vertices.Length/1000);for(int i=0;i<vertices.Length;i+=step)samples.Add(root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i])));}
        float x=samples.Average(p=>p.x),z=samples.Average(p=>p.z);double xx=0,zz=0,xz=0;
        foreach(var p in samples){xx+=(p.x-x)*(p.x-x);zz+=(p.z-z)*(p.z-z);xz+=(p.x-x)*(p.z-z);}
        float axis=.5f*Mathf.Atan2((float)(2*xz),(float)(xx-zz))*Mathf.Rad2Deg;HighwaySceneBuilder.Sample(distance,out var forward);
        root.transform.rotation=Quaternion.Euler(0,axis-Mathf.Atan2(forward.z,forward.x)*Mathf.Rad2Deg,0);
    }
    private static void Cube(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;}
}
