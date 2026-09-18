using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MobileCoinBuilder
{
    public static object Build()
    {
        const string folder = "Assets/ShooterSurvival/Resources/VFX";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        var vertices = new List<Vector3>(); var colors = new List<Color>(); var indices = new List<int>();
        Color edge = MobileUIArt.Hex("#99601A"), rim = MobileUIArt.Hex("#FFF3A6"), gold = MobileUIArt.Hex("#FFCC3F"), inset = MobileUIArt.Hex("#DA8C1D");
        void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            int start = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            colors.Add(color); colors.Add(color); colors.Add(color);
            indices.Add(start); indices.Add(start+1); indices.Add(start+2);
        }
        Vector3 Point(float radius, float angle, float z) => new Vector3(Mathf.Cos(angle)*radius, Mathf.Sin(angle)*radius,z);
        void Ring(float outside, float inside, float z, Color color)
        {
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16, b=(i+1)*Mathf.PI/16;
                Triangle(Point(outside,a,z),Point(outside,b,z),Point(inside,b,z),color);
                Triangle(Point(outside,a,z),Point(inside,b,z),Point(inside,a,z),color);
            }
        }
        for(int i=0;i<32;i++)
        {
            float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
            Triangle(Point(.58f,a,-.065f),Point(.58f,b,-.065f),Point(.58f,b,.065f),i%2==0?edge:inset);
            Triangle(Point(.58f,a,-.065f),Point(.58f,b,.065f),Point(.58f,a,.065f),i%2==0?edge:inset);
        }
        foreach(float side in new[]{-1f,1f})
        {
            Ring(.58f,.535f,side*.066f,edge); Ring(.535f,.47f,side*.067f,rim);
            Ring(.47f,.43f,side*.068f,inset); Ring(.43f,0,side*.069f,gold);
            for(int i=0;i<10;i++)
            {
                float a=Mathf.PI*.5f+i*Mathf.PI/5,b=Mathf.PI*.5f+(i+1)*Mathf.PI/5;
                Triangle(new Vector3(0,0,side*.072f),Point(i%2==0?.29f:.13f,a,side*.072f),Point(i%2==0?.13f:.29f,b,side*.072f),edge);
            }
            // Small fixed glint remains readable without particles or transparent overdraw.
            var center=new Vector3(-.31f,.32f,side*.075f);
            Triangle(center+Vector3.up*.12f,center+Vector3.right*.055f,center-Vector3.up*.12f,Color.white);
            Triangle(center+Vector3.up*.12f,center-Vector3.up*.12f,center-Vector3.right*.055f,Color.white);
        }
        const string meshPath=folder+"/CoinTokenMesh.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(mesh==null){mesh=new Mesh{name="Coin Token"};AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
        mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        const string materialPath=folder+"/CoinToken.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("ShooterSurvival/CoinToken"));AssetDatabase.CreateAsset(material,materialPath);}
        AssetDatabase.SaveAssets(); return new { triangles=indices.Count/3,vertices=vertices.Count,diameter=1.16f,draws=1 };
    }
}
