using UnityEngine;
using UnityEngine.Rendering;

namespace IndianOceanAssets.ShooterSurvival
{
    // Owns temporary material instances only; authored materials and collision stay intact.
    internal sealed class TemporarySceneryFade
    {
        readonly Renderer renderer;
        readonly Material[] original, transparent;
        readonly Color[] colors;
        readonly Color[] outlines;
        readonly string[] colorProperties;
        public float Opacity { get; private set; } = 1f;

        public TemporarySceneryFade(Renderer target)
        {
            renderer=target;original=target.sharedMaterials;transparent=new Material[original.Length];
            colors=new Color[original.Length];outlines=new Color[original.Length];colorProperties=new string[original.Length];
            for(int i=0;i<original.Length;i++)
            {
                if(original[i]==null)continue;
                var material=new Material(original[i]){name=original[i].name+" (camera fade)",hideFlags=HideFlags.DontSave};
                transparent[i]=material;colorProperties[i]=material.HasProperty("_BaseColor")?"_BaseColor":material.HasProperty("_Color")?"_Color":"_FaceColor";
                colors[i]=material.HasProperty(colorProperties[i])?material.GetColor(colorProperties[i]):Color.white;
                if(material.HasProperty("_OutlineColor"))outlines[i]=material.GetColor("_OutlineColor");
                if(material.HasProperty("_Surface"))material.SetFloat("_Surface",1);
                if(material.HasProperty("_Mode"))material.SetFloat("_Mode",2);
                if(material.HasProperty("_SrcBlend"))material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
                if(material.HasProperty("_DstBlend"))material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                if(material.HasProperty("_ZWrite"))material.SetFloat("_ZWrite",0);
                material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
                material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.EnableKeyword("_ALPHABLEND_ON");material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                foreach(string pass in new[]{"Outline","ShadowCaster","DepthOnly","DepthNormals"})material.SetShaderPassEnabled(pass,false);
            }
            renderer.sharedMaterials=transparent;
        }
        public void Advance(bool obstructing,float deltaTime)
        {
            Opacity=Mathf.MoveTowards(Opacity,obstructing?.4f:1f,Mathf.Max(0,deltaTime)*3f);
            for(int i=0;i<transparent.Length;i++)if(transparent[i]!=null&&transparent[i].HasProperty(colorProperties[i]))
            {var color=colors[i];color.a*=Opacity;transparent[i].SetColor(colorProperties[i],color);if(transparent[i].HasProperty("_OutlineColor")){var outline=outlines[i];outline.a*=Opacity;transparent[i].SetColor("_OutlineColor",outline);}}
        }
        public void Restore()
        {
            if(renderer!=null)renderer.sharedMaterials=original;
            foreach(var material in transparent)if(material!=null)
            {if(Application.isPlaying)Object.Destroy(material);else Object.DestroyImmediate(material);}
        }
    }
}
