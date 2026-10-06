using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

// ShoeTower presentation only. Derives a portrait-readable interior from the
// accepted actual shoe mesh. Leaves source FBXs, exterior Jamsil, transforms,
// all colliders, moving targets, goal and offering collectible untouched.
public static class RepairCrownInteriorReadability
{
    const string ScenePath="Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity";
    const string Source="Assets/ShooterSurvival/Models/Chapters/Chapters45/Crown";
    const string Art="Assets/ShooterSurvival/Models/Chapters/Chapters45/CrownInteriorReadableV1";
    const string Record="outputs/chapters45-2026-10-02";
    const string Owned="CrownInteriorDetail_V1";
    static readonly List<string> notes=new List<string>();
    static readonly List<Vector3[]> surface=new List<Vector3[]>();
    static readonly Dictionary<Material,Material> insideMaterials=new Dictionary<Material,Material>();
    static float floor,centerX,toeStart;
    static int keptTriangles,hardware;

    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Idle Edit Mode required.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scenes first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;bool saved=false;
        try
        {
            scene=EditorSceneManager.OpenScene(ScenePath);
            var director=Object.FindObjectsByType<Chapter45Director>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(d=>d.gameObject.scene==scene&&d.chapter==5);
            var showroom=director.transform.Find("Scenery/InsideTheSneaker");
            var anchor=showroom!=null?showroom.Find("CrownShell"):null;
            var goal=director.GetComponentsInChildren<Chapter45Goal>(true).Single(g=>g.offering);
            if(anchor==null||anchor.GetComponent<LODGroup>()==null||goal.collectionVisual==null)throw new InvalidOperationException("Installed crown shell and physical offering required.");
            director.route.Sample(director.route.Length,out var end,out var forward);
            if(Vector3.Dot(forward,Vector3.forward)<.999f)throw new InvalidOperationException("Expected final +Z crown corridor.");
            floor=end.y;centerX=anchor.position.x;toeStart=goal.transform.position.z+6;
            string before=ProtectedState(scene);var anchorMatrix=anchor.localToWorldMatrix;
            Directory.CreateDirectory(Art);Directory.CreateDirectory(Record);AssetDatabase.Refresh();
            string stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
            string backup=Record+"/ShoeTower-before-crown-readability-"+stamp+".unity";File.Copy(ScenePath,backup,false);
            notes.Clear();surface.Clear();insideMaterials.Clear();keptTriangles=hardware=0;
            var prior=showroom.Find(Owned);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            for(int lod=0;lod<3;lod++)RebuildLOD(anchor,lod);
            anchor.GetComponent<LODGroup>().RecalculateBounds();
            // The installer merged its generic ribs/laces/eyelets into direct
            // showroom batches. Hide those exact direct renderers, not source LODs.
            foreach(var renderer in showroom.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.transform.parent==showroom&&r.GetComponent<TMP_Text>()==null))
            {renderer.enabled=false;notes.Add("Hidden legacy direct showroom renderer: "+renderer.name+" / "+(renderer.GetComponent<MeshFilter>()?.sharedMesh?.name??"none"));}
            var detail=new GameObject(Owned).transform;detail.SetParent(showroom,false);detail.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var ceramic=InteriorMaterial(AssetDatabase.LoadAssetAtPath<Material>(Source+"/OffwhiteCeramic.mat"));
            var brass=InteriorMaterial(AssetDatabase.LoadAssetAtPath<Material>(Source+"/ChampagneFrames.mat"));
            var dark=MakeMaterial("InsoleInk",new Color(.12f,.19f,.22f));
            AddShoeHardware(detail,brass,ceramic);
            // Replaces the now-hidden generic gold batch with a deliberate low
            // display pedestal at the actual collectible, without moving it.
            var offeringBounds=BoundsOf(goal.collectionVisual.GetComponentsInChildren<Renderer>(true));
            float top=offeringBounds.min.y-.035f;var offeringCenter=offeringBounds.center;
            var basePosition=new Vector3(offeringCenter.x,floor,offeringCenter.z);
            Pedestal(detail,basePosition,top-floor,ceramic,brass,dark);
            Merge(detail);
            if(before!=ProtectedState(scene)||anchorMatrix!=anchor.localToWorldMatrix)throw new InvalidOperationException("Interior presentation changed protected gameplay, collider or crown anchor transform.");
            if(detail.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("New interior details must have no colliders.");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("ShoeTower save failed.");saved=true;
            notes.Add("Actual source geometry retained; world cross-section13m, longitudinal extent91m. Central corridor halfwidth4.3m is clipped by polygon planes, not triangle centroids.");
            notes.Add("Curved sidewalls retained up to12m above deck; actual toe wall retained beyond physical goal+6m, up to10m. Source/Jamsil materials untouched; interior clones are two-sided for inward viewing.");
            notes.Add("Crown child/anchor transforms, offering transform, gameplay scripts, labels, colliders and source FBXs unchanged. Native camera/captain/goal review pending.");
            string report=Record+"/crown-interior-readability-"+stamp+".txt";File.WriteAllLines(report,notes);
            return new {scene=ScenePath,backup,report,keptTrianglesAllLODs=keptTriangles,hardwareElements=hardware,interiorWidth=13f,clearCorridorHalfWidth=4.3f,gameplayAndCollidersUnchanged=true};
        }
        finally{if(!saved&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }

    static void RebuildLOD(Transform anchor,int lod)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"/ShoeCrown_LOD"+lod+".fbx");
        var child=anchor.Find("Crown_LOD"+lod);if(src==null||child==null)throw new InvalidOperationException("Missing accepted crown LOD"+lod);
        var sources=src.GetComponentsInChildren<MeshFilter>(true);var filters=child.GetComponentsInChildren<MeshFilter>(true);
        if(sources.Length!=filters.Length)throw new InvalidOperationException("Crown hierarchy differs from source.");
        bool first=true;Bounds full=default;
        for(int j=0;j<filters.Length;j++)foreach(var vertex in sources[j].sharedMesh.vertices)
        {var world=filters[j].transform.TransformPoint(vertex);if(first){full=new Bounds(world,Vector3.zero);first=false;}else full.Encapsulate(world);}
        if(full.size.z<89||full.size.z>93||full.size.x<28||full.size.x>33)throw new InvalidOperationException("Unexpected full imported crown orientation/dimensions: "+full);
        float narrow=13/full.size.x;int count=0;
        for(int j=0;j<filters.Length;j++)
        {
            var input=sources[j].sharedMesh;var filter=filters[j];var sourceVertices=input.vertices;var sourceNormals=input.normals;var sourceUV=input.uv;
            var vertices=new V[sourceVertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var world=filter.transform.TransformPoint(sourceVertices[i]);world.x=centerX+(world.x-centerX)*narrow;
                var normal=sourceNormals.Length==vertices.Length?filter.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(sourceNormals[i]).normalized:Vector3.up;
                normal.x/=narrow;normal.Normalize();vertices[i]=new V(world,normal,sourceUV.Length==vertices.Length?sourceUV[i]:Vector2.zero);
            }
            var output=new MeshBuilder(input.subMeshCount,filter.transform);
            for(int sub=0;sub<input.subMeshCount;sub++)
            {
                var indices=input.GetTriangles(sub);
                for(int i=0;i<indices.Length;i+=3)
                {
                    var triangle=new List<V>{vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]]};
                    Emit(output,sub,Clip(triangle,p=>floor+.035f-p.y),lod);
                    var above=Clip(triangle,p=>p.y-floor-.035f);
                    var sideHeight=Clip(above,p=>floor+12-p.y);
                    Emit(output,sub,Clip(sideHeight,p=>centerX-4.3f-p.x),lod);
                    Emit(output,sub,Clip(sideHeight,p=>p.x-centerX-4.3f),lod);
                    var toe=Clip(Clip(Clip(above,p=>floor+10-p.y),p=>p.z-toeStart),p=>p.x-centerX+4.3f);
                    Emit(output,sub,Clip(toe,p=>centerX+4.3f-p.x),lod);
                }
            }
            var mesh=output.Finish("ActualShoeInterior_LOD"+lod+"_"+j);count+=mesh.triangles.Length/3;
            filter.sharedMesh=SaveMesh(mesh,mesh.name);
            var renderer=filter.GetComponent<MeshRenderer>();renderer.sharedMaterials=renderer.sharedMaterials.Select(InteriorMaterial).ToArray();
        }
        keptTriangles+=count;notes.Add("LOD"+lod+": original bounds="+full+", transverse factor="+narrow.ToString("F4")+", retained triangles="+count);
    }
    static void Emit(MeshBuilder output,int sub,List<V> polygon,int lod)
    {
        if(polygon.Count<3)return;
        for(int i=1;i<polygon.Count-1;i++)
        {
            if(Vector3.Cross(polygon[i].p-polygon[0].p,polygon[i+1].p-polygon[0].p).sqrMagnitude<.00000001f)continue;
            output.Triangle(sub,polygon[0],polygon[i],polygon[i+1]);
            if(lod==0)surface.Add(new[]{polygon[0].p,polygon[i].p,polygon[i+1].p});
        }
    }
    static List<V> Clip(List<V> input,Func<Vector3,float> distance)
    {
        var result=new List<V>();if(input.Count==0)return result;
        var previous=input[input.Count-1];float pd=distance(previous.p);
        foreach(var current in input)
        {
            float cd=distance(current.p);bool a=pd>=0,b=cd>=0;
            if(a!=b){float t=pd/(pd-cd);result.Add(new V(Vector3.Lerp(previous.p,current.p,t),Vector3.Lerp(previous.n,current.n,t).normalized,Vector2.Lerp(previous.uv,current.uv,t)));}
            if(b)result.Add(current);previous=current;pd=cd;
        }
        return result;
    }
    static Material InteriorMaterial(Material source)
    {
        if(source==null)throw new InvalidOperationException("Crown material missing.");
        if(insideMaterials.TryGetValue(source,out var cached))return cached;
        // Idempotence: already-owned clones are returned without clone-of-clone names.
        if(AssetDatabase.GetAssetPath(source).StartsWith(Art+"/",StringComparison.Ordinal))return source;
        string name=source.name.Replace(" (Instance)","")+"_Interior";string path=Art+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_Cull",0);material.doubleSidedGI=true;material.enableInstancing=true;EditorUtility.SetDirty(material);insideMaterials[source]=material;return material;
    }
    static Material MakeMaterial(string name,Color color)
    {string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.enableInstancing=true;return m;}
    static float WallX(int side,float y,float z)
    {
        float nearest=float.PositiveInfinity;
        foreach(var t in surface)
        {
            var a=new Vector2(t[0].y,t[0].z);var b=new Vector2(t[1].y,t[1].z);var c=new Vector2(t[2].y,t[2].z);var p=new Vector2(y,z);
            float denominator=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(denominator)<.00001f)continue;
            float u=((b.y-c.y)*(p.x-c.x)+(c.x-b.x)*(p.y-c.y))/denominator;
            float v=((c.y-a.y)*(p.x-c.x)+(a.x-c.x)*(p.y-c.y))/denominator;float w=1-u-v;
            if(u<0||v<0||w<0)continue;float x=u*t[0].x+v*t[1].x+w*t[2].x;
            if((x-centerX)*side<4.3f)continue;nearest=Mathf.Min(nearest,(x-centerX)*side);
        }
        return float.IsPositiveInfinity(nearest)?float.NaN:centerX+side*nearest;
    }
    static void AddShoeHardware(Transform parent,Material brass,Material ceramic)
    {
        foreach(int side in new[]{-1,1})
        {
            for(float z=-15;z<48;z+=10.5f)
            {
                float y=floor+4.8f,x=WallX(side,y,z);if(float.IsNaN(x))continue;
                var ring=Ring(.38f,.235f,.12f,20);var g=MeshObject(parent,"ShoeEyelet",ring,brass);
                g.transform.position=new Vector3(x-side*.075f,y,z);g.transform.rotation=Quaternion.Euler(0,side*90,0);hardware++;
            }
            for(float z=-19;z<53;z+=1.55f)
            {
                float y=floor+1.45f,x=WallX(side,y,z);if(float.IsNaN(x))continue;
                Box(parent,"SoleStitch",new Vector3(x-side*.045f,y,z),new Vector3(.08f,.075f,.55f),ceramic);hardware++;
            }
        }
    }
    static Mesh Ring(float outer,float inner,float depth,int sides)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
        Vector3 P(int i,float r,float z){float a=i*Mathf.PI*2/sides;return new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,z);}
        for(int i=0;i<sides;i++){int n=(i+1)%sides;float f=-depth*.5f,b=depth*.5f;Quad(P(i,outer,f),P(i,inner,f),P(n,inner,f),P(n,outer,f));Quad(P(i,outer,b),P(n,outer,b),P(n,inner,b),P(i,inner,b));Quad(P(i,outer,f),P(n,outer,f),P(n,outer,b),P(i,outer,b));Quad(P(i,inner,f),P(i,inner,b),P(n,inner,b),P(n,inner,f));}
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void Pedestal(Transform parent,Vector3 p,float height,Material ceramic,Material brass,Material dark)
    {
        height=Mathf.Max(.25f,height);
        Cylinder(parent,"OfferingCeramicPedestal",p+Vector3.up*height*.5f,new Vector3(4.1f,height*.5f,4.1f),ceramic);
        Cylinder(parent,"OfferingBrassReveal",p+Vector3.up*(height-.12f),new Vector3(4.15f,.055f,4.15f),brass);
        // Keep the dark cap2.5cm above the ceramic cap and1cm below the
        // unchanged offering base; coplanar fans otherwise produce a white X.
        Cylinder(parent,"OfferingInsetTop",p+Vector3.up*(height-.01f),new Vector3(3.90f,.035f,3.90f),dark);
    }
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.position=p;g.transform.localScale=size;Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<MeshRenderer>().sharedMaterial=material;return g;}
    static void Cylinder(Transform parent,string name,Vector3 p,Vector3 size,Material material){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.position=p;g.transform.localScale=size;Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<MeshRenderer>().sharedMaterial=material;}
    static GameObject MeshObject(Transform parent,string name,Mesh mesh,Material material){var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;return g;}
    static Mesh SaveMesh(Mesh mesh,string name){string path=Art+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}return mesh;}
    static void Merge(Transform parent)
    {
        var filters=parent.GetComponentsInChildren<MeshFilter>(true);int index=0;
        foreach(var batch in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
        {var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(batch.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();MeshObject(parent,"ShoeHardwareBatch"+index,SaveMesh(mesh,"ShoeHardwareBatch"+index),batch.Key);index++;}
        foreach(var f in filters)Object.DestroyImmediate(f.gameObject);
        foreach(var r in parent.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
    }
    static Bounds BoundsOf(Renderer[] rs){if(rs.Length==0)throw new InvalidOperationException("Offering has no renderers.");var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static string ProtectedState(Scene scene)
    {
        var roots=scene.GetRootGameObjects();var scripts=roots.SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null).OrderBy(c=>c.GetInstanceID());var colliders=roots.SelectMany(r=>r.GetComponentsInChildren<Collider>(true)).OrderBy(c=>c.GetInstanceID());
        return string.Join("\n",scripts.Select(c=>c.GetInstanceID()+":"+EditorJsonUtility.ToJson(c)+":"+c.transform.localToWorldMatrix.ToString()))+"\n"+string.Join("\n",colliders.Select(c=>c.GetInstanceID()+":"+EditorJsonUtility.ToJson(c)+":"+c.transform.localToWorldMatrix.ToString()));
    }
    struct V{public Vector3 p,n;public Vector2 uv;public V(Vector3 p,Vector3 n,Vector2 uv){this.p=p;this.n=n;this.uv=uv;}}
    sealed class MeshBuilder
    {
        readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();readonly List<Vector2> uv=new List<Vector2>();readonly List<int>[] indices;readonly Transform filter;
        public MeshBuilder(int subs,Transform filter){indices=Enumerable.Range(0,subs).Select(_=>new List<int>()).ToArray();this.filter=filter;}
        public void Triangle(int sub,V a,V b,V c){foreach(var v in new[]{a,b,c}){indices[sub].Add(vertices.Count);vertices.Add(filter.InverseTransformPoint(v.p));normals.Add(filter.localToWorldMatrix.transpose.MultiplyVector(v.n).normalized);uv.Add(v.uv);}}
        public Mesh Finish(string name){var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=indices.Length;for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i);mesh.RecalculateBounds();return mesh;}
    }
}
