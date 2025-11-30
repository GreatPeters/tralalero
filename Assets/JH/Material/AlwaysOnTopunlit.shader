Shader "URP/AlwaysOnTopUnlit"
{
    Properties { _BaseColor("Color", Color) = (1,1,1,1) _BaseMap("BaseMap", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseColor;

            struct a { float4 posOS:POSITION; float2 uv:TEXCOORD0; };
            struct v { float4 posCS:SV_POSITION; float2 uv:TEXCOORD0; };

            v vert(a i){ v o; o.posCS = TransformObjectToHClip(i.posOS.xyz); o.uv=i.uv; return o; }
            half4 frag(v i):SV_Target {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                return c;
            }
            ENDHLSL
        }
    }
}
