using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Read-only Play Mode probe: what the main camera draws in a dense highway
// moment, grouped by shader, queue and SRP Batcher compatibility.
public static class TralaleroHighwayBatchProbe
{
    const string Root="outputs/tralalero-reference-2026-10-01";
    static string Json(object value)
    {
        var asm=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json");var f=asm.GetType("Newtonsoft.Json.Formatting");
        return (string)asm.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object),f}).Invoke(null,new object[]{value,Enum.ToObject(f,1)});
    }
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    static int SrpCode(Shader s)
    {
        var m=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
        if(m==null||s==null)return -99;
        try{return (int)m.Invoke(null,new object[]{s,0});}catch{return -98;}
    }
    public static object Probe(string label="highway-dense")
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        var cam=Camera.main;var planes=GeometryUtility.CalculateFrustumPlanes(cam);
        var rs=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(cam.cullingMask&(1<<r.gameObject.layer))!=0&&GeometryUtility.TestPlanesAABB(planes,r.bounds)).ToArray();
        var draws=rs.SelectMany(r=>r.sharedMaterials.Select((m,i)=>new{r,m,i})).Where(x=>x.m!=null).ToArray();
        var byShader=draws.GroupBy(x=>x.m.shader).Select(g=>new{
            shader=g.Key.name,srpBatcherCode=SrpCode(g.Key),passes=g.Key.passCount,draws=g.Count(),materials=g.Select(x=>x.m).Distinct().Count(),
            instancedMaterials=g.Select(x=>x.m).Distinct().Count(m=>!AssetDatabase.Contains(m)),
            propertyBlocks=g.Select(x=>x.r).Distinct().Count(r=>r.HasPropertyBlock()),
            transparent=g.Count(x=>x.m.renderQueue>=2500),
            types=g.GroupBy(x=>x.r.GetType().Name).ToDictionary(t=>t.Key,t=>t.Count()),
            sample=g.Select(x=>PathOf(x.r.transform)).Distinct().Take(4).ToArray()}).OrderByDescending(x=>x.draws).ToArray();
        var transparentSorted=draws.Where(x=>x.m.renderQueue>=2500).Select(x=>new{x.m.shader.name,dist=(x.r.bounds.center-cam.transform.position).sqrMagnitude}).OrderByDescending(x=>x.dist).ToArray();
        int switches=0;for(int i=1;i<transparentSorted.Length;i++)if(transparentSorted[i].name!=transparentSorted[i-1].name)switches++;
        var pipeline=GraphicsSettings.currentRenderPipeline;var srp=pipeline==null?null:pipeline.GetType().GetProperty("useSRPBatcher")?.GetValue(pipeline);
        var result=new{label,frame=Time.frameCount,stats=new{UnityStats.batches,UnityStats.setPassCalls,UnityStats.drawCalls,UnityStats.triangles},
            useSRPBatcher=srp,visibleRenderers=rs.Length,materialDraws=draws.Length,transparentDraws=transparentSorted.Length,transparentShaderSwitchesBackToFront=switches,
            shadowCasters=rs.Count(r=>r.shadowCastingMode!=ShadowCastingMode.Off),byShader};
        Directory.CreateDirectory(Root);File.WriteAllText(Root+"/"+label+".json",Json(result));
        return new{label,result.stats,result.visibleRenderers,result.materialDraws,result.transparentDraws,switches,useSRPBatcher=srp,top=byShader.Take(8).Select(x=>x.shader+" x"+x.draws+" mats"+x.materials+" inst"+x.instancedMaterials+" mpb"+x.propertyBlocks+" srp"+x.srpBatcherCode).ToArray()};
    }
}
