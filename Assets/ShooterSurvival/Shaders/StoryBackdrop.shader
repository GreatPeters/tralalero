Shader "UI/StoryBackdrop"
{
    Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize; float4 _Color;
            struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            Output vert(Input i){Output o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color*_Color;return o;}
            fixed4 frag(Output i):SV_Target
            {
                float2 d=_MainTex_TexelSize.xy*12;
                fixed4 c=tex2D(_MainTex,i.uv)*.2;
                c+=(tex2D(_MainTex,i.uv+float2(d.x,0))+tex2D(_MainTex,i.uv-float2(d.x,0))+tex2D(_MainTex,i.uv+float2(0,d.y))+tex2D(_MainTex,i.uv-float2(0,d.y)))*.12;
                c+=(tex2D(_MainTex,i.uv+d)+tex2D(_MainTex,i.uv-d)+tex2D(_MainTex,i.uv+float2(d.x,-d.y))+tex2D(_MainTex,i.uv+float2(-d.x,d.y)))*.08;
                return c*i.color;
            }
            ENDCG
        }
    }
}
