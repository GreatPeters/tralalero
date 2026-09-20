Shader "Shooter/TalismanGlow"
{
    Properties { [PerRendererData] _MainTex("Texture",2D)="white" {} [HDR] _Tint("Tint",Color)=(2,1.5,.5,1) [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth",Float)=4 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        ZTest [_ZTest]
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _Tint;
            struct Attributes { float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;return o;}
            half4 frag(Varyings i):SV_Target{return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color*_Tint;}
            ENDHLSL
        }
    }
}
