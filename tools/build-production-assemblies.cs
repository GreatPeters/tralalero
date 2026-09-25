using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class BuildProductionAssemblies
{
    const string Root="Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/";
    static Transform group;
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        Directory.CreateDirectory(Root+"Assemblies");AssetDatabase.Refresh();
        foreach(var kind in new[]{"FoodCounter","CoffeeCounter","Hall","Store","Restroom","Shelter","Fuel","Sign"})
        {
            group=new GameObject("Production_"+kind).transform;
            try
            {
                switch(kind)
                {
                    case "FoodCounter": Counter(false); break;
                    case "CoffeeCounter": Counter(true); break;
                    case "Hall": Hall();break;
                    case "Store": Store();break;
                    case "Restroom": Restroom();break;
                    case "Shelter": Shelter();break;
                    case "Fuel": Fuel();break;
                    case "Sign": Add("R06",0,0,0);Add("R05",0,5.2f,.2f);break;
                }
                var lod=group.gameObject.AddComponent<LODGroup>();
                lod.SetLODs(new[]{new LOD(.008f,group.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(group.gameObject,Root+"Assemblies/"+kind+".prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(group.gameObject);}
        }
        AssetDatabase.SaveAssets();return "Eight assemblies built from accepted production models.";
    }
    static Transform Add(string id,float x,float y,float z,float yaw=0,Vector3? size=null)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+id+".prefab")??throw new Exception("Missing "+id);
        var t=((GameObject)PrefabUtility.InstantiatePrefab(prefab,group)).transform;t.name="RST_"+id;t.localPosition=new Vector3(x,y,z);t.localRotation=Quaternion.Euler(0,yaw,0);
        if(size.HasValue){var b=LocalBounds(t);var v=size.Value;t.localScale=new Vector3(v.x/b.size.x,v.y/b.size.y,v.z/b.size.z);}
        return t;
    }
    static void Floor(float width,float depth)
    {
        for(int x=0;x<2;x++)for(int z=0;z<2;z++)Add("B01",(x-.5f)*width/2,-.12f,(z-.5f)*depth/2,0,new Vector3(width/2,.12f,depth/2));
        Add("B02",0,0,-depth/2,0,new Vector3(width,4,.3f));
        Add("B03",-width/2,0,-depth/4,90,new Vector3(depth/2,4,.2f));
        Add("B05",width/2,1,-depth/4,90,new Vector3(depth/2,2.8f,.15f));
        foreach(int side in new[]{-1,1})Add("B04",side*(width/2-.3f),0,depth/2-.3f);
        Add("B08",0,4.8f,-depth/4,0,new Vector3(width+.5f,.35f,depth/2));
        Add("B12",0,4.62f,-depth/4,0,new Vector3(width,.15f,depth/2));
        Add("B09",0,4.1f,depth/2,0,new Vector3(width,.7f,.25f));
        Add("F05",-width/2+.3f,3,depth/2-.25f,90);
        Add("F06",0,4.4f,-depth/4);
    }
    static void Counter(bool coffee)
    {
        var counter=Add("S01",0,0,0,0,new Vector3(5.6f,1.6f,1.6f));
        Add("S04",2,1.6f,0,180);
        if(coffee){Add("S05",-.6f,1.6f,0);Add("S13",1,1.6f,.2f);Add("P06",1.55f,1.6f,.2f);}
        else{Add("S02",-.8f,1.6f,0,0,new Vector3(2.4f,1.1f,1.2f));Add("S11",1,1.6f,.35f);Add("S12",1,1.7f,.35f);}
    }
    static void Hall()
    {
        Floor(14,10);
        Add("B06",0,0,5);Add("B07",-2,0,5.05f,0,new Vector3(1.1f,3.6f,.12f));Add("B07",2,0,5.05f,0,new Vector3(1.1f,3.6f,.12f));
        var parent=group;var kiosk=new GameObject("Food service").transform;kiosk.SetParent(group,false);kiosk.localPosition=new Vector3(0,0,-2.7f);group=kiosk;Counter(false);group=parent;
        foreach(int side in new[]{-1,1})
        {
            float x=side*4.2f;Add("F07",x,0,1);
            Add(side<0?"F08":"F09",x-1.8f,0,1,90);Add(side<0?"F09":"F08",x+1.8f,0,1,-90);
            Add("F10",x,1.4f,1);Add("S11",x,1.4f,1.6f);Add("S12",x,1.51f,1.6f);
        }
        Add("F11",0,.01f,1);Add("E01",-5.4f,0,-2.5f);Add("E03",3,0,-3.7f,0,new Vector3(3,2.3f,1));
    }
    static void Store()
    {
        Floor(14,10);Add("S03",-4.6f,0,2.8f,0,new Vector3(3.4f,1.6f,1.5f));Add("S04",-4.5f,1.6f,2.9f,180);
        Add("H04",-4.6f,0,1.3f);Add("S07",0,0,-4.2f,0,new Vector3(6,3.4f,.9f));
        Add("S08",5.4f,0,-3.2f);Add("S06",1.3f,0,0,0,new Vector3(4.4f,2.8f,1.6f));
        for(int i=0;i<5;i++)
        {
            Add("S09",-.1f+i*.62f,1.2f,.7f);Add("S10",-.1f+i*.62f,2,.7f);
            Add("S14",-2+i*.6f,1,-3.8f);Add("S15",-2+i*.6f,2,-3.8f);
        }
        Add("F02",6,0,4);Add("F03",6,1.1f,4,0,new Vector3(1.6f,1.6f,1.6f));Add("E04",-6,0,4,90);
        Add("T09",6.7f,3,2,90);
    }
    static void Restroom()
    {
        Floor(12,9);
        for(int i=0;i<3;i++)
        {
            float x=-4+i*2.4f;Add("T01",x-1,0,-2.8f);Add("T02",x,0,-1.6f);
            Add("T03",x,0,-3.4f);Add("T04",4.7f,1,-3+i*2.1f,-90);
            if(i<2)Add("T05",4.2f,.4f,-2+i*2.1f,-90);
        }
        Add("T06",-3,0,2.5f);Add("T07",-3,1.5f,2.15f);Add("T08",-3,2.1f,1.95f);
        Add("T09",0,3.7f,4.3f);Add("E02",-5.6f,0,-3.8f);Add("R12",-3,.01f,3.6f);
        Add("H05",3,0,3.5f,220);Add("P04",4,0,3.5f,220);Add("F10",-1.9f,1.5f,2.5f);
    }
    static void Shelter()
    {
        Add("B10",0,0,-2.1f);Add("B10",0,0,2.1f);Add("B11",0,3.35f,0);
        Add("F01",-2,0,0,90);Add("F01",2,0,0,-90);
        Add("E06",0,0,-4.8f);Add("F04",0,1.2f,-4.8f,0,new Vector3(3.3f,5,3.3f));
        Add("H07",1,0,1.7f,150);Add("P05",1,0,2.2f);Add("E07",0,4.5f,2.4f);
    }
    static void Fuel()
    {
        Add("G01",0,5.5f,0);
        foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})Add("B04",side*6,0,end*3.5f,0,new Vector3(.8f,5.5f,.8f));
        foreach(int side in new[]{-1,1})
        {
            Add("G03",side*4.8f,0,0);Add("G02",side*4.8f,.25f,0,side<0?90:-90);
            foreach(int z in new[]{-1,1})Add("G04",side*4.8f,0,z*2.2f);
        }
        Add("H06",2.8f,0,3,220);Add("R12",0,.01f,3.8f);
    }
    static Bounds LocalBounds(Transform t)
    {
        var b=new Bounds();bool first=true;
        foreach(var r in t.GetComponentsInChildren<Renderer>(true)){var c=r.bounds;foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){var p=t.InverseTransformPoint(c.center+Vector3.Scale(c.extents,new Vector3(x,y,z)));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}
        return b;
    }
}
