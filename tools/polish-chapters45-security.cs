using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

// Pure presentation: children follow the original moving panel/shield transforms.
// No collider, hit shape, target reference, health, opening offset or label changes.
public static class PolishChapters45Security
{
    const string Art = "Assets/ShooterSurvival/Models/Chapters/Chapters45/SecurityRefinementV1";
    const string Record = "outputs/chapters45-2026-10-02";
    const string Owned = "SecurityPresentation_V1";
    static Material[] materials;
    static Mesh shutter, captain, shield;

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Idle Edit Mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve dirty scenes first.");
        var setup = EditorSceneManager.GetSceneManagerSetup(); var receipts = new List<SceneReceipt>();
        Directory.CreateDirectory(Art); Directory.CreateDirectory(Record); AssetDatabase.Refresh();
        Materials(); shutter = BuildTarget(false); captain = BuildTarget(true); shield = BuildShield();
        try
        {
            foreach (string name in new[] { "Jamsil", "ShoeTower" })
            {
                string path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
                var scene = EditorSceneManager.OpenScene(path); bool saved = false;
                try
                {
                    var director = Object.FindObjectsByType<Chapter45Director>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(d => d.gameObject.scene == scene);
                    var targets = director.GetComponentsInChildren<Chapter45Target>(true);
                    if (targets.Length != (name == "Jamsil" ? 3 : 5)) throw new InvalidOperationException("Unexpected target roster in " + name);
                    foreach (var target in targets) ValidateTarget(target);
                    string before = ProtectedState(scene);
                    string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
                    string backup = Record + "/" + name + "-before-security-refinement-" + stamp + ".unity";
                    File.Copy(path, backup, false);
                    foreach (var target in targets) InstallTarget(target);
                    int finVertices = name == "Jamsil" ? RepairEntranceFin(director) : 0;
                    if (before != ProtectedState(scene)) throw new InvalidOperationException("Security presentation changed protected target/gameplay/collider state.");
                    var owned = targets.SelectMany(t => t.GetComponentsInChildren<Transform>(true)).Where(t => t.name == Owned).ToArray();
                    if (owned.Any(t => t.GetComponentsInChildren<Collider>(true).Length != 0)) throw new InvalidOperationException("Presentation must have no colliders.");
                    AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + path);
                    saved = true;
                    receipts.Add(new SceneReceipt { scene = path, backup = backup, targets = targets.Length, presentationRoots = owned.Length, entranceFinVerticesRaised = finVertices,
                        targetGameplayCollidersLabelsAndPanelTransformsUnchanged = true,
                        notes = "Original panel and shield primitives hidden with renderer.enabled=false. New authored articulated meshes are children of the same moving transforms. Shield perimeter retains original shieldVisual active state. TargetRenderers remains unchanged." });
                }
                finally { if (!saved && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        var result = new Receipt { scenes = receipts.ToArray(), shutterTriangles = shutter.triangles.Length / 3, captainTriangles = captain.triangles.Length / 3, shieldTriangles = shield.triangles.Length / 3,
            materialCount = materials.Length, nativeReviewPending = "Verify closed/open/replayed panels, captain vulnerable/amber states, health-label clearance and safe corridor in native captures. Authored replacement geometry is not an AI-generated model; installed toll/cold-store models did not fit this mechanism." };
        File.WriteAllText(Record + "/security-refinement-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + ".json", JsonUtility.ToJson(result, true));
        return result;
    }

    static void ValidateTarget(Chapter45Target target)
    {
        if (target.panel == null || target.panelCollider == null || target.hitCollider == null || target.healthLabel == null) throw new InvalidOperationException("Incomplete target " + target.name);
        if (Vector3.Distance(target.panel.localScale, new Vector3(3, 3, 1)) > .01f) throw new InvalidOperationException("Inspect unexpected panel dimensions for " + target.name);
        var original = target.panel.GetComponent<Renderer>();
        if (original == null || target.targetRenderers.Contains(original)) throw new InvalidOperationException("Original panel renderer reset contract changed for " + target.name);
        if (target.captain && (target.shieldVisual == null || target.shieldVisual.GetComponent<Renderer>() == null)) throw new InvalidOperationException("Captain shield renderer missing.");
    }
    static void InstallTarget(Chapter45Target target)
    {
        ReplaceVisualChild(target.panel, target.captain ? captain : shutter, materials);
        target.panel.GetComponent<Renderer>().enabled = false;
        if (target.captain)
        {
            ReplaceVisualChild(target.shieldVisual.transform, shield, new[] { materials[4] });
            target.shieldVisual.GetComponent<Renderer>().enabled = false;
        }
    }
    static void ReplaceVisualChild(Transform parent, Mesh mesh, Material[] mats)
    {
        var old = parent.Find(Owned); if (old != null) Object.DestroyImmediate(old.gameObject);
        var visual = new GameObject(Owned, typeof(MeshFilter), typeof(MeshRenderer));
        visual.transform.SetParent(parent, false);
        // Existing panel scale contains dimensions. Cancel only that local scale.
        var scale = parent.localScale;
        visual.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
        visual.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = visual.GetComponent<MeshRenderer>(); renderer.sharedMaterials = mats;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
    }
    static void Materials()
    {
        materials = new[] {
            Mat("GraphiteMechanism", new Color(.055f,.085f,.11f), .35f, .6f),
            Mat("BrushedFrame", new Color(.67f,.72f,.73f), .45f, .65f),
            Mat("BlueSteelSlats", new Color(.12f,.24f,.30f), .4f, .45f),
            Mat("CyanCore", new Color(.15f,.80f,.94f), .55f, .2f, true),
            Mat("AmberSafety", new Color(1f,.57f,.08f), .35f, .15f, true)
        };
    }
    static Material Mat(string name, Color color, float smooth, float metal, bool emission = false)
    {
        string path = Art + "/" + name + ".mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", smooth); mat.SetFloat("_Metallic", metal); mat.enableInstancing = true;
        if (emission) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * .4f); }
        EditorUtility.SetDirty(mat); return mat;
    }
    static Mesh BuildTarget(bool isCaptain)
    {
        var b = new Builder(5);
        // Recessed body and two deep guide rails visibly separate from the slat face.
        b.Box(new Vector3(0,0,.24f), new Vector3(2.45f,2.54f,.42f), .14f, 0);
        foreach (int side in new[] { -1,1 })
        {
            b.Box(new Vector3(side*1.335f,-.06f,0), new Vector3(.29f,2.7f,.98f), .065f, 1);
            b.Box(new Vector3(side*1.13f,-.1f,-.36f), new Vector3(.10f,2.40f,.18f), .02f, 0);
            for (int bolt=0; bolt<4; bolt++) b.Disc(new Vector3(side*1.335f,-1.05f+bolt*.68f,-.516f),.044f,.045f,6,0);
        }
        b.Box(new Vector3(0,1.245f,0), new Vector3(2.97f,.48f,.99f), .11f, 1);
        b.Box(new Vector3(0,-1.32f,0), new Vector3(2.97f,.30f,1.0f), .055f, 0);
        b.Box(new Vector3(0,1.25f,-.512f), new Vector3(2.17f,.13f,.08f), .045f, 0);
        b.Box(new Vector3(0,1.25f,-.559f), new Vector3(isCaptain?1.54f:1.03f,.062f,.02f), .02f, 3);
        // Eight individual chamfered slats have distinct lips and physical recesses.
        for (int row=0; row<8; row++)
        {
            float y=-.98f+row*.267f;
            b.Box(new Vector3(0,y,-.21f),new Vector3(2.28f,.232f,.31f),.055f,2);
            b.Box(new Vector3(0,y-.092f,-.37f),new Vector3(2.16f,.042f,.05f),.01f,1);
        }
        // Shootable circular power lock, separate from the existing health label.
        float radius=isCaptain?.65f:.52f;
        b.Disc(new Vector3(0,-.04f,-.395f),radius+.075f,.21f,16,0);
        b.Ring(new Vector3(0,-.04f,-.514f),radius,radius-.075f,.085f,24,1);
        b.Disc(new Vector3(0,-.04f,-.565f),radius-.13f,.07f,20,3);
        b.Ring(new Vector3(0,-.04f,-.608f),radius-.235f,radius-.27f,.025f,20,1);
        b.Box(new Vector3(0,-.04f,-.632f),new Vector3(.07f,(radius-.29f)*2,.018f),.012f,0);
        for(int i=0;i<(isCaptain?8:4);i++)
        {
            float angle=i*Mathf.PI*2/(isCaptain?8:4);
            b.Box(new Vector3(Mathf.Cos(angle)*(radius+.02f),Mathf.Sin(angle)*(radius+.02f)-.04f,-.575f),new Vector3(.10f,.16f,.04f),.02f,1,Quaternion.Euler(0,0,angle*Mathf.Rad2Deg-90));
        }
        // Lock handle, hazard toe strip and two louvred service vents.
        b.Box(new Vector3(.90f,-.02f,-.48f),new Vector3(.14f,.61f,.13f),.05f,0);
        b.Box(new Vector3(.90f,-.02f,-.565f),new Vector3(.055f,.37f,.07f),.018f,1);
        b.Box(new Vector3(0,-1.275f,-.519f),new Vector3(2.50f,.14f,.035f),.025f,4);
        for(int i=0;i<12;i++) b.Box(new Vector3(-1.13f+i*.205f,-1.275f,-.541f),new Vector3(.10f,.16f,.02f),.01f,0,Quaternion.Euler(0,0,-30));
        foreach(int side in new[]{-1,1}) for(int v=0;v<3;v++) b.Box(new Vector3(side*.79f,.79f+v*.065f,-.407f),new Vector3(.34f,.028f,.025f),.007f,0);
        return SaveMesh(b.Finish(isCaptain?"CaptainPowerLock":"ArticulatedSecurityShutter"),isCaptain?"CaptainPowerLock":"ArticulatedSecurityShutter");
    }
    static Mesh BuildShield()
    {
        var b=new Builder(1);
        // Open amber perimeter conveys invulnerability without hiding core or HP text.
        foreach(int side in new[]{-1,1})
        {
            b.Box(new Vector3(side*1.56f,0,0),new Vector3(.10f,2.94f,.07f),.035f,0);
            b.Box(new Vector3(0,side*1.56f,0),new Vector3(2.94f,.10f,.07f),.035f,0);
            foreach(int y in new[]{-1,1}) b.Box(new Vector3(side*1.42f,y*1.42f,0),new Vector3(.26f,.26f,.10f),.07f,0);
        }
        return SaveMesh(b.Finish("CaptainAmberPerimeter"),"CaptainAmberPerimeter");
    }
    static Mesh SaveMesh(Mesh mesh,string name)
    {
        string path=Art+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
        return mesh;
    }
    static int RepairEntranceFin(Chapter45Director director)
    {
        var tower=director.transform.Find("Scenery/Jamsil_CityPolish_V1/JamsilCanonicalTower");
        if(tower==null)throw new InvalidOperationException("Polished Jamsil landmark missing.");
        int changed=0,changedMeshes=0;
        foreach(var filter in tower.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("TowerLandmark_Batch",StringComparison.Ordinal)))
        {
            var mesh=filter.sharedMesh;if(mesh==null||!mesh.isReadable)continue;
            var vertices=mesh.vertices;var found=new List<int>();
            for(int i=0;i<vertices.Length;i++)
            {
                var world=filter.transform.TransformPoint(vertices[i]);
                // Only the lower cap of the central front fin: all side fins, doorway,
                // mullions, crown and collider geometry are outside this exact window.
                if(Mathf.Abs(world.x)<.36f && world.z>1844f && world.z<1846f && world.y>-.7f && world.y<.7f)found.Add(i);
            }
            if(found.Count==0)continue;
            if(found.Count<4||found.Count>24)throw new InvalidOperationException("Unexpected central fin topology: "+found.Count);
            foreach(int index in found){var world=filter.transform.TransformPoint(vertices[index]);world.y=12;vertices[index]=filter.transform.InverseTransformPoint(world);}
            var copy=Object.Instantiate(mesh);copy.name="JamsilEntranceFinRepaired";copy.vertices=vertices;copy.RecalculateNormals();copy.RecalculateBounds();
            filter.sharedMesh=SaveMesh(copy,"JamsilEntranceFinRepaired");changed+=found.Count;changedMeshes++;
        }
        if(changedMeshes>1)throw new InvalidOperationException("Fin repair matched more than one material batch.");
        return changed;
    }
    static string ProtectedState(Scene scene)
    {
        var roots=scene.GetRootGameObjects();
        var behaviour=roots.SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null).OrderBy(c=>c.GetInstanceID());
        var colliders=roots.SelectMany(r=>r.GetComponentsInChildren<Collider>(true)).OrderBy(c=>c.GetInstanceID());
        var targets=roots.SelectMany(r=>r.GetComponentsInChildren<Chapter45Target>(true)).OrderBy(c=>c.GetInstanceID());
        return string.Join("\n",behaviour.Select(c=>c.GetInstanceID()+":"+EditorJsonUtility.ToJson(c)))+"\n"+
            string.Join("\n",colliders.Select(c=>c.GetInstanceID()+":"+EditorJsonUtility.ToJson(c)+":"+c.transform.localToWorldMatrix.ToString()))+"\n"+
            string.Join("\n",targets.Select(t=>t.GetInstanceID()+":"+t.transform.localToWorldMatrix.ToString()+":"+t.panel.localToWorldMatrix.ToString()+":"+t.healthLabel.transform.localToWorldMatrix.ToString()));
    }

    sealed class Builder
    {
        readonly List<Vector3> vertices=new List<Vector3>(); readonly List<Vector2> uv=new List<Vector2>(); readonly List<int>[] indices;
        public Builder(int materials){indices=Enumerable.Range(0,materials).Select(_=>new List<int>()).ToArray();}
        void Face(int material, params Vector3[] points)
        {
            int start=vertices.Count;foreach(var p in points){vertices.Add(p);uv.Add(new Vector2(p.x,p.y));}
            for(int i=1;i<points.Length-1;i++)indices[material].AddRange(new[]{start,start+i,start+i+1});
        }
        void Prism(Vector2[] ring,Vector3 center,float depth,int material,Quaternion rotation)
        {
            var front=ring.Select(p=>center+rotation*new Vector3(p.x,p.y,-depth*.5f)).ToArray();
            var back=ring.Select(p=>center+rotation*new Vector3(p.x,p.y,depth*.5f)).ToArray();
            Face(material,front);Face(material,back.Reverse().ToArray());
            for(int i=0;i<ring.Length;i++){int n=(i+1)%ring.Length;Face(material,front[i],back[i],back[n],front[n]);}
        }
        public void Box(Vector3 center,Vector3 size,float chamfer,int material,Quaternion? rotation=null)
        {
            float x=size.x*.5f,y=size.y*.5f,c=Mathf.Min(chamfer,Mathf.Min(x,y)*.8f);
            Prism(new[]{new Vector2(-x+c,-y),new Vector2(-x,-y+c),new Vector2(-x,y-c),new Vector2(-x+c,y),new Vector2(x-c,y),new Vector2(x,y-c),new Vector2(x,-y+c),new Vector2(x-c,-y)},center,size.z,material,rotation??Quaternion.identity);
        }
        public void Disc(Vector3 center,float radius,float depth,int sides,int material)
        {Prism(Enumerable.Range(0,sides).Select(i=>new Vector2(Mathf.Cos(-i*2*Mathf.PI/sides),Mathf.Sin(-i*2*Mathf.PI/sides))*radius).ToArray(),center,depth,material,Quaternion.identity);}
        public void Ring(Vector3 center,float outer,float inner,float depth,int sides,int material)
        {
            Vector3 P(int i,float radius,float z){float a=-i*2*Mathf.PI/sides;return center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,z);}
            for(int i=0;i<sides;i++){int n=(i+1)%sides;float f=-depth*.5f,b=depth*.5f;
                Face(material,P(i,outer,f),P(n,outer,f),P(n,inner,f),P(i,inner,f));
                Face(material,P(i,inner,b),P(n,inner,b),P(n,outer,b),P(i,outer,b));
                Face(material,P(i,outer,f),P(i,outer,b),P(n,outer,b),P(n,outer,f));
                Face(material,P(i,inner,f),P(n,inner,f),P(n,inner,b),P(i,inner,b));}
        }
        public Mesh Finish(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=indices.Length;
            for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();
            if(vertices.Any(v=>float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)))throw new InvalidOperationException("Invalid authored vertex.");
            return mesh;
        }
    }
    [Serializable] public sealed class SceneReceipt { public string scene,backup,notes; public int targets,presentationRoots,entranceFinVerticesRaised; public bool targetGameplayCollidersLabelsAndPanelTransformsUnchanged; }
    [Serializable] public sealed class Receipt { public SceneReceipt[] scenes; public int shutterTriangles,captainTriangles,shieldTriangles,materialCount; public string nativeReviewPending; }
}
