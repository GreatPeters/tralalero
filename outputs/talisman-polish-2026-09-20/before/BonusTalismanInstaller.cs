using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BonusTalismanInstaller
{
    public const string Root = "Assets/ShooterSurvival/Resources/BonusTalisman";
    public static readonly string[] SceneNames = { "Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop" };

    [MenuItem("Tools/Bonus/Build and Apply Common Talisman")]
    public static void BuildAndApply() { Debug.Log(ApplyAll()); }

    public static object ApplyAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save current scene edits before authoring.");
        BuildArt();
        int prefabs = 0, placements = 0;
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ShooterSurvival/Prefabs/Walls" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset.GetComponentsInChildren<WallScript>(true).Any(w => w.wallType == WallType.BuffWall)) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponentsInChildren<WallScript>(true).Length == 1 && root.GetComponent<BonusWallLifetimeRoot>() == null)
                    root.AddComponent<BonusWallLifetimeRoot>();
                foreach (var wall in root.GetComponentsInChildren<WallScript>(true))
                    if (wall.wallType == WallType.BuffWall) BonusTalismanPresentation.Refresh(wall);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabs++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string name in SceneNames)
        {
            string path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var wall in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WallScript>(true)))
                {
                    if (wall.wallType != WallType.BuffWall) continue;
                    BonusTalismanPresentation.Refresh(wall);
                    EditorUtility.SetDirty(wall.gameObject);
                    placements++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        AssetDatabase.SaveAssets();
        return new { prefabs, placements, visual = Root + "/Talisman.prefab" };
    }

    public static void BuildArt()
    {
        Directory.CreateDirectory(Root);
        AssetDatabase.Refresh();
        var paper = Surface("IvoryPaper", new Color(.95f, .88f, .70f), 0);
        var navy = Surface("NavyEnamel", new Color(.025f, .07f, .15f), .4f);
        var gold = Surface("Rivet", new Color(.8f, .65f, .36f), .65f);
        var ink = Surface("PrintedBorder", new Color(.035f, .09f, .18f), 0);
        CreateGlow();
        var trail = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Trail.mat");
        if (trail == null) { trail = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(trail, Root + "/Trail.mat"); }
        var center = new[] { new Vector2(-.51f,-.87f),new Vector2(-.09f,-.87f),new Vector2(0,-.69f),new Vector2(.09f,-.87f),new Vector2(.51f,-.87f),new Vector2(.55f,.82f),new Vector2(-.55f,.82f) };
        var wing = new[] { new Vector2(0,-.76f),new Vector2(.38f,-.64f),new Vector2(.38f,.67f),new Vector2(0,.81f) };
        Mesh centerMesh = Bevel("PaperCenter", center, .045f);
        Mesh wingMesh = Bevel("PaperWing", wing, .04f);
        Mesh centerPrint = Border("CenterPrint", center, .83f, .86f);
        Mesh wingPrint = Border("WingPrint", wing, .80f, .84f);
        Mesh clipMesh = Bevel("Clip", Rectangle(.29f,.45f), .12f);
        Mesh badgeMesh = Bevel("LabelBadge", Rectangle(2.9f,.46f), .035f);
        var root = new GameObject("Talisman");
        try
        {
            var visual = root.AddComponent<BonusTalismanVisual>();
            visual.paper = Child(root.transform,"Paper");
            Piece(visual.paper,"Center",centerMesh,paper,Vector3.zero);
            Piece(visual.paper,"NavyPrint",centerPrint,ink,new Vector3(0,0,-.026f));
            visual.leftFold = Child(visual.paper,"LeftFold"); visual.leftFold.localPosition = new Vector3(-.51f,0,.015f);
            var leftPiece = Piece(visual.leftFold,"LeftPaper",wingMesh,paper,Vector3.zero); leftPiece.localScale = new Vector3(-1,1,1);
            var leftPrint = Piece(visual.leftFold,"LeftPrint",wingPrint,ink,new Vector3(0,0,-.025f)); leftPrint.localScale = new Vector3(-1,1,1);
            visual.rightFold = Child(visual.paper,"RightFold"); visual.rightFold.localPosition = new Vector3(.51f,0,.015f);
            Piece(visual.rightFold,"RightPaper",wingMesh,paper,Vector3.zero);
            Piece(visual.rightFold,"RightPrint",wingPrint,ink,new Vector3(0,0,-.025f));
            Piece(visual.paper,"NavyClip",clipMesh,navy,new Vector3(0,.85f,-.065f));
            var rivet = GameObject.CreatePrimitive(PrimitiveType.Sphere); rivet.name="Rivet";
            UnityEngine.Object.DestroyImmediate(rivet.GetComponent<Collider>());
            rivet.transform.SetParent(visual.paper,false); rivet.transform.localPosition=new Vector3(0,.85f,-.14f);rivet.transform.localScale=new Vector3(.12f,.12f,.05f);
            rivet.GetComponent<Renderer>().sharedMaterial=gold;
            visual.icon = Child(root.transform,"StatIcon").gameObject.AddComponent<SpriteRenderer>();
            visual.icon.transform.localPosition = new Vector3(0,.03f,-.075f);
            visual.icon.sortingOrder=2;
            visual.labelRoot=Child(root.transform,"Label").gameObject;visual.labelRoot.transform.localPosition=new Vector3(0,-1.22f,0);
            Piece(visual.labelRoot.transform,"Badge",badgeMesh,navy,Vector3.zero);
            visual.label=Child(visual.labelRoot.transform,"Text").gameObject.AddComponent<TextMeshPro>();
            visual.label.transform.localPosition=new Vector3(0,0,-.045f);
            visual.label.font=HarborRefinementFontBuilder.Font;
            visual.label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Resources/HarborRefinement/BonusReadable.mat");
            visual.label.rectTransform.sizeDelta=new Vector2(2.75f,.44f);
            visual.label.fontSize=2.9f;visual.label.enableAutoSizing=true;visual.label.fontSizeMin=1.7f;visual.label.fontSizeMax=2.9f;
            visual.label.alignment=TextAlignmentOptions.MidlineGeoAligned;visual.label.textWrappingMode=TextWrappingModes.NoWrap;
            visual.SetOpen(0);
            visual.SetContent(Resources.Load<Sprite>("WallBonusIcons/WallBonus_Attack"),"공격력 +14%",new Color(1,.68f,.22f));
            foreach(var renderer in root.GetComponentsInChildren<Renderer>()) { renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false; }
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Talisman.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
    }

    static Transform Child(Transform parent,string name) { var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform; }
    static Transform Piece(Transform parent,string name,Mesh mesh,Material material,Vector3 position)
    {var t=Child(parent,name);t.localPosition=position;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;}
    static Vector2[] Rectangle(float width,float height) => new[]{new Vector2(-width/2,-height/2),new Vector2(width/2,-height/2),new Vector2(width/2,height/2),new Vector2(-width/2,height/2)};
    static Material Surface(string name,Color color,float metallic)
    {
        string path=Root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.3f);
        GeneratedStylizedSurface.Apply(m);m.SetFloat("_Cull",0);EditorUtility.SetDirty(m);return m;
    }
    static Mesh SaveMesh(string name,List<Vector3> vertices,List<int> triangles)
    {
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Root+"/"+name+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(previous==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,previous);UnityEngine.Object.DestroyImmediate(mesh);mesh=previous;}
        return mesh;
    }
    static Mesh Bevel(string name,Vector2[] outline,float depth)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();int count=outline.Length;
        float[] factors={.94f,1,1,.94f};float[] zs={-depth*.5f,-depth*.25f,depth*.25f,depth*.5f};
        for(int r=0;r<4;r++)foreach(var p in outline)vertices.Add(new Vector3(p.x*factors[r],p.y*factors[r],zs[r]));
        for(int r=0;r<3;r++)for(int i=0;i<count;i++){int a=r*count+i,b=r*count+(i+1)%count,c=(r+1)*count+i,d=(r+1)*count+(i+1)%count;triangles.AddRange(new[]{a,c,b,b,c,d});}
        int front=vertices.Count;vertices.Add(new Vector3(0,0,zs[0]));int back=vertices.Count;vertices.Add(new Vector3(0,0,zs[3]));
        for(int i=0;i<count;i++){triangles.AddRange(new[]{front,(i+1)%count,i,back,3*count+i,3*count+(i+1)%count});}
        return SaveMesh(name,vertices,triangles);
    }
    static Mesh Border(string name,Vector2[] points,float inner,float outer)
    {
        var v=new List<Vector3>();var tris=new List<int>();
        foreach(var p in points){v.Add(new Vector3(p.x*inner,p.y*inner,0));v.Add(new Vector3(p.x*outer,p.y*outer,0));}
        for(int i=0;i<points.Length;i++){int a=i*2,b=((i+1)%points.Length)*2;tris.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        return SaveMesh(name,v,tris);
    }
    static void CreateGlow()
    {
        const int size=64;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2)));}
        texture.Apply();string path=Root+"/SoftGlow.png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=64;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
    }
}
