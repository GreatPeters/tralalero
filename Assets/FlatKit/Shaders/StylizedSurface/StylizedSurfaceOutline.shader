Shader "FlatKit/Stylized Surface With Outline"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)

        [Space(10)]
        [KeywordEnum(None, Single, Steps, Curve)]_CelPrimaryMode("Cel Shading Mode", Float) = 1
        _ColorDim ("[_CELPRIMARYMODE_SINGLE]Color Shaded", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _ColorDimSteps ("[_CELPRIMARYMODE_STEPS]Color Shaded", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _ColorDimCurve ("[_CELPRIMARYMODE_CURVE]Color Shaded", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _SelfShadingSize ("[_CELPRIMARYMODE_SINGLE]Self Shading Size", Range(0, 1)) = 0.5
        _ShadowEdgeSize ("[_CELPRIMARYMODE_SINGLE]Shadow Edge Size", Range(0, 0.5)) = 0.05
        _Flatness ("[_CELPRIMARYMODE_SINGLE]Localized Shading", Range(0, 1)) = 1.0

        [IntRange]_CelNumSteps ("[_CELPRIMARYMODE_STEPS]Number Of Steps", Range(1, 10)) = 3.0
        _CelStepTexture ("[_CELPRIMARYMODE_STEPS][LAST_PROP_STEPS]Cel steps", 2D) = "black" {}
        _CelCurveTexture ("[_CELPRIMARYMODE_CURVE][LAST_PROP_CURVE]Ramp", 2D) = "black" {}

        [Space(10)]
        [Toggle(DR_CEL_EXTRA_ON)] _CelExtraEnabled("Enable Extra Cel Layer", Int) = 0
        _ColorDimExtra ("[DR_CEL_EXTRA_ON]Color Shaded", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _SelfShadingSizeExtra ("[DR_CEL_EXTRA_ON]Self Shading Size", Range(0, 1)) = 0.6
        _ShadowEdgeSizeExtra ("[DR_CEL_EXTRA_ON]Shadow Edge Size", Range(0, 0.5)) = 0.05
        _FlatnessExtra ("[DR_CEL_EXTRA_ON]Localized Shading", Range(0, 1)) = 1.0

        [Space(10)]
        [Toggle(DR_SPECULAR_ON)] _SpecularEnabled("Enable Specular", Int) = 0
        [HDR] _FlatSpecularColor("[DR_SPECULAR_ON]Specular Color", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _FlatSpecularSize("[DR_SPECULAR_ON]Specular Size", Range(0.0, 1.0)) = 0.1
        _FlatSpecularEdgeSmoothness("[DR_SPECULAR_ON]Specular Edge Smoothness", Range(0.0, 1.0)) = 0

        [Space(10)]
        [Toggle(DR_RIM_ON)] _RimEnabled("Enable Rim", Int) = 0
        [HDR] _FlatRimColor("[DR_RIM_ON]Rim Color", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _FlatRimLightAlign("[DR_RIM_ON]Light Align", Range(0.0, 1.0)) = 0
        _FlatRimSize("[DR_RIM_ON]Rim Size", Range(0, 1)) = 0.5
        _FlatRimEdgeSmoothness("[DR_RIM_ON]Rim Edge Smoothness", Range(0, 1)) = 0.5

        [Space(10)]
        [Toggle(DR_GRADIENT_ON)] _GradientEnabled("Enable Height Gradient", Int) = 0
        [HDR] _ColorGradient("[DR_GRADIENT_ON]Gradient Color", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _GradientCenterX("[DR_GRADIENT_ON]Center X", Float) = 0
        _GradientCenterY("[DR_GRADIENT_ON]Center Y", Float) = 0
        _GradientSize("[DR_GRADIENT_ON]Size", Float) = 10.0
        _GradientAngle("[DR_GRADIENT_ON]Gradient Angle", Range(0, 360)) = 0

        [Space(10)]
        [Toggle(DR_VERTEX_COLORS_ON)] _VertexColorsEnabled("Enable Vertex Colors", Int) = 0

        _LightContribution("[FOLDOUT(Advanced Lighting){5}]Light Color Contribution", Range(0, 1)) = 0
        _LightFalloffSize("Light edge width (point / spot)", Range(0, 1)) = 0

        // Used to provide light direction to cel shading if all light in the scene is baked.
        [Space(5)]
        [Toggle(DR_ENABLE_LIGHTMAP_DIR)]_OverrideLightmapDir("Override light direction", Int) = 0
        _LightmapDirectionPitch("[DR_ENABLE_LIGHTMAP_DIR]Pitch", Range(0, 360)) = 0
        _LightmapDirectionYaw("[DR_ENABLE_LIGHTMAP_DIR]Yaw", Range(0, 360)) = 0
        [HideInInspector] _LightmapDirection("Direction", Vector) = (0, 1, 0, 0)

        [KeywordEnum(None, Multiply, Color)] _UnityShadowMode ("[FOLDOUT(Unity Built-in Shadows){4}]Mode", Float) = 0
        _UnityShadowPower("[_UNITYSHADOWMODE_MULTIPLY]Power", Range(0, 1)) = 0.2
        _UnityShadowColor("[_UNITYSHADOWMODE_COLOR]Color", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _UnityShadowSharpness("Sharpness", Range(1, 10)) = 1.0

        [MainTexture] _BaseMap("[FOLDOUT(Texture maps){6}]Albedo", 2D) = "white" {}
        [Space][KeywordEnum(Multiply, Add)]_TextureBlendingMode("[_]Blending Mode", Float) = 0
        [Space]_TextureImpact("[_]Texture Impact", Range(0, 1)) = 1.0
        [Space(20)]_BumpMap ("Normal Map", 2D) = "bump" {}
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR]_EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        // Declared so every UnityPerMaterial variable has a property (SRP Batcher rule).
        // Impact 0 leaves the shared ForwardLit colour unchanged, as before.
        [HideInInspector] _DetailMap("Detail Map", 2D) = "white" {}
        [HideInInspector] _DetailMapColor("Detail Color", Color) = (1,1,1,1)
        [HideInInspector] _DetailMapImpact("Detail Impact", Range(0, 1)) = 0.0

        [HideInInspector] _Cutoff ("Base Alpha cutoff", Range (0, 1)) = .5

        // Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0

        // --------------------- OUTLINE PROPS -----------------------
        _OutlineColor("[FOLDOUT(Outline){5}]Color", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _OutlineWidth("Width", Float) = 0.01
        _OutlineScale("Scale", Float) = 1.0
        _OutlineDepthOffset("Depth Offset", Range(0, 1)) = 0.0
        _CameraDistanceImpact("Camera Distance Impact", Range(0, 1)) = 0.0

        /* start CurvedWorld */
        //[CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1|1", Vector) = (0, 0, 0, 0)
        /* end CurvedWorld */
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"
        }
        LOD 300

        HLSLINCLUDE
        // #define FLAT_KIT_DOTS_INSTANCING_ON // Uncomment to enable DOTS instancing
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
        ENDHLSL

        // Declare these passes in this shader's keyword space. Cross-shader UsePass
        // raises native keyword-state assertions on Unity 6000.2.6f1 (71 versus 75).
        // Pass bodies stay synced with StylizedSurface.shader by
        // tools/sync-legacy-outline-passes.py; shared StylizedInput keeps SRP batching.
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForwardOnly"}

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma shader_feature_local_fragment __ _CELPRIMARYMODE_SINGLE _CELPRIMARYMODE_STEPS _CELPRIMARYMODE_CURVE
            #pragma shader_feature_local_fragment DR_CEL_EXTRA_ON
            #pragma shader_feature_local_fragment DR_GRADIENT_ON
            #pragma shader_feature_local_fragment __ _GRADIENTSPACE_WORLD _GRADIENTSPACE_LOCAL
            #pragma shader_feature_local_fragment DR_SPECULAR_ON
            #pragma shader_feature_local_fragment DR_RIM_ON
            #pragma shader_feature_local DR_VERTEX_COLORS_ON
            #pragma shader_feature_local_fragment DR_ENABLE_LIGHTMAP_DIR
            #pragma shader_feature_local_fragment __ _UNITYSHADOWMODE_MULTIPLY _UNITYSHADOWMODE_COLOR
            #pragma shader_feature_local_fragment _TEXTUREBLENDINGMODE_MULTIPLY _TEXTUREBLENDINGMODE_ADD
            #pragma shader_feature_local_fragment _UNITYSHADOW_OCCLUSION
            #pragma shader_feature_local_fragment _BASEMAP_PREMULTIPLY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            // #pragma shader_feature_local_fragment _ _SPECGLOSSMAP _SPECULAR_COLOR
            // #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #if UNITY_VERSION >= 600000
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #endif

            // -------------------------------------
            // Universal Pipeline keywords
            #if VERSION_GREATER_EQUAL(11, 0)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #else
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #endif
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #if VERSION_GREATER_EQUAL(12, 0)
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #endif
            #if UNITY_VERSION >= 202220 && UNITY_VERSION < 600000
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS
            #endif
            #if UNITY_VERSION >= 600000
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #define _ENVIRONMENTREFLECTIONS_OFF 1 // Fixes flickering when Probe Blending is enabled on Renderer.
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #else
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #endif
            #if UNITY_VERSION >= 60000012
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #endif

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #if UNITY_VERSION >= 202220
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #endif

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #if defined(FLAT_KIT_DOTS_INSTANCING_ON)
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #endif

            // Detail map.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma shader_feature_local_fragment _DETAILMAPBLENDINGMODE_MULTIPLY _DETAILMAPBLENDINGMODE_ADD _DETAILMAPBLENDINGMODE_INTERPOLATE

            TEXTURE2D(_DetailMap);
            SAMPLER(sampler_DetailMap);

            #pragma vertex StylizedPassVertex
            #pragma fragment StylizedPassFragment
            #if UNITY_VERSION >= 202230
            #define BUMP_SCALE_NOT_SUPPORTED 1
            #endif

            // TODO: Toggle _NORMALMAP from the editor script.
            #define _NORMALMAP

            #include "LibraryUrp/StylizedInput.hlsl"
            #include "LibraryUrp/LitForwardPass_DR.hlsl"
            #include "LibraryUrp/Lighting_DR.hlsl"

			/* start CurvedWorld */
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
			/* end CurvedWorld */

            ENDHLSL
        }

        // Inverted-hull outline with the same screen-space expansion as the original CG pass.
        // LightMode "OutlineLegacy" is drawn by the "Legacy Outline" RenderObjects feature of
        // the Mobile/PC renderers right after opaques, so outlines no longer interleave with
        // ForwardLit per object and batch together. Faded (transparent queue) copies skip it.
        Pass
        {
            Name "OutlineLegacy"
            Tags
            {
                "LightMode" = "OutlineLegacy"
            }
            Cull Front

            HLSLPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram
            #pragma multi_compile_fog

            #include "LibraryUrp/StylizedInput.hlsl"

            struct VertexInput
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float fogCoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VertexOutput VertexProgram(VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 clipPosition = TransformObjectToHClip(v.position.xyz * _OutlineScale);
                const float3 clipNormal = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)UNITY_MATRIX_M, v.normal));
                const half cameraDistanceImpact = lerp(clipPosition.w, 4.0, _CameraDistanceImpact);
                const float2 offset = normalize(clipNormal.xy) / _ScreenParams.xy * _OutlineWidth * cameraDistanceImpact
                    * 2.0;
                clipPosition.xy += offset;
                clipPosition.z -= _OutlineDepthOffset;
                o.position = clipPosition;
                o.fogCoord = ComputeFogFactor(clipPosition.z);
                return o;
            }

            half4 FragmentProgram(VertexOutput i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 color = _OutlineColor;
                color.rgb = MixFog(color.rgb, i.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // Same passes as FlatKit/Stylized Surface (StylizedInput-based, not SimpleLitInput).
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #if defined(FLAT_KIT_DOTS_INSTANCING_ON)
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #endif

            // -------------------------------------
            // Universal Pipeline keywords

            // -------------------------------------
            // Unity defined keywords
            #if UNITY_VERSION >= 202220
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #endif

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "LibraryUrp/StylizedInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

			/* start CurvedWorld */
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
			/* end CurvedWorld */

            ENDHLSL
        }
		Pass
        {
            Name "GBuffer"
            Tags{"LightMode" = "UniversalGBuffer"}

            ZWrite[_ZWrite]
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            // #pragma shader_feature _ALPHAPREMULTIPLY_ON
            // #pragma shader_feature_local_fragment _ _SPECGLOSSMAP _SPECULAR_COLOR
            // #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #if UNITY_VERSION >= 600000
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #endif

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #if UNITY_VERSION >= 600000
            #define _ENVIRONMENTREFLECTIONS_OFF 1 // Fixes flickering when Probe Blending is enabled on Renderer.
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #else
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #endif
            #if UNITY_VERSION >= 60000012
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #endif

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #if UNITY_VERSION >= 600000
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #endif

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #if defined(FLAT_KIT_DOTS_INSTANCING_ON)
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #endif

            #pragma vertex LitPassVertexSimple
            #pragma fragment LitPassFragmentSimple

            #define BUMP_SCALE_NOT_SUPPORTED 1

            #include "LibraryUrp/StylizedInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitGBufferPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #if UNITY_VERSION >= 202220
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #endif

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #if defined(FLAT_KIT_DOTS_INSTANCING_ON)
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #endif

            #include "LibraryUrp/StylizedInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

			/* start CurvedWorld */
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
			/* end CurvedWorld */

            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #if UNITY_VERSION >= 202220
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS
            #endif

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #if defined(FLAT_KIT_DOTS_INSTANCING_ON)
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #endif

            #include "LibraryUrp/StylizedInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"

			/* start CurvedWorld */
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
			/* end CurvedWorld */

            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaSimple
            #if UNITY_VERSION >= 202220
            #pragma shader_feature EDITOR_VISUALIZATION
            #endif

            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SPECGLOSSMAP

            #include "LibraryUrp/StylizedInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitMetaPass.hlsl"

			/* start CurvedWorld */
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
			/* end CurvedWorld */

            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "StylizedSurfaceEditor"
}
