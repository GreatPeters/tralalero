using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using IndianOceanAssets.ShooterSurvival;

public static class BonusTalismanRibbon
{
    const string Folder="Assets/ShooterSurvival/Resources/BonusTalisman/Polished/";
    static readonly HashSet<string> built=new();
    public static void Apply(BonusTalismanVisual visual)
    {
        visual.enhancementIcons=AssetDatabase.LoadAssetAtPath<SpriteDatabase>(OriginalUpgradeArtwork.DatabasePath);
        var model=visual.paper.Find("FoldedPaperModel");
        if(model!=null)foreach(var t in model.GetComponentsInChildren<Transform>(true))
            if(t.name=="ClipFront"||t.name=="ClipBack"||t.name=="ClipBend"||t.name=="Rivet")t.gameObject.SetActive(false);
        var cord=EnsureChild(visual.paper,"CrimsonCord");var eyelet=EnsureChild(visual.paper,"GoldEyelet");
        var red=Material("CrimsonCord",new Color(.67f,.035f,.055f));var gold=Material("CordGold",new Color(1,.68f,.20f));
        var paths=new[]{Loop(new Vector3(-.095f,1.07f,-.16f),.12f,.055f,-25),Loop(new Vector3(.095f,1.07f,-.16f),.12f,.055f,25),
            new[]{new Vector3(-.045f,.91f,-.17f),new Vector3(0,1.03f,-.17f),new Vector3(.045f,.91f,-.17f)},
            Loop(new Vector3(0,1.02f,-.18f),.035f,.035f)};
        SetMesh(cord,MeshAsset("CrimsonCord",paths,.019f),red);
        SetMesh(eyelet,MeshAsset("GoldEyelet",new[]{Loop(new Vector3(0,.90f,-.16f),.066f,.066f)},.015f),gold);
        EditorUtility.SetDirty(visual);
    }
    static Transform EnsureChild(Transform parent,string name)
    {
        var child=parent.Find(name);if(child==null){var go=new GameObject(name);child=go.transform;child.SetParent(parent,false);}return child;
    }
    static Vector3[] Loop(Vector3 center,float x,float y,float angle=0)=>Enumerable.Range(0,25).Select(i=>center+Quaternion.Euler(0,0,angle)*new Vector3(Mathf.Cos(i*Mathf.PI/12)*x,Mathf.Sin(i*Mathf.PI/12)*y,0)).ToArray();
    static Mesh MeshAsset(string name,Vector3[][] paths,float thickness)
    {
        string path=Folder+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null&&built.Contains(name))return mesh;
        var vertices=new List<Vector3>();var triangles=new List<int>();
        foreach(var points in paths)
        {
            int offset=vertices.Count;
            for(int i=0;i<points.Length;i++)
            {
                Vector3 tangent=(points[Mathf.Min(points.Length-1,i+1)]-points[Mathf.Max(0,i-1)]).normalized;
                Vector3 normal=Vector3.Cross(tangent,Vector3.forward).normalized;
                for(int side=0;side<6;side++)vertices.Add(points[i]+thickness*(normal*Mathf.Cos(side*Mathf.PI/3)+Vector3.forward*Mathf.Sin(side*Mathf.PI/3)));
            }
            for(int i=0;i<points.Length-1;i++)for(int side=0;side<6;side++)
            {
                int a=offset+i*6+side,b=offset+i*6+(side+1)%6,c=a+6,d=b+6;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
        }
        bool create=mesh==null;if(create)mesh=new Mesh{name=name};else mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);built.Add(name);return mesh;
    }
    static Material Material(string name,Color color)
    {
        string path=Folder+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name};AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
    }
    static void SetMesh(Transform target,Mesh mesh,Material material)
    {
        var filter=target.GetComponent<MeshFilter>();if(filter==null)filter=target.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
        var renderer=target.GetComponent<MeshRenderer>();if(renderer==null)renderer=target.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
}
