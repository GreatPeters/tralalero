Shader "Tralalero/ArchitecturalGlazingFinish"
{
 Properties {
  _BaseColor("Lower reflection",Color)=(0.15,0.25,0.28,1)
  _SkyColor("Upper sky reflection",Color)=(0.48,0.61,0.64,1)
  _InteriorColor("Interior shade",Color)=(0.17,0.23,0.24,1)
  _ReflectionStrength("Reflection contrast",Range(0,1))=0.65
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Name "ForwardGlazing"
   Tags {"LightMode"="UniversalForward"}
   Cull Back ZWrite On
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma target 2.0
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor, _SkyColor, _InteriorColor;
    half _ReflectionStrength;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;};
   Varyings Vert(Attributes input){
    Varyings o;o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
    o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.uv=input.uv;return o;
   }
   half4 Frag(Varyings input):SV_Target{
    half3 v=normalize(GetCameraPositionWS()-input.positionWS);half3 n=normalize(input.normalWS);
    half facing=saturate(abs(dot(n,v)));half grazing=(1-facing)*(1-facing);
    half y=saturate(input.uv.y);half sky=smoothstep(.18h,.92h,y+v.y*.12h);
    half3 reflection=lerp(_BaseColor.rgb,_SkyColor.rgb,sky);
    half band=1-smoothstep(.04h,.19h,abs(input.uv.x+input.uv.y*.17h+v.x*.06h-.72h));
    reflection+=band*.06h;
    half lowerShade=1-smoothstep(.12h,.29h,y);
    half3 color=lerp(_InteriorColor.rgb,reflection,saturate(_ReflectionStrength+grazing*.27h));
    color*=1-lowerShade*.17h;
    half edge=min(min(input.uv.x,1-input.uv.x),min(input.uv.y,1-input.uv.y));
    color*=lerp(.82h,1.0h,smoothstep(0,.022h,edge));
    return half4(color,1);
   }
   ENDHLSL
  }
 }
 // This complete URP-only pass must not inherit Lit pass keyword state.
 Fallback Off
}
