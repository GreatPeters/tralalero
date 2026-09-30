Shader "ShooterSurvival/NoryangjinGroundPatch"
{
    Properties { _BaseColor("Color",Color)=(.04,.2,.3,.4) _Wet("Wet ripples",Range(0,1))=1 _Spraying("Active splashes",Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float fog:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;half _Wet;half _Spraying;
            CBUFFER_END
            Varyings vert(Attributes i){Varyings o;o.positionHCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.fog=ComputeFogFactor(o.positionHCS.z);return o;}
            half4 frag(Varyings i):SV_Target
            {
                float2 p=i.uv*2-1;float angle=atan2(p.y,p.x);
                float edge=_Wet>.5?length(p)*(1+.18*sin(angle*5)+.12*cos(angle*9)):max(abs(p.x),abs(p.y));
                float mask=1-smoothstep(.76,1,edge);
                float radius=frac(_Time.y*.75);
                float ripple=(1-smoothstep(.018,.055,abs(length(p+float2(.25,-.1))-radius)))*(1-radius)*.2*_Wet*_Spraying;
                half3 color=_Wet>.5?MixFog(_BaseColor.rgb+ripple,i.fog):_BaseColor.rgb;
                return half4(color,_BaseColor.a*mask);
            }
            ENDHLSL
        }
    }
}
