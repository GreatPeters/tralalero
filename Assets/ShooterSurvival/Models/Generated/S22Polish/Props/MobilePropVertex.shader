Shader "ShooterSurvival/Mobile Prop Vertex"
{
    Properties { _BaseColor("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Output { float4 positionCS:SV_POSITION; half4 color:COLOR; float3 normalWS:TEXCOORD0; float fog:TEXCOORD1; };
            Output Vert(Input v)
            {
                Output o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.color=half4(SRGBToLinear(v.color.rgb),v.color.a)*_BaseColor;
                o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(Output i):SV_Target
            {
                Light light=GetMainLight();
                half shade=.55h+.45h*saturate(dot(normalize(i.normalWS),light.direction));
                return half4(MixFog(i.color.rgb*shade,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            float4 Vert(float4 p:POSITION):SV_POSITION { return TransformObjectToHClip(p.xyz); }
            half4 Frag():SV_Target { return 0; }
            ENDHLSL
        }
    }
}
