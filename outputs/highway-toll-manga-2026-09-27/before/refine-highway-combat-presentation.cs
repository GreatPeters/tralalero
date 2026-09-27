using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

public static class RefineHighwayCombatPresentation
{
    const string AssetsRoot="Assets/ShooterSurvival/Models/Highway/CombatRevision20260927";
    const string Prefabs="Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    static float S(string key)=>HighwayChapter2Data.Value(key);
    static HighwayChapter2Controller Chapter()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().name!="HighWay")throw new Exception("HighWay Edit Mode required");
        Directory.CreateDirectory(AssetsRoot);AssetDatabase.Refresh();EnvironmentVariableTables.Reload();return Object.FindFirstObjectByType<HighwayChapter2Controller>();
    }
    static void Dirty(Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
    static void Save(){Undo.FlushUndoRecordObjects();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Save failed");}
    static Transform Group(Transform parent,string name)
    {var t=parent.Find(name);if(t!=null)return t;var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Highway presentation");go.transform.SetParent(parent,false);return go.transform;}
    static Transform Rebuild(Transform parent,string name)
    {var prior=parent.Find(name);if(prior!=null)Undo.DestroyObjectImmediate(prior.gameObject);return Group(parent,name);}
    static Material Mat(string name,Color color,bool glow=false)
    {
        string path=AssetsRoot+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/JH/Model/Enemy/Garden_Spear/Material.001.mat")){name=name};AssetDatabase.CreateAsset(m,path);}
        if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",null);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",null);
        foreach(string p in new[]{"_BaseColor","_Color"})if(m.HasProperty(p))m.SetColor(p,color);
        foreach(string p in new[]{"_ColorDim","_ColorDimSteps","_ColorDimCurve","_ColorDimExtra","_ColorGradient","_UnityShadowColor"})if(m.HasProperty(p))m.SetColor(p,new Color(color.r*.66f,color.g*.66f,color.b*.66f,1));
        if(glow&&m.HasProperty("_EmissionColor")){m.SetColor("_EmissionColor",color*2);m.EnableKeyword("_EMISSION");}
        Dirty(m);return m;
    }
    static Material Navy=>Mat("Navy",new Color(.025f,.09f,.24f));
    static Material Gold=>Mat("Gold",new Color(1,.67f,.06f));
    static Material Ivory=>Mat("Ivory",new Color(1,.97f,.83f));
    static Transform Cube(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var t=Group(parent,name);var mesh=t.GetComponent<MeshFilter>();if(mesh==null){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh=t.gameObject.AddComponent<MeshFilter>();mesh.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(go);}
        var renderer=t.GetComponent<MeshRenderer>();if(renderer==null)renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;t.localPosition=position;t.localScale=size;return t;
    }
    static Mesh Store(string name,Vector3[] vertices,int[] triangles)
    {
        var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();string path=AssetsRoot+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);Dirty(saved);return saved;
    }
    static Transform Panel(Transform parent,string name,Vector3 position,Vector2 size,float depth,Material material)
    {
        float x=size.x*.5f,y=size.y*.5f,b=Mathf.Min(.15f,Mathf.Min(x,y)*.12f);var v=new List<Vector3>{new Vector3(0,0,-depth*.5f)};var tri=new List<int>();
        for(int ring=0;ring<3;ring++)
        {
            float xx=x-(ring==0?b:0),yy=y-(ring==0?b:0),z=ring==0?-depth*.5f:ring==1?-depth*.5f+b:depth*.5f;
            foreach(var p in new[]{new Vector2(-xx+b,-yy),new Vector2(xx-b,-yy),new Vector2(xx,-yy+b),new Vector2(xx,yy-b),new Vector2(xx-b,yy),new Vector2(-xx+b,yy),new Vector2(-xx,yy-b),new Vector2(-xx,-yy+b)})v.Add(new Vector3(p.x,p.y,z));
        }
        v.Add(new Vector3(0,0,depth*.5f));
        for(int i=0;i<8;i++){int j=(i+1)%8;tri.AddRange(new[]{0,1+j,1+i,25,17+i,17+j});for(int r=0;r<2;r++){int a=1+r*8+i,c=1+r*8+j;tri.AddRange(new[]{a,c,a+8,c,c+8,a+8});}}
        var t=Group(parent,name);t.localPosition=position;var filter=t.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=Store(name+"_"+size.x.ToString("F1")+"_"+size.y.ToString("F1"),v.ToArray(),tri.ToArray());t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
    }
    static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");
    static TextMeshPro Text(Transform parent,string name,string label,Vector3 p,Vector2 size,float font,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);go.transform.localPosition=p;var text=go.AddComponent<TextMeshPro>();text.font=Font;text.text=label;text.fontSize=font;text.color=color;text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;text.rectTransform.sizeDelta=size;return text;
    }
    static SpriteRenderer Icon(Transform parent,string name,Sprite sprite,Vector3 position,float width)
    {var t=Group(parent,name);t.localPosition=position;var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;if(sprite!=null)t.localScale=Vector3.one*width/sprite.bounds.size.x;return r;}
    static void Place(HighwayRoute route,Transform t,float d,float lane)
    {route.Sample(d,false,out var p,out var f);t.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*lane,Quaternion.LookRotation(f));Dirty(t);}
    static Bounds BoundsOf(Transform root)
    {
        var result=new Bounds();bool first=true;
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {var m=root.worldToLocalMatrix*r.transform.localToWorldMatrix;var b=r.localBounds;for(int i=0;i<8;i++){var p=m.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}}
        return result;
    }
    static Transform Star(Transform parent,string name,Vector3 position,float radius)
    {
        var points=new List<Vector3>{new Vector3(0,0,-.14f)};var triangles=new List<int>();
        for(int face=0;face<2;face++)for(int i=0;i<10;i++){float a=Mathf.PI*.5f+i*Mathf.PI/5,r=i%2==0?radius:radius*.44f;points.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,face==0?-.14f:.14f));}
        points.Add(new Vector3(0,0,.14f));for(int i=0;i<10;i++){int j=(i+1)%10;triangles.AddRange(new[]{0,j+1,i+1,21,i+11,j+11,i+1,j+1,i+11,j+1,j+11,i+11});}
        var t=Group(parent,name);t.localPosition=position;t.gameObject.AddComponent<MeshFilter>().sharedMesh=Store("Star_"+radius.ToString("F1"),points.ToArray(),triangles.ToArray());t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Gold;return t;
    }
    static Transform Model(Transform parent,string name,string path,float width,bool fitHeight=false)
    {
        var root=Group(parent,name);var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root);var b=BoundsOf(root);go.transform.localScale*=width/(fitHeight?b.size.y:b.size.x);b=BoundsOf(root);go.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
        foreach(var c in root.GetComponentsInChildren<Collider>(true)){c.enabled=false;Dirty(c);}return root;
    }
    static void StripedBase(Transform parent,string name,Vector3 position,float width)
    {
        var t=Group(parent,name);t.localPosition=position;Panel(t,"Base navy "+name,Vector3.zero,new Vector2(width,.5f),.8f,Navy);
        for(int i=0;i<5;i++){var band=Cube(t,"Safety stripe "+i,new Vector3(-width*.4f+i*width*.2f,0,-.43f),new Vector3(.28f,.4f,.035f),Gold);band.localRotation=Quaternion.Euler(0,0,-25);}
    }
    static Vector3 SkinPoint(Transform body,MeshCollider skin,Vector3 from,Vector3 direction)
    {
        if(!skin.Raycast(new Ray(body.TransformPoint(from),body.TransformDirection(direction)),out var hit,20))throw new Exception("Police decal missed "+from+" direction "+direction+" collider "+skin.bounds);
        return body.InverseTransformPoint(hit.point+hit.normal*.016f);
    }
    static void SkinPatch(Transform parent,Transform body,MeshCollider skin,string name,Vector3 from,Vector3 across,Vector3 down,Vector3 ray,Material material)
    {
        const int columns=8,rows=4;var points=new List<Vector3>();var triangles=new List<int>();
        for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)points.Add(SkinPoint(body,skin,from+across*(x/(float)columns)+down*(y/(float)rows),ray));
        for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
        {int a=y*(columns+1)+x,b=a+1,c=a+columns+1,d=c+1;bool reverse=Vector3.Dot(Vector3.Cross(points[b]-points[a],points[c]-points[a]),-ray)<0;triangles.AddRange(reverse?new[]{a,c,b,b,c,d}:new[]{a,b,c,b,d,c});}
        var t=Group(parent,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Store(name,points.ToArray(),triangles.ToArray());t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    static void Sparkles(Transform parent)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magical Source.prefab");
        var effect=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);effect.name="Gate gold sparkles";effect.transform.localPosition=new Vector3(0,3.2f,-.8f);
        foreach(var script in effect.GetComponentsInChildren<MonoBehaviour>(true)){script.enabled=false;Dirty(script);}
        foreach(var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
        {var main=particle.main;main.loop=true;main.playOnAwake=true;main.stopAction=ParticleSystemStopAction.None;main.startLifetime=.75f;main.startSize=.18f;main.startSpeed=.12f;main.maxParticles=12;main.startColor=new Color(1,.85f,.3f,.8f);var emission=particle.emission;emission.rateOverTime=5;emission.SetBursts(Array.Empty<ParticleSystem.Burst>());var shape=particle.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(10,5,.1f);Dirty(particle);}
    }
    public static object RandomGates()
    {
        var chapter=Chapter();var route=chapter.GetComponent<HighwayRoute>();var parent=route.transform.Find("HighwayChapter2_20260927");var gates=new List<HighwayChapter2Controller.MysteryGate>();
        for(int i=0;i<S("mysteryCount");i++)
        {
            var gate=Rebuild(parent,"Random bonus gate "+(i+1));Place(route,gate,S("mystery"+(i+1)),0);
            foreach(float x in new[]{-6.1f,6.1f})
            {
                Panel(gate,"Pillar "+x,new Vector3(x,3.2f,0),new Vector2(.65f,6.4f),.7f,Navy);StripedBase(gate,"Foot "+x,new Vector3(x,.25f,-.1f),1.3f);
                for(int k=0;k<9;k++){var c=Color.HSVToRGB(k/9f,.6f,1);Cube(gate,"LED "+x+"_"+k,new Vector3(x,.8f+k*.59f,-.43f),new Vector3(.32f,.32f,.12f),Mat("LED"+k,c,true));}
                Star(gate,"Golden star "+x,new Vector3(x,6.35f,-.45f),1.1f);
            }
            Panel(gate,"Marquee gold",new Vector3(0,6.55f,0),new Vector2(11.3f,1.8f),.65f,Gold);
            Panel(gate,"Marquee navy",new Vector3(0,6.55f,-.4f),new Vector2(10.95f,1.5f),.2f,Navy);
            Text(gate,"Title shadow","랜덤 보너스",new Vector3(.05f,6.52f,-.55f),new Vector2(10.2f,1.3f),11,new Color(.2f,.07f,.01f));
            Text(gate,"Random bonus title","랜덤 보너스",new Vector3(0,6.6f,-.58f),new Vector2(10.2f,1.3f),11,new Color(1,.78f,.12f));
            for(int k=0;k<12;k++)Cube(gate,"Top lamp "+k,new Vector3(-5.05f+k*.92f,7.55f,-.15f),new Vector3(.38f,.11f,.16f),Mat("TopGlow",new Color(.45f,.86f,1),true));
            var questions=new List<TMP_Text>();var icons=new List<Transform>();
            for(int side=0;side<2;side++)
            {
                var card=Group(gate,"Mystery card "+side);card.localPosition=new Vector3(side==0?-2.7f:2.7f,2.9f,-.25f);
                Panel(card,"Card navy "+side,Vector3.zero,new Vector2(4.35f,5.1f),.35f,Navy);
                Panel(card,"Card gold "+side,new Vector3(0,0,-.25f),new Vector2(4.1f,4.88f),.22f,Gold);
                Panel(card,"Card ivory "+side,new Vector3(0,0,-.4f),new Vector2(3.84f,4.62f),.12f,Ivory);
                Text(card,"Question shadow","?",new Vector3(.055f,-.1f,-.49f),new Vector2(2.6f,3.4f),34,new Color(.43f,.2f,.015f));
                questions.Add(Text(card,"Mystery question","?",new Vector3(0,0,-.52f),new Vector2(2.6f,3.4f),34,new Color(1,.7f,.06f)));
                var sprites=new[]{AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ShooterSurvival/Resources/WallBonusIcons/WallBonus_Health.png"),AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ShooterSurvival/Resources/WallBonusIcons/WallBonus_Attack.png"),chapter.ui.missileIcon,chapter.ui.magnetIcon};
                for(int k=0;k<4;k++)icons.Add(Icon(card,"Bonus symbol "+k,sprites[k],new Vector3(k%2==0?-1.25f:1.25f,k<2?1.8f:-1.8f,-.57f),.68f).transform);
                for(int k=0;k<6;k++)foreach(float x in new[]{-1.98f,1.98f})Cube(card,"Rim light "+x+"_"+k,new Vector3(x,-1.94f+k*.78f,-.52f),new Vector3(.12f,.22f,.08f),Mat("WarmGlow",new Color(1,.85f,.3f),true));
                StripedBase(gate,"Card base "+side,new Vector3(side==0?-2.7f:2.7f,.3f,-.28f),4.45f);
            }
            Sparkles(gate);gates.Add(new HighwayChapter2Controller.MysteryGate{root=gate,questions=questions.ToArray(),icons=icons.ToArray()});
        }
        chapter.mysteryGates=gates.ToArray();Dirty(chapter);Save();return new{gates=gates.Count,style="Navy marquee / gold stars / LED pillars / raised question panels"};
    }
    public static object Toll()
    {
        var chapter=Chapter();var route=chapter.GetComponent<HighwayRoute>();var parent=route.transform.Find("HighwayChapter2_20260927");var toll=Rebuild(parent,"Toll exit without barriers");Place(route,toll,S("tollAt")+S("crashQueueGap"),0);
        var blue=Mat("Toll blue",new Color(.05f,.31f,.63f));var metal=Mat("Satin metal",new Color(.65f,.72f,.76f));
        var roof=Panel(toll,"Layered canopy",new Vector3(0,8.7f,0),new Vector2(23,12),.7f,blue);roof.localRotation=Quaternion.Euler(90,0,0);
        var top=Panel(toll,"Canopy silver cap",new Vector3(0,9.13f,.1f),new Vector2(22.5f,11.3f),.2f,metal);top.localRotation=Quaternion.Euler(90,0,0);
        float median=-S("laneWidth")*1.5f-S("medianShoulder")-S("medianWidth")*.5f;
        foreach(float x in new[]{median,10.4f})foreach(float z in new[]{-4.5f,4.5f})
        {Cube(toll,"Column "+x+"_"+z,new Vector3(x,4.2f,z),new Vector3(.9f,8.4f,.9f),Ivory);Cube(toll,"Column base "+x+"_"+z,new Vector3(x,.6f,z),new Vector3(1.4f,1.2f,1.4f),metal);}
        for(int i=0;i<5;i++)Cube(toll,"Roof rib "+i,new Vector3(0,8.15f,-4.4f+i*2.2f),new Vector3(21,.35f,.25f),Ivory);
        Panel(toll,"Exit header",new Vector3(0,9.22f,-5.9f),new Vector2(12,1.45f),.35f,Navy);Text(toll,"Exit heading","요금소 출구",new Vector3(0,9.28f,-6.13f),new Vector2(11,1.25f),10,Color.white);
        foreach(int side in new[]{0,1})
        {
            float x=side==0?-4.4f:4.4f;Panel(toll,"Lane sign "+side,new Vector3(x,7.42f,-5.8f),new Vector2(7.9f,1.26f),.35f,side==0?blue:Navy);
            Text(toll,"Lane label "+side,side==0?"하이패스":"현금",new Vector3(x,7.48f,-6.02f),new Vector2(7.4f,1.05f),8,Color.white);
            Cube(toll,"Lane green signal "+side,new Vector3(x,6.52f,-5.82f),new Vector3(.9f,.18f,.18f),Mat("Signal green",new Color(.1f,1,.45f),true));
            foreach(float zz in new[]{-3.2f,2.8f})Cube(toll,"Under canopy light "+side+"_"+zz,new Vector3(x,7.92f,zz),new Vector3(5,.1f,.28f),Mat("Ceiling light",new Color(.7f,.9f,1),true));
        }
        var booth=Model(toll,"Cash booth", "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage02_Highway/066_STAGE02_HWY_BUILDING_009_Tollgate_booth_module/066_STAGE02_HWY_BUILDING_009_Tollgate_booth_module.prefab",2.6f);booth.localPosition=new Vector3(9.1f,0,-1.6f);
        var clerk=Model(toll,"Cash booth employee",Prefabs+"C09_toll_attendant.prefab",1.9f,true);clerk.localPosition=new Vector3(8.8f,.25f,-3);clerk.localRotation=Quaternion.Euler(0,180,0);
        foreach(float x in new[]{median,9.2f})for(int i=0;i<5;i++)
        {var bollard=Cube(toll,"Bollard "+x+"_"+i,new Vector3(x,.55f,-10+i*2.2f),new Vector3(.25f,1.1f,.25f),Gold);Cube(bollard,"Reflector",new Vector3(0,.2f,0),new Vector3(1.08f,.12f,1.08f),Ivory);}
        var cards=new List<Transform>();var names=new List<TMP_Text>();var icons=new List<SpriteRenderer>();var pickups=new List<HighwayUniquePickup>();
        for(int i=0;i<2;i++)
        {
            var card=Group(toll,"Unique card "+i);Place(route,card,S("tollAt")-S("tollPickupLead"),i==0?-2.2f:2.2f);card.position+=Vector3.up*2.8f;
            Panel(card,"Unique navy "+i,Vector3.zero,new Vector2(4.1f,4.65f),.3f,Navy);Panel(card,"Unique gold "+i,new Vector3(0,0,-.22f),new Vector2(3.88f,4.43f),.2f,Gold);Panel(card,"Unique ivory "+i,new Vector3(0,0,-.35f),new Vector2(3.6f,4.15f),.12f,Ivory);
            icons.Add(Icon(card,"Unique effect icon",i==0?chapter.ui.missileIcon:chapter.ui.shieldIcon,new Vector3(0,.6f,-.5f),1.9f));
            names.Add(Text(card,"Unique name","유니크 보너스",new Vector3(0,-.9f,-.51f),new Vector2(3.5f,1.1f),3.6f,new Color(.025f,.08f,.22f)));
            Text(card,"Pickup instruction","직접 획득",new Vector3(0,-1.73f,-.51f),new Vector2(3.2f,.5f),3,new Color(.16f,.28f,.4f));Star(card,"Unique star "+i,new Vector3(0,2.6f,-.25f),.6f);
            var trigger=card.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.zero;trigger.size=new Vector3(4.4f,5.6f,.85f);
            var pickup=card.gameObject.AddComponent<HighwayUniquePickup>();pickup.owner=chapter;pickup.choice=i;Dirty(pickup);cards.Add(card);pickups.Add(pickup);
        }
        chapter.uniqueCards=cards.ToArray();chapter.uniqueNames=names.ToArray();chapter.uniqueIcons=icons.ToArray();chapter.exitPickups=pickups.ToArray();chapter.tollRoof=new[]{roof.GetComponent<Renderer>(),top.GetComponent<Renderer>()};Dirty(chapter);Save();return new{physicalChoices=2,barriers=0};
    }
    public static object Police()
    {
        var chapter=Chapter();int count=0;var blue=Mat("Police blue",new Color(.035f,.23f,.68f));var yellow=Mat("Police yellow",new Color(1,.79f,.12f));
        foreach(var car in chapter.GetComponentsInChildren<HighwayVehicleEnemy>(true).Where(v=>v.kind==HighwayVehicleKind.Police))
        {
            var root=Rebuild(car.body,"Korean police livery");var shape=car.GetComponent<BoxCollider>();var b=new Bounds(shape.center,shape.size);float top=b.max.y,front=b.max.z;
            var source=car.body.GetComponentsInChildren<MeshFilter>(true).OrderByDescending(m=>m.sharedMesh.vertexCount).First();
            var projection=new GameObject("Temporary decal projection");projection.transform.SetParent(source.transform,false);var skin=projection.AddComponent<MeshCollider>();skin.sharedMesh=source.sharedMesh;Physics.SyncTransforms();
            try
            {
            // White Korean sedan body; blue/yellow door bands and an unmistakable red/blue lightbar.
            foreach(int side in new[]{-1,1})
            {
                // Stay below the source model's open window cutout; window glass is not a decal surface.
                float y=top*.36f;var from=new Vector3(side*(b.extents.x+1),y-.28f,-1.4f);var ray=Vector3.left*side;
                SkinPatch(root,car.body,skin,"Blue door "+side,from,Vector3.forward*2.8f,Vector3.up*.56f,ray,blue);
                SkinPatch(root,car.body,skin,"Yellow door "+side,from+Vector3.up*.57f,Vector3.forward*2.8f,Vector3.up*.09f,ray,yellow);
                var text=Text(root,"Korean police "+side,"경찰",SkinPoint(car.body,skin,new Vector3(from.x,y,-.2f),ray)+Vector3.right*side*.012f,new Vector2(1.2f,.45f),3.7f,Color.white);text.transform.localRotation=Quaternion.Euler(0,-side*90,0);
                var english=Text(root,"Police label "+side,"POLICE",SkinPoint(car.body,skin,new Vector3(from.x,y,.85f),ray)+Vector3.right*side*.012f,new Vector2(1,.3f),1.8f,Color.white);english.transform.localRotation=text.transform.localRotation;
            }
            SkinPatch(root,car.body,skin,"Blue hood panel",new Vector3(-1.02f,top+1,front-1.68f),Vector3.right*2.04f,Vector3.forward*1.25f,Vector3.down,blue);
            var badgePosition=SkinPoint(car.body,skin,new Vector3(0,top+1,front-1.05f),Vector3.down)+Vector3.up*.02f;
            var badge=Star(root,"Gold police badge",badgePosition,.29f);badge.localRotation=Quaternion.Euler(78,0,0);
            foreach(int side in new[]{-1,1})
            {
                var shape2D=new[]{new Vector2(.12f,-.09f),new Vector2(.8f,.23f),new Vector2(.59f,.19f),new Vector2(.38f,.09f),new Vector2(.67f,.1f),new Vector2(.33f,.01f),new Vector2(.46f,-.015f),new Vector2(.14f,-.16f)};
                var vertices=shape2D.Select(p=>new Vector3(p.x*side,p.y,0)).ToArray();var indices=new List<int>();for(int j=1;j<vertices.Length-1;j++){indices.AddRange(side>0?new[]{0,j+1,j}:new[]{0,j,j+1});}
                var wing=Group(root,"Badge wing "+side);wing.localPosition=badgePosition+Vector3.up*.015f;wing.localRotation=Quaternion.Euler(78,0,0);wing.gameObject.AddComponent<MeshFilter>().sharedMesh=Store("PoliceWing"+side,vertices,indices.ToArray());wing.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Gold;
            }
            Cube(root,"Lightbar rail",new Vector3(0,top+.04f,0),new Vector3(2.15f,.1f,.52f),Navy);
            var lamps=new List<Renderer>();foreach(int side in new[]{-1,1}){var lens=Panel(root,"Korean beacon "+side,new Vector3(side*.55f,top+.18f,0),new Vector2(.94f,.46f),.23f,Mat(side<0?"Beacon red":"Beacon blue",side<0?Color.red:new Color(.05f,.3f,1),true));lens.localRotation=Quaternion.Euler(90,0,0);lamps.Add(lens.GetComponent<Renderer>());}
            foreach(var old in car.lamps)if(old!=null)old.gameObject.SetActive(false);car.lamps=lamps.ToArray();Dirty(car);count++;
            }
            finally{Object.DestroyImmediate(projection);}
        }
        Save();return new{police=count,baseModel="V02_white_sedan",livery="Korean blue/yellow with Korean lettering"};
    }
    public static object VisibilityAndLog()
    {
        var chapter=Chapter();var number=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Chapter2_20260927/VehicleNumber.mat");
        // CanvasRenderer owns TMP's unity_GUIZTestMode at draw time; it is not a serialized material property.
        number.renderQueue=3000;Dirty(number);
        foreach(var car in chapter.GetComponentsInChildren<HighwayVehicleEnemy>(true))
        {car.healthNumber.isOverlay=false;car.healthNumber.canvas.renderMode=RenderMode.WorldSpace;Dirty(car.healthNumber);Dirty(car.healthNumber.canvas);}
        var log=chapter.singleLog;log.localScale=Vector3.one;var b=BoundsOf(log);log.localScale=Vector3.one*S("logLength")/b.size.z;
        var collider=log.GetComponent<BoxCollider>();if(collider==null)collider=log.gameObject.AddComponent<BoxCollider>();collider.center=b.center;collider.size=b.size;collider.isTrigger=true;Dirty(collider);Dirty(log);
        foreach(var old in chapter.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyContactWarning>(true))Undo.DestroyObjectImmediate(old);
        Save();return new{logLength=S("logLength"),healthCanvas="WorldSpace with body visibility checks",predictiveVehicleWarnings=0};
    }
    public static object Apply(){RandomGates();Toll();Police();return VisibilityAndLog();}
    public static object ClearTollSupports()
    {
        var chapter=Chapter();var toll=chapter.transform.Find("HighwayChapter2_20260927/Toll exit without barriers");float median=-S("laneWidth")*1.5f-S("medianShoulder")-S("medianWidth")*.5f;int moved=0;
        foreach(Transform child in toll)
            if(child.localPosition.x<0&&(child.name.StartsWith("Column ")||child.name.StartsWith("Column base ")||child.name.StartsWith("Bollard ")))
            {var p=child.localPosition;p.x=median;child.localPosition=p;Dirty(child);moved++;}
        Save();return new{supportsMoved=moved,medianLane=median};
    }
    public static object Preview()
    {
        var chapter=Chapter();var root=chapter.transform.Find("HighwayChapter2_20260927");
        var sources=new[]{chapter.GetComponentsInChildren<HighwayVehicleEnemy>(true).First(v=>v.kind==HighwayVehicleKind.Police).body,root.Find("Random bonus gate 1"),root.Find("Toll exit without barriers")};
        var names=new[]{"police-native","random-native","toll-native"};string output="outputs/highway-combat-revision-2026-09-27";Directory.CreateDirectory(output);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var light=new GameObject("Preview sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.transform.rotation=Quaternion.Euler(45,150,0);SceneManager.MoveGameObjectToScene(light.gameObject,scene);
            for(int i=0;i<sources.Length;i++)
            {
                var clone=Object.Instantiate(sources[i].gameObject);SceneManager.MoveGameObjectToScene(clone,scene);clone.SetActive(true);clone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                if(i==2)foreach(Transform child in clone.transform)if(child.name.StartsWith("Unique card"))child.gameObject.SetActive(false);
                var renderers=clone.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!(r is ParticleSystemRenderer)).ToArray();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                var camera=new GameObject("Preview camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.79f,.83f,.85f);camera.fieldOfView=30;camera.aspect=1.25f;
                Vector3 direction=Quaternion.Euler(i==0?18:10,i==0?215:12,0)*Vector3.forward;camera.transform.position=bounds.center-direction*bounds.extents.magnitude*3.45f;camera.transform.LookAt(bounds.center);
                var rt=new RenderTexture(1000,800,24);var image=new Texture2D(1000,800,TextureFormat.RGB24,false);var prior=RenderTexture.active;
                try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,800),0,0);image.Apply();File.WriteAllBytes(output+"/"+names[i]+".png",image.EncodeToPNG());}
                finally{RenderTexture.active=prior;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(clone);}
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        return new{output,images=names};
    }
}
